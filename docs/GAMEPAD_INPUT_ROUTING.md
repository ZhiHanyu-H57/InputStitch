# Physical Gamepad Input + Hybrid Controller Routing design note

Status: **XInput input, controller triggers, hybrid macros and multi-controller aggregation are implemented in `v1.3.1-beta.1`; Controlled Replacement is experimental and incomplete.**

状态：**`v1.3.1-beta.1` 已实现 XInput 手柄输入、手柄触发、混合宏和多手柄汇总；受控接管目前仍是实验性、不完整状态。**

## Product goal / 产品目标

InputStitch should provide one common controller platform where physical/other virtual controllers can become input sources, macros can generate keyboard/mouse/controller output, and multiple controllers can be merged into one controlled virtual gamepad when needed.

目标不是简单“手柄转键盘”，而是：

```text
Physical / other virtual controllers
Keyboard / Mouse
Macro / Idle sources
        ↓
Input source normalization
        ↓
Trigger + Layer eligibility
        ↓
Concurrent Macro Runtime / Router
        ↓
Output Ownership
        ↓
Keyboard / Mouse / optional Virtual Gamepad
```

The common Trigger / Runtime / Output Ownership architecture remains authoritative. Do not build a second controller-only macro engine.

## Implemented input backend / 已实现输入层

### XInput scope

`XInputInputService` currently observes Windows XInput user indices `0..3`.

Implemented controls:

- A/B/X/Y;
- D-pad;
- LB/RB;
- L3/R3;
- Start/Back where exposed;
- LT/RT as analog threshold triggers;
- full stick/trigger state for Router aggregation.

LT/RT trigger semantics:

```text
activate: >= 80%
release:  <= 70%
```

This hysteresis prevents analog noise near one threshold from repeatedly starting/stopping Hold behavior.

Stick-region-as-trigger is not implemented yet. Sticks are routed as analog state but are not arbitrary trigger regions.

## Self-feedback prevention

InputStitch's own ViGEm Xbox user index is queried dynamically and excluded from `XInputInputService`.

Bad loop prevented:

```text
macro / Router output → InputStitch ViGEm Xbox
                       ↓
                  XInput polling
                       ↓
          [own slot excluded here]
```

Do not replace this with a fixed “slot N is ours” assumption because XInput slot assignment can change.

## Controller trigger semantics

Configured controller triggers use `GamepadControl` + `GamepadUserIndex`.

Public capture currently represents “this control on any visible external XInput controller.” For Hold behavior, dispatch clones the configured trigger and pins the actual XInput slot that produced the Down edge.

This ensures:

```text
Controller A presses A → starts Hold run pinned to Controller A
Controller B releases A → does NOT stop Controller A's run
Controller A releases A → stops its own run
```

Disconnect/lost-Up fallback evaluates the pinned controller trigger.

## Hybrid mapping / 混合映射

Controller triggers reuse the normal macro engine, so they can launch:

- keyboard output;
- mouse output;
- virtual gamepad output;
- keyboard + mouse + controller mixed sequences;
- ordinary timed/Toggle macros;
- Advanced/complex Hold;
- lightweight Parallel Held Mapping.

### Augmentation vs replacement

Without device hiding, the original physical/virtual controller remains visible to the target application.

Example:

```text
Physical LB
  ├─ game receives original LB
  └─ InputStitch macro emits Keyboard Tab
```

That is augmentation, not suppression.

Do not label an ordinary controller-trigger macro as “replace original button.” True replacement requires the Controlled Replacement stage.

## Multi-controller Router / aggregation

`GamepadRouterService` implements the routing half of the platform.

For each visible external XInput slot:

```text
XInput N state
      ↓
convert complete controller frame
      ↓
Output Ownership source router:xinput:N
      ↓
merged virtual-controller state
```

Routed frame includes:

- digital buttons;
- D-pad;
- left/right sticks;
- LT/RT.

Each source replaces its whole persistent state atomically. Identical frames do not submit redundant updates.

Merge rules reuse Output Ownership:

- digital output: reference ownership;
- triggers: maximum requested value;
- sticks: vector sum + circular normalization;
- D-pad: existing axis conflict rules.

A controller disconnect/recenter/release removes only that controller's contribution.

### Slot limitation

For a game that reads only XInput slot 0:

```text
InputStitch virtual Xbox = slot 0
other controllers = slot 1/2/3
→ routed result can be visible through InputStitch slot 0
```

If InputStitch itself is slot 1/2/3, Router still reads/merges other controllers, but a slot-0-only game may ignore the routed virtual result.

