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
        public bool SupportsConcurrentExecution;
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
            if (!CanStart) return english ? "Cannot run" : "不可运行";
            if (SupportsConcurrentExecution) return english ? "Concurrent" : "可并发";
            return english ? "Exclusive" : "独占";
        }

        public string ReasonText(bool english)
        {
            switch (ReasonCode ?? "")
            {
                case "no-macro":
                    return english ? "No macro is selected." : "尚未选择宏。";
                case "no-steps":
                    return english ? "The macro has no execution steps." : "这个宏还没有执行步骤。";
                case "parallel-held":
                    return english
                        ? "Immediate gamepad-only Hold state. It can coexist with other Parallel Held Mappings and concurrent timed macros."
                        : "即时、仅手柄状态的 Hold 映射；可与其他并行按住映射和可并发时序宏共存。";
                case "timed":
                    return english
                        ? "Ordinary timed/toggle macro. It runs as an independent concurrent runtime source."
                        : "普通时序 / Toggle 宏；会作为独立运行 Source 进入多宏并发运行时。";
                case "hold-finite":
                    return english
                        ? "Finite Hold uses the concurrent Hold runtime instead of the state-only Parallel Held path."
                        : "有限次数 Hold 使用可并发 Hold 运行时，而不是纯状态型并行按住路径。";
                case "hold-trigger":
                    return english
                        ? "This Hold trigger is not eligible for the state-only Parallel Held path; UI/manual execution can still use the concurrent Hold runtime."
                        : "当前 Hold 触发器不符合纯状态型并行按住路径；界面/手动执行仍可进入可并发 Hold 运行时。";
                case "hold-non-gamepad":
                    return english
                        ? "This Hold contains keyboard or mouse output, so it runs as an independent concurrent Hold timeline."
                        : "这个 Hold 包含键盘或鼠标输出，因此作为独立的可并发 Hold 时间线运行。";
                case "hold-action":
                    return english
                        ? "This Hold contains Press/Up sequencing, so it runs as an independent concurrent Hold timeline."
                        : "这个 Hold 包含 Press / Up 时序，因此作为独立的可并发 Hold 时间线运行。";
                case "hold-delay":
                    return english
                        ? "This Hold contains non-zero or random delay, so it runs as an independent concurrent Hold timeline."
                        : "这个 Hold 包含非零或随机延迟，因此作为独立的可并发 Hold 时间线运行。";
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
            if (trigger.Ctrl || trigger.Shift || trigger.Alt || trigger.Win) return false;
            return trigger.Kind != InputKind.WheelUp && trigger.Kind != InputKind.WheelDown;
        }

        public static MacroRuntimeCapability Classify(MacroDefinition macro)
        {
            MacroRuntimeCapability result = new MacroRuntimeCapability();
            if (macro == null)
            {
                result.Category = MacroRuntimeCategory.Invalid;
                result.CanStart = false;
                result.SupportsConcurrentExecution = false;
                result.ReasonCode = "no-macro";
                return result;
            }
            if (macro.Steps == null || macro.Steps.Count == 0)
            {
                result.Category = MacroRuntimeCategory.Invalid;
                result.CanStart = false;
                result.SupportsConcurrentExecution = false;
                result.ReasonCode = "no-steps";
                return result;
            }

            if (macro.RunMode != TriggerRunMode.Hold)
            {
                result.Category = MacroRuntimeCategory.ConcurrentTimedMacro;
                result.CanStart = true;
                result.SupportsConcurrentExecution = true;
                result.ReasonCode = "timed";
                return result;
            }

            result.CanStart = true;
            if (!macro.Infinite)
            {
                result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                result.SupportsConcurrentExecution = true;
                result.ReasonCode = "hold-finite";
                return result;
            }
            if (!IsHoldTriggerSupported(macro.Trigger))
            {
                result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                result.SupportsConcurrentExecution = true;
                result.ReasonCode = "hold-trigger";
                return result;
            }

            foreach (MacroStep step in macro.Steps)
            {
                if (step == null || step.Kind != InputKind.Gamepad)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.SupportsConcurrentExecution = true;
                    result.ReasonCode = "hold-non-gamepad";
                    return result;
                }
                if (step.Action != MacroAction.Down)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.SupportsConcurrentExecution = true;
                    result.ReasonCode = "hold-action";
                    return result;
                }
                if (step.DelayMs != 0 || step.RandomDelay)
                {
                    result.Category = MacroRuntimeCategory.ConcurrentHoldMacro;
                    result.SupportsConcurrentExecution = true;
                    result.ReasonCode = "hold-delay";
                    return result;
                }
            }

            result.Category = MacroRuntimeCategory.ParallelHeldMapping;
            result.SupportsConcurrentExecution = true;
            result.ReasonCode = "parallel-held";
            return result;
        }
    }

    internal sealed class MacroRunRuntime
    {
        public long RunId;
        public string SourceId = "";
        public MacroDefinition OwnerMacro;
        public MacroDefinition Snapshot;
        public MacroRuntimeCapability Capability;
        public Thread Thread;
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
        public string HeldText = "无";
        public readonly Dictionary<string, InputSpec> HeldInputs = new Dictionary<string, InputSpec>();
        public DateTime StartedUtc = DateTime.UtcNow;
    }
}
