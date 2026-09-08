using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InputStitch.Tools.InputLab
{
    internal sealed class InputLabForm : Form
    {
        private readonly Dictionary<int, Label> keyLabels = new Dictionary<int, Label>();
        private readonly Dictionary<string, Label> mouseLabels = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ushort, Label> gamepadLabels = new Dictionary<ushort, Label>();
        private readonly HashSet<int> keysDown = new HashSet<int>();
        private readonly HashSet<string> mouseButtonsDown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> rawKeysDown = new HashSet<int>();
        private readonly HashSet<string> rawMouseButtonsDown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly XInputReader xinput = new XInputReader();

        private readonly ListView eventLog = new ListView();
        private readonly CheckBox pauseLog = new CheckBox();
        private readonly CheckBox logMouseMove = new CheckBox();
        private readonly Label foregroundStatus = new Label();
        private readonly Label hookStatus = new Label();
        private readonly Label eventCountStatus = new Label();
        private readonly Label mousePosition = new Label();
        private readonly Label mouseDelta = new Label();
        private readonly Label mouseWheel = new Label();
        private readonly Label xinputStatus = new Label();
        private readonly Label leftStickValue = new Label();
        private readonly Label rightStickValue = new Label();
        private readonly Label leftTriggerValue = new Label();
        private readonly Label rightTriggerValue = new Label();
        private readonly ProgressBar leftTrigger = new ProgressBar();
        private readonly ProgressBar rightTrigger = new ProgressBar();
        private readonly StickView leftStick = new StickView();
        private readonly StickView rightStick = new StickView();
        private readonly Timer refreshTimer = new Timer();

        private NativeInput.HookProc keyboardProc;
        private NativeInput.HookProc mouseProc;
        private IntPtr keyboardHook;
        private IntPtr mouseHook;
        private bool haveMousePoint;
        private NativeInput.POINT lastMousePoint;
        private int verticalWheelTotal;
        private int horizontalWheelTotal;
        private bool haveXInputState;
        private uint activeXInputSlot = uint.MaxValue;
        private int preferredXInputSlot = -1;
        private XInputReader.XINPUT_STATE previousXInputState;
        private int rawKeyboardEventCount;
        private int rawMouseEventCount;
        private bool rawInputRegistered;
        private int injectedKeyboardEventCount;
        private int injectedMouseEventCount;
        private int lastHookKeyboardVirtualKey = -1;
        private bool lastHookKeyboardDown;
        private int lastRawKeyboardVirtualKey = -1;
        private bool lastRawKeyboardDown;
        private readonly bool automationMode;

        private static readonly Color IdleColor = Color.FromArgb(242, 244, 247);
        private static readonly Color ActiveColor = Color.FromArgb(197, 236, 205);
        private static readonly Color InjectedColor = Color.FromArgb(203, 225, 250);
        private static readonly Color TextColor = Color.FromArgb(32, 38, 48);

        internal InputLabForm(int initialView)
            : this(initialView, false)
        {
        }

        internal InputLabForm(int initialView, bool automationMode)
        {
            this.automationMode = automationMode;
            Text = "InputStitch Input Lab v0.2";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 700);
            Size = new Size(1220, 840);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.White;
            KeyPreview = true;

            BuildUi(initialView);

            keyboardProc = KeyboardHookCallback;
            mouseProc = MouseHookCallback;
            refreshTimer.Interval = 16;
            refreshTimer.Tick += delegate { RefreshRuntimeState(); };

            Shown += delegate
            {
                InstallHooks();
                InstallRawInput();
                refreshTimer.Start();
                LogEvent("Lab", "Ready", "RUNNING", automationMode
                    ? "Background acceptance observer ready; foreground activation is disabled."
                    : "Bring this window to foreground, then trigger InputStitch mappings.");
            };
            FormClosed += delegate
            {
                refreshTimer.Stop();
                if (keyboardHook != IntPtr.Zero) NativeInput.UnhookWindowsHookEx(keyboardHook);
                if (mouseHook != IntPtr.Zero) NativeInput.UnhookWindowsHookEx(mouseHook);
            };
        }

        protected override bool ShowWithoutActivation
        {
            get { return automationMode; }
        }

        private void BuildUi(int initialView)
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            TabPage keyboardTab = new TabPage("Keyboard");
            TabPage devicesTab = new TabPage("Mouse + XInput");
            TabPage logTab = new TabPage("Event log");

            keyboardTab.Controls.Add(BuildKeyboardPanel());

            TableLayoutPanel devices = new TableLayoutPanel();
            devices.Dock = DockStyle.Fill;
            devices.ColumnCount = 2;
            devices.RowCount = 1;
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            devices.Controls.Add(BuildMousePanel(), 0, 0);
            devices.Controls.Add(BuildGamepadPanel(), 1, 0);
            devicesTab.Controls.Add(devices);

            logTab.Controls.Add(BuildLogPanel());
            tabs.TabPages.Add(keyboardTab);
            tabs.TabPages.Add(devicesTab);
            tabs.TabPages.Add(logTab);
            tabs.SelectedIndex = Math.Max(0, Math.Min(2, initialView));
            root.Controls.Add(tabs, 0, 1);
        }

        private Control BuildHeader()
        {
            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 2;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            Label title = new Label();
            title.Text = "Black-box input target — keyboard, mouse and XInput";
            title.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.ForeColor = TextColor;
            header.Controls.Add(title, 0, 0);

            foregroundStatus.Dock = DockStyle.Fill;
            foregroundStatus.TextAlign = ContentAlignment.MiddleRight;
            foregroundStatus.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            header.Controls.Add(foregroundStatus, 1, 0);

            hookStatus.Dock = DockStyle.Fill;
            hookStatus.Text = "Hooks: not started";
            hookStatus.TextAlign = ContentAlignment.MiddleLeft;
            hookStatus.ForeColor = Color.DimGray;
            header.Controls.Add(hookStatus, 0, 1);

            eventCountStatus.Dock = DockStyle.Fill;
            eventCountStatus.Text = "Events: 0";
            eventCountStatus.TextAlign = ContentAlignment.MiddleRight;
            eventCountStatus.ForeColor = Color.DimGray;
            header.Controls.Add(eventCountStatus, 1, 1);
            return header;
        }

        private Control BuildKeyboardPanel()
        {
            GroupBox group = CreateGroup("Keyboard — global low-level hook");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(8);
            layout.RowCount = 7;
            layout.ColumnCount = 1;
            layout.AutoScroll = true;
            for (int i = 0; i < 7; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));

            layout.Controls.Add(CreateKeyRow(new object[,] {
                {27,"Esc",44},{112,"F1",40},{113,"F2",40},{114,"F3",40},{115,"F4",40},{116,"F5",40},{117,"F6",40},{118,"F7",40},{119,"F8",40},{120,"F9",40},{121,"F10",44},{122,"F11",44},{123,"F12",44}
            }), 0, 0);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {192,"`",36},{49,"1",36},{50,"2",36},{51,"3",36},{52,"4",36},{53,"5",36},{54,"6",36},{55,"7",36},{56,"8",36},{57,"9",36},{48,"0",36},{189,"-",36},{187,"=",36},{8,"Back",62}
            }), 0, 1);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {9,"Tab",54},{81,"Q",38},{87,"W",38},{69,"E",38},{82,"R",38},{84,"T",38},{89,"Y",38},{85,"U",38},{73,"I",38},{79,"O",38},{80,"P",38},{219,"[",38},{221,"]",38},{220,"\\",42}
            }), 0, 2);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {20,"Caps",62},{65,"A",40},{83,"S",40},{68,"D",40},{70,"F",40},{71,"G",40},{72,"H",40},{74,"J",40},{75,"K",40},{76,"L",40},{186,";",40},{222,"'",40},{13,"Enter",72}
            }), 0, 3);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {160,"LShift",78},{90,"Z",42},{88,"X",42},{67,"C",42},{86,"V",42},{66,"B",42},{78,"N",42},{77,"M",42},{188,",",42},{190,".",42},{191,"/",42},{161,"RShift",82}
            }), 0, 4);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {162,"LCtrl",66},{91,"LWin",62},{164,"LAlt",58},{32,"Space",210},{165,"RAlt",58},{92,"RWin",62},{93,"Menu",58},{163,"RCtrl",66}
            }), 0, 5);
            layout.Controls.Add(CreateKeyRow(new object[,] {
                {37,"←",48},{38,"↑",48},{40,"↓",48},{39,"→",48},{45,"Ins",48},{46,"Del",48},{36,"Home",52},{35,"End",48},{33,"PgUp",54},{34,"PgDn",54}
            }), 0, 6);

            group.Controls.Add(layout);
            return group;
        }

        private FlowLayoutPanel CreateKeyRow(object[,] keys)
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.WrapContents = false;
            row.AutoScroll = true;
            row.Padding = new Padding(0, 3, 0, 0);
            int count = keys.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                int vk = (int)keys[i, 0];
                string text = (string)keys[i, 1];
                int width = (int)keys[i, 2];
                Label label = CreateStateLabel(text, width, 34);
                label.Tag = vk;
                keyLabels[vk] = label;
                row.Controls.Add(label);
            }
            return row;
        }

        private Control BuildMousePanel()
        {
            GroupBox group = CreateGroup("Mouse");
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.Padding = new Padding(10);
            panel.AutoScroll = true;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.WrapContents = true;
            AddMouseButton(buttons, "Left", "L");
            AddMouseButton(buttons, "Right", "R");
            AddMouseButton(buttons, "Middle", "M");
            AddMouseButton(buttons, "X1", "X1");
            AddMouseButton(buttons, "X2", "X2");
            panel.Controls.Add(buttons);

            mousePosition.AutoSize = true;
            mousePosition.Margin = new Padding(4, 14, 4, 4);
            mousePosition.Text = "Position: —";
            panel.Controls.Add(mousePosition);
            mouseDelta.AutoSize = true;
            mouseDelta.Margin = new Padding(4);
            mouseDelta.Text = "Delta: —";
            panel.Controls.Add(mouseDelta);
            mouseWheel.AutoSize = true;
            mouseWheel.Margin = new Padding(4);
            mouseWheel.Text = "Wheel V/H: 0 / 0";
            panel.Controls.Add(mouseWheel);

            logMouseMove.AutoSize = true;
            logMouseMove.Text = "Log mouse movement";
            logMouseMove.Margin = new Padding(4, 16, 4, 4);
            logMouseMove.Checked = false;
            panel.Controls.Add(logMouseMove);

            Label hint = new Label();
            hint.AutoSize = true;
            hint.MaximumSize = new Size(220, 0);
            hint.Margin = new Padding(4, 14, 4, 4);
            hint.ForeColor = Color.DimGray;
            hint.Text = "Blue = injected (SendInput). Green = non-injected. Mouse move logging is off by default to avoid flooding.";
            panel.Controls.Add(hint);

            group.Controls.Add(panel);
            return group;
        }

        private void AddMouseButton(Control parent, string name, string text)
        {
            Label label = CreateStateLabel(text, 52, 42);
            mouseLabels[name] = label;
            parent.Controls.Add(label);
        }

        private Control BuildGamepadPanel()
        {
            GroupBox group = CreateGroup("XInput controller");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(8);
            layout.ColumnCount = 2;
            layout.RowCount = 6;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            xinputStatus.Dock = DockStyle.Fill;
            xinputStatus.Text = "No controller";
            xinputStatus.TextAlign = ContentAlignment.MiddleLeft;
            xinputStatus.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            layout.SetColumnSpan(xinputStatus, 2);
            layout.Controls.Add(xinputStatus, 0, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.WrapContents = true;
            AddGamepadButton(buttons, XInputReader.A, "A");
            AddGamepadButton(buttons, XInputReader.B, "B");
            AddGamepadButton(buttons, XInputReader.X, "X");
            AddGamepadButton(buttons, XInputReader.Y, "Y");
            AddGamepadButton(buttons, XInputReader.LEFT_SHOULDER, "LB");
            AddGamepadButton(buttons, XInputReader.RIGHT_SHOULDER, "RB");
            AddGamepadButton(buttons, XInputReader.LEFT_THUMB, "LS");
            AddGamepadButton(buttons, XInputReader.RIGHT_THUMB, "RS");
            layout.SetColumnSpan(buttons, 2);
            layout.Controls.Add(buttons, 0, 1);

            FlowLayoutPanel dpad = new FlowLayoutPanel();
            dpad.Dock = DockStyle.Fill;
            dpad.WrapContents = true;
            AddGamepadButton(dpad, XInputReader.DPAD_UP, "↑");
            AddGamepadButton(dpad, XInputReader.DPAD_DOWN, "↓");
            AddGamepadButton(dpad, XInputReader.DPAD_LEFT, "←");
            AddGamepadButton(dpad, XInputReader.DPAD_RIGHT, "→");
            AddGamepadButton(dpad, XInputReader.BACK, "Back");
            AddGamepadButton(dpad, XInputReader.START, "Start");
            layout.SetColumnSpan(dpad, 2);
            layout.Controls.Add(dpad, 0, 2);

            leftStick.Dock = DockStyle.Fill;
            rightStick.Dock = DockStyle.Fill;
            layout.Controls.Add(leftStick, 0, 3);
            layout.Controls.Add(rightStick, 1, 3);

            leftStickValue.Dock = DockStyle.Fill;
            rightStickValue.Dock = DockStyle.Fill;
            leftStickValue.TextAlign = ContentAlignment.MiddleCenter;
            rightStickValue.TextAlign = ContentAlignment.MiddleCenter;
            leftStickValue.Text = "LS 0.000, 0.000";
            rightStickValue.Text = "RS 0.000, 0.000";
            layout.Controls.Add(leftStickValue, 0, 4);
            layout.Controls.Add(rightStickValue, 1, 4);

            layout.Controls.Add(BuildTriggerPanel("LT", leftTrigger, leftTriggerValue), 0, 5);
            layout.Controls.Add(BuildTriggerPanel("RT", rightTrigger, rightTriggerValue), 1, 5);

            group.Controls.Add(layout);
            return group;
        }

        private Control BuildTriggerPanel(string name, ProgressBar bar, Label value)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 3;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
            Label nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;
            bar.Minimum = 0;
            bar.Maximum = 255;
            bar.Dock = DockStyle.Fill;
            value.Dock = DockStyle.Fill;
            value.Text = "0%";
            value.TextAlign = ContentAlignment.MiddleRight;
            panel.Controls.Add(nameLabel, 0, 0);
            panel.Controls.Add(bar, 1, 0);
            panel.Controls.Add(value, 2, 0);
            return panel;
        }

        private void AddGamepadButton(Control parent, ushort flag, string text)
        {
            Label label = CreateStateLabel(text, text.Length > 2 ? 56 : 42, 34);
            gamepadLabels[flag] = label;
            parent.Controls.Add(label);
        }

        private Control BuildLogPanel()
        {
            GroupBox group = CreateGroup("Event log");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.WrapContents = false;
            Button clear = new Button();
            clear.Text = "Clear log";
            clear.AutoSize = true;
            clear.Click += delegate { eventLog.Items.Clear(); eventCountStatus.Text = "Events: 0"; clock.Restart(); };
            actions.Controls.Add(clear);
            pauseLog.AutoSize = true;
            pauseLog.Text = "Pause logging";
            pauseLog.Margin = new Padding(12, 8, 4, 4);
            actions.Controls.Add(pauseLog);
            Label tip = new Label();
            tip.AutoSize = true;
            tip.Margin = new Padding(14, 9, 4, 4);
            tip.ForeColor = Color.DimGray;
            tip.Text = "Injected=SendInput/other synthetic input; XInput is polled independently.";
            actions.Controls.Add(tip);
            layout.Controls.Add(actions, 0, 0);

            eventLog.Dock = DockStyle.Fill;
            eventLog.View = View.Details;
            eventLog.FullRowSelect = true;
            eventLog.GridLines = true;
            eventLog.HideSelection = false;
            eventLog.Columns.Add("Time", 96);
            eventLog.Columns.Add("Device", 82);
            eventLog.Columns.Add("Input", 130);
            eventLog.Columns.Add("State", 92);
            eventLog.Columns.Add("Details", 720);
            layout.Controls.Add(eventLog, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox CreateGroup(string text)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(5);
            return box;
        }

        private Label CreateStateLabel(string text, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.Width = width;
            label.Height = height;
            label.Margin = new Padding(2);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.BackColor = IdleColor;
            label.ForeColor = TextColor;
            return label;
        }

        private void InstallHooks()
        {
            keyboardHook = NativeInput.InstallHook(NativeInput.WH_KEYBOARD_LL, keyboardProc);
            mouseHook = NativeInput.InstallHook(NativeInput.WH_MOUSE_LL, mouseProc);
            bool keyboardOk = keyboardHook != IntPtr.Zero;
            bool mouseOk = mouseHook != IntPtr.Zero;
            hookStatus.Text = "Keyboard hook: " + (keyboardOk ? "OK" : "FAILED") + "   |   Mouse hook: " + (mouseOk ? "OK" : "FAILED") + "   |   XInput polling: active";
            hookStatus.ForeColor = keyboardOk && mouseOk ? Color.FromArgb(32, 120, 55) : Color.FromArgb(180, 55, 45);
            if (!keyboardOk || !mouseOk)
            {
                int error = Marshal.GetLastWin32Error();
                LogEvent("Lab", "Hook", "ERROR", "SetWindowsHookEx failed. Win32 error " + error.ToString());
            }
        }

        private void InstallRawInput()
        {
            NativeInput.RAWINPUTDEVICE[] devices = new NativeInput.RAWINPUTDEVICE[2];
            devices[0].usUsagePage = 0x01;
            devices[0].usUsage = 0x02; // mouse
            devices[0].dwFlags = automationMode ? NativeInput.RIDEV_INPUTSINK : 0;
            devices[0].hwndTarget = Handle;
            devices[1].usUsagePage = 0x01;
            devices[1].usUsage = 0x06; // keyboard
            devices[1].dwFlags = automationMode ? NativeInput.RIDEV_INPUTSINK : 0;
            devices[1].hwndTarget = Handle;
            rawInputRegistered = NativeInput.RegisterRawInputDevices(devices, (uint)devices.Length,
                (uint)Marshal.SizeOf(typeof(NativeInput.RAWINPUTDEVICE)));
            UpdateHookStatus();
            if (!rawInputRegistered)
                LogEvent("Lab", "Raw Input", "ERROR", "RegisterRawInputDevices failed. Win32 error " + Marshal.GetLastWin32Error().ToString());
            else
                LogEvent("Lab", "Raw Input", "READY", automationMode
                    ? "Background keyboard/mouse Raw Input comparison enabled for acceptance."
                    : "Foreground keyboard/mouse Raw Input comparison enabled.");
        }

        private void UpdateHookStatus()
        {
            bool keyboardOk = keyboardHook != IntPtr.Zero;
            bool mouseOk = mouseHook != IntPtr.Zero;
            hookStatus.Text = "Keyboard hook: " + (keyboardOk ? "OK" : "FAILED") +
                "   |   Mouse hook: " + (mouseOk ? "OK" : "FAILED") +
                "   |   Raw Input: " + (rawInputRegistered ? "OK" : "not ready") +
                " (K " + rawKeyboardEventCount.ToString() + " / M " + rawMouseEventCount.ToString() + ")" +
                "   |   XInput: active";
            hookStatus.ForeColor = keyboardOk && mouseOk && rawInputRegistered
                ? Color.FromArgb(32, 120, 55) : Color.FromArgb(180, 95, 45);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeInput.WM_INPUT) HandleRawInput(m.LParam);
            base.WndProc(ref m);
        }

        private void HandleRawInput(IntPtr lParam)
        {
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(NativeInput.RAWINPUTHEADER));
            if (NativeInput.GetRawInputData(lParam, NativeInput.RID_INPUT, IntPtr.Zero, ref size, headerSize) != 0 || size < headerSize || size > 65536)
                return;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint copied = NativeInput.GetRawInputData(lParam, NativeInput.RID_INPUT, buffer, ref size, headerSize);
                if (copied != size) return;
                NativeInput.RAWINPUTHEADER header = (NativeInput.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(NativeInput.RAWINPUTHEADER));
                IntPtr payload = IntPtr.Add(buffer, (int)headerSize);
                if (header.dwType == NativeInput.RIM_TYPEKEYBOARD)
                {
                    NativeInput.RAWKEYBOARD raw = (NativeInput.RAWKEYBOARD)Marshal.PtrToStructure(payload, typeof(NativeInput.RAWKEYBOARD));
                    rawKeyboardEventCount++;
                    bool up = (raw.Flags & NativeInput.RI_KEY_BREAK) != 0 || raw.Message == NativeInput.WM_KEYUP || raw.Message == NativeInput.WM_SYSKEYUP;
                    if (up) rawKeysDown.Remove(raw.VKey); else rawKeysDown.Add(raw.VKey);
                    lastRawKeyboardVirtualKey = raw.VKey;
                    lastRawKeyboardDown = !up;
                    LogEvent("Raw Keyboard", ((Keys)raw.VKey).ToString() + " (VK " + raw.VKey.ToString() + ")", up ? "UP" : "DOWN",
                        "make=0x" + raw.MakeCode.ToString("X2") + ", flags=0x" + raw.Flags.ToString("X2") + ", device=" + header.hDevice.ToString());
                }
                else if (header.dwType == NativeInput.RIM_TYPEMOUSE)
                {
                    NativeInput.RAWMOUSE raw = (NativeInput.RAWMOUSE)Marshal.PtrToStructure(payload, typeof(NativeInput.RAWMOUSE));
                    rawMouseEventCount++;
                    ushort flags = raw.Buttons.usButtonFlags;
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_LEFT_BUTTON_DOWN, "Left", true, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_LEFT_BUTTON_UP, "Left", false, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_RIGHT_BUTTON_DOWN, "Right", true, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_RIGHT_BUTTON_UP, "Right", false, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_MIDDLE_BUTTON_DOWN, "Middle", true, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_MIDDLE_BUTTON_UP, "Middle", false, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_BUTTON_4_DOWN, "X1", true, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_BUTTON_4_UP, "X1", false, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_BUTTON_5_DOWN, "X2", true, header.hDevice);
                    LogRawMouseButton(flags, NativeInput.RI_MOUSE_BUTTON_5_UP, "X2", false, header.hDevice);
                    if ((flags & NativeInput.RI_MOUSE_WHEEL) != 0)
                    {
                        short delta = unchecked((short)raw.Buttons.usButtonData);
                        LogEvent("Raw Mouse", "Wheel V", delta >= 0 ? "UP" : "DOWN", "delta=" + Signed(delta) + ", device=" + header.hDevice.ToString());
                    }
                    if ((flags & NativeInput.RI_MOUSE_HWHEEL) != 0)
                    {
                        short delta = unchecked((short)raw.Buttons.usButtonData);
                        LogEvent("Raw Mouse", "Wheel H", delta >= 0 ? "RIGHT" : "LEFT", "delta=" + Signed(delta) + ", device=" + header.hDevice.ToString());
                    }
                    if (logMouseMove.Checked && (raw.lLastX != 0 || raw.lLastY != 0))
                        LogEvent("Raw Mouse", "Move", "MOVE", "dx=" + Signed(raw.lLastX) + ", dy=" + Signed(raw.lLastY) + ", device=" + header.hDevice.ToString());
                }
                UpdateHookStatus();
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private void LogRawMouseButton(ushort flags, ushort mask, string name, bool down, IntPtr device)
        {
            if ((flags & mask) == 0) return;
            if (down) rawMouseButtonsDown.Add(name); else rawMouseButtonsDown.Remove(name);
            LogEvent("Raw Mouse", name, down ? "DOWN" : "UP", "device=" + device.ToString());
        }
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == NativeInput.HC_ACTION)
            {
                int message = wParam.ToInt32();
                bool down = message == NativeInput.WM_KEYDOWN || message == NativeInput.WM_SYSKEYDOWN;
                bool up = message == NativeInput.WM_KEYUP || message == NativeInput.WM_SYSKEYUP;
                if (down || up)
                {
                    NativeInput.KBDLLHOOKSTRUCT data = (NativeInput.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeInput.KBDLLHOOKSTRUCT));
                    int vk = (int)data.vkCode;
                    bool injected = NativeInput.IsInjectedKeyboard(data.flags);
                    if (injected) injectedKeyboardEventCount++;
                    lastHookKeyboardVirtualKey = vk;
                    lastHookKeyboardDown = down;
                    bool repeat = down && keysDown.Contains(vk);
                    if (down) keysDown.Add(vk); else keysDown.Remove(vk);
                    SetKeyState(vk, down, injected);
                    string keyName = GetKeyName(vk);
                    string detail = (injected ? "injected" : "physical/system") + ", scan=0x" + data.scanCode.ToString("X2") + ", flags=0x" + data.flags.ToString("X2");
                    if (NativeInput.IsLowerIntegrityKeyboard(data.flags)) detail += ", lower-IL";
                    if (repeat) detail += ", repeat";
                    LogEvent("Keyboard", keyName, down ? "DOWN" : "UP", detail);
                    if (automationMode && injected && vk == (int)Keys.K)
                        return new IntPtr(1);
                }
            }
            return NativeInput.CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == NativeInput.HC_ACTION)
            {
                int message = wParam.ToInt32();
                NativeInput.MSLLHOOKSTRUCT data = (NativeInput.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeInput.MSLLHOOKSTRUCT));
                bool injected = NativeInput.IsInjectedMouse(data.flags);
                if (injected && message != NativeInput.WM_MOUSEMOVE) injectedMouseEventCount++;
                if (message == NativeInput.WM_MOUSEMOVE)
                {
                    int dx = 0;
                    int dy = 0;
                    if (haveMousePoint)
                    {
                        dx = data.pt.X - lastMousePoint.X;
                        dy = data.pt.Y - lastMousePoint.Y;
                    }
                    lastMousePoint = data.pt;
                    haveMousePoint = true;
                    mousePosition.Text = "Position: " + data.pt.X.ToString() + ", " + data.pt.Y.ToString();
                    mouseDelta.Text = "Delta: " + Signed(dx) + ", " + Signed(dy) + (injected ? "  [injected]" : "");
                    if (logMouseMove.Checked && (dx != 0 || dy != 0))
                        LogEvent("Mouse", "Move", "MOVE", "dx=" + Signed(dx) + ", dy=" + Signed(dy) + ", " + (injected ? "injected" : "physical/system"));
                }
                else if (message == NativeInput.WM_LBUTTONDOWN || message == NativeInput.WM_LBUTTONUP)
                {
                    HandleMouseButton("Left", message == NativeInput.WM_LBUTTONDOWN, injected, data.flags);
                }
                else if (message == NativeInput.WM_RBUTTONDOWN || message == NativeInput.WM_RBUTTONUP)
                {
                    HandleMouseButton("Right", message == NativeInput.WM_RBUTTONDOWN, injected, data.flags);
                }
                else if (message == NativeInput.WM_MBUTTONDOWN || message == NativeInput.WM_MBUTTONUP)
                {
                    HandleMouseButton("Middle", message == NativeInput.WM_MBUTTONDOWN, injected, data.flags);
                }
                else if (message == NativeInput.WM_XBUTTONDOWN || message == NativeInput.WM_XBUTTONUP)
                {
                    ushort button = NativeInput.HighWordUnsigned(data.mouseData);
                    HandleMouseButton(button == 1 ? "X1" : "X2", message == NativeInput.WM_XBUTTONDOWN, injected, data.flags);
                    if (automationMode && injected && button == 2)
                        return new IntPtr(1);
                }
                else if (message == NativeInput.WM_MOUSEWHEEL)
                {
                    short delta = NativeInput.HighWord(data.mouseData);
                    verticalWheelTotal += delta;
                    UpdateWheelText();
                    LogEvent("Mouse", "Wheel V", delta > 0 ? "UP" : "DOWN", "delta=" + Signed(delta) + ", total=" + verticalWheelTotal.ToString() + ", " + (injected ? "injected" : "physical/system"));
                }
                else if (message == NativeInput.WM_MOUSEHWHEEL)
                {
                    short delta = NativeInput.HighWord(data.mouseData);
                    horizontalWheelTotal += delta;
                    UpdateWheelText();
                    LogEvent("Mouse", "Wheel H", delta > 0 ? "RIGHT" : "LEFT", "delta=" + Signed(delta) + ", total=" + horizontalWheelTotal.ToString() + ", " + (injected ? "injected" : "physical/system"));
                }
            }
            return NativeInput.CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private void HandleMouseButton(string name, bool down, bool injected, uint flags)
        {
            Label label;
            if (down) mouseButtonsDown.Add(name); else mouseButtonsDown.Remove(name);
            if (mouseLabels.TryGetValue(name, out label)) ApplyStateColor(label, down, injected);
            string detail = (injected ? "injected" : "physical/system") + ", flags=0x" + flags.ToString("X2");
            if (NativeInput.IsLowerIntegrityMouse(flags)) detail += ", lower-IL";
            LogEvent("Mouse", name, down ? "DOWN" : "UP", detail);
        }

        private void RefreshRuntimeState()
        {
            bool foreground = NativeInput.GetForegroundWindow() == Handle;
            foregroundStatus.Text = foreground ? "TARGET: FOREGROUND" : "TARGET: BACKGROUND";
            foregroundStatus.ForeColor = foreground ? Color.FromArgb(30, 130, 60) : Color.FromArgb(175, 95, 25);
            PollXInput();
        }

        private void PollXInput()
        {
            XInputReader.XINPUT_STATE state = new XInputReader.XINPUT_STATE();
            uint found = uint.MaxValue;
            if (preferredXInputSlot >= 0 && preferredXInputSlot < 4)
            {
                uint preferred = (uint)preferredXInputSlot;
                if (xinput.TryGetState(preferred, out state)) found = preferred;
            }
            else
            {
                for (uint i = 0; i < 4; i++)
                {
                    if (xinput.TryGetState(i, out state)) { found = i; break; }
                }
            }

            if (found == uint.MaxValue)
            {
                if (haveXInputState) LogEvent("XInput", "Controller", "DISCONNECTED", "Previously slot " + activeXInputSlot.ToString());
                haveXInputState = false;
                activeXInputSlot = uint.MaxValue;
                xinputStatus.Text = "No XInput controller detected";
                ResetGamepadUi();
                return;
            }

            if (!haveXInputState || activeXInputSlot != found)
            {
                LogEvent("XInput", "Controller", "CONNECTED", "slot=" + found.ToString());
                haveXInputState = true;
                activeXInputSlot = found;
                previousXInputState = new XInputReader.XINPUT_STATE();
            }

            xinputStatus.Text = "XInput slot " + found.ToString() + " connected  |  packet " + state.dwPacketNumber.ToString();
            UpdateGamepadUi(state.Gamepad);
            LogGamepadChanges(previousXInputState.Gamepad, state.Gamepad);
            previousXInputState = state;
        }

        private void UpdateGamepadUi(XInputReader.XINPUT_GAMEPAD pad)
        {
            foreach (KeyValuePair<ushort, Label> pair in gamepadLabels)
                ApplyStateColor(pair.Value, (pad.wButtons & pair.Key) != 0, false);

            leftTrigger.Value = pad.bLeftTrigger;
            rightTrigger.Value = pad.bRightTrigger;
            leftTriggerValue.Text = Math.Round(XInputReader.NormalizeTrigger(pad.bLeftTrigger) * 100.0).ToString("0") + "%";
            rightTriggerValue.Text = Math.Round(XInputReader.NormalizeTrigger(pad.bRightTrigger) * 100.0).ToString("0") + "%";

            double lx = XInputReader.NormalizeStick(pad.sThumbLX);
            double ly = XInputReader.NormalizeStick(pad.sThumbLY);
            double rx = XInputReader.NormalizeStick(pad.sThumbRX);
            double ry = XInputReader.NormalizeStick(pad.sThumbRY);
            leftStick.SetValue(lx, ly);
            rightStick.SetValue(rx, ry);
            leftStickValue.Text = "LS " + lx.ToString("+0.000;-0.000;0.000") + ", " + ly.ToString("+0.000;-0.000;0.000");
            rightStickValue.Text = "RS " + rx.ToString("+0.000;-0.000;0.000") + ", " + ry.ToString("+0.000;-0.000;0.000");
        }

        private void LogGamepadChanges(XInputReader.XINPUT_GAMEPAD oldPad, XInputReader.XINPUT_GAMEPAD newPad)
        {
            foreach (KeyValuePair<ushort, Label> pair in gamepadLabels)
            {
                bool oldDown = (oldPad.wButtons & pair.Key) != 0;
                bool newDown = (newPad.wButtons & pair.Key) != 0;
                if (oldDown != newDown) LogEvent("XInput", pair.Value.Text, newDown ? "DOWN" : "UP", "slot=" + activeXInputSlot.ToString());
            }
            if (Math.Abs(newPad.bLeftTrigger - oldPad.bLeftTrigger) >= 2)
                LogEvent("XInput", "LT", "ANALOG", newPad.bLeftTrigger.ToString() + "/255 = " + XInputReader.NormalizeTrigger(newPad.bLeftTrigger).ToString("0.000"));
            if (Math.Abs(newPad.bRightTrigger - oldPad.bRightTrigger) >= 2)
                LogEvent("XInput", "RT", "ANALOG", newPad.bRightTrigger.ToString() + "/255 = " + XInputReader.NormalizeTrigger(newPad.bRightTrigger).ToString("0.000"));

            if (StickChanged(oldPad.sThumbLX, oldPad.sThumbLY, newPad.sThumbLX, newPad.sThumbLY))
                LogEvent("XInput", "Left Stick", "VECTOR", FormatStick(newPad.sThumbLX, newPad.sThumbLY));
            if (StickChanged(oldPad.sThumbRX, oldPad.sThumbRY, newPad.sThumbRX, newPad.sThumbRY))
                LogEvent("XInput", "Right Stick", "VECTOR", FormatStick(newPad.sThumbRX, newPad.sThumbRY));
        }

        private static bool StickChanged(short oldX, short oldY, short newX, short newY)
        {
            return Math.Abs((int)newX - oldX) >= 768 || Math.Abs((int)newY - oldY) >= 768 ||
                   ((newX == 0 && newY == 0) && (oldX != 0 || oldY != 0));
        }

        private static string FormatStick(short x, short y)
        {
            return "raw=(" + x.ToString() + "," + y.ToString() + ") normalized=(" +
                XInputReader.NormalizeStick(x).ToString("+0.000;-0.000;0.000") + "," +
                XInputReader.NormalizeStick(y).ToString("+0.000;-0.000;0.000") + ")";
        }

        private void ResetGamepadUi()
        {
            foreach (Label label in gamepadLabels.Values) ApplyStateColor(label, false, false);
            leftTrigger.Value = 0;
            rightTrigger.Value = 0;
            leftTriggerValue.Text = "0%";
            rightTriggerValue.Text = "0%";
            leftStick.SetValue(0, 0);
            rightStick.SetValue(0, 0);
            leftStickValue.Text = "LS 0.000, 0.000";
            rightStickValue.Text = "RS 0.000, 0.000";
        }

        private void SetKeyState(int vk, bool down, bool injected)
        {
            Label label;
            if (keyLabels.TryGetValue(vk, out label)) ApplyStateColor(label, down, injected);
        }

        private static void ApplyStateColor(Label label, bool down, bool injected)
        {
            label.BackColor = down ? (injected ? InjectedColor : ActiveColor) : IdleColor;
        }

        private string GetKeyName(int vk)
        {
            Label label;
            if (keyLabels.TryGetValue(vk, out label)) return label.Text + " (VK " + vk.ToString() + ")";
            string name = ((Keys)vk).ToString();
            return name + " (VK " + vk.ToString() + ")";
        }

        private void UpdateWheelText()
        {
            mouseWheel.Text = "Wheel V/H: " + verticalWheelTotal.ToString() + " / " + horizontalWheelTotal.ToString();
        }

        private void LogEvent(string device, string input, string state, string details)
        {
            if (pauseLog.Checked) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string>(LogEvent), device, input, state, details);
                return;
            }
            string time = clock.Elapsed.TotalMilliseconds.ToString("000000.0") + " ms";
            ListViewItem item = new ListViewItem(time);
            item.SubItems.Add(device);
            item.SubItems.Add(input);
            item.SubItems.Add(state);
            item.SubItems.Add(details);
            eventLog.Items.Add(item);
            while (eventLog.Items.Count > 1500) eventLog.Items.RemoveAt(0);
            eventCountStatus.Text = "Events: " + eventLog.Items.Count.ToString();
            if (eventLog.Items.Count > 0) eventLog.EnsureVisible(eventLog.Items.Count - 1);
        }

        internal void SetPreferredXInputSlot(int slot)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { SetPreferredXInputSlot(slot); });
                return;
            }
            preferredXInputSlot = slot >= 0 && slot < 4 ? slot : -1;
            haveXInputState = false;
            activeXInputSlot = uint.MaxValue;
            previousXInputState = new XInputReader.XINPUT_STATE();
            PollXInput();
        }

        internal ObservationSnapshot CaptureObservation()
        {
            if (InvokeRequired) return (ObservationSnapshot)Invoke(new System.Func<ObservationSnapshot>(CaptureObservation));
            ObservationSnapshot snapshot = new ObservationSnapshot();
            snapshot.XInputConnected = haveXInputState;
            snapshot.XInputSlot = activeXInputSlot;
            snapshot.Gamepad = previousXInputState.Gamepad;
            snapshot.KeysDown.AddRange(keysDown);
            snapshot.MouseButtonsDown.AddRange(mouseButtonsDown);
            snapshot.RawKeysDown.AddRange(rawKeysDown);
            snapshot.RawMouseButtonsDown.AddRange(rawMouseButtonsDown);
            snapshot.RawInputRegistered = rawInputRegistered;
            snapshot.RawKeyboardEvents = rawKeyboardEventCount;
            snapshot.RawMouseEvents = rawMouseEventCount;
            snapshot.InjectedKeyboardEvents = injectedKeyboardEventCount;
            snapshot.InjectedMouseEvents = injectedMouseEventCount;
            snapshot.LastHookKeyboardVirtualKey = lastHookKeyboardVirtualKey;
            snapshot.LastHookKeyboardDown = lastHookKeyboardDown;
            snapshot.LastRawKeyboardVirtualKey = lastRawKeyboardVirtualKey;
            snapshot.LastRawKeyboardDown = lastRawKeyboardDown;
            return snapshot;
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value.ToString() : value.ToString();
        }
    }
}
