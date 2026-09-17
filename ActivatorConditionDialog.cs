using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class ActivatorConditionDialog : Form
    {
        private sealed class DeviceChoice
        {
            public string DeviceKey = "";
            public string Text = "";
            public override string ToString() { return Text; }
        }

        private readonly Func<DeviceInventorySnapshot> inventoryProvider;
        private readonly ActivatorConfig activator;
        private readonly MacroConditionConfig conditions;
        private readonly string layerText;
        private readonly bool edgeOnlyTrigger;

        private readonly ComboBox mode = new ComboBox();
        private readonly NumericUpDown longPress = Number(100, 10000, 500);
        private readonly NumericUpDown doubleWindow = Number(100, 2000, 300);
        private readonly TextBox process = new TextBox();
        private readonly TextBox titleContains = new TextBox();
        private readonly ComboBox device = new ComboBox();
        private readonly Button refreshDevices = new Button();
        private readonly CheckBox analogEnabled = new CheckBox();
        private readonly ComboBox analogControl = new ComboBox();
        private readonly ComboBox analogDirection = new ComboBox();
        private readonly NumericUpDown analogMin = Number(0, 100, 80);
        private readonly NumericUpDown analogMax = Number(0, 100, 100);
        private readonly Label behaviorNote = new Label();

        internal ActivatorConditionDialog(MacroDefinition macro, Func<DeviceInventorySnapshot> provider, string currentLayerText)
        {
            inventoryProvider = provider;
            activator = macro == null || macro.Activator == null ? new ActivatorConfig() : macro.Activator.Clone();
            conditions = macro == null || macro.Conditions == null ? new MacroConditionConfig() : macro.Conditions.Clone();
            layerText = currentLayerText ?? "";
            edgeOnlyTrigger = macro != null && macro.Trigger != null &&
                (macro.Trigger.Kind == InputKind.WheelUp || macro.Trigger.Kind == InputKind.WheelDown);

            Text = Localizer.IsEnglish ? "Activation & conditions" : "激活与条件";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(700, 590);
            ClientSize = new Size(780, 690);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(730, 0);
            intro.Text = Localizer.IsEnglish
                ? "Advanced activation is opt-in. 'Legacy' preserves the existing Toggle/Hold behavior exactly. Additional conditions are AND gates. The macro still runs through the normal concurrent runtime and Output Ownership."
                : "高级激活规则需要主动启用。“兼容旧方式”会严格保留现有 Toggle / Hold 行为。下面的附加条件按 AND 同时判断；宏本身仍使用现有并发运行时和 Output Ownership。";
            intro.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(intro, 0, 0);

            GroupBox activationGroup = new GroupBox();
            activationGroup.Text = Localizer.IsEnglish ? "Activator" : "激活方式";
            activationGroup.AutoSize = true;
            activationGroup.Dock = DockStyle.Top;
            activationGroup.Padding = new Padding(10);
            TableLayoutPanel activation = Grid();
            activationGroup.Controls.Add(activation);

            mode.DropDownStyle = ComboBoxStyle.DropDownList;
            mode.Items.AddRange(new object[]
            {
                Localizer.IsEnglish ? "Legacy (use current Toggle/Hold)" : "兼容旧方式（使用当前 Toggle / Hold）",
                Localizer.IsEnglish ? "On Press" : "按下时",
                Localizer.IsEnglish ? "On Release" : "松开时",
                Localizer.IsEnglish ? "While Held" : "按住期间",
                Localizer.IsEnglish ? "Long Press" : "长按",
                Localizer.IsEnglish ? "Double Press" : "双击"
            });
            mode.SelectedIndex = ModeIndex(activator.Mode);
            mode.SelectedIndexChanged += delegate { UpdateActivatorUi(); };
            AddRow(activation, 0, Localizer.IsEnglish ? "Mode" : "方式", mode);
            AddNumberRow(activation, 1, Localizer.IsEnglish ? "Long-press threshold" : "长按阈值", longPress, "ms");
            AddNumberRow(activation, 2, Localizer.IsEnglish ? "Double-press window" : "双击间隔", doubleWindow, "ms");
            behaviorNote.AutoSize = true;
            behaviorNote.MaximumSize = new Size(650, 0);
            behaviorNote.ForeColor = Color.FromArgb(86, 96, 112);
            behaviorNote.Margin = new Padding(0, 8, 0, 0);
            activation.Controls.Add(behaviorNote, 0, 3);
            activation.SetColumnSpan(behaviorNote, 2);
            root.Controls.Add(activationGroup, 0, 1);

            GroupBox conditionGroup = new GroupBox();
            conditionGroup.Text = Localizer.IsEnglish ? "Conditions (all must match)" : "条件（全部满足才生效）";
            conditionGroup.Dock = DockStyle.Fill;
            conditionGroup.Padding = new Padding(10);
            TableLayoutPanel condition = Grid();
            condition.AutoScroll = true;
            conditionGroup.Controls.Add(condition);

            Label layer = ValueLabel(string.IsNullOrWhiteSpace(layerText)
                ? (Localizer.IsEnglish ? "Base / current macro layer" : "基础层 / 当前宏所属层")
                : layerText);
            AddRow(condition, 0, Localizer.IsEnglish ? "Layer" : "映射层", layer);
            process.Text = conditions.ForegroundProcessName ?? "";
            AddRow(condition, 1, Localizer.IsEnglish ? "Foreground process" : "前台进程", process);
            titleContains.Text = conditions.ForegroundTitleContains ?? "";
            AddRow(condition, 2, Localizer.IsEnglish ? "Title contains" : "窗口标题包含", titleContains);

            device.DropDownStyle = ComboBoxStyle.DropDown;
            device.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            device.AutoCompleteSource = AutoCompleteSource.ListItems;
            FlowLayoutPanel deviceLine = new FlowLayoutPanel();
            deviceLine.AutoSize = true;
            deviceLine.Dock = DockStyle.Top;
            device.Width = 400;
            refreshDevices.Text = Localizer.IsEnglish ? "Refresh" : "刷新设备";
            refreshDevices.AutoSize = true;
            refreshDevices.Click += delegate { RefreshDeviceChoices(true); };
            deviceLine.Controls.Add(device);
            deviceLine.Controls.Add(refreshDevices);
            AddRow(condition, 3, Localizer.IsEnglish ? "DeviceKey" : "来源设备", deviceLine);

            analogEnabled.AutoSize = true;
            analogEnabled.Text = Localizer.IsEnglish ? "Require an analog zone" : "要求模拟量位于指定区间";
            analogEnabled.Checked = conditions.Analog != null && conditions.Analog.Enabled;
            analogEnabled.CheckedChanged += delegate { UpdateAnalogUi(); };
            AddRow(condition, 4, Localizer.IsEnglish ? "Analog gate" : "模拟量条件", analogEnabled);

            analogControl.DropDownStyle = ComboBoxStyle.DropDownList;
            analogControl.Items.AddRange(new object[]
            {
                Localizer.IsEnglish ? "Left stick" : "左摇杆",
                Localizer.IsEnglish ? "Right stick" : "右摇杆",
                Localizer.IsEnglish ? "Left trigger" : "左扳机",
                Localizer.IsEnglish ? "Right trigger" : "右扳机"
            });
            analogControl.SelectedIndex = ControlIndex(conditions.Analog == null ? GamepadControl.LeftTrigger : conditions.Analog.Control);
            analogControl.SelectedIndexChanged += delegate { UpdateAnalogUi(); };
            AddRow(condition, 5, Localizer.IsEnglish ? "Analog control" : "模拟量来源", analogControl);

            analogDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            analogDirection.Items.AddRange(new object[]
            {
                Localizer.IsEnglish ? "Magnitude" : "幅度",
                "+X", "-X", "+Y", "-Y"
            });
            analogDirection.SelectedIndex = Math.Max(0, Math.Min(4, (int)(conditions.Analog == null ? AnalogConditionDirection.Magnitude : conditions.Analog.Direction)));
            AddRow(condition, 6, Localizer.IsEnglish ? "Direction" : "方向 / 半轴", analogDirection);

            FlowLayoutPanel zone = new FlowLayoutPanel();
            zone.AutoSize = true;
            zone.Dock = DockStyle.Top;
            zone.Controls.Add(analogMin);
            zone.Controls.Add(ValueLabel("%  —  "));
            zone.Controls.Add(analogMax);
            zone.Controls.Add(ValueLabel("%"));
            AddRow(condition, 7, Localizer.IsEnglish ? "Accepted range" : "有效区间", zone);

            Label conditionsHelp = new Label();
            conditionsHelp.AutoSize = true;
            conditionsHelp.MaximumSize = new Size(650, 0);
            conditionsHelp.ForeColor = Color.FromArgb(86, 96, 112);
            conditionsHelp.Text = Localizer.IsEnglish
                ? "Leave process/title/DeviceKey empty to ignore them. DeviceKey uses the stable Device Identity layer and fails closed when the runtime source cannot be proven. Analog zone is an additional condition, not a new stick-as-trigger mode."
                : "进程、标题、DeviceKey 留空即不限制。DeviceKey 使用稳定设备身份；运行时无法证明设备对应关系时会安全地判定为不满足。模拟量区间只是附加条件，不是新的“摇杆直接作为触发键”模式。";
            condition.Controls.Add(conditionsHelp, 0, 8);
            condition.SetColumnSpan(conditionsHelp, 2);
            root.Controls.Add(conditionGroup, 0, 2);

            Label deferred = new Label();
            deferred.AutoSize = true;
            deferred.MaximumSize = new Size(730, 0);
            deferred.ForeColor = Color.DarkOrange;
            deferred.Text = Localizer.IsEnglish
                ? "Stage 1 deliberately does not emulate Turbo/repeat or arbitrary stick-region triggers. Those need explicit semantics rather than another hidden execution path. Infinite one-shot macros still require the normal Stop button or Emergency Stop."
                : "第一阶段不会假装实现 Turbo / 连发，也不会把任意摇杆区域直接当触发器。这些功能需要先定义清楚语义，不能偷偷再造一套执行逻辑。一次性激活的无限宏仍需使用原有“停止”按钮或紧急停止。";
            deferred.Margin = new Padding(0, 8, 0, 8);
            root.Controls.Add(deferred, 0, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button cancel = new Button();
            cancel.Text = Localizer.IsEnglish ? "Cancel" : "取消";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            Button ok = new Button();
            ok.Text = Localizer.IsEnglish ? "OK" : "确定";
            ok.AutoSize = true;
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate { CaptureValues(); };
            actions.Controls.Add(cancel);
            actions.Controls.Add(ok);
            root.Controls.Add(actions, 0, 4);
            AcceptButton = ok;
            CancelButton = cancel;

            longPress.Value = Math.Max(longPress.Minimum, Math.Min(longPress.Maximum, activator.LongPressMs));
            doubleWindow.Value = Math.Max(doubleWindow.Minimum, Math.Min(doubleWindow.Maximum, activator.DoublePressWindowMs));
            AnalogZoneCondition az = conditions.Analog ?? new AnalogZoneCondition();
            analogMin.Value = Math.Max(analogMin.Minimum, Math.Min(analogMin.Maximum, az.MinimumPercent));
            analogMax.Value = Math.Max(analogMax.Minimum, Math.Min(analogMax.Maximum, az.MaximumPercent));
            RefreshDeviceChoices(false);
            UpdateActivatorUi();
            UpdateAnalogUi();
        }

        internal ActivatorConfig SelectedActivator { get { return activator.Clone(); } }
        internal MacroConditionConfig SelectedConditions { get { return conditions.Clone(); } }

        private void CaptureValues()
        {
            activator.Mode = ModeFromIndex(mode.SelectedIndex);
            activator.LongPressMs = (int)longPress.Value;
            activator.DoublePressWindowMs = (int)doubleWindow.Value;
            ActivatorConfig.Normalize(activator);

            conditions.ForegroundProcessName = (process.Text ?? "").Trim();
            conditions.ForegroundTitleContains = (titleContains.Text ?? "").Trim();
            DeviceChoice selected = device.SelectedItem as DeviceChoice;
            conditions.DeviceKey = selected != null ? selected.DeviceKey : ParseDeviceKey(device.Text);
            if (conditions.Analog == null) conditions.Analog = new AnalogZoneCondition();
            conditions.Analog.Enabled = analogEnabled.Checked;
            conditions.Analog.Control = ControlFromIndex(analogControl.SelectedIndex);
            conditions.Analog.Direction = (AnalogConditionDirection)Math.Max(0, Math.Min(4, analogDirection.SelectedIndex));
            conditions.Analog.MinimumPercent = (int)analogMin.Value;
            conditions.Analog.MaximumPercent = (int)analogMax.Value;
            MacroConditionConfig.Normalize(conditions);
        }

        private void UpdateActivatorUi()
        {
            string selected = ModeFromIndex(mode.SelectedIndex);
            longPress.Enabled = string.Equals(selected, ActivatorModes.LongPress, StringComparison.OrdinalIgnoreCase);
            doubleWindow.Enabled = string.Equals(selected, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(selected, ActivatorModes.Legacy, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish
                    ? "Legacy mode does not alter the current Toggle/Hold setting."
                    : "兼容旧方式不会改变当前的 Toggle / Hold 设置。";
            else if (string.Equals(selected, ActivatorModes.WhileHeld, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish
                    ? "While Held uses the existing Hold runtime. Releasing the trigger or losing an active condition stops only this macro's source."
                    : "按住期间使用现有 Hold 运行时。松开触发键或活动条件失效时，只停止这个宏自己的输出来源。";
            else if (string.Equals(selected, ActivatorModes.LongPress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish ? "Long Press fires once after the threshold while the trigger and conditions remain valid." : "长按会在触发键持续按住且条件一直满足时，于阈值到达后触发一次。";
            else if (string.Equals(selected, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish ? "Double Press fires on the second press within the configured window." : "双击会在第二次按下发生在设定时间窗内时触发。";
            else
                behaviorNote.Text = Localizer.IsEnglish ? "This mode starts the macro once at the selected physical edge." : "该方式会在指定的物理边沿触发宏一次。";
            if (edgeOnlyTrigger && !string.Equals(selected, ActivatorModes.Legacy, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(selected, ActivatorModes.Press, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(selected, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text += Localizer.IsEnglish
                    ? "  The current wheel trigger has no held/release state, so this mode will not fire until you choose a persistent trigger."
                    : "　当前触发键是滚轮方向，没有持续按住/松开状态；在改成可持续按住的触发键之前，该方式不会触发。";
        }

        private void UpdateAnalogUi()
        {
            bool enabled = analogEnabled.Checked;
            analogControl.Enabled = enabled;
            analogMin.Enabled = enabled;
            analogMax.Enabled = enabled;
            bool stick = analogControl.SelectedIndex == 0 || analogControl.SelectedIndex == 1;
            analogDirection.Enabled = enabled && stick;
            if (!stick) analogDirection.SelectedIndex = 0;
        }

        private void RefreshDeviceChoices(bool preserveEdit)
        {
            string current = preserveEdit ? ParseDeviceKey(device.Text) : (conditions.DeviceKey ?? "").Trim();
            device.Items.Clear();
            device.Items.Add(new DeviceChoice { DeviceKey = "", Text = Localizer.IsEnglish ? "(any / no device condition)" : "（任意设备 / 不限制设备）" });
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DeviceInventorySnapshot snapshot = null;
            try { snapshot = inventoryProvider == null ? null : inventoryProvider(); } catch { }
            if (snapshot != null && snapshot.Items != null)
            {
                foreach (DeviceInventoryItem item in snapshot.Items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.DeviceKey) || item.VirtualBus) continue;
                    if (item.Role != DeviceInventoryRole.PersistentGamingDevice && item.Role != DeviceInventoryRole.KnownOffline) continue;
                    if (!seen.Add(item.DeviceKey)) continue;
                    string suffix = item.Present ? "" : (Localizer.IsEnglish ? " (offline)" : "（离线）");
                    device.Items.Add(new DeviceChoice
                    {
                        DeviceKey = item.DeviceKey,
                        Text = item.DisplayName + suffix + " — " + item.DeviceKey
                    });
                }
            }
            if (current.Length == 0) { device.SelectedIndex = 0; return; }
            for (int i = 1; i < device.Items.Count; i++)
            {
                DeviceChoice choice = device.Items[i] as DeviceChoice;
                if (choice != null && string.Equals(choice.DeviceKey, current, StringComparison.OrdinalIgnoreCase))
                { device.SelectedIndex = i; return; }
            }
            device.Text = current;
        }

        private static string ParseDeviceKey(string text)
        {
            string value = (text ?? "").Trim();
            int separator = value.LastIndexOf(" — ", StringComparison.Ordinal);
            if (separator >= 0 && separator + 3 < value.Length) value = value.Substring(separator + 3).Trim();
            if (value.StartsWith("(", StringComparison.Ordinal) || value.StartsWith("（", StringComparison.Ordinal)) return "";
            return value;
        }

        private static int ModeIndex(string value)
        {
            string mode = ActivatorModes.Normalize(value);
            if (mode == ActivatorModes.Press) return 1;
            if (mode == ActivatorModes.Release) return 2;
            if (mode == ActivatorModes.WhileHeld) return 3;
            if (mode == ActivatorModes.LongPress) return 4;
            if (mode == ActivatorModes.DoublePress) return 5;
            return 0;
        }

        private static string ModeFromIndex(int index)
        {
            if (index == 1) return ActivatorModes.Press;
            if (index == 2) return ActivatorModes.Release;
            if (index == 3) return ActivatorModes.WhileHeld;
            if (index == 4) return ActivatorModes.LongPress;
            if (index == 5) return ActivatorModes.DoublePress;
            return ActivatorModes.Legacy;
        }

        private static int ControlIndex(GamepadControl control)
        {
            if (control == GamepadControl.LeftStick) return 0;
            if (control == GamepadControl.RightStick) return 1;
            if (control == GamepadControl.RightTrigger) return 3;
            return 2;
        }

        private static GamepadControl ControlFromIndex(int index)
        {
            if (index == 0) return GamepadControl.LeftStick;
            if (index == 1) return GamepadControl.RightStick;
            if (index == 3) return GamepadControl.RightTrigger;
            return GamepadControl.LeftTrigger;
        }

        private static TableLayoutPanel Grid()
        {
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        private static void AddRow(TableLayoutPanel table, int row, string title, Control value)
        {
            table.RowCount = Math.Max(table.RowCount, row + 1);
            Label label = ValueLabel(title);
            label.Margin = new Padding(0, 6, 10, 6);
            value.Dock = value is FlowLayoutPanel ? DockStyle.Top : DockStyle.Fill;
            value.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(value, 1, row);
        }

        private static void AddNumberRow(TableLayoutPanel table, int row, string title, NumericUpDown value, string unit)
        {
            FlowLayoutPanel line = new FlowLayoutPanel();
            line.AutoSize = true;
            line.Dock = DockStyle.Top;
            value.Width = 110;
            line.Controls.Add(value);
            Label suffix = ValueLabel(unit);
            suffix.Margin = new Padding(4, 6, 0, 0);
            line.Controls.Add(suffix);
            AddRow(table, row, title, line);
        }

        private static NumericUpDown Number(int min, int max, int value)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.Value = Math.Max(min, Math.Min(max, value));
            return box;
        }

        private static Label ValueLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            return label;
        }
    }
}
