namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.5";
        public const string FileVersion = "1.5.0.4";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "这次更新放大了界面文字，并调整配套布局。\r\n\r\n" +
            "• 正文、表单和按钮统一为 12pt，虚拟键盘键名提高到 10pt。\r\n" +
            "• 同步增加按钮高度、列表行高及标题和说明的空间。\r\n" +
            "• 手柄预览说明保持正常字号，不再随图形缩小。\r\n" +
            "• 创建、编辑、保存、运行及不同窗口尺寸继续由自动化验收。";
        public const string ReleaseSummaryEn =
            "This update makes interface text larger and adjusts the surrounding layout.\r\n\r\n" +
            "• Body text, forms and buttons now use 12pt. Keyboard key labels use 10pt.\r\n" +
            "• Taller buttons and list rows give larger text enough space.\r\n" +
            "• Controller preview captions retain their normal font size as the diagram scales.\r\n" +
            "• Automated acceptance continues to cover creation, editing, saving, running and window layouts.";
    }
}
