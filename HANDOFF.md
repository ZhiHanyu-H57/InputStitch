# HANDOFF.md

# Current Development State

Updated: 2026-09-08

## Current version

- Public Beta: `v1.3.0-beta.2`
- Stable rollback baseline: `v1.2.0`
- Branch: `main`
- Public Beta product/runtime commit: resolve `v1.3.0-beta.2` with `git rev-list -n 1 v1.3.0-beta.2`; the release tag is the source of truth.
- Beta 2 publishes the unified Concurrent Macro Runtime intended for Stable 1.3.0. The runtime baseline remains the `v1.3.0-beta.2` tag; `main` may move ahead for roadmap/documentation changes before evidence-backed runtime fixes or Stable promotion work begins.

For the commit containing this handoff document itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

This avoids a self-referential commit hash inside the file.

## Working on

**Targeted real-game acceptance for the unified Concurrent Macro Runtime, then Stable 1.3.0 promotion.**

The post-Beta runtime now supports concurrency across ordinary timed/Toggle macros, Advanced/complex Hold timelines and state-only Parallel Held Mappings. Automated validation is green. The remaining engineering gate is intentionally narrow: verify this newly expanded universal-concurrency behavior in the actual target game—multiple complex Holds together, source-local Hold release, coexistence with ordinary + Parallel Held output, one Alt+Tab/lost-KeyUp sample, and mixed-state Emergency Stop. Do not mechanically re-test unchanged SendInput/ViGEm recognition. If this targeted real-use gate is clean, prepare Stable 1.3.0.

**After Stable 1.3.0, the next highest-priority feature is Physical Gamepad Input + Hybrid Controller Routing. Layer has been reprioritized behind that track.** The first goal is Windows XInput controller buttons/D-pad/trigger thresholds as first-class InputStitch triggers; the second is controller→keyboard/mouse hybrid mapping; the third is controlled replacement/routing through one virtual gamepad with device-hiding only after a mature fail-safe approach is chosen. Full design: `docs/GAMEPAD_INPUT_ROUTING.md`.

Current `main` is now one evidence-backed trigger fix ahead of the `v1.3.0-beta.2` runtime tag. Holding **Shift** no longer disables a separately configured bare ordinary-key trigger such as `E`; exact explicit chords still win, and Ctrl/Alt/Win remain strict for bare keys. Modifier chords are now valid Hold triggers. The chord starts on the terminal-key down edge with required modifiers already held; terminal-key release or release of any required modifier stops only the matching Hold source, and lost-KeyUp reconciliation checks the complete chord state.

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

- Multiple qualifying state-only Held Mappings may stay active simultaneously on the dedicated Parallel Held fast path.
- Multiple **distinct ordinary timed/Toggle macros and Advanced/complex Hold macros** may run concurrently; one active run per `MacroDefinition` remains intentional.
- Every worker-backed run has independent `RunId`, `SourceId`, stop event, progress/timing state and source-local held-output bookkeeping.
- Advanced Hold is no longer exclusive. Keyboard/mouse Hold output, gamepad Press/Up sequencing, non-zero/random delays and finite Hold all classify as `ConcurrentHoldMacro` / **Concurrent Advanced Hold**.
- Physical Hold runs snapshot their trigger per run. Terminal KeyUp/MouseUp and lost-KeyUp settle probes target individual run IDs, so releasing one Hold cannot stop another.
- Finite Hold preserves historical semantics: it may complete naturally before physical trigger release; release only stops it early while it is still active.
- All timed/Toggle/Advanced-Hold runs may coexist with active Parallel Held Mappings and share the same Output Ownership merger.
- Duplicate physical triggers still use macro-list priority rather than launching every duplicate mapping.
- A digital pulse/click targeting a key/button already held by another source is deterministically masked instead of forcing a release/repress bounce; persistent ownership remains until the last owner releases.

### Safety and diagnostics

