# InputStitch architecture review

Date: 2026-09-21 · Reviewer: Codex (repository evidence + vendor/API facts verified the same day)

Scope: current `main` (post-`v1.4.3`, including the momentary Layer work) — source-only review. No hooks were
installed, no real input was injected and no user configuration was touched during this review.

## Method

- per-file source metrics and type inventory (`InputStitch.cs` = **11,886 lines / 604 KB**, `MainForm` starts
  at line 4746, ~30 top-level types);
- startup/hot-path reading: timers, threads, hooks, input polling, condition evaluation;
- build/test/release pipeline inspection: `build.ps1`, `tests/Run-Tests.ps1`,
  `tools/InputLab/build-acceptance.ps1`, `InputStitch.csproj`, `.github/workflows/*`;
- external facts via GitHub API and vendor documentation (2026-09-21).

## What is healthy (keep it that way)

- **Output Ownership + deterministic concurrency** with 303,716 automated checks — the core differentiator.
- **Fail-closed patterns** everywhere it matters: config writes/backups, updater, controlled replacement,
  Emergency Stop, layer transitions, device identity resolution.
- **Bounded diagnostics**: `RuntimeTrace` keeps the last 64 events, `AppLog` rotates at 2 MB.
- **Structured continuation docs**: `AGENTS.md`, `PLAN.md`, `ROADMAP.md`, `HANDOFF.md`, design notes.
- **Release discipline**: verify → publish → R2 mirror workflows with manifest/hash validation.
- **Small distribution**: single ~850 KB EXE per architecture, no installer, no external runtime install.

## Findings

### F1 — P1, performance/timing: real-time work runs on the UI thread

`gamepadInputTimer.Interval = 10` is a `System.Windows.Forms.Timer`, so its Tick runs on the UI thread and
performs, nominally 100 times per second: XInput polling for 4 slots, device-topology checks,
`gamepadRouter.Tick(...)` and `TickAdvancedActivators()` (condition evaluation for every macro with an
advanced policy). `foregroundTimer` (50 ms) additionally runs `ReconcileHookState`,
`ReconcileHeldTriggerRelease` (`GetAsyncKeyState` probes) and UI-safety refresh on the same thread.

Consequences: `WM_TIMER` coalescing under load, input-latency spikes while menus/dialogs/large list
refreshes hold the UI thread, and no isolation between input timing and presentation.

Recommendation: dedicated input/tick thread (or `System.Threading.Timer` worker) owning polling + router +
activator ticks at a fixed cadence; UI updates stay marshalled. Require before/after jitter measurements.

**Addressed 2026-09-21 in two steps:**

1. *Cadence instrumentation* — `TickCadenceStats` records tick count, stall count (gap greater than five
   times the interval), maximum gap and maximum/average tick duration for the input pipeline and the
   foreground timer, and reports them in Runtime Observation and Diagnostics.
2. *Dedicated input thread* — XInput polling, topology refresh and router mirroring moved from
   `Windows.Forms.Timer` to `InputPipelineScheduler` (`InputPipeline.cs`), a background thread with
   deadline pacing, so UI stalls cannot delay input timing. Activator decisions remain UI-affine: the input
   thread posts a coalesced activator-tick request that the UI thread performs, preserving the existing
   dispatch contract. `tests/InputPipelineSchedulerTests.cs` covers repeated ticking, cadence bounds,
   shutdown, idempotent dispose and error containment.

Remaining evidence: a real-world run should compare the cadence statistics (the numbers only exist while the
application runs; this unattended session deliberately did not start the hooked application).

### F2 — P1, performance: foreground condition evaluation opens a Process per call

`AreMacroConditionsSatisfied` → `NativeWindowFocus.Describe()` performs window text/class reads and
`Process.GetProcessById(...).ProcessName` on every evaluation. It is called for trigger events and inside
`TickAdvancedActivators()` every 10 ms tick for each macro that carries conditions.

Consequences: `macros × 100` process-handle lookups and `TargetWindowIdentity` allocations per second on the
UI thread, amplified by F1.

