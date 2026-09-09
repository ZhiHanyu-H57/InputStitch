namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.3.1-beta.1";
        public const string FileVersion = "1.3.1.1";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "现在 InputStitch 可以直接用手柄触发宏、把多个手柄汇总到自己的虚拟手柄，并在实验性接管模式下先安全取得 0 号槽位，再统一转发所选原手柄。\r\n\r\n" +
            "• 可读取所有可见的 XInput 手柄槽位；手柄按键和扳机可以直接作为宏触发方式。\r\n" +
            "• 如果只使用键盘/鼠标宏或手柄触发，可以在设置中选择“不创建虚拟手柄”；启动时不会因为保存了手柄输出宏而创建虚拟设备。\r\n" +
            "• 可选的“手柄汇总”会持续转发其他手柄的按键、摇杆和扳机，并与宏输出安全合并。\r\n" +
            "• 基础层始终启用；普通时序宏、切换宏、复杂按住宏和轻量按住映射都可以放入映射层。\r\n" +
            "• 实验性“手柄接管”会在隐藏原设备前先重新排列外部 XInput 手柄并硬验证 InputStitch 已成为 0 号；任何一步失败都会先恢复并终止。真正隐藏原设备仍需要用户自行安装 HidHide，实体手柄/实际游戏验收仍在继续。";
        public const string ReleaseSummaryEn =
            "InputStitch can now use connected controllers to trigger macros, merge multiple controllers into its virtual controller, and in experimental takeover mode safely acquire XInput slot 0 before selected original controllers are routed through it.\r\n\r\n" +
            "• All visible XInput slots can be read; controller buttons and triggers can start macros.\r\n" +
            "• If only keyboard/mouse macros or controller triggers are needed, Settings can disable virtual-controller creation; saved gamepad-output macros no longer force a virtual device to appear at startup.\r\n" +
            "• Optional controller merging mirrors buttons, sticks and triggers from other controllers and safely combines them with macro output.\r\n" +
            "• Base is always active; timed, toggle, complex Hold and lightweight Held Mapping macros can all belong to a layer.\r\n" +
            "• Experimental takeover re-enumerates external XInput/XUSB controllers and hard-verifies InputStitch in slot 0 before HidHide can hide any selected original device; failures restore/stop before hiding. Real physical-controller/game acceptance is still pending.";
    }
}
