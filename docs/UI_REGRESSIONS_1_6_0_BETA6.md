# InputStitch 1.6.0-beta.6 — 设置渲染与计时编辑修复

2026-10-24，本地测试版。已验证实现：`7c99137300efca87365f6fb017420e200ad3e4dd`。

## 已修复

设置“自动化”的嵌套 UserControl 使用了自己的字体自动缩放，叠加外层窗口的 DPI 缩放。
在原生 144 DPI（150%）下可产生重复放大、异常行高和横向裁切。
现在由外层窗口统一缩放一次；内容按实际宽度计算，去掉异步反复回写高度。
手柄方向行和目标选择行保留正常内容高度。常用的“目标窗口与方案”放在自动化页上方，
精简闲置手柄的说明，运行行为和旧配置保持原样。

断网音效页的嵌套自动布局按错误的最小宽度计算了换行高度，并把多余高度留在最后一行。
现改为按内容排布，标签顶端对齐，恢复正常组间距；音效文件框限宽，短按钮按文字尺寸展开，
减少空白。导入、使用内置、试听和独立音量均保留。

计时器列表将“选择编辑对象”和“勾选是否显示”分开处理。
行选择、Shift 扩选和行双击保留所有显示勾选；点击勾选框只改变对应部分，空格仍可切换显示。
批量编辑提示移到列表上方，并显示选中数量。
切换选中项时合并处理通知，保留右侧面板；文字和数字编辑区使用相同高度，
字体、字号等格式控件保持原位。选择本身复用预览图像，右侧内容仍对应实际选中项。

## 自动化结果

按改动范围执行测试。

| 范围 | 结果 |
|---|---|
| 源码清单／工具集成 | 424／205 项通过 |
| 编辑保护／闲置手柄逻辑 | 26／58 项通过；无真实键鼠或手柄输出 |
| 发布策略／构建 | Beta 通道、元数据与 x64／x86 Release Verification 通过 |
| 设置专项 | 216 项、8 组窗口；中英文 100%／125%／150%／200%，反复切页和行高检查 |
| 计时编辑专项 | 688 项、8 组窗口；320 次选择切换、当前内容绑定、位置稳定、预览复用、Shift 行选择、双击和勾选框 |
| 网络音效专项 | 120 项、8 组窗口；组间距、内容高度、反复切页；包含于 704 项／24 窗口的组合回归 |
| 原生 150% 设置 | 144 DPI 实际窗口，中英文 2 组、2,188 项，裁切问题 0；含默认／缩小／恢复及所有标签页 |
| 设置缩放矩阵 | 8 组窗口、8,784 项，100%–200% 与缩放字体，问题 0 |
| 旧配置与设置 | SettingsSmoke 通过；默认值、长目标名称、保存的手柄方向和力度编辑 |
| 最终程序 | x64／x86 实际启动、消息循环、保存重开、正常退出及配置恢复通过；运行库提取、缓存复用和损坏修复通过 |

工具完整布局矩阵在本轮阶段回归中通过 76,374 项／72 组窗口；后续短按钮和设置调整由上述专项复验覆盖。
最终计时编辑与网络代码在后续原生 DPI 修复中没有改变；最后的 DPI 修复只影响闲置手柄设置面板。
原生 DPI 首次复验确实抓到了重复缩放错误，修复后中英文完整重跑通过。
模拟缩放和真实 DPI 现在分别保留验收结果。

选择切换记录共 320 次，含消息处理的平均每次耗时约 23.9–36.6 ms。
每组 40 次切换，格式区 32 次布局，无先清空再禁用面板的过渡，预览图像保持同一缓存。
这是编辑选择路径的计时，不代表整个程序 CPU 占比。

用户按物理 Esc 终止桌面交互检查后，已停止继续操作窗口。交付依据为已完成的自动化结果。
本轮未运行完整正式发布测试，也没有进行真实断网或重跑 OBS；宏输出、网络底层和计时绘制引擎未修改。

## 复验

```powershell
./tests/Run-Tests.ps1 -Suite SourceManifestTests,ToolIntegrationTests,UiSafetyTests,IdleGamepadTests,ReleasePolicyTests
./artifacts/tests/ToolIntegrationTests.exe --ui
./artifacts/tests/ToolIntegrationTests.exe --dialog-regressions
./artifacts/tests/ToolIntegrationTests.exe --dialog-regressions --regression-focus=timer
./artifacts/tests/ToolIntegrationTests.exe --dialog-regressions --regression-focus=settings
./tests/Run-SecondaryLayout.ps1 -Native -SettingsOnly
./tests/Run-SecondaryLayout.ps1 -ScaledFonts -SettingsOnly
powershell.exe -NoProfile -File ./build.ps1 -SkipSourceArchive
powershell.exe -NoProfile -File ./tests/SettingsSmoke.ps1
powershell.exe -NoProfile -STA -File ./tests/Run-PackagedSmoke.ps1 -Architecture x64
powershell.exe -NoProfile -STA -File ./tests/Run-PackagedSmoke.ps1 -Architecture x86
```

新增可复现的隔离窗口检查入口：ToolIntegrationTests.exe --inspect-settings／--inspect-network／--inspect-timer，
加 --native-dpi 使用当前设备原生 DPI；这些入口不加载用户配置，5 分钟后自动关闭。
普通开发检查继续按改动范围选择，正式发布再执行完整验收。

## 可运行程序

仍为单个 InputStitch.exe，67 个生产源码文件，没有新增依赖或后台轮询。

| 架构 | 大小 | 相对 beta.5 | SHA-256 |
|---|---:|---:|---|
| x64 | 2,176,000 B | +512 B | `E60CB6549F2E6801A707E54B9F9577E035FCF634132EB189BB5E7F5B51006835` |
| x86 | 2,006,016 B | +512 B | `FED49EA39B41E399AAB69E9F0D367E970B424CE53FA97404BBCA80EDE8EEEFD8` |

聊天工作区 outputs/beta160-6 含程序、使用说明、日志、截图及 verification.json。
运行前正常退出原有 InputStitch，各版本共用用户配置。当前为本地交付，未发布到 GitHub。