- One worker-backed run completing/stopping/failing clears only its own Output Ownership source; unrelated timed/Toggle/Advanced-Hold runs and Parallel Held Mappings remain active.
- The primary Run/Stop button controls the selected macro; Emergency Stop remains the global escape path and stops every worker run + Parallel Held source.
- UI safety evaluates the immutable running snapshot for mouse output across all active runs and suspends/resumes affected sources without global neutralization.
- Editing a live macro's trigger/run/repeat/step definition signals only that macro's current run/Parallel Held source before mutation; unrelated runs remain active.
- Shutdown and backend-failure cleanup signal every active run and retain ownership-level fail-closed cleanup.
- Runtime observation lists every active worker run with RunId/Source/category/phase/iteration/step/logical-held state, followed by Parallel Held mappings, active ownership sources and merged output.
- The macro editor shows live runtime category + concurrency eligibility + reason from the same authoritative `MacroRuntimeClassifier` used by execution; there is no duplicate UI rule table.
- Single-step / Next Step remains intentionally exclusive as a diagnostic execution mode and is interruptible by Stop/Emergency Stop; it is not a limitation on normal macro concurrency.
- Bare ordinary-key trigger selection now permits Shift-only fallback after exact matching. Therefore an explicit `Shift+E` trigger wins over bare `E`, while bare `E` still works under Shift if no explicit chord matches. Bare Ctrl/Alt/Win combinations remain strict.
- Modifier-chord Hold is supported in both Parallel Held Mapping and Concurrent Advanced Hold. Hook release handling recognizes either terminal-key release or required-modifier release; the physical-state fallback evaluates the full chord rather than only the terminal key.

### Layer preparation

- Layer architecture has been designed but is intentionally deferred behind the post-1.3 Physical Gamepad Input / Hybrid Routing track.
- Design document: `docs/LAYER_DESIGN.md`.
- Intended first version: `Base + one active Layer`, initially scoped to Held Mapping eligibility.

### Physical Gamepad Input / Hybrid Routing — planned next after Stable 1.3.0

- First input backend: Windows XInput / Xbox-class controllers.
- First trigger scope: controller buttons + D-pad, then LT/RT threshold triggers with hysteresis; stick-region triggers are optional/later.
- Controller triggers should reuse the existing macro runtime, so they can start keyboard, mouse, virtual-gamepad or mixed-output macros instead of creating a second execution engine.
- The first controller→keyboard/mouse implementation is **augmentation only**: without hiding the physical device, the target game still receives the original controller action as well as InputStitch's mapped output.
- Full replacement requires the Gamepad Router stage: hide/take over the selected physical controller from the game, read it in InputStitch, route retained analog/gamepad controls to one virtual controller, and send selected controls as keyboard/mouse/macros.
- Do not write an InputStitch kernel filter driver as the first solution. Investigate a mature external hiding/filter mechanism, including admin/signing/Secure Boot/anti-cheat implications and fail-safe unhide/recovery.
- Avoid controller feedback loops: InputStitch must never treat its own ViGEm virtual controller output as a physical trigger source. XInput index alone is not a stable identity guarantee; virtual-slot exclusion/source identity must be designed before release.
- Hot-plug/reconnect and selected-controller ownership must be deterministic.
- PC only. DualShock/DualSense on Windows may be added later via DirectInput/HID/GameInput if worthwhile; native PlayStation-console support is out of scope unless an official low-friction path appears.

### Release and verification

`v1.3.0-beta.2` is the current GitHub prerelease and publishes the unified macro-concurrency runtime. Stable `v1.2.0` remains `Latest`; beta.1 remains historical and must not be overwritten.

Beta releases use the isolated `InputStitch-beta.xml` channel. The beta.2 publication workflow must verify both architectures/assets and confirm the Stable release fingerprint is unchanged before and after publishing.

Immediately before this handoff, the full regression suite was run again on the **Concurrent Macro Runtime build** and passed:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 354 productivity checks;
- 303,708 Output Ownership checks, including the universal timed/Toggle/Advanced-Hold + Parallel-Held matrix and modifier-chord Hold classification/release semantics;
- zh-CN/en-US Settings smoke tests at normal and narrow sizes;
- old XML configuration compatibility;
- saved gamepad vector initialization/editing smoke tests.

The new ownership/runtime coverage verifies ordinary timed/Toggle runs and multiple Advanced Hold timelines together with a Parallel Held Mapping, source-local terminal release, independent lost-KeyUp probe state, definition-change isolation, one-run Stop preserving unrelated runs, duplicate same-macro start rejection, shared-digital reference ownership, deterministic pulse masking on an already-held key, exact finite-repeat / finite-Hold completion, Runtime Observation enumeration, and mixed-state Emergency Stop cleanup. These regression suites use fake/injected backends and do not create real input or virtual devices.

