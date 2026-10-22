using System;
using System.Windows.Forms;

namespace InputStitch
{
    public partial class MainForm
    {
        // Isolated integration tests supply physical edges/focus without SendInput.
        internal Func<TriggerSpec, bool> ActivatorPhysicalProbe = null;
        internal Func<bool> ActivatorOwnWindowProbe = null;

        private bool IsActivatorPhysicallyHeld(TriggerSpec trigger)
        {
            return ActivatorPhysicalProbe != null ? ActivatorPhysicalProbe(trigger) : IsPhysicalHoldTriggerSatisfied(trigger);
        }

        private bool IsActivatorOwnWindowForeground()
        {
            return ActivatorOwnWindowProbe != null ? ActivatorOwnWindowProbe() :
                NativeWindowFocus.IsCurrentProcessWindow(NativeWindowFocus.ForegroundWindow());
        }

        private bool IsActivatorStartBlocked()
        {
            return uiSafetyPauseRequested || pauseHotkeys || manualTriggerSuspend || captureMode != CaptureMode.None ||
                recordingActive || uiSafetyModalDepth > 0 || IsDisposed || Disposing;
        }

        private bool PreparedOutputReady(MacroDefinition snapshot)
        {
            return isolatedUiHost || !MacroUsesGamepad(snapshot) ||
                (GamepadOutput.IsConnected && string.Equals(GamepadOutput.ConnectedType,
                    VirtualGamepadTypes.Normalize(config.GamepadDeviceType), StringComparison.OrdinalIgnoreCase));
        }

        private bool IsActivatorMacroCurrent(MacroDefinition macro)
        {
            MacroConfig current = config;
            return current != null && current.Macros != null && current.Macros.Contains(macro);
        }

        private void DispatchPreparedActivator(ActivatorDecision decision)
        {
            // First-time virtual-device connection and inspection of our own editor
            // remain on the UI lane. Only a ready, external-target run starts here.
            if (IsActivatorOwnWindowForeground() || !PreparedOutputReady(decision.Snapshot))
            {
                try { BeginInvoke((MethodInvoker)delegate { AdmitPreparedActivator(decision, false); }); }
                catch { activatorEngine.Reset(decision.Macro); }
                return;
            }
            AdmitPreparedActivator(decision, true);
        }

        private void AdmitPreparedActivator(ActivatorDecision decision, bool background)
        {
            if (!background)
            {
                if (!activatorEngine.IsPreparedCurrent(decision) || !IsActivatorMacroCurrent(decision.Macro)) return;
                UpdateUiSafetyPauseState();
                // A driver prompt may pump messages. Never hold the admission lock
                // across device connection/dialogs; revalidate the ticket afterwards.
                if (!IsActivatorStartBlocked() && !isolatedUiHost && !EnsureGamepadReady(decision.Snapshot, false))
                { activatorEngine.Reset(decision.Macro); return; }
            }
            activatorEngine.TryDispatchPrepared(decision, delegate
            {
                if (background && (IsActivatorOwnWindowForeground() || !PreparedOutputReady(decision.Snapshot))) return;
                int deviceIndex = decision.TriggerEvent == null ? -1 : decision.TriggerEvent.DeviceIndex;
                if (IsActivatorStartBlocked() || !IsActivatorMacroCurrent(decision.Macro) || !decision.Macro.Enabled || !IsActivatorPhysicallyHeld(decision.Snapshot.Trigger) ||
                    !AreMacroConditionsSatisfied(decision.Snapshot, deviceIndex)) return;
                MacroRuntimeCapability capability = MacroRuntimeClassifier.Classify(decision.Snapshot);
                if (!capability.CanStart || IsMacroActuallyRunning(decision.Macro)) return;
                if (idleGamepad != null) idleGamepad.SetBusy(true);
                runtimeTrace.Add("activator-start", "reason=long-press; lane=" + (background ? "input" : "ui"));
                StartPreparedMacro(decision.Macro, decision.Snapshot, capability, 0, null, false, false, null, background);
            });
        }

        private bool WorkerUiPauseRequested(bool nativeUiGuard)
        {
            return uiSafetyPauseRequested || (nativeUiGuard && config.PauseMacroInRiskyUi && IsActivatorOwnWindowForeground());
        }
    }
}
