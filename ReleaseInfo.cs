namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.13";
        public const string FileVersion = "1.5.0.12";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "收紧过宽的选择框，让常用设置和更多设置的范围更清楚。\r\n\r\n" +
            "• 屏蔽触发键直接可见；更多设置有独立边框，步骤编辑的手柄示意图更大。\r\n" +
            "• 压缩内置运行库，缩小单文件体积；原有宏、手柄与准星功能保留。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "Compact choice fields and clearer separation between common and advanced settings.\r\n\r\n" +
            "• Trigger blocking stays visible, advanced controls have a clear frame, and the step controller diagram is larger.\r\n" +
            "• Compressed bundled runtimes reduce file size while retaining macro, controller and crosshair features.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
