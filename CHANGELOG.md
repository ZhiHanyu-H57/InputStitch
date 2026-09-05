# Changelog / 更新日志

All notable public changes to InputStitch are documented here.  
InputStitch 的重要公开变更记录在此处。

## 1.1.0

### English

- Added a full-size virtual keyboard to trigger and step input menus: single/multiple selection, standalone modifiers and Esc, and distinct main/numpad Enter.
- Added modifier-plus-key triggers and ordered chord steps from the virtual keyboard.
- Refined the live controller preview with Xbox 360 and DualShock 4 layouts and clearer highlighting.
- Added optional idle gamepad input in Settings > Automation, disabled by default. Keyboard/mouse, supported physical controllers, and macros restart the timer; editing and macro execution pause idle output. Emergency Stop disables it until explicitly re-enabled.
- Organized settings into tabs, retained configuration compatibility, and published a new release while keeping v1.0.0 available for rollback.

### 简体中文

- 在触发键与步骤输入菜单中加入全尺寸虚拟键盘：单键/多键选择、单独的修饰键和 Esc，并区分主键盘与数字小键盘 Enter。
- 支持虚拟键盘选择“修饰键 + 主键”触发组合，以及按顺序按下、反序释放的组合步骤。
- 优化 Xbox 360 和 DualShock 4 动态预览布局与编辑项高亮。
- 在“设置 > 自动化”中加入默认关闭的闲置手柄输入；键鼠、支持的实体手柄与宏活动会重新计时，编辑和宏执行时暂停，紧急停止后需明确重新启用。
- 设置改为分页、保持配置兼容；独立发布新版，保留 v1.0.0 供回退。

## 1.0.0

Initial public release. / 首个公开正式版。

### English

- Added a DPI-aware, resizable Windows Forms interface with a compact layout for common resolutions.
- Added instant Simplified Chinese and English interface switching.
- Added visual keyboard and mouse macro editing, recording, timing, repetition, and hold-to-run controls.
- Added Xbox 360 and PS4 / DualShock 4 virtual gamepad steps for buttons, sticks, triggers, D-pad, shoulders, stick clicks, and menu controls.
- Replaced raw stick X/Y editing with direction (-180° to 180°) and strength, plus a live highlighted controller preview; existing X/Y configuration remains compatible.
- Added a compatibility-first Xbox 360 default, persistent virtual-device connection, neutralization on Stop/Emergency Stop, and bilingual missing-driver guidance linking only to the official retired ViGEmBus project.
- Added global triggers, configurable Emergency Stop, trigger-conflict checks, and reliable release of macro-held inputs.
- Added selective protection against unintended high-risk Windows shortcut combinations without blocking ordinary gameplay input such as holding `W + Shift`.
- Added scan-code keyboard output, target-window activation, profiles, foreground-app profile switching, macro packages, local backups, diagnostics, and tray controls.
- Added separate x64 and x86 Windows executables and published SHA-256 checksums.
- Added automatic, manual, and disabled update modes. Automatic checking is the default; installation always requires a user prompt and SHA-256 verification against the official GitHub Release manifest.
- Made the update prompt dismissible through Cancel, Escape, or its title-bar close button.
- Deduplicated identical held-controller reports, throttled zero-delay idle loops and status updates, aggregated input-repair logging, and added bounded log rotation.

### 简体中文

- 新增适配 DPI、可调整大小的 Windows Forms 界面，并针对常用分辨率采用紧凑布局。
- 新增简体中文与 English 界面即时切换。
- 新增可视化键鼠宏编辑、录制、时序、循环次数和按住运行控制。
- 新增 Xbox 360 与 PS4 / DualShock 4 虚拟手柄步骤，支持按键、摇杆、模拟扳机、方向键、肩键、摇杆按下和菜单键。
- 将原始摇杆 X/Y 编辑改为方向（-180°～180°）与力度，并加入高亮当前控制项的动态手柄预览；旧 X/Y 配置继续兼容。
- 新增兼容性优先的 Xbox 360 默认类型、虚拟设备常驻连接、停止/紧急停止归零，以及仅指向已停止维护的 ViGEmBus 官方页面的双语缺驱动引导。
- 新增全局触发、可自定义紧急停止、触发冲突检查，以及可靠释放宏所按住输入的机制。
- 新增针对高风险 Windows 特殊组合键的选择性防护，同时允许按住 `W + Shift` 等正常游戏输入。
- 新增扫描码键盘输出、目标窗口切换、配置方案、按前台程序自动切换方案、宏包、本地备份、诊断和托盘控制。
- 提供独立的 Windows x64 与 x86 可执行文件，并发布 SHA-256 校验值。
- 新增自动、手动和关闭三种更新模式。默认启动时自动检查；安装前始终询问用户，并使用官方 GitHub Release 清单执行 SHA-256 校验。
- 更新提示现在可通过“取消”、Esc 或标题栏关闭按钮安全关闭。
- 对相同的手柄保持状态去重，降低零间隔空转与状态刷新频率，聚合输入修复日志，并加入有界日志轮转。