Router therefore reports the InputStitch virtual Xbox user index explicitly.

## Optional virtual-controller creation

`v1.3.1-beta.1` adds a third virtual-controller preference:

- Xbox 360;
- PS4 / DualShock 4;
- **Do not create a virtual controller**.

“No virtual controller” is a persisted output preference, not a controller-input disable switch.

When selected:

- external XInput input/trigger polling still works;
- keyboard/mouse macros still work;
- saved gamepad-output macros do not create ViGEm at startup;
- editing a gamepad step does not create ViGEm;
- a runtime path that truly needs virtual output is refused rather than silently overriding the preference;
- Idle Gamepad is disabled;
- Router/Controlled Replacement requires Xbox output.

This separation is important to the platform architecture:

```text
controller INPUT capability ≠ virtual-controller OUTPUT capability
```

## Controlled Replacement / handover

Full controlled replacement aims for:

```text
Original controller(s)
        ↓
InputStitch remains allowed to read
        ↓
Original device hidden from ordinary target applications
        ↓
Router / macros / Output Ownership
        ↓
one intended game-visible InputStitch virtual controller
```

### Implemented experimental foundation

`ControlledReplacement.cs` currently provides:

- `IDeviceHidingBackend`;
- `HidHideCliBackend`;
- `--dev-gaming` device discovery parsing;
- hidden-device list parsing;
- application whitelist list parsing;
- cloak-state inspection;
- explicit app registration/unregistration;
- explicit device hide/unhide;
- 10-second command timeout;
- transaction coordinator;
- crash-recovery journal;
- experimental user UI.

InputStitch does **not** install HidHide automatically.

### Hard safety gates

Current activation order:

```text
1. hiding backend available?
2. Router ready?
3. user explicitly selected device(s)?
4. stop managed controller output / neutralize Router sources
5. disconnect InputStitch virtual Xbox once and re-enumerate to remove own-device identities
6. if slot 0 is not already stable, temporarily disable every present external XUSB source (non-persistent PnP disable)
7. reconnect InputStitch first and hard-verify target slot 0
8. re-enable every external XUSB source and require every cycled XUSB identity to return while InputStitch remains slot 0
9. re-establish Router and external source visibility; re-check slot 0
10. pass only explicitly selected external HidHide identities into the hiding transaction
11. snapshot existing HidHide state
12. persist HidHide recovery journal BEFORE mutation
13. whitelist InputStitch if needed
14. hide only selected entries that were not already hidden
15. enable cloak if needed
16. verify Router / target slot / source visibility / cloak state
17. Active
```

Any activation failure triggers rollback of **only changes InputStitch owns**.

Existing user HidHide entries, app whitelist entries and cloak state are preserved.

### Crash recovery

Before the first hiding mutation, InputStitch stores:

- application path added by InputStitch;
- hidden device paths added by InputStitch;
- original cloak state.

If the process dies abnormally, the next launch attempts to restore those InputStitch-owned changes before starting any new takeover.

Emergency Stop and normal shutdown also attempt to restore original visibility.

## Implemented: safe target-slot acquisition

`SlotAcquisition.cs` implements the previously missing recoverable target-slot transaction.

The important separation is:

```text
slot-acquisition scope = every present external XUSB source that can occupy XInput 0..3
HidHide scope         = only external devices the user explicitly selected for takeover
```

This matters when, for example, the user selects Controller B for takeover but an unselected Controller A currently occupies XInput 0. Cycling only B cannot guarantee that InputStitch becomes player 1/slot 0. The transaction therefore temporarily re-enumerates **all** present external XUSB sources, lets InputStitch reconnect first, then restores every source. Unselected sources are never forwarded into HidHide.

The PnP portion uses Windows Configuration Manager device-instance operations rather than VID/PID guessing. Temporary disable is deliberately non-persistent and has its own `slot-acquisition-recovery.xml` journal, separate from `controlled-replacement-recovery.xml` used for HidHide. A previous unresolved recovery journal blocks a new transaction instead of being overwritten.

Hard properties:

- own virtual-controller identities are filtered by disconnecting InputStitch once and comparing the re-enumerated device set;
- if InputStitch initially is not slot 0, elevation is required before any runtime/PnP mutation;
- every XUSB source that InputStitch plans to cycle must be confirmed enabled before mutation;
- more than three external XUSB sources is refused before disable because InputStitch + external controllers must fit XInput's four slots;
- recovery information is persisted before the first PnP disable;
- every cycled device must be confirmed disabled before InputStitch reconnects;
- InputStitch must be observed in slot 0 before any external source is restored;
- after restore, every specific XUSB identity must return—not merely the same controller count;
- InputStitch must still be slot 0 after restore and Router/source preparation;
- `ControlledTakeoverPipeline` makes HidHide Begin unreachable until all of those gates succeed;
- any failure rolls back/recovers before the HidHide stage.

