using System;
using System.Collections.Generic;

namespace InputStitch
{
    internal struct MacroRunControls
    {
        internal readonly int RunCount;
        internal readonly bool SelectedRunning, SelectedStopping, SingleStepActive, SingleStepWaiting, SingleStepStopping;
        internal MacroRunControls(int count, MacroRunRuntime selected, MacroRunRuntime singleStep)
        {
            RunCount = count;
            SelectedRunning = selected != null;
            SelectedStopping = selected != null && selected.Stop != null && selected.Stop.IsSet;
            SingleStepActive = singleStep != null;
            SingleStepWaiting = singleStep != null && singleStep.SingleStepWaiting;
            SingleStepStopping = singleStep != null && singleStep.Stop != null && singleStep.Stop.IsSet;
        }
    }

    // Detached presentation data. Consumers may format/mutate their input copies
    // without racing the worker or changing runtime/output state.
    internal sealed class MacroRunProgressSnapshot
    {
        internal readonly long RunId;
        internal readonly string Name, SourceId, Phase;
        internal readonly int Iteration, StepIndex, StepCount;
        internal readonly bool SingleStep, SingleStepWaiting;
        internal readonly MacroRuntimeCapability Capability;
        internal readonly InputSpec[] HeldInputs;
        internal MacroRunProgressSnapshot(MacroRunRuntime run)
        {
            RunId = run.RunId; SourceId = run.SourceId;
            Name = run.OwnerMacro == null ? "?" : run.OwnerMacro.Name;
            Phase = run.Phase; Iteration = run.Iteration; StepIndex = run.StepIndex; StepCount = run.StepCount;
            SingleStep = run.SingleStep; SingleStepWaiting = run.SingleStepWaiting;
            Capability = run.Capability ?? MacroRuntimeClassifier.Classify(run.OwnerMacro);
            HeldInputs = new InputSpec[run.HeldInputs.Count];
            int index = 0;
            foreach (InputSpec input in run.HeldInputs.Values) HeldInputs[index++] = input.Clone();
        }
    }

    // Owns run identity and lifecycle. No Form, UI dispatch, output backend or timer.
    // Shares the existing runtime lock so held mappings and UI safety keep their
    // atomic observations. Execution and presentation remain separate callers.
    internal sealed class MacroRunCoordinator
    {
        private readonly object sync;
        private readonly Action<long, string> onStopRequested;
        private readonly Dictionary<long, MacroRunRuntime> runs = new Dictionary<long, MacroRunRuntime>();
        private readonly Dictionary<MacroDefinition, long> byMacro = new Dictionary<MacroDefinition, long>();
        private long sequence;
        private string lastStopReason = "none";

        internal MacroRunCoordinator(object sync, Action<long, string> onStopRequested)
        {
            this.sync = sync;
            this.onStopRequested = onStopRequested;
        }

        internal int Count { get { lock (sync) return runs.Count; } }
        internal string LastStopReason { get { lock (sync) return lastStopReason; } }

        // Caller must hold sync throughout enumeration and any mutable run reads.
        internal Dictionary<long, MacroRunRuntime>.ValueCollection ActiveRunsLocked { get { return runs.Values; } }

        internal MacroRunRuntime Find(MacroDefinition macro)
        {
            lock (sync)
            {
                long id;
                MacroRunRuntime run;
                return macro != null && byMacro.TryGetValue(macro, out id) && runs.TryGetValue(id, out run) ? run : null;
            }
        }

        internal MacroRunRuntime SingleStepRun
        {
            get
            {
                lock (sync)
                {
                    foreach (MacroRunRuntime run in runs.Values) if (run.SingleStep) return run;
                    return null;
                }
            }
        }

        internal bool TryRegister(MacroRunRuntime run)
        {
            lock (sync)
            {
                if (Find(run.OwnerMacro) != null || (run.SingleStep && runs.Count != 0)) return false;
                run.RunId = ++sequence;
                run.SourceId = "macro:" + run.RunId.ToString();
                run.Phase = run.SingleStep ? "single-step-start" : "starting";
                run.SingleStepWaiting = false;
                run.StepCount = run.Snapshot == null || run.Snapshot.Steps == null ? 0 : run.Snapshot.Steps.Count;
                runs.Add(run.RunId, run);
                byMacro.Add(run.OwnerMacro, run.RunId);
                lastStopReason = "running";
                return true;
            }
        }

        internal bool IsCurrent(MacroRunRuntime run)
        {
            lock (sync)
            {
                MacroRunRuntime current;
                return run != null && runs.TryGetValue(run.RunId, out current) && object.ReferenceEquals(current, run);
            }
        }

        internal List<MacroRunRuntime> Snapshot()
        {
            lock (sync)
            {
                List<MacroRunRuntime> result = new List<MacroRunRuntime>(runs.Values);
                result.Sort(delegate(MacroRunRuntime a, MacroRunRuntime b) { return a.RunId.CompareTo(b.RunId); });
                return result;
            }
        }

