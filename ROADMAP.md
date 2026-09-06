# Development roadmap / 开发路线

## Release policy / 发布规则

- Stable v1.1.0 and its existing assets/manifest stay unchanged during this development cycle. All new test releases use `-beta.N`, GitHub `prerelease=true`, and `make_latest=false`.
- Public Beta releases and source commits are public. A draft release is only visible to users with repository write access; it does not make source commits on a public repository private.
- Existing stable applications check the stable `releases/latest` manifest only. They cannot discover Beta by manually checking either; use GitHub to download Beta. No forced stable upgrade is needed to introduce this policy.
- Beta applications never automatically check for updates. Manual Check opens the official Releases page after confirmation, not an unattended installer. A future manual Beta update channel can be added separately after validation.
- Beta has `InputStitch-beta.xml` with version-specific URLs, never `InputStitch-update.xml`. File/product versions, artifact names and workflow channel come from `ReleaseInfo.cs`.
- Stable release promotion requires a separate reviewed decision; the current publisher deliberately supports Beta only and refuses stable publication.
- Beta and stable share `%APPDATA%\InputStitch` for now. Back up configuration and run one version at a time. Keeping an old executable is not a substitute for a configuration backup.

正式版 v1.1.0 暂时冻结；新功能先进入公开 Beta，不进入自动更新提醒。公开仓库中的源码提交仍公开可见；若需要仅维护者查看某次发布附件，可使用 Draft，但不是隐藏源码的机制。旧版手动检查也不会发现 Beta，直接从 GitHub 下载即可，不必先升级正式版。测试前请备份配置，并退出另一版本。

## Milestones / 阶段

### 1.1.1-beta.3 — foreground hotkeys and runtime evidence / 前台热键与运行诊断

Completed this slice: remove idle hover/non-editing-focus false blocks; preserve editing and pointer-output safety; refresh protection before dispatch; explain blocked state. Continue the reliability roadmap with a bounded memory-only trace of macro lifecycle events in diagnostics.

本阶段完成前台热键的已知静默拦截路径修复，并继续稳定性路线：加入触发接受/拦截、运行启动、首次输出提交、停止/完成/错误的有限诊断。23 项新增检查覆盖保护策略及并发事件上限。

Limits: tests cover policy/control classification, not full live desktop/game replay. Submitted output is not proof a game accepted it. Direct mapping editor, multi-source ownership, startup health recovery and broader physical-device testing remain pending.


### 1.1.1-beta.2 — Shift and safer replacement / Shift 与更新安全

Implemented: native Shift passthrough for held, standalone Shift gamepad-only macros; clear bilingual UI; staged verification, old-EXE and main config.xml backups, original-process exit checks, recovery on replacement/launch failure. Tests include 30 new Shift checks and 43 updater checks.

已完成：按住独立 Shift 的纯手柄宏保留原始游戏按键；更新暂存校验、旧 EXE/主配置备份、旧进程退出检查与失败恢复。

Limits: GTA acceptance still needs user testing. Beta manual downloads do not invoke the hardened installer; it prepares a future reviewed stable release. Only main config.xml is snapshotted, not profiles/packages. Post-launch crash rollback, power-loss recovery and signature verification remain future work.


### 1.1.1-beta.1 — first reliability slice / 第一批稳定性改进

Implemented: idle pulse release independent of UI refresh, stale callback protection, 10,000 injected start/cancel cycles, regression tests in CI, centralized release metadata, isolated Beta publishing and manual downloads.

已完成：闲置输入后台释放、过期回调防护、1 万次模拟启停、回归测试纳入 CI、版本集中管理，以及不影响正式版的 Beta 发布流程。

Still not claimed: real-time scheduling guarantees, crash/hung-driver recovery, real hardware/game compatibility, a complete updater rollback system, or completion of the entire roadmap.

不宣称已解决：Windows 实时调度、进程崩溃/驱动卡死恢复、全部设备和游戏兼容性、完整更新回退，以及整个路线的全部功能。

### Following reliability Betas / 后续稳定性测试版

- Safer update replacement, executable/config backups and verified recovery; dependency/signature strategy.
- Explicit runtime state and stop reasons; long-running keyboard/mouse/controller tests; sleep/resume and device reconnect matrix.
- Test matrix covering Windows versions, 100/125/150/200% DPI, Chinese/English, foreground/background modes and cooperating controller tools. Report actual measurements rather than claiming universal support.

### Direct mappings / 直接映射

- A first-class hold/release mapping editor, not a user-assembled infinite loop.
- Input ownership and multi-source merging before enabling simultaneous mappings; resolve opposing stick directions and competing macro/idle output.
- Examples: key held -> stick direction/strength; key released -> only that source releases.

### Editing and maintenance / 编辑与维护

- Undo/redo, editable chord groups, batch editing, step-by-step testing and clear execution progress.
- Split UI, runtime, configuration and controller backends incrementally; avoid a wholesale UI rewrite.
- Decide project licensing with the repository owner. Do not silently choose a license.

No dates or stable promotion are promised by this document. Each Beta should be tested before expanding scope.
