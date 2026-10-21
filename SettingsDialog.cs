using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Extracted from InputStitch.cs during the incremental MainForm decomposition (architecture-review
    // finding F4); members moved verbatim, behaviour is unchanged.
    public class GearButton : Button
    {
        private readonly Font gearFont = new Font("Segoe UI Symbol", 13F, FontStyle.Regular, GraphicsUnit.Point);

        public GearButton()
        {
            Text = "";
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.White;
            FlatAppearance.BorderColor = Color.FromArgb(190, 199, 211);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 242, 250);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 231, 245);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            TextRenderer.DrawText(pevent.Graphics, "⚙", gearFont, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) gearFont.Dispose();
            base.Dispose(disposing);
        }
    }

    public class SettingsDialog : Form
    {
        private readonly CheckBox scanCodeBox;
        private readonly CheckBox uiSafetyBox;
        private readonly CheckBox trayBox;
        private readonly CheckBox topMostBox;
        private readonly CheckBox activateTargetBox;
        private readonly NumericUpDown delayBox;
        private readonly CheckBox autoProfileBox;
        private readonly ComboBox languageBox;
        private readonly ComboBox updateModeBox;
        private readonly ComboBox gamepadTypeBox;
        private readonly CheckBox gamepadRouterBox;
        private readonly CheckBox sdl3InputBox;
        private readonly Button gamepadRouterSourcesButton;
        private readonly Label gamepadRouterSourcesSummary;
        private readonly Label gamepadStatusLabel;
        private readonly IdleGamepadSettingsPanel idleSettings;
        private readonly Func<DeviceInventorySnapshot> routerInventoryProvider;
        private RouterSourcePolicyConfig routerSourcePolicy;

        public event EventHandler CheckForUpdatesRequested;

        public bool UseScanCodeInput { get { return scanCodeBox.Checked; } }
        public bool PauseMacroInRiskyUi { get { return uiSafetyBox.Checked; } }
        public bool MinimizeToTray { get { return trayBox.Checked; } }
        public bool KeepWindowTopMost { get { return topMostBox.Checked; } }
        public bool ActivateTargetWindowOnUiRun { get { return activateTargetBox.Checked; } }
        public int UiRunStartDelayMs { get { return (int)delayBox.Value; } }
        public bool AutoSwitchProfiles { get { return autoProfileBox.Checked; } }
        public IdleGamepadOptions SelectedIdleOptions { get { return idleSettings.ReadOptions(); } }
        public string SelectedLanguage { get { return languageBox.SelectedIndex == 1 ? Localizer.English : Localizer.Chinese; } }
        public string SelectedGamepadType
        {
            get
            {
                if (gamepadTypeBox.SelectedIndex == 2) return VirtualGamepadTypes.None;
                return gamepadTypeBox.SelectedIndex == 1 ? VirtualGamepadTypes.DualShock4 : VirtualGamepadTypes.Xbox360;
            }
        }
        public bool SelectedGamepadRouterEnabled { get { return gamepadRouterBox.Checked; } }
        public bool Sdl3InputEnabled { get { return sdl3InputBox.Checked; } }
        public RouterSourcePolicyConfig SelectedGamepadRouterSourcePolicy
        {
            get { return routerSourcePolicy == null ? new RouterSourcePolicyConfig() : routerSourcePolicy.Clone(); }
        }
        public string SelectedUpdateMode
        {
            get
            {
                if (updateModeBox.SelectedIndex == 1) return UpdateModes.Manual;
                if (updateModeBox.SelectedIndex == 2) return UpdateModes.Disabled;
                return UpdateModes.Automatic;
            }
        }

        public SettingsDialog(MacroConfig config, Func<TargetWindowIdentity> recentIdleTargetProvider = null)
            : this(config, recentIdleTargetProvider, null)
        {
        }

        internal SettingsDialog(MacroConfig config, Func<TargetWindowIdentity> recentIdleTargetProvider,
            Func<DeviceInventorySnapshot> currentRouterInventoryProvider)
        {
            routerInventoryProvider = currentRouterInventoryProvider;
            routerSourcePolicy = config.GamepadRouterSourcePolicy == null
                ? new RouterSourcePolicyConfig() : config.GamepadRouterSourcePolicy.Clone();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Text = Localizer.T("设置");
            Font = EditorTheme.BodyFont();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            MinimumSize = new Size(620, 480);
            ClientSize = new Size(780, 620);
            BackColor = Color.FromArgb(247, 249, 252);
            AutoScroll = true;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label hint = new Label();
            hint.Text = Localizer.T("这些选项通常保持默认即可。修改会在点击确定后生效。");
            hint.AutoSize = true;
            hint.MaximumSize = new Size(540, 0);
            hint.ForeColor = Color.FromArgb(86, 96, 112);
            hint.Margin = new Padding(3, 0, 3, 14);
            root.Controls.Add(hint, 0, 0);

            GroupBox general = MakeGroup(Localizer.T("常规"));
            TableLayoutPanel generalLayout = MakeStack();
            general.Controls.Add(generalLayout);
            languageBox = new EditorChoice();
            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.Items.Add("简体中文");
            languageBox.Items.Add("English");
            languageBox.SelectedIndex = string.Equals(config.Language, Localizer.English, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            TableLayoutPanel languageRow = new TableLayoutPanel();
            languageRow.AutoSize = true;
            languageRow.Dock = DockStyle.Top;
            languageRow.ColumnCount = 2;
            languageRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languageRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label languageLabel = new Label();
            languageLabel.Text = Localizer.T("界面语言：");
            languageLabel.AutoSize = true;
            languageLabel.Anchor = AnchorStyles.Left;
            languageLabel.Margin = new Padding(0, 5, 12, 5);
            languageBox.Width = 190;
            languageBox.MinimumSize = new Size(160, 0);
            languageBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            languageBox.Margin = new Padding(0, 4, 0, 0);
            languageRow.Controls.Add(languageLabel, 0, 0);
            languageRow.Controls.Add(languageBox, 1, 0);
            generalLayout.Controls.Add(languageRow);
            trayBox = MakeCheck(Localizer.T("最小化到系统托盘"), config.MinimizeToTray);
            topMostBox = MakeCheck(Localizer.T("保持主窗口置顶"), config.KeepWindowTopMost);
            generalLayout.Controls.Add(trayBox);
            generalLayout.Controls.Add(topMostBox);

            GroupBox input = MakeGroup(Localizer.T("输入与安全"));
            TableLayoutPanel inputLayout = MakeStack();
            input.Controls.Add(inputLayout);
            scanCodeBox = MakeCheck(Localizer.T("使用扫描码发送键盘输入（推荐游戏）"), config.UseScanCodeInput);
            uiSafetyBox = MakeCheck(Localizer.T("在编辑界面时暂停宏输出（推荐）"), config.PauseMacroInRiskyUi);
            inputLayout.Controls.Add(scanCodeBox);
            inputLayout.Controls.Add(uiSafetyBox);

            GroupBox gamepad = MakeGroup(Localizer.T("虚拟手柄输出"));
            TableLayoutPanel gamepadLayout = MakeStack();
            gamepad.Controls.Add(gamepadLayout);
            TableLayoutPanel gamepadTypeRow = new TableLayoutPanel();
            gamepadTypeRow.AutoSize = true;
            gamepadTypeRow.Dock = DockStyle.Top;
            gamepadTypeRow.ColumnCount = 2;
            gamepadTypeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gamepadTypeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label gamepadTypeLabel = new Label();
            gamepadTypeLabel.Text = Localizer.T("手柄类型：");
            gamepadTypeLabel.AutoSize = true;
            gamepadTypeLabel.Anchor = AnchorStyles.Left;
            gamepadTypeLabel.Margin = new Padding(0, 5, 12, 5);
            gamepadTypeBox = new EditorChoice();
            gamepadTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            gamepadTypeBox.Dock = DockStyle.Fill;
            gamepadTypeBox.Items.Add(EditorDialogs.T("Xbox 360（推荐）", "Xbox 360 (recommended)"));
            gamepadTypeBox.Items.Add(Localizer.T("PS4 / DualShock 4"));
            gamepadTypeBox.Items.Add(EditorDialogs.T("仅键盘 / 鼠标（不创建虚拟手柄）", "Keyboard / mouse only"));
            gamepadTypeBox.SelectedIndex = VirtualGamepadTypes.IsDisabled(config.GamepadDeviceType) ? 2 :
                (string.Equals(config.GamepadDeviceType, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            gamepadTypeRow.Controls.Add(gamepadTypeLabel, 0, 0);
            gamepadTypeRow.Controls.Add(gamepadTypeBox, 1, 0);
            gamepadLayout.Controls.Add(gamepadTypeRow);

            gamepadRouterBox = MakeCheck(
                EditorDialogs.T("转发其他手柄的输入（测试）", "Forward input from other controllers (Beta)"),
                config.GamepadRouterEnabled);
            int gamepadTypeBeforeRouter = gamepadTypeBox.SelectedIndex;
            gamepadRouterBox.CheckedChanged += delegate
            {
                if (gamepadRouterBox.Checked)
                {
                    gamepadTypeBeforeRouter = gamepadTypeBox.SelectedIndex;
                    gamepadTypeBox.SelectedIndex = 0;
                    gamepadTypeBox.Enabled = false;
                }
                else
                {
                    gamepadTypeBox.Enabled = true;
                    if (gamepadTypeBeforeRouter >= 0 && gamepadTypeBeforeRouter < gamepadTypeBox.Items.Count)
                        gamepadTypeBox.SelectedIndex = gamepadTypeBeforeRouter;
                }
                gamepadRouterSourcesButton.Enabled = gamepadRouterBox.Checked;
                RefreshRouterSourceSummary();
                RefreshGamepadStatus();
            };
            if (gamepadRouterBox.Checked) { gamepadTypeBox.SelectedIndex = 0; gamepadTypeBox.Enabled = false; }
            gamepadLayout.Controls.Add(gamepadRouterBox);
            Label routerHint = new Label();
            routerHint.AutoSize = true;
            routerHint.Dock = DockStyle.Fill;
            routerHint.ForeColor = Color.FromArgb(86, 96, 112);
            routerHint.Margin = new Padding(24, 0, 0, 6);
            routerHint.Text = Localizer.IsEnglish
                ? "Forward physical controller buttons, sticks and triggers to the virtual controller. Games that only read XInput 0 also require the virtual controller in slot 0. This switch does not hide physical controllers."
                : "把实体手柄的按键、摇杆和扳机操作转发给虚拟手柄。只读取首个手柄的游戏需要虚拟手柄处于 XInput 0；此开关不会隐藏原手柄。";
            gamepadLayout.Controls.Add(routerHint);
            sdl3InputBox = MakeCheck(
                Localizer.IsEnglish
                    ? "Support more controller types (SDL3; restart required)"
                    : "支持更多类型的手柄（SDL3，重启后生效）",
                config.Sdl3InputEnabled);
            gamepadLayout.Controls.Add(sdl3InputBox);
            Label sdl3Hint = new Label();
            sdl3Hint.AutoSize = true;
            sdl3Hint.Dock = DockStyle.Fill;
            sdl3Hint.ForeColor = Color.FromArgb(86, 96, 112);
            sdl3Hint.Margin = new Padding(24, 0, 0, 6);
            sdl3Hint.Text = Localizer.IsEnglish
                ? "Adds PlayStation, Switch and other HID controllers. Xbox controllers keep using XInput. Changes apply after restarting InputStitch."
                : "接入 PlayStation、Switch 等手柄。Xbox 手柄继续使用 XInput；更改后需重启 InputStitch。";
            gamepadLayout.Controls.Add(sdl3Hint);

            gamepadRouterSourcesButton = MakeDialogButton(Localizer.IsEnglish ? "Choose routed controllers..." : "选择汇总来源...");
            gamepadRouterSourcesButton.Margin = new Padding(24, 2, 0, 2);
            gamepadRouterSourcesButton.Click += delegate
            {
                using (RouterSourceDialog sourceDialog = new RouterSourceDialog(routerSourcePolicy, routerInventoryProvider))
                {
                    if (sourceDialog.ShowDialog(this) != DialogResult.OK) return;
                    routerSourcePolicy = sourceDialog.SelectedPolicy;
                    RefreshRouterSourceSummary();
                }
            };
            gamepadRouterSourcesButton.Enabled = gamepadRouterBox.Checked;
            gamepadLayout.Controls.Add(gamepadRouterSourcesButton);

            gamepadRouterSourcesSummary = new Label();
            gamepadRouterSourcesSummary.AutoSize = true;
            gamepadRouterSourcesSummary.Dock = DockStyle.Fill;
            gamepadRouterSourcesSummary.ForeColor = Color.FromArgb(86, 96, 112);
            gamepadRouterSourcesSummary.Margin = new Padding(24, 0, 0, 6);
            gamepadLayout.Controls.Add(gamepadRouterSourcesSummary);
            RefreshRouterSourceSummary();

            gamepadStatusLabel = new Label();
            gamepadStatusLabel.AutoSize = true;
            gamepadStatusLabel.Dock = DockStyle.Fill;
            gamepadStatusLabel.Margin = new Padding(0, 5, 0, 8);
            gamepadLayout.Controls.Add(gamepadStatusLabel);
            Button connectGamepad = MakeDialogButton(Localizer.T("检测驱动并连接"));
            connectGamepad.Margin = new Padding(0, 2, 0, 6);
            connectGamepad.Enabled = !VirtualGamepadTypes.IsDisabled(SelectedGamepadType);
            gamepadTypeBox.SelectedIndexChanged += delegate
            {
                connectGamepad.Enabled = !VirtualGamepadTypes.IsDisabled(SelectedGamepadType);
                RefreshGamepadStatus();
            };
            connectGamepad.Click += delegate
            {
                try
                {
                    GamepadOutput.EnsureConnected(SelectedGamepadType);
                    RefreshGamepadStatus();
                }
                catch (GamepadOutputException ex)
                {
                    RefreshGamepadStatus();
                    GamepadDriverGuidance.Show(this, ex);
                }
            };
            gamepadLayout.Controls.Add(connectGamepad);
            Button openDriverPage = MakeDialogButton(Localizer.T("打开官方驱动下载页"));
            openDriverPage.Margin = new Padding(0, 0, 0, 6);
            openDriverPage.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(AppInfo.ViGEmBusReleaseUrl) { UseShellExecute = true }); }
                catch (Exception ex)
                {
                    AppLog.Write("Could not open official ViGEmBus release page", ex);
                    LocalizedMessageBox.Show(this, AppInfo.ViGEmBusReleaseUrl, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            gamepadLayout.Controls.Add(openDriverPage);
            Label gamepadHint = new Label();
            gamepadHint.Text = Localizer.T("虚拟手柄需要 ViGEmBus 驱动。该上游项目已停止维护；InputStitch 不会静默安装驱动，只会打开原作者的官方 GitHub 下载页。");
            gamepadHint.AutoSize = true;
            gamepadHint.Dock = DockStyle.Fill;
            gamepadHint.ForeColor = Color.FromArgb(120, 92, 44);
            gamepadHint.Margin = new Padding(0, 3, 0, 3);
            gamepadLayout.Controls.Add(gamepadHint);
            RefreshGamepadStatus();

            GroupBox target = MakeGroup(Localizer.T("目标窗口与方案"));
            TableLayoutPanel targetLayout = MakeStack();
            target.Controls.Add(targetLayout);
            activateTargetBox = MakeCheck(Localizer.T("从界面运行时先切换到目标窗口"), config.ActivateTargetWindowOnUiRun);
            targetLayout.Controls.Add(activateTargetBox);
            TableLayoutPanel delayRow = new TableLayoutPanel();
            delayRow.AutoSize = true;
            delayRow.Dock = DockStyle.Top;
            delayRow.ColumnCount = 3;
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            delayRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label delayLabel = new Label();
            delayLabel.Text = Localizer.T("切换后等待：");
            delayLabel.AutoSize = true;
            delayLabel.Anchor = AnchorStyles.Left;
            delayLabel.Margin = new Padding(24, 5, 12, 5);
            delayBox = new NumericUpDown();
            delayBox.Minimum = 0;
            delayBox.Maximum = 5000;
            delayBox.Increment = 50;
            delayBox.Value = Math.Max(delayBox.Minimum, Math.Min(delayBox.Maximum, config.UiRunStartDelayMs));
            delayBox.Width = 100;
            delayBox.Anchor = AnchorStyles.Left;
            Label ms = new Label();
            ms.Text = "ms";
            ms.AutoSize = true;
            ms.Anchor = AnchorStyles.Left;
            ms.Margin = new Padding(8, 5, 0, 5);
            delayRow.Controls.Add(delayLabel, 0, 0);
            delayRow.Controls.Add(delayBox, 1, 0);
            delayRow.Controls.Add(ms, 2, 0);
            targetLayout.Controls.Add(delayRow);
            autoProfileBox = MakeCheck(Localizer.T("按前台程序自动切换已绑定方案"), config.AutoSwitchProfiles);
            targetLayout.Controls.Add(autoProfileBox);

            GroupBox updates = MakeGroup(Localizer.T("软件更新"));
            TableLayoutPanel updatesLayout = MakeStack();
            updates.Controls.Add(updatesLayout);
            TableLayoutPanel updateRow = new TableLayoutPanel();
            updateRow.AutoSize = true;
            updateRow.Dock = DockStyle.Top;
            updateRow.ColumnCount = 2;
            updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label updateLabel = new Label();
            updateLabel.Text = Localizer.T("更新方式：");
            updateLabel.AutoSize = true;
            updateLabel.Anchor = AnchorStyles.Left;
            updateLabel.Margin = new Padding(0, 5, 12, 5);
            updateModeBox = new EditorChoice();
            updateModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            updateModeBox.Dock = DockStyle.Fill;
            updateModeBox.Items.Add(Localizer.T("启动时自动检查并提示安装"));
            updateModeBox.Items.Add(Localizer.T("仅手动检查（推荐）"));
            updateModeBox.Items.Add(Localizer.T("不检查更新"));
            updateModeBox.Enabled = ReleaseInfo.AutomaticChecksAllowed;
            updateModeBox.SelectedIndex = string.Equals(config.UpdateMode, UpdateModes.Disabled, StringComparison.OrdinalIgnoreCase) ? 2 :
                (string.Equals(config.UpdateMode, UpdateModes.Manual, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            updateRow.Controls.Add(updateLabel, 0, 0);
            updateRow.Controls.Add(updateModeBox, 1, 0);
            updatesLayout.Controls.Add(updateRow);
            Label updateHint = new Label();
            updateHint.Text = Localizer.T("更新优先从 InputStitch 官网检查和下载；如果官网不可用，会自动尝试官方 GitHub Release。下载后必须通过 SHA-256 校验。");
            if (!ReleaseInfo.AutomaticChecksAllowed)
                updateHint.Text = Localizer.IsEnglish
                    ? "Beta build: automatic update checks are disabled. Check manually to open GitHub Releases and download a test build. Your stable update preference is preserved."
                    : "测试版不自动检查更新。点击手动检查可打开 GitHub Releases 下载测试版；正式版的更新偏好保持不变。";
            updateHint.AutoSize = true;
            updateHint.Dock = DockStyle.Fill;
            updateHint.ForeColor = Color.FromArgb(86, 96, 112);
            updateHint.Margin = new Padding(0, 5, 0, 8);
            updatesLayout.Controls.Add(updateHint);
            Button checkNow = MakeDialogButton(Localizer.T("立即检查更新"));
            checkNow.Margin = new Padding(0, 2, 0, 2);
            checkNow.Click += delegate
            {
                EventHandler handler = CheckForUpdatesRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            };
            updatesLayout.Controls.Add(checkNow);

            idleSettings = new IdleGamepadSettingsPanel(config.IdleGamepad ?? new IdleGamepadOptions(), recentIdleTargetProvider);
            GroupBox idleGroup = MakeGroup(Localizer.IsEnglish ? "Idle gamepad input" : "闲置自动手柄输入");
            idleGroup.Controls.Add(idleSettings);
            TabControl tabs = new EditorTabs();
            tabs.Dock = DockStyle.Fill;
            AddSettingsTab(tabs, Localizer.T("常规"), general, updates);
            AddSettingsTab(tabs, Localizer.T("输入与安全"), input, gamepad);
            AddSettingsTab(tabs, Localizer.IsEnglish ? "Automation" : "自动化", idleGroup, target);
            root.RowStyles.Clear();
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(tabs, 0, 1);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            Button ok = MakeDialogButton(EditorDialogs.T("保存设置", "Save settings"));
            ok.DialogResult = DialogResult.OK;
            Button cancel = MakeDialogButton(Localizer.T("取消"));
            cancel.DialogResult = DialogResult.Cancel;
            Button defaults = MakeDialogButton(Localizer.T("恢复默认"));
            defaults.Click += delegate
            {
                scanCodeBox.Checked = true;
                uiSafetyBox.Checked = true;
                trayBox.Checked = true;
                topMostBox.Checked = false;
                activateTargetBox.Checked = false;
                delayBox.Value = 300;
                autoProfileBox.Checked = false;
                updateModeBox.SelectedIndex = 0;
                gamepadRouterBox.Checked = false;
                routerSourcePolicy = new RouterSourcePolicyConfig();
                RefreshRouterSourceSummary();
                gamepadTypeBox.SelectedIndex = 0;
                sdl3InputBox.Checked = new MacroConfig().Sdl3InputEnabled;
                idleSettings.LoadOptions(new IdleGamepadOptions());
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(defaults);
            root.Controls.Add(buttons, 0, 2);
            AcceptButton = ok;
            CancelButton = cancel;
            root.Padding = new Padding(0);
            EditorDialogs.Frame(this, root, buttons, EditorDialogs.T("常用选项保持默认即可。点击保存设置后生效，取消不会修改当前设置。", "Defaults work for most macros. Save settings to apply your changes, or cancel to keep your current setup."), hint);
        }

        private static void AddSettingsTab(TabControl tabs, string title, params Control[] groups)
        {
            TabPage page = new TabPage(title);
            page.BackColor = Color.FromArgb(247, 249, 252);
            page.AutoScroll = true;
            page.Padding = new Padding(10);
            TableLayoutPanel stack = new TableLayoutPanel();
            stack.AutoSize = true;
            stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            stack.Dock = DockStyle.Top;
            stack.ColumnCount = 1;
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            foreach (Control group in groups)
            {
                group.Dock = DockStyle.Fill;
                group.Margin = new Padding(0, 0, 0, 10);
                stack.Controls.Add(group);
            }
            page.Controls.Add(stack);
            tabs.TabPages.Add(page);
        }

        private void RefreshRouterSourceSummary()
        {
            if (gamepadRouterSourcesSummary == null) return;
            RouterSourcePolicyConfig policy = routerSourcePolicy == null ? new RouterSourcePolicyConfig() : routerSourcePolicy;
            RouterSourcePolicyConfig.Normalize(policy);
            if (string.Equals(policy.Mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase))
            {
                gamepadRouterSourcesSummary.Text = Localizer.IsEnglish
                    ? "Route all connected, identifiable external controllers."
                    : "汇总所有已连接且可识别的外部手柄。";
                gamepadRouterSourcesSummary.ForeColor = Color.FromArgb(86, 96, 112);
                return;
            }

            int count = policy.SelectedDeviceKeys == null ? 0 : policy.SelectedDeviceKeys.Count;
            gamepadRouterSourcesSummary.Text = Localizer.IsEnglish
                ? "Source policy: selected stable devices only (" + count.ToString() + "). Unresolved XInput sources are blocked."
                : "来源策略：仅已选择的稳定设备（" + count.ToString() + " 个）；未解析的 XInput 来源不会被转发。";
            gamepadRouterSourcesSummary.ForeColor = count == 0 ? Color.DarkOrange : Color.FromArgb(86, 96, 112);
        }

        private void RefreshGamepadStatus()
        {
            if (gamepadTypeBox != null && VirtualGamepadTypes.IsDisabled(SelectedGamepadType) &&
                (gamepadRouterBox == null || !gamepadRouterBox.Checked))
            {
                if (GamepadOutput.IsConnected)
                {
                    gamepadStatusLabel.Text = Localizer.IsEnglish
                        ? "Do not create is selected. The currently connected virtual controller will be disconnected after you save Settings."
                        : "已选择“不创建虚拟手柄”。保存设置后，当前已连接的虚拟手柄会断开。";
                    gamepadStatusLabel.ForeColor = Color.DarkOrange;
                }
                else
                {
                    gamepadStatusLabel.Text = Localizer.IsEnglish
                        ? "Virtual controller creation is disabled. Keyboard/mouse macros and physical-controller triggers can still be used."
                        : "当前不会创建虚拟手柄；键盘/鼠标宏和实体手柄触发仍可使用。";
                    gamepadStatusLabel.ForeColor = Color.FromArgb(86, 96, 112);
                }
                return;
            }
            if (GamepadOutput.IsConnected)
            {
                string type = string.Equals(GamepadOutput.ConnectedType, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase)
                    ? "PS4 / DualShock 4" : "Xbox 360";
                int slot = GamepadOutput.OwnXboxUserIndex;
                string slotText = slot >= 0 ? " · XInput " + slot.ToString() : "";
                gamepadStatusLabel.Text = Localizer.T("已连接：") + type + slotText;
                if (gamepadRouterBox != null && gamepadRouterBox.Checked && slot > 0)
                {
                    gamepadStatusLabel.Text += Localizer.IsEnglish
                        ? " · Warning: games limited to XInput 0 may ignore routed output."
                        : " · 注意：只读取 0 号槽位的游戏可能仍看不到汇总后的输出。";
                    gamepadStatusLabel.ForeColor = Color.DarkOrange;
                }
                else gamepadStatusLabel.ForeColor = Color.ForestGreen;
            }
            else
            {
                gamepadStatusLabel.Text = Localizer.T("未连接。首次使用前请先连接；启动游戏前连接可避免游戏忽略热插拔设备。");
                gamepadStatusLabel.ForeColor = Color.FromArgb(86, 96, 112);
            }
        }

        private static GroupBox MakeGroup(string text)
        {
            GroupBox box = new EditorSection();
            box.Text = text;
            box.Dock = DockStyle.Top;
            box.AutoSize = false;
            box.Padding = new Padding(12, 18, 12, 10);
            box.Margin = new Padding(0, 0, 0, 12);
            box.BackColor = Color.White;
            EditorDialogs.FitSection(box);
            return box;
        }

        private static TableLayoutPanel MakeStack()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return panel;
        }

        private static CheckBox MakeCheck(string text, bool value)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = value;
            box.AutoSize = true;
            box.Margin = new Padding(0, 5, 0, 5);
            return box;
        }

        private static Button MakeDialogButton(string text)
        {
            Button button = new EditorButton();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(96, 34);
            button.Padding = new Padding(8, 0, 8, 0);
            return button;
        }
    }
}
