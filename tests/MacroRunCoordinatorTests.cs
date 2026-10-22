using System;
using System.Collections.Generic;
using System.Threading;
using InputStitch;

internal static class MacroRunCoordinatorTests
{
    private static int checks;
    private static void Check(bool ok, string message)
    { checks++; if (!ok) throw new Exception(message); }

    private static MacroRunRuntime Run(MacroDefinition macro, bool singleStep)
    {
        return new MacroRunRuntime { OwnerMacro = macro, Stop = new ManualResetEventSlim(false),
            SingleStep = singleStep, StepGate = singleStep ? new AutoResetEvent(false) : null,
            Work = new MacroWorkItem(delegate { }) };
    }
    private static void Dispose(MacroRunRuntime run)
    { run.Stop.Dispose(); if (run.StepGate != null) run.StepGate.Dispose(); }

    public static int Main()
    {
        try
        {
            Lifecycle();
            ConcurrentRegistration();
            Console.WriteLine("PASS run coordinator: " + checks + " checks; identity, independent stop, stale completion, single-step, stop-all and concurrent registration; no UI or output.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Lifecycle()
    {
        int traces = 0;
        MacroRunCoordinator coordinator = new MacroRunCoordinator(new object(), delegate { traces++; });
        MacroDefinition first = new MacroDefinition(), second = new MacroDefinition();
        MacroRunRuntime a = Run(first, false), b = Run(second, false), duplicate = Run(first, false);
        Check(coordinator.Count == 0 && coordinator.LastStopReason == "none", "empty initial state");
        Check(coordinator.TryRegister(a) && coordinator.TryRegister(b), "independent runs register");
        Check(a.RunId == 1 && b.RunId == 2 && a.SourceId == "macro:1" && b.SourceId == "macro:2", "unique monotonic run/source identity");
        Check(!coordinator.TryRegister(duplicate), "duplicate owner rejected");
        MacroRunRuntime step = Run(new MacroDefinition(), true);
        Check(!coordinator.TryRegister(step), "single-step cannot enter while other runs exist");
        List<MacroRunRuntime> snapshot = coordinator.Snapshot(); snapshot.Clear();
        Check(coordinator.Count == 2, "snapshot membership is defensive");
        coordinator.Stop(first, "local-stop");
        Check(a.Stop.IsSet && !b.Stop.IsSet && a.StopReason == "local-stop", "stop by owner is source-local");
        coordinator.Stop(a.RunId, "repeat-stop");
        Check(traces == 1 && a.StopReason == "repeat-stop", "stop signals are idempotent and latest reason retained");
        Check(!coordinator.TryRegister(duplicate), "stopping run retains registration until completion");
        Check(coordinator.Finish(a, null) && !coordinator.Finish(a, "late") && a.StopReason == "repeat-stop", "completion removes exactly once and retains stop reason");
        Check(coordinator.TryRegister(duplicate) && duplicate.RunId == 3, "restarted owner gets new identity");
        a.RunId = duplicate.RunId;
        Check(!coordinator.Finish(a, "stale") && coordinator.Find(first) == duplicate, "stale completion cannot delete restarted run");
        coordinator.RequestStop(a, "stale-stop");
        Check(!duplicate.Stop.IsSet, "stale stop cannot cancel restarted run");
        coordinator.Stop(999, "missing");
        Check(coordinator.Count == 2, "unknown id does not affect active runs");
        List<MacroWorkItem> work = coordinator.StopAll("emergency");
        Check(work.Count == 2 && b.Stop.IsSet && duplicate.Stop.IsSet, "stop-all returns independent work completions");
        Check(coordinator.Count == 2, "stop-all waits for workers to finish before removal");
        coordinator.Finish(b, "error"); coordinator.Finish(duplicate, "emergency");
        Check(coordinator.TryRegister(step) && coordinator.SingleStepRun == step, "single-step registers on empty runtime");
        coordinator.Stop(step.RunId, "step-stop");
        Check(step.Stop.IsSet && step.StepGate.WaitOne(0), "stop wakes single-step wait");
        Check(coordinator.Finish(step, "start-error") && coordinator.LastStopReason == "start-error", "start failure clears registry and reports reason");
        Check(coordinator.Count == 0 && coordinator.SingleStepRun == null && coordinator.Snapshot().Count == 0, "final registry empty");
        Dispose(a); Dispose(b); Dispose(duplicate); Dispose(step);
    }

    private static void ConcurrentRegistration()
    {
        MacroRunCoordinator coordinator = new MacroRunCoordinator(new object(), null);
        MacroDefinition owner = new MacroDefinition();
        MacroRunRuntime[] runs = new MacroRunRuntime[16];
        Thread[] threads = new Thread[runs.Length];
        int accepted = 0;
        using (ManualResetEventSlim gate = new ManualResetEventSlim(false))
        {
            for (int i = 0; i < runs.Length; i++)
            {
                int index = i; runs[i] = Run(owner, false);
                threads[i] = new Thread(delegate() { gate.Wait(); if (coordinator.TryRegister(runs[index])) Interlocked.Increment(ref accepted); });
                threads[i].Start();
            }
            gate.Set();
            foreach (Thread thread in threads) Check(thread.Join(3000), "registration thread completes");
        }
        Check(accepted == 1 && coordinator.Count == 1, "racing starts install exactly one owner");
        MacroRunRuntime winner = coordinator.Find(owner);
        coordinator.Finish(winner, null);
        Check(winner.StopReason == "completed", "natural completion selects completed reason under lock");
        foreach (MacroRunRuntime run in runs) Dispose(run);
    }
}
