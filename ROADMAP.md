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

### Stable 1.2.0 — published rollback baseline / 已发布稳定回退基线

Stable 1.2.0 has completed promotion and remains the recommended rollback baseline while runtime ownership semantics are tested in public Beta. `releases/latest` and `InputStitch-update.xml` must continue to resolve to Stable 1.2.0 while 1.3.x prereleases are being evaluated.

Stable 1.2.0 已完成发布，在 1.3.x 的运行时所有权改造进入真实使用验收期间继续作为稳定回退基线。Beta 不得修改 Stable 的 `releases/latest` 与 `InputStitch-update.xml`。

### 1.3.0-beta.1 — Input Ownership + multi-source Held Mapping / 当前 Beta

The ownership foundation is now implemented locally for Beta 1. Persistent output is no longer owned by a single “current macro”; each contributor has an independent source identity and a central merge manager computes the final output.

Implemented in Beta 1:

- multiple qualifying Held Mappings can remain active simultaneously;
- at most one ordinary Toggle/timed macro may coexist with those mappings;
- advanced/complex Hold workers remain exclusive rather than joining arbitrary concurrency;
- ordinary macro, Held Mapping and Idle Gamepad output share one ownership core;
- digital outputs use reference ownership;
- analog triggers use maximum requested value;
- stick vectors sum and normalize to the circular range;
- opposing D-pad directions cancel independently per axis while orthogonal directions can remain diagonal;
- Emergency Stop, backend failure and shutdown have fail-closed global cleanup;
- live definition changes stop the affected Held Mapping before mutating its trigger/run/step definition;
- lightweight **Runtime observation** exposes source contributions, merged state, ordinary macro step/phase and stop reason;
- ordinary timed macros support **Single-step / Next Step** execution;
- UI safety suspends only the ordinary macro source rather than globally neutralizing unrelated Held Mappings.

Beta 1 deliberately does **not** enable arbitrary parallel timed macros. Duplicate physical triggers continue to use macro-list priority, and Emergency Stop remains absolute priority.

Beta 1 已完成 Ownership 核心、多 Held Mapping 并行、最多一个普通时序宏共存、明确的摇杆/扳机/数字输出/D-pad 合并规则、轻量运行观察和普通宏单步执行。范围仍刻意限制：不开放任意普通宏并行，高级 Hold 宏保持独占，同触发键继续按列表顺序决定优先级。

### Layer / 映射层 — designed, gated by real-use acceptance / 已设计，等待实测门槛

Layer is **not** enabled in 1.3.0-beta.1. Ownership is the first structural concurrency change and still requires real-game acceptance. Shipping a second structural runtime change in the same first Beta would make failures harder to isolate.

The proposed first Layer is `Base + one active Layer`, scoped to Held Mapping only. A layer switch removes old-layer sources, changes eligibility, and does not synthesize activation for keys that were already physically held before the switch; they must be released and pressed again. Full design: [`docs/LAYER_DESIGN.md`](docs/LAYER_DESIGN.md).

Layer 已完成设计但不在 beta.1 中启用。第一版拟采用 `Base + 一个活动 Layer`，只筛选 Held Mapping；切层会先移除旧层 Source，不会对切层前已经按住的键自动补触发，必须松开后重新按下。只有 Ownership Beta 经真实游戏验收稳定后才开始实现。

### Expected 1.3.0-beta.2 / 预期下一 Beta

Beta 2 is not a pre-committed feature bundle. It exists only if real-use acceptance finds ownership/release/observation problems that need another public hardening cycle. The priority is evidence-driven fixes, not adding speculative concurrency.

If Beta 1 passes real-use acceptance cleanly, the next substantial feature work may move directly to the Layer implementation described above. If Beta 1 exposes issues, Beta 2 remains an ownership-hardening release first.

Beta 2 不预先绑定功能清单：若真实使用暴露 Ownership、释放或观察问题，则先做针对性修复；若 beta.1 实测稳定，再进入 Layer 实现。不会为了版本号而强行添加任意并发或复杂条件系统。

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
| Layer / mapping layer | Beta 1 real-use ownership acceptance | next structural candidate |
| Conditions / richer groups | clear recurring scenarios after Layer | later |
| Step-by-step execution | implemented in 1.3.0-beta.1 | completed |
| More controller backends | ViGEm compatibility issue or mature replacement | on demand |
| Installer / signing | external-user installation friction grows | on demand |
| Project license | repository owner decides explicitly | pending |

Not currently committed: cross-platform support, cloud sync, plugin marketplace, scripting language, image recognition, or wholesale UI-framework migration.

## Testing and promotion / 测试与晋升

- Keep and extend automated regression coverage.
- Use `tools/InputLab/` for routine black-box keyboard, mouse and XInput acceptance of InputStitch output. The v0.1 tool visualizes injected keyboard/mouse events, XInput buttons/triggers/sticks and ordered event timing without changing the product runtime.
- Input Lab reduces repeated game launches during development but does not replace final real-game compatibility validation. Future test-tool extensions may add Raw Input/message comparison, DS4/DirectInput/HID observation and scripted expected-vs-observed scenarios.
- Replace the old mechanical “30 minutes each” rule with **scenario acceptance + long-running real use**.
- API/output submission success never substitutes for actual desktop/game acceptance.
- Each Beta should have one primary value goal: implement → use for real → fix evidenced problems → freeze → publish.
- Small commits do not require releases.
- Stable promotion requires a clear rollback baseline and remote post-publish verification.

## Version path / 版本路径

Current / 当前：

`1.1.1-beta.4 → Stable 1.2.0 (published rollback baseline)`

`1.3.0-beta.1 — Input Ownership + multi-source Held Mapping + runtime observation + single-step`

Next / 下一轮：

`real-use acceptance → ownership hardening beta only if evidence requires it`

`ownership accepted → Layer / mapping-layer implementation`

`reliability closeout → Stable 1.3.0`

Long-term product goal / 长期目标：

> **Common tasks simple, advanced tasks explicit, every task reliable.**  
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**
