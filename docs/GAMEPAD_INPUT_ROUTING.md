# Physical Gamepad Input + Hybrid Controller Routing design note

Status: **The 1.3.1 Beta line implements XInput input, controller triggers, hybrid macros, multi-controller aggregation, recoverable slot-0 acquisition and experimental HidHide Controlled Replacement. `v1.3.1-beta.2` adds continuous takeover health monitoring and fail-safe automatic disengage; physical-controller + HidHide + game acceptance is still pending.**

状态：**1.3.1 Beta 已实现 XInput 手柄输入、手柄触发、混合宏、多手柄汇总、可恢复的 0 号槽位取得和实验性 HidHide 接管；`v1.3.1-beta.2` 又加入接管运行自检与异常自动脱离恢复。由于当前没有实体测试手柄和 HidHide 环境，最终硬件验收仍未完成。**

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

## Persistent Device Identity foundation — implemented on post-v1.4.3 main

XInput user index is not a durable physical-device identity. The first identity layer is now implemented independently of Router/takeover policy:

- a separate atomic `devices.xml` registry stores opaque versioned `DeviceKey` records;
- XInput user slot is never used to generate a DeviceKey;
- identity evidence can include Windows PnP Container ID / container path, VID/PID + serial, XUSB identity and HID/PnP instance path;
- aliases are retained so stronger later evidence can enrich an existing record without changing its key;
- native Windows XUSB/PnP discovery works when HidHide is absent;
- HidHide can enrich metadata when installed;
- InputStitch-owned Xbox/DS4 virtual output uses a fixed product identity, while other virtual-bus provider rows are labeled separately;
- Device Manager presents current XInput slots as transient observations and keeps them explicitly unresolved when a stable provider↔slot correlation cannot be proven.

Router Source Policy stage 1 now consumes DeviceKey only where a current runtime correlation is established. The Router's low-level source ownership IDs remain slot-shaped (`router:xinput:N`) because those are transient execution channels, not identity. Do not infer which physical device to route or hide solely from “it is currently XInput N.”

## Router Source Policy stage 1 — implemented on post-v1.4.3 main

The Router no longer has only one unconditional “aggregate every external XInput slot” behavior. It now has two explicit policy modes:

- `AllVisible` — the backward-compatible default; every visible non-own XInput source is routed exactly as before;
- `SelectedDevices` — persists a set of stable `DeviceKey` values and routes only runtime sources that can currently be correlated to one of those selected devices.

Safety rules are deliberately conservative:

- XInput slot is never persisted as device identity;
- the current provider can correlate a source only in the cardinality-1 case: exactly one visible external XInput source and exactly one present non-virtual device with current native Windows XUSB metadata;
- HidHide/enrichment metadata that merely contains an XUSB-looking path is not sufficient proof by itself;
- two or more external XInput sources remain unresolved because XInput does not expose a durable device path and discovery order must not be treated as slot order;
- `XInputInputService` maintains a monotonic topology generation. Connect/disconnect, own-slot movement and source-count changes invalidate an old DeviceKey↔slot snapshot immediately;
- while the identity refresh catches up, `SelectedDevices` fails closed and clears any stale Router-owned output rather than allowing a newly occupying device to inherit an old permission;
- after a stable one-to-one topology is refreshed, the same selected DeviceKey can resume routing even if its transient XInput slot changed.

Settings exposes this as “选择汇总来源… / Choose routed controllers...”. Known offline devices remain selectable for future reconnect; currently unresolved devices are labeled as such. Runtime Observation/Diagnostics report policy, blocked source count and unresolved source count.

Experimental Controller Takeover is temporarily restricted to `AllVisible`. The hiding transaction predates per-device Router policy and must not be allowed to hide an original controller that the Router may then intentionally omit. Integrating takeover with selected-device policy is a later hardware-acceptance/identity transaction change, not something to guess into this stage.

## Analog Transform Engine stage 1 — implemented on post-v1.4.3 main

Router analog shaping is now a standalone pre-ownership layer rather than ad-hoc math inside `GamepadRouterService` or the ViGEm backend. The runtime path is:

```text
XInput report
  → InputSpec conversion
  → AnalogTransformEngine (per routed source)
  → Output Ownership merge
  → virtual output backend
```

The serializable `AnalogTransformProfile` has independent left/right stick and trigger settings. Stick transforms support radial inner/outer deadzone, response curve, scaling, X/Y inversion and final max-output clamp. Trigger transforms support inner/outer deadzone, curve, scaling and final clamp. The fixed operation order is **deadzone remap → response curve → scale → inversion → max-output clamp**.

