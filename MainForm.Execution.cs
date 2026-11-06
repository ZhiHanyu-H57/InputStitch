using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    public partial class MainForm
    {
        // The executor calls these same policies at the same points as the old
        // loop. Only this adapter/host layer knows about editing and presentation.
        private sealed class MainFormExecutionEnvironment : IMacroExecutionEnvironment
        {
            private readonly MainForm owner;
            internal MainFormExecutionEnvironment(MainForm owner) { this.owner = owner; }
            public bool WaitForTriggerRelease(MacroExecutionContext execution, TriggerSpec trigger)
            { return WaitForTriggerReleaseStable(execution.Run.Stop, trigger, PhysicalReleaseSettleMs); }
            public bool WaitForUiSafetyClear(MacroExecutionContext execution)
            { return owner.WaitForUiSafetyClear(execution.Run.Stop, execution.Name, execution.Held, execution.Run.SourceId, execution.Run.NativeUiGuard); }
            public bool WaitOrStopWithUiSafety(MacroExecutionContext execution, int milliseconds)
            { return owner.WaitOrStopWithUiSafety(execution.Run.Stop, milliseconds, execution.Name, execution.Held, execution.Run.SourceId, execution.Run.NativeUiGuard); }
            public bool WaitForDangerousShortcutClear(MacroExecutionContext execution, MacroStep step)
            { return owner.WaitForDangerousPhysicalShortcutToClear(execution.Run.Stop, execution.Name, execution.Held, step, execution.Run.SourceId, execution.Run.NativeUiGuard); }
            public bool WaitForSingleStepAdvance(MacroExecutionContext execution, int completedSteps, int totalSteps)
            { return owner.WaitForSingleStepAdvance(execution.Run, completedSteps, totalSteps); }
            public void ReportStatus(MacroExecutionContext execution, MacroExecutionPhase phase, int value)
            {
                string text;
                if (phase == MacroExecutionPhase.WaitingForRelease)
                    text = "准备执行：" + execution.Name + "（等待触发键松开）";
                else if (phase == MacroExecutionPhase.StartDelay)
                    text = "准备执行：" + execution.Name + "（等待目标窗口稳定 " + value.ToString() + " ms）";
                else
                    text = "正在执行：" + execution.Name + (execution.Infinite ? "（第 " + value.ToString() + " 次，无限循环）" : "（" + value.ToString() + "/" + execution.RepeatCount.ToString() + "）");
                owner.SetMacroRunStatusSafe(execution.Run, text);
            }
        }

        private void MacroWorker(MacroRunRuntime runtime, int startDelayMs, TriggerSpec waitForReleaseTrigger)
        {
            ManualResetEventSlim stop = runtime.Stop;
            System.Threading.AutoResetEvent stepGate = runtime.StepGate;
            MacroExecutionContext execution = new MacroExecutionContext(runtime);
            string sourceId = runtime.SourceId;
            string executionError = null;
            try
            {
                macroExecutor.Execute(execution, startDelayMs, waitForReleaseTrigger);
            }
            catch (Exception ex)
            {
                executionError = ex.Message;
                AppLog.Write("Macro worker failed: " + execution.Name, ex);
                if (ex is OutputOwnershipException) HandleOutputOwnershipFailure(ex, "macro-worker");
            }
            finally
            {
                runtimeTrace.Add(executionError != null ? "worker-error" : (stop.IsSet ? "worker-cancelled" : "worker-complete"),
                    "run=" + runtime.RunId.ToString() + "; output-submitted=" + execution.OutputObserved.ToString());
                try { outputOwnership.ClearSource(sourceId); }
                catch (OutputOwnershipException ex) { HandleOutputOwnershipFailure(ex, "macro-finally"); }
                execution.Held.Clear();
                SyncRunHeldInputs(runtime, execution.Held);

                bool removed = runCoordinator.Finish(runtime, executionError != null ? "error" : null);
                try { stop.Dispose(); } catch { }
                try { if (stepGate != null) stepGate.Dispose(); } catch { }
                if (idleGamepad != null) idleGamepad.NotifyActivity();
                if (removed)
                {
                    if (executionError != null)
                        SetStatusSafe((Localizer.IsEnglish ? "Macro failed: " : "执行出错：") + executionError);
                    else RefreshAggregateRunStatusSafe();
                    UpdateRunButtonSafe();
                }
            }
        }

        private bool WaitForSingleStepAdvance(MacroRunRuntime runtime, int completedSteps, int totalSteps)
        {
            if (runtime == null || runtime.Stop == null || runtime.Stop.IsSet) return true;
            ManualResetEventSlim stop = runtime.Stop;
            System.Threading.AutoResetEvent gate = runtime.StepGate;
            runCoordinator.SetSingleStepWaiting(runtime, true);
            SetStatusSafe((Localizer.IsEnglish ? "Single-step: completed " : "单步：已执行 ") + completedSteps.ToString() + "/" + totalSteps.ToString() +
                (Localizer.IsEnglish ? "; click Next Step to continue." : "；点击“下一步”继续。"));
            UpdateRunButtonSafe();

            while (!stop.IsSet)
            {
                if (gate != null && gate.WaitOne(50)) break;
                if (gate == null && stop.Wait(50)) break;
            }

            runCoordinator.SetSingleStepWaiting(runtime, false);
            UpdateRunButtonSafe();
            if (!stop.IsSet) SetStatusSafe((Localizer.IsEnglish ? "Single-step executing: " : "单步执行：") + (runtime.Snapshot == null ? "" : runtime.Snapshot.Name));
            return stop.IsSet;
        }

        private void SyncRunHeldInputs(MacroRunRuntime runtime, Dictionary<string, InputSpec> held)
        {
            runCoordinator.UpdateHeldInputs(runtime, held);
        }

        private void SetMacroRunStatusSafe(MacroRunRuntime runtime, string singleRunText)
        {
            int count = ActiveMacroRunCount();
            if (count <= 1)
            {
                SetStatusSafe(singleRunText);
                return;
            }
            string name = runtime == null || runtime.Snapshot == null ? "" : runtime.Snapshot.Name;
            SetStatusSafe(Localizer.IsEnglish
                ? "Running " + count.ToString() + " macros (latest activity: " + name + ")"
                : "正在并发执行 " + count.ToString() + " 个宏（最近活动：" + name + "）");
        }

        private void RefreshAggregateRunStatusSafe()
        {
            int runCount = ActiveMacroRunCount();
            if (runCount > 0)
            {
                SetStatusSafe(Localizer.IsEnglish ? "Running macros: " + runCount.ToString() : "正在执行的宏：" + runCount.ToString() + " 个");
                return;
            }
            int heldCount = ActiveParallelHeldMappingCount();
            if (heldCount > 0)
            {
                SetStatusSafe(Localizer.IsEnglish ? "Held mappings active: " + heldCount.ToString() : "按住映射活动：" + heldCount.ToString());
                return;
            }
            SetStatusSafe(Localizer.T("状态：空闲"));
        }

        private bool WaitForUiSafetyClear(ManualResetEventSlim stop, string macroName, Dictionary<string, InputSpec> held, string sourceId, bool nativeUiGuard = false)
        {
            if (stop.IsSet) return true;
            if (!WorkerUiPauseRequested(nativeUiGuard)) return false;

            // Suspend only this worker's source. Other parallel Held Mapping sources remain active.
            try { outputOwnership.SuspendSource(sourceId); }
            catch (OutputOwnershipException ex) { HandleOutputOwnershipFailure(ex, "ui-suspend"); return true; }

            string reason = uiSafetyPauseReason;
            if (string.IsNullOrWhiteSpace(reason)) reason = "编辑控件";
            SetStatusSafe("已暂停：" + macroName + "（UI编辑保护：" + Localizer.T(reason) + "）");

            while (!stop.IsSet && WorkerUiPauseRequested(nativeUiGuard))
            {
                if (stop.Wait(10)) return true;
            }
            if (stop.IsSet) return true;

            try { outputOwnership.ResumeSource(sourceId); }
            catch (OutputOwnershipException ex) { HandleOutputOwnershipFailure(ex, "ui-resume"); return true; }
            SetStatusSafe("正在执行：" + macroName + "（UI编辑保护已恢复）");
            return false;
        }

        private bool WaitOrStopWithUiSafety(ManualResetEventSlim stop, int ms, string macroName, Dictionary<string, InputSpec> held, string sourceId, bool nativeUiGuard = false)
        {
            if (WaitForUiSafetyClear(stop, macroName, held, sourceId, nativeUiGuard)) return true;
            if (ms <= 0) return stop.IsSet;

            // Do not assume that Wait(10) advances time by exactly 10 ms. On Windows the scheduler
            // may wake substantially later (commonly around the system timer quantum). Subtracting
            // the requested slice once per loop makes long macro delays drift badly because that
            // overshoot accumulates. Track actual monotonic elapsed time instead.
            double remainingMs = ms;
            while (remainingMs > 0.0 && !stop.IsSet)
            {
                if (WaitForUiSafetyClear(stop, macroName, held, sourceId, nativeUiGuard)) return true;
                int slice = Math.Max(1, Math.Min(10, (int)Math.Ceiling(remainingMs)));
                long before = Stopwatch.GetTimestamp();
                if (stop.Wait(slice)) return true;
                long after = Stopwatch.GetTimestamp();

                // If UI protection became active during this slice, preserve the historical rule:
                // none of that slice counts toward macro timing. The next iteration waits for the
                // protection state to clear before timing resumes.
                if (!WorkerUiPauseRequested(nativeUiGuard))
                {
                    double elapsedMs = (after - before) * 1000.0 / Stopwatch.Frequency;
                    if (elapsedMs <= 0.0) elapsedMs = slice;
                    remainingMs -= elapsedMs;
                }
            }
            return stop.IsSet;
        }

        private bool WaitForDangerousPhysicalShortcutToClear(ManualResetEventSlim stop, string macroName, Dictionary<string, InputSpec> held, MacroStep step, string sourceId, bool nativeUiGuard = false)
        {
            if (step == null || step.Action == MacroAction.Up || step.Kind != InputKind.Keyboard) return stop.IsSet;

            bool observedConflict = false;
            long conflictFreeSince = 0;
            long requiredTicks = PhysicalReleaseSettleMs * Stopwatch.Frequency / 1000L;
            while (!stop.IsSet)
            {
                if (WorkerUiPauseRequested(nativeUiGuard) && WaitForUiSafetyClear(stop, macroName, held, sourceId, nativeUiGuard)) return true;

                HookManager h = hooks;
                int hookMask = h == null ? 0 : h.PhysicalModifierMaskSnapshot;
                int macroHeldMask = GetMacroHeldModifierMask(held);
                // GetAsyncKeyState includes InputStitch's own SendInput. Remove macro-held
                // modifiers from that source, then add back any same modifier proven physical
                // by the low-level hook snapshot.
                int asyncMask = PhysicalInputState.GetModifierMask();
                int physicalMask = hookMask | (asyncMask & ~macroHeldMask);
                int effectiveMask = physicalMask | macroHeldMask;
                int dangerousMask = ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(step, effectiveMask);
                int physicalBlockers = dangerousMask & physicalMask;

                if (physicalBlockers == 0)
                {
                    if (!observedConflict) return stop.IsSet;
                    long now = Stopwatch.GetTimestamp();
                    if (conflictFreeSince == 0) conflictFreeSince = now;
                    else if (now - conflictFreeSince >= requiredTicks) break;
                }
                else
                {
                    conflictFreeSince = 0;
                    if (!observedConflict)
                    {
                        observedConflict = true;
                        Interlocked.Increment(ref dangerousShortcutAvoidanceCount);
                        AppLog.Write("Delayed macro step to avoid a physical shortcut: macro=" +
                            macroName + ", key=" + InputNames.FormatInput(step) +
                            ", modifierMask=" + physicalBlockers.ToString());
                        SetStatusSafe("正在执行：" + macroName + "（等待避免特殊快捷键冲突）");
                    }
                }
                if (WaitOrStop(stop, 5)) return true;
            }
            if (!stop.IsSet) SetStatusSafe("正在执行：" + macroName);
            return stop.IsSet;
        }

        private static int GetMacroHeldModifierMask(Dictionary<string, InputSpec> held)
        {
            if (held == null || held.Count == 0) return 0;
            int mask = 0;
            foreach (KeyValuePair<string, InputSpec> pair in held)
            {
                InputSpec input = pair.Value;
                if (input == null || input.Kind != InputKind.Keyboard) continue;
                int vk = input.VirtualKey;
                if (vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey || vk == (int)Keys.ControlKey)
                    mask |= ModifierSafetyPolicy.Ctrl;
                else if (vk == (int)Keys.LShiftKey || vk == (int)Keys.RShiftKey || vk == (int)Keys.ShiftKey)
                    mask |= ModifierSafetyPolicy.Shift;
                else if (vk == (int)Keys.LMenu || vk == (int)Keys.RMenu || vk == (int)Keys.Menu)
                    mask |= ModifierSafetyPolicy.Alt;
                else if (vk == (int)Keys.LWin || vk == (int)Keys.RWin)
                    mask |= ModifierSafetyPolicy.Win;
            }
            return mask;
        }

        private static bool WaitForTriggerReleaseStable(ManualResetEventSlim stop, TriggerSpec trigger, int settleMs)
        {
            long releasedSince = 0;
            long requiredTicks = Math.Max(0, settleMs) * Stopwatch.Frequency / 1000L;

            while (!stop.IsSet)
            {
                if (!PhysicalInputState.IsTriggerTerminalReleased(trigger))
                {
                    releasedSince = 0;
                }
                else
                {
                    long now = Stopwatch.GetTimestamp();
                    if (releasedSince == 0)
                    {
                        releasedSince = now;
                    }
                    else if (now - releasedSince >= requiredTicks)
                    {
                        return false;
                    }
                }

                if (stop.Wait(5)) return true;
            }
            return true;
        }

    }
}
