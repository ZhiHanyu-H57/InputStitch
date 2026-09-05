using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace InputStitch
{
    [Serializable]
    public sealed class IdleGamepadOptions
    {
        public bool Enabled = false;
        public int IdleSeconds = 300;
        public int HoldMilliseconds = 150;
        public InputSpec Pulse = DefaultPulse();

        public static InputSpec DefaultPulse()
        {
            InputSpec input = new InputSpec();
            input.Kind = InputKind.Gamepad;
            input.GamepadControl = GamepadControl.DPadDown;
            input.GamepadX = 0;
            input.GamepadY = -100;
            input.GamepadValue = 100;
            return input;
        }

        public IdleGamepadOptions CloneNormalized()
        {
            IdleGamepadOptions value = new IdleGamepadOptions();
            value.Enabled = Enabled;
            value.IdleSeconds = Math.Max(10, Math.Min(86400, IdleSeconds));
            value.HoldMilliseconds = Math.Max(50, Math.Min(2000, HoldMilliseconds));
            value.Pulse = Pulse == null ? DefaultPulse() : Pulse.Clone();
            value.Pulse.Kind = InputKind.Gamepad;
            if (!Enum.IsDefined(typeof(GamepadControl), value.Pulse.GamepadControl)) value.Pulse.GamepadControl = GamepadControl.DPadDown;
            value.Pulse.GamepadX = Math.Max(-100, Math.Min(100, value.Pulse.GamepadX));
            value.Pulse.GamepadY = Math.Max(-100, Math.Min(100, value.Pulse.GamepadY));
            value.Pulse.GamepadValue = Math.Max(0, Math.Min(100, value.Pulse.GamepadValue));
            return value;
        }
    }

    // Deterministic scheduler: no input, native calls or wall-clock dependencies.
    public sealed class IdleGamepadScheduler
    {
        private long interval = 300000;
        private long lastActivity;
        public bool Enabled { get; private set; }
        public bool Suspended { get; private set; }
        public bool Faulted { get; private set; }
        public bool Busy { get; private set; }
        public bool PulseActive { get; private set; }

        public void Configure(bool enabled, int idleSeconds, long now, bool explicitEnable)
        {
            Enabled = enabled;
            interval = (long)Math.Max(10, Math.Min(86400, idleSeconds)) * 1000;
            if (explicitEnable && enabled) { Suspended = false; Faulted = false; }
            PulseActive = false;
            lastActivity = now;
        }

        public void NotifyActivity(long now) { lastActivity = now; }

        public void SetBusy(bool busy, long now)
        {
            if (Busy != busy || busy) lastActivity = now;
            Busy = busy;
        }

        public bool TryBeginPulse(long now)
        {
            if (!Enabled || Suspended || Faulted || Busy || PulseActive) return false;
            if (now < lastActivity) { lastActivity = now; return false; }
            if (now - lastActivity < interval) return false;
            PulseActive = true;
            return true;
        }

        public void FinishPulse(long now) { PulseActive = false; lastActivity = now; }
        public void PanicStop(long now) { Suspended = true; FinishPulse(now); }
        public void Fail(long now) { Faulted = true; FinishPulse(now); }
        public long RemainingMilliseconds(long now) { return Math.Max(0, interval - Math.Max(0, now - lastActivity)); }
    }

    public sealed class IdleGamepadFailureEventArgs : EventArgs
    {
        public readonly Exception Exception;
        public IdleGamepadFailureEventArgs(Exception exception) { Exception = exception; }
    }

    // Create/configure/tick/dispose on the UI thread. NotifyActivity and SetBusy may
    // also be called by hooks/workers. No timer thread can race a macro's output.
    public sealed class IdleGamepadService : IDisposable
    {
        private readonly object sync = new object();
        private readonly IdleGamepadScheduler scheduler = new IdleGamepadScheduler();
        private readonly Action ensureConnected;
        private readonly Action<InputSpec, bool> send;
        private readonly Func<long> clock;
        private readonly IdleManualActivitySensor sensor;
        private IdleGamepadOptions options = new IdleGamepadOptions();
        private InputSpec pressed;
        private long releaseAt;
        private bool disposed;
        private Exception pendingFailure;

        public event EventHandler<IdleGamepadFailureEventArgs> Failure;
        public event Action<Exception> Failed;
        public bool Suspended { get { lock (sync) return scheduler.Suspended; } }
        public bool IsSuspended { get { lock (sync) return scheduler.Suspended; } }
        public bool HasFailed { get { lock (sync) return scheduler.Faulted; } }
        public bool IsEnabled { get { lock (sync) return scheduler.Enabled; } }
        public bool IsPulsing { get { lock (sync) return pressed != null; } }

        public IdleGamepadService(Func<int> ownXboxSlot)
            : this(GamepadOutput.EnsureConnected, GamepadOutput.Send, ownXboxSlot, MonotonicMilliseconds, true) { }

        // Injectable boundaries let tests prove cancellation and failure behavior
        // without creating a virtual controller or sending real input.
        public IdleGamepadService(Action ensureConnected, Action<InputSpec, bool> send,
            Func<int> ownXboxSlot, Func<long> clock, bool detectManualActivity)
        {
            if (ensureConnected == null || send == null || clock == null) throw new ArgumentNullException();
            this.ensureConnected = ensureConnected;
            this.send = send;
            this.clock = clock;
            if (detectManualActivity) sensor = new IdleManualActivitySensor(ownXboxSlot);
        }

        public static long MonotonicMilliseconds()
        {
            return (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
        }

        public void Configure(IdleGamepadOptions value, bool explicitEnable)
        {
            lock (sync)
            {
                if (disposed) return;
                long now = clock();
                ReleaseLocked(now);
                options = (value ?? new IdleGamepadOptions()).CloneNormalized();
                scheduler.Configure(options.Enabled, options.IdleSeconds, now, explicitEnable);
                if (explicitEnable) pendingFailure = null;
                if (sensor != null) sensor.Reset();
            }
        }

        public void AttachWindow(IntPtr handle)
        {
            if (sensor != null) sensor.AttachWindow(handle);
        }

        // Forward WM_INPUT (0x00FF) and WM_INPUT_DEVICE_CHANGE (0x00FE) before the
        // form's base.WndProc. This only observes physical DualShock 4 controls.
        public void ProcessWindowMessage(int message, IntPtr wParam, IntPtr lParam)
        {
            if (sensor != null && sensor.ProcessWindowMessage(message, wParam, lParam)) NotifyActivity();
        }

        public void NotifyActivity()
        {
            lock (sync)
            {
                if (disposed) return;
                long now = clock();
                ReleaseLocked(now);
                scheduler.NotifyActivity(now);
            }
        }

        public void SetBusy(bool busy)
        {
            lock (sync)
            {
                if (disposed) return;
                long now = clock();
                if (busy) ReleaseLocked(now);
                scheduler.SetBusy(busy, now);
            }
        }

        public void PanicStop()
        {
            lock (sync)
            {
                if (disposed) return;
                long now = clock();
                ReleaseLocked(now);
                scheduler.PanicStop(now);
            }
        }

        public void Tick()
        {
            Exception report = null;
            lock (sync)
            {
                if (disposed) return;
                long now = clock();
                if (scheduler.Enabled && !scheduler.Suspended && !scheduler.Faulted)
                {
                    if (sensor != null && sensor.Poll(now))
                    {
                        ReleaseLocked(now);
                        scheduler.NotifyActivity(now);
                    }
                    if (pressed != null && now >= releaseAt) ReleaseLocked(now);
                    if (scheduler.TryBeginPulse(now))
                    {
                        try
                        {
                            ensureConnected();
                            // Remember ownership before send: if a partial native
                            // report fails, the matching release is still attempted.
                            pressed = options.Pulse.Clone();
                            releaseAt = now + options.HoldMilliseconds;
                            send(pressed, true);
                        }
                        catch (Exception ex)
                        {
                            ReleaseLocked(now);
                            FailLocked(ex, now);
                        }
                    }
                }
                else if (pressed != null) ReleaseLocked(now);
                report = pendingFailure;
                pendingFailure = null;
            }
            EventHandler<IdleGamepadFailureEventArgs> handler = Failure;
            if (report != null && handler != null) handler(this, new IdleGamepadFailureEventArgs(report));
            Action<Exception> failed = Failed;
            if (report != null && failed != null) failed(report);
        }

        private void ReleaseLocked(long now)
        {
            InputSpec input = pressed;
            pressed = null;
            if (input == null) return;
            try { send(input, false); }
            catch (Exception ex) { FailLocked(ex, now); }
            scheduler.FinishPulse(now);
        }

        private void FailLocked(Exception ex, long now)
        {
            if (!scheduler.Faulted) pendingFailure = ex;
            scheduler.Fail(now);
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                ReleaseLocked(clock());
                disposed = true;
                if (sensor != null) sensor.Dispose();
            }
        }
    }

    public sealed class IdleGamepadSettingsPanel : UserControl
    {
        private readonly CheckBox enabledBox;
        private readonly NumericUpDown secondsBox;
        private readonly NumericUpDown holdBox;
        private readonly ComboBox controlBox;
        private readonly NumericUpDown angleBox;
        private readonly NumericUpDown strengthBox;
        private readonly FlowLayoutPanel analogRow;
        private readonly Label angleLabel;
        private readonly List<GamepadControl> controls = new List<GamepadControl>();
        private readonly TableLayoutPanel fields;

        private static string T(string chinese, string english) { return Localizer.IsEnglish ? english : chinese; }

        public IdleGamepadSettingsPanel(IdleGamepadOptions value)
        {
            IdleGamepadOptions current = (value ?? new IdleGamepadOptions()).CloneNormalized();
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Top;
            Margin = new Padding(0);
            Padding = new Padding(4);
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Top;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            enabledBox = new CheckBox();
            enabledBox.AutoSize = true;
            enabledBox.Text = T("无操作时自动发送一次手柄输入", "Send a gamepad input after inactivity");
            enabledBox.Checked = current.Enabled;
            enabledBox.Margin = new Padding(0, 3, 0, 12);
            root.Controls.Add(enabledBox, 0, 0);
            fields = new TableLayoutPanel();
            fields.AutoSize = true;
            fields.Dock = DockStyle.Top;
            fields.ColumnCount = 2;
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            secondsBox = Number(10, 86400, current.IdleSeconds);
            holdBox = Number(50, 2000, current.HoldMilliseconds);
            AddRow(fields, T("无操作时间（秒）：", "Idle time (seconds):"), secondsBox, 0);
            controlBox = new ComboBox();
            controlBox.DropDownStyle = ComboBoxStyle.DropDownList;
            controlBox.Dock = DockStyle.Fill;
            controlBox.MinimumSize = new Size(210, 0);
            foreach (GamepadControl item in Enum.GetValues(typeof(GamepadControl)))
            {
                controls.Add(item);
                InputSpec display = IdleGamepadOptions.DefaultPulse();
                display.GamepadControl = item;
                controlBox.Items.Add(InputNames.FormatInput(display));
            }
            controlBox.SelectedIndex = controls.IndexOf(current.Pulse.GamepadControl);
            AddRow(fields, T("手柄操作：", "Gamepad input:"), controlBox, 1);
            analogRow = new FlowLayoutPanel();
            analogRow.AutoSize = true;
            analogRow.Dock = DockStyle.Top;
            analogRow.WrapContents = true;
            angleLabel = Label(T("方向（度）：", "Angle (degrees):"));
            angleBox = Number(-180, 180, 0);
            int angle, strength;
            GamepadVector.FromCartesian(current.Pulse.GamepadX, current.Pulse.GamepadY, out angle, out strength);
            angleBox.Value = angle;
            bool isTrigger = current.Pulse.GamepadControl == GamepadControl.LeftTrigger || current.Pulse.GamepadControl == GamepadControl.RightTrigger;
            strengthBox = Number(1, 100, Math.Max(1, isTrigger ? current.Pulse.GamepadValue : strength));
            analogRow.Controls.Add(angleLabel);
            analogRow.Controls.Add(angleBox);
            analogRow.Controls.Add(Label(T("力度（%）：", "Strength (%):")));
            analogRow.Controls.Add(strengthBox);
            fields.Controls.Add(analogRow, 0, 2);
            fields.SetColumnSpan(analogRow, 2);
            AddRow(fields, T("按住时间（毫秒）：", "Hold time (ms):"), holdBox, 3);
            root.Controls.Add(fields, 0, 1);
            Label explanation = Label(T(
                "键鼠或手柄操作、宏运行会重新计时；按住按键或摇杆也视为活动。紧急停止会暂停本功能，需在此重新启用。",
                "Keyboard, mouse, gamepad activity and macros restart the timer. Held keys/sticks count as activity. Emergency Stop suspends this feature until you enable it again here."));
            explanation.Dock = DockStyle.Top;
            explanation.Margin = new Padding(0, 12, 0, 6);
            explanation.ForeColor = Color.FromArgb(85, 96, 112);
            root.Controls.Add(explanation, 0, 2);
            Label background = Label(T(
                "可在后台发送，不切换窗口。是否接收由游戏决定；其他接收同一手柄的程序也可能响应。开始游戏前连接手柄可提高兼容性。",
                "Sends in the background without switching windows. The game decides whether it accepts background input; other apps using the same controller may also respond. Connect the controller before starting the game."));
            background.Dock = DockStyle.Top;
            background.Margin = new Padding(0, 4, 0, 6);
            background.ForeColor = Color.FromArgb(85, 96, 112);
            root.Controls.Add(background, 0, 3);
            root.SizeChanged += delegate
            {
                int width = Math.Max(220, root.ClientSize.Width - 8);
                explanation.MaximumSize = new Size(width, 0);
                background.MaximumSize = new Size(width, 0);
            };
            Controls.Add(root);
            enabledBox.CheckedChanged += delegate { UpdateFields(); };
            controlBox.SelectedIndexChanged += delegate { UpdateFields(); };
            UpdateFields();
        }

        private void UpdateFields()
        {
            fields.Enabled = enabledBox.Checked;
            GamepadControl control = controls[Math.Max(0, controlBox.SelectedIndex)];
            bool stick = control == GamepadControl.LeftStick || control == GamepadControl.RightStick;
            bool trigger = control == GamepadControl.LeftTrigger || control == GamepadControl.RightTrigger;
            analogRow.Visible = stick || trigger;
            angleLabel.Visible = angleBox.Visible = stick;
        }

        public IdleGamepadOptions ReadOptions()
        {
            IdleGamepadOptions value = new IdleGamepadOptions();
            value.Enabled = enabledBox.Checked;
            value.IdleSeconds = (int)secondsBox.Value;
            value.HoldMilliseconds = (int)holdBox.Value;
            value.Pulse.GamepadControl = controls[Math.Max(0, controlBox.SelectedIndex)];
            GamepadVector.ToCartesian((int)angleBox.Value, (int)strengthBox.Value, out value.Pulse.GamepadX, out value.Pulse.GamepadY);
            value.Pulse.GamepadValue = (int)strengthBox.Value;
            return value.CloneNormalized();
        }

        public void ResetDefaults()
        {
            LoadOptions(new IdleGamepadOptions());
        }

        public void LoadOptions(IdleGamepadOptions value)
        {
            IdleGamepadOptions current = (value ?? new IdleGamepadOptions()).CloneNormalized();
            enabledBox.Checked = current.Enabled;
            secondsBox.Value = current.IdleSeconds;
            holdBox.Value = current.HoldMilliseconds;
            controlBox.SelectedIndex = controls.IndexOf(current.Pulse.GamepadControl);
            int angle, strength;
            GamepadVector.FromCartesian(current.Pulse.GamepadX, current.Pulse.GamepadY, out angle, out strength);
            angleBox.Value = angle;
            bool trigger = current.Pulse.GamepadControl == GamepadControl.LeftTrigger || current.Pulse.GamepadControl == GamepadControl.RightTrigger;
            strengthBox.Value = Math.Max(1, trigger ? current.Pulse.GamepadValue : strength);
            UpdateFields();
        }

        private static NumericUpDown Number(int minimum, int maximum, int value)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = minimum; box.Maximum = maximum;
            box.Value = Math.Max(minimum, Math.Min(maximum, value));
            box.Width = 100;
            box.Margin = new Padding(0, 3, 8, 8);
            return box;
        }

        private static Label Label(string text)
        {
            Label label = new Label();
            label.Text = text; label.AutoSize = true;
            label.Margin = new Padding(0, 7, 8, 5);
            label.UseMnemonic = false;
            return label;
        }

        private static void AddRow(TableLayoutPanel table, string text, Control control, int row)
        {
            table.Controls.Add(Label(text), 0, row);
            table.Controls.Add(control, 1, row);
        }
    }

    // Observes activity only. No reports from another controller are forwarded.
    // Raw Input is used for physical Sony DS4 controls; XInput handles Xbox pads.
    internal sealed class IdleManualActivitySensor : IDisposable
    {
        private readonly Func<int> ownXboxSlot;
        private readonly Dictionary<IntPtr, Ds4State> ds4States = new Dictionary<IntPtr, Ds4State>();
        private readonly bool[] xboxConnected = new bool[4];
        private readonly ulong[] xboxSignatures = new ulong[4];
        private bool haveLastInput;
        private uint lastInput;
        private long nextPoll;
        private int xinputLibrary;
        private bool rawRegistered;
        private IntPtr registeredWindow;

        private sealed class Ds4State
        {
            public bool Physical;
            public bool Active;
            public bool HasReport;
            public ulong Signature;
        }

        public IdleManualActivitySensor(Func<int> ownXboxSlot)
        {
            this.ownXboxSlot = ownXboxSlot;
            Reset();
        }

        public void Reset()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            haveLastInput = GetLastInputInfo(ref info);
            lastInput = info.dwTime;
            nextPoll = 0;
            Array.Clear(xboxConnected, 0, xboxConnected.Length);
            Array.Clear(xboxSignatures, 0, xboxSignatures.Length);
        }

        public bool Poll(long now)
        {
            if (now < nextPoll) return false;
            nextPoll = now + 200;
            bool active = false;
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (GetLastInputInfo(ref info))
            {
                active = haveLastInput && lastInput != info.dwTime;
                lastInput = info.dwTime;
                haveLastInput = true;
            }
            // A held key can stop producing repeated events (mouse buttons and
            // modifiers, for example), but must never be considered inactivity.
            for (int key = 1; key < 255; key++)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0) { active = true; break; }
            }
            int excluded = -1;
            if (ownXboxSlot != null)
            {
                try { excluded = ownXboxSlot(); }
                catch { excluded = -1; }
            }
            for (int index = 0; index < 4; index++)
            {
                if (index == excluded) { xboxConnected[index] = false; continue; }
                XINPUT_STATE state;
                bool connected = TryXInput((uint)index, out state);
                if (!connected)
                {
                    if (xboxConnected[index]) active = true;
                    xboxConnected[index] = false;
                    continue;
                }
                bool held;
                ulong signature = XboxSignature(state.Gamepad, out held);
                if (held || !xboxConnected[index] || signature != xboxSignatures[index]) active = true;
                xboxConnected[index] = true;
                xboxSignatures[index] = signature;
            }
            foreach (Ds4State state in ds4States.Values)
                if (state.Physical && state.Active) active = true;
            return active;
        }

        internal static ulong XboxSignature(XINPUT_GAMEPAD pad, out bool active)
        {
            bool left = Math.Abs((int)pad.ThumbLX) > 7849 || Math.Abs((int)pad.ThumbLY) > 7849;
            bool right = Math.Abs((int)pad.ThumbRX) > 8689 || Math.Abs((int)pad.ThumbRY) > 8689;
            int lt = pad.LeftTrigger > 30 ? pad.LeftTrigger : 0;
            int rt = pad.RightTrigger > 30 ? pad.RightTrigger : 0;
            active = pad.Buttons != 0 || left || right || lt != 0 || rt != 0;
            // Exact button/trigger changes plus held-axis flags are sufficient:
            // held axes refresh continuously, while center jitter stays inactive.
            return (ulong)pad.Buttons | ((ulong)lt << 16) | ((ulong)rt << 24) |
                (left ? 1UL << 32 : 0) | (right ? 1UL << 33 : 0);
        }

        private bool TryXInput(uint index, out XINPUT_STATE state)
        {
            state = new XINPUT_STATE();
            if (xinputLibrary == -1) return false;
            try
            {
                if (xinputLibrary == 0 || xinputLibrary == 4)
                {
                    try { uint result = XInputGetState14(index, out state); xinputLibrary = 4; return result == 0; }
                    catch (DllNotFoundException) { xinputLibrary = 3; }
                    catch (EntryPointNotFoundException) { xinputLibrary = 3; }
                }
                if (xinputLibrary == 3)
                {
                    try { uint result = XInputGetState13(index, out state); return result == 0; }
                    catch (DllNotFoundException) { xinputLibrary = 9; }
                    catch (EntryPointNotFoundException) { xinputLibrary = 9; }
                }
                return XInputGetState91(index, out state) == 0;
            }
            catch (DllNotFoundException) { xinputLibrary = -1; }
            catch (EntryPointNotFoundException) { xinputLibrary = -1; }
            return false;
        }

        public void AttachWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero || (rawRegistered && registeredWindow == handle)) return;
            RAWINPUTDEVICE[] devices = NewRegistrations(0x100 | 0x2000, handle); // INPUTSINK | DEVNOTIFY
            rawRegistered = RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
            if (rawRegistered) registeredWindow = handle;
        }

        private static RAWINPUTDEVICE[] NewRegistrations(uint flags, IntPtr handle)
        {
            RAWINPUTDEVICE[] values = new RAWINPUTDEVICE[2];
            values[0].UsagePage = values[1].UsagePage = 1;
            values[0].Usage = 4; values[1].Usage = 5; // Joystick, gamepad
            values[0].Flags = values[1].Flags = flags;
            values[0].Target = values[1].Target = handle;
            return values;
        }

        public bool ProcessWindowMessage(int message, IntPtr wParam, IntPtr lParam)
        {
            if (!rawRegistered) return false;
            if (message == 0x00FE)
            {
                bool wasActive = false;
                Ds4State state;
                if (ds4States.TryGetValue(lParam, out state)) wasActive = state.Active;
                ds4States.Remove(lParam);
                return wasActive;
            }
            if (message != 0x00FF) return false;
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            if (GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref size, headerSize) != 0 || size < headerSize + 8 || size > 65536) return false;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint copied = GetRawInputData(lParam, 0x10000003, buffer, ref size, headerSize);
                if (copied != size) return false;
                RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                if (header.Type != 2) return false;
                Ds4State state;
                if (!ds4States.TryGetValue(header.Device, out state))
                {
                    if (ds4States.Count >= 128) ds4States.Clear();
                    state = new Ds4State();
                    state.Physical = IsPhysicalDs4(header.Device);
                    ds4States[header.Device] = state;
                }
                if (!state.Physical) return false;
                int reportSize = Marshal.ReadInt32(buffer, (int)headerSize);
                int count = Marshal.ReadInt32(buffer, (int)headerSize + 4);
                if (reportSize < 10 || reportSize > 512 || count < 1 || count > 128 ||
                    (long)headerSize + 8 + (long)reportSize * count > size) return false;
                bool activity = false;
                byte[] report = new byte[reportSize];
                for (int i = 0; i < count; i++)
                {
                    Marshal.Copy(IntPtr.Add(buffer, (int)headerSize + 8 + i * reportSize), report, 0, reportSize);
                    bool active;
                    ulong signature;
                    if (!TryDs4Signature(report, out signature, out active)) continue;
                    if (active || (state.HasReport && signature != state.Signature)) activity = true;
                    state.Signature = signature; state.Active = active; state.HasReport = true;
                }
                return activity;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        internal static bool TryDs4Signature(byte[] report, out ulong signature, out bool active)
        {
            signature = 0; active = false;
            if (report == null || report.Length < 10) return false;
            int offset = report[0] == 0x01 ? 1 : report[0] == 0x11 ? 3 : -1;
            if (offset < 0 || report.Length < offset + 9) return false;
            // Sony DS4 USB/minimal Bluetooth/full Bluetooth control fields.
            // Sequence counters, timestamps and motion-sensor noise are ignored.
            bool sticks = Math.Abs(report[offset] - 128) > 24 || Math.Abs(report[offset + 1] - 128) > 24 ||
                Math.Abs(report[offset + 2] - 128) > 24 || Math.Abs(report[offset + 3] - 128) > 24;
            int buttons = report[offset + 4] | (report[offset + 5] << 8) | ((report[offset + 6] & 3) << 16);
            int lt = report[offset + 7] > 30 ? report[offset + 7] : 0;
            int rt = report[offset + 8] > 30 ? report[offset + 8] : 0;
            active = sticks || (buttons & 0xFFFFF0) != 0 || (buttons & 15) < 8 || lt != 0 || rt != 0;
            signature = (ulong)(uint)buttons | ((ulong)(uint)lt << 24) | ((ulong)(uint)rt << 32) | (sticks ? 1UL << 40 : 0);
            return true;
        }

        private static bool IsPhysicalDs4(IntPtr device)
        {
            // Bluetooth HID paths do not necessarily use USB's VID_ / PID_
            // spelling. Read the device's numeric identity before walking its
            // parents, so USB and Bluetooth take the same validation path.
            RID_DEVICE_INFO info = new RID_DEVICE_INFO();
            info.Size = (uint)Marshal.SizeOf(typeof(RID_DEVICE_INFO));
            uint infoSize = info.Size;
            if (GetRawInputDeviceInfo(device, 0x2000000B, ref info, ref infoSize) == uint.MaxValue ||
                info.Type != 2 || !IsSupportedDs4Device(info.VendorId, info.ProductId)) return false;
            uint length = 0;
            if (GetRawInputDeviceInfo(device, 0x20000007, null, ref length) == uint.MaxValue || length == 0 || length > 4096) return false;
            StringBuilder path = new StringBuilder((int)length + 1);
            if (GetRawInputDeviceInfo(device, 0x20000007, path, ref length) == uint.MaxValue) return false;
            string name = path.ToString();
            if (name.StartsWith("\\\\?\\", StringComparison.Ordinal)) name = name.Substring(4);
            int suffix = name.LastIndexOf("#{", StringComparison.Ordinal);
            if (suffix >= 0) name = name.Substring(0, suffix);
            name = name.Replace('#', '\\');
            uint node;
            if (CM_Locate_DevNode(out node, name, 0) != 0) return false;
            bool physical = false;
            for (int depth = 0; depth < 12; depth++)
            {
                StringBuilder id = new StringBuilder(512);
                if (CM_Get_Device_ID(node, id, id.Capacity, 0) != 0) return false;
                string instance = id.ToString();
                if (instance.IndexOf("VIGEM", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                if (instance.StartsWith("USB\\VID_054C", StringComparison.OrdinalIgnoreCase) ||
                    instance.StartsWith("BTHENUM\\", StringComparison.OrdinalIgnoreCase) ||
                    instance.StartsWith("BTHLEDEVICE\\", StringComparison.OrdinalIgnoreCase)) physical = true;
                uint parent;
                if (CM_Get_Parent(out parent, node, 0) != 0) break;
                node = parent;
            }
            return physical;
        }

        internal static bool IsSupportedDs4Device(uint vendorId, uint productId)
        {
            return vendorId == 0x054C && (productId == 0x05C4 || productId == 0x09CC || productId == 0x0BA0);
        }

        public void Dispose()
        {
            if (rawRegistered)
            {
                RAWINPUTDEVICE[] devices = NewRegistrations(1, IntPtr.Zero); // REMOVE
                RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
                rawRegistered = false;
            }
            ds4States.Clear();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct XINPUT_GAMEPAD
        {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE { public uint PacketNumber; public XINPUT_GAMEPAD Gamepad; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE { public ushort UsagePage, Usage; public uint Flags; public IntPtr Target; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER { public uint Type, Size; public IntPtr Device, WParam; }
        // The native union is 24 bytes (its keyboard member is largest).
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct RID_DEVICE_INFO
        {
            [FieldOffset(0)] public uint Size;
            [FieldOffset(4)] public uint Type;
            [FieldOffset(8)] public uint VendorId;
            [FieldOffset(12)] public uint ProductId;
        }
        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] private static extern uint XInputGetState14(uint index, out XINPUT_STATE state);
        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")] private static extern uint XInputGetState13(uint index, out XINPUT_STATE state);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")] private static extern uint XInputGetState91(uint index, out XINPUT_STATE state);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);
        [DllImport("user32.dll")] private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetRawInputDeviceInfoW")]
        private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, StringBuilder data, ref uint size);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetRawInputDeviceInfoW")]
        private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, ref RID_DEVICE_INFO data, ref uint size);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Locate_DevNodeW")]
        private static extern uint CM_Locate_DevNode(out uint node, string deviceId, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW")]
        private static extern uint CM_Get_Device_ID(uint node, StringBuilder buffer, int length, uint flags);
        [DllImport("cfgmgr32.dll")] private static extern uint CM_Get_Parent(out uint parent, uint node, uint flags);
    }
}
