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
        private readonly CheckBox enabled, visible, bold, italic, outline;
        private readonly ComboBox backend, format, monitor;
        private readonly NumericUpDown size, border, spacing, opacity, x, y;
        private readonly TextBox template;
        private readonly Button font, color, outlineColor, save, start;
        private readonly Label feedback;
        private readonly Control preview;
        internal TimerOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }

        internal TimerSettingsDialog(TimerOptions options, TimerController timer, Func<TimerOptions, string> validator)
        {
            draft = (options ?? new TimerOptions()).Clone(); draft.Normalize(); controller = timer; validate = validator;
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("屏幕计时器", "Screen timer"); StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 650); MinimumSize = new Size(700, 530); MinimizeBox = MaximizeBox = false;
            EditorTabs tabs = new EditorTabs { Dock = DockStyle.Fill };
            TabPage appearance = new TabPage(T("外观与文字", "Appearance & text")), position = new TabPage(T("位置", "Position")), keys = new TabPage(T("控制与快捷键", "Controls & hotkeys"));
            tabs.TabPages.AddRange(new[] { appearance, position, keys });
            TableLayoutPanel look = ToolFields.Page(appearance), place = ToolFields.Page(position), hotkeys = ToolFields.Page(keys);
            enabled = ToolFields.Check(T("启用计时器", "Enable timer")); enabled.Checked = draft.Enabled;
            visible = ToolFields.Check(T("显示计时器", "Show timer")); visible.Checked = draft.Visible;
            start = ToolFields.Button(T("开始 / 暂停", "Start / pause")); start.Name = "timerStartPause";
            Button reset = ToolFields.Button(T("归零", "Reset")); reset.Name = "timerReset";
            start.Click += delegate { if (controller != null) { enabled.Checked = visible.Checked = true; controller.StartPause(); Changed(); } };
            reset.Click += delegate { if (controller != null) { controller.Reset(); Changed(); } };
            ToolFields.Row(hotkeys, T("开始 / 暂停", "Start / pause"), ToolFields.Shortcut(this, "timerStartHotkey", () => draft.StartPauseHotkey, v => draft.StartPauseHotkey = v, Changed));
            ToolFields.Row(hotkeys, T("归零", "Reset"), ToolFields.Shortcut(this, "timerResetHotkey", () => draft.ResetHotkey, v => draft.ResetHotkey = v, Changed));
            ToolFields.Row(hotkeys, T("显示 / 隐藏", "Show / hide"), ToolFields.Shortcut(this, "timerVisibilityHotkey", () => draft.VisibilityHotkey, v => draft.VisibilityHotkey = v, Changed));
            ToolFields.Row(hotkeys, "", ToolFields.Help(T("启动软件时从零暂停；外观、文字、位置和快捷键会保存。归零保持当前运行或暂停状态。", "The timer starts paused at zero on launch. Appearance, text, position and hotkeys are saved. Reset keeps the current running or paused state.")));

            backend = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Name = "timerBackend" };
            backend.Items.AddRange(new object[] { T("普通置顶窗口", "Desktop overlay"), T("RTSS 游戏内绘制", "RTSS in-game overlay") }); backend.SelectedIndex = (int)draft.Backend;
            ToolFields.Row(look, T("显示方式", "Output"), backend);
            ToolFields.Row(look, "", ToolFields.Help(T("普通窗口穿透鼠标，不进入 OBS 的“游戏捕获”。RTSS 7.3.7+ 需已安装并运行；OBS 请开启“捕获第三方叠加层”，录制结果取决于游戏兼容性。", "The desktop overlay passes clicks through and stays outside OBS Game Capture. RTSS 7.3.7+ must be installed and running. In OBS, enable Capture third-party overlays; capture depends on game compatibility.")));
            font = ToolFields.Button(draft.FontFamily); font.Name = "timerFont";
            font.Click += delegate
            {
                using (FontDialog picker = new FontDialog { Font = new Font(draft.FontFamily, Math.Max(6, (float)size.Value), FontStyle.Regular, GraphicsUnit.Pixel), FontMustExist = true, ShowEffects = false })
                    if (picker.ShowDialog(this) == DialogResult.OK) { draft.FontFamily = picker.Font.FontFamily.Name; font.Text = draft.FontFamily; Changed(); }
            };
            ToolFields.Row(look, T("字体", "Font"), font);
            size = ToolFields.Number(8, 160, (decimal)draft.FontSize, 1, "timerFontSize"); ToolFields.Row(look, T("字号（像素）", "Font size (px)"), size);
            bold = ToolFields.Check(T("加粗", "Bold")); bold.Checked = draft.Bold;
            italic = ToolFields.Check(T("斜体", "Italic")); italic.Checked = draft.Italic;
            ToolFields.Row(look, T("字形", "Style"), ToolFields.Flow(bold, italic));
            outline = ToolFields.Check(T("描边", "Outline")); outline.Checked = draft.Outline;
            border = ToolFields.Number(0, 12, (decimal)draft.OutlineWidth, 1, "timerOutlineWidth"); ToolFields.Row(look, T("描边厚度（像素）", "Outline width (px)"), ToolFields.Flow(outline, border));
            spacing = ToolFields.Number(-4, 40, (decimal)draft.LetterSpacing, 1, "timerLetterSpacing"); ToolFields.Row(look, T("字距（像素）", "Letter spacing (px)"), spacing);
            color = ColorButton(false); outlineColor = ColorButton(true); ToolFields.Row(look, T("颜色", "Colors"), ToolFields.Flow(color, outlineColor));
            opacity = ToolFields.Number(0, 100, draft.Opacity, 0, "timerOpacity"); ToolFields.Row(look, T("不透明度（%）", "Opacity (%)"), opacity);
            format = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Name = "timerFormat" };
            format.Items.AddRange(new object[] { "00:00.000", "00:00:00.000", "0.000" }); format.SelectedIndex = (int)draft.TimeFormat;
            ToolFields.Row(look, T("时间格式", "Time format"), format);
            template = new TextBox { Text = draft.TextTemplate, Multiline = true, Height = 64, Dock = DockStyle.Top, MaxLength = 512, ScrollBars = ScrollBars.Vertical, Name = "timerTemplate" };
            ToolFields.Row(look, T("显示文字", "Text"), template);
            ToolFields.Row(look, "", ToolFields.Help(T("{time} 代表当前时间；支持中英文和换行，例如：本次用时 {time}。", "Use {time} for elapsed time. Add text in any language or multiple lines, for example: Run time {time}.")));
            preview = new Panel { Height = 84, Dock = DockStyle.Top, BackColor = Color.FromArgb(32, 40, 52), Name = "timerPreview" };
            preview.Paint += PaintPreview; ToolFields.Row(look, "", preview);

            monitor = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Name = "timerMonitor" };
            monitor.Items.Add(T("主显示器", "Primary display")); foreach (Screen screen in Screen.AllScreens) monitor.Items.Add(screen.DeviceName);
            if (draft.MonitorDevice.Length > 0 && !monitor.Items.Contains(draft.MonitorDevice)) monitor.Items.Add(draft.MonitorDevice);
            monitor.SelectedIndex = 0; for (int i = 1; i < monitor.Items.Count; i++) if ((string)monitor.Items[i] == draft.MonitorDevice) monitor.SelectedIndex = i;
            ToolFields.Row(place, T("显示器（普通窗口）", "Display (desktop)"), monitor);
            x = ToolFields.Number(-16384, 16384, draft.X, 0, "timerX"); y = ToolFields.Number(-16384, 16384, draft.Y, 0, "timerY");
            ToolFields.Row(place, T("水平位置（像素）", "X position (px)"), PositionSlider(x, 7680, "timerXSlider"));
            ToolFields.Row(place, T("垂直位置（像素）", "Y position (px)"), PositionSlider(y, 4320, "timerYSlider"));
            ToolFields.Row(place, "", ToolFields.Help(T("以左上角为原点；滑块用于粗调，数字框每次微调 1 像素。RTSS 位置以游戏画面为准。", "Positions start at the top-left. Use sliders for coarse adjustment and numbers for one-pixel changes. RTSS coordinates refer to the game frame.")));
            Button home = ToolFields.Button(T("回到左上角", "Reset position")); home.Click += delegate { x.Value = y.Value = 24; }; ToolFields.Row(place, "", home);
            feedback = ToolFields.Help(""); feedback.Name = "timerFeedback";
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.Controls.Add(ToolFields.Flow(enabled, visible, start, reset), 0, 0); body.Controls.Add(tabs, 0, 1); body.Controls.Add(feedback, 0, 2);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "timerSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(save, cancel), T("计时、外观和位置", "Stopwatch, appearance and position"), null);
            foreach (CheckBox control in new[] { enabled, visible, bold, italic, outline }) control.CheckedChanged += delegate { Changed(); };
            foreach (NumericUpDown control in new[] { size, border, spacing, opacity, x, y }) control.ValueChanged += delegate { Changed(); };
            foreach (ComboBox control in new[] { backend, format, monitor }) control.SelectedIndexChanged += delegate { Changed(); };
            template.TextChanged += delegate { Changed(); }; Changed();
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
        private Button ColorButton(bool isOutline)
        {
            Button button = ToolFields.Button(isOutline ? T("描边颜色…", "Outline color…") : T("文字颜色…", "Text color…"));
            button.Click += delegate
            {
                using (ColorDialog picker = new ColorDialog { FullOpen = true, Color = CrosshairOptions.ParseColor(isOutline ? draft.OutlineColor : draft.Color, "#FFFFFF") })
                    if (picker.ShowDialog(this) == DialogResult.OK) { if (isOutline) draft.OutlineColor = CrosshairOptions.Hex(picker.Color); else draft.Color = CrosshairOptions.Hex(picker.Color); Changed(); }
            }; return button;
        }
        private TimerOptions CaptureOptions()
        {
            TimerOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Visible = visible.Checked; o.Backend = (TimerBackend)backend.SelectedIndex;
            o.FontSize = (float)size.Value; o.Bold = bold.Checked; o.Italic = italic.Checked; o.Outline = outline.Checked; o.OutlineWidth = (float)border.Value;
            o.LetterSpacing = (float)spacing.Value; o.Opacity = (int)opacity.Value; o.X = (int)x.Value; o.Y = (int)y.Value;
            o.MonitorDevice = monitor.SelectedIndex <= 0 ? "" : (string)monitor.SelectedItem; o.TextTemplate = template.Text; o.TimeFormat = (TimerTimeFormat)format.SelectedIndex; o.Normalize(); return o;
        }
        private void Changed()
        {
            if (save == null) return;
            TimerOptions o = CaptureOptions();
            string error = !ToolFields.ValidShortcut(draft.StartPauseHotkey) || !ToolFields.ValidShortcut(draft.ResetHotkey) || !ToolFields.ValidShortcut(draft.VisibilityHotkey)
                ? T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.") : validate == null ? "" : validate(o);
            save.Enabled = error.Length == 0;
            feedback.ForeColor = error.Length == 0 ? EditorTheme.Muted : Color.Firebrick;
            border.Enabled = outlineColor.Enabled = outline.Checked; monitor.Enabled = backend.SelectedIndex == 0; preview.Invalidate();
            if (controller != null && error.Length == 0) controller.Apply(o);
            start.Text = controller != null && controller.IsRunning ? T("暂停", "Pause") : T("开始", "Start");
            feedback.Text = error.Length > 0 ? error : controller == null ? "" : StatusText(controller.Status);
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
        private void PaintPreview(object sender, PaintEventArgs e)
        {
            if (save == null) return;
            // The preview shares the production glyph renderer with both output backends.
            using (Bitmap image = TimerTextRenderer.Render(CaptureOptions(), TimeSpan.FromMilliseconds(123456)))
            {
                float scale = Math.Min(1F, Math.Min(Math.Max(1, preview.Width - 16) / (float)image.Width, Math.Max(1, preview.Height - 16) / (float)image.Height));
                e.Graphics.DrawImage(image, new Rectangle(8, 8, Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale))));
            }
        }
    }

    internal sealed class NetworkSettingsDialog : Form
    {
        private readonly NetworkControlOptions draft;
        private readonly CheckBox enabled;
        private readonly ComboBox mode;
        private readonly TextBox path;
        private readonly Button save, browse;
        private readonly Label feedback;
        private readonly Func<NetworkControlOptions, string> validate;
        internal NetworkControlOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        internal NetworkSettingsDialog(NetworkControlOptions options, Func<NetworkControlOptions, string> validator)
        {
            draft = (options ?? new NetworkControlOptions()).Clone(); validate = validator;
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("网络开关", "Network toggle"); ClientSize = new Size(720, 420); MinimumSize = new Size(620, 380);
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
            Panel page = new Panel(); TableLayoutPanel fields = ToolFields.Page(page); page.Dock = DockStyle.Fill;
            enabled = ToolFields.Check(T("启用网络开关快捷键", "Enable network toggle hotkey")); enabled.Checked = draft.Enabled;
            ToolFields.Row(fields, "", enabled);
            mode = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Name = "networkMode" };
            mode.Items.AddRange(new object[] { T("指定程序", "Selected application"), T("整台电脑", "Whole computer") }); mode.SelectedIndex = draft.Mode == NetworkBlockMode.EntireComputer ? 1 : 0;
            ToolFields.Row(fields, T("断网范围", "Block scope"), mode);
            path = new TextBox { Text = draft.ExecutablePath, Dock = DockStyle.Top, Name = "networkExecutable" };
            browse = ToolFields.Button(T("选择程序…", "Choose app…")); browse.Click += delegate
            { using (OpenFileDialog picker = new OpenFileDialog { Filter = T("应用程序 (*.exe)|*.exe", "Applications (*.exe)|*.exe"), CheckFileExists = true }) if (picker.ShowDialog(this) == DialogResult.OK) path.Text = picker.FileName; };
            ToolFields.Row(fields, T("程序路径", "Application path"), path); ToolFields.Row(fields, "", browse);
            ToolFields.Row(fields, T("断开 / 恢复", "Block / restore"), ToolFields.Shortcut(this, "networkHotkey", () => draft.Hotkey, v => draft.Hotkey = v, Changed));
            ToolFields.Row(fields, "", ToolFields.Help(T("按一次断网，再按一次恢复。指定程序包含该路径的所有运行实例；整机模式包含所有应用。首次断网需要 Windows 管理员授权。", "Press once to block, again to restore. Application mode covers all instances at that path; whole-computer mode covers all apps. Windows administrator approval is required to block traffic.")));
            ToolFields.Row(fields, "", ToolFields.Help(T("设置菜单可点“恢复网络”，紧急停止键也会恢复网络。退出 InputStitch 或网络助手结束后自动恢复。启动软件不会自动断网。", "Use Restore network in the settings menu or Emergency Stop to restore traffic. Closing InputStitch or its network helper also restores traffic. Launching the app does not block it.")));
            feedback = ToolFields.Help(""); feedback.Name = "networkFeedback"; ToolFields.Row(fields, "", feedback);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "networkSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, page, EditorDialogs.Actions(save, cancel), T("选择范围和快捷键", "Choose the scope and hotkey"), null);
            enabled.CheckedChanged += delegate { Changed(); }; mode.SelectedIndexChanged += delegate { Changed(); }; path.TextChanged += delegate { Changed(); }; Changed();
        }
        private NetworkControlOptions CaptureOptions()
        { NetworkControlOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Mode = mode.SelectedIndex == 1 ? NetworkBlockMode.EntireComputer : NetworkBlockMode.Executable; o.ExecutablePath = path.Text.Trim(); o.Normalize(); return o; }
        private void Changed()
        {
            if (save == null) return;
            NetworkControlOptions o = CaptureOptions(); path.Enabled = browse.Enabled = mode.SelectedIndex == 0;
            string error = o.Enabled && o.Mode == NetworkBlockMode.Executable && (!Path.IsPathRooted(o.ExecutablePath) || !File.Exists(o.ExecutablePath) || !string.Equals(Path.GetExtension(o.ExecutablePath), ".exe", StringComparison.OrdinalIgnoreCase)) ? T("请选择存在的程序文件。", "Choose an existing application file.") : "";
            if (error.Length == 0 && !ToolFields.ValidShortcut(draft.Hotkey)) error = T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.");
            if (error.Length == 0 && validate != null) error = validate(o);
            feedback.Text = error; feedback.ForeColor = Color.Firebrick; save.Enabled = error.Length == 0;
        }
    }
}
