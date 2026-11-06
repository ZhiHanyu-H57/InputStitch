namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2-beta.3";
        public const string FileVersion = "1.6.2.3";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "改善宏执行与退出清理。\r\n\r\n" +
            "• 重复运行时复用已准备好的步骤输入。\r\n" +
            "• 修复零等待滚轮循环的节流遗漏。\r\n" +
            "• 单个组件退出失败时继续停止宏、恢复网络并释放输入。";
        public const string ReleaseSummaryEn =
            "Improve macro execution and shutdown cleanup.\r\n\r\n" +
            "• Reuse prepared step inputs across repeated execution.\r\n" +
            "• Restore pacing for zero-delay wheel loops.\r\n" +
            "• Continue stopping macros, restoring network access and releasing input after individual cleanup failures.";
    }
}
