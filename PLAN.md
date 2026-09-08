# PLAN.md

# Current Development Plan

Updated: 2026-09-08

## Current milestone

**1.3.0 concurrent macro runtime and Stable promotion**

Current public Beta: `v1.3.0-beta.1`
Stable rollback baseline: `v1.2.0`

The Input Ownership foundation, multi-source Held Mapping support, one-ordinary-macro coexistence, runtime observation, and single-step execution are implemented and published in Beta 1. The highest-priority remaining 1.3.0 capability is now **Concurrent Macro Runtime**: multiple independent ordinary timed macros must be able to run at the same time instead of sharing one global worker. Layer is explicitly deferred until after Stable 1.3.0 unless the roadmap is changed again.

## Immediate work

1. Add the agreed "parallel/concurrency eligibility" UI feedback (option B): keep macros freely editable, but show the runtime category and why a macro is or is not eligible for the parallel Held path.
2. Design and implement the first Concurrent Macro Runtime: multiple distinct ordinary timed macros may run concurrently, each with its own RunId/SourceId/stop/timing state, while continuing to use the shared Output Ownership merger.
3. The 1.3.0 concurrency target must support ordinary one-shot/timed macros with keyboard, mouse and virtual-gamepad steps, non-zero delays, press-to-start behavior, and finite repeat counts, while coexisting with existing parallel Held Mappings.
4. Define deterministic behavior for overlapping digital state vs edge/pulse requests before declaring the feature complete; do not hide ambiguous same-key/same-button interactions behind thread scheduling.
5. Extend automated regression and Input Lab acceptance for multiple timed workers, independent cleanup, stop/Emergency Stop, release order, mixed keyboard/mouse/gamepad output, and coexistence with Held Mapping sources.
6. Run targeted real-game acceptance for the newly affected concurrency/release paths and fix evidence-backed defects.
7. When the complete regression suite, black-box acceptance, and real-use concurrency gate pass, promote directly to **Stable `v1.3.0`**. Publish another Beta only if a public hardening cycle is actually useful.

## Real-use acceptance matrix

Prioritize:

- simultaneous WASD -> left-stick mappings;
- Shift/Ctrl -> analog trigger mappings;
- mouse side buttons -> controller shoulder mappings;
- 2, 3, and 4 Held Mappings held together;
- different press/release orders;
- multiple Held Mappings plus one ordinary timed macro;
- Alt+Tab / foreground transitions and lost-KeyUp recovery;
- Runtime observation source/merged-state accuracy;
- Single-step + Stop / Emergency Stop while waiting;
- repeated start/stop cycles with no stuck keyboard, mouse, trigger, stick, D-pad, or controller-button state.

## Acceptance tooling

Developer-only `tools/InputLab/` v0.2 now covers routine keyboard, mouse, Raw Input and XInput acceptance without launching a real game for every iteration. It provides low-level hook vs foreground Raw Input comparison, XInput button/trigger/stick observation, ordered event logging, and an isolated automated acceptance host that drives the real InputStitch ownership/runtime path and checks final `SendInput` / ViGEm output with explicit PASS/FAIL assertions.

The hardened automated core ownership matrix passed three consecutive `50/50` runs at this breakpoint while a 50 ms foreground sampler confirmed that the acceptance process never became the foreground process. The same stability run verified that the user's normal `%APPDATA%\InputStitch\config.xml` SHA-256, size and modification time were unchanged before/after acceptance. Acceptance-only injected keyboard/mouse outputs are swallowed after observation so they do not leak into the user's current foreground application.

The runner now performs a real ViGEm/XInput report preflight before controller assertions. A local XUSB/ViGEm stack that enumerates a controller but fails to propagate state is reported as `SUMMARY: BLOCKED` (exit code 2), not as a product regression. This materially reduces routine manual testing, but does **not** replace final real-game compatibility checks, especially Alt+Tab/lost-KeyUp, privilege boundaries, exclusive-fullscreen and game-specific input stacks.

Next tooling candidates are target-window message comparison, DS4/DirectInput/HID observation, longer soak/repeated-cycle scenarios and machine-readable report export.

## Gate for Layer

Layer is no longer the next structural feature. **Concurrent Macro Runtime has higher priority and is part of the Stable 1.3.0 gate.** Do not begin Layer until Stable 1.3.0 is released and the concurrent runtime has demonstrated reliable cleanup/ownership behavior, unless the user explicitly reprioritizes it again.

The planned first Layer scope remains documented for later work:

- `Base + one active Layer`;
- Layer affects Held Mapping eligibility first;
- it must compose with the multi-worker Concurrent Macro Runtime rather than restoring a single global worker assumption;
- changing Layer removes old-layer sources before changing eligibility;
- keys already physically held before a Layer switch are not synthetically re-triggered; they must be released and pressed again;
- Emergency Stop remains above Layer selection and all mapping priority logic.

## Release discipline

Stable `v1.2.0` remains the rollback baseline while 1.3.x is being evaluated. Beta releases remain prereleases and use `InputStitch-beta.xml`; they must not alter the Stable latest/update path.
