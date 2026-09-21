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
3. **`IInputProvider` + broad controller input**: define the seam, port XInput first, then add one more
   provider. Preferred order: GameInput (Windows-native, covers Xbox + HID + DirectInput) and SDL3
   (zlib, broad device database) as the follow-up if a concrete device gap appears.
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
