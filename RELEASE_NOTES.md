# InputStitch 1.1.0

[English](#english) | [简体中文](#简体中文)

## English

### What's new

- **Full-size virtual keyboard:** open the dropdown beside Record Trigger or Capture Input, then choose Virtual Keyboard. Click keys to select/highlight; click again to deselect. Single/multiple-key modes include modifiers, Esc, function/navigation keys, and the number pad. Triggers support a standalone key or modifiers plus one main key; step chords press in order and release in reverse.
- **Refined controller preview:** Xbox 360 and PS4 / DualShock 4 layouts, clearer highlighting, and live stick direction/strength. Previewing does not send real input.
- **Optional idle gamepad input:** Settings > Automation lets you choose inactivity interval, controller input, and hold time. Disabled by default. Keyboard/mouse activity, supported physical controllers, and macros restart the timer. Macro execution and editing pause idle output; Emergency Stop disables it until explicitly re-enabled.
- **Tabbed settings** and compatibility with existing macro/configuration files.

Idle input does not switch windows. Background reception depends on the game and its controller settings; background GTA Online support is not guaranteed. Other apps receiving the same controller may respond too. Follow the automation rules of the software/game you use.

### Download and upgrade

- **64-bit Windows:** `InputStitch-1.1.0-Windows-x64.exe`
- **32-bit Windows:** `InputStitch-1.1.0-Windows-x86.exe`
- **Complete source:** `InputStitch-1.1.0-Source.zip`
- **Checksums:** `SHA256SUMS.txt`; updater manifest: `InputStitch-update.xml`

This is a new release, not a replacement of v1.0.0. Builds with the updater can detect v1.1.0; older builds without it require manual downloading. Configuration stays in `%APPDATA%\InputStitch`. Back up that folder before upgrading or rolling back.

Windows only; .NET Framework 4.7.2 or a compatible later release is required. No macOS/Linux executable is provided. EXEs are currently **not code-signed**; SmartScreen may warn about an unknown publisher. Use this repository's official downloads and verify SHA-256.

Virtual gamepad output requires the separately installed [ViGEmBus driver](https://github.com/nefarius/ViGEmBus/releases/latest). The upstream project is retired; use only the original author's official Release page. InputStitch provides missing-driver guidance, but never silently installs drivers. Keyboard/mouse functions do not require it. Connect the virtual controller before launching the game for best compatibility.

## 简体中文

### 本次更新

- **全尺寸虚拟键盘：** 点击“录制触发键”或“捕获输入”旁的下拉按钮，选择虚拟键盘。点击按键高亮选择，再次点击取消；支持单键/多键模式，包含修饰键、Esc、功能键、导航键和数字小键盘。触发键可为单独按键或“修饰键 + 一个主键”；步骤组合键按顺序按下、反序释放。
- **优化手柄预览：** 区分 Xbox 360 与 PS4 / DualShock 4 布局，更清晰地高亮编辑项，摇杆随方向和力度动态显示。预览不会发送实际输入。
- **可选闲置手柄输入：** 在“设置 > 自动化”中选择无操作时间、手柄操作和按住时长，默认关闭。键鼠、支持的实体手柄活动和宏运行会重新计时；宏运行和编辑时暂停闲置输出，紧急停止后需明确重新启用。
- **分页设置界面**，继续兼容已有宏与配置文件。

闲置输入不会切换窗口。游戏能否在后台接收取决于游戏及其手柄设置，不保证 GTA Online 后台挂机可用；其他接收同一虚拟手柄的程序也可能响应。请遵守所使用软件或游戏的自动化规则。

### 下载与升级

- **64 位 Windows：** `InputStitch-1.1.0-Windows-x64.exe`
- **32 位 Windows：** `InputStitch-1.1.0-Windows-x86.exe`
- **完整源码：** `InputStitch-1.1.0-Source.zip`
- **校验值：** `SHA256SUMS.txt`；更新清单：`InputStitch-update.xml`

此次独立发布新版，不覆盖 v1.0.0。带更新器的版本可以检测到 v1.1.0；没有更新器的较早构建需手动下载。配置仍保存在 `%APPDATA%\InputStitch`，升级或回退前建议备份该文件夹。

仅支持 Windows，需要 .NET Framework 4.7.2 或兼容的更高版本，不提供 macOS/Linux 可执行文件。EXE 目前**未做代码签名**，SmartScreen 可能提示未知发布者；请使用本仓库官方下载并核对 SHA-256。

手柄输出需要另外安装 [ViGEmBus 驱动](https://github.com/nefarius/ViGEmBus/releases/latest)。上游项目已停止维护，请只使用原作者的官方 Release 页面。缺少驱动时程序会提醒并提供链接，不会静默安装；键鼠功能不依赖此驱动。建议在启动游戏前连接虚拟手柄。
