# InputStitch

[English](README.md) | [简体中文](README.zh-CN.md)

**InputStitch —— Windows 精确输入映射与自动化工具**

InputStitch 是一款轻量级 Windows 可视化键盘、鼠标与虚拟手柄宏工具，重点关注精确时序、适合游戏场景的输入方式，以及可靠的紧急停止能力。

程序基于 Windows Forms 和 .NET Framework 4.7.2 构建。界面可以在 **简体中文** 与 **English** 之间即时切换，无需重启。

## 下载

**当前正式版：v1.2.0；公开 Beta：v1.3.0-beta.2。** 正式版仍是推荐的稳定回退基线。Beta 2 延续 beta.1 的 Input Ownership 底座，并发布统一 Concurrent Macro Runtime：普通时序 / Toggle、高级/复杂 Hold 与 Parallel Held Mapping 可以同时运行，分别进行 Source 清理和逐 Hold 松键跟踪。Beta 始终作为 GitHub prerelease 发布，不替换 Stable 的 `releases/latest` 或 `InputStitch-update.xml`。

Beta 与正式版目前共用配置。测试 Beta 前请退出另一版本并备份 `%APPDATA%\InputStitch`，不要同时运行两个版本。详见[开发路线和发布规则](ROADMAP.md)。

可以从 [GitHub 最新正式版](../../releases/latest) 直接下载可执行文件：

| Windows 架构 | 直接下载 |
| --- | --- |
| 64 位 Windows（x64） | [InputStitch-1.2.0-Windows-x64.exe](../../releases/latest/download/InputStitch-1.2.0-Windows-x64.exe) |
| 32 位 Windows（x86） | [InputStitch-1.2.0-Windows-x86.exe](../../releases/latest/download/InputStitch-1.2.0-Windows-x86.exe) |
| 完整源码 | [InputStitch-1.2.0-Source.zip](../../releases/latest/download/InputStitch-1.2.0-Source.zip) |
| 文件校验值 | [SHA256SUMS.txt](../../releases/latest/download/SHA256SUMS.txt) |

当前 Beta 使用固定版本下载地址：

| Beta 文件 | 直接下载 |
| --- | --- |
| 64 位 Windows（x64） | [InputStitch-1.3.0-beta.2-Windows-x64.exe](../../releases/download/v1.3.0-beta.2/InputStitch-1.3.0-beta.2-Windows-x64.exe) |
| 32 位 Windows（x86） | [InputStitch-1.3.0-beta.2-Windows-x86.exe](../../releases/download/v1.3.0-beta.2/InputStitch-1.3.0-beta.2-Windows-x86.exe) |
| Beta 完整源码 | [InputStitch-1.3.0-beta.2-Source.zip](../../releases/download/v1.3.0-beta.2/InputStitch-1.3.0-beta.2-Source.zip) |
| Beta 更新清单 | [InputStitch-beta.xml](../../releases/download/v1.3.0-beta.2/InputStitch-beta.xml) |
| Beta 文件校验值 | [SHA256SUMS.txt](../../releases/download/v1.3.0-beta.2/SHA256SUMS.txt) |

InputStitch 仅支持 Windows，没有 macOS 或 Linux 版 EXE。如果不确定应该下载哪个版本，现代 64 位 Windows 通常请选择 x64。

EXE 为便携式程序：下载后放入有写入权限的文件夹即可运行。系统需要安装 .NET Framework 4.7.2 或兼容的更高版本。键鼠输出不需要额外驱动；虚拟手柄输出还需要另行安装下文说明的 ViGEmBus 驱动。

## 主要功能

