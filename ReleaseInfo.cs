namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.0-beta.7";
        public const string FileVersion = "1.6.0.7";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "新增屏幕计时器与可逆网络开关。\r\n\r\n" +
            "• 计时器可调整字体、文字和位置，选择普通置顶窗口或 RTSS 游戏内绘制。\r\n" +
            "• 网络开关可通过快捷键断开与恢复整机或指定程序的联网。\r\n" +
            "• 准星新增可调缺口的缺角方环样式。";
        public const string ReleaseSummaryEn =
            "Screen timer and reversible network toggle.\r\n\r\n" +
            "• Customize timer fonts, text and position with desktop or RTSS output.\r\n" +
            "• Block and restore traffic for an application or the computer using a shortcut.\r\n" +
            "• Crosshairs include a square ring with adjustable corner gaps.";
    }
}
