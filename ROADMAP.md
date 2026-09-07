# Development roadmap / 开发路线

Updated / 更新：2026-09-08

## Product principles / 产品原则

- Keep basic, frequent workflows simple; advanced, infrequent workflows may require learning but must remain explicit and diagnosable.
- Quick Create and Advanced Edit share one configuration/runtime core.
- Reliability is non-negotiable: configuration integrity, input release, Emergency Stop, clear failure reporting and rollback boundaries come before feature count.
- Real usage findings may interrupt feature development; speculative features must not interrupt reliability closeout.
- Prefer incremental architecture work around the part being changed; avoid whole-application rewrites for aesthetic reasons.

基础高频需求保持低门槛；复杂低频能力可以有学习成本，但必须参数明确、可验证、可诊断。快捷创建与高级编辑共用同一核心。配置安全、输入释放、紧急停止和错误提示优先于功能数量。真实使用发现的问题可以打断功能开发；假想需求不能打断可靠性收尾。

## Release policy / 发布规则

- Stable releases are explicit reviewed promotions. `releases/latest` and `InputStitch-update.xml` belong to Stable only.
- Beta releases use `-beta.N`, GitHub `prerelease=true`, `make_latest=false`, and a separate `InputStitch-beta.xml`.
- Beta builds never perform unattended automatic update checks; manual Beta update opens GitHub Releases.
- Stable builds may use the stable automatic-check/update path with SHA-256 verification.
- Beta and Stable currently share `%APPDATA%\InputStitch`; run only one version at a time and keep configuration backups.
- Every public release must be rebuilt from reviewed source, verified locally, then re-verified after publication: main commit, tag, release state, assets, manifest and hashes.
- No telemetry is added.

正式版与 Beta 分离：Stable 独占 `releases/latest` 与 `InputStitch-update.xml`；Beta 使用 prerelease 与独立 beta 清单。每次正式发布都必须经过本地构建/回归/校验，并在 GitHub 发布后再次核对提交、tag、Release、资产、清单和哈希。

## Completed development cycle / 已完成并归档的开发周期

### 1.1.1-beta.1 — idle release reliability / 闲置输入释放可靠性

Completed:
- idle pulse release independent of UI refresh;
- stale callback/generation protection;
- 10,000 simulated start/cancel cycles;
- centralized release metadata;
- isolated Beta channel and manual Beta downloads.

### 1.1.1-beta.2 — modifier safety and update replacement / 修饰键与更新替换安全

Completed:
- standalone Shift passthrough for held gamepad-only mappings;
- modifier safety policy;
- staged updater verification;
- EXE/config backup and rollback paths;
- updater fault-injection regression coverage.

### 1.1.1-beta.3 — foreground hotkeys and runtime evidence / 前台热键与运行诊断

Completed:
- removed false hotkey blocks caused by idle hover/non-editing focus;
- preserved editing and pointer-output safety;
- visible pause reasons;
- bounded in-memory runtime trace;
- UI-safety/diagnostic regression coverage.

### 1.1.1-beta.4 — safer persistence, productivity and live-use fixes / 配置安全、易用性与真实使用修复

Completed:
- staged/verified main-config replacement, visible save failure and five valid backups;
- Quick Create: Held Mapping, fixed-count Repeat, Sequence;
- step Undo/Redo with bounded history;
- Held Mapping support for normal keys, mouse buttons and standalone left/right Ctrl/Shift/Alt/Win;
- duplicate enabled triggers follow macro-list priority; Up/Down changes priority immediately;
- Shift passthrough UI clarified;
- lost-KeyUp/Alt+Tab hold-release fallback using physical state reconciliation;
- Idle Gamepad physical mouse movement uses Raw Input so game cursor recentering does not fake activity;
- independent Idle target in Settings > Automation;
- typing/mouse use in other apps no longer resets a background target's idle timer;
- configured Idle target absent => idle output pauses completely; target return => full fresh idle interval;
- formal idle defaults: off, 120 seconds, left stick down, 150 ms;
- narrow Settings layout keeps Idle target controls on one line, ellipsizes long target names and exposes full text via Tooltip.

This completes the old near-term plan: **safe persistence / live validation → simple Held Mapping → step Undo/Redo**. Those items are archived, not future work.

以上完成原近期计划：**安全保存与真实验收 → 简易按住映射 → 步骤撤销/重做**。这些任务正式归档，不再作为待办。

## Current release target / 当前发布目标

### Stable 1.2.0 — reliable single-macro baseline / 可靠单宏基线

Purpose: promote the validated Beta 1–4 reliability/productivity work to a clean Stable baseline before changing runtime ownership semantics.

Release-gate work only:
- add a once-per-version startup popup summarizing what changed after an upgrade;
- final regression and live sanity checks;
- build x64/x86 EXEs, Source.zip, `InputStitch-update.xml`, `SHA256SUMS.txt`;
- verify file/product versions and hashes;
- publish main commit + `v1.2.0` tag + public Stable Release (`prerelease=false`);
- re-download/re-check remote assets and stable update manifest.