- 可视化编辑键盘、鼠标按键、侧键、滚轮、Xbox 360 与 PS4 / DualShock 4 步骤
- 支持虚拟摇杆（方向 -180°～180°、力度 0%～100%）、模拟扳机（0%～100%）、正面按键、肩键、摇杆按下、方向键和菜单键
- 编辑手柄步骤时显示动态虚拟手柄预览，高亮当前按键、扳机或摇杆方向
- 为每个步骤设置按住时长和执行后间隔
- 快捷创建“按住映射 / 定次数连按 / 顺序执行”；普通键、单独左右 Ctrl/Shift/Alt/Win 和鼠标按钮都可作为 Held Mapping 触发键
- Input Ownership + Concurrent Macro Runtime：当前 `main` 可同时保持多个符合条件的 Held Mapping，并并发运行多个不同的普通时序 / Toggle 宏
- 明确的合并规则：数字输出按来源引用、扳机取最大值、摇杆向量按圆形范围合并、D-pad 对向轴相互取消
- 轻量“运行观察”，实时查看活动 Source、各来源贡献、合并结果、普通宏步骤/阶段和停止原因
- 普通时序宏支持“单步执行 / 下一步”
- 步骤 Undo / Redo，历史最多保留 50 次操作
- 配置采用暂存/验证后替换，并保留最近 5 份有效主配置备份
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
2. 启动 InputStitch，新建或选择一个宏；“新增”菜单可以直接创建按住映射、定次数连按或顺序执行模板。
3. 手动添加步骤，或者使用“录制宏”。录制器只录制物理键鼠；虚拟手柄步骤需手动添加。
4. 录制触发键并选择触发方式。
5. 从主界面执行宏，或者启用该宏的全局触发。
6. 在其他程序中使用前，确认并测试 InputStitch 中显示的紧急停止键。

点击齿轮按钮可以进入设置，其中包含“语言 / Language”、虚拟手柄类型、更新方式、安全选项、目标窗口行为、运行观察/诊断和托盘偏好。虚拟手柄默认选择 Xbox 360，因为它对 Windows/XInput 游戏的兼容性通常最好；原生支持 PlayStation 手柄的游戏可选择 PS4 / DualShock 4。Stable 可以使用带 SHA-256 校验的自动更新通道；Beta / prerelease 会主动关闭无人值守的自动更新检查，手动检查 Beta 时只打开 GitHub Releases，不修改 Stable 更新清单。

手柄摇杆在编辑器中采用更直观的方向与力度：`0°` 表示向前、`90°` 表示向右、`-90°` 表示向左、`±180°` 表示向后。旧配置中的 X/Y 数值仍会兼容读取并保持原值；只有实际修改方向或力度时才换算为新的摇杆状态。要做“按住一个键就持续保持某个手柄状态”的映射，推荐直接使用“快捷创建 → 按住映射”。在 1.3.0-beta.1 中，符合条件的 Held Mapping 各自拥有独立 Source；松开一个映射只会移除它自己的贡献，其余摇杆、扳机和按钮来源会继续保持并重新合并。

## 虚拟键盘与闲置输入

“录制触发键”与“捕获输入”旁的下拉箭头可打开全尺寸虚拟键盘。点击按键选择，再点一次取消选择；支持单键与多键模式，确认后应用。小键盘、导航键和功能键无需实体键盘具备；左右 Shift、Ctrl、Alt、Win 和 Esc 均可单独选择。组合触发键采用“修饰键＋一个主键”；多个步骤按键会插入配有对应释放动作的组合。

手柄预览会按 Xbox 360 / PS4 类型显示相应布局。摇杆 0° 为向前，+90° 为向右，-90° 为向左，±180° 为向后；力度控制偏移幅度。预览不会发送实际输入。

在“设置 → 自动化 → 闲置自动手柄输入”中，可在空闲一段时间后发送一次短暂的手柄动作。此功能默认关闭，正式默认参数为 **120 秒、左摇杆向下、150 ms**。该区域有自己独立的“挂机目标”，可先切到游戏、再切回设置并点击“锁定刚才的窗口”；这个目标只用于闲置计时，**不依赖也不会修改主界面的目标窗口、界面运行时切换或按前台程序自动切换方案**。设置挂机目标后，只有该目标程序位于前台时的键鼠操作才会重新计时；在浏览器、聊天软件等其他程序中打字或移动鼠标，不会阻止后台目标游戏按时收到挂机手柄输入。若已设置的挂机目标当前不存在，闲置输出会完全暂停，不发送任何手柄脉冲；目标重新出现后会从完整空闲时间重新计时。未设置挂机目标时仍按全局键鼠活动计时；实体手柄活动和宏运行会重新计时，编辑弹窗期间暂停。真实鼠标移动由 Raw Input 判断，因此游戏自己锁定或重定位光标不会被误当成持续操作。紧急停止会关闭闲置输出，须自行重新开启。它通过既有虚拟手柄发送输入，不切换前台窗口；游戏仍需支持该设备和后台输入，程序无法强制游戏接收。对于只在启动时识别手柄的游戏，请先连接虚拟手柄，再启动游戏。

