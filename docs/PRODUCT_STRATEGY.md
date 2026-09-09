# InputStitch product positioning and competitive strategy

Updated: 2026-09-10

## Positioning

**InputStitch is a deterministic input orchestration and macro platform for Windows.**

It combines keyboard, mouse and controller inputs into precise, concurrent and observable automation. The product is not trying to win a hardware-feature-count race against mature all-purpose controller remappers.

中文定位：**InputStitch 是面向 Windows 的精确输入编排与宏平台，让键盘、鼠标和手柄输入能够以可并发、可观察、可恢复的方式统一组合和输出。**

The product center is:

```text
input event / device state
        ↓
trigger + activator + condition
        ↓
concurrent macro/runtime + routing
        ↓
Output Ownership / transforms
        ↓
keyboard + mouse + optional virtual controller
```

Controller routing/takeover is important, but it is one part of the orchestration platform rather than the sole reason the product exists.

## Competitive reference set

This is a strategy reference, not a claim that every listed product has identical scope.

### PadForge

- Project: https://github.com/hifihedgehog/PadForge
- Broad “any input in → virtual controller out” remapper.
- Uses SDL3 for broad device input and HIDMaestro for virtual-controller output.
- Strong breadth: many controller families, macros, shift layers, activators, conditions, curves, touch/gyro, menus, MIDI, remote features and community-profile import.

Strategic implication: **do not measure InputStitch success by supported-device count or exotic-controller feature count.** Competing on breadth alone would turn InputStitch into a smaller imitation of a project already optimized for that goal.

### reWASD

- Product: https://www.rewasd.com/
- Mature commercial remapper with extensive device support, polished UX, activators, Shift layers, analog tuning, gyro, menus and community profiles.

Strategic implication: hardware coverage and polished device-specific UX are expensive long-term advantages. InputStitch should differentiate through orchestration semantics, concurrency, diagnostics and fail-safe behavior instead of attempting feature parity.

### Joystick Gremlin

- Project: https://github.com/WhiteMagic/JoystickGremlin
- General joystick/HOTAS configuration platform.
- Notable strengths include arbitrary joystick support, modes, response curves, conditions, device merging and Python extensibility.

Strategic implication: this is one of the strongest architectural references for **modes, transforms and composable input logic**. InputStitch should learn from the composability without immediately exposing unrestricted scripting that bypasses safety/ownership rules.

### Steam Input

- Documentation: https://partner.steamgames.com/doc/features/steam_controller/action_set_layers
- Strong action-set/layer and activator model integrated into the Steam/game ecosystem.
- Multiple action-set layers can be overlaid, but Valve also documents the complexity and potential confusion of excessive layering.

Strategic implication: richer Layer semantics are useful, but InputStitch should preserve a state model that remains easy to reason about. Add momentary/toggle/cycle semantics before considering arbitrary multi-layer stacking/inheritance.

### DS4Windows 5

- Project: https://github.com/hbashton/DS4Windows
- Deep PlayStation/Nintendo-oriented device support, audio/haptics/adaptive triggers and virtual-controller output.
- The current v5 line uses VIIPER rather than ViGEm for virtual output.

Strategic implication: InputStitch should not become a vendor-feature specialist. Specialized DualSense/gyro/haptics work belongs later and only when a real workflow demands it.

### AntiMicroX / JoyToKey / AutoHotkey

These products represent the opposite side of the market: simple controller→keyboard/mouse mapping or highly programmable keyboard/mouse automation.

Strategic implication: **basic tasks must remain easy.** Advanced architecture must not make “press this → send that” harder than it is in simple tools.

## Current advantages

### 1. Explicit Output Ownership and deterministic concurrency

InputStitch treats persistent output as contributions owned by independent sources rather than a stream of uncoordinated key-down/key-up events.

Important consequences:

- one macro finishing does not release another macro's still-owned output;
- Router, macro, Idle and Held sources merge through one model;
- digital output uses reference ownership;
- analog triggers use deterministic merge rules;
- sticks use vector merge/normalization;
- D-pad conflicts use deterministic axis rules.

This should remain a core architectural differentiator.

### 2. Observable runtime

Runtime Observation and diagnostics expose active runs, source IDs, steps/phases, merged outputs, layers, Router state, virtual slot, takeover state, health and recovery information.

Long-term principle: **complex behavior should be explainable by the product itself.** A user should be able to answer “why is this output active?” without guessing.

### 3. Fail-safe transactions and recovery

The controller-takeover work establishes a broader design pattern:

```text
prepare → verify → mutate → verify → rollback/recover on failure
```

The same philosophy already appears in configuration saves, updates, Emergency Stop, Layer transitions and source-local cleanup.

Reliability is not merely a marketing adjective; it is part of the architecture.

### 4. Keyboard/mouse-only mode has no virtual-controller requirement

