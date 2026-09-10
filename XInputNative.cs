using System;
using System.Runtime.InteropServices;

namespace InputStitch
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XInputNativeGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XInputNativeState
    {
        public uint PacketNumber;
        public XInputNativeGamepad Gamepad;
    }

    // Shared native XInput fallback reader. The selected DLL remains per consumer instance,
    // preserving the independent fallback state that existed before this refactor.
    internal sealed class XInputNativeReader
    {
        private int library;

        internal bool TryGetState(uint index, out XInputNativeState state)
        {
            state = new XInputNativeState();
            if (library == -1) return false;
            try
            {
                if (library == 0 || library == 4)
                {
                    try { uint result = XInputGetState14(index, out state); library = 4; return result == 0; }
                    catch (DllNotFoundException) { library = 3; }
                    catch (EntryPointNotFoundException) { library = 3; }
                }
                if (library == 3)
                {
                    try { uint result = XInputGetState13(index, out state); return result == 0; }
                    catch (DllNotFoundException) { library = 9; }
                    catch (EntryPointNotFoundException) { library = 9; }
                }
                return XInputGetState91(index, out state) == 0;
            }
            catch (DllNotFoundException) { library = -1; }
            catch (EntryPointNotFoundException) { library = -1; }
            return false;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState14(uint index, out XInputNativeState state);
        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState13(uint index, out XInputNativeState state);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState91(uint index, out XInputNativeState state);
    }
}
