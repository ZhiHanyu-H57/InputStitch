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

The architecture work planned for Beta 1 is complete. Do not assume the next task is automatically “add more concurrency.” The current engineering gate is to validate the new ownership runtime under black-box/real-game use conditions, then fix evidence-backed issues before enabling Layer. A developer-only Input Lab was added to make this acceptance much cheaper and more repeatable.

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

Immediately before this handoff, the full regression suite was run again on the desktop and passed:

- 323 keyboard checks;
- 58 Idle Gamepad assertions;
- 43 updater checks;
- 23 UI safety / diagnostics checks;
- 348 productivity checks;
- 303,643 Output Ownership checks;
- zh-CN/en-US Settings smoke tests at normal and narrow sizes;
- old XML configuration compatibility;
- saved gamepad vector initialization/editing smoke tests;
- x64/x86 Release Verification.

No real input or virtual device was created by the automated ownership tests.

### Input Lab v0.1 developer test target

- Added a standalone developer-only tool under `tools/InputLab/`; it does not modify the InputStitch runtime path.
- Keyboard page uses `WH_KEYBOARD_LL`, highlights key state and distinguishes Windows-injected (`SendInput`) events from non-injected events.
- Mouse page observes left/right/middle/X1/X2, wheel, position and optional movement logging with injected-event distinction.
- XInput page polls slots 0-3 and visualizes controller buttons, D-pad, LT/RT and both stick vectors with raw/normalized values.
- Event Log records ordered keyboard/mouse/XInput transitions with relative millisecond timestamps and useful raw details.
- UI is split into `Keyboard`, `Mouse + XInput` and `Event log` tabs so it remains usable on the 2160x1440 laptop at 150% scaling. Keyboard rows use fixed compact spacing after live UI review.
- `--view devices` and `--view log` can open non-default views directly for automated/manual validation.
- Runtime smoke validation succeeded with real synthetic/virtual output paths: scan-code `SendInput` W DOWN/UP, injected X1 DOWN/UP, and a ViGEm Xbox 360 test state containing A + RB + RT 75% + left stick approximately (+0.5,+0.75).
- The complete existing InputStitch regression suite was rerun after adding the tool and passed unchanged.

## Not completed yet

- Real-game acceptance of the new ownership/concurrency runtime.
- Evidence-driven fixes, if real testing exposes ownership/release/merge/observation problems.
- Layer implementation.
- Input Lab Raw Input/message comparison, DS4/DirectInput/HID observation and automated expected-vs-observed scenario assertions.
- Arbitrary parallel ordinary timed macros (not currently planned; this is an intentional boundary, not an unfinished Beta-1 task).

## Known issues / known limitations

There is no currently known automated-regression failure at this breakpoint. The important unresolved risk is **lack of sufficient real-game validation of the new structural concurrency path**.

Input Lab v0.1 is intentionally not a complete game-API simulator yet: its keyboard/mouse lane is based on low-level hooks, and its controller lane is XInput. A pass in Input Lab therefore does not prove Raw Input, DirectInput/HID, GameInput, anti-cheat, privilege-boundary or game-specific compatibility.

Intentional limitations that must not be mistaken for bugs:

- only qualifying Held Mappings participate in the new parallel path;
- only one ordinary timed/Toggle macro may coexist with them;
- advanced/complex Hold behavior remains exclusive;
- duplicate physical triggers use list priority;
- Layer is not enabled in `1.3.0-beta.1`;
- Beta and Stable share `%APPDATA%\InputStitch`, so they should not run simultaneously;
- the application still depends on ViGEmBus for virtual-controller output and the EXE is not code-signed.

## Next step

First, test `v1.3.0-beta.1` in real use. Focus on:

1. WASD held in combinations and released in different orders.
2. Shift/Ctrl trigger mappings held together with stick mappings.
3. Mouse side-button shoulder mappings combined with the above.
4. Two to four Held Mappings plus one ordinary timed macro.
5. Alt+Tab / foreground changes and lost-KeyUp recovery.
6. Runtime observation matching the actual active sources and merged state.
7. Single-step waiting interrupted by Stop / Emergency Stop.
8. Repeated start/stop cycles with no residual input.

Use `tools/InputLab/` first for repeatable black-box checks of output values, ordering and release behavior, then confirm important scenarios in the actual target game. If failures appear, reproduce them with the smallest mapping set possible and harden ownership/release behavior first. If this acceptance is clean, proceed to the Layer implementation described in `docs/LAYER_DESIGN.md`.

## Important design constraints

- Do not regress existing Held Mapping behavior.
- Preserve import/configuration compatibility unless a deliberate migration is implemented and tested.
- Do not replace source-local cleanup with global neutralization; that would break concurrent ownership semantics.
- Emergency Stop must remain absolute priority and must be able to clear all source state.
- Output backend failures must remain fail-closed.
- UI safety/edit protection must not accidentally clear unrelated parallel Held Mapping sources.
- Do not expand to arbitrary parallel timed macros without a separate explicit design and test plan.
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
