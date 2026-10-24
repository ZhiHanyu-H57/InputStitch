# InputStitch 1.6.0

新增可自定义的屏幕计时器和网络开关，完善准星样式与设置界面。

- 计时器可分开显示、编辑时／分／秒／毫秒和自定义文字，支持多行、行距、批量格式、字体、描边、字距、不透明度及精确位置调整。
- 计时控制提供单键、双键、三键及自定义方案，可组合开始、暂停、归零和显隐；显示刷新间隔可调。
- 普通置顶显示与 RTSS 游戏内绘制可选。OBS 游戏捕获录入 RTSS 时，开启“捕获第三方叠加层”。
- 网络开关支持多个指定程序或整机，可分别设置断开、恢复快捷键，选择内置／WAV 音效和音量。网络助手按需请求管理员授权，退出后自动恢复网络。
- 准星增加可调大小、线宽和缺口的“缺角方环”。修复自动化页空白、计时器 Shift 选择误改勾选及主界面吞掉 PrintScreen 的处理，减少窗口准备时的重复测量。

64 位 Windows 选择 x64，32 位选择 x86。正常退出旧版后启动新版，已有宏与设置自动读取；新增工具默认关闭。

[1.6.0 验证记录](https://github.com/ZhiHanyu-H57/InputStitch/blob/main/docs/RELEASE_1_6_0_VERIFICATION.md)

---

This release adds a customizable screen timer, reversible network controls and a corner-cut square-ring crosshair. Timer parts support independent or shared styles, multiline text, adjustable refresh and custom shortcut actions. Network controls support multiple applications, separate disconnect/restore shortcuts and built-in or WAV cues with volume controls. Settings layout, timer selection, PrintScreen handling and window preparation are improved.

Existing macros and settings continue to load; new tools default to off. Choose x64 for 64-bit Windows and x86 for 32-bit Windows. Desktop and optional RTSS timer rendering are available; enable third-party overlays in OBS Game Capture to include the RTSS timer.
