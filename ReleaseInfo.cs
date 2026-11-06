namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2";
        public const string FileVersion = "1.6.2.6";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "改进宏运行与停止响应。\r\n\r\n" +
            "• 减少设备采样、宏执行和手柄路由的重复工作。\r\n" +
            "• 取消失效的触发请求，改善停止和输入释放。\r\n" +
            "• 修复无等待滚轮循环和退出清理问题，保留已有宏与设置。";
        public const string ReleaseSummaryEn =
            "Improve macro execution and stop response.\r\n\r\n" +
            "• Reduce repeated work in device sampling, macro execution and controller routing.\r\n" +
            "• Cancel stale triggers and improve stop and input release handling.\r\n" +
            "• Fix zero-delay wheel loops and shutdown cleanup while keeping existing settings.";
    }
}
