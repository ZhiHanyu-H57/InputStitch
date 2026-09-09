# InputStitch 1.4.1

## 一句话看懂

**这是 1.4.0 的界面稳定性修复版：修复“管理映射层”弹出菜单关闭时过早释放 `ContextMenuStrip`，导致 WinForms 在菜单点击收尾阶段抛出 `ObjectDisposedException` 的问题。**

1.4.1 不新增功能、不修改配置格式，也不改变 1.4.0 的手柄、Layer、宏并发或更新策略。

## 修复内容

### 1. 映射层管理菜单不再在 `Closed` 事件里同步释放

1.4.0 的“管理映射层”使用临时 `ContextMenuStrip`。菜单关闭时，旧代码会直接在 `Closed` 回调中执行 `Dispose()`。

但 WinForms 在触发 `Closed` 后仍可能继续执行当前菜单点击的内部收尾流程，例如 `ToolStripDropDown.OnItemClicked`、`SetVisibleCore` 和 `ModalMenuFilter`。这时对象已经被 InputStitch 提前释放，就会出现：

```text
System.ObjectDisposedException: 无法访问已释放的对象。
对象名:“ContextMenuStrip”。
```

1.4.1 改为：

- `Closed` 发生时**不立即 Dispose**；
- 通过 UI dispatcher 把释放动作排到**下一轮 Windows 消息循环**；
- 当前 ToolStrip 点击/关闭流程先完整结束，再释放临时菜单。

“快速创建”菜单也统一使用同一个延迟释放辅助函数，避免两套生命周期规则重新分叉。

### 2. 主窗口关闭时不再同步 Dispose 可能仍活跃的菜单

如果程序关闭动作恰好由菜单项触发，WinForms 的 `ModalMenuFilter` 在点击处理完全返回前仍可能保存对该下拉菜单的引用。

1.4.1 因此在主窗口退出清理阶段不再主动同步 `Dispose()` `toolsMenu` / `trayMenu`；程序正在退出，直接放弃应用层引用比在 ToolStrip 内部仍可能使用它们时强制销毁更安全。

## 回归验证

UI Safety/Diagnostics 专项从 23 项增加到 **26 项**，新增验证：

- ContextMenuStrip 的释放不会发生在当前 ToolStrip 事件内；
- 下一轮 UI 消息处理后才完成释放；
- dispatcher 已经释放时不会退化成同步销毁菜单。

完整回归、x64/x86 Release Verification、Stable 更新清单和 GitHub Windows CI 仍是 1.4.1 发布门槛。

## 其他说明

用户日志里更早出现过 1.3.0 的 `Nefarius.ViGEm.Client` `FileNotFoundException`。检查确认旧 1.3.0 备份和当前 1.4.x EXE 都实际内嵌了 `InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll`，隔离探针也能成功执行 `EmbeddedDependencyLoader.Register → GamepadOutput.NeutralizeAll`。目前无法在 1.4.x 复现该程序集解析错误，因此 1.4.1 不对依赖加载器做未经证实的改动；后续如果再次出现会继续按独立问题调查。

---

# English summary

**InputStitch 1.4.1 is a UI-stability hotfix for 1.4.0. It fixes premature `ContextMenuStrip` disposal in Mapping Layer management, which could raise `ObjectDisposedException` while WinForms was still completing the current ToolStrip click/close sequence.**

Changes:

- transient context menus are disposed on the next UI message turn instead of synchronously from `Closed`;
- Quick Create and Layer Management now share the same deferred-disposal rule;
- shutdown no longer synchronously disposes tools/tray context menus that may still be referenced by WinForms' `ModalMenuFilter`;
- no feature or configuration-format changes from 1.4.0;
- UI Safety/Diagnostics coverage increases from 23 to 26 checks.
