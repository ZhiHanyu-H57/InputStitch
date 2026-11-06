namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.6.2-beta.5";
        public const string FileVersion = "1.6.2.5";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "改善停止响应与后台开销。\r\n\r\n" +
            "• 界面卡顿时也能响应松键、停止和紧急释放。\r\n" +
            "• 禁用、删除或编辑宏后，取消排队中的旧触发。\r\n" +
            "• 减少重复保存、无关条件检查和手柄路由分配。";
        public const string ReleaseSummaryEn =
            "Improve stop response and background efficiency.\r\n\r\n" +
            "• Release and stop macros even while the interface is busy.\r\n" +
            "• Cancel queued triggers after disabling, removing or editing a macro.\r\n" +
            "• Reduce unchanged saves, unrelated condition checks and controller routing allocations.";
    }
}
