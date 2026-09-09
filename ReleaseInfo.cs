namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.4.0";
        public const string FileVersion = "1.4.0.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "1.4.0 正式版把并发宏、手柄输入/路由和多映射层整合成同一套可观察、可恢复的输入编排平台；实验性手柄接管仍保留为高级测试功能。\r\n\r\n" +
            "• 基础层始终有效，除此之外可以创建任意多个自定义映射层；所有宏类型都能分层。\r\n" +
            "• 实体/其他 XInput 手柄可以作为宏触发来源，并可把多个外部手柄汇总到 InputStitch 的虚拟 Xbox 手柄；也可以选择完全不创建虚拟手柄，仅使用键盘/鼠标功能。\r\n" +
            "• 多个普通时序、Toggle、复杂 Hold 和持续映射可以并发运行，并通过 Output Ownership 只释放各自拥有的输出。\r\n" +
            "• 更新器增加断网超时、有限重试和半包清理；配置保存继续使用验证、备份和严格的新旧顺序。\r\n" +
            "• 手柄接管仍标记为实验性：事务、回滚、恢复和运行自检已实现，但实体手柄 + HidHide + 实际游戏的最终硬件验收尚未完成。";
        public const string ReleaseSummaryEn =
            "InputStitch 1.4.0 promotes concurrent macros, controller input/routing, and flexible mapping layers into one observable and recoverable input-orchestration platform; controller takeover remains an advanced Experimental feature.\r\n\r\n" +
            "• Base is always eligible and any number of additional named layers can be created; every macro type participates in layer eligibility.\r\n" +
            "• Physical/other XInput controllers can trigger macros and multiple external controllers can be aggregated into the InputStitch virtual Xbox; virtual-controller creation can also be disabled completely for keyboard/mouse-only use.\r\n" +
            "• Ordinary timed, Toggle, complex Hold, and persistent mappings can overlap while Output Ownership removes only each source's own contribution.\r\n" +
            "• The updater now handles temporary network loss with bounded retries and partial-download cleanup; configuration saves retain verified backups in deterministic order.\r\n" +
            "• Controller takeover remains Experimental: transaction, rollback, recovery, and runtime health checks are implemented, but final physical-controller + HidHide + target-game acceptance is still pending.";
    }
}
