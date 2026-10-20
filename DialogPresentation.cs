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

        internal static void Frame(Form form, Control body, Control actions, string description, Control oldIntro)
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
            TableLayoutPanel shell = new TableLayoutPanel { Name = "dialogShell", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20, 16, 20, 12) };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.Controls.Add(new EditorIntro { Dock = DockStyle.Fill, Text = form.Text, Description = description, Margin = new Padding(0) }, 0, 0);
            body.Dock = DockStyle.Fill;
            body.Margin = new Padding(0);
            shell.Controls.Add(body, 0, 1);
            actions.Dock = DockStyle.Fill;
            actions.Margin = new Padding(0);
            actions.Padding = new Padding(0, 12, 0, 0);
            actions.MinimumSize = new Size(0, 52);
            shell.Controls.Add(actions, 0, 2);
            form.AutoScroll = false;
            form.Controls.Add(shell);
            EditorTheme.Apply(form);
            Button primary = form.AcceptButton as Button;
            if (primary != null) EditorTheme.Primary(primary, false);
        }

        internal static EditorSection Section(string title)
        {
            return new EditorSection { Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top, Padding = new Padding(16, 46, 16, 14), Margin = new Padding(0, 0, 0, 12) };
        }

        internal static FlowLayoutPanel Actions(params Control[] controls)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.RightToLeft };
            row.Controls.AddRange(controls);
            return row;
        }

        internal static Button Button(string text, DialogResult result)
        {
            return new EditorButton { Text = text, AutoSize = true, MinimumSize = new Size(100, 36), DialogResult = result };
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
            ItemSize = new Size(145, 36);
            Padding = new Point(16, 8);
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
}
