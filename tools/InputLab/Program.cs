using System;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("InputStitch Input Lab")]
[assembly: AssemblyProduct("InputStitch Input Lab")]
[assembly: AssemblyDescription("Black-box input observation and acceptance tool for InputStitch")]
[assembly: AssemblyCompany("InputStitch")]
[assembly: AssemblyVersion("0.3.1.0")]
[assembly: AssemblyFileVersion("0.3.1.0")]
[assembly: AssemblyInformationalVersion("0.3.1")]

namespace InputStitch.Tools.InputLab
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (HasArgument(args, "--self-test-window-messages"))
                return RunWindowMessageSelfTest();
            if (HasArgument(args, "--self-test-keyboard-visual"))
                return RunKeyboardVisualSelfTest();

            int initialView = 0;
            for (int i = 0; args != null && i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "--view", StringComparison.OrdinalIgnoreCase)) continue;
                string value = args[i + 1];
                if (string.Equals(value, "devices", StringComparison.OrdinalIgnoreCase)) initialView = 1;
                else if (string.Equals(value, "log", StringComparison.OrdinalIgnoreCase)) initialView = 2;
                break;
            }

            Application.Run(new InputLabForm(initialView));
            return 0;
        }

        private static bool HasArgument(string[] args, string value)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static int RunWindowMessageSelfTest()
        {
            int result = 3;
            Timer timer = null;
            InputLabForm lab = new InputLabForm(2);
            lab.ShowInTaskbar = false;
            lab.WindowState = FormWindowState.Minimized;
            lab.Shown += delegate
            {
                int scanCode = 0x25; // K
                long keyDown = 1L | ((long)scanCode << 16);
                long keyUp = keyDown | (1L << 30) | (1L << 31);
                int mousePoint = 12 | (12 << 16);
                bool posted =
                    NativeInput.PostMessage(lab.Handle, NativeInput.WM_KEYDOWN, new IntPtr((int)Keys.K), new IntPtr(keyDown)) &&
                    NativeInput.PostMessage(lab.Handle, NativeInput.WM_KEYUP, new IntPtr((int)Keys.K), new IntPtr(keyUp)) &&
                    NativeInput.PostMessage(lab.Handle, NativeInput.WM_LBUTTONDOWN, IntPtr.Zero, new IntPtr(mousePoint)) &&
                    NativeInput.PostMessage(lab.Handle, NativeInput.WM_LBUTTONUP, IntPtr.Zero, new IntPtr(mousePoint));
                if (!posted)
                {
                    result = 2;
                    lab.Close();
                    return;
                }

                timer = new Timer();
                timer.Interval = 120;
                timer.Tick += delegate
                {
                    timer.Stop();
                    ObservationSnapshot snapshot = lab.CaptureObservation();
                    result = snapshot.WindowKeyboardEvents >= 2 && snapshot.WindowMouseEvents >= 2 ? 0 : 1;
                    lab.Close();
                };
                timer.Start();
            };

            Application.Run(lab);
            if (timer != null) timer.Dispose();
            return result;
        }

        private static int RunKeyboardVisualSelfTest()
        {
            int result = 3;
            Timer timer = null;
            int phase = 0;
            InputLabForm lab = new InputLabForm(0, true);
            lab.ShowInTaskbar = false;
            lab.WindowState = FormWindowState.Minimized;
            lab.Shown += delegate
            {
                timer = new Timer();
                timer.Interval = 70;
                timer.Tick += delegate
                {
                    if (phase == 0)
                    {
                        lab.ApplyRawKeyboardVisualStateForSelfTest((int)Keys.K, true);
                        phase = 1;
                        return;
                    }
                    if (phase == 1)
                    {
                        ObservationSnapshot snapshot = lab.CaptureObservation();
                        if (!snapshot.KeysDown.Contains((int)Keys.K) || !lab.IsKeyVisuallyHighlighted((int)Keys.K))
                        {
                            result = 1;
                            lab.ApplyRawKeyboardVisualStateForSelfTest((int)Keys.K, false);
                            lab.Close();
                            return;
                        }
                        lab.ApplyRawKeyboardVisualStateForSelfTest((int)Keys.K, false);
                        phase = 2;
                        return;
                    }
                    if (phase == 2)
                    {
                        ObservationSnapshot snapshot = lab.CaptureObservation();
                        if (snapshot.KeysDown.Contains((int)Keys.K) || !lab.IsKeyVisuallyHighlighted((int)Keys.K))
                        {
                            result = 1;
                            lab.Close();
                            return;
                        }
                        timer.Interval = 230;
                        phase = 3;
                        return;
                    }

                    result = lab.IsKeyVisuallyHighlighted((int)Keys.K) ? 1 : 0;
                    lab.Close();
                };
                timer.Start();
            };

            Application.Run(lab);
            if (timer != null) timer.Dispose();
            return result;
        }
    }
}
