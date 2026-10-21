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
        public void SendDown(InputSpec input) { Interlocked.Increment(ref Downs); }
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
            engine.Clear();
            request.Invoke(form, null);
            if ((int)queued.GetValue(form) != 0) throw new Exception("clearing activators must restore idle behavior");
        }
        WorkerReuse(directory);
        Console.WriteLine("PASS input pipeline integration: 10 checks; idle UI work, worker reuse, source-local cancellation and configuration-change completion; isolated observer, no hooks or real output.");
        return 0;
    }
}
