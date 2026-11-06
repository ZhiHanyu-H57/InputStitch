namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2-beta.4";
        public const string FileVersion = "1.6.2.4";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "分离宏执行与界面。\r\n\r\n" +
            "• 执行循环使用独立模块，界面保留编辑保护和运行控制。\r\n" +
            "• 减少执行过程中重复复制输入状态的开销。\r\n" +
            "• 沿用现有配置、停止和输入释放机制。";
        public const string ReleaseSummaryEn =
            "Separate macro execution from the interface.\r\n\r\n" +
            "• Run steps in an independent executor with the existing editing protection.\r\n" +
            "• Reduce redundant copies of active input state.\r\n" +
            "• Keep existing settings, stop controls and input release behavior.";
    }
}
