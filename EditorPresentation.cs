using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputStitch
{
    // Presentation adapter only. Existing XML and execution policies remain authoritative.
    internal static class TriggerEditor
    {
        internal static string[] Names
        {
            get { return Localizer.IsEnglish
                ? new[] { "Press to start / again to stop", "Hold to run / release to stop", "On press", "On release", "Short press", "Long press", "Double press" }
                : new[] { "按一次启动，再按停止", "按住运行，松开停止", "按下时执行", "松开时执行", "短按后执行", "长按后执行", "双击后执行" }; }
        }

        internal static int Index(MacroDefinition macro)
        {
            if (macro == null) return 0;
            string mode = macro.Activator == null ? ActivatorModes.Legacy : ActivatorModes.Normalize(macro.Activator.Mode);
            if (mode == ActivatorModes.WhileHeld) return 1;
            if (mode == ActivatorModes.Press) return 2;
            if (mode == ActivatorModes.Release) return 3;
            if (mode == ActivatorModes.ShortPress) return 4;
            if (mode == ActivatorModes.LongPress) return 5;
            if (mode == ActivatorModes.DoublePress) return 6;
            return macro.RunMode == TriggerRunMode.Hold ? 1 : 0;
        }

        internal static void Select(MacroDefinition macro, int index)
        {
            if (macro == null || index < 0 || index >= Names.Length || Index(macro) == index) return;
            if (macro.Activator == null) macro.Activator = new ActivatorConfig();
            string[] modes = { ActivatorModes.Legacy, ActivatorModes.WhileHeld, ActivatorModes.Press,
                ActivatorModes.Release, ActivatorModes.ShortPress, ActivatorModes.LongPress, ActivatorModes.DoublePress };
            macro.Activator.Mode = modes[index];
            macro.RunMode = index == 1 ? TriggerRunMode.Hold : TriggerRunMode.Toggle;
        }

        internal static string Hint(MacroDefinition macro)
        {
            int index = Index(macro);
            if (index == 0) return Localizer.IsEnglish ? "Press your shortcut to start. Press it again to stop early." : "按一下触发键开始执行；执行中再按一下即可停止。";
            if (index == 1) return Localizer.IsEnglish ? "Runs while you hold the shortcut. Releasing it stops the macro." : "按住触发键时运行，松开立即停止。";
            return Localizer.IsEnglish ? "Runs at the selected moment. Use Stop to end a repeating macro." : "在所选时机执行；持续循环时，点击“停止”结束。";
        }
    }

    internal static class EditorTheme
    {
        internal static readonly Color Canvas = Color.FromArgb(241, 244, 248);
        internal static readonly Color Ink = Color.FromArgb(30, 43, 62);
        internal static readonly Color Muted = Color.FromArgb(99, 113, 133);
        internal static readonly Color Accent = Color.FromArgb(40, 92, 176);
        internal static readonly Color Line = Color.FromArgb(218, 225, 234);
        internal static readonly Color Selection = Color.FromArgb(226, 237, 253);

        internal static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static void Apply(Control root)
        {
            if (root is Form) { root.BackColor = Canvas; root.ForeColor = Ink; }
            foreach (Control child in root.Controls)
            {
                Button button = child as Button;
                if (button != null)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Line;
                    button.FlatAppearance.MouseOverBackColor = Selection;
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(209, 225, 248);
                    button.BackColor = Color.White;
                    button.ForeColor = Ink;
                    button.UseVisualStyleBackColor = false;
                    button.Padding = new Padding(8, 2, 8, 2);
                    if (button.MinimumSize.Height < 30) button.MinimumSize = new Size(button.MinimumSize.Width, 30);
                }
                DataGridView grid = child as DataGridView;
                if (grid != null)
                {
                    grid.EnableHeadersVisualStyles = false;
                    grid.BorderStyle = BorderStyle.None;
                    grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                    grid.GridColor = Line;
                    grid.BackgroundColor = Color.White;
                    grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = Canvas;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
                    grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Canvas;
                    grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 7, 6, 7);
                    grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
                    grid.DefaultCellStyle.ForeColor = Ink;
                    grid.DefaultCellStyle.SelectionBackColor = Selection;
                    grid.DefaultCellStyle.SelectionForeColor = Ink;
                    grid.RowTemplate.Height = 42;
                    grid.DefaultCellStyle.SelectionForeColor = Accent;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 254);
                }
                ComboBox choice = child as ComboBox;
                if (choice != null && choice.DropDownStyle == ComboBoxStyle.DropDownList && choice.DrawMode == DrawMode.Normal)
                {
                    choice.DrawMode = DrawMode.OwnerDrawFixed;
                    choice.ItemHeight = Math.Max(22, choice.Font.Height + 6);
                    choice.DrawItem += DrawChoice;
                }
                Apply(child);
            }
        }

        private static void DrawChoice(object sender, DrawItemEventArgs e)
        {
            ComboBox choice = (ComboBox)sender;
            e.DrawBackground();
            string text = e.Index >= 0 && e.Index < choice.Items.Count ? choice.Items[e.Index].ToString() : choice.Text;
            TextRenderer.DrawText(e.Graphics, text, e.Font, Rectangle.Inflate(e.Bounds, -3, 0),
                choice.Enabled ? Ink : SystemColors.GrayText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        internal static void Primary(Button button, bool stop)
        {
            button.BackColor = stop ? Color.FromArgb(174, 52, 60) : Accent;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = button.BackColor;
            button.FlatAppearance.MouseOverBackColor = stop ? Color.FromArgb(148, 42, 50) : Color.FromArgb(31, 75, 148);
            button.FlatAppearance.MouseDownBackColor = button.FlatAppearance.MouseOverBackColor;
        }
    }

    internal sealed class EditorSection : GroupBox
    {
        internal string Badge;
        internal EditorSection()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? EditorTheme.Canvas : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = EditorTheme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 10))
            using (Brush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(EditorTheme.Line))
            { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(border, shape); }
            int unit = Font.Height;
            int titleLeft = 18;
            if (!string.IsNullOrEmpty(Badge))
            {
                Rectangle badge = new Rectangle(16, 12, unit + 8, unit + 8);
                using (Brush fill = new SolidBrush(EditorTheme.Selection)) e.Graphics.FillEllipse(fill, badge);
                TextRenderer.DrawText(e.Graphics, Badge, Font, badge, EditorTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                titleLeft = badge.Right + 10;
            }
            using (Font title = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, title, new Rectangle(titleLeft, 12, Math.Max(0, Width - titleLeft - 16), unit + 8),
                    EditorTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    // Native Buttons retain keyboard navigation, default-button and accessibility behavior.
    internal class EditorButton : Button
    {
        private bool hovered;
        private bool pressed;
        internal EditorButton() { DoubleBuffered = true; FlatStyle = FlatStyle.Flat; }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? EditorTheme.Canvas : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = !Enabled ? Color.FromArgb(242, 244, 247) : pressed ? FlatAppearance.MouseDownBackColor : hovered ? FlatAppearance.MouseOverBackColor : BackColor;
            using (GraphicsPath shape = EditorTheme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (Brush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(Focused ? EditorTheme.Accent : FlatAppearance.BorderColor))
            { e.Graphics.FillPath(brush, shape); e.Graphics.DrawPath(pen, shape); }
            TextRenderer.DrawText(e.Graphics, Text, Font, Rectangle.Inflate(ClientRectangle, -5, -2),
                Enabled ? ForeColor : EditorTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
        }
    }

    internal sealed class EditorIntro : Control
    {
        internal string Description = "";
        internal EditorIntro() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Font heading = new Font(Font.FontFamily, Font.Size + 6, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, heading, new Rectangle(0, 2, Width, heading.Height + 8), EditorTheme.Ink, TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, Description, Font, new Rectangle(0, Font.Height + 22, Width, Height - Font.Height - 22), EditorTheme.Muted,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class EditorWordmark : Control
    {
        internal EditorWordmark() { DoubleBuffered = true; AccessibleName = "InputStitch"; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int mark = Math.Min(32, Font.Height + 12);
            using (Pen front = new Pen(EditorTheme.Accent, 3))
            using (Pen back = new Pen(EditorTheme.Ink, 3))
            using (GraphicsPath first = EditorTheme.Rounded(new Rectangle(2, 5, mark - 9, mark - 9), 5))
            using (GraphicsPath second = EditorTheme.Rounded(new Rectangle(10, 13, mark - 9, mark - 9), 5))
            { e.Graphics.DrawPath(back, first); e.Graphics.DrawPath(front, second); }
            using (Font brand = new Font(Font.FontFamily, 14F, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, "InputStitch", brand, new Rectangle(mark + 10, 5, Width - mark - 10, 34), EditorTheme.Ink,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class EditorTemplateCard : Button
    {
        internal string Description;
        internal string Symbol;
        private bool hovered;
        internal EditorTemplateCard() { DoubleBuffered = true; FlatStyle = FlatStyle.Flat; TextAlign = ContentAlignment.MiddleLeft; }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = EditorTheme.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), 10))
            using (Brush fill = new SolidBrush(hovered || Focused ? EditorTheme.Selection : Color.White))
            using (Pen border = new Pen(hovered || Focused ? EditorTheme.Accent : EditorTheme.Line))
            { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(border, shape); }
            int u = Font.Height;
            Rectangle tile = new Rectangle(18, 18, u + 22, u + 22);
            using (Brush fill = new SolidBrush(EditorTheme.Selection)) e.Graphics.FillEllipse(fill, tile);
            using (Font icon = new Font(Font.FontFamily, Font.Size + 4, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Symbol, icon, tile, EditorTheme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (Font title = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, title, new Rectangle(tile.Right + 12, 20, Width - tile.Right - 28, u + 10), EditorTheme.Ink,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, Description, Font, new Rectangle(18, tile.Bottom + 12, Width - 36, Height - tile.Bottom - 22), EditorTheme.Muted,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -6, -6));
        }
    }

    internal sealed class NewMacroDialog : Form
    {
        internal QuickTemplate? Template { get; private set; }
        internal NewMacroDialog()
        {
            Text = Localizer.IsEnglish ? "Create a macro" : "新建宏";
            Font = new Font("Microsoft YaHei UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(800, 570);
            MinimumSize = new Size(760, 580);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            Controls.Add(root);
            root.Controls.Add(new EditorIntro { Dock = DockStyle.Fill, Text = Localizer.IsEnglish ? "What would you like to do?" : "想让按键帮你做什么？",
                Description = Localizer.IsEnglish ? "Choose a starting point. You can change every setting after creating it." : "选择一种常用操作作为起点，创建后随时可以修改。" }, 0, 0);
            TableLayoutPanel cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0) };
            for (int i = 0; i < 2; i++) { cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); }
            string[] names = Localizer.IsEnglish ? new[] { "Repeat an action", "Run a sequence", "Hold a controller input", "Start from scratch" }
                : new[] { "重复按键", "按顺序执行", "按住控制手柄", "从空白开始" };
            string[] descriptions = Localizer.IsEnglish ? new[] { "Press a key a set number of times, for example click 10 times.", "Combine keys and waits, for example press A, wait, then press B.", "Hold a key to move a virtual stick or hold a controller button.", "Add each step yourself. Suitable for custom keyboard, mouse or controller actions." }
                : new[] { "把同一操作重复指定次数，例如连续点击 10 次。", "组合多个按键和等待，例如按 A，稍等，再按 B。", "按住一个键来推动虚拟摇杆，或按住手柄按钮。", "自己添加每个步骤，适合自定义键盘、鼠标或手柄操作。" };
            QuickTemplate?[] templates = { QuickTemplate.Repeat, QuickTemplate.Sequence, QuickTemplate.HeldMapping, null };
            string[] symbols = { "↻", "→", "＋", "…" };
            for (int i = 0; i < names.Length; i++)
            {
                QuickTemplate? selected = templates[i];
                EditorTemplateCard card = new EditorTemplateCard { Name = "template" + i, Dock = DockStyle.Fill, Text = names[i], Description = descriptions[i], Symbol = symbols[i],
                    AccessibleName = names[i], AccessibleDescription = descriptions[i], Margin = new Padding(i % 2 == 0 ? 0 : 7, 0, i % 2 == 0 ? 7 : 0, 12) };
                card.Click += delegate { Template = selected; DialogResult = DialogResult.OK; Close(); };
                cards.Controls.Add(card, i % 2, i / 2);
            }
            root.Controls.Add(cards, 0, 1);
            Button cancel = new EditorButton { AutoSize = true, Text = Localizer.IsEnglish ? "Cancel" : "取消", MinimumSize = new Size(96, 36), Anchor = AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            root.Controls.Add(cancel, 0, 2);
            CancelButton = cancel;
            EditorTheme.Apply(this);
        }
    }

    internal sealed class EditorKeyPreview : Control
    {
        internal EditorKeyPreview() { DoubleBuffered = true; BackColor = Color.White; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int unit = Font.Height;
            Rectangle key = new Rectangle(22, Math.Max(42, (Height - 90) / 2), Math.Max(80, Width - 44), 80);
            using (GraphicsPath shape = EditorTheme.Rounded(key, 12))
            using (Brush fill = new SolidBrush(EditorTheme.Selection))
            using (Pen outline = new Pen(EditorTheme.Accent, 2))
            { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(outline, shape); }
            using (Font keyFont = new Font(Font.FontFamily, Font.Size + 8, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, keyFont, Rectangle.Inflate(key, -8, -4), EditorTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, Localizer.IsEnglish ? "Your selected input" : "这个步骤操作的按键", Font,
                new Rectangle(4, key.Bottom + 16, Width - 8, unit * 2), EditorTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }

    // WinForms omits DropDownList selection text during WM_PRINT on some Windows
    // themes. Print the real selected value so off-screen layout captures match it.
    internal sealed class EditorChoice : ComboBox
    {
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if ((message.Msg == 0x0317 || message.Msg == 0x0318) && message.WParam != IntPtr.Zero &&
                DropDownStyle == ComboBoxStyle.DropDownList && SelectedIndex >= 0)
            {
                using (Graphics graphics = Graphics.FromHdc(message.WParam))
                {
                    Rectangle content = new Rectangle(4, 3, Math.Max(0, Width - SystemInformation.VerticalScrollBarWidth - 8), Height - 6);
                    using (Brush background = new SolidBrush(Enabled ? SystemColors.Window : SystemColors.Control)) graphics.FillRectangle(background, content);
                    TextRenderer.DrawText(graphics, GetItemText(SelectedItem), Font,
                        content,
                        Enabled ? EditorTheme.Ink : SystemColors.GrayText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }
    }

    public partial class MainForm
    {
        private TableLayoutPanel optionalSettingsPanel;
        private bool optionalSettingsExpanded;
        private Button optionalSettingsButton;
        private Label triggerBehaviorLabel;
        private Label macroCountLabel;
        private Label stepsHintLabel;
        private Panel emptyStepsPanel;
        private Button addStepButton;
        private Button editStepButton;
        private Button newMacroButton;

        private void ShowNewMacroChooser(object sender, EventArgs e)
        {
            CancelCapture();
            bool previousPause = pauseHotkeys;
            pauseHotkeys = true;
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (NewMacroDialog dialog = new NewMacroDialog())
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    if (dialog.Template.HasValue) QuickCreateMacro(dialog.Template.Value);
                    else { AddMacro_Click(this, EventArgs.Empty); nameBox.Focus(); nameBox.SelectAll(); }
                }
            }
            finally { uiSafetyModalDepth--; pauseHotkeys = previousPause; UpdateUiSafetyPauseState(); }
        }

        private void ShowLibraryMenu(object sender, EventArgs e)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(Localizer.T("导入宏"), null, ImportConfigButton_Click).ToolTipText = Localizer.IsEnglish ? "Add macros to your list." : "追加到现有列表，不覆盖已有宏。";
            menu.Items.Add(Localizer.T("导出宏"), null, ExportConfigButton_Click).ToolTipText = Localizer.IsEnglish ? "Share selected macros." : "分享或备份选中的宏。";
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Localizer.T("载入方案"), null, LoadProfileButton_Click).ToolTipText = Localizer.IsEnglish ? "Replace the full setup; a backup is saved first." : "切换整套宏与设置，载入前自动备份。";
            menu.Items.Add(Localizer.T("保存方案"), null, SaveProfileButton_Click).ToolTipText = Localizer.IsEnglish ? "Back up all macros and settings." : "备份全部宏和程序设置。";
            menu.ShowItemToolTips = true;
            RegisterUiSafetyControl(menu, "导入与备份");
            menu.Disposed += delegate { uiSafetyControls.Remove(menu); };
            menu.Closed += delegate { DeferContextMenuDispose(this, menu); };
            Button button = (Button)sender;
            menu.Show(button, new Point(0, button.Height));
        }

        private void RefreshEditorGuidance()
        {
            if (macroCountLabel != null) macroCountLabel.Text = Localizer.IsEnglish ? "MY MACROS  ·  " + config.Macros.Count : "我的宏  ·  " + config.Macros.Count;
            MacroDefinition macro = SelectedMacro;
            int count = macro == null || macro.Steps == null ? 0 : macro.Steps.Count;
            if (stepsHintLabel != null) stepsHintLabel.Text = count == 0
                ? (Localizer.IsEnglish ? "Add a key or record your actions to get started." : "添加一个按键，或直接录制你的操作。")
                : (Localizer.IsEnglish ? count + " step(s), in order · Double-click a row to edit" : count + " 个步骤，按顺序执行 · 双击任意一行可编辑");
            if (emptyStepsPanel != null) emptyStepsPanel.Visible = macro != null && count == 0;
            if (grid != null) grid.Visible = count > 0;
            if (addStepButton != null) addStepButton.Enabled = macro != null && !recordingActive;
            if (editStepButton != null) editStepButton.Enabled = macro != null && count > 0 && !recordingActive;
            if (macroList != null) macroList.Invalidate();
        }

        private void DrawMacroItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= config.Macros.Count) return;
            MacroDefinition macro = config.Macros[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
            Rectangle card = Rectangle.Inflate(e.Bounds, -3, -4);
            using (GraphicsPath shape = EditorTheme.Rounded(card, 8))
            using (Brush fill = new SolidBrush(selected ? EditorTheme.Selection : Color.FromArgb(248, 250, 253))) e.Graphics.FillPath(fill, shape);
            using (Brush state = new SolidBrush(macro.Enabled ? EditorTheme.Accent : EditorTheme.Muted))
                e.Graphics.FillEllipse(state, card.Left + 12, card.Top + 15, 6, 6);
            string name = IsMacroTriggerConflicted(macro) && macro.Enabled ? "⚠ " + macro.Name : macro.Name;
            using (Font title = new Font(e.Font, selected ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(e.Graphics, name, title, new Rectangle(e.Bounds.Left + 27, e.Bounds.Top + 10, e.Bounds.Width - 43, e.Font.Height + 3), EditorTheme.Ink,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            string detail = InputNames.FormatTrigger(macro.Trigger) + "  ·  " + TriggerEditor.Names[TriggerEditor.Index(macro)];
            TextRenderer.DrawText(e.Graphics, detail, e.Font, new Rectangle(e.Bounds.Left + 14, e.Bounds.Top + e.Font.Height + 17, e.Bounds.Width - 28, e.Font.Height + 4), EditorTheme.Muted,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        private void ToggleOptionalSettings(object sender, EventArgs e)
        {
            bool show = !optionalSettingsExpanded;
            optionalSettingsExpanded = show;
            optionalSettingsPanel.Visible = show;
            UpdateTargetSectionVisibility();
            RefreshOptionalSettingsText(show);
        }

        private void RefreshOptionalSettingsText(bool expanded)
        {
            if (optionalSettingsButton != null) optionalSettingsButton.Text = Localizer.IsEnglish
                ? (expanded ? "▾ Fewer options" : "▸ More options: grouping & input")
                : (expanded ? "▾ 收起更多设置" : "▸ 更多设置：分组与按键传递");
        }
    }
}
