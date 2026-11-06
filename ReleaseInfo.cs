namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2-beta.2";
        public const string FileVersion = "1.6.2.2";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "减少后台输入处理的开销。\r\n\r\n" +
            "• 减少 SDL 原生状态读取的临时内存分配。\r\n" +
            "• 合并后台状态通知，避免界面忙时积压。\r\n" +
            "• 完善 SDL 设备关闭失败后的清理。";
        public const string ReleaseSummaryEn =
            "Reduce background input-processing overhead.\r\n\r\n" +
            "• Reduce temporary allocations in native SDL state reads.\r\n" +
            "• Coalesce background status updates while the UI is busy.\r\n" +
            "• Complete SDL cleanup after individual device-close failures.";
    }
}
