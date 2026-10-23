using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace InputStitch
{
    internal static class ToolFields
    {
        internal static string T(string zh, string en) { return EditorDialogs.T(zh, en); }
        internal static TableLayoutPanel Page(ScrollableControl page)
        {
            page.BackColor = Color.White; page.Padding = new Padding(12); page.AutoScroll = true;
            TableLayoutPanel grid = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 0, Padding = new Padding(0, 0, 12, 12) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(grid); return grid;
        }
        internal static void Row(TableLayoutPanel grid, string text, Control control)
        {
            int row = grid.RowCount++; grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Margin = new Padding(3, 5, 3, 5);
            if (text.Length == 0) { grid.Controls.Add(control, 0, row); grid.SetColumnSpan(control, 2); }
            else
            {
                grid.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 5, 12, 5) }, 0, row);
                grid.Controls.Add(control, 1, row);
            }
        }
        internal static Label Help(string text) { return new Label { Text = text, AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Muted }; }
        internal static CheckBox Check(string text) { return new CheckBox { Text = text, AutoSize = true, Anchor = AnchorStyles.Left }; }
        internal static NumericUpDown Number(decimal min, decimal max, decimal value, int decimals, string name)
        { return new NumericUpDown { Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, value)), DecimalPlaces = decimals, Increment = decimals == 0 ? 1 : .1M, Width = 105, AutoScaleMode = AutoScaleMode.None, Name = name }; }
        internal static FlowLayoutPanel Flow(params Control[] controls)
        { FlowLayoutPanel flow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }; flow.Controls.AddRange(controls); return flow; }
        internal static Button Button(string text) { return EditorDialogs.Button(text, DialogResult.None); }
        internal static bool ValidShortcut(TriggerSpec key)
        { return key == null || (key.Kind == InputKind.Keyboard && key.VirtualKey > 0 && key.VirtualKey < 255 && !ModifierSafetyPolicy.IsModifierVirtualKey(key.VirtualKey)); }
        internal static TriggerSpec PickHotkey(IWin32Window owner, TriggerSpec value, string title)
        {
            using (VirtualKeyboardDialog picker = new VirtualKeyboardDialog(true, VirtualKeyboardDialog.InputsFromTrigger(value), T("直接按下组合键，或点击虚拟键盘选择。", "Press a shortcut, or select keys on the virtual keyboard."), title))
                return picker.ShowDialog(owner) == DialogResult.OK ? picker.SelectedTrigger : value;
        }
        internal static Control Shortcut(IWin32Window owner, string name, Func<TriggerSpec> get, Action<TriggerSpec> set, Action changed)
        {
            Button choose = Button(""); choose.Name = name;
            Action refresh = delegate { choose.Text = get() == null ? T("选择快捷键…", "Choose hotkey…") : InputNames.FormatTrigger(get()); };
            choose.Click += delegate { set(PickHotkey(owner, get(), T("选择快捷键", "Choose hotkey"))); refresh(); changed(); };
            Button clear = Button(T("清除", "Clear")); clear.Click += delegate { set(null); refresh(); changed(); };
            refresh(); return Flow(choose, clear);
        }
    }

    internal sealed class TimerSettingsDialog : Form
    {
        private readonly TimerOptions draft;
        private readonly TimerController controller;
        private readonly Func<TimerOptions, string> validate;
        private readonly System.Collections.Generic.List<TimerPart> parts;
        private readonly CheckBox enabled, visible, bold, italic, outline;
        private readonly ComboBox backend, preset, partKind, monitor;
        private readonly NumericUpDown size, border, spacing, opacity, partOpacity, digits, x, y;
        private readonly TextBox partText;
        private readonly Button font, color, outlineColor, save, start;
        private readonly Label feedback;
        private readonly Control preview;
        private readonly ListView partList;
        private readonly TableLayoutPanel detail;
        private readonly int textRow, digitsRow;
        private int selectedIndex;
        private bool loading, partsEdited;
        internal TimerOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        private TimerPart SelectedPart { get { return selectedIndex >= 0 && selectedIndex < parts.Count ? parts[selectedIndex] : null; } }

        internal TimerSettingsDialog(TimerOptions options, TimerController timer, Func<TimerOptions, string> validator)
        {
            draft = (options ?? new TimerOptions()).Clone(); draft.Normalize(); controller = timer; validate = validator;
            parts = TimerText.CreateParts(draft);
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("屏幕计时器", "Screen timer"); StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(920, 700); MinimumSize = new Size(800, 560); MinimizeBox = MaximizeBox = false;
            EditorTabs tabs = new EditorTabs { Dock = DockStyle.Fill };
            TabPage content = new TabPage(T("内容与样式", "Content & style")), position = new TabPage(T("显示与位置", "Output & position")), keys = new TabPage(T("快捷键", "Hotkeys"));
            tabs.TabPages.AddRange(new[] { content, position, keys });
            TableLayoutPanel place = ToolFields.Page(position), hotkeys = ToolFields.Page(keys);
            enabled = ToolFields.Check(T("启用计时器", "Enable timer")); enabled.Checked = draft.Enabled;
            visible = ToolFields.Check(T("显示计时器", "Show timer")); visible.Checked = draft.Visible;
            start = ToolFields.Button(T("开始 / 暂停", "Start / pause")); start.Name = "timerStartPause";
            start.Click += delegate { if (controller != null) controller.StartPause(); Changed(); };
            Button reset = ToolFields.Button(T("归零", "Reset")); reset.Click += delegate { if (controller != null) controller.Reset(); Changed(); };

            TableLayoutPanel editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 94)); content.Controls.Add(editor);
            TableLayoutPanel listPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0, 0, 12, 0) };
            listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            preset = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, Name = "timerPreset" };
            preset.Items.AddRange(new object[] { T("选择预设…", "Choose preset…"), T("分:秒.毫秒", "Minutes:seconds.ms"), T("时:分:秒.毫秒", "Hours:minutes:seconds.ms"), T("秒.毫秒", "Seconds.ms") }); preset.SelectedIndex = 0;
            listPanel.Controls.Add(ToolFields.Flow(new Label { Text = T("预设", "Preset"), AutoSize = true, Anchor = AnchorStyles.Left }, preset), 0, 0);
            partList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, CheckBoxes = true, Name = "timerParts" };
            partList.Columns.Add(T("部分", "Part")); partList.Columns.Add(T("内容", "Content")); listPanel.Controls.Add(partList, 0, 1);
            Button add = ToolFields.Button(T("添加文字", "Add text")), remove = ToolFields.Button(T("删除", "Delete"));
            add.Name = "timerAddPart"; remove.Name = "timerDeletePart";
            add.Click += delegate { AddPart(); }; remove.Click += delegate { RemovePart(); };
            Button up = ToolFields.Button("↑"), down = ToolFields.Button("↓");
            up.MinimumSize = down.MinimumSize = new Size(38, 32);
            up.AutoSize = down.AutoSize = false; up.Size = down.Size = new Size(38, 32);
            up.AccessibleName = T("上移", "Move up"); down.AccessibleName = T("下移", "Move down");
            ToolTip orderTips = new ToolTip(); orderTips.SetToolTip(up, T("上移（Alt + ↑）", "Move up (Alt + ↑)")); orderTips.SetToolTip(down, T("下移（Alt + ↓）", "Move down (Alt + ↓)")); Disposed += delegate { orderTips.Dispose(); };
            up.Name = "timerPartUp"; down.Name = "timerPartDown";
            up.Click += delegate { MovePart(-1); }; down.Click += delegate { MovePart(1); };
            listPanel.Controls.Add(ToolFields.Flow(add, remove, up, down), 0, 2);
            listPanel.Controls.Add(ToolFields.Help(T("勾选决定是否显示，选中后在右侧编辑。", "Check parts to show them; select one to edit its style.")), 0, 3);
            editor.Controls.Add(listPanel, 0, 0);
            Panel stylePanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            detail = ToolFields.Page(stylePanel); editor.Controls.Add(stylePanel, 1, 0);
            partKind = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 165, Name = "timerPartKind" };
            partKind.Items.AddRange(new object[] { T("时", "Hours"), T("分", "Minutes"), T("秒", "Seconds"), T("毫秒", "Milliseconds"), T("自定义文字", "Custom text") });
            ToolFields.Row(detail, T("内容类型", "Part type"), partKind);
            partText = new TextBox { Multiline = true, Height = 45, Dock = DockStyle.Top, MaxLength = 512, ScrollBars = ScrollBars.Vertical, Name = "timerPartText" };
            textRow = detail.RowCount; ToolFields.Row(detail, T("文字", "Text"), partText);
            digits = ToolFields.Number(1, 6, 2, 0, "timerPartDigits"); digitsRow = detail.RowCount; ToolFields.Row(detail, T("显示位数", "Digits"), digits);
            font = ToolFields.Button(""); font.Name = "timerFont"; font.AutoEllipsis = true; font.Anchor = AnchorStyles.Left;
            font.Click += delegate
            {
                TimerPart part = SelectedPart; if (part == null) return;
                using (Font initial = TimerRenderer.CreateFont(part.Style.FontFamily, part.Style.FontSize, (part.Style.Bold ? FontStyle.Bold : FontStyle.Regular) | (part.Style.Italic ? FontStyle.Italic : FontStyle.Regular)))
                using (FontDialog picker = new FontDialog { Font = initial, MinSize = 6, MaxSize = 120, ShowEffects = false })
                    if (picker.ShowDialog(this) == DialogResult.OK) ApplySelectedFont(picker.Font);
            };
            ToolFields.Row(detail, T("字体", "Font"), font);
            size = ToolFields.Number(8, 160, 28, 1, "timerFontSize"); bold = ToolFields.Check(T("加粗", "Bold")); italic = ToolFields.Check(T("斜体", "Italic"));
            ToolFields.Row(detail, T("字号（像素）", "Size (px)"), ToolFields.Flow(size, bold, italic));
            outline = ToolFields.Check(T("启用", "On")); border = ToolFields.Number(0, 12, 2, 1, "timerOutlineWidth");
            ToolFields.Row(detail, T("描边（像素）", "Outline (px)"), ToolFields.Flow(outline, border));
            color = ColorButton(false); outlineColor = ColorButton(true); ToolFields.Row(detail, T("颜色", "Colors"), ToolFields.Flow(color, outlineColor));
            spacing = ToolFields.Number(-4, 40, 0, 1, "timerLetterSpacing"); ToolFields.Row(detail, T("字距（像素）", "Spacing (px)"), spacing);
            partOpacity = ToolFields.Number(0, 100, 100, 0, "timerPartOpacity"); ToolFields.Row(detail, T("本段不透明度", "Part opacity (%)"), partOpacity);
            foreach (Control field in detail.Controls) field.Margin = new Padding(field.Margin.Left, 3, field.Margin.Right, 3);
            preview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 40, 52), Name = "timerPreview", Margin = new Padding(0, 8, 0, 0) };
            preview.Paint += PaintPreview; editor.Controls.Add(preview, 0, 1); editor.SetColumnSpan(preview, 2);

            backend = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Name = "timerBackend" };
            backend.Items.AddRange(new object[] { T("普通置顶窗口", "Desktop overlay"), T("RTSS 游戏内绘制", "RTSS in-game overlay") }); backend.SelectedIndex = (int)draft.Backend;
            ToolFields.Row(place, T("显示方式", "Output"), backend);
            ToolFields.Row(place, "", ToolFields.Help(T("普通置顶窗口不进入 OBS“游戏捕获”。录制 RTSS：先运行 RTSS 7.3.7+，在 OBS 开启“捕获第三方叠加层”，在 RTSS Setup 开启“Use Microsoft Detours API hooking”。", "Desktop overlays stay outside OBS Game Capture. To record RTSS, run RTSS 7.3.7+, enable Capture third-party overlays in OBS and Use Microsoft Detours API hooking in RTSS Setup.")));
            opacity = ToolFields.Number(0, 100, draft.Opacity, 0, "timerOpacity"); ToolFields.Row(place, T("整体不透明度（%）", "Overall opacity (%)"), opacity);
            monitor = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Name = "timerMonitor" };
            monitor.Items.Add(T("主显示器", "Primary display")); foreach (Screen screen in Screen.AllScreens) monitor.Items.Add(screen.DeviceName);
            if (draft.MonitorDevice.Length > 0 && !monitor.Items.Contains(draft.MonitorDevice)) monitor.Items.Add(draft.MonitorDevice);
            monitor.SelectedIndex = 0; for (int i = 1; i < monitor.Items.Count; i++) if ((string)monitor.Items[i] == draft.MonitorDevice) monitor.SelectedIndex = i;
            ToolFields.Row(place, T("显示器（普通窗口）", "Display (desktop)"), monitor);
            x = ToolFields.Number(-16384, 16384, draft.X, 0, "timerX"); y = ToolFields.Number(-16384, 16384, draft.Y, 0, "timerY");
            ToolFields.Row(place, T("水平位置（像素）", "X position (px)"), PositionSlider(x, 7680, "timerXSlider"));
            ToolFields.Row(place, T("垂直位置（像素）", "Y position (px)"), PositionSlider(y, 4320, "timerYSlider"));
            ToolFields.Row(place, "", ToolFields.Help(T("左上角为原点；滑块粗调，数字框每次微调 1 像素。RTSS 以游戏画面为准。", "Positions start at the top-left. Sliders move coarsely; numbers adjust by one pixel. RTSS uses game-frame coordinates.")));
            Button home = ToolFields.Button(T("回到左上角", "Reset position")); home.Click += delegate { x.Value = y.Value = 24; }; ToolFields.Row(place, "", home);
            ToolFields.Row(hotkeys, T("开始 / 暂停", "Start / pause"), ToolFields.Shortcut(this, "timerStartHotkey", () => draft.StartPauseHotkey, v => draft.StartPauseHotkey = v, Changed));
            ToolFields.Row(hotkeys, T("归零并暂停", "Reset & pause"), ToolFields.Shortcut(this, "timerResetHotkey", () => draft.ResetHotkey, v => draft.ResetHotkey = v, Changed));
            ToolFields.Row(hotkeys, T("显示 / 隐藏", "Show / hide"), ToolFields.Shortcut(this, "timerVisibilityHotkey", () => draft.VisibilityHotkey, v => draft.VisibilityHotkey = v, Changed));
            feedback = ToolFields.Help(""); feedback.Name = "timerFeedback";
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.Controls.Add(ToolFields.Flow(enabled, visible, start, reset), 0, 0); body.Controls.Add(tabs, 0, 1); body.Controls.Add(feedback, 0, 2);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "timerSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(save, cancel), T("分段编辑计时内容和样式", "Edit timer parts and their styles"), null);
            partList.Resize += delegate { FitColumns(); };
            partList.SelectedIndexChanged += delegate { if (!loading && partList.SelectedIndices.Count > 0) { selectedIndex = partList.SelectedIndices[0]; LoadPart(); } };
            partList.ItemChecked += delegate(object sender, ItemCheckedEventArgs e) { if (!loading) { parts[e.Item.Index].Visible = e.Item.Checked; partsEdited = true; Changed(); } };
            partList.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) { RemovePart(); e.Handled = true; } else if (e.Alt && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)) { MovePart(e.KeyCode == Keys.Up ? -1 : 1); e.Handled = true; } };
            preset.SelectedIndexChanged += delegate { if (preset.SelectedIndex > 0) ApplyPreset((TimerTimeFormat)(preset.SelectedIndex - 1)); };
            partKind.SelectedIndexChanged += delegate { if (loading || SelectedPart == null) return; SelectedPart.Kind = (TimerPartKind)partKind.SelectedIndex; partsEdited = true; LoadPart(); RebuildList(); Changed(); };
            partText.TextChanged += delegate { if (!loading && SelectedPart != null) { SelectedPart.Text = partText.Text.Replace("\r\n", "\n"); partsEdited = true; UpdateItem(); Changed(); } };
            digits.ValueChanged += delegate { if (!loading && SelectedPart != null) { SelectedPart.Digits = (int)digits.Value; partsEdited = true; UpdateItem(); Changed(); } };
            foreach (CheckBox control in new[] { bold, italic, outline }) control.CheckedChanged += delegate { EditStyle(); };
            foreach (NumericUpDown control in new[] { size, border, spacing, partOpacity }) control.ValueChanged += delegate { EditStyle(); };
            foreach (CheckBox control in new[] { enabled, visible }) control.CheckedChanged += delegate { Changed(); };
            foreach (NumericUpDown control in new[] { opacity, x, y }) control.ValueChanged += delegate { Changed(); };
            foreach (ComboBox control in new[] { backend, monitor }) control.SelectedIndexChanged += delegate { Changed(); };
            selectedIndex = parts.Count == 0 ? -1 : 0; RebuildList(); LoadPart(); Changed();
        }
        private void FitColumns()
        { if (partList.Columns.Count < 2) return; int first = Math.Min(partList.ClientSize.Width / 2, (int)Math.Ceiling(145 * partList.Font.Height / 16F)); partList.Columns[0].Width = Math.Max(70, first); partList.Columns[1].Width = Math.Max(40, partList.ClientSize.Width - first - SystemInformation.VerticalScrollBarWidth - 4); }
        private static string PartName(TimerPart part)
        { switch (part.Kind) { case TimerPartKind.Hours: return T("时", "Hours"); case TimerPartKind.Minutes: return T("分", "Minutes"); case TimerPartKind.Seconds: return T("秒", "Seconds"); case TimerPartKind.Milliseconds: return T("毫秒", "Milliseconds"); default: return T("文字", "Text"); } }
        private static string PartContent(TimerPart part) { return part.Kind == TimerPartKind.Text ? part.Text.Replace("\n", " ↵ ") : new string('0', part.Digits); }
        private void RebuildList()
        {
            loading = true; partList.BeginUpdate(); partList.Items.Clear();
            foreach (TimerPart part in parts) { ListViewItem item = new ListViewItem(PartName(part)) { Checked = part.Visible }; item.SubItems.Add(PartContent(part)); partList.Items.Add(item); }
            if (selectedIndex >= 0 && selectedIndex < partList.Items.Count) { partList.Items[selectedIndex].Selected = true; partList.Items[selectedIndex].EnsureVisible(); }
            partList.EndUpdate(); loading = false; FitColumns();
        }
        private void UpdateItem() { if (SelectedPart == null || selectedIndex >= partList.Items.Count) return; partList.Items[selectedIndex].Text = PartName(SelectedPart); partList.Items[selectedIndex].SubItems[1].Text = PartContent(SelectedPart); }
        private void LoadPart()
        {
            loading = true; TimerPart part = SelectedPart; detail.Enabled = part != null;
            if (part != null)
            {
                TimerPartStyle style = part.Style; partKind.SelectedIndex = (int)part.Kind; partText.Text = part.Text.Replace("\n", "\r\n");
                digits.Maximum = part.Kind == TimerPartKind.Milliseconds ? 3 : 6; digits.Value = Math.Min(digits.Maximum, Math.Max(digits.Minimum, part.Digits));
                bool text = part.Kind == TimerPartKind.Text; partText.Visible = detail.GetControlFromPosition(0, textRow).Visible = text;
                digits.Visible = detail.GetControlFromPosition(0, digitsRow).Visible = !text;
                font.Text = style.FontFamily; size.Value = (decimal)style.FontSize; bold.Checked = style.Bold; italic.Checked = style.Italic;
                outline.Checked = style.Outline; border.Value = (decimal)style.OutlineWidth; spacing.Value = (decimal)style.LetterSpacing; partOpacity.Value = style.Opacity;
                border.Enabled = outlineColor.Enabled = style.Outline;
            }
            loading = false;
        }
        private void AddPart()
        {
            if (parts.Count >= 32) return;
            TimerPartStyle style = SelectedPart == null ? TimerPartStyle.FromOptions(draft) : SelectedPart.Style.Clone(); style.Opacity = 100;
            parts.Add(new TimerPart { Kind = TimerPartKind.Text, Text = T("文字", "Text"), Style = style }); selectedIndex = parts.Count - 1; partsEdited = true; RebuildList(); LoadPart(); Changed();
        }
        private void RemovePart() { if (SelectedPart == null) return; parts.RemoveAt(selectedIndex); selectedIndex = Math.Min(selectedIndex, parts.Count - 1); partsEdited = true; RebuildList(); LoadPart(); Changed(); }
        private void MovePart(int delta)
        { int target = selectedIndex + delta; if (SelectedPart == null || target < 0 || target >= parts.Count) return; TimerPart part = parts[selectedIndex]; parts.RemoveAt(selectedIndex); parts.Insert(target, part); selectedIndex = target; partsEdited = true; RebuildList(); Changed(); }
        private void ApplyPreset(TimerTimeFormat value)
        { parts.Clear(); parts.AddRange(TimerText.CreatePreset(value, draft)); draft.TimeFormat = value; selectedIndex = 0; partsEdited = true; RebuildList(); LoadPart(); Changed(); }
        private void EditStyle()
        {
            if (loading || SelectedPart == null) return; TimerPartStyle style = SelectedPart.Style;
            style.FontSize = (float)size.Value; style.Bold = bold.Checked; style.Italic = italic.Checked; style.Outline = outline.Checked; style.OutlineWidth = (float)border.Value;
            style.LetterSpacing = (float)spacing.Value; style.Opacity = (int)partOpacity.Value; partsEdited = true; border.Enabled = outlineColor.Enabled = outline.Checked; Changed();
        }
        private void ApplySelectedFont(Font selected)
        {
            if (SelectedPart == null) return; SelectedPart.Style.FontFamily = selected.FontFamily.Name; font.Text = selected.FontFamily.Name;
            loading = true; size.Value = Math.Max(size.Minimum, Math.Min(size.Maximum, (decimal)(selected.SizeInPoints * 96F / 72F))); bold.Checked = selected.Bold; italic.Checked = selected.Italic; loading = false; EditStyle();
        }
        private Button ColorButton(bool stroke)
        {
            Button button = ToolFields.Button(stroke ? T("描边颜色…", "Outline…") : T("文字颜色…", "Text color…"));
            button.Click += delegate { if (SelectedPart == null) return; TimerPartStyle style = SelectedPart.Style; using (ColorDialog picker = new ColorDialog { FullOpen = true, Color = CrosshairOptions.ParseColor(stroke ? style.OutlineColor : style.Color, "#FFFFFF") })
                if (picker.ShowDialog(this) == DialogResult.OK) { if (stroke) style.OutlineColor = CrosshairOptions.Hex(picker.Color); else style.Color = CrosshairOptions.Hex(picker.Color); partsEdited = true; Changed(); } }; return button;
        }
        private TimerOptions CaptureOptions()
        {
            TimerOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Visible = visible.Checked; o.Backend = (TimerBackend)backend.SelectedIndex;
            o.Opacity = (int)opacity.Value; o.X = (int)x.Value; o.Y = (int)y.Value; o.MonitorDevice = monitor.SelectedIndex <= 0 ? "" : (string)monitor.SelectedItem;
            if (partsEdited || draft.Parts != null)
            {
                o.Parts = new System.Collections.Generic.List<TimerPart>(); foreach (TimerPart part in parts) o.Parts.Add(part.Clone());
                System.Text.StringBuilder fallback = new System.Text.StringBuilder();
                foreach (TimerPart part in parts) if (part.Visible) fallback.Append(part.Kind == TimerPartKind.Text ? part.Text.Replace("{", "{{").Replace("}", "}}") : part.Kind == TimerPartKind.Hours ? "{hh}" : part.Kind == TimerPartKind.Minutes ? "{mm}" : part.Kind == TimerPartKind.Seconds ? "{ss}" : "{fff}");
                o.TextTemplate = fallback.ToString();
            }
            o.Normalize(); return o;
        }
        private void Changed()
        {
            if (save == null || loading) return; TimerOptions o = CaptureOptions();
            string error = !ToolFields.ValidShortcut(draft.StartPauseHotkey) || !ToolFields.ValidShortcut(draft.ResetHotkey) || !ToolFields.ValidShortcut(draft.VisibilityHotkey)
                ? T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.") : validate == null ? "" : validate(o);
            save.Enabled = error.Length == 0; feedback.ForeColor = error.Length == 0 ? EditorTheme.Muted : Color.Firebrick;
            monitor.Enabled = backend.SelectedIndex == 0; preview.Invalidate(); if (controller != null && error.Length == 0) controller.Apply(o);
            start.Text = controller != null && controller.IsRunning ? T("暂停", "Pause") : T("开始", "Start"); feedback.Text = error.Length > 0 ? error : controller == null ? "" : StatusText(controller.Status);
        }
        private void PaintPreview(object sender, PaintEventArgs e)
        {
            if (save == null) return;
            using (Bitmap image = TimerTextRenderer.Render(CaptureOptions(), TimeSpan.FromMilliseconds(123456)))
            { float scale = Math.Min(1F, Math.Min(Math.Max(1, preview.Width - 16) / (float)image.Width, Math.Max(1, preview.Height - 16) / (float)image.Height)); e.Graphics.DrawImage(image, new Rectangle(8, 8, Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)))); }
        }
        private Control PositionSlider(NumericUpDown number, int max, string name)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            TrackBar slider = new TrackBar { Minimum = 0, Maximum = max, TickStyle = TickStyle.None, LargeChange = 100, SmallChange = 10, Value = (int)Math.Max(0, Math.Min(max, number.Value)), Dock = DockStyle.Fill, Height = 30, Name = name };
            bool synchronizing = false;
            slider.ValueChanged += delegate { if (synchronizing) return; synchronizing = true; number.Value = slider.Value; synchronizing = false; };
            number.ValueChanged += delegate { if (synchronizing) return; synchronizing = true; slider.Value = (int)Math.Max(0, Math.Min(max, number.Value)); synchronizing = false; };
            row.Controls.Add(number, 0, 0); row.Controls.Add(slider, 1, 0); return row;
        }
        private static string StatusText(string status)
        {
            switch (status)
            {
                case "RtssNotRunning": case "RtssUnavailable": return T("请先启动 RivaTuner Statistics Server (RTSS)。", "Start RivaTuner Statistics Server (RTSS) first.");
                case "RtssUnsupported": return T("请使用 RTSS 7.3.7 或更新版本。", "Use RTSS 7.3.7 or newer.");
                case "RtssBusy": return T("RTSS 正忙；稍后重试。", "RTSS is busy. Try again shortly.");
                case "RtssNoSlot": return T("RTSS 叠加层已满，请关闭一个其他叠加层。", "RTSS has no free overlay slot. Close another overlay.");
                case "RtssAccessDenied": return T("无法连接 RTSS，请以相同权限运行两个程序。", "Cannot connect to RTSS. Run both apps at the same privilege level.");
                case "RtssTextTooLong": return T("文字过长，请缩短显示文字。", "Shorten the overlay text.");
                case "RtssPathEncoding": return T("RTSS 无法读取图片路径，请换用普通置顶窗口。", "RTSS cannot read the image path. Select the desktop overlay.");
                case "RtssInvalidMemory": case "RtssIoError": case "RtssRenderError": return T("RTSS 绘制失败，请重启 RTSS 后重试。", "RTSS output failed. Restart RTSS and try again.");
                default: return status ?? "";
            }
        }

    }

    internal sealed class NetworkSettingsDialog : Form
    {
        private readonly NetworkControlOptions draft;
        private readonly CheckBox enabled, blockSound, restoreSound;
        private readonly ComboBox mode;
        private readonly TextBox path;
        private readonly Button save, browse;
        private readonly Label feedback, shortcutHint;
        private readonly Func<NetworkControlOptions, string> validate;
        internal NetworkControlOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        internal NetworkSettingsDialog(NetworkControlOptions options, Func<NetworkControlOptions, string> validator)
        {
            draft = (options ?? new NetworkControlOptions()).Clone(); validate = validator;
            TriggerSpec oldBlock = draft.BlockShortcut, oldRestore = draft.RestoreShortcut;
            draft.BlockHotkey = oldBlock == null ? null : oldBlock.Clone(); draft.RestoreHotkey = oldRestore == null ? null : oldRestore.Clone(); draft.Hotkey = null;
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("断网与恢复", "Network control"); ClientSize = new Size(740, 570); MinimumSize = new Size(650, 460);
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
            Panel page = new Panel(); TableLayoutPanel fields = ToolFields.Page(page); page.Dock = DockStyle.Fill;
            enabled = ToolFields.Check(T("启用网络控制快捷键", "Enable network hotkeys")); enabled.Checked = draft.Enabled;
            ToolFields.Row(fields, "", enabled);
            mode = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left, Width = 220, Name = "networkMode" };
            mode.Items.AddRange(new object[] { T("指定程序", "Selected application"), T("整台电脑", "Whole computer") }); mode.SelectedIndex = draft.Mode == NetworkBlockMode.EntireComputer ? 1 : 0;
            ToolFields.Row(fields, T("断网范围", "Block scope"), mode);
            path = new TextBox { Text = draft.ExecutablePath, Dock = DockStyle.Top, Name = "networkExecutable" };
            browse = ToolFields.Button(T("选择程序…", "Choose app…")); browse.Click += delegate
            { using (OpenFileDialog picker = new OpenFileDialog { Filter = T("应用程序 (*.exe)|*.exe", "Applications (*.exe)|*.exe"), CheckFileExists = true }) if (picker.ShowDialog(this) == DialogResult.OK) path.Text = picker.FileName; };
            TableLayoutPanel app = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            app.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); app.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            app.Controls.Add(path, 0, 0); app.Controls.Add(browse, 1, 0); ToolFields.Row(fields, T("程序路径", "Application path"), app);
            ToolFields.Row(fields, T("断开网络", "Block traffic"), ToolFields.Shortcut(this, "networkBlockHotkey", () => draft.BlockHotkey, v => draft.BlockHotkey = v, Changed));
            ToolFields.Row(fields, T("恢复网络", "Restore traffic"), ToolFields.Shortcut(this, "networkRestoreHotkey", () => draft.RestoreHotkey, v => draft.RestoreHotkey = v, Changed));
            shortcutHint = ToolFields.Help(""); ToolFields.Row(fields, "", shortcutHint);
            blockSound = ToolFields.Check(T("断开音效", "Block sound")); blockSound.Name = "networkBlockSound"; blockSound.Checked = draft.BlockSound;
            restoreSound = ToolFields.Check(T("恢复音效", "Restore sound")); restoreSound.Name = "networkRestoreSound"; restoreSound.Checked = draft.RestoreSound;
            Button hearBlock = ToolFields.Button(T("试听断开", "Preview block")), hearRestore = ToolFields.Button(T("试听恢复", "Preview restore"));
            hearBlock.Name = "networkBlockSoundPreview"; hearRestore.Name = "networkRestoreSoundPreview";
            hearBlock.Click += delegate { NetworkSounds.Play(NetworkSoundKind.Blocked); }; hearRestore.Click += delegate { NetworkSounds.Play(NetworkSoundKind.Restored); };
            ToolFields.Row(fields, T("状态音效", "Sound cues"), ToolFields.Flow(blockSound, restoreSound, hearBlock, hearRestore));
            ToolFields.Row(fields, "", ToolFields.Help(T("指定程序包含同一路径的所有实例。首次断网需管理员授权。", "Application mode covers all instances at that path. First use requires administrator approval.")));
            ToolFields.Row(fields, "", ToolFields.Help(T("紧急停止、设置菜单中的“恢复网络”和退出程序都可以恢复连接。", "Emergency Stop, Restore network in Settings, and closing the app all restore traffic.")));
            feedback = ToolFields.Help(""); feedback.Name = "networkFeedback"; ToolFields.Row(fields, "", feedback);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "networkSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, page, EditorDialogs.Actions(save, cancel), T("选择范围、快捷键和音效", "Choose the scope, hotkeys and sound cues"), null);
            enabled.CheckedChanged += delegate { Changed(); }; blockSound.CheckedChanged += delegate { Changed(); }; restoreSound.CheckedChanged += delegate { Changed(); };
            mode.SelectedIndexChanged += delegate { Changed(); }; path.TextChanged += delegate { Changed(); }; Changed();
        }
        private NetworkControlOptions CaptureOptions()
        { NetworkControlOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Mode = mode.SelectedIndex == 1 ? NetworkBlockMode.EntireComputer : NetworkBlockMode.Executable; o.ExecutablePath = path.Text.Trim(); o.BlockSound = blockSound.Checked; o.RestoreSound = restoreSound.Checked; o.Normalize(); return o; }
        private void Changed()
        {
            if (save == null) return;
            NetworkControlOptions o = CaptureOptions(); path.Enabled = browse.Enabled = mode.SelectedIndex == 0;
            string error = o.Enabled && o.Mode == NetworkBlockMode.Executable && (!Path.IsPathRooted(o.ExecutablePath) || !File.Exists(o.ExecutablePath) || !string.Equals(Path.GetExtension(o.ExecutablePath), ".exe", StringComparison.OrdinalIgnoreCase)) ? T("请选择存在的程序文件。", "Choose an existing application file.") : "";
            if (error.Length == 0 && (!ToolFields.ValidShortcut(draft.BlockHotkey) || !ToolFields.ValidShortcut(draft.RestoreHotkey))) error = T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.");
            if (error.Length == 0 && validate != null) error = validate(o);
            feedback.Text = error; feedback.ForeColor = Color.Firebrick; save.Enabled = error.Length == 0;
            bool same = o.BlockShortcut != null && o.RestoreShortcut != null && InputNames.FormatTrigger(o.BlockShortcut) == InputNames.FormatTrigger(o.RestoreShortcut);
            shortcutHint.Text = same ? T("两个快捷键相同：按一次断开，再按一次恢复。", "Same shortcut: press once to block, again to restore.")
                : T("断开与恢复分别使用上面的快捷键。", "Use the separate shortcuts above to block and restore traffic.");
        }
    }
}
