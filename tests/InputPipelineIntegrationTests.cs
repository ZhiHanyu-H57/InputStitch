using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Windows.Forms;
using InputStitch;

internal static class InputPipelineIntegrationTests
{
    private sealed class Observer : IOutputBackend
    {
        internal int Downs, Ups;
        internal long FirstDownAt;
        public void SendDown(InputSpec input) { Interlocked.CompareExchange(ref FirstDownAt, Stopwatch.GetTimestamp(), 0); Interlocked.Increment(ref Downs); }
        public void SendUp(InputSpec input) { Interlocked.Increment(ref Ups); }
        public void NeutralizeGamepad() { }
    }
    private static object Call(MainForm form, string method, params object[] args)
    { return typeof(MainForm).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(x => x.Name == method && x.GetParameters().Length == args.Length).Invoke(form, args); }
    private static void Wait(Func<bool> condition)
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < 3000) { Application.DoEvents(); Thread.Sleep(2); }
        if (!condition()) throw new Exception("worker integration timed out");
    }
    private static void WorkerReuse(string directory)
    {
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition first = new MacroDefinition { Infinite = true }, second = new MacroDefinition { Infinite = true };
        first.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8, Action = MacroAction.Down, DelayMs = 1000 });
        second.Steps.Add(first.Steps[0].Clone());
        MacroDefinition finite = new MacroDefinition { Infinite = false, RepeatCount = 1 };
        finite.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9, Action = MacroAction.Press, HoldMs = 1, DelayMs = 1 });
        config.Macros.Add(first); config.Macros.Add(second); config.Macros.Add(finite);
        Observer observer = new Observer();
        using (MainForm form = new MainForm(config, directory, observer))
        {
            OutputOwnershipManager ownership = (OutputOwnershipManager)typeof(MainForm).GetField("outputOwnership", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            Call(form,"StartMacro",first,0,null,false,false); Call(form,"StartMacro",second,0,null,false,false);
            Wait(delegate { return ownership.Snapshot().Sources.Count == 2; });
            foreach (string language in new[] { "zh-CN", "en-US" })
            {
                Localizer.SetLanguage(language);
                string observation = (string)Call(form, "BuildRuntimeObservationText");
                string diagnostics = (string)Call(form, "BuildDiagnosticsText");
                if (!observation.Contains("RunId=1") || !observation.Contains("RunId=2")) throw new Exception("observation must show both coordinated runs");
                if (!observation.Contains("held=F8") || !diagnostics.Contains("Held=F8")) throw new Exception("held names must still render on demand in both languages");
                if (!diagnostics.Contains("MacroRunCount: 2")) throw new Exception("diagnostics must show a consistent coordinated count");
            }
            Localizer.SetLanguage("zh-CN");
            Call(form,"StopMacro",first,"reuse-test");
            Wait(delegate { return ownership.Snapshot().Sources.Count == 1; });
            if (ownership.Snapshot().Merged.Count != 1) throw new Exception("stopping one reused worker released the other run's output");
            if (!(bool)Call(form,"StopRuntimeForConfigChange") || (bool)Call(form,"HasWorkerBackedRuns")) throw new Exception("configuration changes must wait for run completion, not reusable thread exit");
            for (int i=0;i<50;i++)
            {
                Call(form,"StartMacro",finite,0,null,false,false);
                Wait(delegate { return !(bool)Call(form,"HasWorkerBackedRuns"); });
            }
            if (ownership.Snapshot().Sources.Count != 0 || observer.Downs != observer.Ups) throw new Exception("reused runs must release every output");
            MacroWorkerPool workers = (MacroWorkerPool)typeof(MainForm).GetField("macroWorkers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            if (workers.CreatedThreadCount > 6) throw new Exception("actual macro restarts must reuse bounded workers");
        }
    }

    // Input seam -> production scheduler/UI dispatch -> real worker -> observer.
    // Measures current UI-affine activators; this does not claim physical hook or
    // game latency, nor does it remove UI safety checks to make the sample pass.
    private static long AdvancedLatency(string directory, int uiBlockedMs)
    {
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition macro = new MacroDefinition { Name = "long-press-latency", Enabled = true, Infinite = false, RepeatCount = 1,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 },
            Activator = new ActivatorConfig { Mode = ActivatorModes.LongPress, LongPressMs = 150 } };
        macro.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F23, Action = MacroAction.Press, HoldMs = 1 });
        config.Macros.Add(macro); Observer observer = new Observer();
        using (MainForm form = new MainForm(config, directory, observer))
        {
            IntPtr handle = form.Handle;
            InputEventInfo input = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 } };
            using (InputPipelineScheduler scheduler = new InputPipelineScheduler(10, delegate { Call(form, "RequestActivatorTick"); }, null))
            {
                long start = Stopwatch.GetTimestamp();
                Call(form, "HandleTerminalInput", input); scheduler.Start();
                if (uiBlockedMs != 0) Thread.Sleep(uiBlockedMs);
                Wait(delegate { return Interlocked.Read(ref observer.FirstDownAt) != 0; });
                long latency = (Interlocked.Read(ref observer.FirstDownAt) - start) * 1000 / Stopwatch.Frequency;
                if (latency < 150 || latency < uiBlockedMs) throw new Exception("long-press must respect the threshold and current UI dispatch boundary");
                Call(form, "HandleTerminalInputReleased", input);
                Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); });
                if (observer.Downs != observer.Ups) throw new Exception("latency sample left held output");
                return latency;
            }
        }
    }
    [STAThread]
    public static int Main()
    {
        AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Dump(e.ExceptionObject as Exception); };
        try { return Run(); } catch(Exception ex) { Dump(ex); return 1; }
    }
    private static void Dump(Exception ex)
    {
        while (ex != null) { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worker-failure.txt"), ex.GetType().FullName + " | " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine); ex = ex.InnerException; }
    }
    private static int Run()
    {
        Application.EnableVisualStyles();
        EmbeddedDependencyLoader.Register();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        string directory = Path.Combine(Path.GetTempPath(), "InputStitch-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using (MainForm form = new MainForm(new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None }, directory))
        {
            MethodInfo request = typeof(MainForm).GetMethod("RequestActivatorTick", flags);
            FieldInfo queued = typeof(MainForm).GetField("activatorTickQueued", flags);
            ActivatorRuntimeEngine engine = (ActivatorRuntimeEngine)typeof(MainForm).GetField("activatorEngine", flags).GetValue(form);
            for (int i = 0; i < 1000; i++) request.Invoke(form, null);
            if ((int)queued.GetValue(form) != 0) throw new Exception("idle advanced activators must not enqueue UI work");
            MacroDefinition macro = new MacroDefinition { Activator = new ActivatorConfig { Mode = ActivatorModes.LongPress, LongPressMs = 2000 } };
            InputEventInfo input = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 } };
            engine.OnDown(macro, input, new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 }, IdleGamepadService.MonotonicMilliseconds(), true);
            request.Invoke(form, null);
            if ((int)queued.GetValue(form) != 1) throw new Exception("a pending advanced activator must still enqueue its tick");
            request.Invoke(form, null);
            Application.DoEvents();
            if ((int)queued.GetValue(form) != 0) throw new Exception("advanced ticks must still coalesce and dispatch");
            engine.OnUp(input, IdleGamepadService.MonotonicMilliseconds(), null);
            if (engine.TrackedCount != 1) throw new Exception("released advanced states must retain history");
            for (int i = 0; i < 10000; i++) request.Invoke(form, null);
            if ((int)queued.GetValue(form) != 0) throw new Exception("released advanced state must not enqueue periodic UI work");
            engine.Clear();
            request.Invoke(form, null);
            if ((int)queued.GetValue(form) != 0) throw new Exception("clearing activators must restore idle behavior");
        }
        WorkerReuse(directory);
        long responsive = AdvancedLatency(directory, 0), blocked = AdvancedLatency(directory, 400);
        string report = "{\"version\":\"" + AppInfo.Version + "\",\"thresholdMs\":150,\"uiBlockedMs\":400,\"responsiveMs\":" + responsive +
            ",\"blockedMs\":" + blocked + ",\"backend\":\"isolated observer\",\"note\":\"Input seam, production scheduler and worker; UI affinity remains; no physical hooks or real output.\"}";
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activator-ui-latency.json"), report);
        Console.WriteLine(report);
        Console.WriteLine("PASS input pipeline integration: 24 checks; idle UI work, worker reuse, source-local cancellation, bilingual progress diagnostics, configuration completion and UI-blocked long-press samples; isolated observer, no hooks or real output.");
        return 0;
    }
}
