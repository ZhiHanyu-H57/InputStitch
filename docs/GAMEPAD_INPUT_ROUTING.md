# Physical Gamepad Input + Hybrid Controller Routing design note

Status: **planned; highest-priority structural direction after Stable 1.3.0**.

状态：**已进入正式路线；Stable 1.3.0 之后最高优先级结构性方向。**

## Product goal / 产品目标

InputStitch should allow a physical Windows gamepad to become a first-class input source, then route selected controls to keyboard, mouse, macros, or a virtual controller without throwing away the gamepad's analog strengths.

目标不是“把整个手柄变成键盘”，而是构建 Hybrid Controller：摇杆、扳机等适合模拟量控制的部分保留手柄语义；需要快捷键、直接动作或宏的部分可以映射成键盘/鼠标/宏。

Long-term shape:

```text
Physical Gamepad(s)
Keyboard / Mouse
Macro / Idle / Other sources
        ↓
Input source normalization
        ↓
Trigger / Mapping / Runtime
        ↓
Output Ownership / Routing
        ↓
Keyboard / Mouse / Virtual Gamepad
```

This direction should extend the existing trigger/runtime/source architecture. Do not create a separate controller-only macro engine.

## Why this is useful / 为什么值得做

On PC games such as GTA Online, controller and keyboard/mouse have different strengths. Analog sticks/triggers are useful for steering, throttle, braking and flight, while keyboard/mouse bindings often provide faster direct actions, denser shortcut capacity and more efficient menu/weapon interactions.

A hybrid profile can preserve analog controller behavior while selectively borrowing PC keyboard/mouse actions. This is more useful than globally converting all controller input into keyboard input.

## Phase 1 — Physical XInput controller as Trigger/Input Source

First target: Windows Xbox/XInput-class controllers.

### Initial trigger scope

- A/B/X/Y
- LB/RB
- L3/R3
- View/Menu where practical
- D-pad directions
- LT/RT threshold triggers with hysteresis

Example analog trigger semantics:

```text
activate RT trigger: RT >= 80%
release RT trigger:  RT <= 70%
```

Use separate activation/release thresholds so noisy values near one threshold cannot repeatedly trigger/stop a Hold macro.

Stick-as-trigger regions are not required for the first implementation. Add them only after a concrete scenario defines sensible deadzone/angle/magnitude semantics.

### Runtime integration

A controller trigger should enter the same path used by keyboard/mouse triggers:

```text
physical controller edge/state
        ↓
TriggerSpec / trigger resolver
        ↓
MacroRuntimeClassifier
        ↓
Concurrent Macro Runtime / Parallel Held path
        ↓
Output Ownership
```

This means a controller button can trigger existing keyboard, mouse, virtual-gamepad, delayed, finite, Toggle or Advanced Hold macros without duplicating execution logic.

### Device selection and hot-plug

The first version should prefer one explicitly selected controller source rather than silently consuming every connected controller.

Requirements:

- deterministic selected-controller identity for the current session;
- visible disconnected/reconnected state;
- fail-safe behavior on unplug;
- no silent switch to a different controller if that could trigger macros unexpectedly;
- reconnect behavior must be explicit and testable.

XInput user index alone is not a durable physical-device identity, so the implementation must be careful about re-enumeration and slot changes.

## Critical loop-avoidance requirement / 必须避免自反馈

InputStitch already creates a ViGEm virtual controller for output. Once XInput becomes an input source, the application must not accidentally read its own virtual controller output and feed it back into macros.

Potential bad loop:

```text
Macro → ViGEm virtual A
        ↓
XInput input poll sees A
        ↓
controller trigger fires macro
        ↓
ViGEm virtual A ...
```

Therefore **virtual-output exclusion is a release gate**, not a later optimization.

XInput slot number alone may not be sufficient because slot assignment can change. Investigate reliable ways to identify/exclude the InputStitch-owned virtual device, potentially by correlating the app's virtual output instance with Windows device enumeration rather than assuming a fixed user index.

## Phase 2 — Controller → keyboard/mouse hybrid mapping

Once physical controller triggers are stable, expose practical hybrid mappings:

```text
Left Stick → remains native/virtual gamepad stick
RT/LT      → remains analog gamepad trigger
Button X   → keyboard key / shortcut
LB + D-pad → macro
Other btn  → mouse button / keyboard action
```

The existing macro editor should remain the main output-definition surface; avoid a second simplified mapping model that later diverges from runtime behavior.

### Important limitation: augmentation vs replacement

Without device hiding, the physical controller still reaches the target game directly.

Example:

```text
Physical LB
  ├─ target game receives LB
  └─ InputStitch emits Keyboard Tab
```

So the first controller→keyboard/mouse feature is **augmentation**, not true replacement/suppression.

The UI/documentation must state this clearly. Do not offer a “replace original button” option until the Router/device-hiding path exists and is verified.

### Mixed-input acceptance

Real-game testing should include:

- simultaneous stick + keyboard action;
- controller + mouse output;
- input glyph/UI mode switching;
- weapon/menu state behavior;
- context-sensitive controls;
- whether mixed input causes one API path to cancel or override another;
- repeated start/stop cycles with no stuck keyboard/mouse/controller output.

