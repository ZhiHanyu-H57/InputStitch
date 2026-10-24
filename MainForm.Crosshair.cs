using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputStitch
{
    public partial class MainForm
    {
        private Button crosshairButton;
        private CrosshairController crosshairController;
        private void ApplyCrosshair()
        {
            if (crosshairController == null) crosshairController = new CrosshairController(this);
            crosshairController.Apply(config.Crosshair);
        }
        private void DisposeCrosshair()
        {
            if (crosshairController != null) { crosshairController.Dispose(); crosshairController = null; }
        }
        private string CrosshairShortcutConflict(CrosshairOptions options)
        {
            if (!options.Enabled || options.Hotkey == null) return "";
            TriggerSpec key = options.Hotkey;
            string toolConflict = ToolShortcutConflict(key, null, false, true, true);
            if (toolConflict.Length > 0) return toolConflict;
            InputEventInfo candidate = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = key.VirtualKey, Extended = key.Extended },
                Ctrl = key.Ctrl, Shift = key.Shift, Alt = key.Alt, Win = key.Win };
            if (ModifierSafetyPolicy.TriggerRequiredModifiersMatch(config.PanicTrigger, candidate))
                return EditorDialogs.T("此快捷键与紧急停止冲突，请另选一组按键。", "This hotkey conflicts with Emergency Stop. Choose another combination.");
            if (config.Macros != null)
                foreach (MacroDefinition macro in config.Macros)
                    if (macro != null && macro.Enabled && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(macro.Trigger, candidate))
                        return EditorDialogs.T("此快捷键已用于宏：", "This hotkey is used by macro: ") + macro.Name;
            if (config.MappingLayers != null)
                foreach (MappingLayerDefinition layer in config.MappingLayers)
                    if (layer != null && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(layer.SwitchTrigger, candidate))
                        return EditorDialogs.T("此快捷键已用于分组：", "This hotkey is used by layer: ") + layer.Name;
            return "";
        }
        private bool HandleCrosshairShortcut(InputEventInfo e)
        {
            CrosshairOptions o = config.Crosshair;
            if (o == null || !o.Enabled || o.Hotkey == null || uiSafetyModalDepth > 0 || pauseHotkeys || uiSafetyPauseRequested ||
                ModifierSafetyPolicy.GetTriggerModifierMask(o.Hotkey) != ModifierSafetyPolicy.GetEventModifierMask(e) ||
                !ModifierSafetyPolicy.TriggerTerminalMatches(o.Hotkey, e) || CrosshairShortcutConflict(o).Length > 0) return false;
            // The existing physical-edge dispatcher filters injected events and keyboard repeats.
            try { BeginInvoke((MethodInvoker)ToggleCrosshairVisibility); } catch (InvalidOperationException) { }
            return true;
        }
        private void ToggleCrosshairVisibility()
        {
            if (!config.Crosshair.Enabled || uiSafetyModalDepth > 0 || IsDisposed) return;
            bool previous = config.Crosshair.Visible;
            config.Crosshair.Visible = !previous;
            if (!SaveConfig()) config.Crosshair.Visible = previous;
            ApplyCrosshair();
        }
        private void ShowCrosshairDialog(object sender, EventArgs e)
        {
            uiSafetyModalDepth++; UpdateUiSafetyPauseState();
            try
            {
                if (crosshairController == null) crosshairController = new CrosshairController(this);
                using (CrosshairSettingsDialog dialog = new CrosshairSettingsDialog(config.Crosshair, crosshairController.Apply, CrosshairShortcutConflict))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    CrosshairOptions previous = config.Crosshair;
                    config.Crosshair = dialog.Result;
                    if (!SaveConfig()) config.Crosshair = previous;
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1); UpdateUiSafetyPauseState();
                ApplyCrosshair();
            }
        }
    }
}
