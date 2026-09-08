# HANDOFF.md

# Current Development State

Updated: 2026-09-08

## Current version

- Public Beta: `v1.3.0-beta.1`
- Stable rollback baseline: `v1.2.0`
- Branch: `main`
- Public Beta product/runtime commit: `689b4e3ae4e0227dff1ef8de6f33901838cc0cac` (`[publish-beta] Release 1.3.0-beta.1`)
- Current `main` contains post-Beta Concurrent Macro Runtime work intended for Stable 1.3.0; use `git log -1 --oneline` for the current code breakpoint rather than assuming the public Beta tag is current `main`.

For the commit containing this handoff document itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

This avoids a self-referential commit hash inside the file.

## Working on

**Targeted real-game acceptance for the completed Concurrent Macro Runtime, then Stable 1.3.0 promotion.**

The post-Beta multi-run architecture is implemented and automated validation is green. The remaining engineering gate is intentionally narrow: verify the newly affected multi-run behavior in the actual target game—overlapping ordinary timed macros, stopping one while another stays active, coexistence with Held Mapping output, and mixed-state Emergency Stop. Do not mechanically re-test unchanged SendInput/ViGEm recognition. If this targeted real-use gate is clean, prepare Stable 1.3.0; Layer remains deferred until after that release.

## Completed

### Input Ownership core

- Persistent outputs now belong to independent runtime sources.
- Central ownership/merge manager computes final keyboard, mouse, and virtual-controller output.
- Digital keyboard/mouse/controller-button outputs use reference ownership.
- Analog triggers merge by maximum requested value.
- Stick X/Y contributions sum and normalize to the circular stick range.
- Opposing D-pad directions cancel by axis; orthogonal directions may remain as diagonals.
- Backend output failure is fail-closed.
- Emergency Stop clears all sources and forces safe release / controller neutralization.

### Concurrency boundary

- Multiple qualifying Held Mappings may stay active simultaneously.
- Multiple **distinct ordinary timed/Toggle macros** may now run concurrently; one active run per `MacroDefinition` is allowed.
- Each ordinary run has independent `RunId`, `SourceId`, stop event, progress/timing state and source-local held-output bookkeeping.
- Ordinary timed runs may coexist with active parallel Held Mappings and share the same Output Ownership merger.
- Advanced/complex Hold workers remain exclusive for Stable 1.3.0; this is a deliberate safety boundary, not a permanent architectural requirement.
- Duplicate physical triggers still use macro-list priority rather than launching every duplicate mapping.
- A digital pulse/click targeting a key/button already held by another source is deterministically masked instead of forcing a release/repress bounce; persistent ownership remains until the last owner releases.

### Safety and diagnostics

- One ordinary run completing/stopping/failing clears only its own Output Ownership source; unrelated timed runs and Held Mappings remain active.
- The primary Run/Stop button controls the selected macro; Emergency Stop remains the global escape path and stops every ordinary run + Held Mapping source.
- UI safety detects mouse output across all active ordinary runs and suspends/resumes each affected source without global neutralization.
- Editing a live Held Mapping definition stops the affected mapping before mutating critical trigger/run/step state.
- Shutdown and backend-failure cleanup signal every active run and retain ownership-level fail-closed cleanup.
- Runtime observation lists every active ordinary run with RunId/Source/category/phase/iteration/step/logical-held state, followed by Held Mappings, active ownership sources and merged output.
- The macro editor shows live runtime category + concurrency eligibility + reason from the same authoritative `MacroRuntimeClassifier` used by execution; there is no duplicate UI rule table.
- Ordinary timed macros support Single-step / Next Step; single-step remains exclusive among ordinary runs and is interruptible by Stop/Emergency Stop.

### Layer preparation

- Layer architecture has been designed but intentionally not enabled in Beta 1.
- Design document: `docs/LAYER_DESIGN.md`.
- Intended first version: `Base + one active Layer`, initially scoped to Held Mapping eligibility.

### Release and verification

`v1.3.0-beta.1` was published as a GitHub prerelease. Stable `v1.2.0` remained `Latest`.

The release was verified locally, rebuilt from the published Source archive in an isolated temporary directory, and re-downloaded from GitHub for hash/manifest verification.

Immediately before this handoff, the full regression suite was run again on the **Concurrent Macro Runtime build** and passed:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 348 productivity checks;
- 303,677 Output Ownership checks, including the new multi-run matrix;
- zh-CN/en-US Settings smoke tests at normal and narrow sizes;
- old XML configuration compatibility;
- saved gamepad vector initialization/editing smoke tests.

The new ownership/runtime coverage verifies three simultaneous ordinary timed runs (keyboard + mouse + gamepad) with a Held Mapping active, one-run Stop preserving the others, duplicate same-macro start rejection, shared-digital reference ownership, deterministic pulse masking on an already-held key, exact finite-repeat overlap, Runtime Observation enumeration, and mixed-state Emergency Stop cleanup. These regression suites use fake/injected backends and do not create real input or virtual devices.

