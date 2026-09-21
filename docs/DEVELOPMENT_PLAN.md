# InputStitch development plan (living document)

Updated: 2026-09-21

This is the source of truth for "what next". Companions: `ROADMAP.md` (milestones), `PLAN.md` (current
milestone detail), `HANDOFF.md` (machine-to-machine continuation), `docs/ARCHITECTURE_REVIEW.md`
(findings/risks), `docs/PRODUCT_STRATEGY.md` (positioning).

## Direction

North star (unchanged): **a deterministic input orchestration and macro platform for Windows** — compete on
determinism, observability and reliability, not on device-feature count.

Direction updates from the 2026-09-21 research:

1. treat the retired ViGEm stack as a **compatibility backend**, never as the platform;
2. give the input side the same provider seam the output side already has, then adopt one modern provider
   (GameInput or SDL3) and prove the seam with a second implementation;
3. pay down the platform-hygiene debt first (five duplicated source lists, an 11.9 k-line file, real-time
   work on the UI thread) because every future feature currently pays that tax.

## Research summary (verified 2026-09-21)

| Item | Fact | Implication |
|---|---|---|
| ViGEmBus / ViGEmClient | archived, last push 2023-11 / 2023-09; BSD-3-Clause / MIT | keep as compatibility backend; second backend stays strategic |
| HIDMaestro | active (pushed 2026-09-20), **MIT** | leading candidate second virtual-output backend |
| VIIPER | active, repository **GPL-3.0** (standalone server client libraries reported MIT) | license review required before any use |
| PadForge | active, **CC-BY-NC-SA-4.0** | reference only, never a code source |
| DS4Windows (hbashton) | active, GPL-3.0 | reference only |
| HidHide | active (2026-07-05), MIT | keep as the device-hiding backend |
| SDL | active, zlib, latest **3.4.16** (2026-09-02) | candidate broad-device input provider |
| Microsoft GameInput | PC via NuGet, Windows 10 19H1+, superset of XInput/DirectInput/RawInput/HID, haptics/sensors/force feedback, polling or callbacks | strong candidate provider for a Windows-only product |
| .NET Framework | 4.7.2 / 4.8 / 4.8.1 still supported as OS components | no forced retarget; keep 4.7.2 until a concrete blocker |

## Horizons

### H1 — Platform hygiene (current focus, before the next feature wave)

1. **Single production-source manifest** consumed by `build.ps1`, `tests/Run-Tests.ps1`,
   `tools/InputLab/build-acceptance.ps1`, plus a drift-guard test against the csproj list (F3).
2. **Performance probe + foreground-condition cache** with measured before/after numbers (F2).
3. **Dedicated input/tick thread** for XInput polling, router tick and activator tick; UI stays marshalled;
   require jitter/latency numbers before/after (F1).
4. **Incremental `MainForm` decomposition**: make the class `partial`, then extract Layer →
   diagnostics/observation → controller takeover → macro editor, one commit per extraction, no behavior
   change, full regression each time (F4).
5. Optional, evidence-driven: string-table groundwork for localization (F5).

### H2 — Product capability (repository order, updated)

1. **Profile/context integration** on DeviceKey + Condition — implement
   `docs/PROFILE_CONTEXT_DESIGN.md` with these defaults (decided here):
   layer-level context condition; precedence `momentary hold > context layer > persistent layer`;
   conditions limited to foreground process/title + stable `DeviceKey` in stage 1; management entry in
   "manage layers"; active-by-context state visible in runtime observation and diagnostics.
2. **Activator/Condition stage 2** (Short Press, Triple Press, Turbo/repeat, another-input-held, richer
   analog regions) — only with explicitly defined lifecycles and the same shared runtime.
3. **`IInputProvider` + broad controller input**: seam first (XInput ported behind it, behaviour-neutral),
   then **SDL3** as the second provider — decided 2026-09-21 in `docs/INPUT_PROVIDER_EVALUATION.md` (zlib,
   x86+x64, no installer, no OS-floor change). GameInput is deferred to an explicit user decision: its NuGet
   package is native-only, ships an MSI redistributable and has no x86 library.
4. **Second virtual-output backend** behind `IVirtualGamepadBackend` — HIDMaestro first (MIT); VIIPER only
   after a license decision.

### H3 — Distribution, trust, governance

