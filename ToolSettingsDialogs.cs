using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace InputStitch
{
    internal static class ToolFields
    {
        internal static string T(string zh, string en) { return EditorDialogs.T(zh, en); }
        internal static TableLayoutPanel Page(ScrollableControl page)
        {
            page.BackColor = Color.White; page.Padding = new Padding(12); page.AutoScroll = true;
            TableLayoutPanel grid = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 0, Padding = new Padding(0, 0, 12, 12) }; grid.SuspendLayout();
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
        { FlowLayoutPanel flow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }; flow.SuspendLayout(); flow.Controls.AddRange(controls); return flow; }
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
            Button choose = Button(""), clear = Button(T("清除", "Clear")); choose.Name = name; clear.Name = name + "Clear";
            Action refresh = delegate { choose.Text = get() == null ? T("选择快捷键…", "Choose hotkey…") : InputNames.FormatTrigger(get()); clear.Enabled = get() != null; };
            choose.Click += delegate { set(PickHotkey(owner, get(), T("选择快捷键", "Choose hotkey"))); refresh(); changed(); };
            clear.Click += delegate { set(null); refresh(); changed(); };
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
        private readonly ComboBox backend, preset, partKind, monitor, controlMode;
        private readonly NumericUpDown size, border, spacing, opacity, partOpacity, digits, x, y, updateInterval, lineGap;
        private readonly TextBox partText;
        private readonly Button font, color, outlineColor, save, start, resetTimer, matchStyles;
        private Button addShortcutRule, editShortcutRule, deleteShortcutRule;
        private TimerOptions previewOptions;
        private Bitmap previewImage;
        private readonly Label feedback, selectionHelp, controlHelp;
        private readonly Control preview;
        private readonly ListView partList;
        private readonly TableLayoutPanel detail;
        private readonly TableLayoutPanel hotkeyFields;
        private readonly ListView ruleList;
        private readonly Control ruleEditor;
        private readonly int textRow, digitsRow;
        private int selectedIndex;
        private bool loading, partsEdited;
        internal TimerOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        private TimerPart SelectedPart { get { return selectedIndex >= 0 && selectedIndex < parts.Count ? parts[selectedIndex] : null; } }

        internal TimerSettingsDialog(TimerOptions options, TimerController timer, Func<TimerOptions, string> validator)
        {
            SuspendLayout(); draft = (options ?? new TimerOptions()).Clone(); draft.Normalize(); controller = timer; validate = validator;
            parts = TimerText.CreateParts(draft);
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("屏幕计时器", "Screen timer"); StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(920, 700); MinimumSize = new Size(800, 560); MinimizeBox = MaximizeBox = false;
            EditorTabs tabs = new EditorTabs { Dock = DockStyle.Fill };
            TabPage content = new TabPage(T("内容与样式", "Content & style")), position = new TabPage(T("显示与位置", "Output & position")), keys = new TabPage(T("快捷键", "Hotkeys"));
            tabs.TabPages.AddRange(new[] { content, position, keys });
            TableLayoutPanel place = ToolFields.Page(position); hotkeyFields = ToolFields.Page(keys);
            enabled = ToolFields.Check(T("启用计时器", "Enable timer")); enabled.Checked = draft.Enabled;
            visible = ToolFields.Check(T("显示计时器", "Show timer")); visible.Checked = draft.Visible;
            start = ToolFields.Button(T("开始 / 暂停", "Start / pause")); start.Name = "timerStartPause";
            start.Click += delegate { if (controller != null) controller.StartPause(); Changed(); };
            resetTimer = ToolFields.Button(T("归零", "Reset")); resetTimer.Click += delegate { if (controller != null) controller.Reset(); Changed(); };

            TableLayoutPanel editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) }; editor.SuspendLayout();
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 94)); content.Controls.Add(editor);
            TableLayoutPanel listPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0, 0, 12, 0) }; listPanel.SuspendLayout();
            listPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); listPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            preset = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, Name = "timerPreset" };
            preset.Items.AddRange(new object[] { T("选择预设…", "Choose preset…"), T("分:秒.毫秒", "Minutes:seconds.ms"), T("时:分:秒.毫秒", "Hours:minutes:seconds.ms"), T("秒.毫秒", "Seconds.ms") }); preset.SelectedIndex = 0;
            listPanel.Controls.Add(ToolFields.Flow(new Label { Text = T("预设", "Preset"), AutoSize = true, Anchor = AnchorStyles.Left }, preset), 0, 0);
            partList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = true, CheckBoxes = true, Name = "timerParts" };
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
            Button selectAll = ToolFields.Button(T("全选", "Select all")), unify = ToolFields.Button(T("套用首项样式", "Use first style")), newline = ToolFields.Button(T("换行", "New line"));
            orderTips.SetToolTip(unify, T("复制第一个选中部分的完整格式。", "Copy the first selected part's complete style."));
            matchStyles = unify; selectAll.Name = "timerSelectAll"; unify.Name = "timerMatchStyles"; newline.Name = "timerAddLineBreak";
            selectAll.Click += delegate { SelectAllParts(); }; unify.Click += delegate { UnifySelectedStyles(); }; newline.Click += delegate { AddTextPart("\n"); };
            listPanel.Controls.Add(ToolFields.Flow(add, remove, up, down, selectAll, unify, newline), 0, 2);
            selectionHelp = ToolFields.Help(""); listPanel.Controls.Add(selectionHelp, 0, 3);
            editor.Controls.Add(listPanel, 0, 0);
            Panel stylePanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty }; stylePanel.SuspendLayout();
            detail = ToolFields.Page(stylePanel); editor.Controls.Add(stylePanel, 1, 0);
            partKind = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 165, Name = "timerPartKind" };
            partKind.Items.AddRange(new object[] { T("时", "Hours"), T("分", "Minutes"), T("秒", "Seconds"), T("毫秒", "Milliseconds"), T("自定义文字", "Custom text") });
            ToolFields.Row(detail, T("内容类型", "Part type"), partKind);
            partText = new TextBox { Multiline = true, AcceptsReturn = true, Height = 45, Dock = DockStyle.Top, MaxLength = 512, ScrollBars = ScrollBars.Vertical, Name = "timerPartText" };
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
            preview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 40, 52), Name = "timerPreview", Margin = new Padding(0, 8, 0, 0) }; preview.SuspendLayout();
            preview.Paint += PaintPreview; editor.Controls.Add(preview, 0, 1); editor.SetColumnSpan(preview, 2);

            backend = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Name = "timerBackend" };
            backend.Items.AddRange(new object[] { T("普通置顶窗口", "Desktop overlay"), T("RTSS 游戏内绘制", "RTSS in-game overlay") }); backend.SelectedIndex = (int)draft.Backend;
            ToolFields.Row(place, T("显示方式", "Output"), backend);
            ToolFields.Row(place, "", ToolFields.Help(T("普通置顶窗口不进入 OBS“游戏捕获”。录制 RTSS：先运行 RTSS 7.3.7+，在 OBS 开启“捕获第三方叠加层”，在 RTSS Setup 开启“Use Microsoft Detours API hooking”。", "Desktop overlays stay outside OBS Game Capture. To record RTSS, run RTSS 7.3.7+, enable Capture third-party overlays in OBS and Use Microsoft Detours API hooking in RTSS Setup.")));
            opacity = ToolFields.Number(0, 100, draft.Opacity, 0, "timerOpacity"); ToolFields.Row(place, T("整体不透明度（%）", "Overall opacity (%)"), opacity);
            updateInterval = ToolFields.Number(16, 5000, draft.UpdateIntervalMs, 0, "timerUpdateInterval");
            ToolFields.Row(place, T("刷新间隔（毫秒）", "Refresh interval (ms)"), ToolFields.Flow(updateInterval, ToolFields.Help(T("只影响显示频率，计时精度不变。", "Changes display frequency; elapsed time stays accurate."))));
            lineGap = ToolFields.Number(-64, 160, (decimal)draft.LineSpacing, 1, "timerLineSpacing"); ToolFields.Row(place, T("额外行距（像素）", "Extra line spacing (px)"), lineGap);
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
            controlMode = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380, Name = "timerControlMode" };
            controlMode.Items.AddRange(new object[] { T("三键独立", "Three independent keys"), T("单键循环", "One-key cycle"), T("双键：开始结束 + 暂停继续", "Two keys: start/finish + pause/resume"), T("双键：开始暂停 + 归零隐藏", "Two keys: start/pause + reset/hide"), T("自定义", "Custom") }); controlMode.SelectedIndex = (int)draft.ControlMode;
            ToolFields.Row(hotkeyFields, T("控制方式", "Control mode"), controlMode);
            controlHelp = ToolFields.Help(""); ToolFields.Row(hotkeyFields, "", controlHelp);
            ToolFields.Row(hotkeyFields, T("开始 / 暂停", "Start / pause"), ToolFields.Shortcut(this, "timerStartHotkey", () => draft.StartPauseHotkey, v => draft.StartPauseHotkey = v, Changed));
            ToolFields.Row(hotkeyFields, T("归零并暂停", "Reset & pause"), ToolFields.Shortcut(this, "timerResetHotkey", () => draft.ResetHotkey, v => draft.ResetHotkey = v, Changed));
            ToolFields.Row(hotkeyFields, T("显示 / 隐藏", "Show / hide"), ToolFields.Shortcut(this, "timerVisibilityHotkey", () => draft.VisibilityHotkey, v => draft.VisibilityHotkey = v, Changed));
            ruleList = new ListView { Height = 155, Dock = DockStyle.Top, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, Name = "timerShortcutRules" };
            ruleList.Columns.Add(T("快捷键", "Hotkey"), 150); ruleList.Columns.Add(T("未开始", "Ready"), 180); ruleList.Columns.Add(T("计时中", "Running"), 180); ruleList.Columns.Add(T("已暂停", "Paused"), 180);
            Button addRule = ToolFields.Button(T("添加快捷键", "Add hotkey")), editRule = ToolFields.Button(T("编辑", "Edit")), removeRule = ToolFields.Button(T("删除", "Delete"));
            addRule.Name = "timerAddRule"; editRule.Name = "timerEditRule"; removeRule.Name = "timerDeleteRule";
            addShortcutRule = addRule; editShortcutRule = editRule; deleteShortcutRule = removeRule;
            ruleList.SelectedIndexChanged += delegate { RefreshRuleActions(); };
            ruleList.Resize += delegate { FitRuleColumns(); };
            addRule.Click += delegate { EditRule(-1); }; editRule.Click += delegate { if (ruleList.SelectedIndices.Count > 0) EditRule(ruleList.SelectedIndices[0]); }; removeRule.Click += delegate { if (ruleList.SelectedIndices.Count > 0) { draft.ShortcutRules.RemoveAt(ruleList.SelectedIndices[0]); RefreshRules(); Changed(); } };
            ruleList.DoubleClick += delegate { if (ruleList.SelectedIndices.Count > 0) EditRule(ruleList.SelectedIndices[0]); };
            TableLayoutPanel rules = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2 }; rules.SuspendLayout(); rules.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); rules.Controls.Add(ruleList, 0, 0); rules.Controls.Add(ToolFields.Flow(addRule, editRule, removeRule), 0, 1); ruleEditor = rules;
            ToolFields.Row(hotkeyFields, "", ruleEditor);
            feedback = ToolFields.Help(""); feedback.Name = "timerFeedback";
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 }; body.SuspendLayout();
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.Controls.Add(ToolFields.Flow(enabled, visible, start, resetTimer), 0, 0); body.Controls.Add(tabs, 0, 1); body.Controls.Add(feedback, 0, 2);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "timerSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(save, cancel), T("分段编辑计时内容和样式", "Edit timer parts and their styles"), null);
            partList.Resize += delegate { FitColumns(); };
            partList.SelectedIndexChanged += delegate { if (!loading) { selectedIndex = partList.SelectedIndices.Count == 0 ? -1 : partList.SelectedIndices[0]; LoadPart(); } };
            partList.ItemChecked += delegate(object sender, ItemCheckedEventArgs e) { if (!loading) { parts[e.Item.Index].Visible = e.Item.Checked; partsEdited = true; Changed(); } };
            partList.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) { RemovePart(); e.Handled = true; } else if (e.Alt && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)) { MovePart(e.KeyCode == Keys.Up ? -1 : 1); e.Handled = true; } };
            preset.SelectedIndexChanged += delegate { if (preset.SelectedIndex > 0) ApplyPreset((TimerTimeFormat)(preset.SelectedIndex - 1)); };
            partKind.SelectedIndexChanged += delegate { if (loading || SelectedPart == null) return; SelectedPart.Kind = (TimerPartKind)partKind.SelectedIndex; partsEdited = true; LoadPart(); RebuildList(); Changed(); };
            partText.TextChanged += delegate { if (!loading && SelectedPart != null) { SelectedPart.Text = partText.Text.Replace("\r\n", "\n"); partsEdited = true; UpdateItem(); Changed(); } };
            digits.ValueChanged += delegate { if (!loading && SelectedPart != null) { SelectedPart.Digits = (int)digits.Value; partsEdited = true; UpdateItem(); Changed(); } };
            bold.CheckedChanged += delegate { ApplyStyle(v => v.Bold = bold.Checked); }; italic.CheckedChanged += delegate { ApplyStyle(v => v.Italic = italic.Checked); }; outline.CheckedChanged += delegate { ApplyStyle(v => v.Outline = outline.Checked); };
            size.ValueChanged += delegate { ApplyStyle(v => v.FontSize = (float)size.Value); }; border.ValueChanged += delegate { ApplyStyle(v => v.OutlineWidth = (float)border.Value); }; spacing.ValueChanged += delegate { ApplyStyle(v => v.LetterSpacing = (float)spacing.Value); }; partOpacity.ValueChanged += delegate { ApplyStyle(v => v.Opacity = (int)partOpacity.Value); };
            foreach (CheckBox control in new[] { enabled, visible }) control.CheckedChanged += delegate { Changed(); };
            foreach (NumericUpDown control in new[] { opacity, x, y, updateInterval, lineGap }) control.ValueChanged += delegate { Changed(); };
            foreach (ComboBox control in new[] { backend, monitor }) control.SelectedIndexChanged += delegate { Changed(); };
            controlMode.SelectedIndexChanged += delegate { ChangeControlMode(); };
            selectedIndex = parts.Count == 0 ? -1 : 0; RebuildList(); LoadPart(); RefreshRules(); RefreshControlMode(); Changed();
            EditorDialogs.ResumeChildLayouts(this); ResumeLayout(true);
        }
        private void FitColumns()
        { if (partList.Columns.Count < 2) return; int first = Math.Min(partList.ClientSize.Width / 2, (int)Math.Ceiling(145 * partList.Font.Height / 16F)); partList.Columns[0].Width = Math.Max(70, first); partList.Columns[1].Width = Math.Max(40, partList.ClientSize.Width - first - SystemInformation.VerticalScrollBarWidth - 4); }
        private static string PartName(TimerPart part)
        { switch (part.Kind) { case TimerPartKind.Hours: return T("时", "Hours"); case TimerPartKind.Minutes: return T("分", "Minutes"); case TimerPartKind.Seconds: return T("秒", "Seconds"); case TimerPartKind.Milliseconds: return T("毫秒", "Milliseconds"); default: return part.Text == "\n" ? T("换行", "Line break") : T("文字", "Text"); } }
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
            int count = StyleIndices().Count; matchStyles.Enabled = count > 1;
            selectionHelp.Text = count > 1 ? T("已选 ", "Selected ") + count + T(" 项，右侧改动会批量应用；套用样式以首项为准。", " parts. Right-side edits apply to all; matching uses the first part.") : T("勾选控制显示，Ctrl / Shift 多选可批量改格式。", "Check to show. Ctrl / Shift selects parts for batch formatting.");
            partKind.Enabled = partText.Enabled = digits.Enabled = count == 1;
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
        { AddTextPart(T("文字", "Text")); }
        private void AddTextPart(string text)
        {
            if (parts.Count >= 32) return;
            TimerPartStyle style = SelectedPart == null ? TimerPartStyle.FromOptions(draft) : SelectedPart.Style.Clone(); style.Opacity = 100;
            parts.Add(new TimerPart { Kind = TimerPartKind.Text, Text = text, Style = style }); selectedIndex = parts.Count - 1; partsEdited = true; RebuildList(); LoadPart(); Changed();
        }
        private void RemovePart() { List<int> chosen = StyleIndices(); if (chosen.Count == 0) return; for (int i = chosen.Count - 1; i >= 0; i--) parts.RemoveAt(chosen[i]); selectedIndex = Math.Min(chosen[0], parts.Count - 1); partsEdited = true; RebuildList(); LoadPart(); Changed(); }
        private void MovePart(int delta)
        {
            List<int> chosen = StyleIndices(); if (chosen.Count == 0) return;
            bool[] selected = new bool[parts.Count]; foreach (int index in chosen) selected[index] = true;
            TimerPart lead = SelectedPart; bool moved = false;
            for (int i = delta < 0 ? 1 : parts.Count - 2; delta < 0 ? i < parts.Count : i >= 0; i += delta < 0 ? 1 : -1)
            {
                int target = i + (delta < 0 ? -1 : 1); if (!selected[i] || selected[target]) continue;
                TimerPart swap = parts[target]; parts[target] = parts[i]; parts[i] = swap;
                selected[target] = true; selected[i] = false; moved = true;
            }
            if (!moved) return; selectedIndex = parts.IndexOf(lead); partsEdited = true; RebuildList();
            loading = true; for (int i = 0; i < selected.Length; i++) partList.Items[i].Selected = selected[i]; loading = false; LoadPart(); Changed();
        }
        private void ApplyPreset(TimerTimeFormat value)
        { parts.Clear(); parts.AddRange(TimerText.CreatePreset(value, draft)); draft.TimeFormat = value; selectedIndex = 0; partsEdited = true; RebuildList(); LoadPart(); Changed(); }
        private List<int> StyleIndices()
        {
            List<int> selected = new List<int>(); foreach (int index in partList.SelectedIndices) selected.Add(index);
            if (selected.Count == 0 && SelectedPart != null) selected.Add(selectedIndex); return selected;
        }
        private void SelectAllParts()
        {
            IntPtr handle = partList.Handle; loading = true;
            foreach (ListViewItem item in partList.Items) item.Selected = true;
            selectedIndex = parts.Count > 0 ? 0 : -1; loading = false; LoadPart();
        }
        private void UnifySelectedStyles()
        {
            if (SelectedPart == null) return; TimerPartStyle source = SelectedPart.Style.Clone();
            foreach (int index in StyleIndices()) parts[index].Style = source.Clone(); partsEdited = true; LoadPart(); Changed();
        }
        private void ApplyStyle(Action<TimerPartStyle> change)
        {
            if (loading || SelectedPart == null) return;
            foreach (int index in StyleIndices()) change(parts[index].Style);
            partsEdited = true; LoadPart(); Changed();
        }
        private void ApplySelectedFont(Font selected)
        {
            ApplyStyle(delegate(TimerPartStyle style) { style.FontFamily = selected.FontFamily.Name; style.FontSize = Math.Max(8, Math.Min(160, selected.SizeInPoints * 96F / 72F)); style.Bold = selected.Bold; style.Italic = selected.Italic; });
        }
        private Button ColorButton(bool stroke)
        {
            Button button = ToolFields.Button(stroke ? T("描边颜色…", "Outline…") : T("文字颜色…", "Text color…"));
            button.Click += delegate { if (SelectedPart == null) return; TimerPartStyle style = SelectedPart.Style; using (ColorDialog picker = new ColorDialog { FullOpen = true, Color = CrosshairOptions.ParseColor(stroke ? style.OutlineColor : style.Color, "#FFFFFF") })
                if (picker.ShowDialog(this) == DialogResult.OK) ApplyStyle(delegate(TimerPartStyle target) { if (stroke) target.OutlineColor = CrosshairOptions.Hex(picker.Color); else target.Color = CrosshairOptions.Hex(picker.Color); }); }; return button;
        }
        private TimerOptions CaptureOptions()
        {
            TimerOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Visible = visible.Checked; o.Backend = (TimerBackend)backend.SelectedIndex;
            o.Opacity = (int)opacity.Value; o.X = (int)x.Value; o.Y = (int)y.Value; o.MonitorDevice = monitor.SelectedIndex <= 0 ? "" : (string)monitor.SelectedItem;
            o.UpdateIntervalMs = (int)updateInterval.Value; o.LineSpacing = (float)lineGap.Value; o.ControlMode = (TimerControlMode)controlMode.SelectedIndex;
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
            string error = ""; foreach (TriggerSpec key in TimerControls.ActiveKeys(draft)) if (!ToolFields.ValidShortcut(key)) error = T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.");
            if (error.Length == 0 && validate != null) error = validate(o);
            save.Enabled = error.Length == 0; feedback.ForeColor = error.Length == 0 ? EditorTheme.Muted : Color.Firebrick;
            monitor.Enabled = backend.SelectedIndex == 0; preview.Invalidate(); if (controller != null && error.Length == 0) controller.Apply(o);
            start.Enabled = resetTimer.Enabled = o.Enabled && controller != null && error.Length == 0; visible.Enabled = o.Enabled;
            start.Text = controller != null && controller.IsRunning ? T("暂停", "Pause") : T("开始", "Start");
            feedback.Text = error.Length > 0 ? error : !o.Enabled ? T("勾选“启用计时器”，然后点击开始或设置快捷键。", "Enable the timer, then click Start or set a hotkey.") : controller == null ? "" : StatusText(controller.Status);
        }
        private void PaintPreview(object sender, PaintEventArgs e)
        {
            if (save == null) return;
            Bitmap image = GetPreviewImage();
            float scale = Math.Min(1F, Math.Min(Math.Max(1, preview.Width - 16) / (float)image.Width, Math.Max(1, preview.Height - 16) / (float)image.Height)); e.Graphics.DrawImage(image, new Rectangle(8, 8, Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale))));
        }
        private Bitmap GetPreviewImage()
        {
            TimerOptions next = CaptureOptions();
            if (previewImage == null || !next.SameRendering(previewOptions))
            {
                Bitmap image = TimerTextRenderer.Render(next, TimeSpan.FromMilliseconds(123456));
                if (previewImage != null) previewImage.Dispose(); previewImage = image; previewOptions = next;
            }
            return previewImage;
        }
        protected override void Dispose(bool disposing)
        { if (disposing && previewImage != null) { previewImage.Dispose(); previewImage = null; } base.Dispose(disposing); }
        private void ChangeControlMode()
        {
            TimerControlMode next = (TimerControlMode)controlMode.SelectedIndex;
            if (next == TimerControlMode.Custom && draft.ShortcutRules == null)
            {
                List<TimerShortcutRule> rules = new List<TimerShortcutRule>(); TriggerSpec[] keys = TimerControls.ActiveKeys(draft); int key = 0;
                foreach (TriggerSpec shortcut in keys)
                {
                    key++; if (shortcut == null) continue;
                    rules.Add(new TimerShortcutRule { Hotkey = shortcut.Clone(), Ready = TimerControls.Resolve(draft, key, TimerRunState.Ready), Running = TimerControls.Resolve(draft, key, TimerRunState.Running), Paused = TimerControls.Resolve(draft, key, TimerRunState.Paused) });
                }
                if (rules.Count > 0) draft.ShortcutRules = rules;
            }
            draft.ControlMode = next; RefreshRules(); RefreshControlMode(); Changed();
        }
        private void RefreshRuleActions()
        {
            if (addShortcutRule == null) return;
            addShortcutRule.Enabled = draft.ShortcutRules == null || draft.ShortcutRules.Count < 16;
            editShortcutRule.Enabled = deleteShortcutRule.Enabled = ruleList.SelectedIndices.Count > 0;
        }
        private void RefreshControlMode()
        {
            TimerControlMode mode = (TimerControlMode)controlMode.SelectedIndex; bool custom = mode == TimerControlMode.Custom;
            EditorDialogs.RowVisible(hotkeyFields, 2, !custom); EditorDialogs.RowVisible(hotkeyFields, 3, !custom && mode != TimerControlMode.SingleKeyCycle); EditorDialogs.RowVisible(hotkeyFields, 4, mode == TimerControlMode.Independent); EditorDialogs.RowVisible(hotkeyFields, 5, custom);
            hotkeyFields.GetControlFromPosition(0, 2).Text = mode == TimerControlMode.SingleKeyCycle ? T("控制键", "Control key") : mode == TimerControlMode.StartFinishPause ? T("开始 / 结束", "Start / finish") : T("开始 / 暂停", "Start / pause");
            hotkeyFields.GetControlFromPosition(0, 3).Text = mode == TimerControlMode.StartFinishPause ? T("暂停 / 继续", "Pause / resume") : mode == TimerControlMode.StartPauseReset ? T("归零并隐藏", "Reset & hide") : T("归零并暂停", "Reset & pause");
            controlHelp.Text = custom ? (draft.ShortcutRules == null || draft.ShortcutRules.Count == 0 ? T("点击“添加快捷键”，分别设置未开始、计时中和暂停时的动作。", "Click Add hotkey to choose actions for ready, running and paused states.") : T("选中一行后编辑，可分别设置三个计时状态的动作。", "Select a row to edit its actions for each timer state.")) : mode == TimerControlMode.SingleKeyCycle ? T("依次按：开始并显示 → 暂停 → 归零并隐藏。", "Press to cycle: start & show → pause → reset & hide.") : mode == TimerControlMode.StartFinishPause ? T("第一键开始，或结束并归零隐藏；第二键暂停或继续。", "First key starts, or finishes, resets and hides. Second key pauses or resumes.") : mode == TimerControlMode.StartPauseReset ? T("第一键开始或暂停并显示；第二键归零并隐藏。", "First key starts or pauses and shows. Second key resets and hides.") : T("开始暂停、归零、显隐各自使用一个快捷键。", "Separate shortcuts for start/pause, reset and show/hide.");
        }
        private void RefreshRules()
        {
            int selected = ruleList.SelectedIndices.Count > 0 ? ruleList.SelectedIndices[0] : 0; ruleList.Items.Clear(); if (draft.ShortcutRules == null) { FitRuleColumns(); RefreshRuleActions(); return; }
            foreach (TimerShortcutRule rule in draft.ShortcutRules)
            {
                ListViewItem item = new ListViewItem(rule.Hotkey == null ? T("选择快捷键…", "Choose hotkey…") : InputNames.FormatTrigger(rule.Hotkey));
                item.SubItems.Add(TimerShortcutDialog.Describe(rule.Ready)); item.SubItems.Add(TimerShortcutDialog.Describe(rule.Running)); item.SubItems.Add(TimerShortcutDialog.Describe(rule.Paused));
                item.ToolTipText = string.Join(" | ", new[] { item.SubItems[1].Text, item.SubItems[2].Text, item.SubItems[3].Text }); ruleList.Items.Add(item);
            }
            ruleList.ShowItemToolTips = true; if (ruleList.Items.Count > 0) ruleList.Items[Math.Min(selected, ruleList.Items.Count - 1)].Selected = true; FitRuleColumns(); RefreshRuleActions();
        }
        private void FitRuleColumns()
        {
            if (ruleList.Columns.Count < 4) return;
            float scale = ruleList.Font.Height / 16F; int first = (int)Math.Ceiling(100 * scale);
            foreach (ListViewItem item in ruleList.Items) first = Math.Max(first, TextRenderer.MeasureText(item.Text, ruleList.Font).Width + (int)Math.Ceiling(20 * scale));
            first = Math.Min(first, Math.Max(1, ruleList.ClientSize.Width / 3)); ruleList.Columns[0].Width = first;
            for (int i = 1; i < 4; i++) ruleList.Columns[i].Width = Math.Max(1, (ruleList.ClientSize.Width - first - SystemInformation.VerticalScrollBarWidth - 5) / 3);
        }
        private void EditRule(int index)
        {
            if (index < 0 && draft.ShortcutRules != null && draft.ShortcutRules.Count >= 16) return;
            TimerShortcutRule initial = index < 0 ? new TimerShortcutRule { Ready = new TimerControlStep { Run = TimerRunCommand.Start, Visibility = TimerVisibilityCommand.Show }, Running = new TimerControlStep { Run = TimerRunCommand.Pause }, Paused = new TimerControlStep { Reset = true, Visibility = TimerVisibilityCommand.Hide } } : draft.ShortcutRules[index];
            using (TimerShortcutDialog dialog = new TimerShortcutDialog(initial, delegate(TriggerSpec key)
            {
                TimerOptions candidate = CaptureOptions(); candidate.ControlMode = TimerControlMode.Custom;
                if (candidate.ShortcutRules == null) candidate.ShortcutRules = new List<TimerShortcutRule>();
                TimerShortcutRule trial = initial.Clone(); trial.Hotkey = key;
                if (index < 0) candidate.ShortcutRules.Add(trial); else candidate.ShortcutRules[index] = trial;
                return validate == null ? "" : validate(candidate);
            }))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                { if (draft.ShortcutRules == null) draft.ShortcutRules = new List<TimerShortcutRule>(); if (index < 0) draft.ShortcutRules.Add(dialog.Result); else draft.ShortcutRules[index] = dialog.Result; RefreshRules(); Changed(); }
        }
        private Control PositionSlider(NumericUpDown number, int max, string name)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 }; row.SuspendLayout();
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

    internal sealed class TimerShortcutDialog : Form
    {
        private readonly TimerShortcutRule draft;
        private readonly ComboBox[] run = new ComboBox[3], visibility = new ComboBox[3];
        private readonly CheckBox[] reset = new CheckBox[3];
        private readonly Func<TriggerSpec, string> validate;
        private readonly Button save;
        private readonly Label feedback;
        internal TimerShortcutRule Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        internal TimerShortcutDialog(TimerShortcutRule initial, Func<TriggerSpec, string> validator)
        {
            SuspendLayout(); draft = (initial ?? new TimerShortcutRule()).Clone(); validate = validator;
            Text = T("自定义计时快捷键", "Custom timer hotkey"); Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(780, 390); MinimumSize = new Size(700, 350); StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
            Panel page = new Panel(); page.SuspendLayout(); TableLayoutPanel fields = ToolFields.Page(page);
            ToolFields.Row(fields, T("快捷键", "Hotkey"), ToolFields.Shortcut(this, "timerRuleHotkey", () => draft.Hotkey, key => draft.Hotkey = key, Changed));
            TableLayoutPanel states = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 4, RowCount = 4 }; states.SuspendLayout();
            states.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); states.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); states.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); states.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            string[] headers = { T("计时状态", "Timer state"), T("计时动作", "Timer action"), T("先归零", "Reset first"), T("显示动作", "Display action") };
            for (int i = 0; i < 4; i++) states.Controls.Add(new Label { Text = headers[i], AutoSize = true, Margin = new Padding(4, 4, 12, 8) }, i, 0);
            TimerControlStep[] steps = { draft.Ready, draft.Running, draft.Paused }; string[] names = { T("未开始", "Ready"), T("计时中", "Running"), T("已暂停", "Paused") };
            for (int i = 0; i < 3; i++)
            {
                states.Controls.Add(new Label { Text = names[i], AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 6, 12, 6) }, 0, i + 1);
                run[i] = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Name = "timerRuleRun" + i };
                run[i].Items.AddRange(new object[] { T("保持", "Keep"), T("开始 / 继续", "Start / resume"), T("暂停", "Pause"), T("开始 / 暂停切换", "Toggle start/pause") }); run[i].SelectedIndex = (int)steps[i].Run;
                reset[i] = ToolFields.Check(""); reset[i].Name = "timerRuleReset" + i; reset[i].Checked = steps[i].Reset;
                visibility[i] = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Name = "timerRuleVisibility" + i };
                visibility[i].Items.AddRange(new object[] { T("保持", "Keep"), T("显示", "Show"), T("隐藏", "Hide"), T("显隐切换", "Toggle visibility") }); visibility[i].SelectedIndex = (int)steps[i].Visibility;
                states.Controls.Add(run[i], 1, i + 1); states.Controls.Add(reset[i], 2, i + 1); states.Controls.Add(visibility[i], 3, i + 1);
            }
            ToolFields.Row(fields, "", states); feedback = ToolFields.Help(""); ToolFields.Row(fields, "", feedback);
            save = EditorDialogs.Button(T("确定", "OK"), DialogResult.None); Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureRule(); DialogResult = DialogResult.OK; Close(); };
            EditorDialogs.Frame(this, page, EditorDialogs.Actions(save, cancel), T("按当前状态执行：先归零，再处理计时，最后处理显示。", "For the current state: reset first, then apply timing and display actions."), null); Changed();
            EditorDialogs.ResumeChildLayouts(this); ResumeLayout(true);
        }
        private TimerShortcutRule CaptureRule()
        {
            TimerShortcutRule result = draft.Clone(); TimerControlStep[] steps = { result.Ready, result.Running, result.Paused };
            for (int i = 0; i < 3; i++) { steps[i].Run = (TimerRunCommand)run[i].SelectedIndex; steps[i].Reset = reset[i].Checked; steps[i].Visibility = (TimerVisibilityCommand)visibility[i].SelectedIndex; } return result;
        }
        private void Changed()
        { if (save == null) return; string error = draft.Hotkey == null || !ToolFields.ValidShortcut(draft.Hotkey) ? T("请选择快捷键。", "Choose a hotkey.") : validate == null ? "" : validate(draft.Hotkey); feedback.Text = error; feedback.ForeColor = Color.Firebrick; save.Enabled = error.Length == 0; }
        internal static string Describe(TimerControlStep step)
        {
            List<string> text = new List<string>(); if (step.Reset) text.Add(T("归零", "Reset"));
            if (step.Run != TimerRunCommand.Keep) text.Add(step.Run == TimerRunCommand.Start ? T("开始", "Start") : step.Run == TimerRunCommand.Pause ? T("暂停", "Pause") : T("开始/暂停", "Start/pause"));
            if (step.Visibility != TimerVisibilityCommand.Keep) text.Add(step.Visibility == TimerVisibilityCommand.Show ? T("显示", "Show") : step.Visibility == TimerVisibilityCommand.Hide ? T("隐藏", "Hide") : T("显隐切换", "Show/hide"));
            return text.Count == 0 ? T("保持", "Keep") : string.Join(" + ", text.ToArray());
        }
    }

}