The current source also completed x64/x86 release build verification successfully after the multi-run changes.

### Input Lab v0.2 developer test target

- Added a standalone developer-only tool under `tools/InputLab/`; it does not modify the InputStitch runtime path.
- Keyboard page uses `WH_KEYBOARD_LL`, highlights key state and distinguishes Windows-injected (`SendInput`) events from non-injected events.
- Mouse page observes left/right/middle/X1/X2, wheel, position and optional movement logging with injected-event distinction.
- Manual mode registers foreground Raw Input keyboard/mouse observation as a separate comparison lane; automated mode uses `RIDEV_INPUTSINK` so the acceptance window does not need foreground focus. Low-level Hook and Raw Input may legitimately report synthetic input differently.
- XInput page polls slots 0-3 and visualizes controller buttons, D-pad, LT/RT and both stick vectors with raw/normalized values.
- Event Log records ordered keyboard/mouse/XInput transitions with relative millisecond timestamps and useful raw details.
- UI is split into `Keyboard`, `Mouse + XInput` and `Event log` tabs so it remains usable on the 2160x1440 laptop at 150% scaling. Keyboard rows use fixed compact spacing after live UI review.
- `--view devices` and `--view log` can open non-default views directly for automated/manual validation.
- Runtime smoke validation succeeded with real synthetic/virtual output paths: scan-code `SendInput` W DOWN/UP, injected X1 DOWN/UP, and a ViGEm Xbox 360 test state containing A + RB + RT 75% + left stick approximately (+0.5,+0.75).
- Added an isolated automated acceptance host (`build-acceptance.ps1` / `run-acceptance.ps1`). Simulated trigger edges enter InputStitch's real trigger handler, while macro execution, Output Ownership, `SendInput`, ViGEm and XInput remain the real paths.
- Automated mode is non-activating and hidden from the taskbar. Acceptance-only injected keyboard `K` and mouse `X2` events are logged by the low-level hooks and then swallowed, preventing them from being delivered to the user's current foreground application.
- XInput acceptance binds to the actual InputStitch virtual Xbox by holding a distinctive ViGEm preflight report and observing which XInput slot reflects it. If a local ViGEm/XUSB stack enumerates the controller but does not propagate the report, the run returns `SUMMARY: BLOCKED` / exit code 2 rather than converting an environment problem into product assertion failures.
- Automated expected-vs-observed coverage includes WASD release order, opposing stick axes, trigger maximum merge, shared digital ownership, D-pad axis conflict, Held/ordinary coexistence, Emergency Stop, keyboard API-lane observation, mouse injected output and final neutral state.
- The Concurrent Runtime build adds a pre-XInput real-SendInput concurrency lane: F7 holds keyboard `K`, F8 holds mouse `X2`, both must overlap, Runtime Observation must list both active ordinary runs, stopping F7 must leave F8 active, and both paths must be observed as injected SendInput. On the final laptop run this lane passed all 11 checks before the existing ViGEm/XInput environment preflight returned `SUMMARY: BLOCKED | checks=11 | failures=0`.
- A later full controller-backed concurrency scenario (ordinary gamepad + keyboard + mouse overlap) is compiled into the acceptance runner and will execute when the local ViGEm/XInput report-propagation preflight is healthy; do not bypass that preflight merely to force a green result.
- The final hardened automated black-box suite passed three consecutive monitored runs at this breakpoint: `SUMMARY: PASS | checks=50 | failures=0` on each run. The acceptance process was sampled every 50 ms and never became the foreground process.
- Across that stability run, `%APPDATA%\InputStitch\config.xml` had identical SHA-256, byte length and UTC modification time before and after acceptance, confirming the isolated host did not alter the user's real configuration.
- Startup XInput state is recorded as informational rather than a product assertion because Windows can expose stale/other-controller state before the first InputStitch output packet; all scenario release checks and the final neutral state remain hard PASS/FAIL assertions.

## Not completed yet

- Targeted real-game acceptance of the new multi-run concurrency/release behavior and evidence-driven fixes if needed.
- Stable 1.3.0 promotion after that real-use gate passes.
- Layer implementation after Stable 1.3.0.
- Optional future expansion of Advanced/complex Hold from exclusive execution into the same multi-run runtime, if repeated real use justifies it.
- Input Lab target-window message comparison, DS4/DirectInput/HID observation, longer soak scenarios and machine-readable report export.

## Known issues / known limitations

There is no currently known automated-regression failure at this breakpoint. The important unresolved risk is **lack of sufficient real-game validation of the new structural concurrency path**.

