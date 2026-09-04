# InputStitch

[English](README.md) | [简体中文](README.zh-CN.md)

InputStitch 是一款轻量级 Windows 可视化键盘、鼠标与虚拟手柄宏工具，重点关注精确时序、适合游戏场景的输入方式，以及可靠的紧急停止能力。

程序基于 Windows Forms 和 .NET Framework 4.7.2 构建。界面可以在 **简体中文** 与 **English** 之间即时切换，无需重启。

## 下载

可以从 [GitHub 最新正式版](../../releases/latest) 直接下载可执行文件：

| Windows 架构 | 直接下载 |
| --- | --- |
| 64 位 Windows（x64） | [InputStitch-1.0.0-Windows-x64.exe](../../releases/latest/download/InputStitch-1.0.0-Windows-x64.exe) |
| 32 位 Windows（x86） | [InputStitch-1.0.0-Windows-x86.exe](../../releases/latest/download/InputStitch-1.0.0-Windows-x86.exe) |
| 完整源码 | [InputStitch-1.0.0-Source.zip](../../releases/latest/download/InputStitch-1.0.0-Source.zip) |
| 文件校验值 | [SHA256SUMS.txt](../../releases/latest/download/SHA256SUMS.txt) |

InputStitch 仅支持 Windows，没有 macOS 或 Linux 版 EXE。如果不确定应该下载哪个版本，现代 64 位 Windows 通常请选择 x64。

EXE 为便携式程序：下载后放入有写入权限的文件夹即可运行。系统需要安装 .NET Framework 4.7.2 或兼容的更高版本。键鼠输出不需要额外驱动；虚拟手柄输出还需要另行安装下文说明的 ViGEmBus 驱动。

## 主要功能

- 可视化编辑键盘、鼠标按键、侧键、滚轮、Xbox 360 与 PS4 / DualShock 4 步骤
- 支持虚拟摇杆（X/Y -100%～100%）、模拟扳机（0%～100%）、正面按键、肩键、摇杆按下、方向键和菜单键
- 为每个步骤设置按住时长和执行后间隔
- 全局热键、单次触发启停和按住运行模式
- 固定执行次数或无限循环
- 录制物理键盘、鼠标按钮和滚轮操作并自动还原时序；有意不录制鼠标移动
- 使用扫描码发送键盘输入，提高在许多游戏中的兼容性
- 用宏包分享选定的宏
- 用配置方案保存并切换整套设置
- 可选的目标窗口切换，以及按前台程序自动切换已绑定方案
- 选择性快捷键冲突防护：允许 `W + Shift` 等正常游戏操作，同时拦截高风险的意外特殊组合键
- 可自定义的全局紧急停止键；即使普通宏触发被暂停，紧急停止仍然有效
- 简体中文 / English 即时切换
- 内置自动、手动和关闭三种更新模式；安装前会根据 Release 更新清单核对官方 EXE 的 SHA-256
- 适配 DPI 且可自由调整大小的 Windows Forms 界面

## 快速开始

1. 下载与 Windows 架构匹配的 EXE。
2. 启动 InputStitch，新建或选择一个宏。
3. 手动添加执行步骤，或者点击“录制宏”。录制器只录制物理键鼠；虚拟手柄步骤需手动添加。
4. 录制触发键并选择触发方式。
5. 从主界面执行宏，或者启用该宏的全局触发。
6. 在其他程序中使用前，确认并测试 InputStitch 中显示的紧急停止键。

点击齿轮按钮可以进入设置，其中包含“语言 / Language”、虚拟手柄类型、更新方式、安全选项、目标窗口行为、诊断和托盘偏好。虚拟手柄默认选择 Xbox 360，因为它对 Windows/XInput 游戏的兼容性通常最好；原生支持 PlayStation 手柄的游戏可选择 PS4 / DualShock 4。更新方式默认是“启动时自动检查并提示安装”：程序只检查官方 GitHub Release，并会在下载和安装前询问；也可以改为仅手动检查或完全不检查。

## 可选的虚拟手柄驱动

虚拟手柄输出依赖 [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest)。这个上游项目已经停止维护，不再获得更新。InputStitch 不会静默安装驱动：检测到缺少驱动时，会说明原因并询问是否打开原作者的官方 GitHub Release 页面。请只从该官方页面下载，自行完成安装，然后重启 InputStitch。

建议先在“设置 → 虚拟手柄输出”中连接手柄，再启动游戏。InputStitch 会让一个处于中立状态的虚拟手柄保持连接直到程序退出；停止宏或使用紧急停止只会把全部手柄输入归零，不会拔掉设备。这能规避部分游戏只在启动时枚举手柄、忽略后续热插拔的问题。在游戏运行中切换 Xbox 360 / PS4 类型后，可能需要重启游戏。

## 安全提示

InputStitch 会向 Windows 发送模拟的键盘、鼠标和可选虚拟手柄输入。正式使用新宏前，请先在无关紧要的程序里测试。

- 设置一个容易按到的紧急停止键，并先确认它能够正常工作。
- 不要运行未经检查的宏，也不要导入来源不可信的宏包。
- 不要在违反软件、服务、游戏、单位规定或当地法律的场景中使用自动化。
- 在线游戏或受保护的程序可能禁止自动化或拒绝模拟输入，兼容性不作保证。
- 其他手柄工具可能占用虚拟设备或 XInput 玩家槽位。如果游戏识别错手柄，请关闭相互竞争的工具，先连接 InputStitch，再启动游戏。
- InputStitch 不会绕过反作弊、访问控制或程序安全机制。

安全问题报告方式请参阅 [SECURITY.md](SECURITY.md)。

## 从源码构建

需要：

- Windows
- .NET Framework 4.7.2 Developer Pack 或兼容的 MSBuild 工具
- 带有“.NET 桌面开发”组件的 Visual Studio，或对应的 Build Tools

可以执行：

```powershell
.\build.ps1
```

或者：

```bat
build.bat
```

也可以用 Visual Studio 打开 `InputStitch.csproj`。正式发布产物会分别构建 x64 和 x86 版本。

## 数据与隐私

配置、方案、宏包、备份和日志都保存在本地，InputStitch 不要求登录在线账号。默认情况下，程序会在启动时通过 HTTPS 请求本仓库最新 Release 的更新清单；可以在设置中改为手动或关闭。程序不会上传配置或宏内容。分享诊断信息或配置文件之前，请先检查内容，因为其中可能包含本机的窗口标题、进程名、宏名称或路径。

## 参与贡献

欢迎提交问题报告和范围明确的拉取请求。参与前请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证

本仓库目前**没有声明开源许可证**。源代码可公开访问，并不代表自动授予复制、修改、再发布或其他超出适用法律规定范围的使用权。仓库所有者以后可能会补充许可证。

程序内嵌的 `Nefarius.ViGEm.Client` 依赖另行采用 MIT 许可证，详见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
