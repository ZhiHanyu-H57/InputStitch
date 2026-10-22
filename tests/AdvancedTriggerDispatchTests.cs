using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class AdvancedTriggerDispatchTests
{
    private sealed class Observer : IOutputBackend
    {
        internal int Downs, Ups;
        internal long FirstDown;
        public void SendDown(InputSpec input) { Interlocked.CompareExchange(ref FirstDown, Stopwatch.GetTimestamp(), 0); Interlocked.Increment(ref Downs); }
        public void SendUp(InputSpec input) { Interlocked.Increment(ref Ups); }
        public void NeutralizeGamepad() { }
    }
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static object Call(MainForm form, string name, params object[] args)
    { return typeof(MainForm).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(x => x.Name == name && x.GetParameters().Length == args.Length).Invoke(form, args); }
    private static void Set(MainForm form, string name, object value)
    { typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, value); }
    private static void Until(Func<bool> condition, bool pump)
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < 2000) { if (pump) Application.DoEvents(); Thread.Sleep(2); }
        Check(condition(), "timed out waiting for coordinated output/stop");
    }
    private static InputEventInfo Key(Keys key)
    { return new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key } }; }

    private static void AdmissionTickets()
    {
        ActivatorRuntimeEngine engine = new ActivatorRuntimeEngine();
        MacroDefinition macro = new MacroDefinition { Enabled = true, Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 },
            Activator = new ActivatorConfig { Mode = ActivatorModes.LongPress, LongPressMs = 150 } };
        macro.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A });
        InputEventInfo input = Key(Keys.F24);
        engine.OnDown(macro, input, macro.Trigger, 0, true, true);
        Check(engine.Tick(200, null).Count == 0 && !engine.HasUiPendingTick, "prepared long-press must have one timing lane");
        Check(engine.TickPrepared(149, null, delegate { return true; }).Count == 0, "no early threshold");
        ActivatorDecision first = engine.TickPrepared(150, null, delegate { return true; }).Single();
        macro.Steps[0].VirtualKey = (int)Keys.B;
        Check(first.Snapshot.Steps[0].VirtualKey == (int)Keys.A, "execution definition must be isolated at the accepted edge");
        engine.OnUp(input, 151, null);
        engine.OnDown(macro, input, macro.Trigger, 152, true, true);
        Check(!engine.TryDispatchPrepared(first, delegate { throw new Exception("stale admission executed"); }), "release/repress must invalidate an older ticket on the same state");
        ActivatorDecision next = engine.TickPrepared(302, null, delegate { return true; }).Single();
        int starts = 0;
        Check(engine.TryDispatchPrepared(next, delegate { starts++; }), "current admission must execute");
        Check(!engine.TryDispatchPrepared(next, delegate { starts++; }) && starts == 1, "admission must execute once");
        engine.Reset(macro);
        engine.OnDown(macro, input, macro.Trigger, 400, true, true);
        ActivatorDecision reset = engine.TickPrepared(550, null, delegate { return true; }).Single();
        engine.Reset(macro);
        Check(!engine.TryDispatchPrepared(reset, delegate { starts++; }), "definition reset must invalidate pending admission");
        engine.OnDown(macro, input, macro.Trigger, 600, true, true);
        ActivatorDecision stopped = engine.TickPrepared(750, null, delegate { return true; }).Single();
        engine.Clear();
        Check(!engine.TryDispatchPrepared(stopped, delegate { starts++; }) && !engine.HasPendingTick, "all-stop must cancel pending admission and idle work");
    }

    private static long Scenario(string directory, string scenario)
    {
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition macro = new MacroDefinition { Enabled = true, Infinite = scenario == "worker-protection" || scenario == "worker-panic",
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 },
            Activator = new ActivatorConfig { Mode = ActivatorModes.LongPress, LongPressMs = 150 } };
        macro.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F23, Action = MacroAction.Press, HoldMs = 15, DelayMs = 15 });
        if (scenario == "layer-loss") macro.MappingLayerId = "test";
        config.Macros.Add(macro);
        Observer observer = new Observer();
        int physical = 1, ownWindow = scenario.StartsWith("fallback-", StringComparison.Ordinal) ? 1 : 0;
        using (MainForm form = new MainForm(config, directory, observer))
        {
            form.ActivatorPhysicalProbe = delegate { return Volatile.Read(ref physical) != 0; };
            form.ActivatorOwnWindowProbe = delegate { return Volatile.Read(ref ownWindow) != 0; };
            IntPtr handle = form.Handle;
            if (scenario == "layer-loss") Set(form, "activeMappingLayerId", "test");
            InputEventInfo input = Key(Keys.F24);
            using (InputPipelineScheduler scheduler = new InputPipelineScheduler(10, delegate { Call(form, "RequestActivatorTick"); }, null))
            {
                long start = Stopwatch.GetTimestamp();
                Call(form, "HandleTerminalInput", input);
                scheduler.Start();
                Thread.Sleep(70); // UI remains blocked throughout these checks.
                if (scenario.StartsWith("fallback-", StringComparison.Ordinal))
                    Check((int)typeof(MainForm).GetField("activatorTickQueued", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form) == 1,
                        "our own foreground window must keep timing/condition inspection coalesced on the UI lane");
                if (scenario == "physical-release") Volatile.Write(ref physical, 0);
                if (scenario == "release") Call(form, "HandleTerminalInputReleased", input);
                if (scenario == "reset") Call(form, "StopMacroForDefinitionChange", macro, "test-edit");
                if (scenario == "stop-all") Call(form, "StopAllMacroRuns", "test-stop");
                if (scenario == "stop-selected") Call(form, "StopMacro", macro, "test-stop");
                if (scenario == "panic")
                {
                    TriggerSpec panic = config.PanicTrigger;
                    Call(form, "HandleTerminalInput", new InputEventInfo { Input = new InputSpec { Kind = panic.Kind, VirtualKey = panic.VirtualKey }, Ctrl = panic.Ctrl, Alt = panic.Alt, Shift = panic.Shift, Win = panic.Win });
                }
                if (scenario == "disabled") macro.Enabled = false;
                if (scenario == "removed" || scenario == "profile-replaced" || scenario == "fallback-removed")
                {
                    // Reproduce an edge selected before a definition/profile change but
                    // committed to the trigger engine after its reset. Old UI admission
                    // rejected this owner by membership in the current configuration.
                    Call(form, "StopAllMacroRuns", "profile-change");
                    if (scenario == "profile-replaced")
                    {
                        MacroConfig replacement = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
                        replacement.Macros.Add(macro.Clone());
                        Set(form, "config", replacement);
                    }
                    else config.Macros.Remove(macro);
                    ActivatorRuntimeEngine engine = (ActivatorRuntimeEngine)typeof(MainForm).GetField("activatorEngine", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                    engine.OnDown(macro, input, macro.Trigger, IdleGamepadService.MonotonicMilliseconds(), true, true);
                }
                if (scenario == "layer-loss") Set(form, "activeMappingLayerId", "");
                if (scenario == "editor") Set(form, "uiSafetyPauseRequested", true);
                if (scenario == "dialog") Set(form, "pauseHotkeys", true);
                if (scenario == "suspended") Set(form, "manualTriggerSuspend", true);
                if (scenario == "recording") Set(form, "recordingActive", true);
                if (scenario == "modal") Set(form, "uiSafetyModalDepth", 1);
                if (scenario == "capture")
                {
                    FieldInfo field = typeof(MainForm).GetField("captureMode", BindingFlags.Instance | BindingFlags.NonPublic);
                    field.SetValue(form, Enum.Parse(field.FieldType, "StepInput"));
                }
                Thread.Sleep(330);
                bool run = scenario == "latency" || scenario == "worker-protection" || scenario == "worker-panic";
                Check((observer.Downs != 0) == run, scenario + ": incorrect output while UI blocked");
                long latency = observer.FirstDown == 0 ? -1 : (observer.FirstDown - start) * 1000 / Stopwatch.Frequency;
                if (run) Check(latency >= 150 && latency < 400, "long-press output must respect 150ms threshold and precede the 400ms UI recovery; observed=" + latency);
                if (scenario == "worker-protection")
                {
                    Volatile.Write(ref ownWindow, 1);
                    Thread.Sleep(80);
                    int protectedDowns = observer.Downs;
                    Thread.Sleep(100);
                    Check(observer.Downs == protectedDowns && observer.Downs == observer.Ups, "native foreground guard must suspend and release output without UI pumping");
                    Volatile.Write(ref ownWindow, 0);
                    Until(delegate { return observer.Downs > protectedDowns; }, false);
                }
                if (scenario == "worker-panic")
                {
                    TriggerSpec panic = config.PanicTrigger;
                    Call(form, "HandleTerminalInput", new InputEventInfo { Input = new InputSpec { Kind = panic.Kind, VirtualKey = panic.VirtualKey }, Ctrl = panic.Ctrl, Alt = panic.Alt, Shift = panic.Shift, Win = panic.Win });
                    Until(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, false);
                    int afterStop = observer.Downs;
                    Thread.Sleep(80);
                    Check(observer.Downs == afterStop && observer.Downs == observer.Ups, "panic must stop/release during UI block");
                }
                if (scenario == "fallback-reset") Call(form, "StopMacroForDefinitionChange", macro, "test-edit");
                if (scenario == "fallback-release") { Volatile.Write(ref physical, 0); Call(form, "HandleTerminalInputReleased", input); }
                if (scenario == "fallback-safe")
                {
                    Application.DoEvents();
                    Until(delegate { return observer.Downs != 0; }, true);
                }
                Set(form, "recordingActive", false);
                if (!run && scenario != "fallback-safe")
                {
                    Application.DoEvents();
                    Check(observer.Downs == 0, scenario + ": stale trigger started after UI resumed");
                }
                Call(form, "StopAllMacroRuns", "test-cleanup");
                Until(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, true);
                Call(form, "HandleTerminalInputReleased", input);
                Application.DoEvents();
                Check(observer.Downs == observer.Ups, scenario + ": held output leaked");
                if (!run && scenario != "fallback-safe") Check(observer.Downs == 0, scenario + ": stale trigger started after UI resumed");
                if (scenario == "latency") Check(observer.Downs == 1, "one long-press must start exactly once");
                return latency;
            }
        }
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles(); EmbeddedDependencyLoader.Register();
            AdmissionTickets();
            string directory = Path.Combine(Path.GetTempPath(), "InputStitch-advanced-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            if (args.Length != 0 && args[0] == "--stale-guard-only")
            {
                Scenario(directory, "profile-replaced");
                Console.WriteLine("PASS replaced-profile late edge cannot start its old macro");
                return 0;
            }
            List<long> samples = new List<long>();
            for (int i = 0; i < 8; i++) samples.Add(Scenario(directory, "latency"));
            foreach (string scenario in new[] { "physical-release", "release", "reset", "stop-all", "stop-selected", "panic", "disabled", "removed", "profile-replaced", "layer-loss", "editor", "dialog", "suspended", "capture", "recording", "modal", "fallback-reset", "fallback-release", "fallback-removed", "fallback-safe", "worker-protection", "worker-panic" })
                Scenario(directory, scenario);
            string report = "{\"version\":\"" + AppInfo.Version + "\",\"thresholdMs\":150,\"uiBlockedMs\":400,\"samplesMs\":[" + string.Join(",", samples) + "],\"checks\":" + checks + ",\"backend\":\"isolated observer; explicit physical state/focus\",\"outputBeforeUiResumes\":true}";
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "advanced-trigger-dispatch.json"), report);
            Console.WriteLine(report);
            Console.WriteLine("PASS advanced trigger dispatch: " + checks + " checks; latency, cancellation, editing/capture/modal/recording protection, source cleanup, safe UI fallback, native foreground suspension/resume and synchronous panic stop.");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
