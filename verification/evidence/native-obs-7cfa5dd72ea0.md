# InputStitch 1.6.0-beta.5 — 窗口响应与设置易用性

2026-10-24，本地测试版。已验证实现：`7cfa5dd72ea01afd295846e1a9e51936ab1ffa9f`。

## 窗口打开速度

排查确认，弹窗创建时的重复布局和下拉项反复测量占用了明显时间。
现在将控件创建、主题和布局更新合并处理，复用未变化的文字测量结果。
下拉项内容、语言和字体改变后会重新测量；缩放和窄窗口继续按实际空间排版。
宏编辑保护和停止机制沿用原逻辑。

同一设备、中英文各 3 次，统计构造窗口到 Show／Update 首次绘制完成的总耗时中位数：

| 窗口 | 修改前 | 修改后 | 耗时降低 |
|---|---:|---:|---:|
| 设置 | 838 ms | 279 ms | 66.7% |
| 编辑步骤 | 610 ms | 138 ms | 77.3% |
| 屏幕准星 | 704 ms | 160 ms | 77.2% |
| 屏幕计时器 | 1534 ms | 256 ms | 83.3% |
| 断网与恢复 | 784 ms | 120 ms | 84.8% |
| 虚拟键盘／鼠标图 | 200 ms | 166 ms | 17.1% |

测试使用隔离的主窗口、配置和输出观察器；每次创建新弹窗并完成首次绘制。
这是同机窗口开销对照，首次启动、设备负载和窗口内容会影响实际耗时。
编辑保护状态检查 100 次合计约 2–4 ms，性能修改集中在布局和文字测量。
原始记录见验证日志中的 dialog-opening-before.log、dialog-opening-final-cache.log。

## 设置操作

- 计时器未启用时显示启用提示，开始、归零及显示控件随可用状态更新。
- 多选计时内容可一起上移／下移，保留选中项及其顺序。
- 从已有快捷键方案首次切换到自定义时，带入已绑定的快捷键和各状态动作；已编辑的自定义规则继续保留。
- 清晰标示“套用首项样式”和“换行”，空快捷键与空规则列表的按钮状态跟随内容。
- 指定程序列表先显示文件名再显示路径，替换／移除按钮跟随选择状态。
- 导入音效显示文件名，悬停可看完整路径；“使用内置”可恢复内置音效。

## 计时绘制开销

分段计时复用未变化的文字表面，仅更新变化的数字；额外保留表面有 4 MiB 总预算，
超出预算时使用临时绘制。编辑器预览在外观没有变化时复用图像。
没有新增后台线程、轮询、依赖或生产源码文件，生产源码仍为 67 个。

以实际 beta.4 与 beta.5 x64 二进制对照，旧模板、分段、多行、负行距、透明度、
进位、七天累计时间与回退时间共 **28 组原始像素哈希完全相同**。
每种动态样例各 3 × 200 帧，记录每帧耗时中位数：

| 样例 | beta.4 | beta.5 |
|---|---:|---:|
| 分段计时 | 0.247 ms | 0.095 ms |
| 大号双语多行 | 1.781 ms | 0.756 ms |
| 负行距 | 1.828 ms | 0.755 ms |
| 未变化的编辑预览（50 次） | 17.348 ms | 0.585 ms |

旧模板绘制约 0.043→0.047 ms，每帧差异约 0.004 ms。
这些数据衡量绘制／预览开销，不能作为整机或整个程序 CPU 降幅。

另做 60 秒持续缓存绘制，两组各 600 帧、无强制 GC：

| 样例 | 实测时长 | 每帧绘制 | 单核 CPU | GDI 起止 | 缓存条目 |
|---|---:|---:|---:|---:|---:|
| 默认分段 | 30.004 s | 0.984 ms | 1.562% | 3→3 | 12 |
| 96px 双语、36px 毫秒 | 30.016 s | 3.140 ms | 6.559% | 3→3 | 43 |

两组均无 GC，未发生 GDI 累积；额外表面缓存上限和释放断言通过。
CPU 是该测试进程相对单核的占比。详细结果保留在 final-render-soak.log。

## 自动化验证

按改动范围执行回归和专项验收；本轮不是完整正式发布验收。

