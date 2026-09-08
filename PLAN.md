# PLAN.md

# Current Development Plan

Updated: 2026-09-08

## Current milestone

**1.3.0 concurrent macro runtime and Stable promotion**

Current public Beta: `v1.3.0-beta.2`
Stable rollback baseline: `v1.2.0`

Beta 2 publishes the **unified Concurrent Macro Runtime** built on Beta 1's Input Ownership foundation. Multiple distinct ordinary timed/Toggle macros and Advanced/complex Hold macros can all run concurrently while state-only Parallel Held Mappings remain active. Every worker-backed run has its own RunId/SourceId/stop/timing/trigger-release state, and a single authoritative `MacroRuntimeClassifier` drives both execution eligibility and the option-B live UI classification. The remaining 1.3.0 work is targeted real-game acceptance and evidence-backed fixes only; Layer remains deferred until after Stable 1.3.0.

## Immediate work

The implementation and automated validation phases are complete at the current local breakpoint. Remaining Stable 1.3.0 work is deliberately narrow:

1. Run targeted real-game acceptance for the **newly affected universal-concurrency paths only**: multiple complex Hold macros overlapping, complex Hold + ordinary timed/Toggle + Parallel Held coexistence, releasing one Hold trigger while the others remain active, and Emergency Stop from a mixed active state.
2. Confirm no stuck keyboard/mouse/controller state after repeated mixed start/stop/release cycles in the target game, including one Alt+Tab/lost-KeyUp sample while multiple Hold/Held sources are active.
3. Fix only evidence-backed defects found by that real-use acceptance; unchanged low-level SendInput/ViGEm recognition does not need a full re-validation.
4. If the targeted real-use gate is clean, promote directly to **Stable `v1.3.0`**. Another Beta is optional rather than mandatory.

Implemented and automated already:

- multiple distinct ordinary timed/Toggle macros with keyboard, mouse and virtual-gamepad steps;
- multiple Advanced/complex Hold timelines with keyboard, mouse, gamepad, Press/Up sequencing, non-zero/random delays, finite or infinite execution;
- per-run Hold-trigger snapshots and independent terminal-release / lost-KeyUp settle state;
- state-only Parallel Held Mappings coexisting with all worker-backed macro types;
- independent RunId/SourceId, stop state, progress and source-local cleanup;
- definition changes signal only the affected active macro run/source before mutation;
- selected-macro Stop and global Emergency Stop semantics;
- multi-run Runtime observation and UI-safety handling;
- option-B runtime/concurrency eligibility feedback sourced from the same classifier used by execution;
- deterministic digital state semantics: if one source already holds a digital output, a second pulse/click on that same output is masked rather than forcing an artificial release/repress bounce;
- one active run per `MacroDefinition`; Single-step remains an intentionally exclusive diagnostic execution mode rather than a normal concurrency class.

## Real-use acceptance matrix

The old core Held/Ownership matrix is already covered by regression + Input Lab and does not need to be mechanically repeated. For Stable 1.3.0, prioritize only the new universal-concurrency behavior:

- hold two different complex Hold macros simultaneously (prefer one keyboard/mouse output and one gamepad output) and confirm both effects remain active;
- release only one complex Hold trigger and confirm every unrelated complex Hold/timed/Parallel Held source remains active;
- run complex Hold + ordinary timed/Toggle + Parallel Held Mapping together and release/stop them in different orders;
- run at least one finite complex Hold beside an infinite Hold and confirm the finite macro can finish naturally without stopping the other;
- trigger Emergency Stop while multiple complex Hold + ordinary + Parallel Held sources are active and confirm all output returns neutral;
- repeat mixed start/stop/release patterns enough times to catch stale RunId/SourceId/trigger-release state or stuck input;
- perform one Alt+Tab/lost-KeyUp sample while at least two Hold/Held triggers are physically down, then release them in the background and confirm fallback cleanup is source-local.

## Acceptance tooling

Developer-only `tools/InputLab/` v0.2 now covers routine keyboard, mouse, Raw Input and XInput acceptance without launching a real game for every iteration. It provides low-level hook vs foreground Raw Input comparison, XInput button/trigger/stick observation, ordered event logging, and an isolated automated acceptance host that drives the real InputStitch ownership/runtime path and checks final `SendInput` / ViGEm output with explicit PASS/FAIL assertions.

The hardened automated core ownership matrix passed three consecutive `50/50` runs at this breakpoint while a 50 ms foreground sampler confirmed that the acceptance process never became the foreground process. The same stability run verified that the user's normal `%APPDATA%\InputStitch\config.xml` SHA-256, size and modification time were unchanged before/after acceptance. Acceptance-only injected keyboard/mouse outputs are swallowed after observation so they do not leak into the user's current foreground application.

The runner now performs a real ViGEm/XInput report preflight before controller assertions. A local XUSB/ViGEm stack that enumerates a controller but fails to propagate state is reported as `SUMMARY: BLOCKED` (exit code 2), not as a product regression. Before that preflight, the current universal runtime build validates both ordinary-timed overlap and **complex Hold overlap** on real SendInput: F9→K and F10→X2 are delayed Advanced Hold timelines, both must remain active together, Runtime observation must classify both as Concurrent Advanced Hold, releasing F9 must leave F10/X2 active, and each release must clear only its own output. All **18 pre-XInput checks pass** on the laptop before the existing ViGEm/XInput preflight reports `BLOCKED | failures=0`. A controller-backed universal-mix scenario (Parallel Held + complex keyboard/mouse/gamepad Hold + ordinary timed macro) is also compiled and runs when that environment preflight is healthy.

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
