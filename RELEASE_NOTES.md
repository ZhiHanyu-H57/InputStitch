# InputStitch 1.5.1

本次是性能与稳定性维护版本，冻结现有功能，不新增功能或第三方运行依赖。1.4.3、1.5.0 的宏和设置继续使用，无需重新配置。

## 本次改进

- 将运行生命周期、进度与单步控制逐步交给独立协调器，减少重复处理；诊断名称在查看时按需生成。
- 高级触发松开后，停止不必要的空闲界面任务。
- 已准备好输出的长按宏，可以在界面繁忙时按输入线程的节拍启动，减少等待；保留编辑保护、松开取消、紧急停止和输出所有权。
- 首次设备准备、程序自身窗口的编辑状态和条件判断仍保留必要的界面协作。这次改进不代表所有物理按键、驱动及游戏延迟均不受界面影响。

## 下载与升级

- 现代 64 位 Windows 请选择 **InputStitch-1.5.1-Windows-x64.exe**；32 位系统请选择 x86。下载后可改名为 **InputStitch.exe** 使用。
- 便携式单文件程序；需 .NET Framework 4.7.2 或兼容的更高版本。关闭旧版后启动新版，自动读取已有配置。
- 软件内更新后，主程序继续固定为 **InputStitch.exe**。常见桌面/开始菜单中指向旧程序的快捷方式同步目标，保留自定义名称与参数；其他位置的快捷方式可手动指向新文件。
- 更新先备份原程序及配置，安装或启动失败时恢复旧程序；目录中已有另一个 InputStitch.exe 时停止更新，保留原文件。
- 键鼠宏无需额外驱动；虚拟手柄输出沿用单独安装的 ViGEmBus。保留原更新偏好和官网优先、GitHub 备用更新链路。
- 窗口/无边框准星继续直接使用；独占全屏准星不在本版支持范围，无需 Game Bar 组件。手柄接管仍属实验性能力。
- 附完整源码、正式更新清单与 SHA-256 校验文件。

## 自动化验收

正式发布执行完整回归、x64/x86 成品启动、原始 1.4.3 / 1.5.0 配置加载与保存重开、更新备份/回滚、真实键鼠和虚拟手柄输出，以及十分钟持续运行检查。验收以自动化结果为依据，不要求每轮人工试用。

- 本地正式验收：36 套完整回归通过；188 个弹窗布局样本零问题；81 项真实输出检查零失败；原始 1.4.3、1.5.0 文件升级与配置重开通过。
- 持续运行 600.1 秒、4,520 轮，结束无残留输出，只创建 3 个宏工作线程，未强制垃圾回收。
- x64 主程序约 1.90 MiB，x86 约 1.74 MiB；x64 比 1.5.0 增加约 6.5 KiB。

实际结果和证据范围见 [1.5.1 正式验收记录](docs/RELEASE_1_5_1_VERIFICATION.md)。十分钟持续运行是有界证据，不替代具体实体设备和游戏的兼容性结论。

---

This maintenance release reduces repeated runtime bookkeeping and idle UI work,
and lets prepared long-press runs start while the UI is busy. Editing safety,
release cancellation, emergency stop and output ownership remain in place.
Initial device preparation and own-window checks still require UI cooperation.
No new features, runtime dependencies or configuration fields are introduced.

Existing 1.4.3 and 1.5.0 settings remain compatible. In-app updates retain the fixed
name **InputStitch.exe**, executable/configuration backups and failure rollback.
Use the x64 portable EXE on 64-bit Windows. Virtual-controller output requires
ViGEmBus; exclusive fullscreen crosshairs are not supported. Full automated release
acceptance is recorded in the linked verification document.
