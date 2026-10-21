namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.9";
        public const string FileVersion = "1.5.0.8";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "界面优先易读、易用、整洁：适中字号、紧凑间距，给执行步骤更多展示空间。\r\n\r\n" +
            "• 调整步骤列表列宽与行高，减少滚动和手动调整布局。\r\n" +
            "• 压缩内置运行库，缩小单文件体积；原有宏、手柄与准星功能保留。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "A clearer, more compact editor with balanced text and more room for steps.\r\n\r\n" +
            "• Improved row heights and column widths reduce scrolling and manual resizing.\r\n" +
            "• Compressed bundled runtimes reduce file size while retaining macro, controller and crosshair features.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
