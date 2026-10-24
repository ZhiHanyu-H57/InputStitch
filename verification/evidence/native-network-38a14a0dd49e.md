# InputStitch 1.6.0-beta.4 — 自定义计时控制与多程序断网

2026-10-24，本地测试版。实现提交：`38a14a0dd49e2fd227bd5921e00994dd231d5e2f`。

## 使用

计时器“快捷键”页提供三键独立、单键循环、两种双键方案和“自定义”。
自定义可添加多个快捷键，每键分别设置“未开始／计时中／已暂停”的动作。
每个状态可组合归零、开始或继续、暂停、切换开始暂停，以及保持、显示、隐藏、切换显示。
动作按“先归零，再计时动作，再显示动作”执行。例如同一个键可在未开始时开始并显示，
计时中暂停并隐藏，暂停后归零重新开始并显示。每条规则单独编辑、保存；支持 1–16 条。

“显示与位置”可设置 **16–5000 毫秒刷新间隔**和 **−64–160 像素额外行距**。
刷新间隔改变显示频率，内部仍使用单调高精度时钟。“内容与样式”可 Ctrl／Shift 多选，
直接修改选中部分的格式；“全选”用于整体修改，“统一样式”将第一个选中部分的全部格式
复制到其它选中部分。“添加换行”和自定义文字中的 Enter 都可创建多行。
每个时间部分、文字、显示位数和勾选状态仍可分别编辑。

“断网与恢复”的“指定程序”支持添加多个程序，一次选择或逐项添加，最多 16 个。
全部所选程序的各实例一起断开或恢复。断开、恢复可使用不同快捷键，也可设同键切换。
“音效”页可分别导入 **8/16 位 PCM WAV，单声道或立体声**，设置各自 0–100% 音量、
试听或关闭。清除文件后使用内置音效；音效文件路径和音量随设置保存。
音量按音效采样振幅处理。网络状态确认后播放；声音失败不影响恢复。

运行测试版前正常退出当前 InputStitch，各版本共用用户配置。
计时器重开后从零暂停；网络控制启动后保持联网。旧模板、三键设置及单程序配置均可继续读取。

## 自动化验证

按本次改动执行 **9／42 组**回归：SourceManifest、Timer、NetworkControl、ToolIntegration、
UiSafety、InputPipelineIntegration、MacroRunCoordinator、Productivity、ReleasePolicy。
SettingsSmoke 和以下专项验收通过；完整正式发布回归留待正式发布。

| 项目 | 结果 |
|---|---|
| 计时器核心 | 129 项；自定义状态组合、预设、刷新节流、行距、旧 XML、真实设置保存重开和克隆 |
| 网络控制核心 | x64／x86 各 36 组；多目标协议、同一事务回滚、音效格式／独立音量／资源释放 |
| 工具集成 | 186 项；自定义快捷键冲突、编辑保护、导入与清除、批量样式、保存取消 |
| 中英文布局 | 75,790 项、48 组窗口；100%、125%、150%、200%，默认和缩小尺寸 |
| 默认布局 | 样式控件、程序选择、音效及规则编辑可查看；程序列表行高跟随字体，快捷键列随缩放调整 |
| 原有宏完整流程 | 142 项；创建、触发、编辑、保存运行，隔离配置与运行观察器 |
| 实际桌面显示 | x64／x86 各 142 项；双行、分段、鼠标穿透、不抢焦点、显隐和释放 |
| 打包程序 | x64／x86 实际消息循环及退出；自定义规则、刷新／行距、程序列表、音效路径／音量恢复 |
| 配置与启动 | 旧配置兼容，计时器启动暂停，启动不创建网络助手；压缩运行库提取、校验、损坏修复通过 |

复验命令：

```powershell
./build.ps1 -SkipSourceArchive
./tests/Run-Tests.ps1 -Suite SourceManifestTests,TimerTests,NetworkControlTests,ToolIntegrationTests,UiSafetyTests,InputPipelineIntegrationTests,MacroRunCoordinatorTests,ProductivityTests,ReleasePolicyTests -SettingsSmoke
./artifacts/tests/ToolIntegrationTests.exe --ui
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x64
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x86
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File ./tests/Run-PackagedSmoke.ps1 -Architecture x64
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File ./tests/Run-PackagedSmoke.ps1 -Architecture x86
./artifacts/tests/TimerTests.exe --parts-performance
```

## 实录和真实网络

OBS 32.2.2、原生 x64 D3D9 游戏捕获，**4 段录像／48 帧通过**。
双行 RTSS 计时器被录入，普通置顶计时器在桌面可见而未进入游戏捕获；
关闭第三方叠加层和仅启用普通计时器的对照通过。
录制采用 100ms 刷新、12px 行距和较小斜体毫秒，12 帧有 12 个不同的时间文字掩模。
OBS 的 17 份用户配置哈希不变；专用 RTSS UseDetours 测试 profile 已删除。
本轮启动 RTSS 后，其 Config 文件由 RTSS 重写；RTSS 保持运行。
OBS、原生目标和计时器录制进程均已退出。
复验：[OBS 自动验收](OBS_TIMER_ACCEPTANCE.md)。

隔离 Windows 来宾的原生 WFP 验收 **330 项通过，103.7 秒**。
程序 A＋B 在同一会话同时被阻断，程序 C 的 IPv4／IPv6 TCP／UDP 新连接及已有流保持连通；
恢复后 A＋B 原有 UDP 套接字继续可用。随后整台来宾分别验证主动恢复、正常退出、
强制结束主进程、强制结束网络助手，全部恢复连接。
整机断网只在身份门禁保护的来宾执行，宿主整机阻断为 0，宿主 HTTPS 检查返回 200。
来宾与回声端已退出。首次运行遇到来宾测试指令文件短暂共享占用，修复验收脚本的读取等待后
完整重跑通过；生产网络助手的连接时限不变。复验：[隔离网络验收](ISOLATED_NETWORK_ACCEPTANCE.md)。

## 轻量和开销

67 个生产源码文件，新增 1 个网络对话框文件。没有新增驱动、解码库或运行依赖。
刷新间隔不会重建字形图集；数字复用缓存，RTSS 更新不重写 PNG；音效结束释放播放资源，
音效回收定时器在空闲时停止。宏引擎、输入输出和所有权代码未改。

连续 60 秒的两组分段缓存观察，无强制 GC，每组 600 帧：

| 样例 | 每帧平均绘制 | 单核 CPU | GDI 起止 | 缓存条目 |
|---|---:|---:|---:|---:|
| 默认分段 | 0.685 ms | 1.145% | 3→3 | 12 |
| 96px 双语、36px 毫秒 | 3.561 ms | 7.395% | 3→3 | 43 |

CPU 为该进程相对一个核心的比例。桌面和 RTSS 的新增双行间距像素对照通过。

| 程序 | 大小 | 相对 beta.3 | SHA-256 |
|---|---:|---:|---|
| x64 InputStitch.exe | 2,171,392 B（2.07 MiB） | −16.5 KiB | `53C6E48A93CB3A5F021D8416CD02352DA358B72DC2AB42A150F6753AE355E4A5` |
| x86 InputStitch.exe | 2,001,408 B（1.91 MiB） | −2.5 KiB | `209D6FF19E25B14665F8D26EF886F1620A446FC07616CB62A37868576BDD2404` |

产物位于聊天工作区 `outputs/beta160-4`，含程序、验证日志、中英文截图、OBS 实录和
隔离网络证据。GitHub 及 Stable 1.5.2 保持当前发布状态。本地自动化验收已完成。
