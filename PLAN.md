# PLAN.md

# Current Development Plan

Updated: 2026-09-08

## Current milestone

**1.3.x runtime ownership acceptance and hardening**

Current public Beta: `v1.3.0-beta.1`
Stable rollback baseline: `v1.2.0`

The Input Ownership foundation, multi-source Held Mapping support, one-ordinary-macro coexistence, runtime observation, and single-step execution are implemented and published in Beta 1. The next decision is evidence-driven: validate this architecture in real use before stacking another structural runtime feature on top of it.

## Immediate work

1. Real-use acceptance of `v1.3.0-beta.1`.
2. Fix any ownership, release, merge, observation, or coexistence defects found in that testing.
3. Publish `1.3.0-beta.2` only if another public hardening cycle is actually needed.
4. After ownership behavior is accepted as stable, begin the first Layer implementation defined in `docs/LAYER_DESIGN.md`.

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

A developer-only `tools/InputLab/` black-box target now covers routine keyboard, mouse and XInput acceptance without launching a real game for every iteration. Use it to inspect injected scan-code keyboard events, mouse buttons/wheel/movement, virtual Xbox buttons/triggers/sticks and exact event ordering. It reduces the cost of the ownership acceptance matrix but does **not** replace final real-game compatibility checks.

Next tooling hardening candidates are Raw Input comparison, target-window message comparison, DS4/DirectInput/HID observation and scripted expected-vs-observed assertions.

## Gate for Layer

Do **not** implement Layer merely because it is next on the roadmap. Start Layer only after the ownership Beta has enough real-use evidence that failures can be separated from Layer behavior, unless the user explicitly overrides this gate.

The planned first Layer scope remains:

- `Base + one active Layer`;
- Layer affects Held Mapping eligibility first;
- ordinary timed macro remains the single global worker;
- changing Layer removes old-layer sources before changing eligibility;
- keys already physically held before a Layer switch are not synthetically re-triggered; they must be released and pressed again;
- Emergency Stop remains above Layer selection and all mapping priority logic.

## Release discipline

Stable `v1.2.0` remains the rollback baseline while 1.3.x is being evaluated. Beta releases remain prereleases and use `InputStitch-beta.xml`; they must not alter the Stable latest/update path.
