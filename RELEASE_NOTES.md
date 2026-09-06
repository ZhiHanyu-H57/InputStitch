# InputStitch 1.1.1-beta.3

## 简体中文

公开测试版，**正式版 v1.1.0 与自动更新入口不变**。手动下载，不覆盖旧发行记录。

### 前台热键修复

旧版会把“鼠标悬停在按钮、列表、表格上”也视为正在编辑，直接忽略热键，状态却可能保持空闲。本版移除此已确认的静默拦截路径：

- 空闲时仅悬停、选中按钮/列表不再阻止启动；执行前将非编辑焦点移到安全输入控件。
- 实际编辑名称、备注、数值或下拉设置，以及模态编辑窗口，仍受保护；明确显示原因而不是空闲。点击空白区域/分区标题/底部可结束编辑。
- 每次物理触发和排队执行前刷新保护状态，避免短暂沿用旧焦点/前台状态。
- 已运行宏仍保护有焦点的操作控件；鼠标/滚轮宏仍防止误点或滚动鼠标指向的控件，首步也受保护。没有关闭编辑保护。

### 后续稳定性开发：运行诊断

诊断窗口新增最近 64 条内存事件：匹配热键接受/拦截、执行启动、首次输出提交、停止请求、完成/取消/错误。不会记录未匹配的普通打字，不逐帧/逐循环写盘，退出后清空。分享诊断前仍应检查其中已有的配置路径等信息。

“首次输出提交”仅表示输出调用返回，不证明游戏接收。可用于区分未触发、被保护拦截、已经运行三个阶段。

### 验证和边界

新增 23 项 UI 保护/诊断检查，含真实控件分类和 1 万次并发诊断事件的容量测试；原有键盘、闲置手柄、修饰键、更新故障、双语设置及手柄参数测试保留。测试不等同于真实桌面/游戏完整重放，不宣称排除所有偶发故障。

测试前退出旧版，备份整个 %APPDATA%\InputStitch。Beta 与正式版共用配置；不同时运行。Beta 不自动检查更新，手动检查经确认打开 GitHub。EXE 未签名，手柄输出需要 ViGEmBus。

建议依次测试：悬停按钮时触发；选中宏列表后触发；编辑名称时确认显示保护；点击空白后再触发。若仍异常，在关闭程序前复制诊断中的 RecentRuntimeEvents。

## English

Public opt-in Beta. **Stable v1.1.0 and automatic-update assets are unchanged.**

- Remove silent idle-hover/non-editing-focus hotkey blocks. Transfer safe non-editor focus before execution; refresh protection on physical trigger and before queued dispatch.
- Focused text/numeric/combo editors and modal editing stay protected with a visible explanation. Empty areas, section labels and footer clicks finish editing. Running macros retain control-focus protection; mouse/wheel output keeps pointer-hover protection, including its first step.
- Continue reliability work with a bounded, memory-only 64-event runtime trace in diagnostics: matched-trigger decisions, worker start, first output submission, stop, completion/cancellation/error. No ordinary typing or per-loop disk log. Submission does not establish game acceptance.
- Add 23 UI-policy/control/trace checks including 10,000 concurrent events; existing regression suites remain. These are not full live desktop/game playback tests and do not establish that every intermittent failure is eliminated.
- Close InputStitch and back up %APPDATA%\InputStitch before testing; versions share configuration. Manual Beta downloads only; unsigned EXEs; ViGEmBus required for gamepad output. If the issue persists, copy RecentRuntimeEvents from diagnostics before closing.

## Files / 文件

- InputStitch-1.1.1-beta.3-Windows-x64.exe
- InputStitch-1.1.1-beta.3-Windows-x86.exe
- InputStitch-1.1.1-beta.3-Source.zip
- InputStitch-beta.xml
- SHA256SUMS.txt
