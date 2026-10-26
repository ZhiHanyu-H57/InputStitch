# InputStitch 1.6.1

- 修复鼠标触发键屏蔽 InputStitch 自身界面点击的问题，菜单、标题栏和关闭按钮可正常操作。
- 紧急停止后暂停全局宏触发，防止下一次点击立即重启宏。需要恢复时，在齿轮菜单取消“暂停全局宏触发”。“全部停止”按钮仍可停止当前运行后继续使用触发键。
- 修复按住 Shift 时 PageDown 等单键无法触发的问题，统一支持普通按键与 Shift 共用；明确配置的组合键优先匹配。

正常退出旧程序后更新，已有宏和设置会保留。

---

- Mouse triggers no longer consume clicks on InputStitch controls, menus, title bars or close buttons.
- Emergency Stop pauses global macro triggers to prevent an immediate restart. Uncheck **Pause global macro triggers** in the gear menu to resume. The ordinary **Stop all** button keeps its existing behavior.
- Bare keys, including PageDown, work while Shift is held. Explicitly configured shortcuts take priority.

Close the previous version before updating. Existing macros and settings are retained.