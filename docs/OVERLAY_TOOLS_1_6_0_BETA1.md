# InputStitch 1.6.0-beta.1 — 使用与验证结果

本地功能测试版，2026-10-23。GitHub 正式版保持 1.5.2；没有发布新版本或修改更新源。
新工具默认关闭，普通宏程序继续以普通权限运行。

## 使用

- 左侧“准星…”：选择“缺角方环”，用大小、线宽和“四角缺口”调节轮廓，沿用颜色、描边、不透明度、位置及显隐快捷键。
- 左侧“计时器…”：顶部直接开始、暂停或归零。外观可选择字体、8–160 像素字号、粗斜体、描边、字距、颜色及不透明度；位置支持数字框逐像素调节和滑块粗调。显示文字用 `{time}` 插入时间，例如 `本次用时 {time}` 或 `Run time {time}`，支持中英文与换行。外观和三个控制快捷键保存；每次启动从零暂停。
- 设置齿轮 →“网络开关…”：选择程序文件或整台电脑，启用后设置断开／恢复快捷键。指定程序按完整文件路径匹配所有实例。首次断网需要 Windows 管理员授权，仅网络助手提权；本次应用会话可复用助手。设置菜单中的“恢复网络”和紧急停止键也可解除限制。退出应用、主进程或助手意外结束后自动解除临时过滤。启动应用不会自动断网。

