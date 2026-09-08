# PLAN.md

# Current Development Plan

Updated: 2026-09-08

## Current milestone

**1.3.0 concurrent macro runtime and Stable promotion**

Current public Beta: `v1.3.0-beta.1`
Stable rollback baseline: `v1.2.0`

The Input Ownership foundation from Beta 1 has now been extended locally with the **Concurrent Macro Runtime** planned for Stable 1.3.0. Multiple distinct ordinary timed/Toggle macros can run at the same time, each with its own RunId/SourceId/stop/timing state, while existing parallel Held Mapping sources remain active. A single authoritative `MacroRuntimeClassifier` drives both execution eligibility and the option-B live UI classification. Layer remains deferred until after Stable 1.3.0.

## Immediate work

The implementation and automated validation phases are complete at the current local breakpoint. Remaining Stable 1.3.0 work is deliberately narrow:

1. Run targeted real-game acceptance for the **newly affected multi-run paths only**: two or more ordinary timed macros overlapping, stopping one while another remains active, ordinary timed macros coexisting with Held Mapping output, and Emergency Stop from a mixed active state.
2. Confirm no stuck keyboard/mouse/controller state after repeated multi-run start/stop cycles in the target game.
3. Fix only evidence-backed defects found by that real-use acceptance; unchanged low-level SendInput/ViGEm recognition does not need a full re-validation.
4. If the targeted real-use gate is clean, promote directly to **Stable `v1.3.0`**. Another Beta is optional rather than mandatory.

Implemented and automated already:

- multiple distinct ordinary timed macros with keyboard, mouse and virtual-gamepad steps;
- non-zero delays and finite repeat counts;
- independent run identity, stop state, progress and source-local cleanup;
- coexistence with parallel Held Mappings;
- selected-macro Stop and global Emergency Stop semantics;
- multi-run Runtime observation and UI-safety handling;
- option-B runtime/concurrency eligibility feedback sourced from the same classifier used by execution;
- deterministic digital state semantics: if one source already holds a digital output, a second pulse/click on that same output is masked rather than forcing an artificial release/repress bounce;
- automated tests for same-key ownership, pulse masking, duplicate same-macro start rejection, finite-repeat overlap, independent stop and mixed Emergency Stop.

## Real-use acceptance matrix

The old core Held/Ownership matrix is already covered by regression + Input Lab and does not need to be mechanically repeated. For Stable 1.3.0, prioritize only the new multi-run behavior:

- start two ordinary timed macros with overlapping lifetimes and confirm both effects are visible in-game;
- stop one ordinary macro while the other continues; unrelated output must remain active;
- run at least one keyboard/mouse timed macro alongside a parallel Held Mapping and confirm neither clears the other;
- run two finite-repeat macros with different durations and confirm each finishes on its own schedule;
- trigger Emergency Stop while multiple ordinary runs + a Held Mapping are active and confirm all output returns neutral;
- repeat the above start/stop patterns enough times to catch stale run/source state or stuck input;
- optionally include an Alt+Tab/lost-KeyUp check only where a Hold/Held source is active, because the low-level backend itself was not rewritten by this change.

## Acceptance tooling

Developer-only `tools/InputLab/` v0.2 now covers routine keyboard, mouse, Raw Input and XInput acceptance without launching a real game for every iteration. It provides low-level hook vs foreground Raw Input comparison, XInput button/trigger/stick observation, ordered event logging, and an isolated automated acceptance host that drives the real InputStitch ownership/runtime path and checks final `SendInput` / ViGEm output with explicit PASS/FAIL assertions.

The hardened automated core ownership matrix passed three consecutive `50/50` runs at this breakpoint while a 50 ms foreground sampler confirmed that the acceptance process never became the foreground process. The same stability run verified that the user's normal `%APPDATA%\InputStitch\config.xml` SHA-256, size and modification time were unchanged before/after acceptance. Acceptance-only injected keyboard/mouse outputs are swallowed after observation so they do not leak into the user's current foreground application.

The runner now performs a real ViGEm/XInput report preflight before controller assertions. A local XUSB/ViGEm stack that enumerates a controller but fails to propagate state is reported as `SUMMARY: BLOCKED` (exit code 2), not as a product regression. The current Concurrent Runtime build adds a pre-XInput real-SendInput overlap lane: two ordinary timed macros hold keyboard `K` and mouse `X2` concurrently, Runtime observation must show both runs, stopping the keyboard run must leave the mouse run active, and both paths must be observed as injected SendInput. This lane passes all 11 checks on the laptop before the existing local ViGEm/XInput preflight reports `BLOCKED` with `failures=0`.

Next tooling candidates are target-window message comparison, DS4/DirectInput/HID observation, longer soak/repeated-cycle scenarios and machine-readable report export.

## Gate for Layer

Concurrent Macro Runtime is now implemented locally, but **Layer still remains behind the Stable 1.3.0 promotion gate**. Do not begin Layer until the new multi-run runtime passes targeted real-game acceptance and Stable 1.3.0 is released, unless the user explicitly reprioritizes it again.

The planned first Layer scope remains documented for later work:

- `Base + one active Layer`;
- Layer affects Held Mapping eligibility first;
- it must compose with the multi-worker Concurrent Macro Runtime rather than restoring a single global worker assumption;
- changing Layer removes old-layer sources before changing eligibility;
- keys already physically held before a Layer switch are not synthetically re-triggered; they must be released and pressed again;
- Emergency Stop remains above Layer selection and all mapping priority logic.

## Release discipline

Stable `v1.2.0` remains the rollback baseline while 1.3.x is being evaluated. Beta releases remain prereleases and use `InputStitch-beta.xml`; they must not alter the Stable latest/update path.
