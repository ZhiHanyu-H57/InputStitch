# PLAN.md

# Current Development Plan

Updated: 2026-09-08

## Current milestone

**Stable 1.3.0 release and transition to Physical Gamepad Input**

Current Stable: `v1.3.0`
Previous public Beta: `v1.3.0-beta.2` (historical)
Previous Stable rollback baseline: `v1.2.0`

Stable 1.3.0 promotes the validated **Input Ownership + unified Concurrent Macro Runtime** built through the 1.3 Beta line. Multiple distinct ordinary timed/Toggle macros and Advanced/complex Hold macros can run concurrently while state-only Parallel Held Mappings remain active. Every worker-backed run has its own RunId/SourceId/stop/timing/trigger-release state, and one authoritative `MacroRuntimeClassifier` drives execution and live eligibility UI.

The Stable source also includes the post-beta.2 trigger fixes from real use: bare ordinary keys remain triggerable while **Shift alone** is held unless a more specific explicit chord exists; Hold supports modifier chords with whole-chord release/lost-KeyUp cleanup; Quick Create follows the same rules. The gear menu now includes a one-click link to the InputStitch GitHub project.

The next highest-priority product direction is **Physical Gamepad Input + Hybrid Controller Routing**, not Layer. The goal is to let a Windows controller become a first-class InputStitch trigger/input source, preserve the analog controls that gamepads are good at, and selectively map controller inputs to keyboard/mouse or macros where PC keyboard/mouse bindings are more efficient. Layer remains designed but moves behind this routing track.

## Immediate work

1. Begin **Physical XInput gamepad as a first-class trigger/input source** after the Stable 1.3.0 publication checkpoint.
2. Start with buttons and D-pad, then LT/RT threshold triggers with hysteresis; do not jump directly to stick regions or device hiding.
3. Reuse the existing trigger selector, Concurrent Macro Runtime and Output Ownership rather than creating a second controller-specific execution engine.
4. Prevent feedback loops by ensuring InputStitch never consumes its own virtual ViGEm output as physical controller input.
5. After controller triggers are stable, add controller→keyboard/mouse hybrid mapping as augmentation; only then evaluate controlled replacement/device hiding.

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
- verify `Shift` held + bare ordinary-key trigger (for example `Shift` macro + separate `E` macro) works concurrently; then add an explicit `Shift+E` trigger and confirm the explicit chord wins;
- verify modifier-chord Hold (`Shift+E`, and optionally a Ctrl/Shift multi-modifier chord) starts only when the terminal key is pressed with required modifiers already down, and releasing either the terminal key or a required modifier stops only that Hold source;

## Acceptance tooling

Developer-only `tools/InputLab/` v0.2 now covers routine keyboard, mouse, Raw Input and XInput acceptance without launching a real game for every iteration. It provides low-level hook vs foreground Raw Input comparison, XInput button/trigger/stick observation, ordered event logging, and an isolated automated acceptance host that drives the real InputStitch ownership/runtime path and checks final `SendInput` / ViGEm output with explicit PASS/FAIL assertions.

The hardened automated core ownership matrix passed three consecutive `50/50` runs at this breakpoint while a 50 ms foreground sampler confirmed that the acceptance process never became the foreground process. The same stability run verified that the user's normal `%APPDATA%\InputStitch\config.xml` SHA-256, size and modification time were unchanged before/after acceptance. Acceptance-only injected keyboard/mouse outputs are swallowed after observation so they do not leak into the user's current foreground application.

The runner now performs a real ViGEm/XInput report preflight before controller assertions. A local XUSB/ViGEm stack that enumerates a controller but fails to propagate state is reported as `SUMMARY: BLOCKED` (exit code 2), not as a product regression. Before that preflight, the current universal runtime build validates both ordinary-timed overlap and **complex Hold overlap** on real SendInput: F9→K and F10→X2 are delayed Advanced Hold timelines, both must remain active together, Runtime observation must classify both as Concurrent Advanced Hold, releasing F9 must leave F10/X2 active, and each release must clear only its own output. All **18 pre-XInput checks pass** on the laptop before the existing ViGEm/XInput preflight reports `BLOCKED | failures=0`. A controller-backed universal-mix scenario (Parallel Held + complex keyboard/mouse/gamepad Hold + ordinary timed macro) is also compiled and runs when that environment preflight is healthy.

