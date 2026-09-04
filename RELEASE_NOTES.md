# InputStitch 1.0.0

[English](#english) | [简体中文](#简体中文)

## English

InputStitch 1.0.0 is the first public release of the lightweight visual keyboard and mouse macro tool for Windows.

### Download

Choose the executable that matches your Windows architecture:

- **64-bit Windows:** `InputStitch-1.0.0-Windows-x64.exe`
- **32-bit Windows:** `InputStitch-1.0.0-Windows-x86.exe`
- **Complete source:** `InputStitch-1.0.0-Source.zip`
- **SHA-256 checksums:** `SHA256SUMS.txt`
- **Updater manifest:** `InputStitch-update.xml`

InputStitch supports Windows only. There is no macOS or Linux executable. The program requires .NET Framework 4.7.2 or a compatible later release.

### Highlights

- DPI-aware, resizable interface with instant Simplified Chinese / English switching
- Visual keyboard and mouse macro editing and physical-input recording
- Per-step hold duration and delay, finite repetition, infinite looping, and hold-to-run
- Global triggers and configurable Emergency Stop
- Selective protection against unintended high-risk Windows shortcut combinations while preserving normal held-modifier gameplay
- Scan-code keyboard output for game-friendly compatibility
- Macro packages, profiles, optional target-window activation, and foreground-app profile switching
- Local configuration, backups, diagnostics, and tray controls
- Automatic, manual, or disabled update checks; Automatic is the default, installation is always prompted, and downloaded executables must match the official release SHA-256 manifest

If you downloaded an earlier build carrying the same `1.0.0` version label, that build cannot update itself because it did not yet contain the updater. Download this refreshed `1.0.0` executable once; subsequent official replacements or newer versions can then be detected automatically.

### Before running

These executables are currently **not code-signed**. Windows Defender SmartScreen may therefore display an “unrecognized app” warning, even when the downloaded file is unchanged. Download only from this repository's official Releases page and compare the file's SHA-256 value with `SHA256SUMS.txt` before running it.

Test a new macro in a harmless application first, confirm that the Emergency Stop hotkey works, and follow the automation rules of the software or game you use.

## 简体中文

InputStitch 1.0.0 是这款轻量级 Windows 可视化键鼠宏工具的首个公开正式版。

### 下载

请根据 Windows 架构选择可执行文件：

- **64 位 Windows：** `InputStitch-1.0.0-Windows-x64.exe`
- **32 位 Windows：** `InputStitch-1.0.0-Windows-x86.exe`
- **完整源码：** `InputStitch-1.0.0-Source.zip`
- **SHA-256 校验值：** `SHA256SUMS.txt`
- **自动更新清单：** `InputStitch-update.xml`

InputStitch 仅支持 Windows，没有 macOS 或 Linux 版 EXE。程序需要 .NET Framework 4.7.2 或兼容的更高版本。

### 主要功能

- 适配 DPI、可调整大小的界面，以及简体中文 / English 即时切换
- 可视化键鼠宏编辑和物理输入录制
- 每步按住时长与间隔、固定循环、无限循环和按住运行
- 全局触发和可自定义的紧急停止
- 选择性防护意外的高风险 Windows 特殊组合键，同时保留正常的按住修饰键游戏操作
- 使用扫描码发送键盘输入，提高游戏场景兼容性
- 宏包、配置方案、可选目标窗口切换，以及按前台程序自动切换方案
- 本地配置、备份、诊断和托盘控制
- 自动、手动或关闭三种更新模式；默认自动检查、安装前始终询问，并要求下载的 EXE 通过官方 Release 清单中的 SHA-256 校验

如果你此前下载过同样标为 `1.0.0` 的较早构建，它本身还没有更新器，因此无法自动更新到这次替换后的构建。请手动下载这次刷新后的 `1.0.0` 一次；此后发布的官方替换构建或更高版本即可被自动检测。

### 运行前须知

目前发布的 EXE **没有代码签名**。因此，即使下载的文件没有被修改，Windows Defender SmartScreen 也可能显示“无法识别的应用”之类的警告。请只从本仓库的官方 Releases 页面下载，并在运行前将文件的 SHA-256 与 `SHA256SUMS.txt` 对照确认。

请先在无关紧要的程序里测试新宏，确认紧急停止键能够正常工作，并遵守所使用软件或游戏的自动化规则。

