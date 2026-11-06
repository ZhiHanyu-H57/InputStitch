# InputStitch 1.6.2

- 将宏执行循环与界面分离，减少设备采样、步骤执行和手柄路由中的重复分配。
- 改进松键、停止和紧急释放的处理；漏掉松键事件时的补救也能在界面忙碌时运行。
- 禁用、删除、编辑宏或切换配置方案后，取消排队中的旧触发，避免意外启动。
- 减少无变化配置的重复写入，只对匹配的宏检查触发条件，降低后台开销。
- 修复无等待滚轮循环占用过高的问题，完善退出时的停止、网络恢复和输入释放。

正常退出旧程序后更新，已有宏、快捷键和设置会保留。

---

- Separate macro execution from the interface and reduce repeated allocations in device sampling, step execution and controller routing.
- Improve release, stop and emergency cleanup handling. Missed-release recovery also runs while the interface is busy.
- Cancel queued triggers after a macro is disabled, removed or edited, or after switching profiles.
- Avoid unchanged configuration writes and evaluate conditions only for matching macro shortcuts.
- Fix excessive activity in zero-delay wheel loops and improve shutdown cleanup for macros, network recovery and held inputs.

Close the previous version before updating. Existing macros, shortcuts and settings are retained.
