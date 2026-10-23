using System;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    public partial class MainForm
    {
        private TimerController timerController;
        private NetworkController networkController;
        private Button timerButton;
        private ToolStripMenuItem networkMenuItem, restoreNetworkMenuItem;
        private int networkOperationPending;
        private int networkRestorePending;
        private readonly object screenToolsGate = new object();
        private bool screenToolsDisposed;

        private void ApplyScreenTools()
        {
            if (timerController == null) timerController = new TimerController(this);
            timerController.Apply(config.Timer);
        }
        private void RefreshNetworkStateUi()
        {
            bool blocked = networkController != null && networkController.IsBlocked;
            if (networkMenuItem != null) networkMenuItem.Checked = blocked;
            if (restoreNetworkMenuItem != null) restoreNetworkMenuItem.Enabled = blocked || Volatile.Read(ref networkOperationPending) != 0;
        }
        private void DisposeScreenTools()
        {
            if (timerController != null) { timerController.Dispose(); timerController = null; }
            NetworkController network;
            lock (screenToolsGate) { screenToolsDisposed = true; network = networkController; networkController = null; }
            if (network != null) network.Dispose();
        }
        private static bool SameShortcut(TriggerSpec a, TriggerSpec b)
        {
            return a != null && b != null && a.Kind == b.Kind && a.VirtualKey == b.VirtualKey && a.Extended == b.Extended &&
                ModifierSafetyPolicy.GetTriggerModifierMask(a) == ModifierSafetyPolicy.GetTriggerModifierMask(b);
        }
        private string ToolShortcutConflict(TriggerSpec key, TriggerSpec[] ownKeys, bool includeCrosshair, bool includeTimer, bool includeNetwork)
        {
            if (key == null) return "";
            if (key.Kind != InputKind.Keyboard || key.VirtualKey <= 0 || ModifierSafetyPolicy.IsModifierVirtualKey(key.VirtualKey))
                return EditorDialogs.T("快捷键需包含一个普通键。", "Include a regular key in the shortcut.");
            InputEventInfo e = new InputEventInfo { Input = new InputSpec { Kind = key.Kind, VirtualKey = key.VirtualKey, Extended = key.Extended }, Ctrl = key.Ctrl, Shift = key.Shift, Alt = key.Alt, Win = key.Win };
            if (ModifierSafetyPolicy.TriggerRequiredModifiersMatch(config.PanicTrigger, e))
                return EditorDialogs.T("快捷键与紧急停止冲突。", "The shortcut conflicts with Emergency Stop.");
            if (config.Macros != null) foreach (MacroDefinition macro in config.Macros)
                if (macro != null && macro.Enabled && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(macro.Trigger, e))
                    return EditorDialogs.T("快捷键已用于宏：", "Shortcut used by macro: ") + macro.Name;
            if (config.MappingLayers != null) foreach (MappingLayerDefinition layer in config.MappingLayers)
                if (layer != null && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(layer.SwitchTrigger, e))
                    return EditorDialogs.T("快捷键已用于映射层：", "Shortcut used by layer: ") + layer.Name;
            if (ownKeys != null)
            {
                int matches = 0; foreach (TriggerSpec other in ownKeys) if (SameShortcut(key, other)) matches++;
                if (matches > 1) return EditorDialogs.T("多个控制使用了相同快捷键。", "Two controls use the same shortcut.");
            }
            if (includeCrosshair && config.Crosshair != null && config.Crosshair.Enabled && SameShortcut(key, config.Crosshair.Hotkey))
                return EditorDialogs.T("快捷键已用于准星。", "Shortcut used by crosshair.");
            if (includeTimer && config.Timer != null && config.Timer.Enabled)
                foreach (TriggerSpec other in new[] { config.Timer.StartPauseHotkey, config.Timer.ResetHotkey, config.Timer.VisibilityHotkey })
                    if (SameShortcut(key, other)) return EditorDialogs.T("快捷键已用于计时器。", "Shortcut used by timer.");
            if (includeNetwork && config.NetworkControl != null && config.NetworkControl.Enabled && SameShortcut(key, config.NetworkControl.Hotkey))
                return EditorDialogs.T("快捷键已用于网络开关。", "Shortcut used by network toggle.");
            return "";
        }
        private string TimerShortcutConflict(TimerOptions options)
        {
            if (!options.Enabled) return "";
            TriggerSpec[] keys = { options.StartPauseHotkey, options.ResetHotkey, options.VisibilityHotkey };
            foreach (TriggerSpec key in keys)
            { string error = ToolShortcutConflict(key, keys, true, false, true); if (error.Length > 0) return error; }
            return "";
        }
        private string NetworkShortcutConflict(NetworkControlOptions options)
        {
            if (!options.Enabled) return "";
            if (options.Hotkey == null) return EditorDialogs.T("请选择断开和恢复网络的快捷键。", "Choose a shortcut to block and restore traffic.");
            return ToolShortcutConflict(options.Hotkey, null, true, true, false);
        }
        private static bool MatchesToolShortcut(TriggerSpec key, InputEventInfo e)
        {
            return key != null && ModifierSafetyPolicy.GetTriggerModifierMask(key) == ModifierSafetyPolicy.GetEventModifierMask(e) && ModifierSafetyPolicy.TriggerTerminalMatches(key, e);
        }
        private bool HandleScreenToolShortcut(InputEventInfo e)
        {
            if (networkController != null && networkController.IsBlocked && config.NetworkControl != null && MatchesToolShortcut(config.NetworkControl.Hotkey, e))
            { QueueNetworkOperation(true); return true; }
            if (uiSafetyModalDepth > 0 || pauseHotkeys || uiSafetyPauseRequested) return false;
            TimerOptions timer = config.Timer;
            if (timer != null && timer.Enabled && TimerShortcutConflict(timer).Length == 0)
            {
                int command = MatchesToolShortcut(timer.StartPauseHotkey, e) ? 1 : MatchesToolShortcut(timer.ResetHotkey, e) ? 2 : MatchesToolShortcut(timer.VisibilityHotkey, e) ? 3 : 0;
                if (command != 0)
                {
                    try { BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || uiSafetyModalDepth > 0 || uiSafetyPauseRequested || pauseHotkeys || !config.Timer.Enabled) return;
                        if (timerController == null) ApplyScreenTools();
                        if (command == 1) timerController.StartPause();
                        else if (command == 2) timerController.Reset();
                        else
                        {
                            config.Timer.Visible = !config.Timer.Visible;
                            if (!SaveConfig()) config.Timer.Visible = !config.Timer.Visible;
                            timerController.Apply(config.Timer);
                        }
                    }); } catch (InvalidOperationException) { }
                    return true;
                }
            }
            NetworkControlOptions network = config.NetworkControl;
            if (network != null && network.Enabled && MatchesToolShortcut(network.Hotkey, e) && NetworkShortcutConflict(network).Length == 0)
            { QueueNetworkOperation(false); return true; }
            return false;
        }
        private void QueueNetworkOperation(bool restore)
        {
            if (restore && networkController == null && Volatile.Read(ref networkOperationPending) == 0) return;
            if (restore) Interlocked.Exchange(ref networkRestorePending, 1);
            if (Interlocked.CompareExchange(ref networkOperationPending, 1, 0) != 0) return;
            NetworkControlOptions options = config.NetworkControl.Clone();
            // UAC and WFP IPC never run on the input hook or the UI thread.
            ThreadPool.QueueUserWorkItem(delegate
            {
                string message;
                try
                {
                    NetworkController network;
                    lock (screenToolsGate)
                    {
                        if (screenToolsDisposed) return;
                        if (networkController == null)
                        {
                            networkController = new NetworkController();
                            NetworkController observed = networkController;
                            observed.StateChanged += delegate
                            {
                                try { BeginInvoke((MethodInvoker)delegate
                                {
                                    if (IsDisposed) return;
                                    RefreshNetworkStateUi();
                                    if (!observed.IsBlocked && observed.LastError.Length > 0) SetStatusSafe(EditorDialogs.T("网络限制已解除。", "Network restrictions removed."));
                                }); } catch (InvalidOperationException) { }
                            };
                        }
                        network = networkController;
                    }
                    bool requestedRestore = Interlocked.Exchange(ref networkRestorePending, 0) != 0;
                    bool restoreNow = restore || requestedRestore;
                    if (!restoreNow && (uiSafetyModalDepth > 0 || pauseHotkeys || uiSafetyPauseRequested || recordingActive || captureMode != CaptureMode.None)) return;
                    bool okay = restoreNow ? network.Restore() : network.Toggle(options);
                    if (Interlocked.Exchange(ref networkRestorePending, 0) != 0) okay = network.Restore();
                    message = okay ? network.IsBlocked ? EditorDialogs.T("网络已断开；再次按快捷键恢复。", "Traffic blocked; press the shortcut again to restore.")
                        : EditorDialogs.T("网络已恢复。", "Traffic restored.") : network.LastError == "Administrator approval was cancelled."
                        ? EditorDialogs.T("已取消管理员授权，网络保持连接。", "Administrator approval cancelled; traffic remains connected.")
                        : EditorDialogs.T("无法切换网络：", "Cannot change network state: ") + network.LastError;
                }
                catch (Exception ex) { message = ex.Message; AppLog.Write("Network toggle failed", ex); }
                finally
                {
                    Interlocked.Exchange(ref networkOperationPending, 0);
                    if (Volatile.Read(ref networkRestorePending) != 0) QueueNetworkOperation(true);
                }
                try { BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) { RefreshNetworkStateUi(); SetStatusSafe(message); } }); } catch (InvalidOperationException) { }
            });
        }
        private void ShowTimerDialog(object sender, EventArgs e)
        {
            uiSafetyModalDepth++; UpdateUiSafetyPauseState();
            try
            {
                ApplyScreenTools();
                using (TimerSettingsDialog dialog = new TimerSettingsDialog(config.Timer, timerController, TimerShortcutConflict))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    TimerOptions previous = config.Timer; config.Timer = dialog.Result;
                    if (!SaveConfig()) config.Timer = previous;
                }
            }
            finally { uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1); UpdateUiSafetyPauseState(); if (!IsDisposed) ApplyScreenTools(); }
        }
        private void ShowNetworkDialog(object sender, EventArgs e)
        {
            uiSafetyModalDepth++; UpdateUiSafetyPauseState();
            try
            {
                using (NetworkSettingsDialog dialog = new NetworkSettingsDialog(config.NetworkControl, NetworkShortcutConflict))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    NetworkControlOptions previous = config.NetworkControl; config.NetworkControl = dialog.Result;
                    if (!SaveConfig()) config.NetworkControl = previous;
                    bool scopeChanged = !config.NetworkControl.Enabled || previous.Mode != config.NetworkControl.Mode ||
                        !string.Equals(previous.ExecutablePath, config.NetworkControl.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                    if (scopeChanged && networkController != null && networkController.IsBlocked) QueueNetworkOperation(true);
                }
            }
            finally { uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1); UpdateUiSafetyPauseState(); }
        }
    }
}
