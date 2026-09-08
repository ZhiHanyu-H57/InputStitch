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
        ExclusiveHold = 3
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
            if (Category == MacroRuntimeCategory.ExclusiveHold)
                return english ? "Advanced / Exclusive Hold" : "高级 / 独占 Hold";
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
                        ? "Hold mode is not infinite, so this is treated as an advanced exclusive Hold macro."
                        : "Hold 模式未启用无限运行，因此按高级独占 Hold 处理。";
                case "hold-trigger":
                    return english
                        ? "The Hold trigger is not supported by the parallel Held path."
                        : "当前 Hold 触发器不符合并行按住映射要求。";
                case "hold-non-gamepad":
                    return english
                        ? "The Hold macro contains keyboard or mouse output, so it remains exclusive."
                        : "这个 Hold 宏包含键盘或鼠标输出，因此保持独占运行。";
                case "hold-action":
                    return english
                        ? "The Hold macro contains Press/Up sequencing, so it remains exclusive."
                        : "这个 Hold 宏包含 Press / Up 时序，因此保持独占运行。";
                case "hold-delay":
                    return english
                        ? "The Hold macro contains a non-zero or random delay, so it remains exclusive."
                        : "这个 Hold 宏包含非零或随机延迟，因此保持独占运行。";
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
                result.Category = MacroRuntimeCategory.ExclusiveHold;
                result.SupportsConcurrentExecution = false;
                result.ReasonCode = "hold-finite";
                return result;
            }
            if (!IsHoldTriggerSupported(macro.Trigger))
            {
                result.Category = MacroRuntimeCategory.ExclusiveHold;
                result.SupportsConcurrentExecution = false;
                result.ReasonCode = "hold-trigger";
                return result;
            }

            foreach (MacroStep step in macro.Steps)
            {
                if (step == null || step.Kind != InputKind.Gamepad)
                {
                    result.Category = MacroRuntimeCategory.ExclusiveHold;
                    result.SupportsConcurrentExecution = false;
                    result.ReasonCode = "hold-non-gamepad";
                    return result;
                }
                if (step.Action != MacroAction.Down)
                {
                    result.Category = MacroRuntimeCategory.ExclusiveHold;
                    result.SupportsConcurrentExecution = false;
                    result.ReasonCode = "hold-action";
                    return result;
                }
                if (step.DelayMs != 0 || step.RandomDelay)
                {
                    result.Category = MacroRuntimeCategory.ExclusiveHold;
                    result.SupportsConcurrentExecution = false;
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
