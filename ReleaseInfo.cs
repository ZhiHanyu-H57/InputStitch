namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.5.0-beta.1";
        public const string FileVersion = "1.5.0.0";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "1.5.0-beta.1 是下一个功能版本的首个测试版，与 1.4.3 正式版通道相互独立。\r\n\r\n" +
            "• 映射层新增“按住临时生效”和“上下文条件自动生效”：按住切换键临时启用某层，松开回到原来的层；\r\n" +
            "  也可按前台进程/窗口标题或稳定设备身份自动启用某层，条件失效后回到手动选择的层。\r\n" +
            "• 新增“短按”激活方式：窗口内松开触发一次，按住超过窗口即作废，滚轮不参与。\r\n" +
            "• 新增可选 SDL3 手柄通道：接入 PlayStation / Switch / HID 手柄；微软系手柄仍走 XInput 通道；\r\n" +
            "  设置里可以关闭；运行时已内置在程序里，没有安装额外组件的要求。\r\n" +
            "• 配置格式未变，可与 1.4.3 共用配置目录（请不要同时运行两个版本）。";
        public const string ReleaseSummaryEn =
            "InputStitch 1.5.0-beta.1 is the first Beta of the next feature version and is independent from the 1.4.3 Stable channel.\r\n\r\n" +
            "• Layers gained hold-to-activate (the layer is active only while its switch key is held) and context conditions (a layer becomes active while a foreground process/title or stable device identity matches).\r\n" +
            "• A new Short Press activator fires once on release inside a configurable window and disarms when the key is held longer.\r\n" +
            "• An optional SDL3 input lane adds PlayStation/Switch/HID controllers; Xbox-family pads stay on XInput and the lane can be turned off in Settings. The runtime is embedded in the executable.\r\n" +
            "• The configuration format is unchanged and can be shared with 1.4.3 (do not run both versions at the same time).";
    }
}
