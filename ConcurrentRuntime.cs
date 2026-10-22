using System;
using System.Collections.Generic;
using System.Threading;

namespace InputStitch
{
    internal enum MacroRuntimeCategory
    {
        Invalid = 0,
        ParallelHeldMapping = 1,
        ConcurrentTimedMacro = 2,
        ConcurrentHoldMacro = 3
    }

    internal sealed class MacroRuntimeCapability
    {
        public MacroRuntimeCategory Category;
        public bool CanStart;
        public bool CanStartFromConfiguredTrigger;
        public string ReasonCode = "";

        public string CategoryText(bool english)
        {
            if (Category == MacroRuntimeCategory.ParallelHeldMapping)
                return english ? "Parallel Held Mapping" : "并行按住映射";
            if (Category == MacroRuntimeCategory.ConcurrentTimedMacro)
                return english ? "Concurrent Timed Macro" : "可并发时序宏";
            if (Category == MacroRuntimeCategory.ConcurrentHoldMacro)
                return english ? "Concurrent Advanced Hold" : "可并发高级 Hold";
            return english ? "Invalid / Incomplete" : "无效 / 未完成";
        }

        public string ConcurrencyText(bool english)
        {
            return CanStart ? (english ? "Concurrent" : "可并发") : (english ? "Cannot run" : "不可运行");
        }

        public string TriggerEligibilityText(bool english)
        {
            if (Category != MacroRuntimeCategory.ConcurrentHoldMacro && Category != MacroRuntimeCategory.ParallelHeldMapping)
                return "";
            if (CanStartFromConfiguredTrigger)
                return english ? "Hold shortcut available" : "按住触发可用";
            return english
                ? "Hold shortcut unavailable: choose a key or mouse button."
                : "按住触发不可用：请选择键盘键或鼠标按钮。";
        }

        public string ReasonText(bool english)
        {
            switch (ReasonCode ?? "")
            {
                case "no-macro":
                    return english ? "No macro is selected." : "尚未选择宏。";
                case "no-steps":
                    return english ? "Add or record a step to run this macro." : "请先添加或录制步骤，再运行这个宏。";
                case "parallel-held":
                    return english
                        ? "Holds the controller input while you hold the shortcut. Releasing it stops the macro."
                        : "按住触发键时保持手柄操作，松开即停止。";
                case "timed":
                    return english
                        ? "Runs the steps in order. Other macros can run at the same time."
                        : "按顺序执行步骤，也可同时运行其他宏。";
                case "hold-finite":
                    return english
                        ? "Runs the set number of times while the shortcut is held. Release it to stop early."
                        : "按住触发键时执行指定次数，松开可提前停止。";
                case "hold-trigger":
                    return english
                        ? "The wheel cannot be held. Use Run, or change the shortcut."
                        : "滚轮不能持续按住。可点击“执行所选宏”，或更换触发键。";
                case "hold-non-gamepad":
                    return english
                        ? "Runs these steps while you hold the shortcut. Release it to stop."
                        : "按住触发键时按顺序执行这些步骤，松开停止。";
                case "hold-action":
                    return english
                        ? "Runs these steps while you hold the shortcut. Release it to stop."
                        : "按住触发键时按顺序执行这些步骤，松开停止。";
                case "hold-delay":
                    return english
                        ? "Runs these steps while you hold the shortcut. Release it to stop."
                        : "按住触发键时按顺序执行这些步骤，松开停止。";
                default:
                    return english ? "Runtime capability is determined from the current macro definition." : "运行资格由宏当前的实际配置决定。";
            }
        }
    }

    internal static class MacroRuntimeClassifier
    {
        public static bool IsHoldTriggerSupported(TriggerSpec trigger)
        {
            if (trigger == null) return false;
            return trigger.Kind != InputKind.WheelUp && trigger.Kind != InputKind.WheelDown;
        }

        public static MacroRuntimeCapability Classify(MacroDefinition macro)
        {
            MacroRuntimeCapability result = new MacroRuntimeCapability();
            if (macro == null)
            {
                result.Category = MacroRuntimeCategory.Invalid;
                result.CanStart = false;
                result.CanStartFromConfiguredTrigger = false;
                result.ReasonCode = "no-macro";
                return result;
            }
            if (macro.Steps == null || macro.Steps.Count == 0)
            {
                result.Category = MacroRuntimeCategory.Invalid;
                result.CanStart = false;
                result.CanStartFromConfiguredTrigger = false;
                result.ReasonCode = "no-steps";
                return result;
            }

            if (!ActivatorModes.IsHoldLifecycle(macro))
            {
                result.Category = MacroRuntimeCategory.ConcurrentTimedMacro;
                result.CanStart = true;
                result.CanStartFromConfiguredTrigger = macro.Trigger != null;
                result.ReasonCode = "timed";
                return result;
            }

            bool holdTriggerSupported = IsHoldTriggerSupported(macro.Trigger);
            result.CanStart = true;
            result.CanStartFromConfiguredTrigger = holdTriggerSupported;

            // Trigger eligibility is orthogonal to manual/UI execution. A wheel-triggered Hold can
            // still be run manually, but it cannot be driven by a physical Hold trigger because a
            // wheel direction has no persistent down state. Check this before finite/infinite shape
            // so UI classification and physical trigger dispatch can never disagree.
            if (!holdTriggerSupported)
            {
                result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                result.ReasonCode = "hold-trigger";
                return result;
            }
            if (!macro.Infinite)
            {
                result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                result.ReasonCode = "hold-finite";
                return result;
            }

            foreach (MacroStep step in macro.Steps)
            {
                if (step == null || step.Kind != InputKind.Gamepad)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.ReasonCode = "hold-non-gamepad";
                    return result;
                }
                if (step.Action != MacroAction.Down)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.ReasonCode = "hold-action";
                    return result;
                }
                if (step.DelayMs != 0 || step.RandomDelay)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.ReasonCode = "hold-delay";
                    return result;
                }
            }

            result.Category = MacroRuntimeCategory.ParallelHeldMapping;
            result.ReasonCode = "parallel-held";
            return result;
        }
    }

    internal sealed class MacroRunRuntime
    {
        internal bool NativeUiGuard;
        public long RunId;
        public string SourceId = "";
        public MacroDefinition OwnerMacro;
        public MacroDefinition Snapshot;
        public MacroRuntimeCapability Capability;
        public MacroWorkItem Work;
        public ManualResetEventSlim Stop;
        public AutoResetEvent StepGate;
        public bool HoldControlled;
        public TriggerSpec HoldTrigger;
        public long ReleaseProbeSince;
        public bool SingleStep;
        public bool SingleStepWaiting;
        public string Phase = "starting";
        public string StopReason = "running";
        public int Iteration;
        public int StepIndex;
        public int StepCount;
        public readonly Dictionary<string, InputSpec> HeldInputs = new Dictionary<string, InputSpec>();
        public DateTime StartedUtc = DateTime.UtcNow;
    }
}
