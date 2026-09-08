namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.3.0";
        public const string FileVersion = "1.3.0.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "• 普通时序 / Toggle、高级/复杂 Hold 与 Parallel Held Mapping 现在可同时运行。\r\n" +
            "• Hold 支持修饰键组合；Shift 按住时裸普通键仍可在没有更具体组合时触发。\r\n" +
            "• 每个运行实例独立跟踪 Source/松键清理；Emergency Stop 仍可一次释放全部输出。\r\n" +
            "• 齿轮菜单新增项目 GitHub 入口，运行资格提示也与统一 classifier 保持一致。";
        public const string ReleaseSummaryEn =
            "• Ordinary timed/Toggle, Advanced/complex Hold and Parallel Held Mapping can now run concurrently.\r\n" +
            "• Hold supports modifier chords, while bare ordinary keys can still trigger under Shift when no more specific chord matches.\r\n" +
            "• Each run owns independent source/release cleanup; Emergency Stop still clears every output at once.\r\n" +
            "• The gear menu now links to the project GitHub page, and runtime eligibility stays aligned with the authoritative classifier.";
    }
}
