using System;
using System.Runtime.InteropServices;

namespace InputStitch.Tools.InputLab
{
    internal sealed class XInputReader
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct XINPUT_GAMEPAD
        {
            internal ushort wButtons;
            internal byte bLeftTrigger;
            internal byte bRightTrigger;
            internal short sThumbLX;
            internal short sThumbLY;
            internal short sThumbRX;
            internal short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XINPUT_STATE
        {
            internal uint dwPacketNumber;
            internal XINPUT_GAMEPAD Gamepad;
        }

        internal const ushort DPAD_UP = 0x0001;
        internal const ushort DPAD_DOWN = 0x0002;
        internal const ushort DPAD_LEFT = 0x0004;
        internal const ushort DPAD_RIGHT = 0x0008;
        internal const ushort START = 0x0010;
        internal const ushort BACK = 0x0020;
        internal const ushort LEFT_THUMB = 0x0040;
        internal const ushort RIGHT_THUMB = 0x0080;
        internal const ushort LEFT_SHOULDER = 0x0100;
        internal const ushort RIGHT_SHOULDER = 0x0200;
        internal const ushort A = 0x1000;
        internal const ushort B = 0x2000;
        internal const ushort X = 0x4000;
        internal const ushort Y = 0x8000;

        private int preferredLibrary;

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState14(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState13(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState910(uint dwUserIndex, out XINPUT_STATE pState);

        internal bool TryGetState(uint index, out XINPUT_STATE state)
        {
            state = new XINPUT_STATE();
            if (preferredLibrary != 0)
            {
                try
                {
                    uint result = Call(preferredLibrary, index, out state);
                    return result == 0;
                }
                catch (DllNotFoundException) { preferredLibrary = 0; }
                catch (EntryPointNotFoundException) { preferredLibrary = 0; }
            }

            int[] libraries = new int[] { 14, 13, 910 };
            foreach (int library in libraries)
            {
                try
                {
                    uint result = Call(library, index, out state);
                    preferredLibrary = library;
                    return result == 0;
                }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
            }
            return false;
        }

        private static uint Call(int library, uint index, out XINPUT_STATE state)
        {
            if (library == 14) return XInputGetState14(index, out state);
            if (library == 13) return XInputGetState13(index, out state);
            return XInputGetState910(index, out state);
        }

        internal static double NormalizeStick(short value)
        {
            if (value < 0) return value / 32768.0;
            return value / 32767.0;
        }

        internal static double NormalizeTrigger(byte value)
        {
            return value / 255.0;
        }
    }
}
