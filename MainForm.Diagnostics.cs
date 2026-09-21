using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Runtime Observation and Diagnostics text builders. Extracted from InputStitch.cs as the second step of
    // the incremental MainForm decomposition (architecture-review finding F4); behaviour is unchanged.
    public partial class MainForm
    {
        private string BuildRuntimeObservationText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AppInfo.ProductName + " " + AppInfo.Version);
            sb.AppendLine(Localizer.IsEnglish ? "Runtime observation" : "运行观察");
            sb.AppendLine(new string('-', 56));

            MappingLayerDefinition activeLayer = FindMappingLayer(activeMappingLayerId);
            string layerText = activeMappingLayerId.Length == 0
                ? (Localizer.IsEnglish ? "Base only" : "仅基础层")
                : (Localizer.IsEnglish ? "Base + " : "基础层 + ") + (activeLayer == null ? activeMappingLayerId : activeLayer.Name);
            if (IsMomentaryLayerHoldActive(activeMappingLayerId))
                layerText += Localizer.IsEnglish ? " (momentary hold)" : "（按住临时生效）";
            else if (IsActiveLayerContextDriven())
                layerText += Localizer.IsEnglish ? " (context)" : "（上下文自动）";
            sb.AppendLine((Localizer.IsEnglish ? "Layer: " : "映射层：") + layerText);
            sb.AppendLine((Localizer.IsEnglish ? "Virtual Xbox slot: " : "虚拟 Xbox 槽位：") + ObservedVirtualXboxSlot().ToString());
            sb.AppendLine((Localizer.IsEnglish ? "Device identity: " : "设备身份：") +
                (deviceIdentityService == null ? (Localizer.IsEnglish ? "unavailable" : "不可用") : deviceIdentityService.DiagnosticsSummary()));
            sb.AppendLine((Localizer.IsEnglish ? "Keyboard fallback: " : "键盘兜底：") +
                (rawKeyboardFallback != null && rawKeyboardFallback.Registered ? (Localizer.IsEnglish ? "ready" : "就绪") : (Localizer.IsEnglish ? "not ready" : "未就绪")) +
                "; raw=" + (rawKeyboardFallback == null ? "0" : rawKeyboardFallback.EventCount.ToString()) +
                "; fallback=" + Volatile.Read(ref rawKeyboardFallbackEdgeCount).ToString() +
                "; suppression-missed=" + Volatile.Read(ref rawKeyboardSuppressionUnavailableCount).ToString() +
                "; hook-recoveries=" + Volatile.Read(ref keyboardHookRecoveryCount).ToString());
            sb.AppendLine((Localizer.IsEnglish ? "Input cadence: " : "输入节拍：") +
                gamepadTickStats.Describe("gamepad") + "; " + foregroundTickStats.Describe("foreground"));
            sb.AppendLine((Localizer.IsEnglish ? "Controller merge: " : "手柄汇总：") +
                (gamepadRouter == null ? (Localizer.IsEnglish ? "unavailable" : "不可用") :
                    (gamepadRouter.Enabled ? gamepadRouter.LastStatus + "; routed=" + gamepadRouter.RoutedControllerCount.ToString() +
                        "; policy=" + gamepadRouter.PolicyStatus : (Localizer.IsEnglish ? "off" : "关闭"))));
            string takeoverState = controlledReplacement == null ? "unavailable" : controlledReplacement.State.ToString();
            sb.AppendLine((Localizer.IsEnglish ? "Controller takeover: " : "手柄接管：") + takeoverState);
            int configuredAdvancedRules = 0;
            if (config != null && config.Macros != null)
                foreach (MacroDefinition macro in config.Macros)
                    if (macro != null && (ActivatorModes.UsesAdvancedPolicy(macro) || (macro.Conditions != null && !macro.Conditions.IsEmpty))) configuredAdvancedRules++;
            sb.AppendLine((Localizer.IsEnglish ? "Activation rules: " : "激活规则：") +
                "configured=" + configuredAdvancedRules.ToString() + "; tracked=" + activatorEngine.TrackedCount.ToString());
            if (controlledReplacementHealthMonitor != null)
            {
                ControlledReplacementHealthSnapshot health = controlledReplacementHealthMonitor.LastSnapshot;
                sb.AppendLine((Localizer.IsEnglish ? "Takeover health: " : "接管健康监控：") +
                    (controlledReplacementHealthMonitor.Monitoring ? (Localizer.IsEnglish ? "monitoring" : "监控中") : (Localizer.IsEnglish ? "inactive" : "未运行")) +
                    "; " + (health == null ? controlledReplacementHealthStatus : (health.Healthy ? "OK: " : "FAIL: ") + health.Reason) +
                    "; failures=" + controlledReplacementHealthMonitor.ConsecutiveFailures.ToString());
            }
            if (replacementExpectedControllerSlots != null && replacementExpectedControllerSlots.Length != 0)
                sb.AppendLine((Localizer.IsEnglish ? "Expected routed XInput sources: " : "预期被路由的 XInput 来源：") + string.Join(",", replacementExpectedControllerSlots));
            sb.AppendLine();

            List<string> runLines = new List<string>();
            List<string> heldMappings = new List<string>();
            string stopReason;
            lock (runLock)
            {
                List<MacroRunRuntime> runs = new List<MacroRunRuntime>(activeMacroRuns.Values);
                runs.RemoveAll(delegate(MacroRunRuntime x) { return x == null; });
                runs.Sort(delegate(MacroRunRuntime a, MacroRunRuntime b) { return a.RunId.CompareTo(b.RunId); });
                foreach (MacroRunRuntime runtime in runs)
                {
                    string name = runtime.OwnerMacro == null ? "?" : runtime.OwnerMacro.Name;
                    MacroRuntimeCapability capability = runtime.Capability ?? MacroRuntimeClassifier.Classify(runtime.OwnerMacro);
                    string line = "RunId=" + runtime.RunId.ToString() + " | " + name + " | " + runtime.SourceId +
                        " | " + capability.CategoryText(Localizer.IsEnglish) +
                        " | phase=" + runtime.Phase +
                        " | iter=" + runtime.Iteration.ToString() +
                        " | step=" + runtime.StepIndex.ToString() + "/" + runtime.StepCount.ToString() +
                        (runtime.SingleStep ? (runtime.SingleStepWaiting ? " | SINGLE-STEP WAIT" : " | SINGLE-STEP") : "") +
                        " | held=" + Localizer.T(runtime.HeldText);
                    runLines.Add(line);
                }
                stopReason = lastRunStopReason;
                foreach (KeyValuePair<MacroDefinition, HeldMappingRuntime> pair in activeParallelHeldMappings)
                {
                    HeldMappingRuntime runtime = pair.Value;
                    if (runtime == null) continue;
                    string name = runtime.Macro == null ? "?" : runtime.Macro.Name;
                    heldMappings.Add(name + " | " + InputNames.FormatTrigger(runtime.Trigger) + " | " + runtime.SourceId);
                }
            }

            sb.AppendLine((Localizer.IsEnglish ? "Macro runs: " : "宏运行实例：") + runLines.Count.ToString());
            if (runLines.Count == 0) sb.AppendLine("  " + (Localizer.IsEnglish ? "None" : "无"));
            else foreach (string line in runLines) sb.AppendLine("  - " + line);
            sb.AppendLine("  " + (Localizer.IsEnglish ? "Last stop reason: " : "上次停止原因：") + stopReason);

            sb.AppendLine();
            sb.AppendLine((Localizer.IsEnglish ? "Parallel Held Mappings: " : "并行按住映射：") + heldMappings.Count.ToString());
            if (heldMappings.Count == 0) sb.AppendLine("  " + (Localizer.IsEnglish ? "None" : "无"));
            else foreach (string item in heldMappings) sb.AppendLine("  - " + item);

            OutputOwnershipSnapshot ownership = outputOwnership.Snapshot();
            sb.AppendLine();
            sb.AppendLine((Localizer.IsEnglish ? "Active output sources: " : "活动输出来源：") + ownership.Sources.Count.ToString() +
                "  Version=" + ownership.Version.ToString() + "  Reason=" + ownership.LastReason);
            foreach (OutputSourceSnapshot source in ownership.Sources)
            {
                sb.AppendLine("  [" + source.SourceId + "]" + (source.Suspended ? " SUSPENDED" : ""));
                foreach (InputSpec input in source.Contributions)
                    sb.AppendLine("    -> " + InputNames.FormatInput(input));
            }

            sb.AppendLine();
            sb.AppendLine((Localizer.IsEnglish ? "Merged output: " : "合并后输出：") + ownership.Merged.Count.ToString());
            if (ownership.Merged.Count == 0) sb.AppendLine("  " + (Localizer.IsEnglish ? "Neutral / released" : "全部中立 / 已释放"));
            else foreach (InputSpec input in ownership.Merged) sb.AppendLine("  = " + InputNames.FormatInput(input));
            return sb.ToString();
        }

        // Provider status for diagnostics: which input lanes are active and whether the optional SDL3 lane is
        // available on this machine (development plan H2.3).
        private string DescribeInputProviders()
        {
            if (gamepadInput == null) return "none";
            CompositeInputProvider composite = gamepadInput as CompositeInputProvider;
            if (composite == null) return gamepadInput.ProviderName + "=active";
            List<string> parts = new List<string>();
            foreach (IGamepadInputProvider provider in composite.Providers)
            {
                Sdl3InputProvider sdl3 = provider as Sdl3InputProvider;
                if (sdl3 != null)
                {
                    parts.Add("sdl3=" + (sdl3.Available ? "ready" : "unavailable") + "(" + sdl3.StatusSummary + ")");
                    continue;
                }
                parts.Add(provider.ProviderName + "=active");
            }
            return string.Join("; ", parts.ToArray());
        }

        private string BuildDiagnosticsText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AppInfo.ProductName + " " + AppInfo.Version);
            sb.AppendLine("ConfigFormat: " + (config == null ? "?" : config.FormatVersion));
            sb.AppendLine("ConfigPath: " + configPath);
            sb.AppendLine("DeviceRegistryPath: " + AppPaths.DeviceRegistry);
            sb.AppendLine("LogPath: " + AppLog.LogPath);
            sb.AppendLine(runtimeTrace.Snapshot());
            sb.AppendLine("Hooks: " + (hooks == null ? Localizer.T("未安装") : Localizer.T("已安装")));
            sb.AppendLine("RawKeyboardFallback: " + (rawKeyboardFallback != null && rawKeyboardFallback.Registered ? "ready" : "not-ready") +
                "; observed=" + (rawKeyboardFallback == null ? "0" : rawKeyboardFallback.EventCount.ToString()) +
                "; fallbackEdges=" + Volatile.Read(ref rawKeyboardFallbackEdgeCount).ToString() +
                "; suppressionUnavailable=" + Volatile.Read(ref rawKeyboardSuppressionUnavailableCount).ToString() +
                "; hookRecoveries=" + Volatile.Read(ref keyboardHookRecoveryCount).ToString());
            sb.AppendLine("ScanCodeInput: " + (config != null && config.UseScanCodeInput ? Localizer.T("开启") : (Localizer.IsEnglish ? "Off" : "关闭")));
            sb.AppendLine("VirtualGamepadType: " + (config == null ? "?" : config.GamepadDeviceType));
            sb.AppendLine("VirtualGamepadConnected: " + (ObservedVirtualGamepadConnected() ? ObservedVirtualGamepadType() : Localizer.T("否")));
            sb.AppendLine("VirtualXboxSlot: " + ObservedVirtualXboxSlot().ToString());
            sb.AppendLine("DeviceIdentity: " + (deviceIdentityService == null ? "unavailable" : deviceIdentityService.DiagnosticsSummary()));
            sb.AppendLine("VirtualGamepadHotplug: " + virtualGamepadHotplugStatus);
            sb.AppendLine("GamepadRouter: " + (gamepadRouter == null ? "unavailable" :
                (gamepadRouter.Enabled ? gamepadRouter.LastStatus + "; routed=" + gamepadRouter.RoutedControllerCount.ToString() +
                    "; policy=" + gamepadRouter.PolicyStatus : "disabled")));
            sb.AppendLine("ControlledReplacement: " + (controlledReplacement == null ? "unavailable" : controlledReplacement.State.ToString() + "; " + controlledReplacement.LastMessage));
            if (controlledReplacementHealthMonitor != null)
            {
                ControlledReplacementHealthSnapshot health = controlledReplacementHealthMonitor.LastSnapshot;
                sb.AppendLine("TakeoverHealthMonitoring: " + (controlledReplacementHealthMonitor.Monitoring ? "active" : "inactive") +
                    "; consecutiveFailures=" + controlledReplacementHealthMonitor.ConsecutiveFailures.ToString() +
                    "; last=" + (health == null ? controlledReplacementHealthStatus : (health.Healthy ? "OK: " : "FAIL: ") + health.Reason));
            }
            sb.AppendLine("ExpectedRoutedXInputSources: " + (replacementExpectedControllerSlots == null || replacementExpectedControllerSlots.Length == 0 ? "none" : string.Join(",", replacementExpectedControllerSlots)));
            sb.AppendLine("ReplacementRecoveryWarning: " + (string.IsNullOrWhiteSpace(replacementRecoveryWarning) ? "none" : replacementRecoveryWarning));
            sb.AppendLine("SlotAcquisitionRecoveryWarning: " + (string.IsNullOrWhiteSpace(slotAcquisitionRecoveryWarning) ? "none" : slotAcquisitionRecoveryWarning));
            MappingLayerDefinition diagnosticActiveLayer = FindMappingLayer(activeMappingLayerId);
            MappingLayerDefinition diagnosticContextLayer = FindMatchingContextLayer_NoLock();
            sb.AppendLine("ActiveMappingLayer: " + (activeMappingLayerId.Length == 0 ? "Base only" : "Base + " + (diagnosticActiveLayer == null ? activeMappingLayerId : diagnosticActiveLayer.Name)) +
                "; MomentaryHold=" + IsMomentaryLayerHoldActive(activeMappingLayerId).ToString() +
                "; ContextLayer=" + (diagnosticContextLayer == null ? "none" : diagnosticContextLayer.Id) +
                "; PersistentLayer=" + (persistentMappingLayerId.Length == 0 ? "base-only" : persistentMappingLayerId) +
                "; MomentaryHolds=" + MomentaryLayerHoldCount().ToString());
            sb.AppendLine("MappingLayerCount: " + (config == null || config.MappingLayers == null ? "0" : config.MappingLayers.Count.ToString()));
            if (config != null && config.MappingLayers != null)
            {
                foreach (MappingLayerDefinition layer in config.MappingLayers)
                {
                    if (layer == null) continue;
                    sb.AppendLine("Layer[" + layer.Id + "]: " + (layer.Name ?? "") +
                        "; Base=" + layer.IsBase.ToString() +
                        "; Mode=" + (layer.MomentarySwitch ? "hold" : "toggle") +
                        "; Context=" + DescribeLayerContext(layer) +
                        "; Switch=" + (layer.SwitchTrigger == null ? "none" : InputNames.FormatTrigger(layer.SwitchTrigger)));
                }
            }
            sb.AppendLine("InputProviders: " + DescribeInputProviders());
            sb.AppendLine(gamepadTickStats.Describe("GamepadInputTick"));
            sb.AppendLine(foregroundTickStats.Describe("ForegroundTick"));
            sb.AppendLine("ActivatorTrackedStates: " + activatorEngine.TrackedCount.ToString());
            if (config != null && config.Macros != null)
            {
                for (int macroIndex = 0; macroIndex < config.Macros.Count; macroIndex++)
                {
                    MacroDefinition macro = config.Macros[macroIndex];
                    if (macro == null || (!ActivatorModes.UsesAdvancedPolicy(macro) && (macro.Conditions == null || macro.Conditions.IsEmpty))) continue;
                    sb.AppendLine("ActivationRule[" + macroIndex.ToString() + "]: " + DescribeActivationRule(macro));
                }
            }
            sb.AppendLine("PanicTrigger: " + InputNames.FormatTrigger(config == null ? null : config.PanicTrigger));
            sb.AppendLine("GlobalTriggersSuspended: " + (manualTriggerSuspend ? Localizer.T("是") : Localizer.T("否")));
            sb.AppendLine("UIProtectionPause: " + (uiSafetyPauseRequested ? Localizer.T("是") + " - " + Localizer.Dynamic(uiSafetyPauseReason) : Localizer.T("否")));
            sb.AppendLine("PhysicalModifiers: " + (hooks == null ? Localizer.T("未知") : hooks.PhysicalModifierText));
            sb.AppendLine("PhysicalInputEvents: " + (hooks == null ? "0" : hooks.PhysicalEventCount.ToString()));
            sb.AppendLine("StaleInputStateRepairs: " + (hooks == null ? "0" : hooks.StaleStateRepairCount.ToString()));
            sb.AppendLine("AvoidedPhysicalShortcutConflicts: " + Volatile.Read(ref dangerousShortcutAvoidanceCount).ToString());
            sb.AppendLine("Recording: " + (recordingActive ? Localizer.T("是") : Localizer.T("否")));
            OutputOwnershipSnapshot ownership = outputOwnership.Snapshot();
            sb.AppendLine("OutputOwnershipSources: " + ownership.Sources.Count.ToString());
            sb.AppendLine("MergedOutputs: " + ownership.Merged.Count.ToString());
            sb.AppendLine("OwnershipLastReason: " + ownership.LastReason);
            sb.AppendLine("Target: " + (targetWindowBox == null ? "" : targetWindowBox.Text));
            sb.AppendLine("AutoProfileSwitch: " + (config != null && config.AutoSwitchProfiles ? Localizer.T("开启") : (Localizer.IsEnglish ? "Off" : "关闭")));
            sb.AppendLine("ActiveProfile: " + (string.IsNullOrWhiteSpace(activeProfilePath) ? Localizer.T("（当前 config.xml）") : activeProfilePath));
            lock (runLock)
            {
                sb.AppendLine("MacroRunCount: " + activeMacroRuns.Count.ToString());
                List<MacroRunRuntime> runs = new List<MacroRunRuntime>(activeMacroRuns.Values);
                runs.RemoveAll(delegate(MacroRunRuntime x) { return x == null; });
                runs.Sort(delegate(MacroRunRuntime a, MacroRunRuntime b) { return a.RunId.CompareTo(b.RunId); });
                foreach (MacroRunRuntime runtime in runs)
                {
                    string name = runtime.OwnerMacro == null ? Localizer.T("无") : runtime.OwnerMacro.Name;
                    sb.AppendLine("Run[" + runtime.RunId.ToString() + "]: " + name +
                        " Source=" + runtime.SourceId +
                        " Phase=" + runtime.Phase +
                        " Iteration=" + runtime.Iteration.ToString() +
                        " Step=" + runtime.StepIndex.ToString() + "/" + runtime.StepCount.ToString() +
                        " Held=" + Localizer.T(runtime.HeldText));
                }
            }
            sb.AppendLine("TriggerConflicts: " + GetTriggerConflictSummary());
            return sb.ToString();
        }

        private bool ObservedVirtualGamepadConnected()
        {
            if (isolatedUiHost) return false;
            try { return GamepadOutput.IsConnected; }
            catch { return false; }
        }

        private string ObservedVirtualGamepadType()
        {
            if (isolatedUiHost) return "";
            try { return GamepadOutput.ConnectedType; }
            catch { return ""; }
        }

        private int ObservedVirtualXboxSlot()
        {
            // Observation must never initialize or connect a virtual device. The isolated UI/test
            // host deliberately avoids the embedded ViGEm dependency, so report no slot there.
            if (isolatedUiHost) return -1;
            try { return GamepadOutput.OwnXboxUserIndex; }
            catch { return -1; }
        }
    }
}
