namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.4";
        public const string FileVersion = "1.5.0.3";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "这次更新统一了其余窗口，并打磨从创建到运行的流程。\r\n\r\n" +
            "• 设置、时间与条件、按键选择及其他窗口采用统一样式，底部操作始终可用。\r\n" +
            "• 时间窗口只显示当前触发方式需要的参数，条件输入提供示例。\r\n" +
            "• 选按键时即时提示组合是否有效，主界面明确下一步和运行方式。\r\n" +
            "• 从创建到选择按键、自动保存和首次运行，均由自动化验收。";
        public const string ReleaseSummaryEn =
            "This update unifies the remaining dialogs and refines creation through the first run.\r\n\r\n" +
            "• Settings, timing, key selection and other dialogs share a consistent layout and persistent actions.\r\n" +
            "• Timing shows only parameters used by your trigger; condition fields include examples.\r\n" +
            "• Key combinations are validated as you select them. The main editor explains the next step and how to run.\r\n" +
            "• Automated acceptance covers actual creation, key selection, automatic saving and the first run.";
    }
}