Response curve uses an exponent percentage: `100` is linear, `200` is squared and `50` is square-root-like. Stick deadzones are radial, so a diagonal direction is preserved instead of being distorted by separate square X/Y deadzones. Default/legacy profiles are exact identity and bypass all transform math, preserving historical Router percentages and diagnostics text.

Stage 1 also exposes reusable signed half-axis conversion and magnitude-zone classification primitives. Activator/Condition stage 1 now consumes those primitives for deterministic analog-zone conditions; they still are not arbitrary stick-region triggers.

Cross-source merge semantics remain owned by Output Ownership and are unchanged: sticks use normalized sum, triggers use max and digital controls retain source ownership. Alternative priority/latest/average merge policies are intentionally deferred rather than creating a second merge engine before multi-device identity can support them safely.

Settings exposes this through Router source selection → “模拟量处理... / Analog transform...”, with separate left/right stick and trigger tabs, live 25/50/75/100% previews and reset-to-default.

## Activator + Condition stage 1 — implemented on post-v1.4.3 main

Controller identity and analog state can now participate in the same macro activation rule model as keyboard/mouse triggers without moving execution into the Router. The boundary is:

```text
physical trigger edge + current deterministic state
  → ActivatorRuntimeEngine timing/edge decision
  → Condition evaluation (Layer + foreground + DeviceKey + analog zone)
  → existing Concurrent Macro Runtime / Parallel Held Mapping
  → Output Ownership
```

`Legacy` remains the exact compatibility adapter for the existing Toggle/Hold behavior. Opt-in advanced modes are Press, Release, While Held, Long Press and Double Press. The old Toggle/Hold selector is disabled in the editor while an advanced activator owns semantics so there are not two competing runtime interpretations.

DeviceKey conditions never trust a remembered XInput slot. They use the same current topology generation and proven DeviceKey↔slot binding as Router Source Policy; stale/unresolved identity fails closed. A controller-triggered macro with a DeviceKey condition must originate from that proven runtime slot. A keyboard/mouse-triggered macro may use DeviceKey as a presence/source-state condition, optionally combined with an analog zone on that same device.

Analog conditions use controller percentage state and the shared magnitude/signed-half-axis zone primitives. Stage 1 supports left/right trigger magnitude, left/right stick magnitude, and ±X/±Y half-axis bands. This is intentionally an additional condition on an ordinary trigger, not yet “move the stick into a region and synthesize a trigger edge.”

Wheel directions remain edge-only: Press and Double Press can consume wheel edges, but Release/While Held/Long Press refuse to invent a persistent down/release lifecycle. If a While Held condition becomes false, only that macro's source is stopped. A condition-invalid press also clears/disarms Double Press history so an invalid middle press cannot bridge two later/earlier valid edges.

Short Press, Triple Press, Turbo/repeat, another-input-held and richer analog-region/direct-axis triggers remain explicit stage-2 candidates.

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

## Continuous takeover health monitoring — implemented in beta.2

Activation-time verification is not enough for long sessions. `ControlledReplacementHealthMonitor` now runs potentially slow HidHide checks on a background timer rather than the WinForms/XInput polling thread.

While takeover is Active it validates:

- HidHide backend remains available;
- Router remains enabled/ready;
- InputStitch virtual Xbox remains the required target slot 0;
- every XInput source expected after takeover remains visible to InputStitch;
- HidHide cloak remains active;
- every user-selected original controller remains in hidden-device state;
- InputStitch remains in the HidHide application whitelist.

The activation check and the ongoing check deliberately use different source-health mechanics:

- activation performs a short settle, actively polls XInput, and proves the hidden originals remain readable before declaring Active;
- ongoing monitoring reads the latest XInput snapshot maintained by the normal 10 ms UI poll and does **not** call `Poll()` on the background thread, so controller edge generation stays single-threaded.

Failure policy:

```text
unhealthy once → record failure, keep takeover active
healthy next   → reset streak
unhealthy twice consecutively → signal one fail-safe disengage
```

On confirmed failure the monitor stops first, then UI-thread recovery:

1. calls Controlled Replacement Stop to restore InputStitch-owned HidHide changes;
2. stops and disables Router;
3. clears managed Output Ownership state;
4. neutralizes virtual-controller output;
5. persists Router disabled so restored original input is not simultaneously duplicated by aggregation;
6. retains the recovery journal and warns the user if visibility restoration itself cannot complete.

The one-failure tolerance reduces false disengagement from short device-enumeration/HidHide CLI transients while two consecutive failures still fail closed.

## Broader platform direction after the 2026 strategy review

Do not grow the controller platform by adding one API/device family at a time. The next architecture should separate input identity, routing policy, transforms and virtual output from today's XInput/ViGEm implementation details.

Completed foundations:

