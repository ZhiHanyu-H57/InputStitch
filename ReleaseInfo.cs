namespace InputStitch
{
    // Build scripts and release workflows read these constants as the release source of truth.
    public static class ReleaseInfo
    {
        public const string Version = "1.3.1-beta.2";
        public const string FileVersion = "1.3.1.2";
        public const bool IsPrerelease = true;
        public static bool AutomaticChecksAllowed { get { return !IsPrerelease; } }
        public const string ReleasesUrl = "https://github.com/ZhiHanyu-H57/InputStitch/releases";
        // Keep this deliberately short: it is shown once after an existing user upgrades.
        // Detailed release notes remain in RELEASE_NOTES.md / GitHub Releases.
        public const string ReleaseSummaryZh =
            "现在映射层可以自由新增、命名、删除和设置快捷切换；实验性手柄接管也会在运行中持续自检，发现关键状态异常时自动退出接管并尝试恢复原手柄。\r\n\r\n" +
            "• 基础层始终有效，除此之外可以创建任意多个自定义映射层；所有宏类型都能分层。\r\n" +
            "• 每个映射层都可以绑定键盘、鼠标或手柄快捷键；切换时旧层只释放自己的输出，新层中原本已按住的触发仍需先松开再重新按。\r\n" +
            "• 删除映射层时，其中的宏会安全移回基础层；修复了旧 Layer 1 删除后重启又自动出现的问题。\r\n" +
            "• 手柄接管运行期间会持续检查 0 号槽位、手柄汇总、原 XInput 来源以及 HidHide 隐藏/白名单状态；连续异常会自动恢复并关闭汇总，避免双输入。\r\n" +
            "• 运行观察和诊断现在会直接显示当前映射层、虚拟手柄槽位、手柄汇总、接管健康和恢复状态。实体手柄 + HidHide + 实际游戏的最终硬件验收仍需等有测试手柄后完成。";
        public const string ReleaseSummaryEn =
            "Mapping layers can now be freely added, named, deleted, and bound to switch shortcuts; experimental controller takeover also monitors its runtime health and automatically disengages/restores on confirmed safety failures.\r\n\r\n" +
            "• Base is always eligible and any number of additional named layers can be created; every macro type participates in layer eligibility.\r\n" +
            "• Each layer can use a keyboard, mouse, or controller switch trigger; leaving a layer removes only that layer's owned output and newly eligible held triggers still require release + press.\r\n" +
            "• Deleting a layer moves its macros safely back to Base, and deleted legacy Layer 1 no longer reappears after XML reload.\r\n" +
            "• Active takeover continuously checks slot 0, Router readiness, routed XInput source visibility, and HidHide cloak/hidden-device/application state; confirmed failure restores takeover changes and disables routing to avoid doubled input.\r\n" +
            "• Runtime observation/diagnostics now expose active layer, virtual slot, Router, takeover health, and recovery state. Real physical-controller + HidHide + game acceptance remains pending.";
    }
}
