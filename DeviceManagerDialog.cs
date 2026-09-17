using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace InputStitch
{
    // Read-only first-stage Device Manager. It exposes what InputStitch actually knows and, just as
    // importantly, what it cannot yet correlate safely. Editing Router/device policy belongs to the
    // next platform phase and is intentionally not mixed into identity discovery.
    internal sealed class DeviceManagerDialog : Form
    {
        private readonly Func<DeviceInventorySnapshot> refreshInventory;
        private readonly Label summaryLabel = new Label();
        private readonly DataGridView grid = new DataGridView();
        private readonly Button refreshButton = new Button();
        private readonly Button closeButton = new Button();

        internal DeviceManagerDialog(Func<DeviceInventorySnapshot> refresh)
        {
            refreshInventory = refresh;
            Text = Localizer.IsEnglish ? "Device Manager" : "设备管理器";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 520);
            Size = new Size(1120, 650);
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(1050, 0);
            intro.Text = Localizer.IsEnglish
                ? "DeviceKey is durable identity. XInput 0/1/2/3 is runtime state and may change after reconnect or re-enumeration. InputStitch never persists a slot number as device identity. Unresolved XInput rows are intentionally left unresolved instead of guessing. Virtual-bus metadata is labeled separately and is not treated as proof that a provider row is InputStitch's own output."
                : "DeviceKey 表示持久设备身份；XInput 0/1/2/3 只是运行时槽位，重连或重新枚举后可能变化。InputStitch 不会把槽位号保存成设备身份。无法可靠对应到具体设备的 XInput 来源会明确显示为“未解析”，不会猜测；虚拟总线设备也会单独标注，不会仅凭这一点就认定它是 InputStitch 自己的输出。";
            root.Controls.Add(intro, 0, 0);

            summaryLabel.AutoSize = true;
            summaryLabel.MaximumSize = new Size(1050, 0);
            summaryLabel.Margin = new Padding(0, 10, 0, 10);
            root.Controls.Add(summaryLabel, 0, 1);

            ConfigureGrid();
            root.Controls.Add(grid, 0, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            closeButton.AutoSize = true;
            closeButton.Text = Localizer.IsEnglish ? "Close" : "关闭";
            closeButton.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(closeButton);
            refreshButton.AutoSize = true;
            refreshButton.Text = Localizer.IsEnglish ? "Refresh" : "刷新设备";
            refreshButton.Click += delegate { RefreshDevices(); };
            actions.Controls.Add(refreshButton);
            root.Controls.Add(actions, 0, 3);
            CancelButton = closeButton;

            Shown += delegate { RefreshDevices(); };
        }

        private void ConfigureGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = false;
            grid.RowHeadersVisible = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;

            AddColumn("status", Localizer.IsEnglish ? "Status" : "状态", 80);
            AddColumn("device", Localizer.IsEnglish ? "Device" : "设备", 185);
            AddColumn("role", Localizer.IsEnglish ? "Role" : "角色", 150);
            AddColumn("key", "DeviceKey", 270);
            AddColumn("slot", "XInput", 70);
            AddColumn("basis", Localizer.IsEnglish ? "Identity basis" : "身份依据", 125);
            AddColumn("vidpid", "VID:PID", 90);
            AddColumn("last", Localizer.IsEnglish ? "Last seen" : "最近见到", 130);
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }

        private void AddColumn(string name, string header, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(column);
        }

        private void RefreshDevices()
        {
            refreshButton.Enabled = false;
            try
            {
                DeviceInventorySnapshot snapshot = refreshInventory == null ? null : refreshInventory();
                Populate(snapshot);
            }
            catch (Exception ex)
            {
                summaryLabel.Text = (Localizer.IsEnglish ? "Device inventory refresh failed: " : "刷新设备清单失败：") + ex.Message;
                summaryLabel.ForeColor = Color.Firebrick;
            }
            finally { refreshButton.Enabled = true; }
        }

        private void Populate(DeviceInventorySnapshot snapshot)
        {
            grid.Rows.Clear();
            if (snapshot == null)
            {
                summaryLabel.Text = Localizer.IsEnglish ? "Device inventory is unavailable in this host." : "当前环境无法读取设备清单。";
                summaryLabel.ForeColor = Color.Firebrick;
                return;
            }

            summaryLabel.Text = snapshot.DiscoveryStatus + "\r\n" +
                (Localizer.IsEnglish ? "Registry: " : "设备身份库：") + snapshot.RegistryStatus +
                (snapshot.RegistryWritable ? "" : (Localizer.IsEnglish ? " (read-only for this run)" : "（本次运行只读）"));
            summaryLabel.ForeColor = snapshot.StableMetadataAvailable ? Color.ForestGreen : Color.DarkOrange;

            foreach (DeviceInventoryItem item in snapshot.Items)
            {
                int rowIndex = grid.Rows.Add(
                    StatusText(item),
                    item.DisplayName,
                    RoleText(item),
                    string.IsNullOrWhiteSpace(item.DeviceKey) ? "—" : item.DeviceKey,
                    item.XInputSlot >= 0 ? item.XInputSlot.ToString(CultureInfo.InvariantCulture) : "—",
                    BasisText(item),
                    VidPidText(item),
                    item.LastSeenUtc == default(DateTime) ? "—" : item.LastSeenUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                grid.Rows[rowIndex].Tag = item;
            }
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            DeviceInventoryItem item = grid.Rows[e.RowIndex].Tag as DeviceInventoryItem;
            if (item == null) return;
            StringBuilder detail = new StringBuilder();
            detail.AppendLine("DeviceKey: " + (string.IsNullOrWhiteSpace(item.DeviceKey) ? "—" : item.DeviceKey));
            detail.AppendLine((Localizer.IsEnglish ? "Provider: " : "来源：") + item.Provider);
            detail.AppendLine((Localizer.IsEnglish ? "Identity basis: " : "身份依据：") + item.IdentityBasis);
            if (!string.IsNullOrWhiteSpace(item.SerialNumber)) detail.AppendLine("Serial: " + item.SerialNumber);
            if (!string.IsNullOrWhiteSpace(item.ContainerId)) detail.AppendLine("Container ID: " + item.ContainerId);
            if (!string.IsNullOrWhiteSpace(item.BaseContainerDeviceInstancePath)) detail.AppendLine("Container path: " + item.BaseContainerDeviceInstancePath);
            if (item.VirtualBus) detail.AppendLine(Localizer.IsEnglish ? "Virtual bus: detected" : "虚拟总线：已检测");
            if (!string.IsNullOrWhiteSpace(item.XusbDeviceInstancePath)) detail.AppendLine("XUSB: " + item.XusbDeviceInstancePath);
            if (!string.IsNullOrWhiteSpace(item.DeviceInstancePath)) detail.AppendLine("PnP/HID: " + item.DeviceInstancePath);
            e.ToolTipText = detail.ToString().TrimEnd();
        }

        private static string StatusText(DeviceInventoryItem item)
        {
            if (item == null) return "—";
            if (item.Role == DeviceInventoryRole.KnownOffline) return Localizer.IsEnglish ? "Offline" : "离线";
            if (item.Role == DeviceInventoryRole.UnresolvedXInputSource) return Localizer.IsEnglish ? "Present" : "在线";
            return item.Present ? (Localizer.IsEnglish ? "Present" : "在线") : (Localizer.IsEnglish ? "Offline" : "离线");
        }

        private static string RoleText(DeviceInventoryItem item)
        {
            if (item == null) return "—";
            if (item.Role == DeviceInventoryRole.InputStitchVirtual)
                return Localizer.IsEnglish ? "InputStitch virtual output" : "InputStitch 虚拟输出";
            if (item.Role == DeviceInventoryRole.PersistentGamingDevice)
                return Localizer.IsEnglish ? "Discovered gaming device" : "已发现游戏设备";
            if (item.Role == DeviceInventoryRole.DiscoveredVirtualBusDevice)
                return Localizer.IsEnglish ? "Discovered virtual-bus device" : "已发现虚拟总线设备";
            if (item.Role == DeviceInventoryRole.UnresolvedXInputSource)
                return Localizer.IsEnglish ? "Unresolved XInput source" : "未解析 XInput 来源";
            return item.VirtualBus
                ? (Localizer.IsEnglish ? "Known virtual-bus device (offline)" : "已知虚拟总线设备（离线）")
                : (Localizer.IsEnglish ? "Known device (offline)" : "已知设备（离线）");
        }

        private static string BasisText(DeviceInventoryItem item)
        {
            if (item == null) return "—";
            if (item.Role == DeviceInventoryRole.UnresolvedXInputSource)
                return Localizer.IsEnglish ? "runtime slot only" : "仅运行时槽位";
            if (item.Role == DeviceInventoryRole.InputStitchVirtual)
                return Localizer.IsEnglish ? "owned virtual ID" : "自有虚拟身份";
            if (string.Equals(item.IdentityBasis, "container-id", StringComparison.OrdinalIgnoreCase)) return "PnP Container ID";
            if (string.Equals(item.IdentityBasis, "container-path", StringComparison.OrdinalIgnoreCase)) return "PnP container path";
            if (string.Equals(item.IdentityBasis, "serial", StringComparison.OrdinalIgnoreCase)) return "VID/PID + serial";
            if (string.Equals(item.IdentityBasis, "xusb", StringComparison.OrdinalIgnoreCase)) return "XUSB path";
            if (string.Equals(item.IdentityBasis, "pnp", StringComparison.OrdinalIgnoreCase)) return "PnP path";
            return string.IsNullOrWhiteSpace(item.IdentityBasis) ? "—" : item.IdentityBasis;
        }

        private static string VidPidText(DeviceInventoryItem item)
        {
            if (item == null || (string.IsNullOrWhiteSpace(item.Vid) && string.IsNullOrWhiteSpace(item.Pid))) return "—";
            return (string.IsNullOrWhiteSpace(item.Vid) ? "????" : item.Vid) + ":" +
                (string.IsNullOrWhiteSpace(item.Pid) ? "????" : item.Pid);
        }
    }
}
