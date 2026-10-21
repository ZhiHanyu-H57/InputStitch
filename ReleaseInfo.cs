namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.10";
        public const string FileVersion = "1.5.0.9";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "整理常用宏设置：触发键直接点击选择，次数与无限循环放在一起。\r\n\r\n" +
            "• 中英文按文字宽度排版，窄窗口整组换行，运行状态直接显示在步骤上方。\r\n" +
            "• 压缩内置运行库，缩小单文件体积；原有宏、手柄与准星功能保留。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "Compact macro settings: click the current shortcut to edit it; repeat count and infinite repeat stay together.\r\n\r\n" +
            "• Fields fit their translated text and wrap as whole groups. Readiness is visible above the steps.\r\n" +
            "• Compressed bundled runtimes reduce file size while retaining macro, controller and crosshair features.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
