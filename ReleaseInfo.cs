namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.14";
        public const string FileVersion = "1.5.0.13";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "整理窗口与选择列表，让常用操作更清楚、紧凑。\r\n\r\n" +
            "• 设置入口更醒目；步骤中的键盘图选择与实际按键录入直接可见。\r\n" +
            "• 更新粉紫到青蓝渐变的钻石图标；保留原有功能和轻量打包。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "Tidier windows and choice lists with clearer, compact controls.\r\n\r\n" +
            "• A clearer settings entry and visible diagram / physical input methods in the step editor.\r\n" +
            "• A vivid pink-violet-cyan diamond icon; existing features and lightweight packaging retained.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
