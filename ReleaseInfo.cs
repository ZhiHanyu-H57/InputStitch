namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.3.0-beta.1";
        public const string FileVersion = "1.3.0.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "• 多个 Held Mapping 现在可以同时保持，并与最多一个普通时序宏共存。\r\n" +
            "• 新增 Input Ownership：按钮引用所有权、扳机 max、摇杆向量合并与 D-pad 冲突消解。\r\n" +
            "• 新增轻量运行观察，可查看来源贡献、合并状态、步骤和停止原因。\r\n" +
            "• 普通宏新增单步执行；Emergency Stop 与退出清理继续全局释放所有输出。";
        public const string ReleaseSummaryEn =
            "• Multiple Held Mappings can now stay active together and coexist with one ordinary timed macro.\r\n" +
            "• Added Input Ownership with digital reference ownership, trigger max, stick-vector merging and D-pad conflict resolution.\r\n" +
            "• Added lightweight runtime observation for source contributions, merged state, steps and stop reasons.\r\n" +
            "• Ordinary macros gain single-step execution; Emergency Stop and shutdown still release every owned output.";
    }
}