普通计时器使用穿透鼠标、不抢焦点的置顶窗口。RTSS 模式需安装并运行
**RTSS 7.3.7 或更新版本**，通过现有 RTSS 绘制到游戏画面；不捆绑 RTSS，不安装驱动。
OBS 使用“游戏捕获”时，普通窗口位于游戏渲染之外；录入 RTSS 需启用
“捕获第三方叠加层”，并满足游戏兼容性。参考 [OBS 官方说明](https://obsproject.com/kb/game-capture-source)。

退出当前 InputStitch 后再运行测试版。Beta 和正式版共用配置；旧宏配置可直接读取。
新方环通过可选 XML 字段保存；1.5.2 能读取同一文件并使用圆环作为回退形状，宏不丢失。

## 准星取舍

这次增加参数化的缺角方环。它直接满足四角留空、边宽和缺口调节的需求，沿用现有操作。
完整绘图编辑器需要额外的绘图、选取、对齐、文件格式和编辑历史，暂不纳入此版。

## 自动化结果

按影响范围运行 **16 个不同回归套件（共 42 个可用套件）**，全部通过；本次没有执行完整正式发布验收。

| 范围 | 结果 |
|---|---|
| 源码清单、版本及 Beta 更新策略 | PASS；66 个生产源码文件，清单／项目一致 |
| 修饰键、运行协调、输出所有权 | PASS；协调 55 项，所有权 303716 项 |
| 旧配置／生产力回归 | 802 项通过；原 1.4.3、1.5.0 数据保存重开 |
| UI 保护、高级触发、输入管线 | 26／163／24 项通过 |
| 鼠标选择、录制、批量时间编辑 | 209 项通过 |
| 准星原流程与实际分层窗口 | 182 项通过 |
| 新方环 | 1300 项通过；旧 8 样式 576 组图像逐字节一致 |
| 新方环窗口 | 65 项交互及 4672 项布局通过，中英／100–200%／默认、紧凑、恢复尺寸 |
| 工具保存、冲突、编辑与退出保护 | 121 项通过；含模拟 UAC 迟到、排队后退出与非法快捷键 |
| 计时器／网络设置界面 | 29933 项检查、32 个视图通过，中英／100–200%／保存取消／模态入口 |
| 宏创建到保存运行 | 142 项通过 |
| 主界面密度 | 3328 项通过，涵盖中英文、不同窗口大小和缩放 |
| 计时器核心／渲染／RTSS 协议 | 66 项通过；单调时钟、七天模拟计时、暂停归零、双语、XML、锁与槽位、图集复用 |
| 计时器实际桌面窗口 | x64／x86 各 77 项通过（66 核心＋11 原生）；真实像素、穿透、不激活、UI 阻塞回算、显隐和资源释放 |
| RTSS 实际 D3D9 | x64／x86 均通过；完整中英文、字号与范围、显隐、逐像素位移，另一独立 OSD 客户端画面变化 0 像素 |
| 网络事务与管道 | x64／x86 各 25 项通过；故障回滚、取消授权、并发、父进程／助手退出、32／64 位 ABI |
| 真实网络 | 专用 exe 的 IPv4 TCP／UDP 新连接和既有流阻断／恢复通过；另一 exe 持续联网；主进程与助手意外结束后恢复通过 |
| 配置回退 | 实际已发布 1.5.2 程序能读取中英文 Beta 配置，宏及时间参数保留，方环回退圆环 |
| 最终打包 | x64／x86 编译、版本验证、2.5 秒真实消息循环、设置恢复、暂停计时器、退出窗口清理均通过；正常配置／设备文件哈希不变 |

RTSS 的图集采用 2 次幂纹理；读取前台游戏缩放，以透明边距补偿采样相位。
不使用会改变共享字体／缩放的 FNT 声明，也不修改 RTSS 或游戏配置。
真实位移对照的像素误差为 0；另一 OSD 客户端的实际区域变化为 0。
位置滑动和显隐复用字形图片，计时更新不重复写 PNG。

桌面文字缓存的本机 60 秒观测（两个样式各 30 秒、各 600 帧，无强制 GC）：
默认样式单核 CPU 0.677%、每帧 0.342 ms；96 像素双行中英、粗斜体、6 像素描边
及 2 像素字距为 2.812%、每帧 1.685 ms。GDI 均为 3→3，缓存 12／14 项。
这是绘制缓存的观测值，不是整个程序的 CPU 基准。

## 验证范围

- 为保持工作连接，本机没有执行整机断网；整机模式的无应用条件过滤、四层事务和恢复路径通过注入测试。实际网络验收只命中专用测试 exe。
- 本机没有非回环 IPv6 地址，未执行 IPv6 真实连通性验收；四个 IPv4／IPv6 WFP 层的原生提交已随专用程序验收执行。
- 已验证实际 RTSS D3D9 绘制；本机未安装 OBS，没有进行 OBS 实录。特定游戏／渲染 API 的兼容性按该环境验证。
- RTSS 独立基准未出现时，验收程序返回 `BLOCKED`／2，不把未绘制的环境计作产品通过。

## 复验命令

```powershell
./tests/Run-Tests.ps1 -Suite SourceManifestTests,ReleasePolicyTests,TimerTests,NetworkControlTests,ToolIntegrationTests,CornerCutCrosshairTests,CornerCutDialogTests
./artifacts/tests/ToolIntegrationTests.exe --ui
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
./tests/Run-ProductAcceptance.ps1 -DensityOnly
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x64
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x86
./tests/Run-TimerAcceptance.ps1 -Backend Rtss -Architecture x64
./tests/Run-TimerAcceptance.ps1 -Backend Rtss -Architecture x86
./tests/Run-LiveNetworkAcceptance.ps1       # 只编译，不改变网络
./tests/Run-LiveNetworkAcceptance.ps1 -Run  # 仅专用 exe，需要系统授权
./build.ps1 -SkipSourceArchive
./tests/Run-PackagedSmoke.ps1 -Architecture x64
./tests/Run-PackagedSmoke.ps1 -Architecture x86
```

## 产物

| 架构 | 字节 | SHA256 |
|---|---:|---|
| x64 | 2116608 | `aeca8a4b90f9dd2695fb3fb5467e8e4e42a1d8e897c3e4d5bc1ad0718bbd22b7` |
| x86 | 1946624 | `f091874553a277ce0a5d437a015003693625e0adbd507ff4335a8cdf962532b3` |

x64 为 2.02 MiB，比 1.5.2 增加 69 KiB（约 3.45%）。没有新增随程序分发的依赖。

## 参考

- 本机 EliteSpeedrunTool v12.0.1.72 的计时器、RTSS 文字设置和更新记录：参考其显示方式分离、文本模板、控制键和 WFP 网络开关；没有复制应用代码。
- [QuellGTA 官方仓库](https://github.com/mageangela/QuellGTA)及其更新记录：参考屏幕计时、RTSS 与网络功能的使用方式，没有复制 GPL 应用代码。
- 本机 RTSS 官方 SDK：`RTSSSharedMemory.h`、OverlayEditor 图片标签及 DesktopOverlayHost D3D9 示例。D3D 验收需要真实图元，空清屏不能证明 RTSS 显示。
- [Microsoft WFP 动态会话](https://learn.microsoft.com/en-us/windows/win32/api/fwpmtypes/ns-fwpmtypes-fwpm_session0)、[ALE 重新授权](https://learn.microsoft.com/windows/win32/fwp/ale-re-authorization)。
