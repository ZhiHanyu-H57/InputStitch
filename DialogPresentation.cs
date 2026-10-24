using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InputStitch
{
    internal static class EditorDialogs
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr parameter, string text);

        internal static void Hint(TextBox input, string text)
        {
            input.AccessibleDescription = text;
            input.HandleCreated += delegate { SendMessage(input.Handle, 0x1501, new IntPtr(1), text); };
            if (input.IsHandleCreated) SendMessage(input.Handle, 0x1501, new IntPtr(1), text);
        }

        internal static string T(string zh, string en) { return Localizer.IsEnglish ? en : zh; }

        internal static void PrepareWindow(Form form)
        {
            Size client = form.ClientSize, minimum = form.MinimumSize;
            form.Shown += delegate
            {
                if(form.Owner!=null) form.Icon=form.Owner.Icon;
                float scale = form.Font.GetHeight() / form.Font.GetHeight(96F);
                Size scaled = new Size((int)Math.Ceiling(client.Width * scale), (int)Math.Ceiling(client.Height * scale));
                // Constructors can assign logical sizes after WinForms' first DPI scaling pass.
                // Normalize once, while preserving an explicitly requested size supplied before Show.
                if (form.ClientSize == client || form.ClientSize == scaled)
                {
                    Rectangle area = Screen.FromControl(form).WorkingArea;
                    form.MinimumSize = new Size(Math.Min((int)Math.Ceiling(minimum.Width * scale),area.Width - 24),
                        Math.Min((int)Math.Ceiling(minimum.Height * scale),area.Height - 24));
                    form.ClientSize = scaled;
                    if (form.StartPosition != FormStartPosition.Manual)
                    {
                        form.Size = new Size(Math.Min(form.Width,area.Width - 24),Math.Min(form.Height,area.Height - 24));
                        Rectangle anchor = form.Owner == null ? area : form.Owner.Bounds;
                        form.Location = new Point(Math.Max(area.Left+12,Math.Min(area.Right-form.Width-12,anchor.Left+(anchor.Width-form.Width)/2)),
                            Math.Max(area.Top+12,Math.Min(area.Bottom-form.Height-12,anchor.Top+(anchor.Height-form.Height)/2)));
                    }
                }
            };
        }


        internal static void Frame(Form form, Control body, Control actions, string description, Control oldIntro)
        {
            form.SuspendLayout();
            try { FrameCore(form, body, actions, description, oldIntro); }
            finally { form.ResumeLayout(true); }
        }

        private static void FrameCore(Form form, Control body, Control actions, string description, Control oldIntro)
        {
            if (oldIntro != null) { oldIntro.Parent.Controls.Remove(oldIntro); oldIntro.Dispose(); }
            TableLayoutPanel oldTable = actions.Parent as TableLayoutPanel;
            if (oldTable != null)
            {
                int row = oldTable.GetRow(actions);
                if (row >= 0 && row < oldTable.RowStyles.Count) oldTable.RowStyles[row] = new RowStyle(SizeType.Absolute, 0);
            }
            if (actions.Parent != null) actions.Parent.Controls.Remove(actions);
            if (body.Parent != null) body.Parent.Controls.Remove(body);
            TableLayoutPanel shell = new TableLayoutPanel { Name = "dialogShell", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14, 10, 14, 10) };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.Controls.Add(new EditorIntro { AutoSize = true, Dock = DockStyle.Top, Text = form.Text, Description = description, Margin = new Padding(0) }, 0, 0);
            body.Dock = DockStyle.Fill;
            body.Margin = new Padding(0);
            shell.Controls.Add(body, 0, 1);
            actions.Dock = DockStyle.Fill;
            actions.Margin = new Padding(0);
            actions.Padding = new Padding(0, 8, 0, 0);
            actions.MinimumSize = new Size(0, 40);
            shell.Controls.Add(actions, 0, 2);
            form.AutoScroll = false;
            form.Controls.Add(shell);
            EditorTheme.Apply(form);
            Button primary = form.AcceptButton as Button;
            if (primary != null) EditorTheme.Primary(primary, false);
        }

        internal static void ResumeChildLayouts(Control root)
        {
            foreach (Control child in root.Controls) { ResumeChildLayouts(child); child.ResumeLayout(true); }
        }

        internal static EditorSection Section(string title)
        {
            EditorSection section = new EditorSection { Text = title,
                Dock = DockStyle.Top, Padding = new Padding(12, 18, 12, 10), Margin = new Padding(0, 0, 0, 8) };
            FitSection(section);
            return section;
        }

        internal static void FitSection(GroupBox section)
        {
            section.AutoSize = false;
            section.Layout += delegate
            {
                if (section.Controls.Count == 0) return;
                Control content = section.Controls[0];
                int height = (content.AutoSize ? content.GetPreferredSize(new Size(Math.Max(1,section.ClientSize.Width-section.Padding.Horizontal),0)).Height : content.Height) +
                    section.Padding.Vertical + section.Font.Height;
                if (section.MinimumSize.Height != height) section.MinimumSize = new Size(0,height);
                if (section.Height != height) section.Height = height;
            };
        }

        internal static FlowLayoutPanel Actions(params Control[] controls)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, FlowDirection = FlowDirection.RightToLeft };
            row.Controls.AddRange(controls);
            return row;
        }

        internal static Button Button(string text, DialogResult result)
        {
            return new EditorButton { Text = text, AutoSize = true, MinimumSize = new Size(100, 32), DialogResult = result };
        }

        internal static void RowVisible(TableLayoutPanel table, int row, bool visible)
        {
            foreach (Control control in table.Controls) if (table.GetRow(control) == row) control.Visible = visible;
            while (table.RowStyles.Count <= row) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles[row].SizeType = visible ? SizeType.AutoSize : SizeType.Absolute;
            table.RowStyles[row].Height = 0;
        }
    }

    internal sealed class EditorTabs : TabControl
    {
        internal EditorTabs()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(150, 34);
            Padding = new Point(12, 5);
            Multiline = true;
        }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); FitLabels(); }
        protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); FitLabels(); }
        private void FitLabels()
        {
            int width = 90;
            foreach (TabPage page in TabPages) width = Math.Max(width, TextRenderer.MeasureText(page.Text, Font).Width + 24);
            ItemSize = new Size(width, Math.Max(30, Font.Height + 12));
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            bool selected = e.Index == SelectedIndex;
            using (Brush fill = new SolidBrush(selected ? EditorTheme.Selection : EditorTheme.Canvas)) e.Graphics.FillRectangle(fill, e.Bounds);
            TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, e.Bounds, selected ? EditorTheme.Accent : EditorTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (selected) using (Pen line = new Pen(EditorTheme.Accent, 3)) e.Graphics.DrawLine(line, e.Bounds.Left + 12, e.Bounds.Bottom - 2, e.Bounds.Right - 12, e.Bounds.Bottom - 2);
        }
    }

    // Keep native checkbox, keyboard and accessibility behavior, with room around every list row.
    internal sealed class EditorCheckedListBox : CheckedListBox
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, IntPtr value);
        public override int ItemHeight { get { return Math.Max(base.ItemHeight, Font.Height + 8); } set { base.ItemHeight = value; } }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); FitRows(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); FitRows(); }
        private void FitRows() { if (IsHandleCreated) SendMessage(Handle, 0x01A0, IntPtr.Zero, new IntPtr(ItemHeight)); }
    }
}