Recommendation: cache the foreground identity keyed by window handle (invalidate when the foreground window
changes — `MainForm` already tracks `lastObservedForeground`), and measure the win with the performance
probe.

**Closed 2026-09-21:** `DescribeForegroundIdentityCached()` caches the described identity keyed by the
foreground window handle with a 250 ms TTL, so the per-macro/per-tick cost collapses to at most four
window/process lookups per second while title changes stay observable. `tests/ForegroundIdentityCacheTests.cs`
verifies reuse, TTL refresh and concurrent safety (hook threads and the UI thread both evaluate conditions).

### F3 — P1, maintainability: production source set duplicated in five places

`build.ps1` (compile list + source-archive list), `tests/Run-Tests.ps1`, `InputStitch.csproj` and
`tools/InputLab/build-acceptance.ps1` each carry the production file list. Adding or renaming one file
requires editing all of them; `HANDOFF.md` already records an incident where the Input Lab acceptance list
silently fell behind.

Recommendation: one authoritative manifest consumed by every build/test entry point, plus a drift-guard test
that fails when a root `.cs` file is missing from the manifest or when the csproj list diverges.

### F4 — P2, maintainability: 11.9 k-line file with a ~7.1 k-line `MainForm`

`InputStitch.cs` holds ~30 types; every recent feature (the momentary Layer work added 235 lines) lands in
it. Because of F3, extracting a subsystem currently also means editing five lists.

Recommendation: make `MainForm` `partial` and extract cohesive subsystems incrementally — Layer first, then
diagnostics/observation, then controller takeover, then the macro editor — one extraction per commit, no
behavior change, full regression each time.

**Started 2026-09-21:** `MainForm` is `partial` now, and the Layer subsystem moved to
`MainForm.Layers.cs` (576 lines): display/management logic, the layer runtime state machine (toggle,
momentary holds, context conditions) and both layer combo handlers. `InputStitch.cs` dropped from 12,171 to
11,614 lines. Behaviour-neutrality evidence: the x64/x86 Release build produced **identical EXE sizes** to
the pre-extraction build (860,672 / 861,184 bytes) and the full regression suite — including the
reflection-based Layer and layer-context suites — passed unchanged. Remaining extractions:
diagnostics/observation, controller takeover, macro editor.

Second step (same day): the Runtime Observation / Diagnostics text builders moved to
`MainForm.Diagnostics.cs` (248 lines); `InputStitch.cs` is now 11,379 lines. The x64/x86 binaries are again
byte-size identical and the full suite passed.

Third step (same day): controller-takeover coordination (`ShowControllerTakeoverDialog`, the controlled
replacement health/validation/hand-off/stop methods and the gamepad-router readiness helpers) moved to
`MainForm.ControlledReplacement.cs` (295 lines); `InputStitch.cs` is now 11,101 lines. Binaries are again
identical in size (862,208 / 862,720 bytes) and the full suite — including the 52 controlled-replacement
checks — passed.

Fourth step (same day): the main-window builder `BuildUi()` and `BuildTrayIcon()` moved to
`MainForm.Ui.cs` (1,233 lines). `InputStitch.cs` is now **9,885 lines** (from 12,171 before the
decomposition). Binaries are again identical in size, the full suite passed and the Settings smoke tests
(zh-CN/en-US, normal and narrow layouts) still render the moved layout correctly.

Fifth step (same day): the remaining self-contained UI types moved out of the monolith — `StepEditDialog.cs`
(571 lines), `GamepadPreviewControl.cs` (357, preview control plus input-sink helper), `Dialogs.cs` (336,
macro export selection, batch delay, diagnostics and about dialogs) and `SettingsDialog.cs` (591, the
settings dialog plus its gear button). `InputStitch.cs` is now **8,126 lines**, roughly a third smaller than
the 12,171 it started at, and the x64/x86 binaries are again identical in size (883,712 / 884,224 bytes) with
the full suite and zh-CN/en-US Settings smoke green.

