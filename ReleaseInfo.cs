namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.2";
        public const string FileVersion = "1.5.0.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "这次更新集中改善界面和编辑体验。\r\n\r\n" +
            "• 所有触发方式统一在主界面选择，时间与条件按需调整。\r\n" +
            "• 已有宏直接显示对应的操作方式，不需要选择版本模式。\r\n" +
            "• 宏列表、编辑区和按钮采用统一样式；运行和全部停止始终位于窗口底部。\r\n" +
            "• 配置格式保持不变，已有宏可以继续使用。";
        public const string ReleaseSummaryEn =
            "This update focuses on the editor and everyday usability.\r\n\r\n" +
            "• One selector covers every trigger behavior; timing and optional conditions have their own editor.\r\n" +
            "• Existing macros display their actual behavior directly.\r\n" +
            "• The macro list, editor and buttons share a consistent visual style. Run and Stop all stay at the bottom.\r\n" +
            "• The configuration format is unchanged; your existing macros continue to work.";
    }
}
