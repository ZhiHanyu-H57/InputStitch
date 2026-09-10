using System;

namespace InputStitch
{
    internal static class TargetWindowPolicy
    {
        internal static bool HasConfiguredTarget(MacroConfig config)
        {
            return !string.IsNullOrWhiteSpace(config.TargetProcessName)
                || !string.IsNullOrWhiteSpace(config.TargetWindowTitle)
                || !string.IsNullOrWhiteSpace(config.TargetWindowClass);
        }

        internal static bool IdleKeyboardMouseScopeMatches(bool hasConfiguredTarget, string targetProcessName, string foregroundProcessName, bool exactTargetWindow)
        {
            if (!hasConfiguredTarget) return true;
            if (exactTargetWindow) return true;
            if (string.IsNullOrWhiteSpace(targetProcessName)) return false;
            return string.Equals(targetProcessName.Trim(), (foregroundProcessName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ShouldPauseIdleForMissingTarget(bool hasConfiguredIdleTarget, bool resolvedTargetAvailable)
        {
            return hasConfiguredIdleTarget && !resolvedTargetAvailable;
        }

        internal static bool HasConfiguredIdleTarget(MacroConfig config)
        {
            IdleGamepadOptions idle = config == null ? null : config.IdleGamepad;
            return idle != null && (!string.IsNullOrWhiteSpace(idle.TargetProcessName)
                || !string.IsNullOrWhiteSpace(idle.TargetWindowTitle)
                || !string.IsNullOrWhiteSpace(idle.TargetWindowClass));
        }

        internal static bool IsIdleKeyboardMouseActivityInScope(MacroConfig config, IntPtr foreground, IntPtr idleTargetWindowHandle)
        {
            IdleGamepadOptions idle = config == null ? null : config.IdleGamepad;
            bool hasTarget = idle != null && HasConfiguredIdleTarget(config);
            bool exactTarget = foreground != IntPtr.Zero && idleTargetWindowHandle != IntPtr.Zero && foreground == idleTargetWindowHandle;
            string foregroundProcess = "";
            if (hasTarget && !exactTarget && !string.IsNullOrWhiteSpace(idle.TargetProcessName) && NativeWindowFocus.IsUsableExternalWindow(foreground))
            {
                TargetWindowIdentity info = NativeWindowFocus.Describe(foreground);
                if (info != null) foregroundProcess = info.ProcessName ?? "";
            }
            return IdleKeyboardMouseScopeMatches(hasTarget, idle == null ? "" : idle.TargetProcessName, foregroundProcess, exactTarget);
        }

        internal static IntPtr ResolveIdleTargetWindow(MacroConfig config, IntPtr currentHandle)
        {
            if (NativeWindowFocus.IsUsableExternalWindow(currentHandle)) return currentHandle;
            if (config == null || config.IdleGamepad == null || !HasConfiguredIdleTarget(config)) return IntPtr.Zero;
            return NativeWindowFocus.ResolveConfiguredTarget(
                config.IdleGamepad.TargetProcessName,
                config.IdleGamepad.TargetWindowTitle,
                config.IdleGamepad.TargetWindowClass);
        }

        internal static IntPtr ResolveTargetWindow(MacroConfig config, IntPtr currentHandle)
        {
            if (NativeWindowFocus.IsUsableExternalWindow(currentHandle)) return currentHandle;
            return NativeWindowFocus.ResolveConfiguredTarget(
                config.TargetProcessName,
                config.TargetWindowTitle,
                config.TargetWindowClass);
        }
    }
}
