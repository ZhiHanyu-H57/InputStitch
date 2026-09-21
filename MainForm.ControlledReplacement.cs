using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Controller takeover (experimental controlled replacement) coordination and its device-selection dialog
    // entry. Extracted from InputStitch.cs as the third step of the incremental MainForm decomposition
    // (architecture-review finding F4); behaviour is unchanged.
    public partial class MainForm
    {
        private void ShowControllerTakeoverDialog()
        {
            if (deviceHidingBackend == null || controlledReplacement == null)
            {
                LocalizedMessageBox.Show(this,
                    Localizer.IsEnglish ? "Controller takeover is unavailable in this host." : "当前环境不能使用手柄接管。",
                    AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (ControlledReplacementDialog dialog = new ControlledReplacementDialog(
                    deviceHidingBackend,
                    delegate { return GamepadOutput.OwnXboxUserIndex; },
                    delegate { return GamepadOutput.IsConnected; },
                    delegate { return GamepadOutput.ConnectedType; },
                    delegate { return config.GamepadRouterEnabled && gamepadRouter != null && gamepadRouter.Enabled && GamepadOutput.OwnXboxUserIndex >= 0 &&
                        (routerSourcePolicyEvaluator == null || routerSourcePolicyEvaluator.IsAllVisible); },
                    delegate { return controlledReplacement != null && controlledReplacement.Active; },
                    BeginControlledReplacement,
                    StopControlledReplacement))
                {
                    dialog.ShowDialog(this);
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                UpdateUiSafetyPauseState();
            }
        }

        private bool ValidateControlledReplacementSourceHealth()
        {
            if (gamepadInput == null || replacementExpectedControllerSlots == null || replacementExpectedControllerSlots.Length == 0) return false;
            // Give the filter/PNP stack a short settle window, then require every XInput source that
            // InputStitch was routing before hiding to remain visible to this whitelisted process.
            Thread.Sleep(350);
            gamepadInput.Poll();
            int[] connected = gamepadInput.ConnectedUserIndices();
            HashSet<int> visible = new HashSet<int>(connected);
            foreach (int expected in replacementExpectedControllerSlots)
                if (!visible.Contains(expected)) return false;
            return true;
        }

        private bool ValidateControlledReplacementRuntimeSourceHealth()
        {
            if (gamepadInput == null || replacementExpectedControllerSlots == null || replacementExpectedControllerSlots.Length == 0) return false;
            // Ongoing health checks run on a background timer. Do not call Poll here because the
            // 10 ms UI timer owns edge generation; read only its latest four-slot snapshot.
            int[] connected = gamepadInput.ConnectedUserIndices();
            HashSet<int> visible = new HashSet<int>(connected);
            foreach (int expected in replacementExpectedControllerSlots)
                if (!visible.Contains(expected)) return false;
            return true;
        }

        private void HandleControlledReplacementHealthFailure(ControlledReplacementHealthSnapshot snapshot)
        {
            string reason = snapshot == null ? "unknown takeover health failure" : snapshot.Reason;
            controlledReplacementHealthStatus = "unhealthy: " + reason;
            try { AppLog.Write("Controlled replacement health failure: " + reason); } catch { }
            if (IsDisposed || Disposing) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (IsDisposed || Disposing || controlledReplacement == null || !controlledReplacement.Active) return;
                    if (controlledReplacementHealthMonitor != null) controlledReplacementHealthMonitor.Stop();
                    runtimeTrace.Add("takeover-health-failure", reason);
                    ControlledReplacementResult restored = StopControlledReplacement();

                    // Once original devices are restored, continuing to mirror them would produce
                    // original + routed input at the same time. Disable Router as part of fail-safe
                    // disengage. If restoration itself failed, the recovery journal remains intact.
                    try { if (gamepadRouter != null) gamepadRouter.Stop("takeover-health-failure"); } catch { }
                    if (config != null && config.GamepadRouterEnabled)
                    {
                        config.GamepadRouterEnabled = false;
                        if (gamepadRouter != null) gamepadRouter.Configure(false);
                        SaveConfig();
                    }
                    try { outputOwnership.ClearAll("takeover-health-failure"); } catch { }
                    try { GamepadOutput.NeutralizeAll(); } catch { }

                    string message = Localizer.IsEnglish
                        ? "Controller takeover stopped automatically because its health check failed: " + reason
                        : "手柄接管已因运行状态异常自动停止并尝试恢复原手柄：" + reason;
                    if (restored != null && restored.State == ControlledReplacementState.Failed)
                        message += (Localizer.IsEnglish ? " Restore warning: " : " 恢复警告：") + restored.Message;
                    SetStatusSafe(message);
                    if (restored != null && restored.State == ControlledReplacementState.Failed)
                        LocalizedMessageBox.Show(this, message, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
            catch (Exception ex) { try { AppLog.Write("Could not dispatch takeover health recovery", ex); } catch { } }
        }

        private bool PrepareControlledTakeoverForHiding()
        {
            replacementExpectedControllerSlots = new int[0];
            if (!config.GamepadRouterEnabled || !EnsureGamepadRouterReady(false)) return false;
            if (routerSourcePolicyEvaluator != null && !routerSourcePolicyEvaluator.IsAllVisible) return false;
            if (GamepadOutput.OwnXboxUserIndex != 0 || gamepadInput == null || gamepadRouter == null) return false;

            gamepadInput.Poll();
            gamepadRouter.Tick(gamepadInput, GamepadOutput.OwnXboxUserIndex);
            gamepadInput.Poll();
            replacementExpectedControllerSlots = gamepadInput.ConnectedUserIndices();
            return GamepadOutput.OwnXboxUserIndex == 0 && replacementExpectedControllerSlots.Length > 0;
        }

        private void ResumeGamepadRouterAfterTakeoverFailure()
        {
            replacementExpectedControllerSlots = new int[0];
            if (!config.GamepadRouterEnabled) return;
            try
            {
                EnsureGamepadRouterReady(false);
                if (gamepadInput != null) gamepadInput.Poll();
                if (gamepadRouter != null && gamepadInput != null)
                    gamepadRouter.Tick(gamepadInput, GamepadOutput.OwnXboxUserIndex);
            }
            catch { }
        }

        private ControlledReplacementResult BeginControlledReplacement(IEnumerable<GamingDeviceDescriptor> selectedDevices)
        {
            if (controlledReplacement == null)
                return new ControlledReplacementResult { State = ControlledReplacementState.BackendUnavailable, Message = "Controlled replacement is unavailable." };
            if (!config.GamepadRouterEnabled || !EnsureGamepadRouterReady(false))
                return new ControlledReplacementResult { State = ControlledReplacementState.RouterNotReady, Message = "Controller merging must be enabled and connected first." };
            if (routerSourcePolicyEvaluator != null && !routerSourcePolicyEvaluator.IsAllVisible)
                return new ControlledReplacementResult { State = ControlledReplacementState.RouterNotReady, Message = "Controller takeover currently requires Router source policy 'all visible'. Stop takeover/source filtering before starting controlled replacement." };

            List<GamingDeviceDescriptor> selected = new List<GamingDeviceDescriptor>();
            HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selectedDevices != null)
            {
                foreach (GamingDeviceDescriptor device in selectedDevices)
                {
                    if (device == null || string.IsNullOrWhiteSpace(device.DeviceInstancePath)) continue;
                    if (!seenPaths.Add(device.DeviceInstancePath)) continue;
                    selected.Add(device);
                }
            }
            if (selected.Count == 0)
                return new ControlledReplacementResult { State = ControlledReplacementState.NoSourceDevices, Message = "No controller device was selected for takeover." };

            if (gamepadInput != null) gamepadInput.Poll();
            replacementExpectedControllerSlots = gamepadInput == null ? new int[0] : gamepadInput.ConnectedUserIndices();
            if (replacementExpectedControllerSlots.Length == 0)
                return new ControlledReplacementResult { State = ControlledReplacementState.NoSourceDevices, Message = "No external XInput controller is currently visible to InputStitch." };

            if (slotAcquisition == null || controlledTakeoverPipeline == null)
            {
                replacementExpectedControllerSlots = new int[0];
                return new ControlledReplacementResult { State = ControlledReplacementState.SlotAcquisitionFailed, Message = "Slot-acquisition service is unavailable." };
            }

            // A non-zero starting slot necessarily needs a temporary PnP re-enumeration. Refuse
            // before stopping any active runtime if elevation is unavailable. When already slot 0,
            // no admin rights are needed for the self-identity disconnect/reconnect verification.
            if (GamepadOutput.OwnXboxUserIndex != 0 && pnpDeviceControl != null && !pnpDeviceControl.IsElevated)
            {
                replacementExpectedControllerSlots = new int[0];
                return new ControlledReplacementResult { State = ControlledReplacementState.ElevationRequired, Message = "Administrator permission is required to safely acquire XInput slot 0. No original controller was hidden." };
            }

            // Slot acquisition always disconnects InputStitch's own virtual controller once so its
            // HidHide identity can be excluded without guessing by VID/PID. Stop every managed output
            // first, even when we already own slot 0.
            if (!StopRuntimeForConfigChange())
            {
                replacementExpectedControllerSlots = new int[0];
                return new ControlledReplacementResult { State = ControlledReplacementState.SlotAcquisitionFailed, Message = "Active macro runs could not be stopped safely before slot acquisition." };
            }
            if (idleGamepad != null) idleGamepad.SetBusy(true);
            if (gamepadRouter != null) gamepadRouter.Stop("slot-acquisition");
            outputOwnership.ClearAll("slot-acquisition");
            GamepadOutput.NeutralizeAll();

            ControlledReplacementResult result = controlledTakeoverPipeline.Begin(selected, 0);
            if (result != null && result.State == ControlledReplacementState.Active)
            {
                controlledReplacementHealthStatus = "healthy: monitoring-started";
                if (controlledReplacementHealthMonitor != null) controlledReplacementHealthMonitor.Start();
                runtimeTrace.Add("takeover-health-monitor", "started");
            }
            else
            {
                // The slot and hiding coordinators have already performed their own local rollback.
                // Restore ordinary Router operation only after those rollback boundaries complete.
                ResumeGamepadRouterAfterTakeoverFailure();
            }
            return result;
        }

        private ControlledReplacementResult StopControlledReplacement()
        {
            if (controlledReplacementHealthMonitor != null) controlledReplacementHealthMonitor.Stop();
            controlledReplacementHealthStatus = "inactive";
            if (controlledReplacement == null)
                return new ControlledReplacementResult { State = ControlledReplacementState.Idle, Message = "Controlled replacement is already off." };
            ControlledReplacementResult result = controlledReplacement.Stop();
            replacementExpectedControllerSlots = new int[0];
            return result;
        }

        private bool NoteVirtualGamepadHotplugIfTargetRunning(bool reusedExistingConnection)
        {
            if (reusedExistingConnection || config == null || string.IsNullOrWhiteSpace(config.TargetProcessName)) return false;
            string processName = Path.GetFileNameWithoutExtension(config.TargetProcessName.Trim());
            if (processName.Length == 0) return false;
            Process[] matches = new Process[0];
            try
            {
                matches = Process.GetProcessesByName(processName);
                if (matches == null || matches.Length == 0) return false;
                virtualGamepadHotplugStatus = "virtual controller connected/reconnected while target process was already running: " + processName;
                try { runtimeTrace.Add("virtual-gamepad-hotplug", virtualGamepadHotplugStatus); } catch { }
                try { AppLog.Write("Virtual gamepad hot-plug warning: " + virtualGamepadHotplugStatus); } catch { }
                return true;
            }
            catch { return false; }
            finally
            {
                if (matches != null)
                    foreach (Process process in matches)
                        if (process != null) try { process.Dispose(); } catch { }
            }
        }

        private string VirtualGamepadHotplugWarningText()
        {
            return Localizer.IsEnglish
                ? " The virtual controller was just connected while the configured target was already running. Some games do not reacquire a hot-plugged XInput device; restart the game if controller input is ignored."
                : " 虚拟手柄是在目标程序已经运行时刚刚连接/重连的；部分游戏不会重新接管热插拔后的 XInput 设备。如果游戏收不到手柄输入，请重启游戏。";
        }

        private bool EnsureGamepadRouterReady(bool startup)
        {
            if (config == null || !config.GamepadRouterEnabled) return true;
            try
            {
                // XInput routing targets an Xbox-compatible virtual controller so it owns a real
                // XInput user slot that can be measured and excluded from the source scanner.
                bool reusedConnection = GamepadOutput.IsConnected && string.Equals(GamepadOutput.ConnectedType, VirtualGamepadTypes.Xbox360, StringComparison.OrdinalIgnoreCase);
                config.GamepadDeviceType = VirtualGamepadTypes.Xbox360;
                GamepadOutput.Configure(VirtualGamepadTypes.Xbox360);
                GamepadOutput.EnsureConnected(VirtualGamepadTypes.Xbox360);
                bool hotplugRisk = NoteVirtualGamepadHotplugIfTargetRunning(reusedConnection);
                if (gamepadRouter != null) gamepadRouter.Configure(true);
                int slot = GamepadOutput.OwnXboxUserIndex;
                string message;
                if (slot == 0)
                    message = Localizer.IsEnglish
                        ? "Controller merge is active on XInput 0. Other visible XInput controllers are routed into it."
                        : "手柄汇总已启用：InputStitch 虚拟手柄位于 XInput 0，其他可见 XInput 手柄会转发到这里。";
                else
                    message = (Localizer.IsEnglish ? "Controller merge is active on XInput " : "手柄汇总已启用，但 InputStitch 虚拟手柄位于 XInput ") +
                        slot.ToString() + (Localizer.IsEnglish
                            ? ". Games that only read XInput 0 may still ignore routed output."
                            : "。只读取 0 号槽位的游戏仍可能看不到汇总后的输出。");
                if (hotplugRisk) message += VirtualGamepadHotplugWarningText();
                if (!startup || slot != 0 || hotplugRisk) SetStatusSafe(message);
                return true;
            }
            catch (GamepadOutputException ex)
            {
                if (gamepadRouter != null) gamepadRouter.Stop("virtual-controller-unavailable");
                AppLog.Write("Controller router virtual gamepad connection failed", ex);
                SetStatusSafe((Localizer.IsEnglish ? "Controller merge unavailable: " : "手柄汇总不可用：") + ex.Message);
                if (!startup) GamepadDriverGuidance.Show(this, ex);
                return false;
            }
        }

    }
}
