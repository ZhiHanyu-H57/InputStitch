using System;
using System.Drawing;
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
                    button.Padding = new Padding(4, 1, 4, 1);
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
                    grid.RowTemplate.Height = 34;
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
        internal EditorSection()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using (Pen border = new Pen(EditorTheme.Line))
                e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            using (Brush accent = new SolidBrush(EditorTheme.Accent))
                e.Graphics.FillRectangle(accent, 0, 14, 3, 17);
            using (Font title = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, Text, title, new Rectangle(16, 10, Math.Max(0, Width - 32), 25),
                    EditorTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
                    TextRenderer.DrawText(graphics, GetItemText(SelectedItem), Font,
                        new Rectangle(4, 1, Math.Max(0, Width - SystemInformation.VerticalScrollBarWidth - 8), Height - 2),
                        Enabled ? EditorTheme.Ink : SystemColors.GrayText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }

    public partial class MainForm
    {
        private TableLayoutPanel optionalSettingsPanel;
        private bool optionalSettingsExpanded;
        private Button optionalSettingsButton;
        private Label triggerBehaviorLabel;

        private void DrawMacroItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= config.Macros.Count) return;
            MacroDefinition macro = config.Macros[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (Brush fill = new SolidBrush(selected ? EditorTheme.Selection : Color.White)) e.Graphics.FillRectangle(fill, e.Bounds);
            if (selected) using (Brush mark = new SolidBrush(EditorTheme.Accent)) e.Graphics.FillRectangle(mark, e.Bounds.Left, e.Bounds.Top + 8, 3, e.Bounds.Height - 16);
            string name = FormatMacroListItem(macro);
            using (Font title = new Font(e.Font, selected ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(e.Graphics, name, title, new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 7, e.Bounds.Width - 24, 23), EditorTheme.Ink,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            string detail = InputNames.FormatTrigger(macro.Trigger) + "  ·  " + TriggerEditor.Names[TriggerEditor.Index(macro)];
            TextRenderer.DrawText(e.Graphics, detail, e.Font, new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 30, e.Bounds.Width - 24, 23), EditorTheme.Muted,
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
