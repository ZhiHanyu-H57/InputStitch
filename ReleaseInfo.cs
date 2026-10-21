namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.12";
        public const string FileVersion = "1.5.0.11";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "打磨从创建到运行的流程：统一步骤用语，清楚提示自动保存和运行方式。\r\n\r\n" +
            "• 修正步骤变化后的运行按钮刷新，以及快捷创建的组合触发键捕获。\r\n" +
            "• 压缩内置运行库，缩小单文件体积；原有宏、手柄与准星功能保留。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "A clearer create-to-run workflow with consistent step wording and visible save/run guidance.\r\n\r\n" +
            "• Fixed Run readiness after step changes and shortcut-chord capture in Quick create.\r\n" +
            "• Compressed bundled runtimes reduce file size while retaining macro, controller and crosshair features.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
