namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.8";
        public const string FileVersion = "1.5.0.7";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "窗口化与无边框窗口准星测试版，可从左侧“屏幕准星”进入，无需安装组件。\r\n\r\n" +
            "• 8 种常见样式，可调大小、线宽、间隙、圆点、描边、颜色与透明度。\r\n" +
            "• 默认居中，支持选择显示器和逐像素微调，可实时预览。\r\n" +
            "• 默认 Ctrl + Alt + F10 显隐，快捷键可改；设置与显隐状态会保存。\r\n" +
            "• 穿透鼠标点击、不抢焦点。请使用窗口化或无边框窗口；本版不支持独占全屏。";
        public const string ReleaseSummaryEn =
            "Windowed and borderless crosshair beta. Open Screen crosshair in the sidebar; no component installation is needed.\r\n\r\n" +
            "• Eight styles with size, line width, gap, center dot, outline, colors and opacity.\r\n" +
            "• Centered by default, with display selection, pixel offsets and live preview.\r\n" +
            "• Ctrl + Alt + F10 toggles visibility by default. Settings and visibility are saved.\r\n" +
            "• Click-through and non-activating. Use windowed or borderless mode; exclusive fullscreen is not supported in this build.";
    }
}