Input Lab v0.2 is intentionally not a complete game-API simulator yet. It now has low-level Hook + foreground/manual or INPUTSINK/automated Raw Input + XInput lanes, but a pass does not prove target-window message delivery, DirectInput/HID/DS4, GameInput, anti-cheat, privilege-boundary, exclusive-fullscreen or game-specific compatibility. The automated trigger source is an internal simulated physical edge because InputStitch correctly ignores Windows-injected input as a macro trigger; the downstream runtime/output path remains real.

During v0.2 hardening on the laptop, the Windows XUSB stack temporarily produced an unusual state in which the ViGEm Xbox enumerated successfully but XInput remained at a neutral `packet=1` despite submitted reports. Direct ViGEm and all three tested XInput DLL variants reproduced that condition, so it was not treated as an InputStitch Ownership failure. The condition later recovered and the hardened report-signature preflight then passed repeatedly. Keep the `BLOCKED` preflight distinction; do not turn environment inability to observe controller reports into fake product failures.

A later final combined validation reproduced that local condition again: the keyboard/mouse acceptance checks passed, then the controller preflight returned `SUMMARY: BLOCKED | checks=5 | failures=0` with XInput slot 0 still at neutral `packet=1`. This confirms the laptop's ViGEm/XUSB observation path remains intermittent even though three consecutive full `50/50` runs succeeded immediately beforehand. Do not reinstall or replace system drivers merely to force the test green without a separate, reviewed driver-troubleshooting decision.

Intentional limitations that must not be mistaken for bugs:

- only qualifying state-style Held Mappings use the dedicated parallel Held path;
- multiple distinct ordinary timed/Toggle macros are concurrent, but the same `MacroDefinition` still has at most one active run instance;
- advanced/complex Hold behavior remains exclusive in Stable 1.3.0 scope;
- same-output digital pulse/click requests do not force a bounce while another source persistently owns that output; the pulse is masked until the persistent state releases;
- duplicate physical triggers use list priority;
- Layer is not enabled in `1.3.0-beta.1`;
- Beta and Stable share `%APPDATA%\InputStitch`, so they should not run simultaneously;
- the application still depends on ViGEmBus for virtual-controller output and the EXE is not code-signed.

## Next step

Concurrent Macro Runtime and option-B eligibility UI are complete locally. **Do not add another structural feature before the Stable 1.3.0 gate.**

Next actions:

1. In the real target game, start two different ordinary timed macros with overlapping lifetimes and confirm both effects occur.
2. Stop one while the other remains active; confirm the remaining macro's output is not released.
3. Run at least one ordinary timed macro alongside a parallel Held Mapping and confirm independent cleanup in both directions.
4. Trigger Emergency Stop while multiple ordinary runs + a Held Mapping are active; confirm all state returns neutral.
5. Repeat these start/stop patterns enough times to catch stale RunId/Source state or stuck input. Add Alt+Tab/lost-KeyUp only when a Hold/Held source is part of the scenario; unchanged SendInput/ViGEm recognition does not need full re-validation.
6. If no evidence-backed defect appears, prepare and publish **Stable `v1.3.0`** directly. An intermediate beta is optional, not mandatory.
7. After Stable 1.3.0 is accepted, resume Layer work unless the user reprioritizes another feature.

Keep `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1` as the automated ownership/concurrency baseline. On this laptop the pre-XInput concurrent SendInput lane passes; a later `BLOCKED` at ViGEm/XInput preflight is an environment condition, not permission to bypass the preflight.

## Important design constraints

- Do not regress existing Held Mapping behavior.
- Preserve import/configuration compatibility unless a deliberate migration is implemented and tested.
- Do not replace source-local cleanup with global neutralization; that would break concurrent ownership semantics.
- Emergency Stop must remain absolute priority and must be able to clear all source state.
- Output backend failures must remain fail-closed.
- UI safety/edit protection must not accidentally clear unrelated parallel Held Mapping sources.
- Concurrent ordinary timed macros are implemented with dedicated per-run state; do not regress back to singleton worker bookkeeping or bypass Output Ownership with ad-hoc parallel threads.
- Preserve duplicate-trigger macro-list priority unless a new conflict model is explicitly designed.
- Layer switch must remove old-layer ownership before changing eligibility.
- Do not synthesize Held Mapping activation for a key that was already physically down before a Layer switch; require release + press.
- Stable and Beta release/update channels must remain separated.
- Run the complete regression suite before committing runtime changes.

## Resume checklist on another computer

After cloning or pulling the repository:

```text
1. Read AGENTS.md
2. Read PLAN.md
3. Read ROADMAP.md
4. Read HANDOFF.md
5. git log -8 --oneline --decorate
6. git status --short --branch
7. git fetch / git pull as appropriate
8. Confirm the expected code baseline before editing
```

If the new machine has never built InputStitch before, verify Git, the C#/.NET Framework build path, repository dependencies, and GitHub authentication before making release changes.
