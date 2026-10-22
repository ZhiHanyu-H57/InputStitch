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
        internal const float BodyFontSize = 10.5F;
        internal const float KeyFontSize = 10F;
        internal static Font BodyFont() { return new Font("Microsoft YaHei UI", BodyFontSize); }
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
            if (root is Form)
            {
                root.BackColor = Canvas; root.ForeColor = Ink;
                if (!(root is MainForm)) EditorDialogs.PrepareWindow((Form)root);
            }
            foreach (Control child in root.Controls)
            {
                FlowLayoutPanel flow = child as FlowLayoutPanel;
                if (flow != null && flow.AutoSize) flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                EditorChoice compactChoice = child as EditorChoice;
                if (compactChoice != null) compactChoice.FitWidth();
                EditorIntro intro = child as EditorIntro;
                if (intro != null)
                {
                    intro.AutoSize = true;
                    intro.Dock = DockStyle.Top;
                    TableLayoutPanel table = intro.Parent as TableLayoutPanel;
                    if (table != null && table.GetRow(intro) < table.RowStyles.Count)
                        table.RowStyles[table.GetRow(intro)] = new RowStyle(SizeType.AutoSize);
                }
                Button button = child as Button;
                if (button != null)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Line;
                    button.FlatAppearance.MouseOverBackColor = Selection;
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(209, 225, 248);
                    button.BackColor = button is GearButton ? Selection : Color.White;
                    if (button is GearButton) button.FlatAppearance.BorderColor = Accent;
                    button.ForeColor = Ink;
                    button.UseVisualStyleBackColor = false;
                    button.Padding = new Padding(6, 1, 6, 1);
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
                    grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 4, 5, 4);
                    grid.DefaultCellStyle.Padding = new Padding(5, 3, 5, 3);
                    grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    grid.DefaultCellStyle.ForeColor = Ink;
                    grid.DefaultCellStyle.SelectionBackColor = Selection;
                    grid.DefaultCellStyle.SelectionForeColor = Ink;
                    grid.RowTemplate.Height = Math.Max(30, grid.Font.Height + 10);
                    foreach (DataGridViewRow row in grid.Rows) row.Height = grid.RowTemplate.Height;
                    grid.DefaultCellStyle.SelectionForeColor = Accent;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 254);
                }
                ComboBox choice = child as ComboBox;
                NumericUpDown number = child as NumericUpDown;
                if (number != null && !(number.FindForm() is MainForm))
                {
                    int width = Math.Max(TextRenderer.MeasureText(number.Minimum.ToString(), number.Font).Width,
                        TextRenderer.MeasureText(number.Maximum.ToString(), number.Font).Width) + SystemInformation.VerticalScrollBarWidth + 10;
                    number.MinimumSize = new Size(Math.Max(number.MinimumSize.Width, width), number.MinimumSize.Height);
                }
                if (choice != null && choice.DrawMode == DrawMode.Normal)
                {
                    choice.DrawMode = DrawMode.OwnerDrawFixed;
                    choice.ItemHeight = Math.Max(22, choice.Font.Height + 6);
                    choice.DrawItem += DrawChoice;
                }
                Apply(child);
            }
        }

        internal static void Menu(ToolStrip menu, Font font)
        {
            menu.Font = font;
            foreach (ToolStripItem item in menu.Items)
            {
                if (!(item is ToolStripSeparator)) item.Padding = new Padding(2, 4, 2, 4);
                ToolStripMenuItem choice = item as ToolStripMenuItem;
                if (choice != null && choice.HasDropDownItems) Menu(choice.DropDown, font);
            }
        }

        private static void DrawChoice(object sender, DrawItemEventArgs e)
        {
            ComboBox choice = (ComboBox)sender;
            e.DrawBackground();
            string text = e.Index >= 0 && e.Index < choice.Items.Count ? choice.Items[e.Index].ToString() : choice.Text;
            TextRenderer.DrawText(e.Graphics, text, e.Font, Rectangle.Inflate(e.Bounds, -3, 0),
                choice.Enabled ? ((e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : Ink) : SystemColors.GrayText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
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
        internal bool Emphasized;
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
            using (Pen border = new Pen(Emphasized ? EditorTheme.Accent : EditorTheme.Line))
            { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(border, shape); }
            int unit = Font.Height;
            int titleLeft = 18;
            if (!string.IsNullOrEmpty(Badge))
            {
                Rectangle badge = new Rectangle(12, 6, unit + 4, unit + 4);
                using (Brush fill = new SolidBrush(EditorTheme.Selection)) e.Graphics.FillEllipse(fill, badge);
                TextRenderer.DrawText(e.Graphics, Badge, Font, badge, EditorTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                titleLeft = badge.Right + 8;
            }
            using (Font title = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, title, new Rectangle(titleLeft, 6, Math.Max(0, Width - titleLeft - 12), unit + 4),
                    EditorTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    // Native Buttons retain keyboard navigation, default-button and accessibility behavior.
    internal class EditorButton : Button
    {
        internal bool CompactText;
        private bool hovered;
        private bool pressed;
        internal EditorButton() { DoubleBuffered = true; FlatStyle = FlatStyle.Flat; UseMnemonic = false; }
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
            TextRenderer.DrawText(e.Graphics, Text, Font, Rectangle.Inflate(ClientRectangle, CompactText ? -1 : -5, -2),
                Enabled ? ForeColor : EditorTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                (CompactText ? TextFormatFlags.NoPadding : TextFormatFlags.Default));
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
        }
    }

    internal sealed class EditorIntro : Control
    {
        internal string Description = "";
        internal EditorIntro() { DoubleBuffered = true; }
        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = proposedSize.Width > 0 ? proposedSize.Width : Parent == null ? Width : Parent.ClientSize.Width - Parent.Padding.Horizontal;
            width = Math.Max(100, width);
            using (Font heading = new Font(Font.FontFamily, Font.Size + 3, FontStyle.Bold))
                return new Size(width, heading.Height + 8 + TextRenderer.MeasureText(Description, Font,
                    new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 6);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Font heading = new Font(Font.FontFamily, Font.Size + 3, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, Text, heading, new Rectangle(0, 2, Width, heading.Height + 8), EditorTheme.Ink, TextFormatFlags.NoPrefix);
            int top = heading.Height + 8;
            TextRenderer.DrawText(e.Graphics, Description, Font, new Rectangle(0, top, Width, Math.Max(0, Height - top)), EditorTheme.Muted,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
        }
    }

    internal sealed class EditorWordmark : Control
    {
        internal EditorWordmark() { DoubleBuffered = true; AccessibleName = "InputStitch"; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int mark = Math.Min(32, Height - 4);
            Form form = FindForm();
            if (form != null && form.Icon != null)
                e.Graphics.DrawIcon(form.Icon, new Rectangle(0, (Height - mark) / 2, mark, mark));
            using (Font brand = new Font(Font.FontFamily, Font.Size + 1.5F, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, "InputStitch", brand, new Rectangle(mark + 5, 0, Math.Max(0, Width - mark - 5), Height), EditorTheme.Ink,
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
            Font = EditorTheme.BodyFont();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(800, 570);
            MinimumSize = new Size(760, 580);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            Controls.Add(root);
            root.Controls.Add(new EditorIntro { Dock = DockStyle.Fill, Text = Localizer.IsEnglish ? "What would you like to do?" : "想让按键帮你做什么？",
                Description = Localizer.IsEnglish ? "Choose a starting point. You can change every setting after creating it." : "选择一种常用操作作为起点，创建后随时可以修改。" }, 0, 0);
            TableLayoutPanel cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0) };
            for (int i = 0; i < 2; i++) { cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); }
            string[] names = Localizer.IsEnglish ? new[] { "Repeat an action", "Run a sequence", "Hold a controller input", "Start from scratch" }
                : new[] { "重复操作", "按顺序执行", "按住控制手柄", "从空白开始" };
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
        internal Rectangle KeyBounds
        {
            get
            {
                float scale=Font.GetHeight()/Font.GetHeight(96F);
                int height=Math.Max(20,Math.Min((int)(80*scale),Height-Font.Height*3));
                using(Font keyFont=new Font(Font.FontFamily,Font.Size+8,FontStyle.Bold))
                {
                    int width=Math.Min(Math.Max(height,TextRenderer.MeasureText(Text,keyFont).Width+(int)(28*scale)),Math.Max(20,Width-(int)(32*scale)));
                    return new Rectangle((Width-width)/2,Math.Max(0,(Height-height-Font.Height*2-(int)(16*scale))/2),width,height);
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle key=KeyBounds;
            using (GraphicsPath shape = EditorTheme.Rounded(key, 12))
            using (Brush fill = new SolidBrush(EditorTheme.Selection))
            using (Pen outline = new Pen(EditorTheme.Accent, 2))
            { e.Graphics.FillPath(fill, shape); e.Graphics.DrawPath(outline, shape); }
            using (Font keyFont = new Font(Font.FontFamily, Font.Size + 8, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, keyFont, Rectangle.Inflate(key, -8, -4), EditorTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics, Localizer.IsEnglish ? "Selected input" : "当前执行按键", Font,
                new Rectangle(4, key.Bottom + Font.Height, Width - 8, Font.Height * 2), EditorTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }

    // WinForms omits DropDownList selection text during WM_PRINT on some Windows
    // themes. Print the real selected value so off-screen layout captures match it.
    internal sealed class EditorChoice : ComboBox
    {
        internal bool CompactWidth = true;
        private bool busy;
        private ToolTip uiToolTip;
        private Control parent;

        internal void FitWidth()
        {
            if (!CompactWidth || busy || DropDownStyle != ComboBoxStyle.DropDownList) return;
            busy = true;
            try
            {
                float scale = Math.Max(Font.GetHeight() / Font.GetHeight(96F),Font.Size / EditorTheme.BodyFontSize);
                int measured = TextRenderer.MeasureText(Text,Font).Width;
                foreach (object item in Items) measured = Math.Max(measured,TextRenderer.MeasureText(GetItemText(item),Font).Width);
                int width = Math.Max((int)(112*scale),Math.Min((int)(320*scale),measured+SystemInformation.VerticalScrollBarWidth+(int)(14*scale)));
                TableLayoutPanel table = Parent as TableLayoutPanel;
                if (table != null)
                {
                    int column=table.GetColumn(this), span=table.GetColumnSpan(this), available=0;
                    int[] columns=table.GetColumnWidths();
                    for(int i=column;i>=0 && i<Math.Min(columns.Length,column+span);i++) available+=columns[i];
                    if(available>Margin.Horizontal+40*scale) width=Math.Min(width,available-Margin.Horizontal);
                }
                Dock=DockStyle.None; Anchor=AnchorStyles.Left;
                AutoSize=true;
                if(Width!=width) Width=width;
                DropDownWidth=Math.Max(width,Math.Min(Screen.FromControl(this).WorkingArea.Width-32,measured+SystemInformation.VerticalScrollBarWidth+24));
                AccessibleDescription=Text;
                bool clipped=TextRenderer.MeasureText(Text,Font).Width+SystemInformation.VerticalScrollBarWidth+6>width;
                if(clipped && uiToolTip==null) uiToolTip=new ToolTip();
                if(uiToolTip!=null) uiToolTip.SetToolTip(this,clipped?Text:"");
            }
            finally { busy=false; }
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); FitWidth(); }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size size=base.GetPreferredSize(proposedSize);
            size.Height=Math.Max(size.Height,Height);
            if(CompactWidth && DropDownStyle==ComboBoxStyle.DropDownList)
            {
                float scale=Math.Max(Font.GetHeight()/Font.GetHeight(96F),Font.Size/EditorTheme.BodyFontSize);
                int width=TextRenderer.MeasureText(Text,Font).Width;
                foreach(object item in Items) width=Math.Max(width,TextRenderer.MeasureText(GetItemText(item),Font).Width);
                size.Width=Math.Max(MinimumSize.Width,Math.Max((int)(112*scale),Math.Min((int)(320*scale),width+SystemInformation.VerticalScrollBarWidth+(int)(14*scale))));
            }
            return size;
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (DrawMode == DrawMode.OwnerDrawFixed) ItemHeight = Math.Max(22, Font.Height + 6);
            FitWidth();
        }
        protected override void OnDropDown(EventArgs e)
        {
            int width=Width;
            foreach(object item in Items) width=Math.Max(width,TextRenderer.MeasureText(GetItemText(item),Font).Width+SystemInformation.VerticalScrollBarWidth+24);
            DropDownWidth=Math.Min(Screen.FromControl(this).WorkingArea.Width-32,width);
            base.OnDropDown(e);
        }
        protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); FitWidth(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); FitWidth(); }
        private void OnLayout(object sender,LayoutEventArgs e) { FitWidth(); }
        protected override void OnParentChanged(EventArgs e)
        {
            if(parent!=null) parent.Layout-=OnLayout;
            base.OnParentChanged(e); parent=Parent;
            if(parent!=null) parent.Layout+=OnLayout;
            FitWidth();
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(parent!=null) { parent.Layout-=OnLayout; parent=null; }
                if(uiToolTip!=null) { uiToolTip.Dispose(); uiToolTip=null; }
            }
            base.Dispose(disposing);
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if ((message.Msg == 0x0317 || message.Msg == 0x0318) && message.WParam != IntPtr.Zero &&
                Visible && DropDownStyle == ComboBoxStyle.DropDownList && SelectedIndex >= 0)
            {
                using (Graphics graphics = Graphics.FromHdc(message.WParam))
                {
                    Rectangle content = new Rectangle(4, 3, Math.Max(0, Width - SystemInformation.VerticalScrollBarWidth - 8), Height - 6);
                    Rectangle visible = ClientRectangle;
                    for (Control parent = Parent; parent != null; parent = parent.Parent)
                        visible.Intersect(RectangleToClient(parent.RectangleToScreen(parent.ClientRectangle)));
                    if (visible.IsEmpty) return;
                    graphics.SetClip(visible);
                    using (Brush background = new SolidBrush(Enabled ? SystemColors.Window : SystemColors.Control)) graphics.FillRectangle(background, content);
                    TextRenderer.DrawText(graphics, GetItemText(SelectedItem), Font,
                        content,
                        Enabled ? EditorTheme.Ink : SystemColors.GrayText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
                }
            }
        }
    }

    // Lay out whole field groups against the actual width; auto-size flow tables cache stale heights after wrapping.
    internal sealed class EditorFieldFlow : Panel
    {
        private bool layoutQueued;
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); QueueParentLayout(); }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            PerformLayout();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            TableLayoutPanel table = Parent as TableLayoutPanel;
            if (table == null || table.ClientSize.Width <= 0) return;
            int row = table.GetRow(this);
            if (row < 0 || row >= table.RowStyles.Count) return;
            int x = Padding.Left, y = Padding.Top, rowHeight = 0;
            foreach (Control control in Controls)
            {
                Size size = control.AutoSize ? control.GetPreferredSize(Size.Empty) : control.Size;
                if (x > Padding.Left && x + size.Width + control.Margin.Horizontal > ClientSize.Width - Padding.Right)
                { x = Padding.Left; y += rowHeight; rowHeight = 0; }
                control.Bounds = new Rectangle(x + control.Margin.Left, y + control.Margin.Top, size.Width, size.Height);
                x += size.Width + control.Margin.Horizontal;
                rowHeight = Math.Max(rowHeight, size.Height + control.Margin.Vertical);
            }
            int height = y + rowHeight + Padding.Bottom + Margin.Vertical;
            if (table.RowStyles[row].Height != height)
            {
                table.RowStyles[row].Height = height;
                // Flush the containing auto-size tables after their current layout transaction finishes.
                QueueParentLayout();
            }
        }
        private void QueueParentLayout()
        {
            if (!IsHandleCreated || layoutQueued) return;
            layoutQueued = true;
            BeginInvoke((MethodInvoker)delegate
            {
                layoutQueued = false;
                for (Control container = Parent; container != null; container = container.Parent) container.PerformLayout();
            });
        }
    }

    public partial class MainForm
    {
        private FlowLayoutPanel triggerSelector;
        private Label repeatUnitLabel;

        // Only the outer flow wraps; a label and its field (or the whole repeat group) stay together.
        private static FlowLayoutPanel CompactFieldGroup(params Control[] controls)
        {
            FlowLayoutPanel group = new FlowLayoutPanel { AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                Margin = new Padding(0, 0, 14, 4) };
            foreach (Control control in controls)
            {
                control.Anchor = control is ComboBox ? AnchorStyles.Left | AnchorStyles.Top : AnchorStyles.Left;
                if (control is ComboBox) control.Margin = new Padding(0);
                group.Controls.Add(control);
            }
            return group;
        }

        private void RefreshTriggerSelector()
        {
            if (triggerButton == null || triggerModeBox == null) return;
            MacroDefinition macro = SelectedMacro;
            string shortcut = macro == null || macro.Trigger == null ? "" : InputNames.FormatTrigger(macro.Trigger);
            triggerButton.Text = captureMode == CaptureMode.Trigger ? Localizer.T("请按触发键…") :
                string.IsNullOrEmpty(shortcut) ? EditorDialogs.T("选择触发键…", "Choose shortcut…") : shortcut;
            triggerButton.AccessibleName = EditorDialogs.T("触发键：", "Shortcut: ") + triggerButton.Text;
            float scale = Font.GetHeight() / Font.GetHeight(96F);
            int selectorWidth = Math.Max((int)(128 * scale), TextRenderer.MeasureText(triggerButton.Text, triggerButton.Font).Width + (int)(18 * scale));
            triggerButton.Width = Math.Min((int)(260 * scale), selectorWidth);
            if (triggerSelector != null)
                foreach (Control control in triggerSelector.Controls)
                    if (control != triggerButton) control.AccessibleName = EditorDialogs.T("更多输入方式", "More input methods");
            int modeWidth = 0;
            foreach (string name in TriggerEditor.Names)
                modeWidth = Math.Max(modeWidth, TextRenderer.MeasureText(name, triggerModeBox.Font).Width);
            triggerModeBox.Width = modeWidth + (int)(30 * scale);
            // Native combo height can grow when its handle is created; reserve the same field height as buttons.
            if (triggerModeBox.Parent != null) triggerModeBox.Parent.MinimumSize = new Size(0, (int)Math.Ceiling(32 * scale));
            triggerModeBox.DropDownWidth = triggerModeBox.Width;
            if (repeatBox != null)
                repeatBox.Width = TextRenderer.MeasureText(repeatBox.Maximum.ToString(), repeatBox.Font).Width + (int)(24 * scale);
            if (repeatUnitLabel != null) repeatUnitLabel.Text = EditorDialogs.T("次", "times");
            if (uiToolTip != null)
                uiToolTip.SetToolTip(triggerButton, triggerButton.Text + Environment.NewLine +
                    EditorDialogs.T("点击选择；右侧箭头可捕获实体键盘或鼠标。", "Click to choose; use the arrow to capture a physical key or mouse input."));
        }

        private TableLayoutPanel morePanel;
        private EditorSection optionsFrame;
        private bool moreExpanded;
        private Button moreButton;
        private Label macroCountLabel;
        private Label stepsHintLabel;
        private Panel emptyStepsPanel;
        private Button addStepButton;
        private Button editStepButton;
        private Button batchTimingButton;
        private Button newMacroButton;
        private Label workflowGuide;

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
            EditorTheme.Menu(menu, Font);
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
            if (workflowGuide != null) workflowGuide.Text = count == 0
                ? EditorDialogs.T("下一步：在“执行步骤”中添加要按的键，或录制一次操作。", "Next: add the keys to press under Macro Steps, or record your actions.")
                : macro != null && !macro.Enabled
                    ? EditorDialogs.T("设置会自动保存。点击底部运行按钮即可执行；启用触发键后，也能用快捷键启动。", "Changes save automatically. Use Run at the bottom, or enable the shortcut to start with a key.")
                    : EditorDialogs.T("设置会自动保存。点击底部运行按钮，或切到目标程序后按触发键启动；使用“全部停止”可结束所有宏。", "Changes save automatically. Use Run below, or switch to your target app and press the shortcut. Stop all ends every macro.");
            if (workflowGuide != null) workflowGuide.Visible = count == 0;
            if (stepsHintLabel != null) stepsHintLabel.Text = count == 0
                ? EditorDialogs.T("添加或录制操作", "Add or record actions")
                : (Localizer.IsEnglish ? count + (count == 1 ? " step" : " steps") : count + " 个步骤");
            if (emptyStepsPanel != null) emptyStepsPanel.Visible = macro != null && count == 0;
            if (grid != null) grid.Visible = count > 0;
            if (addStepButton != null) addStepButton.Enabled = macro != null && !recordingActive;
            if (editStepButton != null) editStepButton.Enabled = macro != null && count > 0 && !recordingActive;
            if (batchTimingButton != null) batchTimingButton.Enabled = macro != null && count > 0 && !recordingActive;
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
            bool show = !moreExpanded;
            moreExpanded = show;
            morePanel.Visible = show;
            optionsFrame.Visible = show;
            UpdateTargetSectionVisibility();
            RefreshOptionalSettingsText(show);
        }

        private void RefreshOptionalSettingsText(bool expanded)
        {
            if (moreButton != null) moreButton.Text = Localizer.IsEnglish
                ? (expanded ? "▾ Fewer options" : "▸ More options")
                : (expanded ? "▾ 收起更多设置" : "▸ 更多设置");
            if(moreButton!=null)
            {
                moreButton.BackColor=expanded?EditorTheme.Selection:Color.White;
                moreButton.FlatAppearance.BorderColor=expanded?EditorTheme.Accent:EditorTheme.Line;
                moreButton.AccessibleDescription=EditorDialogs.T("映射层与运行目标", "Layers and run target")+(expanded?EditorDialogs.T("，已展开", ", expanded"):EditorDialogs.T("，已收起", ", collapsed"));
            }
            if(optionsFrame!=null) optionsFrame.Text=EditorDialogs.T("更多设置 · 映射层与运行目标", "More options · Layers and run target");
        }
    }
}
