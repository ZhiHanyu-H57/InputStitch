namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.1-beta.2";
        public const string FileVersion = "1.5.1.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "后台运行更省资源，已有设置继续使用。软件内更新后，程序名固定为 InputStitch.exe。\r\n\r\n" +
            "• 已连接的手柄保持及时响应；新接入手柄通常在半秒左右被发现。\r\n" +
            "• 反复运行与停止宏时，减少不必要的资源占用。\r\n" +
            "• 屏幕准星用于窗口化和无边框窗口，默认 Ctrl + Alt + F10 显隐。";
        public const string ReleaseSummaryEn =
            "Lower background resource use; your settings are retained. In-app updates use the fixed name InputStitch.exe.\r\n\r\n" +
            "• Connected controllers stay responsive. Newly connected XInput controllers appear within about half a second.\r\n" +
            "• Repeated macro runs and stops use fewer resources.\r\n" +
            "• Crosshairs support windowed/borderless mode. Ctrl + Alt + F10 toggles visibility by default.";
    }
}