The current universal-concurrency source completed fresh x64/x86 release build verification successfully after the final regression run.

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
- The universal-runtime build now has two pre-XInput real-SendInput concurrency lanes: ordinary F7→K + F8→X2 overlap, and delayed **Advanced Hold** F9→K + F10→X2 overlap. For the complex Hold lane, Runtime Observation must classify both as Concurrent Advanced Hold; releasing F9 must remove only K while F10/X2 stays active; releasing F10 then clears X2. On the final laptop run all **18 pre-XInput checks passed** before the existing ViGEm/XInput environment preflight returned `SUMMARY: BLOCKED | checks=18 | failures=0`.
- A controller-backed universal scenario is compiled into the acceptance runner for a healthy XInput environment: Parallel Held W + complex keyboard Hold + complex mouse Hold + complex RT60 Hold + ordinary A timed macro must coexist, and releasing one complex Hold must preserve the remaining sources. Do not bypass the ViGEm/XInput preflight merely to force this lane green.
- The final hardened automated black-box suite passed three consecutive monitored runs at this breakpoint: `SUMMARY: PASS | checks=50 | failures=0` on each run. The acceptance process was sampled every 50 ms and never became the foreground process.
- Across that stability run, `%APPDATA%\InputStitch\config.xml` had identical SHA-256, byte length and UTC modification time before and after acceptance, confirming the isolated host did not alter the user's real configuration.
- Startup XInput state is recorded as informational rather than a product assertion because Windows can expose stale/other-controller state before the first InputStitch output packet; all scenario release checks and the final neutral state remain hard PASS/FAIL assertions.

## Not completed yet

- Targeted real-game acceptance of the new **universal timed/Toggle/Advanced-Hold + Parallel-Held concurrency** and source-local release behavior.
- Stable 1.3.0 promotion after that real-use gate passes.
- Physical XInput gamepad as a first-class trigger/input source after Stable 1.3.0.
- Controller→keyboard/mouse hybrid mapping, initially as augmentation while original physical controller input still reaches the game.
- Gamepad Router / controlled replacement with device hiding and one game-visible virtual controller after the simpler input-source stage is accepted.
- Layer implementation after the gamepad-input/routing core reaches a stable checkpoint, unless explicitly reprioritized.
- Input Lab target-window message comparison, DS4/DirectInput/HID observation, longer soak scenarios and machine-readable report export.

## Known issues / known limitations

There is no currently known automated-regression failure at this breakpoint. The important unresolved risk is **lack of sufficient real-game validation of the new structural concurrency path**.

Input Lab v0.2 is intentionally not a complete game-API simulator yet. It now has low-level Hook + foreground/manual or INPUTSINK/automated Raw Input + XInput lanes, but a pass does not prove target-window message delivery, DirectInput/HID/DS4, GameInput, anti-cheat, privilege-boundary, exclusive-fullscreen or game-specific compatibility. The automated trigger source is an internal simulated physical edge because InputStitch correctly ignores Windows-injected input as a macro trigger; the downstream runtime/output path remains real.

During v0.2 hardening on the laptop, the Windows XUSB stack temporarily produced an unusual state in which the ViGEm Xbox enumerated successfully but XInput remained at a neutral `packet=1` despite submitted reports. Direct ViGEm and all three tested XInput DLL variants reproduced that condition, so it was not treated as an InputStitch Ownership failure. The condition later recovered and the hardened report-signature preflight then passed repeatedly. Keep the `BLOCKED` preflight distinction; do not turn environment inability to observe controller reports into fake product failures.

A later final combined validation reproduced that local condition again: the keyboard/mouse acceptance checks passed, then the controller preflight returned `SUMMARY: BLOCKED | checks=5 | failures=0` with XInput slot 0 still at neutral `packet=1`. This confirms the laptop's ViGEm/XUSB observation path remains intermittent even though three consecutive full `50/50` runs succeeded immediately beforehand. Do not reinstall or replace system drivers merely to force the test green without a separate, reviewed driver-troubleshooting decision.

Intentional limitations that must not be mistaken for bugs:

