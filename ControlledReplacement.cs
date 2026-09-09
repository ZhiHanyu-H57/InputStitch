using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class DeviceHidingCommandResult
    {
        public int ExitCode;
        public string Output = "";
        public string Error = "";
        public bool Success { get { return ExitCode == 0; } }
    }

    internal interface IDeviceHidingCommandRunner
    {
        DeviceHidingCommandResult Run(string executablePath, string arguments);
    }

    internal sealed class ProcessDeviceHidingCommandRunner : IDeviceHidingCommandRunner
    {
        public DeviceHidingCommandResult Run(string executablePath, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = executablePath;
            start.Arguments = arguments ?? "";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;
            using (Process process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("Could not start device-hiding backend.");
                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    return new DeviceHidingCommandResult { ExitCode = -1, Error = "Device-hiding command timed out after 10 seconds." };
                }
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                return new DeviceHidingCommandResult { ExitCode = process.ExitCode, Output = output ?? "", Error = error ?? "" };
            }
        }
    }

    internal sealed class GamingDeviceDescriptor
    {
        public bool Present;
        public bool GamingDevice;
        public string Vendor = "";
        public string Product = "";
        public string Description = "";
        public string DeviceInstancePath = "";
        public string XusbDeviceInstancePath = "";
        public string BaseContainerDeviceInstancePath = "";

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Product)) return Product;
                if (!string.IsNullOrWhiteSpace(Description)) return Description;
                if (!string.IsNullOrWhiteSpace(Vendor)) return Vendor;
                return DeviceInstancePath;
            }
        }
    }

    internal interface IDeviceHidingBackend
    {
        bool IsAvailable { get; }
        string BackendName { get; }
        IList<GamingDeviceDescriptor> EnumerateGamingDevices();
        ISet<string> GetHiddenDevices();
        ISet<string> GetAllowedApplications();
        bool GetCloakActive();
        void RegisterApplication(string executablePath);
        void UnregisterApplication(string executablePath);
        void HideDevice(string deviceInstancePath);
        void UnhideDevice(string deviceInstancePath);
        void SetCloakActive(bool active);
    }

    internal sealed class HidHideCliBackend : IDeviceHidingBackend
    {
        private readonly string cliPath;
        private readonly IDeviceHidingCommandRunner runner;
        private bool? available;

        internal HidHideCliBackend() : this(FindInstalledCli(), new ProcessDeviceHidingCommandRunner()) { }

        internal HidHideCliBackend(string path, IDeviceHidingCommandRunner commandRunner)
        {
            if (commandRunner == null) throw new ArgumentNullException("commandRunner");
            cliPath = path ?? "";
            runner = commandRunner;
        }

        public string BackendName { get { return "HidHide"; } }

        public bool IsAvailable
        {
            get
            {
                if (available.HasValue) return available.Value;
                if (string.IsNullOrWhiteSpace(cliPath) || !File.Exists(cliPath)) { available = false; return false; }
                try
                {
                    DeviceHidingCommandResult result = runner.Run(cliPath, "--version");
                    available = result != null && result.Success;
                }
                catch { available = false; }
                return available.Value;
            }
        }

        internal static string FindInstalledCli()
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string[] candidates = new string[]
            {
                Path.Combine(pf, "Nefarius Software Solutions", "HidHide", "HidHideCLI.exe"),
                Path.Combine(pf, "Nefarius Software Solutions e.U", "HidHide", "HidHideCLI.exe"),
                Path.Combine(pf, "Nefarius Software Solutions e.U", "HidHideCLI", "HidHideCLI.exe"),
                Path.Combine(pf, "Nefarius", "HidHideCLI", "HidHideCLI.exe")
            };
            foreach (string candidate in candidates)
                if (File.Exists(candidate)) return candidate;
            return "";
        }

        public IList<GamingDeviceDescriptor> EnumerateGamingDevices()
        {
            string output = RequireSuccess("--dev-gaming");
            return ParseGamingDevices(output);
        }

        public ISet<string> GetHiddenDevices()
        {
            return ParseQuotedCommandList(RequireSuccess("--dev-list"), "--dev-hide");
        }

        public ISet<string> GetAllowedApplications()
        {
            return ParseQuotedCommandList(RequireSuccess("--app-list"), "--app-reg");
        }

        public bool GetCloakActive()
        {
            string output = RequireSuccess("--cloak-state");
            return output.IndexOf("--cloak-on", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void RegisterApplication(string executablePath) { RequireSuccess("--app-reg " + Quote(executablePath)); }
        public void UnregisterApplication(string executablePath) { RequireSuccess("--app-unreg " + Quote(executablePath)); }
        public void HideDevice(string deviceInstancePath) { RequireSuccess("--dev-hide " + Quote(deviceInstancePath)); }
        public void UnhideDevice(string deviceInstancePath) { RequireSuccess("--dev-unhide " + Quote(deviceInstancePath)); }
        public void SetCloakActive(bool active) { RequireSuccess(active ? "--cloak-on" : "--cloak-off"); }

        private string RequireSuccess(string arguments)
        {
            if (!IsAvailable && !string.Equals(arguments, "--version", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("HidHide is not installed or its driver is unavailable.");
            DeviceHidingCommandResult result = runner.Run(cliPath, arguments);
            if (result == null || !result.Success)
            {
                string detail = result == null ? "No result." : (string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error);
                throw new InvalidOperationException("HidHide command failed: " + arguments + (string.IsNullOrWhiteSpace(detail) ? "" : " — " + detail.Trim()));
            }
            return result.Output ?? "";
        }

        internal static ISet<string> ParseQuotedCommandList(string output, string command)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(output)) return result;
            string pattern = Regex.Escape(command) + "\\s+\"((?:\\\\.|[^\"])*)\"";
            foreach (Match match in Regex.Matches(output, pattern, RegexOptions.IgnoreCase))
            {
                string value = JsonUnescape(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(value)) result.Add(value);
            }
            return result;
        }

        internal static IList<GamingDeviceDescriptor> ParseGamingDevices(string json)
        {
            List<GamingDeviceDescriptor> result = new List<GamingDeviceDescriptor>();
            if (string.IsNullOrWhiteSpace(json)) return result;
            string quoted = "\"((?:\\\\.|[^\"])*)\"";
            string pattern =
                "\\{\\s*\"present\"\\s*:\\s*(true|false)" +
                ".*?\"gamingDevice\"\\s*:\\s*(true|false)" +
                ".*?\"vendor\"\\s*:\\s*" + quoted +
                ".*?\"product\"\\s*:\\s*" + quoted +
                ".*?\"description\"\\s*:\\s*" + quoted +
                ".*?\"deviceInstancePath\"\\s*:\\s*" + quoted +
                ".*?\"xusbDeviceInstancePath\"\\s*:\\s*" + quoted +
                ".*?\"baseContainerDeviceInstancePath\"\\s*:\\s*" + quoted;
            Regex objectPattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in objectPattern.Matches(json))
            {
                GamingDeviceDescriptor item = new GamingDeviceDescriptor();
                item.Present = string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
                item.GamingDevice = string.Equals(match.Groups[2].Value, "true", StringComparison.OrdinalIgnoreCase);
                item.Vendor = JsonUnescape(match.Groups[3].Value);
                item.Product = JsonUnescape(match.Groups[4].Value);
                item.Description = JsonUnescape(match.Groups[5].Value);
                item.DeviceInstancePath = JsonUnescape(match.Groups[6].Value);
                item.XusbDeviceInstancePath = JsonUnescape(match.Groups[7].Value);
                item.BaseContainerDeviceInstancePath = JsonUnescape(match.Groups[8].Value);
                result.Add(item);
            }
            return result;
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty path is required.");
            if (value.IndexOf('"') >= 0) throw new ArgumentException("Paths containing quotes are not supported.");
            return "\"" + value + "\"";
        }

        private static string JsonUnescape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            StringBuilder result = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length) { result.Append(c); continue; }
                char n = value[++i];
                if (n == '\\' || n == '"' || n == '/') result.Append(n);
                else if (n == 'b') result.Append('\b');
                else if (n == 'f') result.Append('\f');
                else if (n == 'n') result.Append('\n');
                else if (n == 'r') result.Append('\r');
                else if (n == 't') result.Append('\t');
                else if (n == 'u' && i + 4 < value.Length)
                {
                    int code;
                    if (int.TryParse(value.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out code))
                    {
                        result.Append((char)code);
                        i += 4;
                    }
                    else result.Append(n);
                }
                else result.Append(n);
            }
            return result.ToString();
        }
    }

    [Serializable]
    public sealed class ControlledReplacementRecoveryRecord
    {
        public string ApplicationPath = "";
        public bool InitialCloakActive;
        public bool AddedApplication;
        public List<string> AddedHiddenDevices = new List<string>();
    }

    internal interface IControlledReplacementJournal
    {
        ControlledReplacementRecoveryRecord Load();
        void Save(ControlledReplacementRecoveryRecord record);
        void Clear();
    }

    internal sealed class FileControlledReplacementJournal : IControlledReplacementJournal
    {
        private readonly string path;

        internal FileControlledReplacementJournal(string journalPath)
        {
            if (string.IsNullOrWhiteSpace(journalPath)) throw new ArgumentException("A recovery journal path is required.");
            path = Path.GetFullPath(journalPath);
        }

        public ControlledReplacementRecoveryRecord Load()
        {
            if (!File.Exists(path)) return null;
            using (FileStream stream = File.OpenRead(path))
                return (ControlledReplacementRecoveryRecord)new XmlSerializer(typeof(ControlledReplacementRecoveryRecord)).Deserialize(stream);
        }

        public void Save(ControlledReplacementRecoveryRecord record)
        {
            if (record == null) throw new ArgumentNullException("record");
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            string staged = path + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(staged, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new XmlSerializer(typeof(ControlledReplacementRecoveryRecord)).Serialize(stream, record);
                    stream.Flush(true);
                }
                // Validate the staged bytes before they replace the previous recovery record.
                using (FileStream stream = File.OpenRead(staged))
                    new XmlSerializer(typeof(ControlledReplacementRecoveryRecord)).Deserialize(stream);
                if (File.Exists(path)) File.Replace(staged, path, null);
                else File.Move(staged, path);
            }
            finally
            {
                try { if (File.Exists(staged)) File.Delete(staged); } catch { }
            }
        }

        public void Clear()
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    internal static class ControlledReplacementRecovery
    {
        internal static string RestorePending(IDeviceHidingBackend backend, IControlledReplacementJournal journal)
        {
            if (backend == null || journal == null) return "";
            ControlledReplacementRecoveryRecord record;
            try { record = journal.Load(); }
            catch (Exception ex) { return "Could not read recovery journal: " + ex.Message; }
            if (record == null) return "";
            if (!backend.IsAvailable) return "Device hiding backend is unavailable; recovery journal was kept.";

            List<string> errors = new List<string>();
            if (record.AddedHiddenDevices != null)
            {
                for (int i = record.AddedHiddenDevices.Count - 1; i >= 0; i--)
                {
                    string device = record.AddedHiddenDevices[i];
                    if (string.IsNullOrWhiteSpace(device)) continue;
                    try { backend.UnhideDevice(device); }
                    catch (Exception ex) { errors.Add("unhide " + device + ": " + ex.Message); }
                }
            }
            try
            {
                if (backend.GetCloakActive() != record.InitialCloakActive)
                    backend.SetCloakActive(record.InitialCloakActive);
            }
            catch (Exception ex) { errors.Add("restore cloak: " + ex.Message); }

            if (record.AddedApplication && !string.IsNullOrWhiteSpace(record.ApplicationPath))
            {
                try { backend.UnregisterApplication(record.ApplicationPath); }
                catch (Exception ex) { errors.Add("remove app whitelist: " + ex.Message); }
            }

            if (errors.Count != 0) return string.Join(" | ", errors.ToArray());
            try { journal.Clear(); }
            catch (Exception ex) { return "Device visibility was restored but the recovery journal could not be cleared: " + ex.Message; }
            return "";
        }
    }

    internal sealed class ControlledReplacementDialog : Form
    {
        private sealed class DeviceChoice
        {
            public readonly GamingDeviceDescriptor Device;
            public DeviceChoice(GamingDeviceDescriptor device) { Device = device; }
            public override string ToString()
            {
                if (Device == null) return "";
                string name = string.IsNullOrWhiteSpace(Device.DisplayName) ? "Game controller" : Device.DisplayName;
                return name + "  —  " + Device.DeviceInstancePath;
            }
        }

        private readonly IDeviceHidingBackend backend;
        private readonly Func<int> ownSlot;
        private readonly Func<bool> routerReady;
        private readonly Func<bool> isActive;
        private readonly Func<IEnumerable<GamingDeviceDescriptor>, ControlledReplacementResult> begin;
        private readonly Func<ControlledReplacementResult> stop;
        private readonly CheckedListBox devices = new CheckedListBox();
        private readonly Label stateLabel = new Label();
        private readonly Button refreshButton = new Button();
        private readonly Button selectAllButton = new Button();
        private readonly Button startButton = new Button();
        private readonly Button stopButton = new Button();
        private readonly Button closeButton = new Button();

        internal ControlledReplacementDialog(IDeviceHidingBackend hidingBackend, Func<int> virtualSlot,
            Func<bool> isRouterReady, Func<bool> replacementActive,
            Func<IEnumerable<GamingDeviceDescriptor>, ControlledReplacementResult> beginReplacement,
            Func<ControlledReplacementResult> stopReplacement)
        {
            backend = hidingBackend;
            ownSlot = virtualSlot;
            routerReady = isRouterReady;
            isActive = replacementActive;
            begin = beginReplacement;
            stop = stopReplacement;

            Text = Localizer.IsEnglish ? "Controller takeover (Experimental)" : "手柄接管（实验）";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(680, 470);
            Size = new Size(820, 560);
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;

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
            intro.MaximumSize = new Size(760, 0);
            intro.Text = Localizer.IsEnglish
                ? "Experimental: take over the selected XInput controller(s) through one InputStitch virtual Xbox controller in slot 0. Nothing is hidden automatically. If InputStitch is not already slot 0, takeover first performs a recoverable controller re-enumeration; physical hiding starts only after slot 0 and source recovery are verified. Slot reordering requires administrator permission."
                : "实验功能：把所选 XInput 手柄汇总并接管到一个位于 0 号槽位的 InputStitch 虚拟 Xbox 手柄。程序不会自动隐藏任何设备。如果 InputStitch 当前不在 0 号，开始接管时会先执行可恢复的手柄重新枚举；只有确认虚拟手柄已经成为 0 号、原手柄也全部恢复可读后，才进入隐藏阶段。重新排列槽位需要管理员权限。";
            root.Controls.Add(intro, 0, 0);

            stateLabel.AutoSize = true;
            stateLabel.MaximumSize = new Size(760, 0);
            stateLabel.Margin = new Padding(0, 10, 0, 8);
            root.Controls.Add(stateLabel, 0, 1);

            devices.Dock = DockStyle.Fill;
            devices.CheckOnClick = true;
            devices.IntegralHeight = false;
            devices.ItemCheck += delegate { BeginInvoke((MethodInvoker)UpdateActions); };
            root.Controls.Add(devices, 0, 2);

            FlowLayoutPanel utility = new FlowLayoutPanel();
            utility.AutoSize = true;
            utility.Dock = DockStyle.Fill;
            utility.FlowDirection = FlowDirection.LeftToRight;
            refreshButton.AutoSize = true;
            refreshButton.Text = Localizer.IsEnglish ? "Refresh devices" : "重新检测设备";
            refreshButton.Click += delegate { RefreshDevices(); };
            utility.Controls.Add(refreshButton);
            selectAllButton.AutoSize = true;
            selectAllButton.Text = Localizer.IsEnglish ? "Select all listed" : "全选已列出的设备";
            selectAllButton.Click += delegate
            {
                for (int i = 0; i < devices.Items.Count; i++) devices.SetItemChecked(i, true);
                UpdateActions();
            };
            utility.Controls.Add(selectAllButton);
            root.Controls.Add(utility, 0, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            closeButton.AutoSize = true;
            closeButton.Text = Localizer.IsEnglish ? "Close" : "关闭";
            closeButton.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(closeButton);
            stopButton.AutoSize = true;
            stopButton.Text = Localizer.IsEnglish ? "Stop takeover" : "停止接管并恢复原手柄";
            stopButton.Click += delegate { StopReplacement(); };
            actions.Controls.Add(stopButton);
            startButton.AutoSize = true;
            startButton.Text = Localizer.IsEnglish ? "Start takeover" : "开始接管";
            startButton.Click += delegate { StartReplacement(); };
            actions.Controls.Add(startButton);
            root.Controls.Add(actions, 0, 4);
            CancelButton = closeButton;

            Shown += delegate { RefreshDevices(); };
        }

        private void RefreshDevices()
        {
            devices.Items.Clear();
            if (backend == null || !backend.IsAvailable)
            {
                stateLabel.Text = Localizer.IsEnglish
                    ? "HidHide was not detected. Controller takeover is unavailable; controller merging without hiding can still be used."
                    : "未检测到 HidHide。当前不能隐藏原手柄；不隐藏原设备的“手柄汇总”仍可正常使用。";
                UpdateActions();
                return;
            }

            try
            {
                foreach (GamingDeviceDescriptor device in backend.EnumerateGamingDevices())
                {
                    if (device == null || !device.Present || !device.GamingDevice || string.IsNullOrWhiteSpace(device.DeviceInstancePath)) continue;
                    devices.Items.Add(new DeviceChoice(device), false);
                }
            }
            catch (Exception ex)
            {
                stateLabel.Text = (Localizer.IsEnglish ? "Could not enumerate controllers: " : "无法读取手柄设备：") + ex.Message;
                UpdateActions();
                return;
            }
            RefreshStateText();
            UpdateActions();
        }

        private void RefreshStateText()
        {
            bool active = isActive != null && isActive();
            int slot = ownSlot == null ? -1 : ownSlot();
            bool routing = routerReady != null && routerReady();
            if (active)
                stateLabel.Text = Localizer.IsEnglish ? "Takeover is active. Use Stop takeover before changing routing or closing InputStitch." : "手柄接管正在运行。修改手柄汇总设置或退出 InputStitch 前，请先停止接管。";
            else if (!routing)
                stateLabel.Text = Localizer.IsEnglish ? "Controller merging is not ready. Enable it in Settings and connect the virtual Xbox controller first." : "手柄汇总尚未就绪。请先在设置中开启手柄汇总，并连接 InputStitch 虚拟 Xbox 手柄。";
            else if (slot != 0)
                stateLabel.Text = (Localizer.IsEnglish ? "InputStitch virtual Xbox slot: " : "InputStitch 虚拟 Xbox 手柄当前槽位：") + slot.ToString() +
                    (Localizer.IsEnglish ? ". Starting takeover will first try a recoverable slot-0 acquisition. If slot 0 cannot be verified, original devices are restored and no HidHide hiding starts." : "。开始接管时会先尝试以可恢复方式取得 0 号槽位；如果无法验证 0 号，会恢复原设备并终止，不会进入 HidHide 隐藏阶段。");
            else if (devices.Items.Count == 0)
                stateLabel.Text = Localizer.IsEnglish ? "No present gaming controller was reported by HidHide." : "HidHide 当前没有报告可选择的游戏控制设备。";
            else
                stateLabel.Text = Localizer.IsEnglish ? "Ready. Select the original controller device(s) you want InputStitch to take over." : "条件已满足。请勾选要由 InputStitch 接管的原手柄设备；默认不会勾选任何设备。";
        }

        private void UpdateActions()
        {
            bool active = isActive != null && isActive();
            bool available = backend != null && backend.IsAvailable;
            bool routing = routerReady != null && routerReady();
            startButton.Enabled = !active && available && routing && devices.CheckedItems.Count > 0;
            stopButton.Enabled = active;
            refreshButton.Enabled = !active;
            selectAllButton.Enabled = !active && devices.Items.Count > 0;
        }

        private void StartReplacement()
        {
            List<GamingDeviceDescriptor> selected = new List<GamingDeviceDescriptor>();
            foreach (object item in devices.CheckedItems)
            {
                DeviceChoice choice = item as DeviceChoice;
                if (choice != null && choice.Device != null && !string.IsNullOrWhiteSpace(choice.Device.DeviceInstancePath))
                    selected.Add(choice.Device);
            }
            if (selected.Count == 0 || begin == null) { UpdateActions(); return; }

            ControlledReplacementResult result = begin(selected);
            stateLabel.Text = FriendlyResult(result);
            UpdateActions();
        }

        private void StopReplacement()
        {
            if (stop == null) return;
            ControlledReplacementResult result = stop();
            stateLabel.Text = FriendlyResult(result);
            UpdateActions();
        }

        private static string FriendlyResult(ControlledReplacementResult result)
        {
            if (result == null) return Localizer.IsEnglish ? "No result." : "没有返回结果。";
            if (result.State == ControlledReplacementState.Active)
                return Localizer.IsEnglish ? "Controller takeover is active." : "手柄接管已启用：所选原手柄已交由 InputStitch 负责转发。";
            if (result.State == ControlledReplacementState.WrongVirtualSlot)
                return Localizer.IsEnglish ? "Takeover was refused because the InputStitch virtual controller is not in slot 0. No original controller was hidden." : "已拒绝接管：InputStitch 虚拟手柄不在 0 号槽位，因此没有隐藏任何原手柄。";
            if (result.State == ControlledReplacementState.ElevationRequired)
                return Localizer.IsEnglish ? "Administrator permission is required to safely acquire XInput slot 0. No original controller was hidden." : "安全取得 XInput 0 号槽位需要管理员权限；没有隐藏任何原手柄。";
            if (result.State == ControlledReplacementState.SlotAcquisitionFailed)
                return (Localizer.IsEnglish ? "Slot-0 acquisition failed safely. Original devices were restored and HidHide hiding did not start: " : "0 号槽位取得失败，已恢复原设备且没有进入 HidHide 隐藏阶段：") + result.Message;
            if (result.State == ControlledReplacementState.RouterNotReady)
                return Localizer.IsEnglish ? "Takeover was refused because controller merging is not ready." : "已拒绝接管：手柄汇总尚未就绪。";
            if (result.State == ControlledReplacementState.BackendUnavailable)
                return Localizer.IsEnglish ? "HidHide is unavailable; no device was changed." : "HidHide 不可用，没有修改任何设备。";
            if (result.State == ControlledReplacementState.NoSourceDevices)
                return Localizer.IsEnglish ? "No external controller source is currently available to take over." : "当前没有可由 InputStitch 接管的外部手柄输入源。";
            if (result.State == ControlledReplacementState.Idle)
                return Localizer.IsEnglish ? "Takeover is off; InputStitch-owned hiding changes were restored." : "手柄接管已关闭，InputStitch 自己添加的隐藏设置已经恢复。";
            return (Localizer.IsEnglish ? "Takeover failed and InputStitch attempted to restore the original device state: " : "接管失败，InputStitch 已尝试恢复原手柄状态：") + result.Message;
        }
    }

    internal enum ControlledReplacementState
    {
        Idle,
        BackendUnavailable,
        RouterNotReady,
        WrongVirtualSlot,
        ElevationRequired,
        SlotAcquisitionFailed,
        NoSourceDevices,
        Activating,
        Active,
        RollingBack,
        Failed
    }

    internal sealed class ControlledReplacementResult
    {
        public ControlledReplacementState State;
        public string Message = "";
        public bool Success { get { return State == ControlledReplacementState.Active || State == ControlledReplacementState.Idle; } }
    }

    // Enforces the cross-stage safety boundary between XInput slot acquisition and HidHide.
    // The hiding transaction is unreachable unless acquisition succeeded, the caller re-established
    // routing/source health, and the virtual controller still owns the requested target slot.
    internal sealed class ControlledTakeoverPipeline
    {
        private readonly Func<IEnumerable<GamingDeviceDescriptor>, int, SlotAcquisitionResult> acquireSlot;
        private readonly Func<int> virtualSlot;
        private readonly Func<bool> prepareForHiding;
        private readonly Func<IEnumerable<string>, int, ControlledReplacementResult> beginHiding;

        internal ControlledTakeoverPipeline(
            Func<IEnumerable<GamingDeviceDescriptor>, int, SlotAcquisitionResult> slotAcquisition,
            Func<int> ownVirtualSlot,
            Func<bool> prepareRouterAndSourcesForHiding,
            Func<IEnumerable<string>, int, ControlledReplacementResult> beginDeviceHiding)
        {
            if (slotAcquisition == null || ownVirtualSlot == null || prepareRouterAndSourcesForHiding == null || beginDeviceHiding == null)
                throw new ArgumentNullException("controlled takeover pipeline dependency");
            acquireSlot = slotAcquisition;
            virtualSlot = ownVirtualSlot;
            prepareForHiding = prepareRouterAndSourcesForHiding;
            beginHiding = beginDeviceHiding;
        }

        internal ControlledReplacementResult Begin(IEnumerable<GamingDeviceDescriptor> selectedDevices, int targetSlot)
        {
            SlotAcquisitionResult acquisition = acquireSlot(selectedDevices, targetSlot);
            if (acquisition == null)
                return new ControlledReplacementResult { State = ControlledReplacementState.SlotAcquisitionFailed, Message = "Slot acquisition returned no result; device hiding did not start." };
            if (!acquisition.Success)
            {
                return new ControlledReplacementResult
                {
                    State = acquisition.State == SlotAcquisitionState.ElevationRequired
                        ? ControlledReplacementState.ElevationRequired
                        : ControlledReplacementState.SlotAcquisitionFailed,
                    Message = acquisition.Message
                };
            }

            if (virtualSlot() != targetSlot)
                return new ControlledReplacementResult { State = ControlledReplacementState.SlotAcquisitionFailed, Message = "The virtual controller is not in the target slot after slot acquisition; device hiding did not start." };

            List<string> externalPaths = acquisition.ExternalDeviceInstancePaths == null
                ? new List<string>() : new List<string>(acquisition.ExternalDeviceInstancePaths);
            if (externalPaths.Count == 0)
                return new ControlledReplacementResult { State = ControlledReplacementState.NoSourceDevices, Message = "No external HidHide device identity remained after excluding the InputStitch virtual controller." };

            if (!prepareForHiding() || virtualSlot() != targetSlot)
                return new ControlledReplacementResult { State = ControlledReplacementState.SlotAcquisitionFailed, Message = "Router/source/slot verification failed after slot acquisition; device hiding did not start." };

            // This is the first line in the entire pipeline allowed to mutate HidHide state.
            return beginHiding(externalPaths, targetSlot);
        }
    }

    internal sealed class ControlledReplacementCoordinator : IDisposable
    {
        private readonly IDeviceHidingBackend backend;
        private readonly Func<bool> routerReady;
        private readonly Func<int> virtualSlot;
        private readonly Func<bool> sourceHealth;
        private readonly string applicationPath;
        private readonly IControlledReplacementJournal journal;
        private readonly List<string> addedHiddenDevices = new List<string>();
        private bool addedApplication;
        private bool initialCloak;
        private bool snapshotTaken;

        public ControlledReplacementState State { get; private set; }
        public string LastMessage { get; private set; }
        public bool Active { get { return State == ControlledReplacementState.Active; } }

        internal ControlledReplacementCoordinator(IDeviceHidingBackend hidingBackend, Func<bool> isRouterReady,
            Func<int> ownVirtualSlot, Func<bool> routedSourceHealth, string executablePath)
            : this(hidingBackend, isRouterReady, ownVirtualSlot, routedSourceHealth, executablePath, null) { }

        internal ControlledReplacementCoordinator(IDeviceHidingBackend hidingBackend, Func<bool> isRouterReady,
            Func<int> ownVirtualSlot, Func<bool> routedSourceHealth, string executablePath, IControlledReplacementJournal recoveryJournal)
        {
            if (hidingBackend == null) throw new ArgumentNullException("hidingBackend");
            if (isRouterReady == null) throw new ArgumentNullException("isRouterReady");
            if (ownVirtualSlot == null) throw new ArgumentNullException("ownVirtualSlot");
            backend = hidingBackend;
            routerReady = isRouterReady;
            virtualSlot = ownVirtualSlot;
            sourceHealth = routedSourceHealth;
            applicationPath = executablePath ?? "";
            journal = recoveryJournal;
            State = ControlledReplacementState.Idle;
            LastMessage = "idle";
        }

        public ControlledReplacementResult Begin(IEnumerable<string> deviceInstancePaths, int targetSlot)
        {
            if (Active) return Result(ControlledReplacementState.Active, "Controlled replacement is already active.");
            if (!backend.IsAvailable) return Result(ControlledReplacementState.BackendUnavailable, "Device hiding backend is unavailable.");
            if (journal != null)
            {
                try
                {
                    if (journal.Load() != null)
                        return Result(ControlledReplacementState.Failed, "A previous controlled-replacement recovery record is still pending. Restore it before starting a new takeover.");
                }
                catch (Exception ex)
                {
                    return Result(ControlledReplacementState.Failed, "Could not verify controlled-replacement recovery state: " + ex.Message);
                }
            }
            if (!routerReady()) return Result(ControlledReplacementState.RouterNotReady, "Controller routing is not ready.");
            if (virtualSlot() != targetSlot)
                return Result(ControlledReplacementState.WrongVirtualSlot, "Virtual controller is not in the required target slot; no physical device was hidden.");

            List<string> requested = NormalizePaths(deviceInstancePaths);
            if (requested.Count == 0) return Result(ControlledReplacementState.NoSourceDevices, "No physical controller device was selected for hiding.");

            State = ControlledReplacementState.Activating;
            LastMessage = "activating";
            addedHiddenDevices.Clear();
            addedApplication = false;
            snapshotTaken = false;
            try
            {
                ISet<string> hidden = backend.GetHiddenDevices();
                ISet<string> apps = backend.GetAllowedApplications();
                initialCloak = backend.GetCloakActive();
                snapshotTaken = true;

                if (string.IsNullOrWhiteSpace(applicationPath)) throw new InvalidOperationException("InputStitch executable path is unavailable.");

                addedApplication = !apps.Contains(applicationPath);
                foreach (string path in requested)
                    if (!hidden.Contains(path)) addedHiddenDevices.Add(path);

                if (journal != null && (addedApplication || addedHiddenDevices.Count != 0 || !initialCloak))
                {
                    ControlledReplacementRecoveryRecord recovery = new ControlledReplacementRecoveryRecord();
                    recovery.ApplicationPath = applicationPath;
                    recovery.InitialCloakActive = initialCloak;
                    recovery.AddedApplication = addedApplication;
                    recovery.AddedHiddenDevices = new List<string>(addedHiddenDevices);
                    // Persist the exact changes InputStitch intends to own before touching HidHide.
                    // A crash after any following command can then be repaired on the next launch.
                    journal.Save(recovery);
                }

                if (addedApplication) backend.RegisterApplication(applicationPath);
                foreach (string path in addedHiddenDevices) backend.HideDevice(path);
                if (!initialCloak) backend.SetCloakActive(true);

                // Hard safety gate: hiding is not considered active unless the routing side is still
                // healthy and the game-visible target slot condition still holds after the change.
                if (!routerReady()) throw new InvalidOperationException("Router stopped after device hiding was enabled.");
                if (virtualSlot() != targetSlot) throw new InvalidOperationException("Virtual controller left the required target slot after hiding was enabled.");
                if (sourceHealth != null && !sourceHealth()) throw new InvalidOperationException("InputStitch can no longer read the routed controller after hiding it from games.");
                if (!backend.GetCloakActive()) throw new InvalidOperationException("Device hiding did not remain enabled.");

                return Result(ControlledReplacementState.Active, "Controlled replacement is active.");
            }
            catch (Exception ex)
            {
                string rollbackError = RollbackInternal();
                string message = ex.Message;
                if (!string.IsNullOrWhiteSpace(rollbackError)) message += " Rollback warning: " + rollbackError;
                return Result(ControlledReplacementState.Failed, message);
            }
        }

        public ControlledReplacementResult Stop()
        {
            if (!Active && !snapshotTaken) return Result(ControlledReplacementState.Idle, "Controlled replacement is already off.");
            string error = RollbackInternal();
            if (!string.IsNullOrWhiteSpace(error)) return Result(ControlledReplacementState.Failed, "Could not fully restore device visibility: " + error);
            return Result(ControlledReplacementState.Idle, "Controlled replacement is off and InputStitch changes were restored.");
        }

        private string RollbackInternal()
        {
            State = ControlledReplacementState.RollingBack;
            List<string> errors = new List<string>();
            for (int i = addedHiddenDevices.Count - 1; i >= 0; i--)
            {
                try { backend.UnhideDevice(addedHiddenDevices[i]); }
                catch (Exception ex) { errors.Add("unhide " + addedHiddenDevices[i] + ": " + ex.Message); }
            }
            addedHiddenDevices.Clear();

            if (snapshotTaken)
            {
                try { if (backend.GetCloakActive() != initialCloak) backend.SetCloakActive(initialCloak); }
                catch (Exception ex) { errors.Add("restore cloak: " + ex.Message); }
            }
            if (addedApplication)
            {
                try { backend.UnregisterApplication(applicationPath); }
                catch (Exception ex) { errors.Add("remove app whitelist: " + ex.Message); }
                addedApplication = false;
            }
            snapshotTaken = false;
            if (errors.Count == 0 && journal != null)
            {
                try { journal.Clear(); }
                catch (Exception ex) { errors.Add("clear recovery journal: " + ex.Message); }
            }
            State = errors.Count == 0 ? ControlledReplacementState.Idle : ControlledReplacementState.Failed;
            LastMessage = errors.Count == 0 ? "idle" : string.Join(" | ", errors.ToArray());
            return LastMessage == "idle" ? "" : LastMessage;
        }

        private ControlledReplacementResult Result(ControlledReplacementState state, string message)
        {
            State = state;
            LastMessage = message ?? "";
            return new ControlledReplacementResult { State = state, Message = LastMessage };
        }

        private static List<string> NormalizePaths(IEnumerable<string> values)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (values == null) return result;
            foreach (string value in values)
            {
                string path = (value ?? "").Trim();
                if (path.Length == 0 || !seen.Add(path)) continue;
                result.Add(path);
            }
            return result;
        }

        public void Dispose()
        {
            try { Stop(); } catch { }
        }
    }
}
