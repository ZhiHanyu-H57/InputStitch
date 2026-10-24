using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Test-only: all framework threads start on the private desktop. There is no
// SwitchDesktop import or switch access, and the test has no visible takeover.
internal static class IsolatedUiDesktop
{
    private const string DesktopEnvironment = "INPUTSTITCH_TEST_DESKTOP";
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        internal int Size; internal string Reserved, Desktop, Title;
        internal int X, Y, Width, Height, Columns, Rows, Fill, Flags;
        internal short Show, ReservedSize; internal IntPtr ReservedPointer, Input, Output, Error;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInfo { internal IntPtr Process, Thread; internal uint Id, ThreadId; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr mode, int flags, uint access, IntPtr attributes);
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDesktop(uint thread);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr OpenInputDesktop(int flags, bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder value, int length, out int needed);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(string application, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes,
        bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInfo process);
    [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
    [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll")] private static extern bool GetExitCodeProcess(IntPtr process, out uint code);
    [DllImport("kernel32.dll")] private static extern bool TerminateProcess(IntPtr process, uint code);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    private static string Name(IntPtr handle)
    { int needed; StringBuilder value = new StringBuilder(256); return GetUserObjectInformation(handle, 2, value, value.Capacity * 2, out needed) ? value.ToString() : ""; }
    private static string InputName()
    { IntPtr value = OpenInputDesktop(0, false, 1); if (value == IntPtr.Zero) return "unavailable"; try { return Name(value); } finally { CloseDesktop(value); } }
    private static string Quote(string value)
    {
        StringBuilder result = new StringBuilder("\""); int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes); result.Append(c); slashes = 0;
        }
        result.Append('\\', slashes * 2); return result.Append('"').ToString();
    }
    internal static int Run(Func<int> body)
    {
        string requested = Environment.GetEnvironmentVariable(DesktopEnvironment);
        if (!string.IsNullOrEmpty(requested))
        {
            if (!requested.StartsWith("InputStitchTest-", StringComparison.Ordinal) || Name(GetThreadDesktop(GetCurrentThreadId())) != requested || InputName() == requested)
                throw new Exception("Private UI process desktop verification failed.");
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.ThrowException);
            Console.WriteLine("PRIVATE UI process bound: " + requested);
            return body();
        }
        string original = Name(GetThreadDesktop(GetCurrentThreadId())), input = InputName();
        string name = "InputStitchTest-" + Guid.NewGuid().ToString("N");
        IntPtr desktop = CreateDesktop(name, IntPtr.Zero, IntPtr.Zero, 0, 0xcf, IntPtr.Zero);
        if (desktop == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot create private UI test desktop.");
        ProcessInfo child = new ProcessInfo(); bool exited = false; int samples = 0;
        try
        {
            if (Name(GetThreadDesktop(GetCurrentThreadId())) != original) throw new Exception("Caller desktop changed.");
            string executable = Process.GetCurrentProcess().MainModule.FileName;
            StringBuilder command = new StringBuilder(Quote(executable));
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 1; i < arguments.Length; i++) command.Append(' ').Append(Quote(arguments[i]));
            StartupInfo startup = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)), Desktop = name, Flags = 0x100,
                Input = GetStdHandle(-10), Output = GetStdHandle(-11), Error = GetStdHandle(-12) };
            Environment.SetEnvironmentVariable(DesktopEnvironment, name);
            try
            {
                if (!CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, true, 0x08000000, IntPtr.Zero,
                    Environment.CurrentDirectory, ref startup, out child)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot start private UI test process.");
            }
            finally { Environment.SetEnvironmentVariable(DesktopEnvironment, null); }
            CloseHandle(child.Thread); child.Thread = IntPtr.Zero;
            Stopwatch timeout = Stopwatch.StartNew(); bool foregroundSeen = false;
            while (WaitForSingleObject(child.Process, 50) == 258)
            {
                uint foreground; GetWindowThreadProcessId(GetForegroundWindow(), out foreground); samples++;
                foregroundSeen |= foreground == child.Id;
                if (timeout.Elapsed.TotalMinutes > 10) throw new TimeoutException("Private UI verification exceeded ten minutes.");
            }
            exited = true; uint code;
            if (!GetExitCodeProcess(child.Process, out code)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (foregroundSeen || InputName() != input) throw new Exception("UI verification disturbed the user's input desktop.");
            Console.WriteLine("PASS private UI desktop; foreground test-window samples=0/" + samples + "; input desktop unchanged.");
            return (int)code;
        }
        finally
        {
            if (child.Process != IntPtr.Zero)
            {
                if (!exited) { TerminateProcess(child.Process, 1); WaitForSingleObject(child.Process, 5000); }
                CloseHandle(child.Process);
            }
            if (child.Thread != IntPtr.Zero) CloseHandle(child.Thread);
            if (!CloseDesktop(desktop)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot release private UI desktop.");
            Console.WriteLine("PASS private UI process exited and desktop handle released.");
        }
    }
}
