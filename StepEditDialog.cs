using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Extracted from InputStitch.cs during the incremental MainForm decomposition (architecture-review
    // finding F4); members moved verbatim, behaviour is unchanged.
    public class StepEditDialog : Form
    {
        private ComboBox actionBox;
        private ComboBox inputTypeBox;
        private Button captureButton;
        private Button recordButton;
        private Label inputText;
        private FlowLayoutPanel inputPanel;
        private List<InputSpec> selectedInputs;
        private Label inputLabel;
        private Label gamepadLabel;
        private FlowLayoutPanel gamepadPanel;
        private ComboBox gamepadControlBox;
        private Label stickAngleLabel;
        private Label stickStrengthLabel;
        private Label triggerValueLabel;
        private NumericUpDown stickAngleBox;
        private NumericUpDown stickStrengthBox;
        private NumericUpDown triggerValueBox;
        private GamepadPreviewControl gamepadPreview;
        private NumericUpDown holdBox;
        private NumericUpDown delayBox;
        private CheckBox randomDelayBox;
        private NumericUpDown randomMinBox;
        private NumericUpDown randomMaxBox;
        private Label holdLabel;
        private Label fixedDelayLabel;
        private Label randomDelayLabel;
        private InputSpec selectedInput;
        private MainForm ownerMain;
        private bool loadingControls = true;
        private Label stepPreviewText;
        private EditorKeyPreview keyPreview;
        private MouseInputDiagram mousePreview;
        private Button confirmButton;
        private Control angleField, strengthField, triggerField;

        public MacroStep ResultStep;
        public List<MacroStep> ResultSteps;

        public StepEditDialog(MainForm owner, MacroStep source)
        {
            SuspendLayout(); ownerMain = owner;
            Text = source == null ? "添加宏步骤" : "编辑宏步骤";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 500);
            ClientSize = new Size(940, 540);
            Font = EditorTheme.BodyFont();
            BackColor = Color.FromArgb(247, 249, 252);

            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14, 10, 14, 10) }; shell.SuspendLayout();
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(shell);
            shell.Controls.Add(new EditorIntro { Dock = DockStyle.Fill, Text = source == null ? Localizer.T("添加宏步骤") : Localizer.T("编辑宏步骤"),
                Description = Localizer.IsEnglish ? "Choose what to press, how long to hold it, and when to move on." : "选择要按的键、按住多久，以及多久后执行下一步。" }, 0, 0);

            TableLayoutPanel root = new TableLayoutPanel(); root.SuspendLayout();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.BackColor = Color.White;
            root.AutoScroll = true;
            root.ColumnCount = 4;
            root.RowCount = 8;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
            for (int i = 0; i < 7; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.Controls.Add(root, 0, 1);

            Label aLabel = new Label();
            aLabel.Text = "操作：";
            aLabel.Dock = DockStyle.Fill;
            aLabel.MinimumSize = new Size(0, 34);
            aLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(aLabel, 0, 0);

            actionBox = new EditorChoice();
            actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionBox.Items.AddRange(new object[] { EditorDialogs.T("按一下（自动松开）", "Tap (release automatically)"), EditorDialogs.T("持续按住（稍后松开）", "Hold (release later)"), Localizer.T("松开按键") });
            actionBox.Dock = DockStyle.Fill;
            actionBox.Margin = new Padding(3, 4, 3, 6);
            actionBox.SelectedIndexChanged += delegate { UpdateHoldEnabled(); };
            root.Controls.Add(actionBox, 1, 0);
            root.SetColumnSpan(actionBox, 2);

            Label typeLabel = new Label();
            typeLabel.Text = EditorDialogs.T("操作设备：", "Device:");
            typeLabel.Dock = DockStyle.Fill;
            typeLabel.MinimumSize = new Size(0, 34);
            typeLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(typeLabel, 0, 1);

            inputTypeBox = new EditorChoice();
            inputTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            inputTypeBox.Items.Add(EditorDialogs.T("键盘", "Keyboard"));
            inputTypeBox.Items.Add(Localizer.T("虚拟手柄"));
            inputTypeBox.Items.Add(EditorDialogs.T("鼠标", "Mouse"));
            inputTypeBox.Dock = DockStyle.Fill;
            inputTypeBox.Margin = new Padding(3, 4, 3, 6);
            inputTypeBox.SelectedIndexChanged += delegate { if (!loadingControls) UpdateInputMode(); };
            root.Controls.Add(inputTypeBox, 1, 1);
            root.SetColumnSpan(inputTypeBox, 2);

            inputLabel = new Label();
            inputLabel.Text = "按键/鼠标：";
            inputLabel.Dock = DockStyle.Fill;
            inputLabel.MinimumSize = new Size(0, 34);
            inputLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(inputLabel, 0, 2);

            inputPanel = new FlowLayoutPanel { AutoSize=true, AutoSizeMode=AutoSizeMode.GrowAndShrink,
                Dock=DockStyle.Fill, WrapContents=true, Margin=new Padding(3,3,3,6) }; inputPanel.SuspendLayout();
            inputText = new Label { AutoSize=true, Font=new Font(Font,FontStyle.Bold), Anchor=AnchorStyles.Left,
                Margin=new Padding(0,6,10,4) };
            captureButton = new EditorButton { Text=EditorDialogs.T("键盘 / 鼠标图…", "Keyboard / mouse…"), AutoSize=true,
                MinimumSize=new Size(118,32), Margin=new Padding(0,0,6,4) };
            captureButton.AccessibleDescription=EditorDialogs.T("在键盘图中选择键盘键、组合按键或鼠标按钮。", "Choose keys, a key combination or a mouse button on the diagram.");
            captureButton.Click += SelectVirtualKeyboardInput;
            recordButton = new EditorButton { Text=EditorDialogs.T("实际按键录入…", "Press a key…"), AutoSize=true,
                MinimumSize=new Size(118,32), Margin=new Padding(0,0,0,4), Enabled=ownerMain!=null };
            recordButton.AccessibleDescription=EditorDialogs.T("点击后，按下要使用的键盘键、鼠标按钮或滚轮；Esc 取消。", "Click, then press a keyboard key, mouse button or wheel. Escape cancels.");
            recordButton.Click += CaptureButton_Click;
            inputPanel.Controls.AddRange(new Control[]{inputText,captureButton,recordButton});
            root.Controls.Add(inputPanel,1,2);
            root.SetColumnSpan(inputPanel,2);

            gamepadLabel = new Label();
            gamepadLabel.Text = "手柄控制：";
            gamepadLabel.Dock = DockStyle.Fill;
            gamepadLabel.MinimumSize = new Size(0, 38);
            gamepadLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(gamepadLabel, 0, 3);

            gamepadPanel = new FlowLayoutPanel(); gamepadPanel.SuspendLayout();
            gamepadPanel.Dock = DockStyle.Fill;
            gamepadPanel.AutoSize = true;
            gamepadPanel.WrapContents = true;
            gamepadPanel.Margin = new Padding(0);
            gamepadControlBox = new EditorChoice();
            gamepadControlBox.DropDownStyle = ComboBoxStyle.DropDownList;
            gamepadControlBox.Width = 250;
            gamepadControlBox.Margin = new Padding(3, 4, 14, 6);
            foreach (GamepadControl control in Enum.GetValues(typeof(GamepadControl)))
                gamepadControlBox.Items.Add(InputNames.FormatGamepadControl(control));
            gamepadControlBox.SelectedIndexChanged += delegate { if (!loadingControls) UpdateGamepadControl(); };
            gamepadPanel.Controls.Add(gamepadControlBox);
            gamepadPanel.SetFlowBreak(gamepadControlBox,true);

            stickAngleLabel = MakeInlineLabel(Localizer.T("摇杆方向："));
            stickAngleBox = MakeNumberBox(-180, 180, 0, 5);
            stickAngleBox.ValueChanged += GamepadVectorChanged;
            angleField=InlineField(stickAngleLabel,stickAngleBox);
            gamepadPanel.Controls.Add(angleField);
            stickStrengthLabel = MakeInlineLabel(Localizer.T("摇杆力度："));
            stickStrengthBox = MakeNumberBox(0, 100, 100, 5);
            stickStrengthBox.ValueChanged += GamepadVectorChanged;
            strengthField=InlineField(stickStrengthLabel,stickStrengthBox);
            gamepadPanel.Controls.Add(strengthField);
            triggerValueLabel = MakeInlineLabel(Localizer.T("扳机力度："));
            triggerValueBox = MakeNumberBox(0, 100, 100, 5);
            triggerValueBox.ValueChanged += TriggerValueChanged;
            triggerField=InlineField(triggerValueLabel,triggerValueBox);
            gamepadPanel.Controls.Add(triggerField);
            root.Controls.Add(gamepadPanel, 1, 3);
            root.SetColumnSpan(gamepadPanel, 2);

            gamepadPreview = new GamepadPreviewControl();
            gamepadPreview.Dock = DockStyle.Fill;
            gamepadPreview.Margin = new Padding(16, 0, 0, 8);
            root.Controls.Add(gamepadPreview, 3, 0);
            root.SetRowSpan(gamepadPreview, 7);
            keyPreview = new EditorKeyPreview { Dock = DockStyle.Fill, Margin = new Padding(16, 0, 0, 8) };
            root.Controls.Add(keyPreview, 3, 0);
            root.SetRowSpan(keyPreview, 7);
            mousePreview = new MouseInputDiagram { Dock = DockStyle.Fill, Margin = new Padding(16, 0, 0, 8) };
            mousePreview.InputSelected += delegate(InputKind kind)
            {
                if (ownerMain != null) ownerMain.CancelCapture();
                selectedInputs = null;
                selectedInput = new InputSpec { Kind = kind, VirtualKey = 0 };
                RefreshInputText(); UpdateHoldEnabled();
            };
            root.Controls.Add(mousePreview, 3, 0);
            root.SetRowSpan(mousePreview, 7);

            holdLabel = new Label();
            holdLabel.Text = "按住时长：";
            holdLabel.Dock = DockStyle.Fill;
            holdLabel.MinimumSize = new Size(0, 36);
            holdLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(holdLabel, 0, 4);

            FlowLayoutPanel holdRow = new FlowLayoutPanel(); holdRow.SuspendLayout();
            holdRow.Dock = DockStyle.Fill;
            holdRow.AutoSize = true;
            holdRow.WrapContents = true;
            holdRow.Margin = new Padding(0);

            holdBox = new NumericUpDown();
            holdBox.Size = new Size(110, 26);
            holdBox.Minimum = 0;
            holdBox.Maximum = 600000;
            holdBox.Value = 30;
            holdBox.Margin = new Padding(3, 4, 6, 6);
            holdRow.Controls.Add(holdBox);

            Label ms1 = new Label();
            ms1.Text = "ms";
            ms1.AutoSize = true;
            ms1.Margin = new Padding(0, 7, 24, 0);
            holdRow.Controls.Add(ms1);

            randomDelayBox = new CheckBox();
            randomDelayBox.Text = "随机步骤间隔";
            randomDelayBox.AutoSize = true;
            randomDelayBox.Margin = new Padding(0, 6, 0, 0);
            randomDelayBox.CheckedChanged += delegate { UpdateDelayControls(); };
            holdRow.Controls.Add(randomDelayBox);
            root.Controls.Add(holdRow, 1, 4);
            root.SetColumnSpan(holdRow, 2);

            fixedDelayLabel = new Label();
            fixedDelayLabel.Text = "步骤后间隔：";
            fixedDelayLabel.Dock = DockStyle.Fill;
            fixedDelayLabel.MinimumSize = new Size(0, 36);
            fixedDelayLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(fixedDelayLabel, 0, 5);

            FlowLayoutPanel delayRow = new FlowLayoutPanel(); delayRow.SuspendLayout();
            delayRow.Dock = DockStyle.Fill;
            delayRow.AutoSize = true;
            delayRow.WrapContents = false;
            delayRow.Margin = new Padding(0);

            delayBox = new NumericUpDown();
            delayBox.Size = new Size(110, 26);
            delayBox.Minimum = 0;
            delayBox.Maximum = 600000;
            delayBox.Value = 50;
            delayBox.Margin = new Padding(3, 4, 6, 6);
            delayRow.Controls.Add(delayBox);

            Label ms2 = new Label();
            ms2.Text = "ms";
            ms2.AutoSize = true;
            ms2.Margin = new Padding(0, 7, 0, 0);
            delayRow.Controls.Add(ms2);
            root.Controls.Add(delayRow, 1, 5);
            root.SetColumnSpan(delayRow, 2);

            randomDelayLabel = new Label();
            randomDelayLabel.Text = "随机范围：";
            randomDelayLabel.Dock = DockStyle.Fill;
            randomDelayLabel.MinimumSize = new Size(0, 36);
            randomDelayLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(randomDelayLabel, 0, 6);

            FlowLayoutPanel randomRow = new FlowLayoutPanel(); randomRow.SuspendLayout();
            randomRow.Dock = DockStyle.Fill;
            randomRow.AutoSize = true;
            randomRow.WrapContents = false;
            randomRow.Margin = new Padding(0);

            randomMinBox = new NumericUpDown();
            randomMinBox.Size = new Size(90, 26);
            randomMinBox.Minimum = 0;
            randomMinBox.Maximum = 600000;
            randomMinBox.Value = 45;
            randomMinBox.Margin = new Padding(3, 4, 6, 6);
            randomRow.Controls.Add(randomMinBox);

            Label dash = new Label();
            dash.Text = "～";
            dash.AutoSize = true;
            dash.Margin = new Padding(0, 7, 6, 0);
            randomRow.Controls.Add(dash);

            randomMaxBox = new NumericUpDown();
            randomMaxBox.Size = new Size(90, 26);
            randomMaxBox.Minimum = 0;
            randomMaxBox.Maximum = 600000;
            randomMaxBox.Value = 60;
            randomMaxBox.Margin = new Padding(0, 4, 6, 6);
            randomRow.Controls.Add(randomMaxBox);

            Label ms3 = new Label();
            ms3.Text = "ms";
            ms3.AutoSize = true;
            ms3.Margin = new Padding(0, 7, 0, 0);
            randomRow.Controls.Add(ms3);
            root.Controls.Add(randomRow, 1, 6);
            root.SetColumnSpan(randomRow, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel(); buttons.SuspendLayout();
            buttons.Dock = DockStyle.Fill;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 16, 0, 0);

            Button ok = new EditorButton();
            confirmButton = ok;
            ok.Text = source == null ? (Localizer.IsEnglish ? "Add step" : "添加步骤") : (Localizer.IsEnglish ? "Save step" : "保存步骤");
            ok.AutoSize = true;
            ok.MinimumSize = new Size(96, 34);
            ok.DialogResult = DialogResult.None;
            ok.Click += Ok_Click;
            buttons.Controls.Add(ok);

            Button cancel = new EditorButton();
            cancel.Text = "取消";
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size(96, 34);
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancel);
            shell.Controls.Add(buttons, 0, 2);
            CancelButton = cancel;
            AcceptButton = ok;
            stepPreviewText = new Label { AutoSize = true, Dock = DockStyle.Top, ForeColor = EditorTheme.Accent,
                BackColor = EditorTheme.Selection, Padding = new Padding(12), Margin = new Padding(0, 14, 0, 0) };
            root.Controls.Add(stepPreviewText, 0, 7);
            root.SetColumnSpan(stepPreviewText, 4);
            foreach (NumericUpDown number in new[] { holdBox, delayBox, randomMinBox, randomMaxBox })
                number.ValueChanged += delegate { UpdateStepPreview(); };

            if (source == null)
            {
                actionBox.SelectedIndex = 0;
                selectedInput = new InputSpec();
                selectedInput.Kind = InputKind.Keyboard;
                selectedInput.VirtualKey = (int)Keys.A;
            }
            else
            {
                actionBox.SelectedIndex = source.Action == MacroAction.Press ? 0 : (source.Action == MacroAction.Down ? 1 : 2);
                selectedInput = new InputSpec();
                selectedInput.Kind = source.Kind;
                selectedInput.VirtualKey = source.VirtualKey;
                selectedInput.ScanCode = source.ScanCode;
                selectedInput.Extended = source.Extended;
                selectedInput.GamepadControl = source.GamepadControl;
                selectedInput.GamepadX = source.GamepadX;
                selectedInput.GamepadY = source.GamepadY;
                selectedInput.GamepadValue = source.GamepadValue;
                holdBox.Value = ClampDecimal(source.HoldMs, holdBox.Minimum, holdBox.Maximum);
                delayBox.Value = ClampDecimal(source.DelayMs, delayBox.Minimum, delayBox.Maximum);
                randomDelayBox.Checked = source.RandomDelay;
                randomMinBox.Value = ClampDecimal(source.RandomDelayMinMs, randomMinBox.Minimum, randomMinBox.Maximum);
                randomMaxBox.Value = ClampDecimal(source.RandomDelayMaxMs, randomMaxBox.Minimum, randomMaxBox.Maximum);
            }
            int initialAngle;
            int initialStrength;
            GamepadVector.FromCartesian(selectedInput.GamepadX, selectedInput.GamepadY, out initialAngle, out initialStrength);
            inputTypeBox.SelectedIndex = DeviceIndex(selectedInput);
            gamepadControlBox.SelectedIndex = Math.Max(0, Math.Min(gamepadControlBox.Items.Count - 1, (int)selectedInput.GamepadControl));
            stickAngleBox.Value = ClampDecimal(initialAngle, stickAngleBox.Minimum, stickAngleBox.Maximum);
            stickStrengthBox.Value = ClampDecimal(initialStrength, stickStrengthBox.Minimum, stickStrengthBox.Maximum);
            triggerValueBox.Value = ClampDecimal(selectedInput.GamepadValue, triggerValueBox.Minimum, triggerValueBox.Maximum);
            loadingControls = false;
            UpdateInputMode();
            RefreshInputText();
            UpdateHoldEnabled();
            UpdateDelayControls();
            Localizer.ApplyStaticControls(this);
            foreach (Label label in new[] { aLabel, typeLabel, inputLabel, gamepadLabel, holdLabel, fixedDelayLabel, randomDelayLabel })
                label.AutoSize = true;
            EditorTheme.Apply(this);
            EditorTheme.Primary(ok, false);
            RefreshInputText();
            UpdateStepPreview();
            root.Layout += delegate
            {
                float scale=Math.Max(Font.GetHeight()/Font.GetHeight(96F), Font.Size/EditorTheme.BodyFontSize);
                float previewWidth=Math.Min(380*scale,Math.Max(260*scale,root.ClientSize.Width*.4F));
                if(root.ColumnStyles[3].Width!=previewWidth) root.ColumnStyles[3].Width=previewWidth;
                inputText.MaximumSize=new Size((int)(260*scale),0);
                int height = root.Padding.Vertical;
                foreach (Control child in root.Controls)
                    if (child.Visible) height = Math.Max(height,child.Bottom-root.AutoScrollPosition.Y+child.Margin.Bottom+root.Padding.Bottom);
                Size extent = new Size(0,height);
                if (root.AutoScrollMinSize != extent) root.AutoScrollMinSize = extent;
            };
            EditorDialogs.ResumeChildLayouts(this); ResumeLayout(true);
        }

        private static decimal ClampDecimal(int x, decimal min, decimal max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }

        private static Control InlineField(Control label,Control value)
        {
            FlowLayoutPanel group=new FlowLayoutPanel { AutoSize=true, AutoSizeMode=AutoSizeMode.GrowAndShrink, WrapContents=false, Margin=new Padding(0,0,12,0) }; group.SuspendLayout();
            label.Anchor=value.Anchor=AnchorStyles.Left;
            group.Controls.Add(label); group.Controls.Add(value);
            return group;
        }

        private static Label MakeInlineLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Margin = new Padding(0, 8, 5, 0);
            return label;
        }

        private static NumericUpDown MakeNumberBox(int min, int max, int value, int increment)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.Value = value;
            box.Increment = increment;
            box.Width = 72;
            box.Margin = new Padding(0, 4, 12, 6);
            return box;
        }

        private void UpdateInputMode()
        {
            if (selectedInput == null) return;
            bool gamepad = inputTypeBox.SelectedIndex == 1;
            bool mouse = inputTypeBox.SelectedIndex == 2;
            if (gamepad && selectedInput.Kind != InputKind.Gamepad)
            {
                selectedInputs = null;
                selectedInput = new InputSpec();
                selectedInput.Kind = InputKind.Gamepad;
                selectedInput.GamepadControl = GamepadControl.South;
                loadingControls = true;
                gamepadControlBox.SelectedIndex = (int)GamepadControl.South;
                stickAngleBox.Value = 0;
                stickStrengthBox.Value = 100;
                triggerValueBox.Value = 100;
                loadingControls = false;
            }
            else if (mouse && !MouseInputDiagram.IsMouse(selectedInput.Kind))
            {
                selectedInputs = null;
                selectedInput = new InputSpec { Kind = InputKind.MouseLeft, VirtualKey = 0 };
            }
            else if (!gamepad && !mouse && selectedInput.Kind != InputKind.Keyboard)
            {
                selectedInputs = null;
                selectedInput = new InputSpec();
                selectedInput.Kind = InputKind.Keyboard;
                selectedInput.VirtualKey = (int)Keys.A;
            }
            inputLabel.Visible = !gamepad;
            inputPanel.Visible = !gamepad;
            captureButton.Visible = true;
            gamepadLabel.Visible = gamepad;
            gamepadPanel.Visible = gamepad;
            if (gamepad)
            {
                int selectedIndex = Math.Max(0, Math.Min(gamepadControlBox.Items.Count - 1, (int)selectedInput.GamepadControl));
                if (gamepadControlBox.SelectedIndex != selectedIndex)
                {
                    loadingControls = true;
                    gamepadControlBox.SelectedIndex = selectedIndex;
                    loadingControls = false;
                }
                UpdateGamepadControl();
            }
            RefreshInputText();
            UpdateHoldEnabled();
        }

        private void UpdateGamepadControl()
        {
            if (selectedInput == null || inputTypeBox.SelectedIndex != 1 || gamepadControlBox.SelectedIndex < 0) return;
            selectedInput.Kind = InputKind.Gamepad;
            selectedInput.GamepadControl = (GamepadControl)gamepadControlBox.SelectedIndex;
            bool stick = selectedInput.GamepadControl == GamepadControl.LeftStick || selectedInput.GamepadControl == GamepadControl.RightStick;
            bool trigger = selectedInput.GamepadControl == GamepadControl.LeftTrigger || selectedInput.GamepadControl == GamepadControl.RightTrigger;
            stickAngleLabel.Visible = stick;
            stickAngleBox.Visible = stick;
            stickStrengthLabel.Visible = stick;
            stickStrengthBox.Visible = stick;
            triggerValueLabel.Visible = trigger;
            triggerValueBox.Visible = trigger;
            angleField.Visible=strengthField.Visible=stick;
            triggerField.Visible=trigger;
            RefreshInputText();
        }

        private void GamepadVectorChanged(object sender, EventArgs e)
        {
            if (loadingControls || selectedInput == null || selectedInput.Kind != InputKind.Gamepad) return;
            GamepadVector.ToCartesian((int)stickAngleBox.Value, (int)stickStrengthBox.Value,
                out selectedInput.GamepadX, out selectedInput.GamepadY);
            RefreshInputText();
        }

        private void TriggerValueChanged(object sender, EventArgs e)
        {
            if (loadingControls || selectedInput == null || selectedInput.Kind != InputKind.Gamepad) return;
            selectedInput.GamepadValue = (int)triggerValueBox.Value;
            RefreshInputText();
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            recordButton.Text = EditorDialogs.T("录入中 · Esc 取消", "Listening · Esc cancels");
            recordButton.Enabled = false;
            ownerMain.BeginStepInputCapture(delegate(InputSpec input)
            {
                if (IsDisposed) return;
                if (input != null)
                {
                    selectedInputs = null;
                    selectedInput = input.Clone();
                    loadingControls = true;
                    inputTypeBox.SelectedIndex = DeviceIndex(selectedInput);
                    loadingControls = false;
                    UpdateInputMode();
                    RefreshInputText();
                }
                recordButton.Text = EditorDialogs.T("实际按键录入…", "Press a key…");
                recordButton.Enabled = true;
                UpdateHoldEnabled();
            });
        }

        private void RefreshInputText()
        {
            if (selectedInput == null) return;
            if (selectedInputs != null && selectedInputs.Count > 1)
            {
                List<string> names = new List<string>();
                foreach (InputSpec input in selectedInputs) names.Add(VirtualKeyboardDialog.FormatKeyboardInput(input));
                inputText.Text = string.Join(" + ", names.ToArray());
            }
            else inputText.Text = VirtualKeyboardDialog.FormatKeyboardInput(selectedInput);
            if (gamepadPreview != null)
            {
                gamepadPreview.SetState(selectedInput.Kind == InputKind.Gamepad,
                    selectedInput.GamepadControl, (int)stickAngleBox.Value,
                    (int)stickStrengthBox.Value, (int)triggerValueBox.Value);
            }
            if (keyPreview != null)
            {
                bool gamepad = selectedInput.Kind == InputKind.Gamepad;
                bool mouse = MouseInputDiagram.IsMouse(selectedInput.Kind);
                keyPreview.Visible = !gamepad && !mouse;
                gamepadPreview.Visible = gamepad;
                mousePreview.Visible = mouse;
                mousePreview.SetSelection(mouse ? (InputKind?)selectedInput.Kind : null);
                keyPreview.Text = inputText.Text;
                keyPreview.Invalidate();
            }
            UpdateStepPreview();
        }

        private void UpdateHoldEnabled()
        {
            bool press = actionBox.SelectedIndex == 0;
            bool holdable = selectedInput == null || InputSender.IsHoldable(selectedInput);
            holdBox.Enabled = press && holdable;
            holdLabel.Enabled = press && holdable;
            UpdateStepPreview();
        }

        private void SelectVirtualKeyboardInput(object sender, EventArgs e)
        {
            if (ownerMain != null) ownerMain.CancelCapture();
            IList<InputSpec> initial = selectedInputs;
            if (initial == null && selectedInput != null && selectedInput.Kind != InputKind.Gamepad)
                initial = new List<InputSpec>(new InputSpec[] { selectedInput });
            using (VirtualKeyboardDialog dialog = new VirtualKeyboardDialog(false, initial))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                selectedInputs = dialog.SelectedInputs;
                selectedInput = selectedInputs[0].Clone();
                loadingControls = true;
                inputTypeBox.SelectedIndex = DeviceIndex(selectedInput);
                loadingControls = false;
                UpdateInputMode();
                RefreshInputText();
                UpdateHoldEnabled();
            }
        }

        private void UpdateDelayControls()
        {
            bool random = randomDelayBox.Checked;
            fixedDelayLabel.Enabled = !random;
            delayBox.Enabled = !random;
            randomDelayLabel.Enabled = random;
            randomMinBox.Enabled = random;
            randomMaxBox.Enabled = random;
            UpdateStepPreview();
        }

        private static int DeviceIndex(InputSpec input)
        { return input.Kind == InputKind.Gamepad ? 1 : MouseInputDiagram.IsMouse(input.Kind) ? 2 : 0; }

        private void UpdateStepPreview()
        {
            if (loadingControls || stepPreviewText == null || selectedInput == null) return;
            bool validRange = !randomDelayBox.Checked || randomMinBox.Value <= randomMaxBox.Value;
            bool validAction = !HasWheelInput() || actionBox.SelectedIndex == 0;
            if (confirmButton != null) confirmButton.Enabled = validRange && validAction;
            if (!validRange || !validAction)
            {
                stepPreviewText.ForeColor = Color.DarkOrange;
                stepPreviewText.Text = !validRange
                    ? EditorDialogs.T("请调整随机间隔：最小值不能大于最大值。", "Adjust the random wait: minimum must not exceed maximum.")
                    : EditorDialogs.T("滚轮不能持续按住或松开，请把操作改为“按一下”。", "The wheel cannot be held or released. Choose Tap instead.");
                return;
            }
            stepPreviewText.ForeColor = EditorTheme.Accent;
            string input = selectedInput.Kind == InputKind.Gamepad ? InputNames.FormatInput(selectedInput) : inputText.Text;
            bool english = Localizer.IsEnglish;
            string action = actionBox.SelectedIndex == 0
                ? (english ? "Press " + input + (holdBox.Enabled ? " for " + holdBox.Value + " ms, then release." : ".") : "按一下 " + input + (holdBox.Enabled ? "，按住 " + holdBox.Value + " 毫秒后松开。" : "。"))
                : actionBox.SelectedIndex == 1
                    ? (english ? "Hold " + input + " until a release step or the macro stops." : "持续按住 " + input + "，直到松开步骤或宏停止。")
                    : (english ? "Release " + input + "." : "松开 " + input + "。");
            string wait = randomDelayBox.Checked ? randomMinBox.Value + "–" + randomMaxBox.Value : delayBox.Value.ToString();
            stepPreviewText.Text = (english ? "This step\r\n" : "这个步骤会\r\n") + action + (english ? " Then wait " + wait + " ms before the next step." : " 再等待 " + wait + " 毫秒，进入下一步。") +
                (english ? "\r\n1000 ms = 1 second" : "\r\n1000 毫秒 = 1 秒");
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            if (selectedInput == null)
            {
                LocalizedMessageBox.Show(this, "请先选择一个按键、鼠标或手柄输入。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (HasWheelInput() && actionBox.SelectedIndex != 0)
            {
                LocalizedMessageBox.Show(this, "滚轮只支持“按一下”动作，因为滚轮没有持续按下/松开的状态。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (randomDelayBox.Checked && randomMinBox.Value > randomMaxBox.Value)
            {
                LocalizedMessageBox.Show(this, "随机间隔的最小值不能大于最大值。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Editing a virtual-controller step is configuration only. Do not create a ViGEm
            // device merely because the user saved an editable step; runtime connection is lazy.
            MacroStep s = new MacroStep();
            s.Action = actionBox.SelectedIndex == 0 ? MacroAction.Press : (actionBox.SelectedIndex == 1 ? MacroAction.Down : MacroAction.Up);
            s.Kind = selectedInput.Kind;
            s.VirtualKey = selectedInput.VirtualKey;
            s.ScanCode = selectedInput.ScanCode;
            s.Extended = selectedInput.Extended;
            s.GamepadControl = selectedInput.GamepadControl;
            s.GamepadX = selectedInput.GamepadX;
            s.GamepadY = selectedInput.GamepadY;
            s.GamepadValue = selectedInput.GamepadValue;
            s.HoldMs = (int)holdBox.Value;
            s.DelayMs = (int)delayBox.Value;
            s.RandomDelay = randomDelayBox.Checked;
            s.RandomDelayMinMs = (int)randomMinBox.Value;
            s.RandomDelayMaxMs = (int)randomMaxBox.Value;
            ResultStep = s;
            ResultSteps = selectedInputs != null && selectedInputs.Count > 1
                ? VirtualKeyboardDialog.BuildChordSteps(selectedInputs, s)
                : new List<MacroStep>(new MacroStep[] { s });
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool HasWheelInput()
        {
            if (selectedInputs != null) foreach (InputSpec input in selectedInputs)
                if (input.Kind == InputKind.WheelUp || input.Kind == InputKind.WheelDown) return true;
            return selectedInput != null && (selectedInput.Kind == InputKind.WheelUp || selectedInput.Kind == InputKind.WheelDown);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (ownerMain != null) ownerMain.CancelCapture();
            base.OnFormClosed(e);
        }
    }
}