The persisted “Do not create a virtual controller” mode keeps InputStitch useful as a keyboard/mouse macro tool and as a controller-triggered keyboard/mouse tool without creating ViGEm devices.

This supports a useful product ladder:

```text
simple keyboard/mouse automation
        ↓
controller-triggered automation
        ↓
virtual controller output / routing
        ↓
experimental controlled replacement
```

## Current strategic weaknesses

### 1. Analog transformation is shallow

Compared with mature remappers, InputStitch lacks a reusable transform layer for:

- deadzones;
- response curves;
- sensitivity/scaling;
- inversion;
- axis zones/threshold bands;
- half-axis mapping;
- configurable analog merge policies.

This is a more important platform gap than adding another controller model.

### 2. Activators and conditions are not yet generalized

InputStitch already has Press/Toggle/Hold, modifier chords, Layers and application/profile context, but they are not yet unified into one reusable activator/condition model.

A future model should make common semantics explicit:

```text
Activator
- On Press
- On Release
- While Held
- Short Press
- Long Press
- Double/Triple Press
- Toggle
- Turbo
```

and composable conditions such as:

```text
Condition
- active Layer
- source DeviceKey
- analog threshold/zone
- another input held
- foreground application/profile
```

This is preferable to accumulating unrelated special-case trigger modes.

### 3. Device identity is still too slot-oriented

XInput user index is runtime state, not durable identity. Future routing and takeover need a stable `DeviceKey` that can correlate provider/device metadata such as:

- provider;
- PnP/container identity;
- device instance path;
- VID/PID;
- serial when available;
- XUSB identity;
- current XInput slot as a transient attribute.

This enables per-device triggers, Router selection, profiles and safe takeover without treating “slot N” as identity.

### 4. Virtual output is coupled too closely to ViGEm

ViGEmBus is officially retired and its repository is archived/read-only: https://github.com/nefarius/ViGEmBus

ViGEm remains the current supported backend and does not need an emergency removal, but new platform code should not assume:

```text
virtual gamepad output == ViGEm
```

The next architecture should introduce an output-backend interface before adding another driver/backend.

Potential future backends to evaluate after abstraction exists:

- HIDMaestro: https://github.com/hifihedgehog/HIDMaestro
- VIIPER server/API: https://github.com/Alia5/VIIPER

Important license note: VIIPER's in-process `libVIIPER` is GPL-3.0 and requires compatible application licensing, while the standalone VIIPER server client libraries are MIT-licensed. InputStitch currently declares no open-source license, so backend selection must include license/deployment review rather than only technical capability.

## Strategic roadmap principles

### Do now / high priority

1. **Virtual Output Backend abstraction** — separate macro/ownership/Router code from ViGEm-specific implementation.
2. **Persistent Device Identity / Device Manager** — establish durable device keys and source diagnostics.
3. **Router source selection + per-device policy** — route chosen devices instead of always aggregating every visible external XInput source.
4. **Analog Transform Engine** — deadzone, curve, sensitivity, inversion, zones and explicit merge policy.
5. **Activator + Condition Engine** — unify press/release/hold/long/double/turbo/context semantics.

### Do after the above foundation

6. Layer ergonomics: Momentary/Hold-to-Layer first; then Toggle/Latch/Cycle when justified.
7. Profile/context improvements built on DeviceKey + Condition.
8. `IInputProvider` abstraction and SDL3 evaluation for broad controller input.
9. Specialized Raw HID/provider work only for capabilities not cleanly exposed by the general provider.
10. Evaluate a second virtual-output backend only after the output interface is stable.

### Later / evidence-driven

- gyro/touchpad/vendor-specific haptics;
- richer DualSense-specific features;
- more than four sources as a natural consequence of non-XInput providers/backends;
- plugin/script API after internal action/condition/transform interfaces are stable;
- Layer stacking/inheritance;
- radial menus/overlays;
- broad device-count support as a product KPI.

## Deliberate non-goals

InputStitch should not optimize for:

- “support the most controller models” as the primary success metric;
- duplicating every reWASD/PadForge feature;
- writing a custom kernel filter when a mature external solution is sufficient;
- unrestricted plugins/scripts before the safe runtime APIs are stable;
- hiding complexity behind silent implicit behavior.

## License / project-governance decision

The repository is source-visible but currently declares no open-source license. This is acceptable for a personal/source-visible project, but it becomes a strategic blocker if the desired future includes:

- third-party contributions beyond focused patches;
- plugin/backend ecosystem;
- community forks/redistribution;
- package-manager distribution;
- dependencies whose integration requires a particular license.

Do **not** choose a license casually as part of an implementation task. Treat it as a separate project-governance decision.

## Product principle

> **Common tasks simple, advanced tasks explicit, every task reliable.**
>
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**

The strategic addition after this review is:

> **Compete on deterministic orchestration, not on device-feature count.**
>
> **竞争重点是确定、可解释的输入编排，而不是硬件功能数量。**