Next tooling candidates are target-window message comparison, DS4/DirectInput/HID observation, longer soak/repeated-cycle scenarios and machine-readable report export.

## Highest priority after Stable 1.3.0 — Physical Gamepad Input + Hybrid Routing

This track is intentionally split into stages so InputStitch does not confuse simple controller-trigger support with full device takeover.

### Phase 1 — Physical XInput gamepad as an input/trigger source

- Add physical XInput controller buttons and D-pad directions as first-class triggers.
- Add LT/RT threshold triggers with explicit hysteresis (for example trigger at >=80%, release at <=70%) so analog noise cannot chatter a Hold mapping.
- Prefer one explicitly selected physical controller source in the first version; hot-plug/reconnect must be deterministic.
- Decide stick-as-trigger semantics separately; do not overload the first version with arbitrary analog-region logic unless a concrete use case requires it.
- Reuse the existing macro runtime/output paths so a controller trigger can launch keyboard, mouse, virtual-gamepad or mixed-output macros.
- Prevent feedback from InputStitch's own ViGEm virtual controller. XInput user-index polling alone is not enough if the app can observe its own virtual output; source selection / virtual-slot exclusion is a release gate.

### Phase 2 — Controller → keyboard/mouse hybrid mappings

- Support practical PC hybrid profiles where analog movement/driving controls remain gamepad-native while selected controller buttons trigger keyboard/mouse actions or macros.
- Treat this phase as **augmentation**, not replacement: without device hiding, the game can still receive the original physical controller button at the same time as the mapped keyboard/mouse output.
- Test mixed-input behavior in real games, especially input-mode/glyph switching, weapon/menu states, and simultaneous gamepad + keyboard/mouse interpretation.
- Keep mapping, accessibility, ergonomics and compatibility as the product framing; do not position the feature as anti-cheat bypass or competitive automation.

### Phase 3 — Gamepad Router / controlled replacement

- Route selected/all physical-controller state through InputStitch to one game-visible virtual controller, while still allowing selected controls to become keyboard/mouse/macros.
- Investigate a mature external device-hiding/filter solution before implementation. Do **not** start by writing an InputStitch kernel driver.
- First realistic workflow may require enabling routing before launching the game; seamless takeover after a game has already bound an XInput index is not a v1 requirement.
- Device hiding must have fail-safe recovery/unhide behavior, explicit user control, and compatibility review for admin/signing/Secure Boot/anti-cheat implications.
- Once one-controller routing is stable, extend the same source model to multiple physical/virtual controllers and aggregation if real use justifies it.

### Platform scope

- Target Windows PC only.
- Xbox/XInput-class devices are the first implementation target.
- DualShock/DualSense **connected to Windows** may be added later through DirectInput/HID/GameInput if the incremental cost is justified; gyro-to-mouse is a separate later capability.
- Do not plan a native PlayStation-console version unless an officially supported, low-friction path appears. Remote-play or external-hardware workarounds are outside the current product scope.

Full design note: `docs/GAMEPAD_INPUT_ROUTING.md`.

## Gate for Layer

Concurrent Macro Runtime is implemented and promoted to Stable 1.3.0. **Layer is no longer the first post-1.3 structural feature.** Do not begin Layer until the Physical Gamepad Input / Hybrid Routing core has reached a stable checkpoint, unless the user explicitly reprioritizes Layer again.

The planned first Layer scope remains documented for later work:

- `Base + one active Layer`;
- Layer affects Held Mapping eligibility first;
- it must compose with the multi-worker Concurrent Macro Runtime rather than restoring a single global worker assumption;
- changing Layer removes old-layer sources before changing eligibility;
- keys already physically held before a Layer switch are not synthetically re-triggered; they must be released and pressed again;
- Emergency Stop remains above Layer selection and all mapping priority logic.

Layer must remain an eligibility/grouping feature layered on top of the common input/routing/runtime architecture. It must not become a separate controller-input engine.

## Release discipline

Stable `v1.3.0` is the current recommended release; `v1.2.0` remains the previous rollback point. Historical beta.1/beta.2 tags and prereleases remain immutable and must not be overwritten. Future Beta releases, when needed, continue to use the isolated `InputStitch-beta.xml` channel.
