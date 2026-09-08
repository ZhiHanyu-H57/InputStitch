using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InputStitch.Tools.InputLab
{
    internal static class NativeInput
    {
        internal const int WH_KEYBOARD_LL = 13;
        internal const int WH_MOUSE_LL = 14;
        internal const int HC_ACTION = 0;

        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;
        internal const int WM_SYSKEYDOWN = 0x0104;
        internal const int WM_SYSKEYUP = 0x0105;
        internal const int WM_MOUSEMOVE = 0x0200;
        internal const int WM_LBUTTONDOWN = 0x0201;
        internal const int WM_LBUTTONUP = 0x0202;
        internal const int WM_RBUTTONDOWN = 0x0204;
        internal const int WM_RBUTTONUP = 0x0205;
        internal const int WM_MBUTTONDOWN = 0x0207;
        internal const int WM_MBUTTONUP = 0x0208;
        internal const int WM_MOUSEWHEEL = 0x020A;
        internal const int WM_XBUTTONDOWN = 0x020B;
        internal const int WM_XBUTTONUP = 0x020C;
        internal const int WM_MOUSEHWHEEL = 0x020E;

        internal const uint LLKHF_EXTENDED = 0x01;
        internal const uint LLKHF_LOWER_IL_INJECTED = 0x02;
        internal const uint LLKHF_INJECTED = 0x10;
        internal const uint LLKHF_ALTDOWN = 0x20;
        internal const uint LLMHF_INJECTED = 0x00000001;
        internal const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KBDLLHOOKSTRUCT
        {
            internal uint vkCode;
            internal uint scanCode;
            internal uint flags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MSLLHOOKSTRUCT
        {
            internal POINT pt;
            internal uint mouseData;
            internal uint flags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        internal static IntPtr InstallHook(int hookType, HookProc proc)
        {
            using (Process current = Process.GetCurrentProcess())
            using (ProcessModule module = current.MainModule)
            {
                IntPtr moduleHandle = GetModuleHandle(module == null ? null : module.ModuleName);
                return SetWindowsHookEx(hookType, proc, moduleHandle, 0);
            }
        }

        internal static bool IsInjectedKeyboard(uint flags)
        {
            return (flags & LLKHF_INJECTED) != 0;
        }

        internal static bool IsLowerIntegrityKeyboard(uint flags)
        {
            return (flags & LLKHF_LOWER_IL_INJECTED) != 0;
        }

        internal static bool IsInjectedMouse(uint flags)
        {
            return (flags & LLMHF_INJECTED) != 0;
        }

        internal static bool IsLowerIntegrityMouse(uint flags)
        {
            return (flags & LLMHF_LOWER_IL_INJECTED) != 0;
        }

        internal static short HighWord(uint value)
        {
            return unchecked((short)((value >> 16) & 0xFFFF));
        }

        internal static ushort HighWordUnsigned(uint value)
        {
            return (ushort)((value >> 16) & 0xFFFF);
        }
    }
}
