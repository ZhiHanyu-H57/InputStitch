using System;
using System.Collections.Generic;
using System.Threading;

namespace InputStitch
{
    internal enum MacroExecutionPhase { WaitingForRelease, StartDelay, Iteration }

    // One execution owns this state. Cached plan inputs and Held values are read-only;
    // ownership and progress snapshots continue to copy their own data.
    internal sealed class MacroExecutionContext
    {
        internal readonly MacroRunRuntime Run;
        internal readonly string Name;
        internal readonly bool SingleStep;
        internal readonly bool Infinite;
        internal readonly int RepeatCount;
        internal readonly Dictionary<string, InputSpec> Held = new Dictionary<string, InputSpec>();
        internal bool OutputObserved;

        internal MacroExecutionContext(MacroRunRuntime run)
        {
            Run = run;
            Name = run.Snapshot.Name;
            SingleStep = run.SingleStep;
            Infinite = !SingleStep && run.Snapshot.Infinite;
            RepeatCount = SingleStep ? 1 : Math.Max(1, run.Snapshot.RepeatCount);
        }
    }

    // The host owns UI/physical-state policy and interruptible waits. Returning true
    // from a wait cancels execution. No input, stop command or event is marshalled here.
    internal interface IMacroExecutionEnvironment
    {
        bool WaitForTriggerRelease(MacroExecutionContext execution, TriggerSpec trigger);
        bool WaitForUiSafetyClear(MacroExecutionContext execution);
        bool WaitOrStopWithUiSafety(MacroExecutionContext execution, int milliseconds);
        bool WaitForDangerousShortcutClear(MacroExecutionContext execution, MacroStep step);
        bool WaitForSingleStepAdvance(MacroExecutionContext execution, int completedSteps, int totalSteps);
        void ReportStatus(MacroExecutionContext execution, MacroExecutionPhase phase, int value);
    }

    // Executes an admitted run without Forms, window handles or UI dispatch.
    // The caller retains registration, final source release, handle disposal and
    // global failure recovery, so existing stop/completion ordering stays authoritative.
    internal sealed class MacroExecutor
    {
        private readonly MacroRunCoordinator coordinator;
        private readonly OutputOwnershipManager ownership;
        private readonly RuntimeTrace trace;
        private readonly IMacroExecutionEnvironment environment;

        internal MacroExecutor(MacroRunCoordinator coordinator, OutputOwnershipManager ownership,
            RuntimeTrace trace, IMacroExecutionEnvironment environment)
        {
            if (coordinator == null) throw new ArgumentNullException("coordinator");
            if (ownership == null) throw new ArgumentNullException("ownership");
            if (trace == null) throw new ArgumentNullException("trace");
            if (environment == null) throw new ArgumentNullException("environment");
            this.coordinator = coordinator; this.ownership = ownership;
            this.trace = trace; this.environment = environment;
        }

