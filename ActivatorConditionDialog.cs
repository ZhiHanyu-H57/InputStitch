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
        private readonly bool conditionsOnly;

        private readonly NumericUpDown longPress = Number(100, 10000, 500);
        private readonly NumericUpDown doubleWindow = Number(100, 2000, 300);
        private readonly NumericUpDown shortPress = Number(50, 2000, 300);
        private readonly TextBox process = new TextBox();
        private readonly TextBox titleContains = new TextBox();
        private readonly ComboBox device = new ComboBox();
        private readonly Button refreshDevices = new EditorButton();
        private readonly CheckBox analogEnabled = new CheckBox();
        private readonly ComboBox analogControl = new EditorChoice();
        private readonly ComboBox analogDirection = new EditorChoice();
        private readonly NumericUpDown analogMin = Number(0, 100, 80);
        private readonly NumericUpDown analogMax = Number(0, 100, 100);
        private readonly Label behaviorNote = new Label();
        private TableLayoutPanel timingFields;
        private TableLayoutPanel conditionFields;
        private Label conditionSummary;

        internal ActivatorConditionDialog(MacroDefinition macro, Func<DeviceInventorySnapshot> provider, string currentLayerText)
        {
            inventoryProvider = provider;
            activator = macro == null || macro.Activator == null ? new ActivatorConfig() : macro.Activator.Clone();
            conditions = macro == null || macro.Conditions == null ? new MacroConditionConfig() : macro.Conditions.Clone();
            layerText = currentLayerText ?? "";
            edgeOnlyTrigger = macro != null && macro.Trigger != null &&
                (macro.Trigger.Kind == InputKind.WheelUp || macro.Trigger.Kind == InputKind.WheelDown);
            conditionsOnly = false;

            Text = Localizer.IsEnglish ? "Timing & conditions" : "时间与条件";
            BuildLayout();
        }

        // Conditions-only mode is used for Layer context conditions: the activator section is irrelevant for a
        // layer, so it is not added to the layout and the title explains the layer context instead.
        internal ActivatorConditionDialog(MacroConditionConfig initialConditions, Func<DeviceInventorySnapshot> provider, string currentLayerText, string titleOverride)
        {
            inventoryProvider = provider;
            activator = new ActivatorConfig();
            conditions = initialConditions == null ? new MacroConditionConfig() : initialConditions.Clone();
            layerText = currentLayerText ?? "";
            edgeOnlyTrigger = false;
            conditionsOnly = true;
            Text = string.IsNullOrWhiteSpace(titleOverride)
                ? (Localizer.IsEnglish ? "Layer context condition" : "层上下文条件")
                : titleOverride;
            BuildLayout();
        }

        private void BuildLayout()
        {
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            MinimumSize = new Size(700, 590);
            ClientSize = new Size(820, 720);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Top;
            root.AutoSize = true;
            root.Padding = new Padding(0);
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(730, 0);
            intro.Text = conditionsOnly
                ? (Localizer.IsEnglish
                    ? "This group is active while all the conditions below match. Your manually selected group returns when they stop matching."
                    : "下面的条件全部满足时，自动启用这个分组；不再满足时，返回你手动选择的分组。")
                : (Localizer.IsEnglish
                    ? "Trigger behavior is selected in the main window. Here you can adjust timing and add optional limits. Leave fields empty to run without those limits."
                    : "触发方式在主界面选择。在这里可以调整触发时间、添加生效条件；不需要的条件留空即可。");
            intro.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(intro, 0, 0);

            GroupBox activationGroup = EditorDialogs.Section(EditorDialogs.T("触发时间", "Press timing"));
            TableLayoutPanel activation = Grid();
            timingFields = activation;
            activationGroup.Controls.Add(activation);

            AddNumberRow(activation, 1, Localizer.IsEnglish ? "Hold duration before running" : "长按多久后执行", longPress, "ms");
            AddNumberRow(activation, 2, Localizer.IsEnglish ? "Maximum double-press gap" : "两次按键的最长间隔", doubleWindow, "ms");
            AddNumberRow(activation, 3, Localizer.IsEnglish ? "Maximum short-press duration" : "短按最长时长", shortPress, "ms");
            behaviorNote.AutoSize = true;
            behaviorNote.MaximumSize = new Size(650, 0);
            behaviorNote.ForeColor = Color.FromArgb(86, 96, 112);
            behaviorNote.Margin = new Padding(0, 8, 0, 0);
            activation.Controls.Add(behaviorNote, 0, 4);
            activation.SetColumnSpan(behaviorNote, 2);
            root.Controls.Add(activationGroup, 0, 1);
            activationGroup.Visible = !conditionsOnly;

            GroupBox conditionGroup = EditorDialogs.Section(EditorDialogs.T("在哪些情况下生效", "When this applies"));
            TableLayoutPanel condition = Grid();
            conditionFields = condition;
            conditionGroup.Controls.Add(condition);

            Label layer = ValueLabel(string.IsNullOrWhiteSpace(layerText)
                ? (Localizer.IsEnglish ? "Base / current macro layer" : "基础层 / 当前宏所属层")
                : layerText);
            AddRow(condition, 0, Localizer.IsEnglish ? "Group" : "所属分组", layer);
            process.Text = conditions.ForegroundProcessName ?? "";
            EditorDialogs.Hint(process, EditorDialogs.T("例如 game.exe；留空表示所有程序", "For example game.exe; blank means any program"));
            AddRow(condition, 1, Localizer.IsEnglish ? "Only in this program" : "仅在此程序中生效", process);
            titleContains.Text = conditions.ForegroundTitleContains ?? "";
            EditorDialogs.Hint(titleContains, EditorDialogs.T("例如窗口名称中的一段文字；可留空", "Part of the window title; optional"));
            AddRow(condition, 2, Localizer.IsEnglish ? "Title contains" : "窗口标题包含", titleContains);

            device.DropDownStyle = ComboBoxStyle.DropDown;
            device.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            device.AutoCompleteSource = AutoCompleteSource.ListItems;
            FlowLayoutPanel deviceLine = new FlowLayoutPanel();
            deviceLine.AutoSize = true;
            deviceLine.Dock = DockStyle.Top;
            device.Width = 325;
            refreshDevices.Text = Localizer.IsEnglish ? "Refresh" : "刷新设备";
            refreshDevices.AutoSize = true;
            refreshDevices.Click += delegate { RefreshDeviceChoices(true); };
            deviceLine.Controls.Add(device);
            deviceLine.Controls.Add(refreshDevices);
            AddRow(condition, 3, Localizer.IsEnglish ? "Source controller" : "来源手柄", deviceLine);

            analogEnabled.AutoSize = true;
            analogEnabled.Text = Localizer.IsEnglish ? "Only while a stick or trigger is in this range" : "仅在摇杆或扳机处于指定范围时生效";
            analogEnabled.Checked = conditions.Analog != null && conditions.Analog.Enabled;
            analogEnabled.CheckedChanged += delegate { UpdateAnalogUi(); };
            AddRow(condition, 4, Localizer.IsEnglish ? "Stick / trigger limit" : "摇杆 / 扳机条件", analogEnabled);

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
            AddRow(condition, 5, Localizer.IsEnglish ? "Control to check" : "检查哪个控件", analogControl);

            analogDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            analogDirection.Items.AddRange(new object[]
            {
                Localizer.IsEnglish ? "Magnitude" : "幅度",
                "+X", "-X", "+Y", "-Y"
            });
            analogDirection.SelectedIndex = Math.Max(0, Math.Min(4, (int)(conditions.Analog == null ? AnalogConditionDirection.Magnitude : conditions.Analog.Direction)));
            AddRow(condition, 6, Localizer.IsEnglish ? "Stick direction" : "摇杆方向", analogDirection);

            FlowLayoutPanel zone = new FlowLayoutPanel();
            zone.AutoSize = true;
            zone.Dock = DockStyle.Top;
            zone.Controls.Add(analogMin);
            zone.Controls.Add(ValueLabel("%  —  "));
            zone.Controls.Add(analogMax);
            zone.Controls.Add(ValueLabel("%"));
            AddRow(condition, 7, Localizer.IsEnglish ? "Strength range" : "力度范围", zone);

            Label conditionsHelp = new Label();
            conditionSummary = conditionsHelp;
            conditionsHelp.AutoSize = true;
            conditionsHelp.MaximumSize = new Size(650, 0);
            conditionsHelp.ForeColor = Color.FromArgb(86, 96, 112);
            conditionsHelp.Text = Localizer.IsEnglish
                ? "All filled conditions must match. Select a source controller to restrict which device can trigger the macro. A disconnected or unidentified controller does not match."
                : "已填写的条件需要全部满足。选择来源手柄后，仅允许该手柄触发；设备未连接或无法识别时不会触发。";
            condition.Controls.Add(conditionsHelp, 0, 8);
            condition.SetColumnSpan(conditionsHelp, 2);
            root.Controls.Add(conditionGroup, 0, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button cancel = new EditorButton();
            cancel.Text = Localizer.IsEnglish ? "Cancel" : "取消";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            Button ok = new EditorButton();
            ok.Text = EditorDialogs.T("保存条件", "Save conditions");
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
            shortPress.Value = Math.Max(shortPress.Minimum, Math.Min(shortPress.Maximum, activator.ShortPressMs));
            AnalogZoneCondition az = conditions.Analog ?? new AnalogZoneCondition();
            analogMin.Value = Math.Max(analogMin.Minimum, Math.Min(analogMin.Maximum, az.MinimumPercent));
            analogMax.Value = Math.Max(analogMax.Minimum, Math.Min(analogMax.Maximum, az.MaximumPercent));
            RefreshDeviceChoices(false);
            UpdateActivatorUi();
            UpdateAnalogUi();
            Panel viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            viewport.Controls.Add(root);
            Controls.Add(viewport);
            EditorDialogs.Frame(this, viewport, actions, intro.Text, intro);
            process.TextChanged += delegate { UpdateConditionSummary(); };
            titleContains.TextChanged += delegate { UpdateConditionSummary(); };
            device.TextChanged += delegate { UpdateConditionSummary(); };
            UpdateConditionSummary();
        }

        internal ActivatorConfig SelectedActivator { get { return activator.Clone(); } }
        internal MacroConditionConfig SelectedConditions { get { return conditions.Clone(); } }

        private void CaptureValues()
        {
            activator.LongPressMs = (int)longPress.Value;
            activator.DoublePressWindowMs = (int)doubleWindow.Value;
            activator.ShortPressMs = (int)shortPress.Value;
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
            string selected = ActivatorModes.Normalize(activator.Mode);
            longPress.Enabled = string.Equals(selected, ActivatorModes.LongPress, StringComparison.OrdinalIgnoreCase);
            doubleWindow.Enabled = string.Equals(selected, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase);
            shortPress.Enabled = string.Equals(selected, ActivatorModes.ShortPress, StringComparison.OrdinalIgnoreCase);
            EditorDialogs.RowVisible(timingFields, 1, longPress.Enabled);
            EditorDialogs.RowVisible(timingFields, 2, doubleWindow.Enabled);
            EditorDialogs.RowVisible(timingFields, 3, shortPress.Enabled);
            if (string.Equals(selected, ActivatorModes.Legacy, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish
                    ? "This trigger behavior needs no extra timing. You can set optional limits below."
                    : "当前触发方式不需要额外计时，可以直接设置下方生效条件。";
            else if (string.Equals(selected, ActivatorModes.WhileHeld, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish
                    ? "Releasing the shortcut stops this macro. Other macros keep running."
                    : "松开触发键会停止这个宏，不影响其他正在运行的宏。";
            else if (string.Equals(selected, ActivatorModes.LongPress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = EditorDialogs.T("按住触发键达到上面的时间后，执行一次。期间需要一直满足所填条件。", "Hold the shortcut for the time above to run once. Any conditions you set must remain true while holding.");
            else if (string.Equals(selected, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = EditorDialogs.T("两次按下的间隔不超过上面的时间，在第二次按下时执行。", "Press twice within the time above. The second press starts the macro.");
            else if (string.Equals(selected, ActivatorModes.ShortPress, StringComparison.OrdinalIgnoreCase))
                behaviorNote.Text = Localizer.IsEnglish
                    ? "Release the shortcut within the time above to run once. Holding it longer will not run this macro."
                    : "在上面的时间内松开触发键，执行一次；超过时间再松开则不执行。";
            else
                behaviorNote.Text = Localizer.IsEnglish ? "The macro starts at the moment selected in the main window." : "这个宏会在主界面选择的时机开始执行。";
            behaviorNote.Text += EditorDialogs.T("\r\n1000 毫秒 = 1 秒。这里只调整当前触发方式需要的时间。", "\r\n1000 ms = 1 second. Only timing used by your selected trigger behavior is shown.");
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
            if (conditionFields != null)
            {
                EditorDialogs.RowVisible(conditionFields, 5, enabled);
                EditorDialogs.RowVisible(conditionFields, 6, enabled && stick);
                EditorDialogs.RowVisible(conditionFields, 7, enabled);
            }
            UpdateConditionSummary();
        }

        private void UpdateConditionSummary()
        {
            if (conditionSummary == null) return;
            int count = (string.IsNullOrWhiteSpace(process.Text) ? 0 : 1) + (string.IsNullOrWhiteSpace(titleContains.Text) ? 0 : 1) +
                (device.SelectedIndex == 0 || string.IsNullOrWhiteSpace(device.Text) ? 0 : 1) + (analogEnabled.Checked ? 1 : 0);
            conditionSummary.Text = count == 0
                ? EditorDialogs.T("未设置限制：可以在任何程序中生效。需要限制时再填写，留空表示不限制。", "No limits set: available in any program. Fill in only the limits you need; blank fields mean no restriction.")
                : EditorDialogs.T("已设置 " + count + " 项限制，需要全部满足才生效。来源手柄离线或无法识别时不生效。", count + " limits set. All must match. A selected source controller must be connected and identifiable.");
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