Do **not** add unrelated new features to the 1.2.0 release candidate. Only release-blocking fixes are accepted: config loss/corruption, stuck input, Emergency Stop failure, core-function breakage, unusable UI, or release/update integrity defects.

Stable 1.2.0 的意义是建立清晰、可靠、可回退的单宏基线。发布候选冻结后只接受阻断级 Bug，不再顺手添加普通新功能。

## Next development cycle / 下一开发周期

### 1.3.0-beta.1 — Input ownership + multi-source Held Mapping

The next structural bottleneck is no longer configuration difficulty; it is the one-active-macro runtime model.

Do **not** solve this by simply running multiple existing MacroWorkers. First introduce explicit source ownership and merged output state.

#### Core model / 核心模型

Each persistent contributor gets a source identity, e.g.:
- Held Mapping W;
- Held Mapping Shift;
- ordinary macro runtime snapshot;
- Idle Gamepad.

A central Output State Manager owns the final emitted state. Releasing one source removes only that source's contribution and must not clear other active sources.

#### Initial merge rules / 第一版合并规则

- digital keyboard/mouse/gamepad buttons: reference-count semantics; remain down while any source owns them;
- triggers: maximum requested value wins;
- sticks: sum X/Y vectors, then clamp/normalize to the circular stick range; opposing directions naturally cancel;
- Emergency Stop: bypass normal ownership, clear every source and force all final outputs neutral/up.

#### Scope limit / 范围控制

The first release enables **multiple Held Mappings**, not arbitrary concurrent timed macros.

Example target:
- W → left stick forward
- A → left stick left
- S → left stick back
- D → left stick right
- Shift → RT
- Ctrl → LT
- Mouse X1 → LB
- Mouse X2 → RB

These mappings may be active simultaneously.

Ordinary timed macros remain single-active in the first ownership release. Duplicate physical triggers still use list priority; if one trigger needs multiple outputs, put those outputs in one Held Mapping instead of starting several duplicate-trigger mappings.

### 1.3.0-beta.2 — ownership hardening + lightweight runtime observation

After ownership works, add enough visibility to debug it without building a large debugger:
- active source list;
- each source's current contribution;
- merged stick/trigger/button state;
- ordinary macro current step/wait state;
- stop/release reason and ownership conflict evidence.

Step-by-step execution may be added if real debugging cost justifies it. A full breakpoint debugger or scripting environment is not planned here.

## Required ownership regression matrix / Ownership 必测矩阵

At minimum automate and live-check sequences such as:

- `W down → D down → W up → D up` (W release must not neutralize D);
- two sources owning the same digital output; one releases while the other remains;
- RT 100% + RT 50% → 100%; release 100% source → 50%; release last source → 0%;
- W + S → neutral Y; A + D → neutral X; W + D → diagonal with circular normalization;
- Alt+Tab and lost-KeyUp fallback while multiple sources are held;
- Emergency Stop clears every source exactly once and leaves no stuck state;
- Idle Gamepad and Held Mapping competing for controller state must follow the same ownership rules.

## Later capabilities / 后续能力

Only schedule these when repeated real use justifies them:

| Direction / 方向 | Trigger / 触发条件 | Priority / 优先级 |
|---|---|---|
| Modifier-chord Held Mapping | repeated concrete need | after ownership stability |
| Conditions / groups / layers | clear recurring scenarios | later |
| Step-by-step execution | macro debugging becomes expensive | medium |
| More controller backends | ViGEm compatibility issue or mature replacement | on demand |
| Installer / signing | external-user installation friction grows | on demand |
| Project license | repository owner decides explicitly | pending |

Not currently committed: cross-platform support, cloud sync, plugin marketplace, scripting language, image recognition, or wholesale UI-framework migration.

## Testing and promotion / 测试与晋升

- Keep and extend automated regression coverage.
- Replace the old mechanical “30 minutes each” rule with **scenario acceptance + long-running real use**.
- API/output submission success never substitutes for actual desktop/game acceptance.
- Each Beta should have one primary value goal: implement → use for real → fix evidenced problems → freeze → publish.
- Small commits do not require releases.
- Stable promotion requires a clear rollback baseline and remote post-publish verification.

## Version path / 版本路径

Current / 当前：

`1.1.1-beta.4 → Stable 1.2.0`

Next / 下一轮：

`1.3.0-beta.1 — Input ownership foundation + multi-source Held Mapping`

`1.3.0-beta.2 — merge hardening + lightweight runtime observation`

`real-use reliability closeout → Stable 1.3.0`

Long-term product goal / 长期目标：

> **Common tasks simple, advanced tasks explicit, every task reliable.**  
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**