1. Code-signing decision (SmartScreen trust) and whether an installer is needed at all.
2. One-click support bundle (diagnostics export) for user-facing troubleshooting.
3. Repository license decision before plugins, third-party backends or community contributions.
4. Hardware acceptance lane stays parallel: physical controller + HidHide + real game.

## Decision register

**Decided (engineering, this plan):**

- keep .NET Framework 4.7.2 as the shipping runtime until a concrete blocker appears;
- ViGEm remains the production backend until a second backend is implemented and verified;
- one authoritative source manifest + drift-guard test;
- real-time input work moves off the UI thread, but only with before/after measurements;
- profile/context integration follows the defaults listed in H2.1.

**Needs the user (product/money/legal):**

- code-signing certificate (cost, publisher identity);
- repository license choice (affects VIIPER/HIDMaestro and any plugin/community plan);
- a physical XInput controller for the parked hardware-acceptance lane;
- whether gyro/haptics/DualSense-specific work is in scope at all.

## Working rules

- runtime/UI change → full `tests/Run-Tests.ps1` + release build verification; evidence into `HANDOFF.md`;
- refactors → no behavior change, one extraction per commit, full regression;
- local commits only until the user asks to push or publish;
- keep `docs/ARCHITECTURE_REVIEW.md` closure table updated as findings are closed;
- a release still requires: version/changelog/release-notes update, `build.ps1` verification, website
  version check, and the publish/mirror workflows.

## Stage log

- 2026-09-21: plan created from the architecture review and the external research above.
- 2026-09-21: **H1.1 done** — single production-source manifest + drift guard (`80eb025`).
- 2026-09-21: **H1.2 done** — foreground identity cache with TTL + concurrency test (F2 closed).
- 2026-09-21: **H1.3 done** — input pipeline moved to a dedicated, deadline-paced thread
  (`InputPipelineScheduler`) with UI-coalesced activator ticks; cadence statistics (stalls, max gap, max
  duration) are exposed in Runtime Observation and Diagnostics (F1 closed, real-world numbers pending).
- 2026-09-21: **H2.1 core done** — layer context conditions (foreground process/title + stable DeviceKey,
  shared evaluator, precedence `momentary > context > persistent`, deterministic list order, diagnostics and
  a 20-check suite). Remaining: the layer-management UI entry for editing/clearing a context condition.
- 2026-09-21: **H2.1 done** — the layer-management menu now offers "设置上下文条件… / Set context
  condition..." (shared condition dialog in conditions-only mode) and "清除上下文条件 / Clear context
  condition"; the layer context suite covers 22 checks. Next in H1 is the incremental `MainForm`
  decomposition, then H2.2 (activator/condition stage 2) and H2.3 (`IInputProvider`).
- 2026-09-21: **H1.4 started** — `MainForm` is `partial`; the Layer subsystem moved to
  `MainForm.Layers.cs` (576 lines) with identical x64/x86 binary sizes and a fully green regression as the
  behaviour-neutrality evidence. Next extractions: diagnostics/observation, controller takeover, macro
  editor.
- 2026-09-21: **H1.4 step 2** — Runtime Observation / Diagnostics builders moved to
  `MainForm.Diagnostics.cs` (248 lines); `InputStitch.cs` is now 11,379 lines with again identical
  x64/x86 binary sizes and a green regression. Next extractions: controller takeover, macro editor.
- 2026-09-21: **H2.2a done** — Activator Short Press implemented with an explicit lifecycle (release inside
  the window fires once; holding past the window disarms; ineligible press or condition loss cancels;
  wheel stays edge-only). `ActivatorConditionTests` grew 55 -> 73 checks. Remaining stage-2 candidates
  (Triple Press, Turbo/repeat, another-input-held, richer analog regions) stay deferred until their
  lifecycles are defined.
