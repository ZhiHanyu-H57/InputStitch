namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.1-beta.1";
        public const string FileVersion = "1.6.1.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "修复鼠标触发导致界面无法点击的问题。\r\n\r\n" +
            "• 点击 InputStitch 的界面、菜单和关闭按钮时保留鼠标操作。\r\n" +
            "• 紧急停止后暂停全局宏触发，可在齿轮菜单手动恢复。\r\n" +
            "• 单键触发统一兼容按住 Shift，明确设置的组合键优先。";
        public const string ReleaseSummaryEn =
            "Fix mouse triggers blocking application controls.\r\n\r\n" +
            "• Mouse input reaches InputStitch controls, menus and the close button.\r\n" +
            "• Emergency Stop pauses global macro triggers until resumed from the gear menu.\r\n" +
            "• Bare keys work with held Shift; explicitly configured chords take priority.";
    }
}
