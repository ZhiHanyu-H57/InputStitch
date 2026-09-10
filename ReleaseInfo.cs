namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.4.2";
        public const string FileVersion = "1.4.2.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "1.4.2 完成更新下载链路迁移，并继续整理内部架构。\r\n\r\n" +
            "• 正式版优先从 download.zhihanyu.com 检查和下载更新；官网不可用或校验失败时自动回退到官方 GitHub Release。\r\n" +
            "• 保留 GitHub 更新清单兼容旧版 1.4.1，同时为新版提供 R2 专用清单，下载仍强制执行 SHA-256 与产品/版本校验。\r\n" +
            "• 去除重复的 XInput/XML 基础设施，抽象虚拟手柄输出后端，并继续拆分 MainForm；宏、Layer、Output Ownership 与配置格式保持不变。";
        public const string ReleaseSummaryEn =
            "InputStitch 1.4.2 moves the Stable update path to the project download domain and continues internal architecture cleanup.\r\n\r\n" +
            "• Stable updates now check and download from download.zhihanyu.com first, with automatic fallback to the official GitHub Release when the website source is unavailable or fails validation.\r\n" +
            "• The GitHub manifest remains compatible with older 1.4.1 clients, while new clients receive an R2-specific manifest; SHA-256 plus product/version validation remain mandatory.\r\n" +
            "• Shared XInput/XML infrastructure was deduplicated, the virtual gamepad output backend was abstracted, and MainForm responsibilities were further separated without changing macro, Layer, Output Ownership, or configuration semantics.";
    }
}
