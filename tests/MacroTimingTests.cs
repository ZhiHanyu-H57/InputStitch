using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class MacroTimingTests
{
    private sealed class TimingBackend : IOutputBackend
    {
        private readonly object sync = new object();
        public long DownTick;
        public long UpTick;
        public int DownCount;
        public int UpCount;

        public void SendDown(InputSpec input)
        {
            if (input == null || input.Kind != InputKind.MouseX2) return;
            lock (sync)
            {
                DownCount++;
                if (DownTick == 0) DownTick = Stopwatch.GetTimestamp();
            }
        }

        public void SendUp(InputSpec input)
        {
            if (input == null || input.Kind != InputKind.MouseX2) return;
            lock (sync)
            {
                UpCount++;
                if (UpTick == 0) UpTick = Stopwatch.GetTimestamp();
            }
        }

        public void NeutralizeGamepad() { }

        public bool HasReleased
        {
            get { lock (sync) return UpTick != 0; }
        }

        public double HeldMilliseconds
        {
            get
            {
                lock (sync)
                {
                    if (DownTick == 0 || UpTick == 0) return -1.0;
                    return (UpTick - DownTick) * 1000.0 / Stopwatch.Frequency;
                }
            }
        }
    }

    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-MacroTimingTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const int requestedHoldMs = 900;
            MacroDefinition macro = new MacroDefinition
            {
                Name = "timed X2 press",
                Enabled = true,
                RunMode = TriggerRunMode.Toggle,
                Infinite = false,
                RepeatCount = 1,
                Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 },
                Steps = new List<MacroStep>
                {
                    new MacroStep
                    {
                        Action = MacroAction.Press,
                        Kind = InputKind.MouseX2,
                        HoldMs = requestedHoldMs,
                        DelayMs = 0
                    }
                }
            };
            MacroConfig config = new MacroConfig();
            config.PauseMacroInRiskyUi = false;
            config.Macros = new List<MacroDefinition> { macro };
            TimingBackend backend = new TimingBackend();

            using (MainForm form = new MainForm(config, root, backend))
            {
                MethodInfo start = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                Check(start != null, "timed macro start entrypoint exists");
                start.Invoke(form, new object[] { macro, 0, null, false, false });

                Check(WaitUntil(delegate { return backend.DownCount > 0; }, 500), "timed mouse Press sends Down promptly");
                Check(WaitUntil(delegate { return backend.HasReleased; }, 1600),
                    "900 ms timed mouse Press releases without accumulated scheduler-slice drift");

                double heldMs = backend.HeldMilliseconds;
                Check(heldMs >= 750.0, "timed Press is not shortened materially: " + heldMs.ToString("0.0") + " ms");
                Check(heldMs <= 1450.0, "timed Press does not accumulate 10 ms wait overshoot: " + heldMs.ToString("0.0") + " ms");
                Check(backend.DownCount == 1 && backend.UpCount == 1, "timed Press emits one Down and one Up");
                Check(WaitUntil(delegate
                {
                    MethodInfo running = typeof(MainForm).GetMethod("IsMacroActuallyRunning", BindingFlags.Instance | BindingFlags.NonPublic);
                    return running != null && !(bool)running.Invoke(form, new object[] { macro });
                }, 500), "timed macro worker completes after release");
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }

        Console.WriteLine("PASS macro timing: " + checks + " checks; fake output only, no real input or device.");
        return 0;
    }
}