- only qualifying state-style Held Mappings use the dedicated lightweight Parallel Held path; complex Hold uses the worker-backed concurrent runtime instead;
- multiple distinct timed/Toggle/Advanced-Hold macros are concurrent, but the same `MacroDefinition` still has at most one active run instance;
- Single-step remains exclusive because it is a diagnostic execution mode;
- physical Hold triggers support single keyboard keys (including standalone modifiers), modifier+terminal-key chords, and mouse buttons. A chord starts when the terminal key goes down with all declared modifiers already held; releasing the terminal key or any required modifier stops only that Hold run/source. Wheel remains outside Hold-trigger semantics because it has no persistent down state;
- same-output digital pulse/click requests do not force a bounce while another source persistently owns that output; the pulse is masked until the persistent state releases;
- duplicate physical triggers use list priority;
- Layer is not enabled in `1.3.0-beta.2`;
- Beta and Stable share `%APPDATA%\InputStitch`, so they should not run simultaneously;
- the application still depends on ViGEmBus for virtual-controller output and the EXE is not code-signed.

## Next step

Unified normal-macro concurrency and option-B eligibility UI are complete locally. **Do not add another structural feature before the Stable 1.3.0 gate.**

Next actions:

1. In the real target game, hold two different complex Hold macros at the same time and confirm both outputs remain active.
2. Release only one complex Hold trigger; confirm the other Hold plus any ordinary/Parallel Held outputs remain active.
3. Run complex Hold + ordinary timed/Toggle + Parallel Held Mapping together and vary the release/stop order.
4. Test one finite complex Hold beside an infinite Hold and confirm natural finite completion does not affect the infinite run.
5. Trigger Emergency Stop while multiple complex Hold + ordinary + Parallel Held sources are active; confirm every output returns neutral.
6. Do one Alt+Tab/lost-KeyUp sample with at least two physical Hold/Held triggers, release them while backgrounded, and confirm source-local fallback cleanup.
7. Repeat mixed start/stop/release cycles enough times to catch stale RunId/SourceId/release-probe state or stuck input. Existing SendInput/ViGEm recognition does not need full re-validation.
8. If no evidence-backed defect appears, prepare and publish **Stable `v1.3.0`** directly. An intermediate beta is optional, not mandatory.
9. After Stable 1.3.0 is accepted, begin **Physical Gamepad Input + Hybrid Controller Routing**. Start with XInput controller triggers; do not jump directly to device hiding or Layer.

Keep `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1` as the automated ownership/concurrency baseline. On this laptop all 18 pre-XInput ordinary + complex-Hold SendInput checks pass; a later `BLOCKED` at ViGEm/XInput preflight is an environment condition, not permission to bypass the preflight.

## Important design constraints

- Do not regress existing Held Mapping behavior.
- Preserve import/configuration compatibility unless a deliberate migration is implemented and tested.
- Do not replace source-local cleanup with global neutralization; that would break concurrent ownership semantics.
- Emergency Stop must remain absolute priority and must be able to clear all source state.
- Output backend failures must remain fail-closed.
- UI safety/edit protection must not accidentally clear unrelated parallel Held Mapping sources.
- Unified timed/Toggle/Advanced-Hold concurrency is implemented with dedicated per-run state; do not regress back to singleton worker bookkeeping, a global Hold owner, or ad-hoc threads that bypass Output Ownership.
- Preserve duplicate-trigger macro-list priority unless a new conflict model is explicitly designed.
- Controller input must use the same trigger/runtime/source model rather than bypassing Concurrent Macro Runtime with a separate macro engine.
- Do not claim that controller→keyboard/mouse mapping replaces/suppresses the physical controller action until a verified device-hiding/router path exists.
- Prevent self-feedback: InputStitch's own virtual controller output must never be eligible as a physical controller trigger source.
- Analog controller triggers require threshold hysteresis and deterministic release semantics; do not use a single noisy threshold for both activation and release.
- Physical-controller selection, disconnect and reconnect must fail safe and must not silently switch to an unintended controller if that can trigger macros.
- For the first Router implementation, prefer a mature external hiding/filter solution with fail-safe unhide over writing a new kernel driver.
- Keep native PlayStation-console development out of scope; Windows-connected DualShock/DualSense support is a separate later backend question.
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
