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
            if (trigger == null || trigger.Kind == InputKind.Gamepad)
                throw new ArgumentException(VirtualKeyboardDialog.TextFor("请选择触发键。", "Choose a trigger."));
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
        internal MacroDefinition Result { get; private set; }
        private static string T(string zh, string en) { return VirtualKeyboardDialog.TextFor(zh, en); }

        internal QuickCreateDialog(MainForm owner, QuickTemplate kind)
        {
            main = owner;
            template = kind;
            Text = T("快捷创建", "Quick create") + " · " + Title(kind);
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            MinimumSize = new Size(720, 500);
            ClientSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            BackColor = Color.FromArgb(247, 249, 252);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(16) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            root.Controls.Add(body, 0, 0);
            TableLayoutPanel fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 12, 0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.Controls.Add(fields, 0, 0);
            nameBox.Text = Title(kind);
            AddRow(fields, T("名称", "Name"), nameBox);
            triggerButton = new Button { AutoSize = true, Text = T("选择触发键…", "Choose trigger…"), MinimumSize = new Size(140, 32) };
            triggerButton.Click += ChooseTrigger;
            AddRow(fields, T("触发键", "Trigger"), VirtualKeyboardDialog.WithDropDown(triggerButton, CaptureTrigger, ChooseTrigger));
            FlowLayoutPanel mouse = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            ComboBox mouseBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 195 };
            mouseBox.Items.Add(T("或选择鼠标按钮…", "Or choose a mouse button…"));
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
            AddRow(fields, "", mouse);
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
                stepList.Height = 175;
                AddRow(fields, T("执行操作", "Actions"), stepList);
                FlowLayoutPanel actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
                Button add = ButtonFor(T("添加操作…", "Add action…"), delegate { EditAction(false); });
                Button edit = ButtonFor(T("编辑", "Edit"), delegate { EditAction(true); });
                Button remove = ButtonFor(T("删除", "Remove"), delegate
                {
                    int index = stepList.SelectedIndex;
                    if (index >= 0) { steps.RemoveAt(index); RefreshActions(); }
                });
                actions.Controls.AddRange(new Control[] { add, edit, remove });
                AddRow(fields, "", actions);
            }
            Label help = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 8), Text = kind == QuickTemplate.HeldMapping
                ? T("按住输出，松开释放；原始按键仍传给程序。0° 向前，90° 向右，±180° 向后。",
                    "Hold to output, release to stop. Original key passes through. 0° forward, 90° right, ±180° back.")
                : T("按一次触发开始；再次触发可停止。创建后可在主界面继续编辑步骤和高级参数。",
                    "Press the trigger to start; press again to stop. After creation, edit steps and advanced options in the main window.") };
            AddRow(fields, "", help);
            Label limit = new Label { AutoSize = true, Dock = DockStyle.Fill, Text = kind == QuickTemplate.HeldMapping
                ? T("按住映射、普通时序宏和高级 Hold 可以并发运行；同触发键继续按宏列表顺序决定优先级。此处预览不发送真实输入。",
                    "Held Mappings, ordinary timed macros, and Advanced Hold runs can execute concurrently. Shared triggers still use macro-list priority. Preview sends no input.")
                : T("不同的普通时序宏可以同时运行，并可与按住映射和高级 Hold 共存。此处预览不发送真实输入。",
                    "Distinct ordinary timed macros can run concurrently and coexist with Held Mappings and Advanced Hold runs. Preview sends no input.") };
            AddRow(fields, "", limit);
            FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            Button cancel = ButtonFor(T("取消", "Cancel"), delegate { DialogResult = DialogResult.Cancel; });
            cancel.DialogResult = DialogResult.Cancel;
            Button create = ButtonFor(T("创建", "Create"), CreateMacro);
            footer.Controls.AddRange(new Control[] { cancel, create });
            root.Controls.Add(footer, 0, 1);
            CancelButton = cancel;
            AcceptButton = create;
        }

        internal static string Title(QuickTemplate kind)
        {
            return kind == QuickTemplate.HeldMapping ? T("按住映射", "Held mapping") :
                kind == QuickTemplate.Repeat ? T("定次数连按", "Repeat an action") : T("顺序执行", "Sequence");
        }
        private static NumericUpDown Number(int min, int max, int value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 100 };
        }
        private static Button ButtonFor(string text, EventHandler click)
        {
            Button result = new Button { Text = text, AutoSize = true, MinimumSize = new Size(80, 32) };
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
            main.BeginStepInputCapture(delegate(InputSpec input)
            {
                if (IsDisposed) return;
                if (input != null) trigger = new TriggerSpec { Kind = input.Kind, VirtualKey = input.VirtualKey,
                    Extended = input.Extended, MatchExtended = input.VirtualKey == (int)Keys.Enter };
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
            RefreshActions();
        }
        private void RefreshActions()
        {
            stepList.Items.Clear();
            foreach (MacroStep step in steps) stepList.Items.Add(InputNames.FormatAction(step.Action) + " · " + InputNames.FormatInput(step));
            if (stepList.Items.Count > 0) stepList.SelectedIndex = stepList.Items.Count - 1;
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
