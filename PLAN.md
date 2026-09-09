# PLAN.md

# Current Development Plan

Updated: 2026-09-09

## Current milestone

**`v1.3.1-beta.1` — 手柄输入 / 多手柄汇总 / 全类型 Layer / 受控接管安全基础**

- Current Stable: `v1.3.0`
- Current prerelease target: `v1.3.1-beta.1`
- Branch: `main`
- Stable `v1.3.0` remains the recommended rollback baseline and must not be overwritten by this Beta.

## What is implemented in 1.3.1-beta.1

### 1. Physical XInput input and controller triggers — implemented

- Poll all visible XInput user slots `0..3` at low latency.
- Dynamically exclude InputStitch's own ViGEm Xbox slot to prevent output → input feedback.
- Controller buttons, D-pad and LT/RT can be first-class macro triggers.
- LT/RT use `>=80%` activation / `<=70%` release hysteresis.
- Startup/reconnect establishes a baseline before emitting edges; a control already held at reconnect does not create a phantom trigger.
- Disconnect synthesizes the necessary release events.
- Configured controller triggers mean “this control on any visible XInput controller”; a Hold run pins itself to the controller that actually started it so another controller cannot stop it accidentally.

### 2. Controller → keyboard/mouse/virtual-gamepad hybrid mapping — implemented

Controller triggers reuse the existing Trigger → Concurrent Macro Runtime / Parallel Held → Output Ownership path. A controller can therefore start:

- keyboard output;
- mouse output;
- virtual-controller output;
- mixed keyboard + mouse + virtual-controller macros;
- timed, Toggle, finite/infinite Hold and lightweight Held Mapping behavior.

Without device hiding this is still augmentation: the original physical controller remains visible to games.

### 3. Multi-controller Router / aggregation — implemented for visible XInput slots

- Every non-InputStitch XInput slot becomes an independent `router:xinput:N` source.
- The whole controller state is mirrored: buttons, D-pad, left/right sticks and LT/RT.
- Source state is replaced atomically through Output Ownership; identical frames are no-ops.
- Multiple controllers merge with the same deterministic ownership rules already used by macros.
- Disconnect/recenter/release removes only that source's contribution.

Important limitation: Router by itself **does not hide the original controller**. The game may see both the original controller and the routed InputStitch virtual controller.

### 4. Optional virtual-controller creation — implemented

The existing virtual-controller-type dropdown now contains:

- Xbox 360;
- PS4 / DualShock 4;
- **Do not create a virtual controller / 不创建虚拟手柄**.

When “do not create” is selected:

- saved gamepad-output macros do not cause a ViGEm device to appear at startup;
- editing/saving a gamepad output step does not connect ViGEm;
- keyboard/mouse macros still work;
- physical/other XInput controllers can still trigger keyboard/mouse macros;
- a macro that actually requires virtual-controller output is refused rather than silently overriding the preference;
- Idle Gamepad is disabled because it requires virtual output;
- Router/controlled takeover requires Xbox output and therefore cannot remain in the no-output state.

Fresh-install default remains Xbox 360 for compatibility. Changing that default is a separate product decision.

### 5. Layer — implemented for every macro type

Current Layer v1 model:

- mandatory Base layer, always eligible;
- one additional active layer (`Layer 1`) at a time;
- active layer is runtime-only and resets to Base-only after restart;
- every macro type can belong to a layer: ordinary timed, Toggle, Advanced/complex Hold and Parallel Held Mapping;
- duplicate triggers still use normal list priority inside the currently eligible set;
- leaving a layer stops that layer's active runs and removes only those sources;
- a trigger already physically held when a layer becomes eligible must release and press again before it can start.

### 6. Controlled Replacement — experimental safety foundation implemented, full takeover NOT complete

Implemented:

- mature external device-hiding backend abstraction; current implementation targets HidHide CLI rather than a new InputStitch kernel driver;
- explicit user-selected device list; nothing is hidden automatically;
- hard prerequisite: Router ready;
- recoverable XInput slot-acquisition transaction before HidHide: disconnect own virtual Xbox, temporarily re-enumerate every present external XUSB source when needed, reconnect InputStitch first, hard-verify slot 0, restore all external sources, then re-verify their identities and Router visibility;
- slot-acquisition scope is intentionally broader than hiding scope: all external XUSB sources may be cycled to free slot 0, while only explicitly selected external identities proceed to HidHide;
- Windows PnP disable is non-persistent and has its own crash-recovery journal separate from the HidHide recovery journal;
- more than three external XUSB controllers is rejected before PnP mutation because InputStitch + external sources must fit the four XInput slots;
- HidHide Begin is unreachable unless slot acquisition, target-slot verification, Router preparation and external-source verification all succeed;
- preserve pre-existing HidHide hidden devices, app whitelist and cloak state;
- verify Router/target slot/source visibility after hiding;
- rollback only InputStitch-owned changes on activation failure;
- recovery journal is persisted before the first HidHide mutation so an abnormal exit can be repaired next launch;
- Emergency Stop and normal shutdown attempt to restore original controller visibility first;
- experimental UI under Tools → Controller takeover / 手柄接管.