- `IVirtualGamepadBackend` keeps higher-level orchestration independent from concrete ViGEm controller types while ViGEm remains the sole production backend;
- durable `DeviceKey` / Device Manager stage 1 keeps XInput slot as runtime metadata rather than identity and works without HidHide through native Windows XUSB/PnP discovery;
- Router Source Policy stage 1 adds backward-compatible all-visible routing plus conservative DeviceKey-selected routing with topology invalidation and unresolved fail-closed behavior;
- Analog Transform stage 1 adds reusable identity-by-default per-source analog shaping plus half-axis/zone primitives before Output Ownership without changing existing merge semantics;
- Activator + Condition stage 1 adds opt-in Press/Release/While Held/Long Press/Double Press with Layer, foreground, stable DeviceKey and analog-zone conditions while preserving the common runtime/output boundary.

Near-term order:

1. improve Layer ergonomics (Momentary/Hold-to-Layer first) and profile/context behavior on DeviceKey + Condition;
2. add stage-2 Activator/Condition semantics only when stop/repeat/state behavior is explicit;
3. later introduce `IInputProvider`, with SDL3 as the first broad-controller provider candidate.

Only after those boundaries are stable should the project evaluate a second virtual-output backend or specialized Raw HID/device providers.

ViGEmBus is retired/archived, so it must not become more deeply embedded in new orchestration code. It remains the current production backend until an alternative is proven. Candidates such as HIDMaestro or VIIPER must be evaluated for deployment/security/licensing as well as feature breadth.

Features such as >4 controllers, DualSense-specific input, gyro/touchpad/haptics and specialized vendor behavior remain evidence-driven later work rather than standalone roadmap targets.

Native PlayStation-console support remains out of scope without an official low-friction route.

## Automated coverage

Current relevant suites:

- `XInputInputTests`: 26 checks;
- `DeviceIdentityTests`: **33 checks** using fake discovery + temporary `devices.xml`, covering stable/non-slot keys, metadata enrichment, restart/re-enumeration, fixed own-virtual identities, native-XUSB proof gating, topology invalidation, unresolved multi-controller layouts and virtual-bus labeling;
- `RouterSourcePolicyTests`: **22 checks** covering old-config defaults, XML round trip, selected/unselected DeviceKey filtering, Output Ownership release, topology invalidation/recovery, multi-controller fail-closed behavior, legacy `AllVisible` compatibility and source-selection dialog refresh/edit behavior;
- `AnalogTransformTests`: **49 checks** covering exact identity behavior, radial/trigger deadzones, response curves, scaling, inversion, clamps, half-axis/zone primitives, Router pre-ownership integration, XML round trip, diagnostics and dialog smoke;
- `GamepadRouterTests`: 28 checks;
- `ControlledReplacementTests`: **52 checks** using fake backend/parser only, covering activation/rollback gates plus runtime slot/Router/source/HidHide health and consecutive-failure monitor behavior;
- `SlotAcquisitionTests`: 27 checks using fake PnP/XInput only; no real device is disabled;
- `VirtualGamepadPreferenceTests`: 23 checks, explicitly no ViGEm device created;
- `LayerTests`: **39 checks** including arbitrary named layers, switch targets, deletion migration, real XML round trip and side-effect-free observation;
- `OutputOwnershipTests`: 303,716 checks.

Developer-only real neutral probe:

- `tools/InputLab/run-slot-order-probe.ps1`: PASS on this laptop with the full `external 0/1/2 + own 3 → own 0 + external 1/2/3` transition.

Latest Input Lab result on this laptop after Analog Transform stage 1 (2026-09-17):

```text
SUMMARY: PASS | checks=77 | failures=0
```

This real-output acceptance exercised keyboard/mouse SendInput plus ViGEm/XInput output/ownership/concurrency paths. The previously documented ViGEm→XInput environment-only blocker did not reproduce in this run, but remains a valid `BLOCKED` interpretation if it returns later.

## Non-negotiable constraints

- Controller input must use the common trigger/runtime/source model.
- Virtual-controller output must move behind a backend interface before a second backend is added; new runtime/Router code must not deepen ViGEm coupling.
- XInput user index is transient runtime state, not durable device identity.
- DeviceKey-selected routing must fail closed when current provider↔runtime correlation is unresolved; never persist or guess by slot/discovery order.
- InputStitch must never consume its own virtual output as an external input source.
- No-output preference must not be silently overridden.
- Router aggregation must not be described as interception.
- Controlled Replacement is user-initiated only.
- **Target slot not verified → no device hide.**
- Preserve pre-existing hiding configuration.
- Recovery state must be persisted before hiding mutation.
- Do not start with a custom kernel driver when a mature external filter can satisfy the requirement.
- Anti-cheat/game compatibility claims require evidence; do not promise bypass behavior.
