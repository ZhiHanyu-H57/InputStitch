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

### Unified Concurrent Macro Runtime / 任意宏类型多并发 — implemented locally for Stable 1.3.0 / 已本地实现

The Stable 1.3.0 runtime on `main` now supports concurrency across every normal macro execution class: multiple distinct ordinary timed/Toggle macros, multiple Advanced/complex Hold timelines, and state-only Parallel Held Mappings may all coexist. Every worker-backed run owns an independent RunId/SourceId, stop/timing/progress state, immutable Hold-trigger snapshot where applicable, and source-local cleanup; final keyboard/mouse/gamepad state still resolves through Output Ownership.

Advanced Hold is no longer an exclusive runtime class. Hold macros containing keyboard/mouse output, gamepad Press/Up sequencing, non-zero or random delays, or finite repeat counts are classified as **Concurrent Advanced Hold** and run beside ordinary macros and other Hold runs. Simple infinite gamepad-only Down/no-delay Held Mappings keep the lightweight state-only fast path. Physical terminal release and lost-KeyUp reconciliation are tracked per Hold run, so releasing one trigger cannot stop another Hold timeline. Finite Hold keeps its historical semantics: it may complete naturally before the physical trigger is released.

Runtime Observation lists all active worker runs plus Parallel Held sources. The primary Run/Stop control targets the selected macro; Emergency Stop remains global. A single `MacroRuntimeClassifier` is the authority for execution category and the option-B live concurrency/eligibility UI. One active run per `MacroDefinition` remains intentional, and Single-step stays an exclusive diagnostic mode rather than a normal macro category.

Digital overlap semantics remain state-first and deterministic: if one source already owns a digital key/button, another source's pulse/click on that same control does not force a release/repress bounce. The pulse run still completes independently, while the persistent owner keeps the merged control down until the last owner releases.

Stable 1.3.0 候选运行时现在已经支持**任意正常宏类型之间的多并发**：多个普通时序 / Toggle、多个高级/复杂 Hold，以及状态型 Parallel Held Mapping 可以同时存在。复杂 Hold 的物理松键与 lost-KeyUp 恢复按 Run 独立跟踪，一个 Hold 的松开不会停止其他运行实例；最终输出继续统一交给 Output Ownership 合并。

### Layer / 映射层 — designed, deferred until after Stable 1.3.0 / 已设计，延后到 1.3.0 正式版之后

Layer remains designed but is no longer the next feature. The proposed first Layer is still `Base + one active Layer`, initially scoped to Held Mapping eligibility. It must be implemented on top of the multi-worker runtime after Stable 1.3.0 rather than preserving the old single-global-worker assumption. Full design: [`docs/LAYER_DESIGN.md`](docs/LAYER_DESIGN.md).

Layer 设计保留，但不再是下一项功能。第一版仍拟采用 `Base + 一个活动 Layer`，优先筛选 Held Mapping；Stable 1.3.0 发布并验证普通宏多并发后再实现。

### Additional Beta only if needed / 仅在需要时追加 Beta

`1.3.0-beta.2` is not mandatory. Use another public Beta only if Concurrent Macro Runtime or ownership hardening benefits from a separate public validation cycle. If automated, black-box and targeted real-game acceptance are clean, promote directly to Stable `1.3.0` after the concurrent runtime is complete.

## Required ownership regression matrix / Ownership 必测矩阵

The core black-box ownership matrix below is now automated in Input Lab v0.2. The hardened runner passed three consecutive `50/50` runs at the 2026-09-08 breakpoint, did not become the foreground process during 50 ms sampling, and left the user's real configuration hash/size/mtime unchanged. Continue to live-check the same semantics in the actual target game, and keep Alt+Tab/lost-KeyUp/foreground behavior as a real-use requirement because the automated host does not synthesize true hardware state.

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
| Unified macro concurrency (timed/Toggle/Advanced Hold + Parallel Held) | implemented locally; targeted real-use acceptance remains before Stable 1.3.0 | **implemented / acceptance gate** |
| Modifier-chord Held Mapping | repeated concrete need | after 1.3.0 unless reprioritized |
| Layer / mapping layer | Stable 1.3.0 concurrent runtime complete and accepted | after Stable 1.3.0 |
| Conditions / richer groups | clear recurring scenarios after Layer/runtime maturity | later |
| Step-by-step execution | implemented in 1.3.0-beta.1 | completed |
| More controller backends | ViGEm compatibility issue or mature replacement | on demand |
| Controller Aggregation / Gamepad Router | recurring need to merge or redirect multiple physical/virtual gamepads into one game-visible controller; investigate device hiding/filter-driver integration before any implementation | future / after Layer unless explicitly prioritized |
| Installer / signing | external-user installation friction grows | on demand |
| Project license | repository owner decides explicitly | pending |

Not currently committed: cross-platform support, cloud sync, plugin marketplace, scripting language, image recognition, or wholesale UI-framework migration.

## Testing and promotion / 测试与晋升

- Keep and extend automated regression coverage.
- Use `tools/InputLab/` v0.2 for routine black-box keyboard, mouse, foreground/manual Raw Input and XInput acceptance of InputStitch output. It compares low-level hook/Raw Input lanes, visualizes XInput buttons/triggers/sticks and ordered timing, and includes an isolated expected-vs-observed ownership acceptance runner.
- Automated mode is non-activating: Raw Input uses background `INPUTSINK`, and acceptance-only injected `K`/mouse-X2 events are observed then swallowed before reaching the user's current foreground application.
- The automated acceptance runner uses the real InputStitch ownership/runtime plus real `SendInput` and ViGEm/XInput output while isolating user configuration. Before XInput preflight it now validates both ordinary timed overlap and **two delayed Advanced Hold timelines** (F9→K, F10→X2): both Hold runs overlap, Runtime Observation classifies them as Concurrent Advanced Hold, releasing F9 removes only K while X2 remains, and both injected paths are observed. All **18 pre-XInput checks pass**. A controller-backed universal-mix scenario is compiled for healthy XInput environments. The laptop's intermittent controller-stack blocker remains `SUMMARY: BLOCKED` / exit code 2 with `failures=0`, distinct from an InputStitch assertion failure.
- Future test-tool extensions may add target-window message comparison, DS4/DirectInput/HID observation, longer soak/repeated-cycle scenarios and machine-readable report export.
- Replace the old mechanical “30 minutes each” rule with **scenario acceptance + long-running real use**.
- API/output submission success never substitutes for actual desktop/game acceptance.
- Each Beta should have one primary value goal: implement → use for real → fix evidenced problems → freeze → publish.
- Small commits do not require releases.
- Stable promotion requires a clear rollback baseline and remote post-publish verification.

## Version path / 版本路径

Current / 当前：

`1.1.1-beta.4 → Stable 1.2.0 (published rollback baseline)`

`1.3.0-beta.1 — Input Ownership + multi-source Held Mapping + runtime observation + single-step`

Current local / 当前本地：

`1.3.0-beta.1 base + unified timed/Toggle/Advanced-Hold concurrency + Parallel Held + live runtime eligibility UI`

Next / 下一轮：

`targeted real-game universal-concurrency acceptance`

`all 1.3.0 gates pass → Stable 1.3.0`

`Stable 1.3.0 accepted → Layer / mapping-layer implementation`

Long-term product goal / 长期目标：

> **Common tasks simple, advanced tasks explicit, every task reliable.**  
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**
