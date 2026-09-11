namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.4.3";
        public const string FileVersion = "1.4.3.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "1.4.3 修复运行一段时间后键盘热键可能突然整体失效的问题。\r\n\r\n" +
            "• 新增独立键盘输入兜底：低级键盘钩子漏掉真实按键时，仍可继续触发宏并自动恢复键盘钩子。\r\n" +
            "• 恢复过程只重建键盘钩子，不影响鼠标钩子和正在运行的鼠标映射；诊断增加兜底与恢复计数。\r\n" +
            "• 不修改配置格式和宏执行语义；正常主通道下的触发与按键屏蔽行为保持不变。";
        public const string ReleaseSummaryEn =
            "InputStitch 1.4.3 fixes a reliability issue where keyboard hotkeys could stop responding after the application had been running for a while.\r\n\r\n" +
            "• A passive Raw Input fallback now keeps physical keyboard triggers working when the low-level keyboard hook misses an edge, and automatically recovers the keyboard hook.\r\n" +
            "• Recovery rebuilds only the keyboard hook, preserving the mouse hook and active mouse mapping state; diagnostics now expose fallback and recovery counters.\r\n" +
            "• Configuration format and macro semantics are unchanged; normal trigger suppression behavior is unchanged while the primary hook is healthy.";
    }
}
