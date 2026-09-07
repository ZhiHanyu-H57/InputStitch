# Changelog / 更新日志

All notable public changes to InputStitch are documented here.  
InputStitch 的重要公开变更记录在此处。

## 1.2.0

- Promoted the validated 1.1.1 Beta reliability/productivity work to Stable.
- Safer configuration persistence: staged write/flush/deserialize verification, protected replacement, visible save-failure state, and five recent valid backups.
- Added Quick Create for Held Mapping, fixed-count Repeat and Sequence, plus bounded step Undo/Redo.
- Held Mapping supports normal keyboard keys, mouse buttons and standalone left/right Ctrl/Shift/Alt/Win; duplicate enabled triggers intentionally follow macro-list priority.
- Improved foreground/background hotkey reliability, modifier passthrough, lost-KeyUp/Alt+Tab release fallback, and bounded runtime diagnostics.
- Idle Gamepad now uses Raw Input for physical mouse activity, has its own independent Idle target, ignores other-app keyboard/mouse activity for that target, pauses while a configured target is absent, and restarts a full idle interval when it returns. Formal defaults are off / 120 s / left stick down / 150 ms.
- Added a once-per-version startup summary for existing users after upgrading; fresh installs still show only the normal Welcome dialog.
- Improved narrow Settings layout for long Idle target names with one-line ellipsis and full-text Tooltip.
- Stable automatic-update manifest returns to `InputStitch-update.xml`; Beta remains a separate prerelease channel for future development.
- 将 Beta 1–4 已验证的可靠性与易用性改进晋升为正式版：安全配置保存与 5 份有效备份、快捷创建、步骤撤销/重做、按住映射与前后台释放可靠性、独立挂机目标及真实活动识别、窄窗口布局修复，并新增“升级后每个版本只显示一次”的更新摘要弹窗。

## 1.1.1-beta.4 (pre-release / 预发布)

- Added safer configuration persistence: stage, flush, deserialize-verify, atomically replace, and recover without discarding the last valid main config. Keep the five most recent valid config backups and show an explicit Not saved / 未保存 warning on failure.
- Added Quick Create with three focused templates while keeping the existing advanced editor: Held mapping, Repeat an action, and Sequence. Held mapping accepts a single keyboard key (including standalone Ctrl/Shift/Alt/Win) or mouse button as the trigger, passes the original trigger through, and creates an ordinary editable macro.
- Added step Undo/Redo with buttons plus Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z in the step grid. History is limited to 50 operations, clears when switching macros, and restored steps are persisted through the safer config store; text editors keep native undo behavior.
- Duplicate macro triggers remain enabled and intentionally follow macro-list priority: the first enabled macro wins, and Up/Down immediately changes priority. Only an exact Emergency Stop collision disables a newly quick-created macro. Modifier chords and wheel triggers remain unsupported for Hold mode; simultaneous multi-source mappings remain out of scope until input ownership/merging is designed.
- Fixed idle-gamepad activity detection for games that recenter/warp the cursor: physical mouse movement now uses Raw Input instead of cursor/global-last-input changes. Idle gamepad input now has its own independent Idle target in Settings > Automation; other-app keyboard/mouse activity does not reset that target's timer, and the main UI/profile target no longer participates after a one-time beta compatibility copy. If a configured Idle target is absent, idle output pauses completely; when the target returns, a fresh full idle interval starts before any pulse. Formal defaults are off, 120 seconds, left stick down, 150 ms.
- Expanded regression coverage to 323 keyboard checks, 58 idle-gamepad assertions and 342 productivity checks, including standalone left/right Ctrl, Shift, Alt and Win capture/matching, Held Mapping creation, duplicate-trigger list priority, independent Idle target selection/clearing, one-time target migration, target-scoped idle activity, and missing-target pause/resume timing. Updater, UI-safety, release-policy and settings tests also pass. Automated tests do not inject real input or prove game acceptance.
- 新增安全配置保存与最近 5 份有效备份、快捷创建三种模板、步骤撤销/重做；同触发键按宏列表顺序决定优先级；闲置手柄输入不再把游戏重定位鼠标误判为操作，并在“设置 → 自动化”中拥有完全独立的“挂机目标”，不再依赖主界面目标窗口或方案切换逻辑。已设置的挂机目标不存在时完全暂停输出，目标重新出现后从完整空闲时间重新计时。正式默认值为关闭、120 秒、左摇杆向下、150 ms。自动回归已通过，但真实游戏接收仍需实际验收。

## 1.1.1-beta.3 (pre-release / 预发布)

- Fixed a deterministic cause of apparently random foreground hotkey failure: idle hover and non-editing button/list focus no longer silently block starts. Refresh protection on each physical trigger and again before dispatch; transfer non-editing focus to the input sink.
- Keep focused editors/modal protection and running-macro control-focus protection. Pointer-hover protection applies to macros containing mouse/wheel output, including before their first step.
- Show a bilingual reason instead of Idle while editing blocks hotkeys; empty areas, section labels and footer clicks finish editing.
- Add a memory-only 64-event runtime trace to diagnostics: matched-trigger decisions, worker start, first submitted output and completion/cancellation/error. No ordinary typing is recorded; output submission does not prove game acceptance.
- Add 23 UI-safety/trace checks, including bounded storage after 10,000 concurrent events. Stable v1.1.0 and its updater stay unchanged.
- 修复界面前台悬停/非编辑焦点造成的静默拦截，明确提示真正编辑时的暂停原因，并加入有限的运行诊断。未宣称已排除所有机器上的偶发热键问题。

## 1.1.1-beta.2 (pre-release / 预发布)

- Held standalone Shift macros with gamepad-only steps pass the native key through to preserve game sprint. The bilingual UI explains this exception; other modes retain the saved suppression preference.
- Staged update verification, unique old-EXE and main config.xml backups, process-exit checks and recovery on replacement/launch failure.
- Added 30 Shift-policy checks and 43 updater checks with disposable files and injected faults. Real-game acceptance and post-launch crash recovery are not claimed.
- 独立 Shift 的按住手柄宏保留游戏奔跑键；更新安装新增校验、备份和失败恢复。Beta 仍手动下载，正式版 v1.1.0 不变。

## 1.1.1-beta.1 (pre-release / 预发布)

- Added a release-only background deadline for idle gamepad pulses, protected by generation/ownership checks. A stalled UI no longer needs to resume before the owned pulse can be released.
- Added a no-network automatic-update guard for Beta builds. Manual Beta updates use the GitHub Releases page; stable v1.1.0 and its update manifest remain unchanged.
- Centralized release metadata in ReleaseInfo.cs; build and publishing scripts derive names and channels from it. Beta has a separate manifest and version-pinned download links.
- Added repository regression suites, 10,000 simulated start/cancel cycles, UI-stall/stale-callback checks and bilingual settings smoke tests.
- 闲置手柄输入增加仅负责释放的后台定时器和所有权校验，不再等待界面恢复才释放。
- Beta 禁止自动联网检查更新，手动更新转到 GitHub 下载；保留 v1.1.0 正式版和更新清单。
- 将主要版本信息集中到 ReleaseInfo.cs；测试版采用独立清单及指定版本下载链接。
- 将回归测试纳入工程，增加 1 万次模拟启停、界面停顿、过期回调及双语设置检查。

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
