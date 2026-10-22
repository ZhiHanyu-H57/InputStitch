using System;
using System.Collections.Generic;

namespace InputStitch
{
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