Game-specific compatibility must be treated as evidence-based; do not assume that every PC game handles simultaneous controller and keyboard/mouse cleanly.

## Phase 3 — Gamepad Router / controlled replacement

Full replacement means the target game should see the routed virtual controller, not the original selected physical device.

Desired shape:

```text
Physical controller
        ↓
 device hiding/filter
   ├─ visible to InputStitch
   └─ hidden from target game
        ↓
InputStitch Router
   ├─ retained controller controls → Virtual Gamepad
   └─ selected controls → Keyboard / Mouse / Macro
        ↓
Target game sees one controlled virtual controller + intended K/M output
```

### First implementation principles

- Do not begin by writing a new InputStitch kernel driver.
- Investigate a mature external hiding/filter mechanism first.
- Explicit enable/disable UI is required.
- Fail-safe unhide/recovery is mandatory.
- Admin privileges, driver signing, Secure Boot and uninstall/recovery behavior must be understood before shipping.
- Anti-cheat compatibility must be treated conservatively; no bypass claims.
- Enabling routing before launching the game is acceptable for v1.
- Seamless post-launch XInput-index takeover is not required for the first version because some games bind controller indices during enumeration/startup.

### Passthrough semantics

Router should eventually allow per-control intent such as:

- pass through as virtual gamepad control;
- remap to another virtual gamepad control;
- convert to keyboard/mouse/macro only;
- optionally ignore/disable a control.

All persistent controller output should continue through Output Ownership / the common routing state rather than direct ad-hoc ViGEm calls from individual mappings.

## Phase 4 — Aggregation and broader controller backends

After single-controller routing is stable:

- multiple physical controllers may contribute to one game-visible virtual controller;
- per-control source priority/merge can reuse Output Ownership concepts;
- physical/virtual controller aggregation may solve games that only bind one controller index;
- broader input backends can be considered: DirectInput, HID, GameInput, Windows-connected DualShock/DualSense.

DualShock/DualSense support on **Windows PC** is in scope only when justified. Native PlayStation-console development is out of scope unless an official low-friction route appears.

Gyro-to-mouse is a distinct later feature because it requires sensor support, calibration, filtering and sensitivity/curve design; it is not part of basic gamepad-trigger support.

## Merge and conflict considerations

Existing Output Ownership remains the baseline for outputs:

- digital output: reference ownership;
- trigger output: max value;
- stick output: vector merge + circular normalization;
- D-pad: existing axis conflict rules.

Physical controller **input** conflict semantics are separate. First version should avoid over-generalizing. Prefer one explicitly selected physical controller and ordinary trigger priority rules before adding multi-controller input arbitration.

## Safety and diagnostic requirements

Before public release of controller-trigger support, add diagnostics for:

- selected physical controller / connection state;
- XInput slot(s) observed;
- own virtual controller exclusion state;
- last controller trigger edge;
- analog threshold/hysteresis state;
- disconnect reason;
- active controller-triggered Run/Source entries in Runtime Observation.

Emergency Stop must remain absolute priority for generated outputs. It does not physically disconnect the user's controller; Router/hiding state, if implemented later, needs its own clearly defined fail-safe recovery behavior.

## Testing plan

### Automated

- button down/up edge detection;
- D-pad directions;
- LT/RT threshold + hysteresis around boundary values;
- no repeated trigger from steady held state unless run mode requires it;
- unplug/reconnect simulated state transitions;
- selected-controller change behavior;
- own virtual controller exclusion / no feedback loop;
- controller trigger → ordinary timed macro;
- controller trigger → Advanced Hold and terminal release;
- controller trigger → keyboard/mouse SendInput;
- coexistence with keyboard/mouse-triggered runs and Parallel Held mappings;
- Emergency Stop source cleanup.

### Real PC/game acceptance

- one real Xbox/XInput controller;
- GTA Online or another mixed-input-friendly target;
- analog driving retained while selected buttons emit keyboard/mouse actions;
- verify whether the original controller action also reaches the game in augmentation mode;
- mixed-input glyph/mode behavior;
- Alt+Tab, reconnect, app restart and game restart;
- later Router stage: hidden physical device + one visible virtual controller + recovery/unhide.

## Product / safety positioning

Position this work around input mapping, accessibility, ergonomics, compatibility and hybrid PC control.

Do not promise:

- anti-cheat invisibility;
- bypass of game restrictions;
- aim assistance or competitive automation;
- universal suppression of physical-controller input before the Router/hiding stage exists.

## Priority relative to Layer

Current order after Stable 1.3.0:

```text
Physical XInput Input Source
        ↓
Controller → Keyboard/Mouse Hybrid Mapping
        ↓
Gamepad Router / Controlled Replacement
        ↓
Optional Aggregation / Broader Backends
        ↓
Layer / Mapping Layer
```

Layer remains valuable and already designed, but it is less important to current real use than completing bidirectional input routing. If later evidence changes that priority, the roadmap may be reprioritized explicitly.
