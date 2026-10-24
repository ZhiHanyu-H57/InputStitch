# InputStitch 1.6.0-beta.7 — 自动化布局、截图键与窗口准备

2026-10-24，本地测试版。已验证实现：`c4cd0bf79ae8becaa37467f7fe3abf8704e36aa9`。

## 修复内容

“自动化”页面有两处独立的空白来源：目标设置中数字输入框的原生 DPI 缩放会反复放大边距；
闲置手柄面板的嵌套自动布局会把换行测量结果保留为多余高度。
现在目标设置按内容排列，明确数字框的宽度、边距和设置分组的内边距；
闲置面板按可用宽度计算每行及说明文字的高度，按钮／摇杆／扳机切换不会留下空行。

同一原生 144 DPI（150%）窗口的配对结果，中英文一致：

| 项目 | beta.6 | beta.7 |
|---|---:|---:|
| 切换后等待：整行高度 | 178 px | 49 px |
| 数字框宽度 | 225 px | 150 px |
| 数字框上／下边距 | 72／72 px | 4／7 px |
| 目标窗口与方案：分组高度 | 375 px | 226 px |

主界面的输入接收区和运行按钮原先会吞掉所有按键，包括 PrintScreen。
现在这两处放行 PrintScreen 及其修饰键组合，Enter、空格等仍保留原来的界面防误触处理。
全局宏触发、按键捕获和紧急停止的处理路径没有改变。
自动回归覆盖两个控件的按键事件及未绑定 PrintScreen 的全局处理结果。
本轮未向用户桌面发送真实截图按键，Windows 截图工具实际弹出未计入验收结果。

下拉框复用与字体、DPI 对应的缩放测量；显示器宽度在打开列表时查询，
避免每次布局都查询；未变化的提示文字和辅助说明不重复写入。
选项文字被原地修改、字体变化和实际展开的列表宽度继续有回归覆盖。

## 窗口准备耗时

同机 x64、原生 144 DPI，旧版／新版交替执行；中英文每个窗口各 3 次，
共 48 个样本。表中为每个版本 6 个样本的中位数，单位毫秒。

| 窗口 | 构造 beta.6 → beta.7 | 完整准备 beta.6 → beta.7 | 准备耗时下降 |
|---|---:|---:|---:|
| 设置 | 819.8 → 482.2 | 1102.6 → 756.4 | 31.4% |
| 步骤编辑 | 280.7 → 183.2 | 497.3 → 381.5 | 23.3% |
| 屏幕准星 | 355.9 → 250.8 | 578.8 → 480.7 | 16.9% |
| 计时器 | 495.8 → 346.7 | 952.4 → 753.1 | 20.9% |

完整准备包括构造、Show／Update 和整窗 DrawToBitmap，测量在独立测试桌面完成。
它衡量窗口准备工作，不是用户点击到屏幕呈现的系统延迟，也不是整个程序的 CPU 降幅。
原始样本保存在交付目录的 evidence/popup-comparison.csv。

## 自动化结果

按改动范围验收，没有运行无关的整机断网、OBS 或真实键鼠输出测试。

| 范围 | 结果 |
|---|---|
| 源码清单 | 424 项通过，67 个生产源码文件 |
| 编辑保护／截图键 | 35 项通过 |
| 工具集成 | 214 项通过，包括运行按钮放行、全局未绑定键处理和原有保护 |
| 闲置手柄逻辑 | 58 项通过，注入时钟与输出观察器 |
| 发布策略 | Beta 通道、稳定更新地址和测试版不自动联网检查通过 |
| 准星窗口 | 65 项交互＋4,672 项布局通过 |
| 全部辅助窗口布局 | 125,683 项、220 组窗口，问题 0；中英文、100%／125%／150%／200% 与缩放字体，默认／缩小／恢复 |
| 工具窗口组合检查 | 76,814 项、72 组窗口；含列表展开、保存／取消、计时器选择与勾选回归 |
| 自动化页面专项 | 540 项、36 组，原生 144 DPI；中英文、三种尺寸、按钮／摇杆／扳机、启用／停用 |
| 原生 150% 设置 | 2,188 项、2 组窗口，全部标签页及缩小／恢复，问题 0 |
| 新手宏流程 | 142 项通过，使用隔离配置及输出观察器 |
| SettingsSmoke | 中英文 4 种布局、默认值、旧 XML、保存的手柄方向／力度编辑通过 |
| 最终程序 | x64／x86 Release Verification、实际启动、消息循环、保存重开、正常退出通过；运行库提取、复用及损坏修复通过 |

布局与流程测试改为**整个测试进程从启动起使用独立 Windows 桌面**，不切换用户桌面。
父进程检查测试窗口是否抢占前台及输入桌面是否改变；上述最终测试全部通过，前台采样均为 0。
子进程结束后释放桌面句柄，没有遗留测试进程。线程级隔离在旧版和新版的窗口清理中均曾停住，
最终进程级隔离通过相同关闭检查及完整矩阵。这些工具只用于开发，不进入发布程序。

本轮后续没有进行 Computer Use 或真实桌面输入，没有关闭正在使用的 InputStitch。
今后进行桌面交互检查前说明开始，完成后立即说明结束并停止操作。
无需再以用户人工试用作为本轮验收前提。

## 复验命令

```powershell
./tests/Run-Tests.ps1 -Suite SourceManifestTests,UiSafetyTests,IdleGamepadTests,ToolIntegrationTests,CornerCutDialogTests,ReleasePolicyTests
./tests/Run-SecondaryLayout.ps1 -ScaledFonts
./tests/Run-SecondaryLayout.ps1 -Native -SettingsOnly
./artifacts/tests/ToolIntegrationTests.exe --ui
./artifacts/tests/ToolIntegrationTests.exe --automation-layout --native-dpi
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
powershell.exe -NoProfile -File ./build.ps1 -SkipSourceArchive
./tests/Run-IsolatedUiScript.ps1 -Path ./tests/SettingsSmoke.ps1
./tests/Run-IsolatedUiScript.ps1 -Path ./tests/Run-PackagedSmoke.ps1 -Architecture x64 -ScriptArguments @('-Architecture','x64')
./tests/Run-IsolatedUiScript.ps1 -Path ./tests/Run-PackagedSmoke.ps1 -Architecture x86 -ScriptArguments @('-Architecture','x86')
```

耗时配对入口为 ToolIntegrationTests.exe 的 `--compare-popups=<旧版程序路径> --native-dpi`。
实际桌面叠加合成和系统截图工具不由离屏位图替代验证。

## 可运行程序

单个 InputStitch.exe，没有新增运行依赖、后台服务或后台轮询。

| 架构 | 大小 | 相对 beta.6 | SHA-256 |
|---|---:|---:|---|
| x64 | 2,178,560 B | +2,560 B（2.5 KiB） | `10C01E1F6EA014C6DB564B0E105F95BC54C0EA1F289436921F23DE338C4C66C4` |
| x86 | 2,008,576 B | +2,560 B（2.5 KiB） | `92E58DD160E1E18BC23F1A2AA367D72E939479A7AE7265A8A6A888121E4F2BFC` |

聊天工作区 outputs/beta160-7 含程序、使用说明、验证记录与布局图。
使用时正常退出旧版，再启动对应架构的 InputStitch.exe，即可继续使用原配置。
当前为本地交付，没有发布到 GitHub。
