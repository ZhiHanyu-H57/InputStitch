using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace InputStitch
{
    internal enum QuickTemplate { HeldMapping, Repeat, Sequence }

    internal static class QuickMacros
    {
        internal static MacroDefinition Create(QuickTemplate template, string name, TriggerSpec trigger,
            IList<MacroStep> steps, int count)
        {
            if (trigger == null)
                throw new ArgumentException(VirtualKeyboardDialog.TextFor("请选择触发方式。", "Choose a trigger."));
            if (steps == null || steps.Count == 0 || steps.Any(s => s == null))
                throw new ArgumentException(VirtualKeyboardDialog.TextFor("请添加执行操作。", "Add an output action."));
            bool held = template == QuickTemplate.HeldMapping;
            if (held && !MacroRuntimeClassifier.IsHoldTriggerSupported(trigger))
                throw new ArgumentException(VirtualKeyboardDialog.TextFor("按住映射支持单键、修饰键组合和鼠标按钮；滚轮没有持续按下状态，不能作为按住触发键。",
                    "Held mapping supports keys, modifier chords, and mouse buttons. The wheel has no persistent down state and cannot be used as a Hold trigger."));
            if (held && (steps.Count != 1 || steps[0].Kind != InputKind.Gamepad))
                throw new ArgumentException("Held mapping requires one gamepad action.");
            if (count < 1 || count > 100000) throw new ArgumentOutOfRangeException("count");
            MacroDefinition macro = new MacroDefinition();
            macro.Name = name;
            macro.Trigger = trigger.Clone();
            macro.SuppressTrigger = false;
            macro.RunMode = held ? TriggerRunMode.Hold : TriggerRunMode.Toggle;
            macro.Infinite = held;
            macro.RepeatCount = template == QuickTemplate.Repeat ? count : 1;
            macro.Steps = steps.Select(s => s.Clone()).ToList();
            if (held)
            {
                macro.Steps[0].Action = MacroAction.Down;
                macro.Steps[0].DelayMs = 0;
                macro.Steps[0].RandomDelay = false;
            }
            return macro;
        }
    }

    // Definition-only UI: preview never connects a controller or sends any input.
    internal sealed class QuickCreateDialog : Form
    {
        private readonly MainForm main;
        private readonly QuickTemplate template;
        private TriggerSpec trigger;
        private readonly TextBox nameBox = new TextBox();
        private readonly Button triggerButton;
        private readonly ComboBox controlBox = new ComboBox();
        private readonly NumericUpDown angle = Number(-180, 180, 0);
        private readonly NumericUpDown strength = Number(1, 100, 80);
        private readonly NumericUpDown count = Number(1, 100000, 10);
        private readonly Label angleLabel;
        private readonly Label strengthLabel;
        private readonly GamepadPreviewControl preview = new GamepadPreviewControl();
        private readonly ListBox stepList = new ListBox();
        private readonly List<MacroStep> steps = new List<MacroStep>();
        private Button createButton;
        private Button editActionButton, removeActionButton;
        private Label readyLabel;
        internal MacroDefinition Result { get; private set; }
        private static string T(string zh, string en) { return VirtualKeyboardDialog.TextFor(zh, en); }

        internal QuickCreateDialog(MainForm owner, QuickTemplate kind)
        {
            main = owner;
            template = kind;
            Text = T("快捷创建", "Quick create") + " · " + Title(kind);
            Font = EditorTheme.BodyFont();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            MinimumSize = new Size(780, 580);
            ClientSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            BackColor = Color.FromArgb(247, 249, 252);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14, 10, 14, 10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Math.Max(64, Font.Height * 2 + 16)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);
            root.Controls.Add(new EditorIntro { Dock = DockStyle.Fill, Text = Title(kind), Description = T("设置触发键和执行操作，完成后即可在主界面使用。", "Choose a shortcut and an action. Your macro will be ready in the main window.") }, 0, 0);
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, RowCount = 1, BackColor = Color.White, Padding = new Padding(12) };
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            root.Controls.Add(body, 0, 1);
            TableLayoutPanel fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 12, 0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.Controls.Add(fields, 0, 0);
            nameBox.Text = Title(kind);
            AddRow(fields, T("名称", "Name"), nameBox);
            triggerButton = new EditorButton { AutoSize = true, Text = T("选择触发键…", "Choose trigger…"), MinimumSize = new Size(140, 36) };
            triggerButton.Click += ChooseTrigger;
            AddRow(fields, T("触发键", "Trigger"), VirtualKeyboardDialog.WithDropDown(triggerButton, CaptureTrigger, ChooseTrigger));
            FlowLayoutPanel mouse = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            ComboBox mouseBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 195 };
            mouseBox.Items.Add(T("选择鼠标按钮…", "Choose a mouse button…"));
            mouseBox.Width = Math.Max(195, TextRenderer.MeasureText(mouseBox.Items[0].ToString(), Font).Width + 32);
            InputKind[] mouseKinds = { InputKind.MouseLeft, InputKind.MouseRight, InputKind.MouseMiddle, InputKind.MouseX1, InputKind.MouseX2 };
            foreach (InputKind value in mouseKinds) mouseBox.Items.Add(InputNames.FormatInput(new InputSpec { Kind = value }));
            mouseBox.SelectedIndex = 0;
            mouseBox.SelectedIndexChanged += delegate
            {
                if (mouseBox.SelectedIndex <= 0) return;
                if (main != null) main.CancelCapture();
                trigger = new TriggerSpec { Kind = mouseKinds[mouseBox.SelectedIndex - 1], VirtualKey = 0 };
                UpdateTrigger();
                mouseBox.SelectedIndex = 0;
            };
            mouse.Controls.Add(mouseBox);
            AddRow(fields, T("或用鼠标", "Or use mouse"), mouse);
            controlBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (GamepadControl value in Enum.GetValues(typeof(GamepadControl))) controlBox.Items.Add(InputNames.FormatGamepadControl(value));
            controlBox.SelectedIndex = (int)GamepadControl.LeftStick;
            if (kind == QuickTemplate.HeldMapping)
            {
                AddRow(fields, T("手柄操作", "Gamepad"), controlBox);
                angleLabel = AddRow(fields, T("方向 (°)", "Direction (°)"), angle);
                strengthLabel = AddRow(fields, T("力度 (%)", "Strength (%)"), strength);
                body.Controls.Add(preview, 1, 0);
                preview.Dock = DockStyle.Fill;
                controlBox.SelectedIndexChanged += delegate { UpdatePreview(); };
                angle.ValueChanged += delegate { UpdatePreview(); };
                strength.ValueChanged += delegate { UpdatePreview(); };
                UpdatePreview();
            }
            else
            {
                body.SetColumnSpan(fields, 2);
                if (kind == QuickTemplate.Repeat) AddRow(fields, T("执行次数", "Repeat count"), count);
                fields.Dock = DockStyle.Fill;
                fields.AutoSize = false;
                stepList.IntegralHeight = false;
                stepList.DrawMode = DrawMode.OwnerDrawFixed;
                stepList.Font = Font;
                stepList.ItemHeight = Math.Max(28, stepList.Font.Height + 8);
                stepList.FontChanged += delegate { stepList.ItemHeight = Math.Max(28, stepList.Font.Height + 8); };
                stepList.DrawItem += delegate(object sender, DrawItemEventArgs e)
                {
                    if (e.Index < 0) return;
                    e.DrawBackground();
                    TextRenderer.DrawText(e.Graphics, stepList.Items[e.Index].ToString(), e.Font,
                        Rectangle.Inflate(e.Bounds, -5, -2), e.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    e.DrawFocusRectangle();
                };
                stepList.BorderStyle = BorderStyle.FixedSingle;
                AddRow(fields, T("执行步骤", "Steps"), stepList);
                fields.RowStyles[fields.GetRow(stepList)] = new RowStyle(SizeType.Percent, 100);
                FlowLayoutPanel actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
                Button add = ButtonFor(T("添加步骤…", "Add step…"), delegate { EditAction(false); });
                add.Name = "addActionButton";
                Button edit = ButtonFor(T("编辑", "Edit"), delegate { EditAction(true); });
                editActionButton = edit;
                Button remove = ButtonFor(T("删除", "Remove"), delegate
                {
                    int index = stepList.SelectedIndex;
                    if (index >= 0) { steps.RemoveAt(index); RefreshActions(index); }
                });
                removeActionButton = remove;
                stepList.SelectedIndexChanged += delegate { UpdateActionButtons(); };
                stepList.DoubleClick += delegate { EditAction(true); };
                UpdateActionButtons();
                actions.Controls.AddRange(new Control[] { add, edit, remove });
                AddRow(fields, "", actions);
            }
            Label help = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 8), Text = kind == QuickTemplate.HeldMapping
                ? T("按住输出，松开释放；原始按键仍传给程序。0° 向前，90° 向右，±180° 向后。",
                    "Hold to output, release to stop. Original key passes through. 0° forward, 90° right, ±180° back.")
                : T("按触发键开始；运行中再按一次可停止。完成后自动保存，仍可在主界面修改。",
                    "Press your shortcut to start; press again to stop early. Creation saves your macro; you can edit it later.") };
            AddRow(fields, "", help);
            readyLabel = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Accent, TextAlign = ContentAlignment.MiddleLeft, MaximumSize = new Size(520, 0), Padding = new Padding(0, 4, 12, 4) };
            TableLayoutPanel footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Padding = new Padding(0, 12, 0, 0), MinimumSize = new Size(0, 58) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Button cancel = ButtonFor(T("取消", "Cancel"), delegate { DialogResult = DialogResult.Cancel; });
            cancel.DialogResult = DialogResult.Cancel;
            Button create = ButtonFor(T("创建并保存", "Create and save"), CreateMacro);
            createButton = create;
            footer.Controls.Add(readyLabel, 0, 0);
            footer.Controls.Add(create, 1, 0);
            footer.Controls.Add(cancel, 2, 0);
            root.Controls.Add(footer, 0, 2);
            CancelButton = cancel;
            AcceptButton = create;
            EditorTheme.Apply(this);
            mouse.MinimumSize = new Size(0, mouseBox.Height + mouseBox.Margin.Vertical);
            EditorTheme.Primary(create, false);
            UpdateReadyState();
        }

        internal static string Title(QuickTemplate kind)
        {
            return kind == QuickTemplate.HeldMapping ? T("按住控制手柄", "Hold a controller input") :
                kind == QuickTemplate.Repeat ? T("重复操作", "Repeat an action") : T("按顺序执行", "Run a sequence");
        }
        private static NumericUpDown Number(int min, int max, int value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 100 };
        }
        private static Button ButtonFor(string text, EventHandler click)
        {
            Button result = new EditorButton { Text = text, AutoSize = true, MinimumSize = new Size(80, 36) };
            result.Click += click;
            return result;
        }
        private static Label AddRow(TableLayoutPanel table, string text, Control control)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label label = new Label { AutoSize = true, Text = text, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) };
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 5, 0, 5);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
            return label;
        }
        private void UpdateTrigger()
        {
            triggerButton.Text = trigger == null ? T("选择触发键…", "Choose trigger…") : InputNames.FormatTrigger(trigger);
            triggerButton.Enabled = true;
            UpdateReadyState();
        }
        private void ChooseTrigger(object sender, EventArgs e)
        {
            if (main != null) main.CancelCapture();
            using (VirtualKeyboardDialog dialog = new VirtualKeyboardDialog(true, VirtualKeyboardDialog.InputsFromTrigger(trigger)))
                if (dialog.ShowDialog(this) == DialogResult.OK) trigger = dialog.SelectedTrigger;
            UpdateTrigger();
        }
        private void CaptureTrigger(object sender, EventArgs e)
        {
            if (main == null) return;
            triggerButton.Text = T("请按键…（Esc 取消）", "Press a key… (Esc cancels)");
            triggerButton.Enabled = false;
            main.BeginQuickTriggerCapture(delegate(TriggerSpec captured)
            {
                if (IsDisposed) return;
                if (captured != null) trigger = captured;
                UpdateTrigger();
            });
        }
        private void UpdatePreview()
        {
            GamepadControl control = (GamepadControl)controlBox.SelectedIndex;
            bool stick = control == GamepadControl.LeftStick || control == GamepadControl.RightStick;
            bool analog = stick || control == GamepadControl.LeftTrigger || control == GamepadControl.RightTrigger;
            angle.Visible = angleLabel.Visible = stick;
            strength.Visible = strengthLabel.Visible = analog;
            preview.SetState(true, control, (int)angle.Value, (int)strength.Value, (int)strength.Value);
        }
        private void EditAction(bool edit)
        {
            int index = stepList.SelectedIndex;
            if (edit && index < 0) return;
            if (main != null) main.CancelCapture();
            using (StepEditDialog dialog = new StepEditDialog(main, edit ? steps[index] : null))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (edit) { steps.RemoveAt(index); steps.InsertRange(index, dialog.ResultSteps); }
                else steps.AddRange(dialog.ResultSteps);
            }
            RefreshActions(edit ? index : steps.Count - 1);
        }
        private void RefreshActions()
        {
            RefreshActions(steps.Count - 1);
        }
        private void UpdateActionButtons()
        {
            bool selected = stepList.SelectedIndex >= 0;
            if (editActionButton != null) editActionButton.Enabled = selected;
            if (removeActionButton != null) removeActionButton.Enabled = selected;
        }
        private void RefreshActions(int selectedIndex)
        {
            stepList.Items.Clear();
            foreach (MacroStep step in steps)
            {
                string hold = step.Action == MacroAction.Press && step.Kind != InputKind.WheelUp && step.Kind != InputKind.WheelDown
                    ? T(" · 按住 ", " · Hold ") + step.HoldMs + " ms" : "";
                string delay = step.RandomDelay ? Math.Min(step.RandomDelayMinMs, step.RandomDelayMaxMs) + "–" + Math.Max(step.RandomDelayMinMs, step.RandomDelayMaxMs)
                    : step.DelayMs.ToString();
                stepList.Items.Add(InputNames.FormatAction(step.Action) + " · " + InputNames.FormatInput(step) + hold + T(" · 等待 ", " · Wait ") + delay + " ms");
            }
            if (stepList.Items.Count > 0) stepList.SelectedIndex = Math.Min(Math.Max(0, selectedIndex), stepList.Items.Count - 1);
            UpdateActionButtons();
            UpdateReadyState();
        }
        private void UpdateReadyState()
        {
            if (createButton == null) return;
            bool hasActions = template == QuickTemplate.HeldMapping || steps.Count > 0;
            bool validTrigger = trigger != null && (template != QuickTemplate.HeldMapping || MacroRuntimeClassifier.IsHoldTriggerSupported(trigger));
            createButton.Enabled = validTrigger && hasActions;
            readyLabel.Text = trigger == null ? T("下一步：选择用来启动宏的触发键。", "Next: choose the shortcut that starts your macro.")
                : !validTrigger ? T("滚轮不能持续按住，请改选键盘键或鼠标按钮。", "The wheel cannot be held. Choose a key or mouse button.")
                : !hasActions ? T("下一步：点击“添加步骤”，选择宏要替你按的键。", "Next: add a step and choose the key your macro will press.")
                : T("已准备好，点击“创建并保存”，返回主界面运行。", "Ready. Create and save, then run it from the main window.");
        }
        private void CreateMacro(object sender, EventArgs e)
        {
            try
            {
                if (template == QuickTemplate.HeldMapping)
                {
                    int x, y;
                    GamepadVector.ToCartesian((int)angle.Value, (int)strength.Value, out x, out y);
                    steps.Clear();
                    steps.Add(new MacroStep { Kind = InputKind.Gamepad, GamepadControl = (GamepadControl)controlBox.SelectedIndex,
                        GamepadX = x, GamepadY = y, GamepadValue = (int)strength.Value });
                }
                Result = QuickMacros.Create(template, String.IsNullOrWhiteSpace(nameBox.Text) ? Title(template) : nameBox.Text.Trim(), trigger, steps, (int)count.Value);
                DialogResult = DialogResult.OK;
            }
            catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (main != null) main.CancelCapture();
            base.OnFormClosed(e);
        }
    }
}
