using System;
using System.Collections.Generic;

namespace InputStitch
{
    internal sealed class StepTimingEdit
    {
        internal bool AllSteps;
        internal bool EditHold;
        internal int HoldMs = 30;
        internal bool EditWait = true;
        internal bool RandomWait;
        internal int WaitMs = 50;
        internal int MinWaitMs = 45;
        internal int MaxWaitMs = 60;
    }

    internal static class StepTimingEditor
    {
        internal static bool SupportsHold(MacroStep step)
        {
            return step != null && step.Action == MacroAction.Press &&
                step.Kind != InputKind.WheelUp && step.Kind != InputKind.WheelDown;
        }

        private static void ValidateMilliseconds(int value)
        {
            if (value < 0 || value > 600000) throw new ArgumentOutOfRangeException("value");
        }

        internal static int Apply(IList<MacroStep> steps, IList<int> selected, StepTimingEdit edit)
        {
            if (edit == null) throw new ArgumentNullException("edit");
            if (edit.EditHold) ValidateMilliseconds(edit.HoldMs);
            if (edit.EditWait)
            {
                if (edit.RandomWait)
                {
                    ValidateMilliseconds(edit.MinWaitMs);
                    ValidateMilliseconds(edit.MaxWaitMs);
                    if (edit.MinWaitMs > edit.MaxWaitMs) throw new ArgumentException("Invalid wait range.");
                }
                else ValidateMilliseconds(edit.WaitMs);
            }
            if (steps == null || (!edit.EditHold && !edit.EditWait)) return 0;
            HashSet<int> indices = new HashSet<int>();
            if (edit.AllSteps) for (int i = 0; i < steps.Count; i++) indices.Add(i);
            else if (selected != null) foreach (int index in selected)
                if (index >= 0 && index < steps.Count) indices.Add(index);
            int changed = 0;
            foreach (int index in indices)
            {
                MacroStep step = steps[index];
                if (step == null) continue;
                bool different = false;
                if (edit.EditHold && SupportsHold(step) && step.HoldMs != edit.HoldMs)
                { step.HoldMs = edit.HoldMs; different = true; }
                if (edit.EditWait)
                {
                    different |= step.RandomDelay != edit.RandomWait;
                    step.RandomDelay = edit.RandomWait;
                    if (edit.RandomWait)
                    {
                        different |= step.RandomDelayMinMs != edit.MinWaitMs || step.RandomDelayMaxMs != edit.MaxWaitMs;
                        step.RandomDelayMinMs = edit.MinWaitMs;
                        step.RandomDelayMaxMs = edit.MaxWaitMs;
                    }
                    else
                    {
                        different |= step.DelayMs != edit.WaitMs;
                        step.DelayMs = edit.WaitMs;
                    }
                }
                if (different) changed++;
            }
            return changed;
        }
    }
}