## 可选的虚拟手柄驱动

虚拟手柄输出依赖 [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest)。这个上游项目已经停止维护，不再获得更新。InputStitch 不会静默安装驱动：检测到缺少驱动时，会说明原因并询问是否打开原作者的官方 GitHub Release 页面。请只从该官方页面下载，自行完成安装，然后重启 InputStitch。

建议先在“设置 → 虚拟手柄输出”中连接手柄，再启动游戏。InputStitch 会让同一个虚拟手柄保持连接直到程序退出。普通 Source 结束时只移除自己的 ownership，其他仍在活动的 Held Mapping / Idle 来源会继续参与合并；**紧急停止**和程序退出才是清空全部 Source、强制手柄归零的全局边界，而且不会拔掉设备。这能规避部分游戏只在启动时枚举手柄、忽略后续热插拔的问题。在游戏运行中切换 Xbox 360 / PS4 类型后，可能需要重启游戏。

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

### Beta 的 Shift 兼容修复

beta.2 中，**独立 Shift + 按住触发 + 全部为手柄步骤**的宏会自动把原始 Shift 同时传给游戏，保留奔跑键作用。屏蔽复选框会说明此例外，其他宏仍保留原来的屏蔽偏好。这修复了已确认的输入屏蔽冲突，实际游戏接收效果仍需复测。v1.1.0 用户也可以先尝试取消该宏的“屏蔽触发键”。

### 前台热键与诊断（beta.3）

仅悬停或选中非编辑按钮/列表不再阻止热键启动。焦点仍在名称、数值或下拉编辑控件时继续保护，并显示原因；点击空白处即可结束编辑。运行中的鼠标宏仍防止误点控件。诊断包含内存中最近 64 条宏运行事件，不是持续键盘日志；“已提交输出”不代表游戏已接收。
### 易用性与配置安全（beta.4）

beta.4 加入暂存/验证后替换的安全配置保存与最近 5 份有效备份、三种快捷创建模板，以及步骤撤销/重做。按住映射支持普通键盘键、左右单独 Ctrl/Shift/Alt/Win 和鼠标按钮（含侧键）作为触发键；按住模式仍不支持修饰键组合与滚轮。同触发键的已启用宏按列表顺序决定优先级。闲置自动手柄输入不再把游戏自身的鼠标重定位误判为持续操作，并拥有独立的“挂机目标”；其他程序中的键鼠输入不会阻止后台目标游戏的挂机脉冲，已设置的挂机目标不存在时则完全暂停输出，目标重新出现后从完整空闲时间重新计时。当前自动测试已经覆盖这些情况，但不会发送真实输入，正式推广前仍需完成实际游戏和虚拟手柄接收验收。

### Input Ownership 与任意宏多并发（1.3.x）

beta.1 将旧的“单一当前宏拥有全部输出”改成显式 Source ownership。多个符合条件的 Held Mapping 可以同时保持，并与最多一个普通时序宏共存。数字输出按来源引用，扳机取最大值，摇杆向量相加后按圆形范围归一化，D-pad 同轴相反方向相互取消。“工具 → 运行观察…”可以查看活动 Source、各来源贡献和合并结果；普通时序宏新增“单步执行 / 下一步”。

`v1.3.0-beta.2` 正式发布**统一 Concurrent Macro Runtime**：多个不同普通时序 / Toggle 宏、多个高级/复杂 Hold 宏可以同时运行，并与 Parallel Held Mapping 共存；每个 worker 运行实例拥有独立 RunId/SourceId/停止/时序状态，复杂 Hold 还按 Run 独立保存物理触发键与 lost-KeyUp 松键恢复状态，松开一个 Hold 只停止它自己。编辑器里的“运行资格 / 并发能力”实时提示直接来自执行时使用的同一个 classifier。同触发键继续按宏列表顺序决定优先级；Single-step 仍是刻意保持独占的诊断模式；Emergency Stop 始终是全局最高优先级。Layer / 映射层明确延后到 Stable 1.3.0 之后；详见 [Layer 设计说明](docs/LAYER_DESIGN.md)。
