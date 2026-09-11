using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class RawKeyboardEvent
    {
        public int VirtualKey;
        public int ScanCode;
        public bool Extended;
        public bool Down;
    }

    // Passive physical-keyboard fallback for WH_KEYBOARD_LL.
    //
    // Windows can silently remove a low-level hook when its owning thread misses the
    // LowLevelHooksTimeout deadline. The application receives no invalid-handle callback,
    // so a non-zero hook handle is not a health check. Raw Input is registered independently
    // and lets MainForm detect a real keyboard edge that the hook did not observe, process
    // that one edge, then rebuild the low-level hooks.
    //
    // This class does not run macros and does not suppress input. It only reports physical
    // keyboard edges; MainForm keeps the single authoritative trigger/runtime path.
    internal sealed class RawKeyboardFallback : IDisposable
    {
        private const int WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEKEYBOARD = 1;
        private const uint RIDEV_REMOVE = 0x00000001;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        internal const ushort RI_KEY_BREAK = 0x0001;
        internal const ushort RI_KEY_E0 = 0x0002;
        internal const ushort RI_KEY_E1 = 0x0004;

        private bool registered;
        private IntPtr targetWindow;
        private long eventCount;

        public event Action<RawKeyboardEvent> Input;
        public bool Registered { get { return registered; } }
        public long EventCount { get { return Interlocked.Read(ref eventCount); } }

        public void AttachWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero || (registered && targetWindow == handle)) return;
            RAWINPUTDEVICE device = new RAWINPUTDEVICE();
            device.UsagePage = 0x01;
            device.Usage = 0x06; // Generic Desktop / Keyboard
            device.Flags = RIDEV_INPUTSINK;
            device.Target = handle;
            registered = RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
            if (registered) targetWindow = handle;
        }

        public bool ProcessWindowMessage(int message, IntPtr lParam)
        {
            if (!registered || message != WM_INPUT || lParam == IntPtr.Zero) return false;
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            if (GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, headerSize) != 0 ||
                size < headerSize + (uint)Marshal.SizeOf(typeof(RAWKEYBOARD)) || size > 65536) return false;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint copied = GetRawInputData(lParam, RID_INPUT, buffer, ref size, headerSize);
                if (copied != size) return false;
                RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                if (header.Type != RIM_TYPEKEYBOARD) return false;
                RAWKEYBOARD raw = (RAWKEYBOARD)Marshal.PtrToStructure(IntPtr.Add(buffer, (int)headerSize), typeof(RAWKEYBOARD));
                int vk = NormalizeVirtualKey(raw.VKey, raw.MakeCode, raw.Flags);
                if (vk <= 0 || vk == 255) return false;

                RawKeyboardEvent value = new RawKeyboardEvent();
                value.VirtualKey = vk;
                value.ScanCode = raw.MakeCode;
                value.Extended = IsExtended(raw.Flags);
                value.Down = !IsKeyUp(raw.Flags, raw.Message);
                Interlocked.Increment(ref eventCount);
                Action<RawKeyboardEvent> handler = Input;
                if (handler != null) handler(value);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        internal static int NormalizeVirtualKey(int virtualKey, ushort makeCode, ushort flags)
        {
            if (virtualKey == (int)Keys.ShiftKey)
                return makeCode == 0x36 ? (int)Keys.RShiftKey : (int)Keys.LShiftKey;
            if (virtualKey == (int)Keys.ControlKey)
                return (flags & RI_KEY_E0) != 0 ? (int)Keys.RControlKey : (int)Keys.LControlKey;
            if (virtualKey == (int)Keys.Menu)
                return (flags & RI_KEY_E0) != 0 ? (int)Keys.RMenu : (int)Keys.LMenu;
            return virtualKey;
        }

        internal static bool IsKeyUp(ushort flags, uint message)
        {
            return (flags & RI_KEY_BREAK) != 0 || message == 0x0101 || message == 0x0105;
        }

        internal static bool IsExtended(ushort flags)
        {
            // LLKHF_EXTENDED corresponds to an E0-prefixed key. E1 is kept separate by
            // Windows and should not make legacy Enter-style MatchExtended rules fire.
            return (flags & RI_KEY_E0) != 0;
        }

        public void Dispose()
        {
            if (registered)
            {
                RAWINPUTDEVICE device = new RAWINPUTDEVICE();
                device.UsagePage = 0x01;
                device.Usage = 0x06;
                device.Flags = RIDEV_REMOVE;
                device.Target = IntPtr.Zero;
                RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
            }
            registered = false;
            targetWindow = IntPtr.Zero;
            Input = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr WParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);
    }
}
