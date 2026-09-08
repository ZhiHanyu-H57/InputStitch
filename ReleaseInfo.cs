namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.3.0-beta.2";
        public const string FileVersion = "1.3.0.2";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "• 普通时序 / Toggle、高级/复杂 Hold 与 Parallel Held Mapping 现在可同时运行。\r\n" +
            "• 每个复杂 Hold 独立跟踪物理松键与 lost-KeyUp；停止一个运行实例不会清除其他 Source。\r\n" +
            "• 编辑器实时显示由统一 classifier 给出的运行类别与并发能力。\r\n" +
            "• Emergency Stop 与退出清理仍保持全局最高优先级。";
        public const string ReleaseSummaryEn =
            "• Ordinary timed/Toggle, Advanced/complex Hold and Parallel Held Mapping can now run concurrently.\r\n" +
            "• Each complex Hold tracks physical release/lost-KeyUp independently; stopping one run does not clear unrelated sources.\r\n" +
            "• The editor now shows runtime category and concurrency capability from the same authoritative classifier.\r\n" +
            "• Emergency Stop and shutdown remain the global highest-priority cleanup paths.";
    }
}
