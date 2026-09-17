using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class RouterSourceDialog : Form
    {
        private sealed class DeviceChoice
        {
            public string DeviceKey = "";
            public string Text = "";
            public override string ToString() { return Text; }
        }

        private readonly Func<DeviceInventorySnapshot> inventoryProvider;
        private readonly RouterSourcePolicyConfig working;
        private readonly RadioButton allVisible = new RadioButton();
        private readonly RadioButton selectedOnly = new RadioButton();
        private readonly CheckedListBox devices = new CheckedListBox();
        private readonly Label status = new Label();
        private readonly Button refresh = new Button();
        private readonly Button ok = new Button();
        private readonly Button cancel = new Button();

        internal RouterSourceDialog(RouterSourcePolicyConfig current, Func<DeviceInventorySnapshot> provider)
        {
            inventoryProvider = provider;
            working = current == null ? new RouterSourcePolicyConfig() : current.Clone();

            Text = Localizer.IsEnglish ? "Router source selection" : "手柄汇总来源";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(680, 500);
            ClientSize = new Size(760, 560);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(710, 0);
            intro.Text = Localizer.IsEnglish
                ? "Router source policy never treats XInput slot 0/1/2/3 as persistent identity. Selected-device mode routes a controller only when InputStitch can prove which stable DeviceKey owns the current runtime source. Unresolved sources fail closed."
                : "来源策略不会把 XInput 0/1/2/3 槽位当成持久设备身份。“仅已选设备”模式只会转发能够可靠对应到稳定 DeviceKey 的运行时来源；无法确认身份的来源会停止转发，而不是猜测。";
            intro.Margin = new Padding(0, 0, 0, 12);
            root.Controls.Add(intro, 0, 0);

            allVisible.AutoSize = true;
            allVisible.Text = Localizer.IsEnglish
                ? "Route all visible external XInput sources (current/default behavior)"
                : "转发所有可见的外部 XInput 来源（当前/默认行为）";
            allVisible.Checked = string.Equals(working.Mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase);
            root.Controls.Add(allVisible, 0, 1);

            selectedOnly.AutoSize = true;
            selectedOnly.Text = Localizer.IsEnglish
                ? "Route only the selected stable devices"
                : "仅转发下面勾选的稳定设备";
            selectedOnly.Checked = !allVisible.Checked;
            selectedOnly.Margin = new Padding(0, 6, 0, 6);
            root.Controls.Add(selectedOnly, 0, 2);

            devices.Dock = DockStyle.Fill;
            devices.CheckOnClick = true;
            devices.IntegralHeight = false;
            root.Controls.Add(devices, 0, 3);

            status.AutoSize = true;
            status.MaximumSize = new Size(710, 0);
            status.ForeColor = Color.FromArgb(86, 96, 112);
            status.Margin = new Padding(0, 8, 0, 8);
            root.Controls.Add(status, 0, 4);

            Label takeoverNote = new Label();
            takeoverNote.AutoSize = true;
            takeoverNote.MaximumSize = new Size(710, 0);
            takeoverNote.ForeColor = Color.DarkOrange;
            takeoverNote.Text = Localizer.IsEnglish
                ? "Experimental Controller Takeover currently requires the default 'all visible sources' policy. This prevents a hidden original controller from being omitted from Router output."
                : "实验性的“手柄接管”目前仍要求使用“转发所有可见来源”。这样可以避免原手柄被隐藏后，却因为来源策略没有被转发到虚拟手柄。";
            root.Controls.Add(takeoverNote, 0, 5);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            cancel.Text = Localizer.IsEnglish ? "Cancel" : "取消";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.AutoSize = true;
            actions.Controls.Add(cancel);
            ok.Text = Localizer.IsEnglish ? "OK" : "确定";
            ok.DialogResult = DialogResult.OK;
            ok.AutoSize = true;
            ok.Click += delegate { CapturePolicy(); };
            actions.Controls.Add(ok);
            refresh.Text = Localizer.IsEnglish ? "Refresh devices" : "刷新设备";
            refresh.AutoSize = true;
            refresh.Click += delegate { RefreshInventory(); };
            actions.Controls.Add(refresh);
            root.Controls.Add(actions, 0, 6);

            AcceptButton = ok;
            CancelButton = cancel;
            allVisible.CheckedChanged += delegate { UpdateEnabledState(); };
            selectedOnly.CheckedChanged += delegate { UpdateEnabledState(); };
            Shown += delegate { RefreshInventory(); };
            UpdateEnabledState();
        }

        internal RouterSourcePolicyConfig SelectedPolicy
        {
            get { return working.Clone(); }
        }

        private void UpdateEnabledState()
        {
            devices.Enabled = selectedOnly.Checked;
        }

        private void RefreshInventory()
        {
            HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (devices.Items.Count == 0)
            {
                foreach (string key in working.SelectedDeviceKeys ?? new List<string>())
                    if (!string.IsNullOrWhiteSpace(key)) selected.Add(key);
            }
            else
            {
                // On subsequent refreshes the checkboxes are the user's current edit state. Do not
                // re-add keys from the original policy or an intentionally unchecked device would
                // silently become checked again after pressing Refresh devices.
                foreach (object checkedItem in devices.CheckedItems)
                {
                    DeviceChoice oldChoice = checkedItem as DeviceChoice;
                    if (oldChoice != null && !string.IsNullOrWhiteSpace(oldChoice.DeviceKey)) selected.Add(oldChoice.DeviceKey);
                }
            }

            devices.Items.Clear();
            DeviceInventorySnapshot snapshot = null;
            string error = "";
            try { snapshot = inventoryProvider == null ? null : inventoryProvider(); }
            catch (Exception ex) { error = ex.Message; }

            HashSet<string> listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int unresolved = 0;
            if (snapshot != null && snapshot.Items != null)
            {
                foreach (DeviceInventoryItem item in snapshot.Items)
                {
                    if (item == null) continue;
                    if (item.Role == DeviceInventoryRole.UnresolvedXInputSource) { unresolved++; continue; }
                    if (item.VirtualBus || string.IsNullOrWhiteSpace(item.DeviceKey)) continue;
                    if (item.Role != DeviceInventoryRole.PersistentGamingDevice && item.Role != DeviceInventoryRole.KnownOffline) continue;
                    if (!listed.Add(item.DeviceKey)) continue;

                    string suffix = item.XInputSlot >= 0 ? "  [XInput " + item.XInputSlot.ToString() + "]" : "";
                    if (!item.Present) suffix += Localizer.IsEnglish ? "  (offline)" : "  （离线）";
                    else if (item.XInputSlot < 0) suffix += Localizer.IsEnglish ? "  (runtime XInput unresolved)" : "  （当前未解析到 XInput）";
                    DeviceChoice choice = new DeviceChoice
                    {
                        DeviceKey = item.DeviceKey,
                        Text = item.DisplayName + suffix + "  —  " + ShortKey(item.DeviceKey)
                    };
                    devices.Items.Add(choice, selected.Contains(item.DeviceKey));
                }
            }

            foreach (string key in selected)
            {
                if (string.IsNullOrWhiteSpace(key) || listed.Contains(key)) continue;
                DeviceChoice missing = new DeviceChoice
                {
                    DeviceKey = key,
                    Text = (Localizer.IsEnglish ? "Previously selected device" : "此前已选择的设备") + "  —  " + ShortKey(key)
                };
                devices.Items.Add(missing, true);
            }

            if (!string.IsNullOrWhiteSpace(error))
                status.Text = (Localizer.IsEnglish ? "Device inventory refresh failed: " : "设备清单刷新失败：") + error;
            else if (snapshot == null)
                status.Text = Localizer.IsEnglish ? "Device inventory is unavailable." : "当前无法读取设备清单。";
            else
                status.Text = (Localizer.IsEnglish ? "Stable choices: " : "可选择的稳定设备：") + listed.Count.ToString() +
                    (Localizer.IsEnglish ? "; unresolved XInput sources: " : "；未解析 XInput 来源：") + unresolved.ToString() +
                    (unresolved == 0 ? "" : (Localizer.IsEnglish
                        ? ". Unresolved sources are not routed in selected-device mode."
                        : "。在“仅已选设备”模式下，未解析来源不会被转发。"));
        }

        private void CapturePolicy()
        {
            working.Mode = selectedOnly.Checked ? RouterSourceModes.SelectedDevices : RouterSourceModes.AllVisible;
            working.SelectedDeviceKeys = new List<string>();
            foreach (object item in devices.CheckedItems)
            {
                DeviceChoice choice = item as DeviceChoice;
                if (choice == null || string.IsNullOrWhiteSpace(choice.DeviceKey)) continue;
                working.SelectedDeviceKeys.Add(choice.DeviceKey);
            }
            RouterSourcePolicyConfig.Normalize(working);
        }

        private static string ShortKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "—";
            string text = key.Trim();
            return text.Length <= 18 ? text : text.Substring(0, 10) + "…" + text.Substring(text.Length - 6);
        }
    }
}
