using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class CrosshairPreview : Control
    {
        internal CrosshairOptions Options = new CrosshairOptions();
        internal bool Light;
        internal CrosshairPreview() { DoubleBuffered = true; MinimumSize = new Size(220, 230); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color a = Light ? Color.FromArgb(245, 246, 248) : Color.FromArgb(30, 40, 54);
            Color b = Light ? Color.FromArgb(225, 230, 236) : Color.FromArgb(42, 54, 70);
            using (Brush first = new SolidBrush(a)) using (Brush second = new SolidBrush(b))
                for (int y = 0; y < Height; y += 20) for (int x = 0; x < Width; x += 20)
                    e.Graphics.FillRectangle(((x / 20 + y / 20) % 2 == 0) ? first : second, x, y, 20, 20);
            using (Bitmap bitmap = CrosshairRenderer.Render(Options))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                ColorMatrix matrix = new ColorMatrix(); matrix.Matrix33 = Options.Opacity / 100F;
                attributes.SetColorMatrix(matrix);
                e.Graphics.DrawImage(bitmap, new Rectangle(Width / 2 - bitmap.Width / 2, Height / 2 - bitmap.Height / 2, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
            }
        }
    }

    internal sealed class CrosshairSettingsDialog : Form
    {
        private readonly CrosshairOptions draft;
        private readonly Func<CrosshairOptions, string> validate;
        private readonly Action<CrosshairOptions> screenPreview;
        private readonly CheckBox enabled, visible, outline, livePreview;
        private readonly ComboBox style, monitor;
        private readonly NumericUpDown size, line, gap, dot, border, opacity, x, y;
        private readonly Button color, outlineColor, shortcut, save;
        private readonly Label feedback;
        private readonly CrosshairPreview preview;
        private bool loading;
        internal CrosshairOptions Result { get; private set; }
        private static string T(string zh, string en) { return EditorDialogs.T(zh, en); }
        private sealed class DisplayChoice
        {
            internal string Device, Title;
            public override string ToString() { return Title; }
        }

        internal CrosshairSettingsDialog(CrosshairOptions options, Action<CrosshairOptions> previewAction, Func<CrosshairOptions, string> validator)
        {
            SuspendLayout();
            draft = (options ?? new CrosshairOptions()).Clone(); draft.Normalize();
            screenPreview = previewAction; validate = validator;
            Font = EditorTheme.BodyFont();
            AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("屏幕准星", "Screen crosshair");
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
            ClientSize = new Size(920, 700); MinimumSize = new Size(780, 590);

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            EditorTabs tabs = new EditorTabs { Dock = DockStyle.Fill };
            TabPage appearance = new TabPage(T("外观", "Appearance"));
            TabPage position = new TabPage(T("位置与快捷键", "Position & hotkey"));
            tabs.TabPages.AddRange(new[] { appearance, position }); body.Controls.Add(tabs, 0, 0);
            TableLayoutPanel look = Fields(appearance), place = Fields(position);
            enabled = Check(T("开启屏幕准星（下次启动时恢复）", "Enable crosshair (restore on next launch)"));
            visible = Check(T("显示准星（也可用快捷键切换）", "Show crosshair (also controlled by hotkey)"));
            Row(look, "", enabled); Row(look, "", visible);
            style = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Name = "crosshairStyle" };
            style.Items.AddRange(Localizer.IsEnglish ? new object[] { "Dot", "Square", "Cross", "Cross + dot", "Ring", "Ring + dot", "T crosshair", "Diagonal cross" }
                : new object[] { "圆点", "方块", "十字准星", "十字 + 中央圆点", "圆环", "圆环 + 中央圆点", "T 形准星", "斜十字" });
            Row(look, T("准星样式", "Style"), style);
            size = Number(1, 200, "crosshairSize"); Row(look, T("大小（像素）", "Size (px)"), size);
            line = Number(1, 30, "crosshairLine"); Row(look, T("线宽（像素）", "Line width (px)"), line);
            gap = Number(0, 100, "crosshairGap"); Row(look, T("中心间隙", "Center gap (px)"), gap);
            dot = Number(1, 50, "crosshairDot"); Row(look, T("中央圆点大小", "Center dot (px)"), dot);
            color = EditorDialogs.Button("", DialogResult.None); color.Name = "crosshairColor";
            color.Click += delegate { ChooseColor(false); }; Row(look, T("准星颜色", "Crosshair color"), color);
            outline = Check(T("开启描边", "Enable outline")); Row(look, "", outline);
            border = Number(1, 12, "crosshairOutlineWidth"); Row(look, T("描边厚度", "Outline (px)"), border);
            outlineColor = EditorDialogs.Button("", DialogResult.None); outlineColor.Click += delegate { ChooseColor(true); };
            Row(look, T("描边颜色", "Outline color"), outlineColor);
            opacity = Number(0, 100, "crosshairOpacity"); Row(look, T("不透明度（%）", "Opacity (%)"), opacity);
            Row(look, "", Help(T("100% 完全可见，0% 完全透明。", "100% is fully visible; 0% is transparent.")));

            monitor = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Name = "crosshairMonitor" };
            monitor.Items.Add(new DisplayChoice { Device = "", Title = T("主显示器（自动）", "Primary display (automatic)") });
            bool found = string.IsNullOrEmpty(draft.MonitorDevice);
            foreach (Screen screen in Screen.AllScreens)
            {
                monitor.Items.Add(new DisplayChoice { Device = screen.DeviceName, Title = screen.DeviceName + " · " + screen.Bounds.Width + " × " + screen.Bounds.Height });
                if (screen.DeviceName == draft.MonitorDevice) found = true;
            }
            if (!found) monitor.Items.Add(new DisplayChoice { Device = draft.MonitorDevice, Title = draft.MonitorDevice + T("（已断开，暂用主显示器）", " (disconnected; primary used)") });
            Row(place, T("绘制到显示器", "Display"), monitor);
            x = Number(-16384, 16384, "crosshairOffsetX"); y = Number(-16384, 16384, "crosshairOffsetY");
            Row(place, T("水平偏移", "X offset (px)"), x); Row(place, T("垂直偏移", "Y offset (px)"), y);
            Row(place, "", Help(T("默认 0, 0 为整块屏幕中央。向右、向下为正数，向左、向上为负数；每次增减 1 像素。超出屏幕时保持在最近的屏幕边缘。", "0, 0 is the full display center. Positive values move right/down; negative values move left/up. Each step is one pixel. Positions outside the display stay at the nearest edge.")));
            Button center = EditorDialogs.Button(T("回到屏幕中央", "Reset to center"), DialogResult.None); center.Name = "crosshairCenter";
            center.Click += delegate { loading = true; x.Value = y.Value = 0; loading = false; Changed(); }; Row(place, "", center);
            shortcut = EditorDialogs.Button("", DialogResult.None); shortcut.Name = "crosshairHotkey";
            shortcut.Click += delegate
            {
                using (VirtualKeyboardDialog keys = new VirtualKeyboardDialog(true, VirtualKeyboardDialog.InputsFromTrigger(draft.Hotkey),
                    T("选择控制准星显隐的按键组合。确认后回到准星设置。", "Choose a shortcut to show or hide the crosshair, then return to its settings."),
                    T("准星显隐快捷键", "Crosshair visibility hotkey")))
                    if (keys.ShowDialog(this) == DialogResult.OK) { draft.Hotkey = keys.SelectedTrigger; Changed(); }
            };
            Row(place, T("显隐快捷键", "Show/hide hotkey"), shortcut);
            Button clear = EditorDialogs.Button(T("清除快捷键", "Clear hotkey"), DialogResult.None);
            clear.Click += delegate { draft.Hotkey = null; Changed(); }; Row(place, "", clear);
            Row(place, "", Help(T("快捷键仅在开启准星后生效，会记住显隐状态。建议使用 Ctrl / Alt 组合；紧急停止键始终优先。", "The hotkey works while enabled and remembers visibility. Prefer a Ctrl/Alt combination. Emergency Stop always takes priority.")));
            Row(place, "", Help(T("显示器断开后暂时改用主显示器，重新连接后恢复原选择。", "A disconnected display falls back to the primary display until it returns.")));

            Panel sideViewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel side = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 6, Padding = new Padding(12, 0, 0, 0) };
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            side.RowStyles.Add(new RowStyle(SizeType.AutoSize)); side.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
            for (int i = 2; i < 6; i++) side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            side.Controls.Add(Help(T("实时预览 · 按实际像素大小绘制", "Live preview · actual pixel size")), 0, 0);
            preview = new CrosshairPreview { Dock = DockStyle.Fill, Name = "crosshairPreview" }; side.Controls.Add(preview, 0, 1);
            CheckBox light = Check(T("浅色预览背景", "Light preview background"));
            light.CheckedChanged += delegate { preview.Light = light.Checked; preview.Invalidate(); }; side.Controls.Add(light, 0, 2);
            livePreview = Check(T("在屏幕上实时预览", "Preview on screen")); side.Controls.Add(livePreview, 0, 3);
            feedback = Help(""); feedback.Name = "crosshairFeedback"; side.Controls.Add(feedback, 0, 4);
            side.Controls.Add(Help(T("准星穿透鼠标点击，不抢焦点。请将游戏设为窗口化或无边框窗口；本版不支持独占全屏。", "The overlay passes mouse clicks through and does not take focus. Set the game to windowed or borderless mode; exclusive fullscreen is not supported in this build.")), 0, 5);
            sideViewport.Controls.Add(side); body.Controls.Add(sideViewport, 1, 0);
            save = EditorDialogs.Button(T("保存准星设置", "Save crosshair"), DialogResult.None); save.Name = "crosshairSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel);
            AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(save, cancel), T("选择样式并调整位置，保存后下次自动恢复。", "Choose a style and position. Save to restore on next launch."), null);
            LoadDraft();
            foreach (NumericUpDown number in new[] { size, line, gap, dot, border, opacity, x, y }) number.ValueChanged += delegate { Changed(); };
            foreach (CheckBox check in new[] { enabled, visible, outline, livePreview }) check.CheckedChanged += delegate { Changed(); };
            style.SelectedIndexChanged += delegate { Changed(); }; monitor.SelectedIndexChanged += delegate { Changed(); };
            ResumeLayout(true);
            Changed();
        }
        private static TableLayoutPanel Fields(TabPage page)
        {
            page.Padding = new Padding(10); page.AutoScroll = true; page.BackColor = Color.White;
            TableLayoutPanel fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 6, 12, 12) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(fields); return fields;
        }
        private static Label Help(string text) { return new Label { Text = text, AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Muted, Margin = new Padding(3, 4, 3, 4) }; }
        private static CheckBox Check(string text) { return new CheckBox { Text = text, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(3, 4, 3, 4) }; }
        private static NumericUpDown Number(int min, int max, string name) { return new NumericUpDown { AutoScaleMode = AutoScaleMode.None,
            Minimum = min, Maximum = max, Width = 110, MinimumSize = new Size(110, 0), Name = name, Margin = new Padding(3, 3, 3, 3) }; }
        private static void Row(TableLayoutPanel fields, string title, Control input)
        {
            int row = fields.RowCount++; fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (title.Length == 0) { fields.Controls.Add(input, 0, row); fields.SetColumnSpan(input, 2); }
            else
            {
                fields.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 8, 4) }, 0, row);
                fields.Controls.Add(input, 1, row);
            }
        }
        private void LoadDraft()
        {
            loading = true;
            enabled.Checked = draft.Enabled; visible.Checked = draft.Visible; outline.Checked = draft.Outline;
            livePreview.Checked = draft.Enabled && draft.Visible;
            style.SelectedIndex = (int)draft.Style; size.Value = draft.Size; line.Value = draft.LineWidth;
            gap.Value = draft.Gap; dot.Value = draft.DotSize; border.Value = draft.OutlineWidth; opacity.Value = draft.Opacity;
            x.Value = draft.OffsetX; y.Value = draft.OffsetY;
            for (int i = 0; i < monitor.Items.Count; i++) if (((DisplayChoice)monitor.Items[i]).Device == draft.MonitorDevice) monitor.SelectedIndex = i;
            loading = false;
        }
        private CrosshairOptions CaptureOptions()
        {
            CrosshairOptions o = draft.Clone();
            o.Enabled = enabled.Checked; o.Visible = visible.Checked; o.Outline = outline.Checked;
            o.Style = (CrosshairStyle)Math.Max(0, style.SelectedIndex); o.Size = (int)size.Value; o.LineWidth = (int)line.Value;
            o.Gap = (int)gap.Value; o.DotSize = (int)dot.Value; o.OutlineWidth = (int)border.Value; o.Opacity = (int)opacity.Value;
            o.OffsetX = (int)x.Value; o.OffsetY = (int)y.Value;
            o.MonitorDevice = monitor.SelectedItem == null ? "" : ((DisplayChoice)monitor.SelectedItem).Device;
            o.Normalize(); return o;
        }
        private void Changed()
        {
            if (loading) return;
            CrosshairOptions o = CaptureOptions(); preview.Options = o; preview.Invalidate();
            bool solid = o.Style == CrosshairStyle.Dot || o.Style == CrosshairStyle.Square;
            line.Enabled = !solid; gap.Enabled = !solid && o.Style != CrosshairStyle.Ring && o.Style != CrosshairStyle.RingDot;
            dot.Enabled = o.Style == CrosshairStyle.CrossDot || o.Style == CrosshairStyle.RingDot;
            border.Enabled = outlineColor.Enabled = o.Outline;
            color.Text = draft.Color; outlineColor.Text = draft.OutlineColor;
            color.BackColor = CrosshairOptions.ParseColor(draft.Color, "#00FF65");
            outlineColor.BackColor = CrosshairOptions.ParseColor(draft.OutlineColor, "#000000");
            color.ForeColor = color.BackColor.GetBrightness() < .45 ? Color.White : EditorTheme.Ink;
            outlineColor.ForeColor = outlineColor.BackColor.GetBrightness() < .45 ? Color.White : EditorTheme.Ink;
            shortcut.Text = draft.Hotkey == null ? T("未设置", "Not assigned") : InputNames.FormatTrigger(draft.Hotkey);
            string error = draft.Hotkey != null && ModifierSafetyPolicy.IsModifierVirtualKey(draft.Hotkey.VirtualKey)
                ? T("快捷键需包含一个普通键，例如 Ctrl + Alt + F10。", "Include a regular key, for example Ctrl + Alt + F10.")
                : validate == null ? "" : validate(o);
            feedback.ForeColor = error.Length > 0 ? Color.Firebrick : EditorTheme.Muted;
            feedback.Text = error.Length > 0 ? error : o.Opacity == 0 ? T("当前完全透明，提高不透明度后可见。", "Currently transparent. Raise opacity to see the crosshair.")
                : T("预览不改动已保存设置；取消会恢复原设置。", "Preview does not change saved settings. Cancel restores them.");
            save.Enabled = error.Length == 0;
            if (screenPreview != null)
            {
                CrosshairOptions temporary = o.Clone(); temporary.Enabled = temporary.Visible = livePreview.Checked;
                screenPreview(temporary);
            }
        }
        private void ChooseColor(bool isOutline)
        {
            using (ColorDialog picker = new ColorDialog { FullOpen = true, Color = CrosshairOptions.ParseColor(isOutline ? draft.OutlineColor : draft.Color, "#000000") })
                if (picker.ShowDialog(this) == DialogResult.OK)
                { if (isOutline) draft.OutlineColor = CrosshairOptions.Hex(picker.Color); else draft.Color = CrosshairOptions.Hex(picker.Color); Changed(); }
        }
    }
}