### F5 — P2, maintainability/localization: bilingual strings are code, not data

`Localizer.IsEnglish` appears **327 times** as inline ternaries. A third language or a wording audit would
touch logic files.

Recommendation: defer; move user-facing strings behind a small string table only when a third language or a
real translation review appears.

### F6 — P2, extensibility: the input side has no provider boundary

Output already has `IVirtualGamepadBackend` / `IOutputBackend`, but input is concrete (`HookManager`,
`RawKeyboardFallback`, `XInputInput`). Broad controller support, SDL3/GameInput evaluation and non-XInput
devices all need a provider seam first.

Recommendation: introduce `IInputProvider` as part of the planned input work (not as speculative
abstraction): port XInput behind it first, then add a second provider to prove the seam.

**Decided 2026-09-21:** the second provider is **SDL3** behind a hand-written P/Invoke subset (zlib, x86+x64,
no installer, no OS-floor change). GameInput is deferred to a user decision because its NuGet package is
native-only, ships an MSI redistributable and provides no x86 library — the full comparison is in
`docs/INPUT_PROVIDER_EVALUATION.md`.

**Started 2026-09-21:** the seam itself is implemented — `InputProvider.cs` defines
`IGamepadInputProvider` (provider name, topology version, edge callbacks, device indices, state access,
trigger satisfaction, poll), `XInputInputService` implements it with its historical behaviour, and
`GamepadRouterService.Tick` now iterates the provider device set (classic slots plus synthetic indices such
as future SDL3 devices) instead of a fixed 0..3 range, clearing a source exactly once when a device
disappears. `tests/InputProviderSeamTests.cs` (15 checks) proves the seam with a fake provider; the existing
router suites are unchanged. Next: the SDL3 provider implementation.

### F7 — P3, release process: version string lives in many documents

`ReleaseInfo.cs` is the validated source of truth (`scripts/Get-ReleaseInfo.ps1`), but the version literal
also appears in ~10 documents plus `website/download.html` (9 hits). Release choreography is manual and
CI-checked.

Recommendation: keep as-is; track a release checklist in the development plan. Consider generating the
website version fields during publishing later.

### F8 — P3, governance/distribution

- No repository license declared; PadForge is CC-BY-NC-SA-4.0 (reference only), VIIPER is GPL-3.0,
  HIDMaestro is MIT, SDL3 is zlib, HidHide is MIT.
- ViGEmBus and ViGEmClient are archived (last push 2023-11 / 2023-09) — still functional, no maintenance.
- The shipped EXE is unsigned; the SmartScreen warning is documented but remains a user-facing trust cost.

Recommendation: treat license choice and code signing as product decisions (see the development plan's
decision register).

## Verified-clean items (no action)

- `RuntimeTrace` bounded to 64 entries; `AppLog` rotates at 2 MB; no unbounded queues found in hot paths.
- Config/profile/device-registry writes are atomic; corrupt inputs fail read-only instead of overwriting.
- No `TODO/FIXME/HACK` markers in production sources.
- Test harness is strong: per-suite isolated hosts, fakes instead of real devices, 303,716 ownership checks.

## Closure tracking

| Finding | Owner action | Status |
|---|---|---|
| F1 input timing on UI thread | dedicated input thread + cadence stats | closed 2026-09-21 (real-world numbers pending) |
| F2 foreground describe cost | cache (250 ms TTL) + 5-check suite | closed 2026-09-21 |
| F3 duplicated source lists | single manifest + drift guard | open |
| F4 monolith | `partial` + five extractions done 2026-09-21 (Layer, Diagnostics, ControlledReplacement, Ui, dialog types); 12,171 -> 8,126 lines | in progress |
| F5 localization data | deferred | deferred |
| F6 input provider seam | seam + XInput adapter implemented 2026-09-21; SDL3 provider next | in progress |
| F7 release choreography | checklist item | tracked |
| F8 license / signing / backend posture | user decision | needs decision |
