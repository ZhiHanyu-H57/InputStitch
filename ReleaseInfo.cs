namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.1";
        public const string FileVersion = "1.5.1.3";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "长按触发更及时，已有宏和设置继续使用。\r\n\r\n" +
            "• 减少界面繁忙时的长按启动等待，保留编辑保护和紧急停止。\r\n" +
            "• 减少运行进度和诊断处理中的重复工作，保持轻量。\r\n" +
            "• 更新后程序名继续固定为 InputStitch.exe，无需重新配置宏。";
        public const string ReleaseSummaryEn =
            "More timely long-press activation; your macros and settings are retained.\r\n\r\n" +
            "• Less long-press startup delay while the UI is busy, preserving editing protection and Emergency Stop.\r\n" +
            "• Less repeated progress and diagnostic work, with lightweight packaging.\r\n" +
            "• In-app updates keep the fixed name InputStitch.exe. No macro reconfiguration is needed.";
    }
}