        internal MacroRunControls ReadControls(MacroDefinition selected)
        {
            lock (sync) return new MacroRunControls(runs.Count, Find(selected), SingleStepRun);
        }

        internal List<MacroRunProgressSnapshot> ReadProgress()
        {
            lock (sync)
            {
                List<MacroRunProgressSnapshot> result = new List<MacroRunProgressSnapshot>(runs.Count);
                foreach (MacroRunRuntime run in runs.Values) result.Add(new MacroRunProgressSnapshot(run));
                result.Sort(delegate(MacroRunProgressSnapshot a, MacroRunProgressSnapshot b) { return a.RunId.CompareTo(b.RunId); });
                return result;
            }
        }

        internal void BeginExecution(MacroRunRuntime run, int steps)
        {
            lock (sync)
            {
                if (!IsCurrent(run) || run.Stop.IsSet) return;
                run.Phase = run.SingleStep ? "single-step-running" : "running";
                run.Iteration = 0; run.StepIndex = 0; run.StepCount = steps;
                run.HeldInputs.Clear();
            }
        }

        internal void SetIteration(MacroRunRuntime run, int iteration)
        {
            lock (sync) { if (IsCurrent(run) && !run.Stop.IsSet) run.Iteration = iteration; }
        }

        internal void BeginStep(MacroRunRuntime run, int step, int total)
        {
            lock (sync)
            {
                if (!IsCurrent(run) || run.Stop.IsSet) return;
                run.Phase = run.SingleStep ? "single-step-executing" : "running";
                run.StepIndex = step; run.StepCount = total;
            }
        }

        internal void SetSingleStepWaiting(MacroRunRuntime run, bool waiting)
        {
            lock (sync)
            {
                if (!IsCurrent(run)) return;
                run.SingleStepWaiting = waiting && !run.Stop.IsSet;
                run.Phase = run.Stop.IsSet ? "stopping" : (waiting ? "single-step-wait" : "single-step-running");
            }
        }

        internal bool TryAdvanceSingleStep()
        {
            lock (sync)
            {
                MacroRunRuntime run = SingleStepRun;
                if (run == null || !run.SingleStepWaiting || run.StepGate == null || run.Stop.IsSet) return false;
                run.StepGate.Set();
                return true;
            }
        }

        internal void UpdateHeldInputs(MacroRunRuntime run, Dictionary<string, InputSpec> held)
        {
            lock (sync)
            {
                if (!IsCurrent(run)) return;
                run.HeldInputs.Clear();
                if (held != null)
                    foreach (KeyValuePair<string, InputSpec> pair in held)
                        if (pair.Value != null) run.HeldInputs[pair.Key] = pair.Value.Clone();
                // Diagnostic names belong to presentation; no formatting on each output edge.
            }
        }

        internal void ClearHeldInputs()
        {
            lock (sync) foreach (MacroRunRuntime run in runs.Values) run.HeldInputs.Clear();
        }

        internal void RequestStop(MacroRunRuntime run, string reason)
        {
            lock (sync)
            {
                if (!IsCurrent(run) || run.Stop == null) return;
                string resolved = string.IsNullOrWhiteSpace(reason) ? "stop-request" : reason;
                if (!run.Stop.IsSet && onStopRequested != null) onStopRequested(run.RunId, resolved);
                run.StopReason = resolved;
                run.Phase = "stopping";
                lastStopReason = resolved;
                run.Stop.Set();
                if (run.StepGate != null) run.StepGate.Set();
            }
        }

        internal void Stop(MacroDefinition macro, string reason)
        {
            lock (sync) RequestStop(Find(macro), reason);
        }

        internal void Stop(long id, string reason)
        {
            lock (sync)
            {
                MacroRunRuntime run;
                if (runs.TryGetValue(id, out run)) RequestStop(run, reason);
            }
        }

        internal List<MacroWorkItem> StopAll(string reason)
        {
            lock (sync)
            {
                List<MacroWorkItem> work = new List<MacroWorkItem>();
                foreach (MacroRunRuntime run in runs.Values)
                {
                    RequestStop(run, reason);
                    if (run.Work != null) work.Add(run.Work);
                }
                return work;
            }
        }

        internal bool Finish(MacroRunRuntime run, string reason)
        {
            lock (sync)
            {
                if (!IsCurrent(run)) return false;
                runs.Remove(run.RunId);
                byMacro.Remove(run.OwnerMacro);
                run.Phase = "finished";
                run.SingleStepWaiting = false;
                // Choose the final reason under the same lock as Stop; a request
                // arriving at completion must not race a caller-side snapshot.
                run.StopReason = reason ?? (run.Stop.IsSet ? run.StopReason : "completed");
                lastStopReason = run.StopReason;
                return true;
            }
        }
    }
}
