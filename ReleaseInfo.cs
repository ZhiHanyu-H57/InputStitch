namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.2-beta.1";
        public const string FileVersion = "1.5.2.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "修复鼠标选择，完善录制与批量时间编辑。\r\n\r\n" +
            "• 触发键和执行步骤支持鼠标图选择，包括左右键、中键、侧键与滚轮。\r\n" +
            "• 录制无需设置目标窗口，开始录制会结束之前的单键录入。\r\n" +
            "• 可批量修改所选或全部步骤的按住时长与等待时长。";
        public const string ReleaseSummaryEn =
            "Mouse selection, recording and batch timing fixes.\r\n\r\n" +
            "• Select mouse buttons, side buttons and wheel directions on a diagram for shortcuts and steps.\r\n" +
            "• Recording works without a target window and clears any previous single-input capture.\r\n" +
            "• Edit hold duration and wait time for selected steps or all steps at once.";
    }
}