| 范围 | 结果 |
|---|---|
| 源码清单／计时核心／工具集成 | 424／134／205 项通过 |
| 网络控制核心 | 36 项通过；未修改过滤器或提权助手，未进行真实断网 |
| 编辑保护／生产力／输入协调 | 26／802／24 项通过；无真实输入输出 |
| 准星对话框 | 65 项交互＋4,672 项布局通过 |
| 发布策略与构建 | Beta 通道、元数据、x64／x86 Release Verification 通过 |
| 所有次级窗口 | 125,667 项、220 组窗口通过；中英文、缩放字体与不同窗口尺寸，裁切问题 0 |
| 工具窗口布局 | 75,790 项、48 组窗口通过；中英文 100%／125%／150%／200%，默认／缩小／恢复 |
| 原有宏创建到运行 | 142 项通过；隔离配置、运行观察器 |
| 主界面密度 | 3,328 项通过；中英文、100%／125%、3 种窗口尺寸 |
| 设置与旧 XML | SettingsSmoke 通过；保存的手柄向量初始化及编辑通过 |
| 桌面计时器实际显示 | x64／x86 各 147 项通过；像素、双行、穿透、不抢焦点、显隐及资源释放 |
| 最终打包程序 | x64／x86 实际启动、消息循环、保存／重开、正常退出通过；自定义规则、间隔、行距、程序列表、音效及准星配置恢复 |
| 压缩运行库 | 冷启动提取、缓存复用、损坏修复和实际 SDL 初始化通过 |

OBS 32.2.2 原生 x64 D3D9 游戏捕获 **4 段实录／48 帧通过**。
双行、混合字体、100 ms 更新、12 px 行距的 RTSS 计时器被录入，普通置顶计时器在桌面可见且未进入游戏捕获。
关闭第三方叠加层、仅启用普通计时器和无计时器对照通过；12 帧有 12 个不同的计时文字掩模。
OBS 的 17 份用户配置和 RTSS 的 2 份原配置当前哈希与验收基线一致。
临时 RTSS 测试配置已移除，测试进程已退出，测试 websocket 已关闭并清空临时密码。
RTSS 保持原有运行状态。OBS-evidence.zip 只收录本轮实录、帧分析与当前清理检查。

网络底层代码沿用 beta.4；本轮没有进行整机断网或来宾原生网络复验。
先前隔离 IPv4／IPv6 恢复结果见 beta.4 验证报告。

复验命令：

```powershell
./tests/Run-Tests.ps1 -Suite SourceManifestTests,TimerTests,ToolIntegrationTests,UiSafetyTests,ProductivityTests,CornerCutDialogTests,ReleasePolicyTests,InputPipelineIntegrationTests
./tests/Run-Tests.ps1 -Suite NetworkControlTests
./artifacts/tests/ToolIntegrationTests.exe --ui
./artifacts/tests/ToolIntegrationTests.exe --dialog-performance
./tests/Run-SecondaryLayout.ps1 -ScaledFonts
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
./tests/Run-ProductAcceptance.ps1 -DensityOnly
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x64
./tests/Run-TimerAcceptance.ps1 -Backend Desktop -Architecture x86
./build.ps1 -SkipSourceArchive
./tests/SettingsSmoke.ps1
powershell.exe -NoProfile -STA -File ./tests/Run-PackagedSmoke.ps1 -Architecture x64
powershell.exe -NoProfile -STA -File ./tests/Run-PackagedSmoke.ps1 -Architecture x86
./artifacts/tests/TimerTests.exe --parts-performance
powershell.exe -NoProfile -STA -File ./tests/Compare-TimerRendering.ps1 -BaselineExe <beta4-x64-exe> -CurrentExe <beta5-x64-exe> -OutputDirectory <evidence-directory>
```

实际 OBS 流程见 [OBS 验收说明](OBS_TIMER_ACCEPTANCE.md)。

## 可运行程序

两个架构均以 InputStitch.exe 交付，未新增依赖。与 beta.4 相比每个程序增加 4 KiB。

| 架构 | 文件大小 | 相对 beta.4 | SHA-256 |
|---|---:|---:|---|
| x64 | 2,175,488 B | +4 KiB | `21510CFE5029405E08F81F746F7838D5D1E04ABF1444882DFCB44F11AAB8D6D3` |
| x86 | 2,005,504 B | +4 KiB | `965AE5F33C02A8645452467EFAF86B0B86C7D36075FFE92A51326BFC82081E39` |

产物位于聊天工作区 outputs/beta160-5，包含程序、使用说明、验证日志、中英文界面预览和 OBS 实录。
本轮本地交付完成。运行测试版前正常退出原有 InputStitch，各版本共用用户配置。