Not complete / not yet claimed:

- real physical-controller PnP disable/enable acceptance on hardware (automated tests use fake PnP; neutral real ViGEm order has been validated);
- reliable identity correlation beyond XUSB/HidHide `xusbDeviceInstancePath` for arbitrary non-XInput hardware layouts;
- real HidHide hardware acceptance on this development laptop (HidHide is not installed here);
- guaranteed game-specific original-device invisibility;
- DirectInput/HID/GameInput or >4-source aggregation.

## Immediate next engineering work

1. Validate the implemented slot-acquisition + HidHide pipeline with one or more **physical** XInput controllers on a machine where HidHide is installed.
2. Prove the selected original controller disappears from an ordinary observer/target while InputStitch remains whitelisted and keeps reading/routing it.
3. Add continuous health monitoring while Controlled Replacement is active. Any loss of routed source, target slot or hiding backend health should disengage and restore visibility when possible.
4. Exercise physical starting layouts with InputStitch initially in slots 0/1/2/3 and up to three external XInput sources.
5. Only after those real-device/game checks are proven, call the feature “full controller takeover/replacement.”
6. After XInput takeover is mature, evaluate broader controller backends only when concrete use requires them.

## Current automated evidence

Latest clean regression on 2026-09-09:

- 323 keyboard/trigger checks;
- 58 Idle Gamepad assertions;
- 26 XInput input checks;
- 28 Gamepad Router checks;
- 39 Controlled Replacement cross-stage transaction/recovery checks (fake backend; HidHide not invoked);
- 27 slot-acquisition/PnP recovery checks (fake PnP/XInput; no real device disabled);
- 15 optional-virtual-controller preference checks (`no ViGEm device created`);
- 24 Layer checks;
- 7 macro-timing checks;
- modifier safety suite: PASS;
- release-channel policy: PASS;
- 43 updater install/rollback checks;
- 18 update-network interruption/retry checks (injected failures only; no network request);
- 23 UI safety/diagnostics checks;
- 358 productivity/config/UI checks;
- 303,716 Output Ownership checks;
- zh-CN/en-US Settings smoke at 700×660 and 604×441;
- legacy XML compatibility and gamepad-vector editor smoke: PASS.

Additional real neutral-device evidence:

- `tools/InputLab/run-slot-order-probe.ps1` creates four temporary neutral ViGEm Xbox devices without submitting buttons/sticks/triggers;
- observed initial order: external `0/1/2`, InputStitch test device `3`;
- after removing all and reconnecting InputStitch first: InputStitch `0`, restored external devices `1/2/3`;
- this validates the Windows/ViGEm ordering mechanism used by the slot-acquisition transaction, but does not replace physical PnP + HidHide acceptance.

Input Lab final run:

- all 18 pre-XInput real `SendInput` / ordinary-concurrency / complex-Hold checks PASS;
- `failures=0`;
- this laptop then reaches the known local ViGEm→XInput observation blocker and reports `SUMMARY: BLOCKED | checks=18 | failures=0` rather than a product failure.

## Release discipline

`v1.3.1-beta.1` is a **prerelease**, not Stable.

Before publication:

1. keep public Chinese text plain-language first, technical detail later;
2. update PLAN / ROADMAP / HANDOFF / design notes to match actual implementation;
3. rebuild x64/x86 and source archive from the final tree;
4. run release verification and clean regression;
5. inspect `git diff --check` and working tree;
6. commit and push;
7. publish `v1.3.1-beta.1` as a GitHub prerelease without moving or replacing `v1.3.0` / `releases/latest`;
8. verify remote assets and release metadata after publication.

## Non-negotiable design constraints

- Emergency Stop remains global and highest priority.
- Source-local cleanup must never regress into global neutralization during ordinary run completion.
- InputStitch's own virtual controller must never be consumed as an external controller source.
- “Do not create virtual controller” must be respected; runtime may refuse an incompatible action but must not silently create ViGEm.
- Controlled Replacement must fail closed: **target slot not verified → no hide**.
- Do not write a new kernel filter driver while a mature external hiding/filter route is sufficient.
- Do not claim suppression/replacement while the original device remains visible to the target.
- Layer is only eligibility/grouping on top of the common runtime; it must not become a second execution engine.
- Stable and Beta update/release channels remain isolated.
