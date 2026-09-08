# HANDOFF.md

# Current Development State

Updated: 2026-09-08

## Current version

- Public Beta: `v1.3.0-beta.1`
- Stable rollback baseline: `v1.2.0`
- Branch: `main`
- Last verified **product/runtime** commit: `689b4e3ae4e0227dff1ef8de6f33901838cc0cac` (`[publish-beta] Release 1.3.0-beta.1`)
- `v1.3.0-beta.1` and remote `main` were verified to point to that same code commit at release time.

For the commit containing this handoff document itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

This avoids a self-referential commit hash inside the file.

## Working on

**Input Ownership real-use acceptance / hardening.**

The architecture work planned for Beta 1 is complete, and the core ownership semantics now have repeatable automated black-box evidence through Input Lab v0.2. The remaining engineering gate is **real target-game acceptance**, especially true foreground transitions/lost-KeyUp behavior and game-specific input APIs. Do not assume the next task is automatically “add more concurrency”; fix evidence-backed ownership/release issues first, then enable Layer only after the real-use gate is satisfied.

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
- At most one ordinary Toggle/timed macro may coexist with those Held Mappings.
- Advanced/complex Hold workers remain exclusive.
- Duplicate physical triggers still use macro-list priority rather than launching every duplicate mapping.
- Ordinary macro, parallel Held Mapping, and Idle Gamepad output share the ownership core.

### Safety and diagnostics

- Ordinary macro completion/suspension no longer globally neutralizes unrelated Held Mapping output.
- Editing a live Held Mapping definition stops the affected mapping before mutating critical trigger/run/step state.
- Shutdown and Emergency Stop have ownership-level cleanup.
- Runtime observation shows active sources, source contributions, merged output, ordinary macro phase/step, and recent stop/ownership reason.
- Ordinary timed macros support Single-step / Next Step.
- Single-step waiting remains interruptible by Stop and Emergency Stop.

### Layer preparation

- Layer architecture has been designed but intentionally not enabled in Beta 1.
- Design document: `docs/LAYER_DESIGN.md`.
- Intended first version: `Base + one active Layer`, initially scoped to Held Mapping eligibility.

### Release and verification

`v1.3.0-beta.1` was published as a GitHub prerelease. Stable `v1.2.0` remained `Latest`.

The release was verified locally, rebuilt from the published Source archive in an isolated temporary directory, and re-downloaded from GitHub for hash/manifest verification.

Immediately before this handoff, the full regression suite was run again on the laptop after the Input Lab v0.2 changes and passed:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 348 productivity checks;
- 303,643 Output Ownership checks;
- zh-CN/en-US Settings smoke tests at normal and narrow sizes;
- old XML configuration compatibility;
- saved gamepad vector initialization/editing smoke tests.

These existing regression suites use fake/injected backends and do not create real input or virtual devices. Release-time x64/x86 verification for `v1.3.0-beta.1` had already passed when the Beta was published.

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
- Automated expected-vs-observed coverage includes WASD release order, opposing stick axes, trigger maximum merge, shared digital ownership, D-pad axis conflict, four Held Mappings + one ordinary timed macro, Emergency Stop, keyboard API-lane observation, mouse injected output and final neutral state.
- The final hardened automated black-box suite passed three consecutive monitored runs at this breakpoint: `SUMMARY: PASS | checks=50 | failures=0` on each run. The acceptance process was sampled every 50 ms and never became the foreground process.
- Across that stability run, `%APPDATA%\InputStitch\config.xml` had identical SHA-256, byte length and UTC modification time before and after acceptance, confirming the isolated host did not alter the user's real configuration.
- Startup XInput state is recorded as informational rather than a product assertion because Windows can expose stale/other-controller state before the first InputStitch output packet; all scenario release checks and the final neutral state remain hard PASS/FAIL assertions.

## Not completed yet

- Concurrent Macro Runtime for multiple ordinary timed macros (highest-priority unfinished 1.3.0 capability).
- Option-B runtime-category / parallel-eligibility UI feedback.
- Targeted real-game acceptance of the new multi-worker concurrency/release behavior and evidence-driven fixes if needed.
- Stable 1.3.0 promotion after the concurrent runtime passes the complete regression, Input Lab and real-use gates.
- Layer implementation after Stable 1.3.0.
- Input Lab target-window message comparison, DS4/DirectInput/HID observation, longer soak scenarios and machine-readable report export.

## Known issues / known limitations

There is no currently known automated-regression failure at this breakpoint. The important unresolved risk is **lack of sufficient real-game validation of the new structural concurrency path**.