        internal void Execute(MacroExecutionContext execution, int startDelayMs, TriggerSpec waitForReleaseTrigger)
        {
            MacroRunRuntime runtime = execution.Run;
            ManualResetEventSlim stop = runtime.Stop;
            string sourceId = runtime.SourceId;
            Dictionary<string, InputSpec> held = execution.Held;
            Random random = new Random(unchecked(Environment.TickCount * 31 + (int)(runtime.RunId & 0x7FFFFFFF)));
            int lastStatusTick = unchecked(Environment.TickCount - 100);
            MacroExecutionPlan plan = new MacroExecutionPlan(runtime.Snapshot.Steps);
            coordinator.BeginExecution(runtime, plan.Count);

            if (waitForReleaseTrigger != null)
            {
                environment.ReportStatus(execution, MacroExecutionPhase.WaitingForRelease, 0);
                if (environment.WaitForTriggerRelease(execution, waitForReleaseTrigger)) return;
            }
            if (startDelayMs > 0)
            {
                environment.ReportStatus(execution, MacroExecutionPhase.StartDelay, startDelayMs);
                if (environment.WaitOrStopWithUiSafety(execution, startDelayMs)) return;
            }
            if (environment.WaitForUiSafetyClear(execution)) return;

            int iteration = 0;
            while (!stop.IsSet)
            {
                if (!execution.Infinite && iteration >= execution.RepeatCount) break;
                iteration++;
                bool producedOutput = false;
                coordinator.SetIteration(runtime, iteration);
                int statusTick = Environment.TickCount;
                if (iteration == 1 || unchecked(statusTick - lastStatusTick) >= 100)
                {
                    lastStatusTick = statusTick;
                    environment.ReportStatus(execution, MacroExecutionPhase.Iteration, iteration);
                }

                for (int stepIndex = 0; stepIndex < plan.Count; stepIndex++)
                {
                    PreparedMacroStep prepared = plan[stepIndex];
                    MacroStep step = prepared.Definition;
                    if (stop.IsSet) break;
                    if (execution.SingleStep && stepIndex > 0 && environment.WaitForSingleStepAdvance(execution, stepIndex, plan.Count)) break;
                    coordinator.BeginStep(runtime, stepIndex + 1, plan.Count);
                    InputSpec input = prepared.Input;
                    string key = prepared.Key;
                    if (environment.WaitForUiSafetyClear(execution)) break;
                    if (environment.WaitForDangerousShortcutClear(execution, step)) break;

                    if (step.Action == MacroAction.Press)
                    {
                        if (prepared.Holdable)
                        {
                            bool alreadyHeldByThisRun = held.ContainsKey(key);
                            if (!alreadyHeldByThisRun)
                            {
                                ownership.SetDown(sourceId, input); Submitted(execution);
                                producedOutput = true;
                                held[key] = input;
                                coordinator.UpdateHeldInputs(runtime, held);
                            }
                            if (environment.WaitOrStopWithUiSafety(execution, Math.Max(0, step.HoldMs))) break;
                            if (!alreadyHeldByThisRun)
                            {
                                ownership.SetUp(sourceId, input); Submitted(execution);
                                producedOutput = true;
                                held.Remove(key);
                                coordinator.UpdateHeldInputs(runtime, held);
                            }
                        }
                        else
                        {
                            ownership.SetDown(sourceId, input); Submitted(execution);
                            producedOutput = true;
                        }
                    }
                    else if (step.Action == MacroAction.Down)
                    {
                        InputSpec existingHeld;
                        bool duplicateGamepadState = input.Kind == InputKind.Gamepad &&
                            held.TryGetValue(key, out existingHeld) && SameGamepadState(existingHeld, input);
                        if (!duplicateGamepadState)
                        {
                            ownership.SetDown(sourceId, input); Submitted(execution);
                            producedOutput = true;
                            if (prepared.Holdable)
                            {
                                held[key] = input;
                                coordinator.UpdateHeldInputs(runtime, held);
                            }
                        }
                    }
                    else
                    {
                        ownership.SetUp(sourceId, input); Submitted(execution);
                        producedOutput = true;
                        held.Remove(key);
                        coordinator.UpdateHeldInputs(runtime, held);
                    }
                    if (stop.IsSet) break;
                    if (!execution.SingleStep && environment.WaitOrStopWithUiSafety(execution, ResolveStepDelay(step, random))) break;
                }
                if (execution.Infinite && !plan.HasNaturalDelay && !stop.IsSet)
                    if (environment.WaitOrStopWithUiSafety(execution, producedOutput ? 1 : 15)) break;
            }
        }

        private void Submitted(MacroExecutionContext execution)
        {
            if (execution.OutputObserved) return;
            trace.Add("output-submitted", "run=" + execution.Run.RunId.ToString());
            execution.OutputObserved = true;
        }

        internal static int ResolveStepDelay(MacroStep step, Random random)
        {
            if (step == null) return 0;
            if (!step.RandomDelay) return Math.Max(0, step.DelayMs);
            int min = Math.Max(0, Math.Min(step.RandomDelayMinMs, step.RandomDelayMaxMs));
            int max = Math.Max(0, Math.Max(step.RandomDelayMinMs, step.RandomDelayMaxMs));
            if (max <= min) return min;
            return random.Next(min, max + 1);
        }

        private static bool SameGamepadState(InputSpec left, InputSpec right)
        {
            if (left == null || right == null || left.Kind != InputKind.Gamepad || right.Kind != InputKind.Gamepad) return false;
            return left.GamepadControl == right.GamepadControl && left.GamepadX == right.GamepadX &&
                left.GamepadY == right.GamepadY && left.GamepadValue == right.GamepadValue;
        }
    }
}
