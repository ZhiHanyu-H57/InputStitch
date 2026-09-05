using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InputStitch
{
    // This editor changes an input definition only. It never injects a key into Windows.
    internal sealed class VirtualKeyboardDialog : Form
    {
        private readonly bool triggerMode;
        private readonly List<InputSpec> selected = new List<InputSpec>();
        private readonly List<Button> keyButtons = new List<Button>();
        private readonly Label selectionLabel;
        private readonly RadioButton singleButton;
        private readonly RadioButton multipleButton;
        private readonly KeyboardSurface keyboard;
        public List<InputSpec> SelectedInputs { get; private set; }
        public TriggerSpec SelectedTrigger { get; private set; }

        internal static string TextFor(string zh, string en) { return Localizer.IsEnglish ? en : zh; }

        internal static FlowLayoutPanel WithDropDown(Button primary, EventHandler physicalCapture, EventHandler virtualCapture)
        {
            FlowLayoutPanel group = new FlowLayoutPanel();
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            group.WrapContents = false;
            group.Margin = new Padding(0);
            primary.Margin = new Padding(0);
            group.Controls.Add(primary);
            Button arrow = new Button();
            arrow.Text = "▾";
            arrow.AccessibleName = TextFor("更多输入方式", "More input methods");
            arrow.MinimumSize = new Size(30, primary.MinimumSize.Height);
            arrow.Size = new Size(30, primary.Height);
            arrow.Margin = new Padding(0);
            arrow.Enabled = primary.Enabled;
            group.Controls.Add(arrow);
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Opening += delegate
            {
                menu.Items.Clear();
                menu.Items.Add(TextFor("捕获实体键盘 / 鼠标", "Capture physical keyboard / mouse"), null, physicalCapture);
                menu.Items.Add(TextFor("通过虚拟键盘选择…", "Choose from virtual keyboard…"), null, virtualCapture);
            };
            arrow.Click += delegate { menu.Show(arrow, new Point(0, arrow.Height)); };
            primary.EnabledChanged += delegate { arrow.Enabled = primary.Enabled; };
            primary.SizeChanged += delegate { arrow.Height = primary.Height; };
            group.Disposed += delegate { menu.Dispose(); };
            return group;
        }

        public VirtualKeyboardDialog(bool forTrigger, IList<InputSpec> initial)
        {
            triggerMode = forTrigger;
            Text = TextFor("虚拟键盘 · 选择按键", "Virtual keyboard · Select keys");
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            BackColor = Color.FromArgb(245, 247, 251);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MinimumSize = new Size(800, 490);
            ClientSize = new Size(1110, 480);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label help = new Label();
            help.AutoSize = true;
            help.Dock = DockStyle.Fill;
            help.Margin = new Padding(0, 0, 0, 10);
            help.Text = TextFor("点击按键选中，再次点击取消。这里只设置按键，不会向其他程序发送输入。",
                "Click a key to select it; click again to clear it. Selecting keys does not send input to other apps.");
            root.Controls.Add(help, 0, 0);

            FlowLayoutPanel modeRow = new FlowLayoutPanel();
            modeRow.AutoSize = true;
            modeRow.Dock = DockStyle.Fill;
            modeRow.Margin = new Padding(0, 0, 0, 10);
            singleButton = new RadioButton();
            singleButton.AutoSize = true;
            singleButton.Text = TextFor("记录单个按键", "Single key");
            singleButton.Margin = new Padding(0, 3, 24, 3);
            multipleButton = new RadioButton();
            multipleButton.AutoSize = true;
            multipleButton.Text = TextFor("记录多个按键（组合键）", "Multiple keys (chord)");
            modeRow.Controls.Add(singleButton);
            modeRow.Controls.Add(multipleButton);
            Button clear = new Button();
            clear.Text = TextFor("清空选择", "Clear selection");
            clear.AutoSize = true;
            clear.Margin = new Padding(24, 0, 0, 0);
            clear.Click += delegate { selected.Clear(); RefreshSelection(); };
            modeRow.Controls.Add(clear);
            root.Controls.Add(modeRow, 0, 1);

            Panel viewport = new Panel();
            viewport.Dock = DockStyle.Fill;
            viewport.AutoScroll = true;
            viewport.Margin = new Padding(0);
            keyboard = new KeyboardSurface();
            keyboard.Dock = DockStyle.None;
            keyboard.Height = 300;
            keyboard.MinimumSize = new Size(1058, 285);
            viewport.Controls.Add(keyboard);
            viewport.SizeChanged += delegate
            {
                keyboard.Size = new Size(Math.Max(keyboard.MinimumSize.Width, viewport.ClientSize.Width),
                    keyboard.MinimumSize.Height * 300 / 285);
                viewport.AutoScrollMinSize = keyboard.Size;
            };
            root.Controls.Add(viewport, 0, 2);
            BuildKeyboard();

            selectionLabel = new Label();
            selectionLabel.AutoSize = true;
            selectionLabel.Dock = DockStyle.Fill;
            selectionLabel.MinimumSize = new Size(0, 44);
            selectionLabel.Margin = new Padding(0, 10, 0, 5);
            root.Controls.Add(selectionLabel, 0, 3);

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.AutoSize = true;
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.RightToLeft;
            Button ok = new Button();
            ok.Text = TextFor("确定", "OK");
            ok.AutoSize = true;
            ok.MinimumSize = new Size(96, 34);
            ok.Click += ConfirmSelection;
            Button cancel = new Button();
            cancel.Text = TextFor("取消（退出）", "Cancel / Close");
            cancel.AutoSize = true;
            cancel.MinimumSize = new Size(120, 34);
            cancel.DialogResult = DialogResult.Cancel;
            footer.Controls.Add(ok);
            footer.Controls.Add(cancel);
            root.Controls.Add(footer, 0, 4);
            AcceptButton = ok;
            CancelButton = cancel;

            if (initial != null)
                foreach (InputSpec input in initial)
                    if (input != null && input.Kind == InputKind.Keyboard && !ContainsKey(input)) selected.Add(input.Clone());
            multipleButton.Checked = selected.Count > 1;
            singleButton.Checked = !multipleButton.Checked;
            singleButton.CheckedChanged += delegate
            {
                if (singleButton.Checked && selected.Count > 1) selected.RemoveRange(1, selected.Count - 1);
                RefreshSelection();
            };
            multipleButton.CheckedChanged += delegate { RefreshSelection(); };
            RefreshSelection();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Rectangle area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        }

        private bool ContainsKey(InputSpec input)
        {
            foreach (InputSpec key in selected)
                if (key.VirtualKey == input.VirtualKey && key.Extended == input.Extended) return true;
            return false;
        }

        private void AddKey(string label, Keys key, float x, float y, float width, float height, bool extended)
        {
            InputSpec input = new InputSpec();
            input.VirtualKey = (int)key;
            input.Extended = extended;
            Button button = new Button();
            button.Text = label;
            button.AccessibleName = label;
            button.Tag = input;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(208, 216, 227);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(34, 46, 62);
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Segoe UI", 8F);
            button.Click += delegate
            {
                int index = selected.FindIndex(delegate(InputSpec item) { return item.VirtualKey == input.VirtualKey && item.Extended == input.Extended; });
                if (index >= 0) selected.RemoveAt(index);
                else
                {
                    if (singleButton.Checked) selected.Clear();
                    selected.Add(input.Clone());
                }
                RefreshSelection();
            };
            keyButtons.Add(button);
            keyboard.AddKey(button, new RectangleF(x, y, width, height));
        }

        private void Key(string label, Keys key, float x, float y, float width)
        {
            bool ext = key == Keys.Insert || key == Keys.Delete || key == Keys.Home || key == Keys.End ||
                key == Keys.PageUp || key == Keys.PageDown || key == Keys.Left || key == Keys.Right ||
                key == Keys.Up || key == Keys.Down || key == Keys.RControlKey || key == Keys.RMenu ||
                key == Keys.LWin || key == Keys.RWin || key == Keys.Apps || key == Keys.Divide || key == Keys.PrintScreen || key == Keys.NumLock;
            AddKey(label, key, x, y, width, 1F, ext);
        }

        private void BuildKeyboard()
        {
            Key("Esc", Keys.Escape, 0, 0, 1);
            for (int i = 0; i < 12; i++) Key("F" + (i + 1), (Keys)((int)Keys.F1 + i), 2 + i + (i / 4) * .5F, 0, 1);
            Key("PrtSc", Keys.PrintScreen, 15.5F, 0, 1);
            Key("ScrLk", Keys.Scroll, 16.5F, 0, 1);
            Key("Pause", Keys.Pause, 17.5F, 0, 1);

            Key("`  ~", Keys.Oemtilde, 0, 1.3F, 1);
            for (int i = 1; i <= 10; i++) Key((i % 10).ToString(), (Keys)((int)Keys.D0 + i % 10), i, 1.3F, 1);
            Key("-  _", Keys.OemMinus, 11, 1.3F, 1);
            Key("=  +", Keys.Oemplus, 12, 1.3F, 1);
            Key("Backspace", Keys.Back, 13, 1.3F, 2);
            Key("Ins", Keys.Insert, 15.5F, 1.3F, 1);
            Key("Home", Keys.Home, 16.5F, 1.3F, 1);
            Key("PgUp", Keys.PageUp, 17.5F, 1.3F, 1);
            Key("Num\nLock", Keys.NumLock, 19, 1.3F, 1);
            Key("/", Keys.Divide, 20, 1.3F, 1);
            Key("*", Keys.Multiply, 21, 1.3F, 1);
            Key("-", Keys.Subtract, 22, 1.3F, 1);

            Key("Tab", Keys.Tab, 0, 2.3F, 1.5F);
            string top = "QWERTYUIOP";
            for (int i = 0; i < top.Length; i++) Key(top[i].ToString(), (Keys)top[i], 1.5F + i, 2.3F, 1);
            Key("[  {", Keys.OemOpenBrackets, 11.5F, 2.3F, 1);
            Key("]  }", Keys.OemCloseBrackets, 12.5F, 2.3F, 1);
            Key("\\  |", Keys.OemPipe, 13.5F, 2.3F, 1.5F);
            Key("Del", Keys.Delete, 15.5F, 2.3F, 1);
            Key("End", Keys.End, 16.5F, 2.3F, 1);
            Key("PgDn", Keys.PageDown, 17.5F, 2.3F, 1);
            for (int i = 0; i < 3; i++) Key((7 + i).ToString(), (Keys)((int)Keys.NumPad7 + i), 19 + i, 2.3F, 1);
            AddKey("+", Keys.Add, 22, 2.3F, 1, 2, false);

            Key("Caps Lock", Keys.Capital, 0, 3.3F, 1.75F);
            string middle = "ASDFGHJKL";
            for (int i = 0; i < middle.Length; i++) Key(middle[i].ToString(), (Keys)middle[i], 1.75F + i, 3.3F, 1);
            Key(";  :", Keys.OemSemicolon, 10.75F, 3.3F, 1);
            Key("'  \"", Keys.OemQuotes, 11.75F, 3.3F, 1);
            Key("Enter", Keys.Enter, 12.75F, 3.3F, 2.25F);
            for (int i = 0; i < 3; i++) Key((4 + i).ToString(), (Keys)((int)Keys.NumPad4 + i), 19 + i, 3.3F, 1);

            Key("Shift L", Keys.LShiftKey, 0, 4.3F, 2.25F);
            string bottom = "ZXCVBNM";
            for (int i = 0; i < bottom.Length; i++) Key(bottom[i].ToString(), (Keys)bottom[i], 2.25F + i, 4.3F, 1);
            Key(",  <", Keys.Oemcomma, 9.25F, 4.3F, 1);
            Key(".  >", Keys.OemPeriod, 10.25F, 4.3F, 1);
            Key("/  ?", Keys.OemQuestion, 11.25F, 4.3F, 1);
            Key("Shift R", Keys.RShiftKey, 12.25F, 4.3F, 2.75F);
            Key("↑", Keys.Up, 16.5F, 4.3F, 1);
            for (int i = 0; i < 3; i++) Key((1 + i).ToString(), (Keys)((int)Keys.NumPad1 + i), 19 + i, 4.3F, 1);
            AddKey("Enter", Keys.Enter, 22, 4.3F, 1, 2, true);

            Key("Ctrl L", Keys.LControlKey, 0, 5.3F, 1.25F);
            Key("Win L", Keys.LWin, 1.25F, 5.3F, 1.25F);
            Key("Alt L", Keys.LMenu, 2.5F, 5.3F, 1.25F);
            Key("Space", Keys.Space, 3.75F, 5.3F, 6.25F);
            Key("Alt R", Keys.RMenu, 10, 5.3F, 1.25F);
            Key("Win R", Keys.RWin, 11.25F, 5.3F, 1.25F);
            Key("Menu", Keys.Apps, 12.5F, 5.3F, 1.25F);
            Key("Ctrl R", Keys.RControlKey, 13.75F, 5.3F, 1.25F);
            Key("←", Keys.Left, 15.5F, 5.3F, 1);
            Key("↓", Keys.Down, 16.5F, 5.3F, 1);
            Key("→", Keys.Right, 17.5F, 5.3F, 1);
            Key("0", Keys.NumPad0, 19, 5.3F, 2);
            Key(".", Keys.Decimal, 21, 5.3F, 1);
        }

        private void RefreshSelection()
        {
            foreach (Button key in keyButtons)
            {
                bool on = ContainsKey((InputSpec)key.Tag);
                key.BackColor = on ? Color.FromArgb(34, 101, 207) : Color.White;
                key.ForeColor = on ? Color.White : Color.FromArgb(34, 46, 62);
                key.AccessibleDescription = on ? TextFor("已选中", "Selected") : TextFor("未选中", "Not selected");
            }
            if (selectionLabel == null) return;
            List<string> names = new List<string>();
            foreach (InputSpec input in selected) names.Add(FormatKeyboardInput(input));
            string hint = triggerMode
                ? TextFor("组合触发键支持 Ctrl / Shift / Alt / Win 加一个主键；也可单独选择修饰键。", "Trigger chords support Ctrl / Shift / Alt / Win plus one main key. A modifier can also be used alone.")
                : TextFor("多个按键按组合键执行；确认后展开为可编辑的按下、按一下和松开步骤。", "Multiple keys form a chord, expanded into editable Down, Press and Up steps when confirmed.");
            selectionLabel.Text = TextFor("已选：", "Selected: ") + (names.Count == 0 ? TextFor("无", "None") : string.Join(" + ", names.ToArray())) + Environment.NewLine + hint;
        }

        public static string FormatKeyboardInput(InputSpec input)
        {
            if (input.Kind == InputKind.Keyboard && input.VirtualKey == (int)Keys.Enter && input.Extended) return "Num Enter";
            return InputNames.FormatInput(input);
        }

        public static bool TryCreateTrigger(IList<InputSpec> inputs, out TriggerSpec trigger, out string error)
        {
            trigger = null;
            error = null;
            if (inputs == null || inputs.Count == 0)
            {
                error = TextFor("请至少选择一个按键。", "Select at least one key.");
                return false;
            }
            InputSpec terminal = null;
            int ordinary = 0;
            foreach (InputSpec input in inputs)
            {
                if (!ModifierSafetyPolicy.IsModifierVirtualKey(input.VirtualKey)) { terminal = input; ordinary++; }
            }
            if (ordinary > 1)
            {
                error = TextFor("触发组合只能包含一个普通主键；其余按键请选择 Ctrl、Shift、Alt 或 Win。宏步骤可以使用多个普通按键。",
                    "A trigger chord can contain only one ordinary key plus Ctrl, Shift, Alt or Win. Macro steps can contain multiple ordinary keys.");
                return false;
            }
            if (terminal == null) terminal = inputs[inputs.Count - 1];
            trigger = new TriggerSpec();
            trigger.Kind = InputKind.Keyboard;
            trigger.VirtualKey = terminal.VirtualKey;
            trigger.Extended = terminal.Extended;
            trigger.MatchExtended = terminal.VirtualKey == (int)Keys.Enter;
            int terminalMask = ModifierSafetyPolicy.ModifierMaskForKey(terminal.VirtualKey);
            int modifiers = 0;
            foreach (InputSpec input in inputs)
            {
                if (object.ReferenceEquals(input, terminal)) continue;
                int mask = ModifierSafetyPolicy.ModifierMaskForKey(input.VirtualKey);
                if ((mask & (terminalMask | modifiers)) != 0)
                {
                    trigger = null;
                    error = TextFor("左右两侧的同类修饰键不能同时组成触发键，请保留其中一侧。", "Both sides of the same modifier cannot form a trigger chord. Select one side.");
                    return false;
                }
                modifiers |= mask;
            }
            trigger.Ctrl = (modifiers & ModifierSafetyPolicy.Ctrl) != 0;
            trigger.Shift = (modifiers & ModifierSafetyPolicy.Shift) != 0;
            trigger.Alt = (modifiers & ModifierSafetyPolicy.Alt) != 0;
            trigger.Win = (modifiers & ModifierSafetyPolicy.Win) != 0;
            return true;
        }

        public static List<InputSpec> InputsFromTrigger(TriggerSpec trigger)
        {
            List<InputSpec> inputs = new List<InputSpec>();
            if (trigger == null || trigger.Kind != InputKind.Keyboard) return inputs;
            if (trigger.Ctrl) inputs.Add(new InputSpec { VirtualKey = (int)Keys.LControlKey });
            if (trigger.Shift) inputs.Add(new InputSpec { VirtualKey = (int)Keys.LShiftKey });
            if (trigger.Alt) inputs.Add(new InputSpec { VirtualKey = (int)Keys.LMenu });
            if (trigger.Win) inputs.Add(new InputSpec { VirtualKey = (int)Keys.LWin, Extended = true });
            inputs.Add(new InputSpec { VirtualKey = trigger.VirtualKey, Extended = trigger.Extended });
            return inputs;
        }

        // Expand a chord into the existing execution model, keeping modifiers held around
        // ordinary keys. Delay/random delay applies once, after the whole chord.
        public static List<MacroStep> BuildChordSteps(IList<InputSpec> inputs, MacroStep template)
        {
            List<InputSpec> ordered = new List<InputSpec>();
            foreach (InputSpec input in inputs) if (ModifierSafetyPolicy.IsModifierVirtualKey(input.VirtualKey)) ordered.Add(input);
            foreach (InputSpec input in inputs) if (!ModifierSafetyPolicy.IsModifierVirtualKey(input.VirtualKey)) ordered.Add(input);
            List<MacroStep> steps = new List<MacroStep>();
            if (template.Action == MacroAction.Up) ordered.Reverse();
            for (int i = 0; i < ordered.Count; i++)
            {
                MacroAction action = template.Action == MacroAction.Press && i < ordered.Count - 1 ? MacroAction.Down : template.Action;
                steps.Add(MakeStep(ordered[i], template, action));
            }
            if (template.Action == MacroAction.Press)
                for (int i = ordered.Count - 2; i >= 0; i--) steps.Add(MakeStep(ordered[i], template, MacroAction.Up));
            if (steps.Count > 0)
            {
                MacroStep last = steps[steps.Count - 1];
                last.DelayMs = template.DelayMs;
                last.RandomDelay = template.RandomDelay;
                last.RandomDelayMinMs = template.RandomDelayMinMs;
                last.RandomDelayMaxMs = template.RandomDelayMaxMs;
            }
            return steps;
        }

        private static MacroStep MakeStep(InputSpec input, MacroStep template, MacroAction action)
        {
            MacroStep step = template.Clone();
            step.Kind = InputKind.Keyboard;
            step.VirtualKey = input.VirtualKey;
            step.ScanCode = input.ScanCode;
            step.Extended = input.Extended;
            step.Action = action;
            step.DelayMs = 0;
            step.RandomDelay = false;
            return step;
        }

        private void ConfirmSelection(object sender, EventArgs e)
        {
            if (selected.Count == 0)
            {
                MessageBox.Show(this, TextFor("请至少选择一个按键。", "Select at least one key."), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (triggerMode)
            {
                TriggerSpec trigger;
                string error;
                if (!TryCreateTrigger(selected, out trigger, out error))
                {
                    MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                SelectedTrigger = trigger;
            }
            SelectedInputs = new List<InputSpec>();
            foreach (InputSpec input in selected) SelectedInputs.Add(input.Clone());
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class KeyboardSurface : Panel
        {
            private readonly Dictionary<Control, RectangleF> positions = new Dictionary<Control, RectangleF>();
            public void AddKey(Control key, RectangleF position) { positions.Add(key, position); Controls.Add(key); PerformLayout(); }
            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                float unitX = ClientSize.Width / 23F;
                float unitY = ClientSize.Height / 6.3F;
                foreach (KeyValuePair<Control, RectangleF> item in positions)
                {
                    RectangleF p = item.Value;
                    item.Key.Bounds = new Rectangle((int)(p.X * unitX) + 2, (int)(p.Y * unitY) + 2,
                        Math.Max(1, (int)(p.Width * unitX) - 4), Math.Max(1, (int)(p.Height * unitY) - 4));
                }
            }
        }
    }
}