The old unsafe flow remains explicitly forbidden:

```text
hide old physical slot 0
→ hope InputStitch becomes slot 0
```

## Real Windows/ViGEm slot-order evidence

`tools/InputLab/run-slot-order-probe.ps1` is a developer-only neutral probe. It creates four temporary ViGEm Xbox controllers and never submits a button, stick or trigger report.

Latest observed order on the development laptop:

```text
initial:
  external = 0 / 1 / 2
  InputStitch test device = 3

after all are removed and InputStitch reconnects first:
  InputStitch test device = 0
  restored external = 1 / 2 / 3

EXPECTED_ORDER=True
```

This validates the Windows/ViGEm connection-order mechanism used by the slot-acquisition design at the maximum controller count the current XInput backend can support. It is not a substitute for physical PnP + HidHide + game acceptance.

## Remaining identity problem: broader device backends

XInput user index is not a durable physical-device identity. Current XInput takeover uses HidHide's `deviceInstancePath` / `xusbDeviceInstancePath` relationship and re-enumeration-based self filtering. Broader non-XInput support will still need correlation among:

- HID/PnP device instance path;
- device/container identity;
- XUSB identity;
- XInput user slot;
- InputStitch-owned ViGEm instance.

Do not infer which physical device to hide solely from “it is currently XInput N.”

## Real HidHide acceptance still required

The current development laptop has ViGEmBus but no HidHide installation. The Controlled Replacement test suite therefore uses a fake hiding backend and never mutates real devices.

Before claiming full takeover, test on a suitable machine that:

1. InputStitch is allowed through HidHide;
2. selected original device disappears from an ordinary observer/target;
3. InputStitch continues reading that source;
4. routed virtual Xbox remains in the required target slot;
5. stop restores original visibility;
6. crash/relaunch recovery works;
7. pre-existing HidHide configuration is preserved;
8. a real target game sees only the intended controller path.

## Continuous health monitoring — next

Activation-time verification is not enough for long sessions. Future active takeover should periodically validate:

- expected routed source remains visible to InputStitch;
- Router remains enabled/healthy;
- InputStitch virtual pad remains target slot;
- hiding backend remains active as expected.

Unexpected loss should fail safe and restore visibility where possible.

## Broader platform later

After XInput controlled replacement is mature, consider only when justified:

- >4 controllers;
- DirectInput/HID/GameInput;
- Windows-connected DualShock/DualSense input;
- per-device routing identity UI;
- gyro-to-mouse as a separate sensor feature.

Native PlayStation-console support remains out of scope without an official low-friction route.

## Automated coverage

Current relevant suites:

- `XInputInputTests`: 26 checks;
- `GamepadRouterTests`: 28 checks;
- `ControlledReplacementTests`: 39 checks using fake backend/parser only, including the cross-stage “HidHide Begin must remain unreachable” gates;
- `SlotAcquisitionTests`: 27 checks using fake PnP/XInput only; no real device is disabled;
- `VirtualGamepadPreferenceTests`: 15 checks, explicitly no ViGEm device created;
- `LayerTests`: 24 checks;
- `OutputOwnershipTests`: 303,716 checks.

Developer-only real neutral probe:

- `tools/InputLab/run-slot-order-probe.ps1`: PASS on this laptop with the full `external 0/1/2 + own 3 → own 0 + external 1/2/3` transition.

Latest Input Lab result on this laptop:

```text
all 18 pre-XInput real SendInput / concurrency checks PASS
failures=0
then known local ViGEm→XInput environment preflight BLOCKED
SUMMARY: BLOCKED | checks=18 | failures=0
```

## Non-negotiable constraints

- Controller input must use the common trigger/runtime/source model.
- InputStitch must never consume its own virtual output as an external input source.
- No-output preference must not be silently overridden.
- Router aggregation must not be described as interception.
- Controlled Replacement is user-initiated only.
- **Target slot not verified → no device hide.**
- Preserve pre-existing hiding configuration.
- Recovery state must be persisted before hiding mutation.
- Do not start with a custom kernel driver when a mature external filter can satisfy the requirement.
- Anti-cheat/game compatibility claims require evidence; do not promise bypass behavior.