- 2026-09-21: **H2.3 decided** — `docs/INPUT_PROVIDER_EVALUATION.md` compares GameInput and SDL3 with
  verified package/licence/OS facts and selects **SDL3** as the second provider behind a new
  `IInputProvider` seam (XInput remains provider #1). GameInput is deferred pending a user decision about an
  installer/redistributable. Next implementation step: the behaviour-neutral seam plus XInput adapter.
- 2026-09-21: **H1.4 step 3** — controller-takeover coordination moved to
  `MainForm.ControlledReplacement.cs` (295 lines); `InputStitch.cs` is now 11,101 lines with identical
  x64/x86 binary sizes and a green regression (including the 52 controlled-replacement checks). Remaining
  extraction: the macro editor.
- 2026-09-21: **H1.4 step 4** — the main-window builder and tray icon moved to `MainForm.Ui.cs`
  (1,233 lines); `InputStitch.cs` is now 9,885 lines with identical binary sizes, a green regression and
  green Settings smoke tests. Remaining cohesive split: editor/runtime logic inside the remaining file.
- 2026-09-21: **H2.3 step 1 done** — `IGamepadInputProvider` (`InputProvider.cs`) plus the XInput adapter;
  the router consumes the provider device set (classic XInput slots plus synthetic indices for future
  providers) instead of a fixed 0..3 range, clearing a disappeared device exactly once.
  `InputProviderSeamTests` (15 checks) proves the seam with a fake provider; existing router suites are
  unchanged. Next: the SDL3 provider (native P/Invoke subset + device identity evidence).
- 2026-09-21: **H2.3 step 2a done** — `ISdl3Api` contract plus `Sdl3InputProvider`: synthetic device indices
  above the XInput range, SDL3→app state mapping (button mask, Y-axis inversion, trigger scaling), button and
  trigger edges with the XInput hysteresis, hot-plug with index reuse and release-on-disconnect, and
  Microsoft-family duplicate suppression. `Sdl3ProviderTests` (29 checks, fake SDL3 API) covers all of it.
  Next: the native P/Invoke implementation of `ISdl3Api`, the DLL loading/embedding step and MainForm wiring
  (real-device verification stays on the hardware-blocked lane).
- 2026-09-21: **H2.3 step 2b done** — `Sdl3NativeApi.cs` implements `ISdl3Api` against the real SDL3 runtime:
  dynamic `LoadLibrary`/`GetProcAddress` binding (missing DLL degrades gracefully), `SDL_Init(SDL_INIT_GAMEPAD)`,
  `SDL_UpdateGamepads`/`SDL_UpdateJoysticks`, `SDL_GetGamepads` (freed with `SDL_free`), the `...ForID`
  descriptor getters, and per-control state reads through `SDL_GetGamepadButton`/`SDL_GetGamepadAxis` (the
  API shape SDL 3.4 actually provides). Verified against the real SDL3 3.4.16 x64 runtime downloaded from the
  official release (SHA-256 `1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0`):
  `device count=0; provider status=ready; devices=0` with the optional smoke enabled, and the suite skips it
  cleanly when `INPUTSTITCH_SDL3_DLL` is unset. Still to do: choose how SDL3.dll ships (committed third-party
  folder vs restore script vs embedded resource), wire the provider into MainForm, add device-identity
  evidence and run Input Lab; real-device verification stays on the hardware-blocked lane.
- 2026-09-21: **H2.3 step 2c done** — `CompositeInputProvider.cs` merges providers into one shared device
  index space (XInput 0..3, SDL3 ≥4), routes state reads to the owning provider, forwards child edges with
  their original indices, combines topology versions and disposes children. `MainForm` now runs a composite
  of XInput + SDL3, so a machine without SDL3.dll keeps the exact XInput-only behaviour while the optional
  lane activates automatically when the runtime is present; diagnostics report
  `InputProviders: xinput=active; sdl3=<ready|unavailable>(<status>)`. SDL3 devices are also baseline-safe:
  the first observation of a device never synthesizes edges, and pinned triggers can no longer be satisfied
  by an unrelated pad (`XInputInputService.IsTriggerSatisfied` is strict for out-of-range pinned indices).
  `CompositeInputProviderTests` (15 checks) and the extended SDL3 suite (30 checks) cover the rules.
- 2026-09-21: **H2.3 step 2d done** — `scripts/Restore-Sdl3.ps1` downloads the pinned SDL3 3.4.16 release
  assets for x86/x64, verifies both the archive and the extracted DLL SHA-256 values, extracts `SDL3.dll`
  plus its `LICENSE.txt` into `third-party/SDL3/<arch>/` and is idempotent. The folder is gitignored, the
  source archive excludes it deliberately (build.ps1 now packages only the committed ViGEm third-party
  folder, so release artifacts stay deterministic), and the SDL3 test suite auto-detects the restored DLL so
  a developer machine runs the real-runtime smoke while CI skips it. Remaining for this item: the end-user
  shipping decision (bundle/embed/optional).