Input Lab v0.2 is intentionally not a complete game-API simulator yet. It now has low-level Hook + foreground/manual or INPUTSINK/automated Raw Input + XInput lanes, but a pass does not prove target-window message delivery, DirectInput/HID/DS4, GameInput, anti-cheat, privilege-boundary, exclusive-fullscreen or game-specific compatibility. The automated trigger source is an internal simulated physical edge because InputStitch correctly ignores Windows-injected input as a macro trigger; the downstream runtime/output path remains real.

During v0.2 hardening on the laptop, the Windows XUSB stack temporarily produced an unusual state in which the ViGEm Xbox enumerated successfully but XInput remained at a neutral `packet=1` despite submitted reports. Direct ViGEm and all three tested XInput DLL variants reproduced that condition, so it was not treated as an InputStitch Ownership failure. The condition later recovered and the hardened report-signature preflight then passed repeatedly. Keep the `BLOCKED` preflight distinction; do not turn environment inability to observe controller reports into fake product failures.

A later final combined validation reproduced that local condition again: the keyboard/mouse acceptance checks passed, then the controller preflight returned `SUMMARY: BLOCKED | checks=5 | failures=0` with XInput slot 0 still at neutral `packet=1`. This confirms the laptop's ViGEm/XUSB observation path remains intermittent even though three consecutive full `50/50` runs succeeded immediately beforehand. Do not reinstall or replace system drivers merely to force the test green without a separate, reviewed driver-troubleshooting decision.

Intentional limitations that must not be mistaken for bugs:

- only qualifying Held Mappings participate in the new parallel path;
- only one ordinary timed/Toggle macro may coexist with them;
- advanced/complex Hold behavior remains exclusive;
- duplicate physical triggers use list priority;
- Layer is not enabled in `1.3.0-beta.1`;
- Beta and Stable share `%APPDATA%\InputStitch`, so they should not run simultaneously;
- the application still depends on ViGEmBus for virtual-controller output and the EXE is not code-signed.

## Next step

The user has explicitly reprioritized the roadmap: **multiple concurrent ordinary timed macros are now the highest-priority feature and the final major capability gate for Stable 1.3.0. Layer is deferred until after 1.3.0.**

Implement in this order:

1. Add option-B runtime-category/parallel-eligibility feedback in the macro editor. Keep free editing, but clearly identify Parallel Held Mapping vs ordinary timed macro vs advanced/exclusive Hold and explain why eligibility changes.
2. Design the Concurrent Macro Runtime so multiple distinct ordinary timed macros have independent RunId/SourceId, worker/timing state and stop/cleanup state instead of sharing `workerThread`, `stopEvent`, `runningMacro` and related singleton fields.
3. First stable target: concurrent press-to-start ordinary macros with keyboard, mouse and virtual-gamepad steps, non-zero delays and finite repeat counts, coexisting with any active parallel Held Mapping sources.
4. Define deterministic same-output semantics, especially persistent digital ownership versus edge/pulse/click requests. Do not let OS thread timing accidentally define behavior.
5. Extend Runtime Observation and Stop/Emergency Stop semantics for multiple ordinary runs; one run completing or failing must release only its own source.
6. Extend unit/regression tests and Input Lab black-box acceptance for at least two overlapping timed macros, mixed keyboard/mouse/gamepad output, release-order permutations, one-run failure/stop, Emergency Stop, and Held + timed-worker coexistence.
7. Run targeted real-game acceptance only for the newly affected concurrency/release behavior. Existing SendInput/ViGEm recognition does not need a full re-validation when the low-level backend is unchanged.
8. When all existing regression suites plus the new concurrency matrix and real-use gate pass, prepare and publish **Stable `v1.3.0`**. An intermediate beta is optional, not mandatory.

Before or after runtime changes, retain `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1` as the existing ownership baseline so new multi-worker work cannot silently regress Held Mapping behavior.

## Important design constraints

- Do not regress existing Held Mapping behavior.
- Preserve import/configuration compatibility unless a deliberate migration is implemented and tested.
- Do not replace source-local cleanup with global neutralization; that would break concurrent ownership semantics.
- Emergency Stop must remain absolute priority and must be able to clear all source state.
- Output backend failures must remain fail-closed.
- UI safety/edit protection must not accidentally clear unrelated parallel Held Mapping sources.
- Concurrent ordinary timed macros are now explicitly approved and highest priority, but implement them only with a dedicated multi-run design and regression/black-box plan; do not obtain concurrency by merely starting extra threads around singleton runtime state.
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
