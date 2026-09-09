namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.4.1";
        public const string FileVersion = "1.4.1.0";
        public const bool IsPrerelease = false;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "1.4.1 是 1.4.0 的界面稳定性修复版。\r\n\r\n" +
            "• 修复“管理映射层”弹出菜单关闭时过早释放 ContextMenuStrip，导致 WinForms 在菜单点击收尾阶段抛出 ObjectDisposedException 的问题。\r\n" +
            "• 临时菜单改为等当前 ToolStrip 消息处理完后再释放；程序退出时也不再同步销毁仍可能被 WinForms ModalMenuFilter 引用的菜单。\r\n" +
            "• 1.4.0 的功能与配置格式保持不变；实验性手柄接管状态也没有改变。";
        public const string ReleaseSummaryEn =
            "InputStitch 1.4.1 is a UI-stability hotfix for 1.4.0.\r\n\r\n" +
            "• Fixes premature ContextMenuStrip disposal when the Mapping Layer management menu closes, which could raise ObjectDisposedException while WinForms was still completing the menu click.\r\n" +
            "• Transient menus are now disposed on the next UI message turn; shutdown also avoids synchronously disposing menus that may still be referenced by WinForms' ModalMenuFilter.\r\n" +
            "• 1.4.0 behavior and configuration formats are unchanged; controller takeover remains Experimental.";
    }
}
