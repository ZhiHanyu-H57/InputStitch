namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2-beta.1";
        public const string FileVersion = "1.6.2.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "减少后台输入处理的开销。\r\n\r\n" +
            "• 减少手柄轮询中的临时内存分配。\r\n" +
            "• 普通按键跳过无关的工具快捷键冲突检查。\r\n" +
            "• 修复 SDL 手柄断开后扳机释放事件遗漏。";
        public const string ReleaseSummaryEn =
            "Reduce background input-processing overhead.\r\n\r\n" +
            "• Reduce temporary allocations during controller polling.\r\n" +
            "• Skip unrelated tool-shortcut conflict checks for ordinary input.\r\n" +
            "• Release held SDL trigger inputs when a controller disconnects.";
    }
}
