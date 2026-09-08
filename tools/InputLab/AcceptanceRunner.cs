using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch.Tools.InputLab
{
    internal static class AcceptanceProgram
    {
        private sealed class AcceptanceBlockedException : Exception
        {
            public AcceptanceBlockedException(string message) : base(message) { }
        }

        private const int SettleMs = 140;
        private static readonly MethodInfo HandleDown = typeof(MainForm).GetMethod("HandleTerminalInput", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo HandleUp = typeof(MainForm).GetMethod("HandleTerminalInputReleased", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly StringBuilder Report = new StringBuilder();
        private static int failures;
        private static int checks;
        private static string reportPath;

        [STAThread]
        private static void Main(string[] args)
        {
            reportPath = ResolveReportPath(args);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MacroConfig config = BuildAcceptanceConfig();
            InputSender.UseScanCodeInput = true;
            GamepadOutput.Configure(VirtualGamepadTypes.Xbox360);

            string isolatedDir = Path.Combine(Path.GetTempPath(), "InputStitch-Acceptance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(isolatedDir);

            InputLabForm lab = new InputLabForm(1, true);
            lab.Text = "InputStitch Input Lab — automated ownership acceptance";
            lab.ShowInTaskbar = false;
            lab.WindowState = FormWindowState.Minimized;

            MainForm product = new MainForm(config, isolatedDir, new DirectOutputBackend());
            product.ShowInTaskbar = false;
            product.WindowState = FormWindowState.Minimized;
            if (product.Handle == IntPtr.Zero) throw new InvalidOperationException("Acceptance host could not create the InputStitch window handle.");
            GamepadOutput.EnsureConnected();
            GamepadOutput.NeutralizeAll();

            int exitCode = 1;
            lab.Shown += delegate
            {
                Thread worker = new Thread(delegate()
                {
                    try
                    {
                        Thread.Sleep(350);
                        RunAcceptance(lab, product);
                        exitCode = failures == 0 ? 0 : 1;
                    }
                    catch (AcceptanceBlockedException ex)
                    {
                        string reason = ex.Message ?? "local acceptance environment is unavailable";
                        Report.AppendLine();
                        Report.AppendLine("BLOCKED | environment-preflight | " + reason);
                        Report.AppendLine("SUMMARY: BLOCKED | checks=" + checks.ToString() + " | failures=" + failures.ToString());
                        Console.WriteLine("BLOCKED | environment-preflight | " + reason);
                        exitCode = 2;
                    }
                    catch (Exception ex)
                    {
                        Fail("acceptance-runner-exception", ex.GetBaseException().ToString());
                        exitCode = 1;
                    }
                    finally
                    {
                        try { GamepadOutput.NeutralizeAll(); } catch { }
                        try { GamepadOutput.Disconnect(); } catch { }
                        WriteReport();
                        try
                        {
                            lab.BeginInvoke((MethodInvoker)delegate
                            {
                                try { product.Dispose(); } catch { }
                                lab.Close();
                            });
                        }
                        catch { }
                    }
                });
                worker.IsBackground = true;
                worker.Name = "InputLabAcceptance";
                worker.Start();
            };

            Application.Run(lab);
            try { Directory.Delete(isolatedDir, true); } catch { }
            Environment.ExitCode = exitCode;
        }

        private static string ResolveReportPath(string[] args)
        {
            for (int i = 0; args != null && i < args.Length - 1; i++)
                if (string.Equals(args[i], "--report", StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(args[i + 1]);
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acceptance-report.txt"));
        }

        private static void RunAcceptance(InputLabForm lab, MainForm product)
        {
            Report.AppendLine("InputStitch Input Lab automated ownership acceptance");
            Report.AppendLine("Started UTC: " + DateTime.UtcNow.ToString("o"));
            Report.AppendLine("Product version: " + AppInfo.Version);
            Report.AppendLine();

            // Keyboard/mouse lanes do not require ViGEm. Run them first so an XInput
            // environment blocker cannot hide useful SendInput/Raw Input evidence.
            ScenarioKeyboardRawComparison(lab, product);
            ScenarioMouseRawComparison(lab, product);

            BindVirtualXboxObservation(lab, product);
            ObservationSnapshot startup = lab.CaptureObservation();
            Info("Startup XInput snapshot", startup.XInputConnected
                ? ("slot=" + startup.XInputSlot.ToString() + ", LS=(" +
                    XInputReader.NormalizeStick(startup.Gamepad.sThumbLX).ToString("0.000") + "," +
                    XInputReader.NormalizeStick(startup.Gamepad.sThumbLY).ToString("0.000") + ")")
                : "no controller visible yet");
            ScenarioWasdReleaseOrder(lab, product);
            ScenarioOpposingStickAxes(lab, product);
            ScenarioTriggerMaximum(lab, product);
            ScenarioSharedDigitalOwnership(lab, product);
            ScenarioDPadAxisMerge(lab, product);
            ScenarioFourHeldPlusOrdinary(lab, product);
            ScenarioEmergencyStop(lab, product);
            Neutral(product, lab, "final-neutral");

            Report.AppendLine();
            Report.AppendLine("SUMMARY: " + (failures == 0 ? "PASS" : "FAIL") + " | checks=" + checks.ToString() + " | failures=" + failures.ToString());
        }

        private static void BindVirtualXboxObservation(InputLabForm lab, MainForm product)
        {
            InputSpec y = GamepadSpec(GamepadControl.North, 0, 0, 100);
            InputSpec lt = GamepadSpec(GamepadControl.LeftTrigger, 0, 0, 41);
            InputSpec rs = GamepadSpec(GamepadControl.RightStick, 37, -63, 100);
            OutputOwnershipManager ownership = GetOwnershipManager(product);
            const string probeSource = "acceptance-probe";
            ownership.SetDown(probeSource, y);
            ownership.SetDown(probeSource, lt);
            ownership.SetDown(probeSource, rs);

            int slot = FindProbeSlot(10000);
            ownership.ClearSource(probeSource);
            if (slot < 0)
                throw new AcceptanceBlockedException(
                    "ViGEm Xbox enumerated but its XInput report did not reflect the preflight probe. " +
                    DescribeXInputSlots());

            lab.SetPreferredXInputSlot(slot);
            Info("XInput probe binding", "bound to slot=" + slot.ToString() + " using direct ViGEm Y + LT41 + RS(0.37,-0.63) signature");

            ObservationSnapshot cleared;
            bool released = WaitForObservation(lab, delegate(ObservationSnapshot s)
            {
                if (!s.XInputConnected) return false;
                bool yUp = (s.Gamepad.wButtons & XInputReader.Y) == 0;
                bool ltZero = s.Gamepad.bLeftTrigger == 0;
                bool rsZero = Math.Abs((int)s.Gamepad.sThumbRX) <= 256 && Math.Abs((int)s.Gamepad.sThumbRY) <= 256;
                return yUp && ltZero && rsZero;
            }, 1200, out cleared);
            if (!released)
                throw new AcceptanceBlockedException("ViGEm/XInput probe was visible but did not return to neutral before acceptance. " + DescribeXInputSlots());
        }

        private static OutputOwnershipManager GetOwnershipManager(MainForm product)
        {
            FieldInfo field = typeof(MainForm).GetField("outputOwnership", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("InputStitch output ownership field was not found.");
            OutputOwnershipManager manager = field.GetValue(product) as OutputOwnershipManager;
            if (manager == null) throw new InvalidOperationException("InputStitch output ownership manager is unavailable.");
            return manager;
        }

        private static InputSpec GamepadSpec(GamepadControl control, int x, int y, int value)
        {
            InputSpec input = new InputSpec();
            input.Kind = InputKind.Gamepad;
            input.GamepadControl = control;
            input.GamepadX = x;
            input.GamepadY = y;
            input.GamepadValue = value;
            return input;
        }

        private static int FindProbeSlot(int timeoutMs)
        {
            XInputReader reader = new XInputReader();
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, timeoutMs));
            while (DateTime.UtcNow < deadline)
            {
                for (uint slot = 0; slot < 4; slot++)
                {
                    XInputReader.XINPUT_STATE state;
                    if (!reader.TryGetState(slot, out state)) continue;
                    bool yDown = (state.Gamepad.wButtons & XInputReader.Y) != 0;
                    double lt = XInputReader.NormalizeTrigger(state.Gamepad.bLeftTrigger);
                    double rx = XInputReader.NormalizeStick(state.Gamepad.sThumbRX);
                    double ry = XInputReader.NormalizeStick(state.Gamepad.sThumbRY);
                    if (yDown && Math.Abs(lt - 0.41) <= 0.03 && Math.Abs(rx - 0.37) <= 0.04 && Math.Abs(ry + 0.63) <= 0.04)
                        return (int)slot;
                }
                Thread.Sleep(15);
            }
            return -1;
        }

        private static string DescribeXInputSlots()
        {
            XInputReader reader = new XInputReader();
            StringBuilder sb = new StringBuilder("XInput slots:");
            for (uint slot = 0; slot < 4; slot++)
            {
                XInputReader.XINPUT_STATE state;
                if (!reader.TryGetState(slot, out state))
                {
                    sb.Append(" [").Append(slot).Append(": disconnected]");
                    continue;
                }
                sb.Append(" [").Append(slot)
                    .Append(": packet=").Append(state.dwPacketNumber)
                    .Append(" buttons=0x").Append(state.Gamepad.wButtons.ToString("X4"))
                    .Append(" LT=").Append(state.Gamepad.bLeftTrigger)
                    .Append(" RT=").Append(state.Gamepad.bRightTrigger)
                    .Append(" RS=(").Append(state.Gamepad.sThumbRX).Append(',').Append(state.Gamepad.sThumbRY).Append(")]");
            }
            return sb.ToString();
        }

        private static void ScenarioWasdReleaseOrder(InputLabForm lab, MainForm product)
        {
            Section("WASD release order");
            Press(product, KeyEvent(Keys.W));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "W => LS up", 0.0, 1.0, 0.03);

            Press(product, KeyEvent(Keys.D));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "W+D => normalized diagonal", Math.Sqrt(0.5), Math.Sqrt(0.5), 0.04);

            Release(product, KeyEvent(Keys.W));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "release W keeps D", 1.0, 0.0, 0.03);

            Release(product, KeyEvent(Keys.D));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "release last stick source => neutral", 0.0, 0.0, 0.02);
        }

        private static void ScenarioOpposingStickAxes(InputLabForm lab, MainForm product)
        {
            Section("Opposing stick axes");
            Press(product, KeyEvent(Keys.W));
            Press(product, KeyEvent(Keys.S));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "W+S => neutral Y", 0.0, 0.0, 0.03);
            Release(product, KeyEvent(Keys.W));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "release W leaves S", 0.0, -1.0, 0.03);
            Release(product, KeyEvent(Keys.S));

            Press(product, KeyEvent(Keys.A));
            Press(product, KeyEvent(Keys.D));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "A+D => neutral X", 0.0, 0.0, 0.03);
            Release(product, KeyEvent(Keys.A));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "release A leaves D", 1.0, 0.0, 0.03);
            Release(product, KeyEvent(Keys.D));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "opposing-axis scenario cleanup", 0.0, 0.0, 0.02);
        }

        private static void ScenarioTriggerMaximum(InputLabForm lab, MainForm product)
        {
            Section("Analog trigger maximum merge");
            Press(product, KeyEvent(Keys.X));
            SleepAndPump(lab, SettleMs);
            CheckTrigger(lab, "RT 50 source", false, 0.50, 0.03);
            Press(product, KeyEvent(Keys.Z));
            SleepAndPump(lab, SettleMs);
            CheckTrigger(lab, "RT 100 + RT 50 => 100", false, 1.00, 0.02);
            Release(product, KeyEvent(Keys.Z));
            SleepAndPump(lab, SettleMs);
            CheckTrigger(lab, "release RT100 => RT50 remains", false, 0.50, 0.03);
            Release(product, KeyEvent(Keys.X));
            SleepAndPump(lab, SettleMs);
            CheckTrigger(lab, "release last RT source => zero", false, 0.0, 0.01);
        }

        private static void ScenarioSharedDigitalOwnership(InputLabForm lab, MainForm product)
        {
            Section("Shared digital ownership");
            Press(product, KeyEvent(Keys.Q));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "Q => RB", XInputReader.RIGHT_SHOULDER, true);
            Press(product, KeyEvent(Keys.E));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "Q+E share RB", XInputReader.RIGHT_SHOULDER, true);
            Release(product, KeyEvent(Keys.Q));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "release first RB owner keeps RB", XInputReader.RIGHT_SHOULDER, true);
            Release(product, KeyEvent(Keys.E));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "release final RB owner releases RB", XInputReader.RIGHT_SHOULDER, false);
        }

        private static void ScenarioDPadAxisMerge(InputLabForm lab, MainForm product)
        {
            Section("D-pad axis merge");
            Press(product, KeyEvent(Keys.U));
            Press(product, KeyEvent(Keys.J));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "Up+Down cancels Up", XInputReader.DPAD_UP, false);
            CheckButton(lab, "Up+Down cancels Down", XInputReader.DPAD_DOWN, false);
            Press(product, KeyEvent(Keys.I));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "orthogonal Right survives cancelled vertical", XInputReader.DPAD_RIGHT, true);
            Release(product, KeyEvent(Keys.J));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "release Down restores Up", XInputReader.DPAD_UP, true);
            CheckButton(lab, "Up+Right diagonal keeps Right", XInputReader.DPAD_RIGHT, true);
            Release(product, KeyEvent(Keys.U));
            Release(product, KeyEvent(Keys.I));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "D-pad cleanup Up", XInputReader.DPAD_UP, false);
            CheckButton(lab, "D-pad cleanup Right", XInputReader.DPAD_RIGHT, false);
        }

        private static void ScenarioFourHeldPlusOrdinary(InputLabForm lab, MainForm product)
        {
            Section("Four Held Mappings + one ordinary timed macro");
            Press(product, KeyEvent(Keys.W));
            Press(product, KeyEvent(Keys.D));
            Press(product, KeyEvent(Keys.LShiftKey, false, true, false, false));
            Press(product, MouseEvent(InputKind.MouseX1));
            SleepAndPump(lab, SettleMs + 40);
            CheckStick(lab, "four-held: W+D stick", Math.Sqrt(0.5), Math.Sqrt(0.5), 0.04);
            CheckTrigger(lab, "four-held: Shift => LT70", true, 0.70, 0.03);
            CheckButton(lab, "four-held: X1 => LB", XInputReader.LEFT_SHOULDER, true);

            Press(product, KeyEvent(Keys.F6));
            Release(product, KeyEvent(Keys.F6));
            CheckWaitButton(lab, "ordinary macro A becomes down while Held sources remain", XInputReader.A, true, 1200);
            CheckStick(lab, "ordinary macro does not neutralize Held stick", Math.Sqrt(0.5), Math.Sqrt(0.5), 0.04);
            CheckTrigger(lab, "ordinary macro does not neutralize LT", true, 0.70, 0.03);
            CheckButton(lab, "ordinary macro does not neutralize LB", XInputReader.LEFT_SHOULDER, true);
            CheckWaitButton(lab, "ordinary macro completes and releases A", XInputReader.A, false, 1500);
            CheckStick(lab, "Held stick survives ordinary completion", Math.Sqrt(0.5), Math.Sqrt(0.5), 0.04);

            Release(product, MouseEvent(InputKind.MouseX1));
            Release(product, KeyEvent(Keys.D));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "release X1 removes LB", XInputReader.LEFT_SHOULDER, false);
            CheckStick(lab, "release D leaves W", 0.0, 1.0, 0.03);
            Release(product, KeyEvent(Keys.LShiftKey, false, false, false, false));
            SleepAndPump(lab, SettleMs);
            CheckTrigger(lab, "release Shift clears LT", true, 0.0, 0.01);
            Release(product, KeyEvent(Keys.W));
            SleepAndPump(lab, SettleMs);
            CheckStick(lab, "four-held final neutral", 0.0, 0.0, 0.02);
        }

        private static void ScenarioEmergencyStop(InputLabForm lab, MainForm product)
        {
            Section("Emergency Stop global cleanup");
            Press(product, KeyEvent(Keys.W));
            Press(product, KeyEvent(Keys.D));
            Press(product, KeyEvent(Keys.Z));
            Press(product, KeyEvent(Keys.Q));
            SleepAndPump(lab, SettleMs);
            CheckButton(lab, "pre-panic RB active", XInputReader.RIGHT_SHOULDER, true);
            CheckTrigger(lab, "pre-panic RT active", false, 1.0, 0.02);

            Press(product, KeyEvent(Keys.F12, true, true, false, false));
            SleepAndPump(lab, SettleMs + 80);
            CheckStick(lab, "panic clears stick", 0.0, 0.0, 0.02);
            CheckTrigger(lab, "panic clears RT", false, 0.0, 0.01);
            CheckButton(lab, "panic clears RB", XInputReader.RIGHT_SHOULDER, false);

            Release(product, KeyEvent(Keys.W));
            Release(product, KeyEvent(Keys.D));
            Release(product, KeyEvent(Keys.Z));
            Release(product, KeyEvent(Keys.Q));
            Release(product, KeyEvent(Keys.F12, true, true, false, false));
        }

        private static void ScenarioKeyboardRawComparison(InputLabForm lab, MainForm product)
        {
            Section("Keyboard SendInput vs Raw Input comparison");
            ObservationSnapshot before = lab.CaptureObservation();
            Press(product, KeyEvent(Keys.F7));
            Release(product, KeyEvent(Keys.F7));
            Check("Raw Input keyboard lane registered", before.RawInputRegistered, "registered=" + before.RawInputRegistered.ToString());
            ObservationSnapshot after;
            bool cycleObserved = WaitForObservation(lab, delegate(ObservationSnapshot s)
            {
                int rawDelta = s.RawKeyboardEvents - before.RawKeyboardEvents;
                int hookDelta = s.InjectedKeyboardEvents - before.InjectedKeyboardEvents;
                bool rawCycle = rawDelta >= 2 && s.LastRawKeyboardVirtualKey == (int)Keys.K && !s.LastRawKeyboardDown;
                bool hookCycle = hookDelta >= 2 && s.LastHookKeyboardVirtualKey == (int)Keys.K && !s.LastHookKeyboardDown;
                return rawCycle || hookCycle;
            }, 1800, out after);
            Check("scan-code SendInput K DOWN/UP observed", cycleObserved,
                "rawDelta=" + (after.RawKeyboardEvents - before.RawKeyboardEvents).ToString() +
                ", rawLast=" + after.LastRawKeyboardVirtualKey.ToString() + "/" + (after.LastRawKeyboardDown ? "DOWN" : "UP") +
                ", hookInjectedDelta=" + (after.InjectedKeyboardEvents - before.InjectedKeyboardEvents).ToString() +
                ", hookLast=" + after.LastHookKeyboardVirtualKey.ToString() + "/" + (after.LastHookKeyboardDown ? "DOWN" : "UP"));
            Info("Keyboard lane comparison", "Raw Input events delta=" + (after.RawKeyboardEvents - before.RawKeyboardEvents).ToString() +
                "; low-level injected events delta=" + (after.InjectedKeyboardEvents - before.InjectedKeyboardEvents).ToString());
            if (!cycleObserved) Info("F7 runtime observation", GetRuntimeObservation(product).Replace("\r", " ").Replace("\n", " | "));
        }

        private static void ScenarioMouseRawComparison(InputLabForm lab, MainForm product)
        {
            Section("Mouse SendInput vs Raw Input comparison");
            ObservationSnapshot before = lab.CaptureObservation();
            Press(product, KeyEvent(Keys.F8));
            Release(product, KeyEvent(Keys.F8));
            ObservationSnapshot during;
            bool reachedDown = WaitForObservation(lab, delegate(ObservationSnapshot s) { return s.MouseButtonsDown.Contains("X2"); }, 1200, out during);
            Check("SendInput X2 reaches low-level hook", reachedDown,
                "X2 down=" + during.MouseButtonsDown.Contains("X2").ToString());
            Check("mouse event is marked injected", during.InjectedMouseEvents > before.InjectedMouseEvents,
                "injected mouse events delta=" + (during.InjectedMouseEvents - before.InjectedMouseEvents).ToString());
            Info("Raw Input mouse delta during injected macro", (during.RawMouseEvents - before.RawMouseEvents).ToString() +
                " (comparison lane observation; interpretation is API/driver dependent)");
            CheckWaitMouse(lab, "timed mouse macro releases X2", "X2", false, 1500);
        }

        private static void Neutral(MainForm product, InputLabForm lab, string label)
        {
            try
            {
                MethodInfo emergency = typeof(MainForm).GetMethod("EmergencyStop", BindingFlags.Instance | BindingFlags.NonPublic);
                product.Invoke((MethodInvoker)delegate { emergency.Invoke(product, new object[] { label }); Application.DoEvents(); });
            }
            catch { try { GamepadOutput.NeutralizeAll(); } catch { } }
            CheckWaitStick(lab, label + " stick", 0.0, 0.0, 0.02, 1200);
            CheckWaitTrigger(lab, label + " LT", true, 0.0, 0.01, 1200);
            CheckWaitTrigger(lab, label + " RT", false, 0.0, 0.01, 1200);
        }

        private static void Press(MainForm product, InputEventInfo input)
        {
            product.Invoke((MethodInvoker)delegate
            {
                HandleDown.Invoke(product, new object[] { input });
                Application.DoEvents();
            });
            product.Invoke((MethodInvoker)delegate { Application.DoEvents(); });
        }

        private static void Release(MainForm product, InputEventInfo input)
        {
            product.Invoke((MethodInvoker)delegate
            {
                HandleUp.Invoke(product, new object[] { input });
                Application.DoEvents();
            });
            product.Invoke((MethodInvoker)delegate { Application.DoEvents(); });
        }

        private static void SleepAndPump(InputLabForm lab, int milliseconds)
        {
            Thread.Sleep(milliseconds);
            lab.CaptureObservation();
        }

        private static InputEventInfo KeyEvent(Keys key)
        {
            return KeyEvent(key, false, false, false, false);
        }

        private static InputEventInfo KeyEvent(Keys key, bool ctrl, bool shift, bool alt, bool win)
        {
            InputEventInfo e = new InputEventInfo();
            e.Input = new InputSpec();
            e.Input.Kind = InputKind.Keyboard;
            e.Input.VirtualKey = (int)key;
            e.Ctrl = ctrl;
            e.Shift = shift;
            e.Alt = alt;
            e.Win = win;
            return e;
        }

        private static InputEventInfo MouseEvent(InputKind kind)
        {
            InputEventInfo e = new InputEventInfo();
            e.Input = new InputSpec();
            e.Input.Kind = kind;
            e.Input.VirtualKey = 0;
            return e;
        }

        private static string GetRuntimeObservation(MainForm product)
        {
            try
            {
                MethodInfo method = typeof(MainForm).GetMethod("BuildRuntimeObservationText", BindingFlags.Instance | BindingFlags.NonPublic);
                object value = product.Invoke(new System.Func<object>(delegate { return method == null ? "unavailable" : method.Invoke(product, null); }));
                return value == null ? "unavailable" : value.ToString();
            }
            catch (Exception ex) { return "observation failed: " + ex.GetBaseException().Message; }
        }

        private static bool WaitForObservation(InputLabForm lab, System.Func<ObservationSnapshot, bool> predicate, int timeoutMs, out ObservationSnapshot snapshot)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, timeoutMs));
            snapshot = lab.CaptureObservation();
            while (!predicate(snapshot) && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(15);
                snapshot = lab.CaptureObservation();
            }
            return predicate(snapshot);
        }

        private static void CheckWaitButton(InputLabForm lab, string label, ushort flag, bool expected, int timeoutMs)
        {
            ObservationSnapshot s;
            bool reached = WaitForObservation(lab, delegate(ObservationSnapshot x)
            {
                return x.XInputConnected && (((x.Gamepad.wButtons & flag) != 0) == expected);
            }, timeoutMs, out s);
            bool actual = s.XInputConnected && (s.Gamepad.wButtons & flag) != 0;
            Check(label, reached, "expected=" + expected.ToString() + " observed=" + actual.ToString() + " timeout=" + timeoutMs.ToString() + "ms");
        }

        private static void CheckWaitMouse(InputLabForm lab, string label, string button, bool expected, int timeoutMs)
        {
            ObservationSnapshot s;
            bool reached = WaitForObservation(lab, delegate(ObservationSnapshot x) { return x.MouseButtonsDown.Contains(button) == expected; }, timeoutMs, out s);
            bool actual = s.MouseButtonsDown.Contains(button);
            Check(label, reached, "expected=" + expected.ToString() + " observed=" + actual.ToString() + " timeout=" + timeoutMs.ToString() + "ms");
        }

        private static void CheckWaitStick(InputLabForm lab, string label, double expectedX, double expectedY, double tolerance, int timeoutMs)
        {
            ObservationSnapshot s;
            bool reached = WaitForObservation(lab, delegate(ObservationSnapshot x)
            {
                if (!x.XInputConnected) return false;
                double observedX = XInputReader.NormalizeStick(x.Gamepad.sThumbLX);
                double observedY = XInputReader.NormalizeStick(x.Gamepad.sThumbLY);
                return Math.Abs(observedX - expectedX) <= tolerance && Math.Abs(observedY - expectedY) <= tolerance;
            }, timeoutMs, out s);
            double actualX = s.XInputConnected ? XInputReader.NormalizeStick(s.Gamepad.sThumbLX) : 0.0;
            double actualY = s.XInputConnected ? XInputReader.NormalizeStick(s.Gamepad.sThumbLY) : 0.0;
            Check(label, reached, "expected=(" + expectedX.ToString("0.000") + "," + expectedY.ToString("0.000") + ") observed=(" + actualX.ToString("0.000") + "," + actualY.ToString("0.000") + ") timeout=" + timeoutMs.ToString() + "ms");
        }

        private static void CheckWaitTrigger(InputLabForm lab, string label, bool left, double expected, double tolerance, int timeoutMs)
        {
            ObservationSnapshot s;
            bool reached = WaitForObservation(lab, delegate(ObservationSnapshot x)
            {
                if (!x.XInputConnected) return false;
                double value = XInputReader.NormalizeTrigger(left ? x.Gamepad.bLeftTrigger : x.Gamepad.bRightTrigger);
                return Math.Abs(value - expected) <= tolerance;
            }, timeoutMs, out s);
            double actual = s.XInputConnected ? XInputReader.NormalizeTrigger(left ? s.Gamepad.bLeftTrigger : s.Gamepad.bRightTrigger) : 0.0;
            Check(label, reached, "expected=" + expected.ToString("0.000") + " observed=" + actual.ToString("0.000") + " timeout=" + timeoutMs.ToString() + "ms");
        }

        private static void CheckStick(InputLabForm lab, string label, double expectedX, double expectedY, double tolerance)
        {
            ObservationSnapshot s = lab.CaptureObservation();
            if (!s.XInputConnected)
            {
                Check(label, false, "no XInput controller detected");
                return;
            }
            double x = XInputReader.NormalizeStick(s.Gamepad.sThumbLX);
            double y = XInputReader.NormalizeStick(s.Gamepad.sThumbLY);
            bool ok = Math.Abs(x - expectedX) <= tolerance && Math.Abs(y - expectedY) <= tolerance;
            Check(label, ok, "expected=(" + expectedX.ToString("0.000") + "," + expectedY.ToString("0.000") + ") observed=(" + x.ToString("0.000") + "," + y.ToString("0.000") + ")");
        }

        private static void CheckTrigger(InputLabForm lab, string label, bool left, double expected, double tolerance)
        {
            ObservationSnapshot s = lab.CaptureObservation();
            if (!s.XInputConnected)
            {
                Check(label, false, "no XInput controller detected");
                return;
            }
            double value = XInputReader.NormalizeTrigger(left ? s.Gamepad.bLeftTrigger : s.Gamepad.bRightTrigger);
            Check(label, Math.Abs(value - expected) <= tolerance,
                "expected=" + expected.ToString("0.000") + " observed=" + value.ToString("0.000"));
        }

        private static void CheckButton(InputLabForm lab, string label, ushort flag, bool expected)
        {
            ObservationSnapshot s = lab.CaptureObservation();
            if (!s.XInputConnected)
            {
                Check(label, false, "no XInput controller detected");
                return;
            }
            bool actual = (s.Gamepad.wButtons & flag) != 0;
            Check(label, actual == expected, "expected=" + expected.ToString() + " observed=" + actual.ToString());
        }

        private static void Check(string label, bool ok, string details)
        {
            checks++;
            if (!ok) failures++;
            string line = (ok ? "PASS" : "FAIL") + " | " + label + " | " + details;
            Report.AppendLine(line);
            Console.WriteLine(line);
        }

        private static void Fail(string label, string details)
        {
            checks++;
            failures++;
            string line = "FAIL | " + label + " | " + details;
            Report.AppendLine(line);
            Console.WriteLine(line);
        }

        private static void Info(string label, string details)
        {
            string line = "INFO | " + label + " | " + details;
            Report.AppendLine(line);
            Console.WriteLine(line);
        }

        private static void Section(string name)
        {
            Report.AppendLine();
            Report.AppendLine("## " + name);
            Console.WriteLine("## " + name);
        }

        private static void WriteReport()
        {
            try { File.WriteAllText(reportPath, Report.ToString(), Encoding.UTF8); }
            catch (Exception ex) { Console.Error.WriteLine("Could not write acceptance report: " + ex.Message); }
        }

        private static MacroConfig BuildAcceptanceConfig()
        {
            MacroConfig config = new MacroConfig();
            config.Language = Localizer.English;
            config.GamepadDeviceType = VirtualGamepadTypes.Xbox360;
            config.UseScanCodeInput = true;
            config.PauseMacroInRiskyUi = false;
            config.MinimizeToTray = false;
            config.HasSeenWelcome = true;
            config.LastShownReleaseSummaryVersion = AppInfo.Version;
            config.Macros.Clear();

            config.Macros.Add(HeldKey("W -> LS Up", Keys.W, GamepadControl.LeftStick, 0, 100, 100));
            config.Macros.Add(HeldKey("S -> LS Down", Keys.S, GamepadControl.LeftStick, 0, -100, 100));
            config.Macros.Add(HeldKey("A -> LS Left", Keys.A, GamepadControl.LeftStick, -100, 0, 100));
            config.Macros.Add(HeldKey("D -> LS Right", Keys.D, GamepadControl.LeftStick, 100, 0, 100));
            config.Macros.Add(HeldKey("Z -> RT100", Keys.Z, GamepadControl.RightTrigger, 0, 0, 100));
            config.Macros.Add(HeldKey("X -> RT50", Keys.X, GamepadControl.RightTrigger, 0, 0, 50));
            config.Macros.Add(HeldKey("Q -> RB", Keys.Q, GamepadControl.RightShoulder, 0, 0, 100));
            config.Macros.Add(HeldKey("E -> RB", Keys.E, GamepadControl.RightShoulder, 0, 0, 100));
            config.Macros.Add(HeldKey("U -> DPadUp", Keys.U, GamepadControl.DPadUp, 0, 0, 100));
            config.Macros.Add(HeldKey("J -> DPadDown", Keys.J, GamepadControl.DPadDown, 0, 0, 100));
            config.Macros.Add(HeldKey("I -> DPadRight", Keys.I, GamepadControl.DPadRight, 0, 0, 100));
            config.Macros.Add(HeldKey("Shift -> LT70", Keys.LShiftKey, GamepadControl.LeftTrigger, 0, 0, 70));
            config.Macros.Add(HeldMouse("X1 -> LB", InputKind.MouseX1, GamepadControl.LeftShoulder));
            config.Macros.Add(TimedGamepadPress("F6 -> A timed", Keys.F6, GamepadControl.South, 300));
            config.Macros.Add(TimedKeyboardPress("F7 -> K timed", Keys.F7, Keys.K, 240));
            config.Macros.Add(TimedMousePress("F8 -> X2 timed", Keys.F8, InputKind.MouseX2, 240));
            return config;
        }

        private static MacroDefinition HeldKey(string name, Keys triggerKey, GamepadControl control, int x, int y, int value)
        {
            MacroDefinition m = BaseHeld(name);
            m.Trigger.Kind = InputKind.Keyboard;
            m.Trigger.VirtualKey = (int)triggerKey;
            m.Steps.Add(GamepadDown(control, x, y, value));
            return m;
        }

        private static MacroDefinition HeldMouse(string name, InputKind triggerKind, GamepadControl control)
        {
            MacroDefinition m = BaseHeld(name);
            m.Trigger.Kind = triggerKind;
            m.Trigger.VirtualKey = 0;
            m.Steps.Add(GamepadDown(control, 0, 0, 100));
            return m;
        }

        private static MacroDefinition BaseHeld(string name)
        {
            MacroDefinition m = new MacroDefinition();
            m.Name = name;
            m.Enabled = true;
            m.RunMode = TriggerRunMode.Hold;
            m.Infinite = true;
            m.RepeatCount = 1;
            m.SuppressTrigger = false;
            m.Steps.Clear();
            return m;
        }

        private static MacroStep GamepadDown(GamepadControl control, int x, int y, int value)
        {
            MacroStep s = new MacroStep();
            s.Action = MacroAction.Down;
            s.Kind = InputKind.Gamepad;
            s.GamepadControl = control;
            s.GamepadX = x;
            s.GamepadY = y;
            s.GamepadValue = value;
            s.HoldMs = 0;
            s.DelayMs = 0;
            s.RandomDelay = false;
            return s;
        }

        private static MacroDefinition TimedGamepadPress(string name, Keys trigger, GamepadControl control, int holdMs)
        {
            MacroDefinition m = BaseTimed(name, trigger);
            MacroStep s = new MacroStep();
            s.Action = MacroAction.Press;
            s.Kind = InputKind.Gamepad;
            s.GamepadControl = control;
            s.HoldMs = holdMs;
            s.DelayMs = 0;
            m.Steps.Add(s);
            return m;
        }

        private static MacroDefinition TimedKeyboardPress(string name, Keys trigger, Keys output, int holdMs)
        {
            MacroDefinition m = BaseTimed(name, trigger);
            MacroStep s = new MacroStep();
            s.Action = MacroAction.Press;
            s.Kind = InputKind.Keyboard;
            s.VirtualKey = (int)output;
            s.HoldMs = holdMs;
            s.DelayMs = 0;
            m.Steps.Add(s);
            return m;
        }

        private static MacroDefinition TimedMousePress(string name, Keys trigger, InputKind output, int holdMs)
        {
            MacroDefinition m = BaseTimed(name, trigger);
            MacroStep s = new MacroStep();
            s.Action = MacroAction.Press;
            s.Kind = output;
            s.HoldMs = holdMs;
            s.DelayMs = 0;
            m.Steps.Add(s);
            return m;
        }

        private static MacroDefinition BaseTimed(string name, Keys trigger)
        {
            MacroDefinition m = new MacroDefinition();
            m.Name = name;
            m.Enabled = true;
            m.RunMode = TriggerRunMode.Toggle;
            m.Infinite = false;
            m.RepeatCount = 1;
            m.SuppressTrigger = false;
            m.Trigger.Kind = InputKind.Keyboard;
            m.Trigger.VirtualKey = (int)trigger;
            m.Steps.Clear();
            return m;
        }
    }
}
