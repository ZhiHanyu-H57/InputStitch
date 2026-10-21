namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.11";
        public const string FileVersion = "1.5.0.10";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "整理其余窗口：减少多余留白，列表行距更清楚，标题与页签适配文字。\r\n\r\n" +
            "• 改善设置、时间与条件、按键选择、导出和设备窗口的中英文与高 DPI 排版。\r\n" +
            "• 压缩内置运行库，缩小单文件体积；原有宏、手柄与准星功能保留。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "Clearer dialogs with less wasted space, separated list rows and text-aware headings and tabs.\r\n\r\n" +
            "• Improved bilingual and high-DPI layouts for settings, conditions, key selection, export and device dialogs.\r\n" +
            "• Compressed bundled runtimes reduce file size while retaining macro, controller and crosshair features.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
