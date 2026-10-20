namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.3";
        public const string FileVersion = "1.5.0.2";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "这次更新继续完善界面和日常操作。\r\n\r\n" +
            "• 用带用途说明的卡片新建宏，导入与备份集中在一个入口。\r\n" +
            "• 编辑区有清楚的步骤引导，空白宏会提示从哪里开始。\r\n" +
            "• 编辑步骤时，实时预览按键、按住时长和等待时间。\r\n" +
            "• 快捷创建会提示下一步，设置完整后即可创建。\r\n" +
            "• 已有配置和运行方式保持兼容。";
        public const string ReleaseSummaryEn =
            "This update refines the editor and everyday workflow.\r\n\r\n" +
            "• Create macros from descriptive cards; import and backup share one menu.\r\n" +
            "• Numbered sections and an empty-state guide show where to start.\r\n" +
            "• Step editing previews the key, hold duration and wait time as you make changes.\r\n" +
            "• Quick create explains the next step and enables Create when setup is complete.\r\n" +
            "• Existing configurations and execution behavior stay compatible.";
    }
}
