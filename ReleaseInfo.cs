namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.2.0";
        public const string FileVersion = "1.2.0.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "• 配置保存更安全：验证后替换，并保留最近 5 份有效备份。\r\n" +
            "• 新增快捷创建与步骤撤销/重做，按住映射更容易配置。\r\n" +
            "• 修复前后台切换、修饰键按住映射与释放可靠性问题。\r\n" +
            "• 闲置手柄输入新增独立挂机目标，并改进真实活动识别。";
        public const string ReleaseSummaryEn =
            "• Safer verified config replacement with five recent valid backups.\r\n" +
            "• Quick Create and step Undo/Redo make common mappings easier to edit.\r\n" +
            "• Improved foreground/background, modifier-held mapping and release reliability.\r\n" +
            "• Idle gamepad input now has an independent target and better real-activity detection.";
    }
}
