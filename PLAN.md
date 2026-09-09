# PLAN.md

# Current Development Plan

Updated: 2026-09-10

## Current milestone

**Post-`v1.4.1` platform foundation — output backend abstraction / Device Identity / Router policy / Analog Transform**

- Current Stable: `v1.4.1`
- Previous prerelease: `v1.3.1-beta.1` — published and immutable
- Latest historical prerelease: `v1.3.1-beta.2` — published and immutable
- Branch: `main`
- Previous Stable `v1.4.0` remains an immutable rollback reference; `v1.4.1` is the recommended Stable line.

The 2026 competitive review is now an explicit planning input. InputStitch is positioned as a **deterministic input orchestration and macro platform**, not a “support the most controller models” remapper. See [`docs/PRODUCT_STRATEGY.md`](docs/PRODUCT_STRATEGY.md).

## What is implemented in 1.3.1-beta.2

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

### 5. Layer — flexible named layers implemented for every macro type

Current Layer model:

- mandatory Base layer, always eligible;
- any number of additional user-defined layers with stable IDs and editable names;
- one non-Base layer is active at a time, so eligibility is always Base + current layer;
- active layer is runtime-only and resets to Base-only after restart;
- every macro type can belong to a layer: ordinary timed, Toggle, Advanced/complex Hold and Parallel Held Mapping;
- each layer, including Base, may have a keyboard/mouse/controller switch trigger; the Base trigger returns to Base-only;
- user-facing management supports add, rename and delete;
- deleting a layer stops its active runs, moves assigned macros back to Base, and deleting the active layer first returns runtime state to Base-only;
- duplicate triggers still use normal list priority inside the currently eligible set;
- leaving a layer stops that layer's active runs and removes only those sources;
- a trigger already physically held when a layer becomes eligible must release and press again before it can start;
- XML serialization now replaces the explicit layer list, so deleted legacy Layer 1 does not reappear after restart; truly old configs with no layer data still migrate to Base + Layer 1.

### 6. Controlled Replacement — experimental transaction + runtime health monitoring implemented, hardware acceptance NOT complete

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
- after activation, a background health monitor continuously checks target slot, Router readiness, expected XInput source visibility, HidHide cloak state, requested hidden-device membership and the InputStitch application whitelist;
- one transient health failure is tolerated; two consecutive failures dispatch one fail-safe disengage;
- confirmed health failure restores InputStitch-owned HidHide changes, stops/disables Router, clears managed routed output and keeps recovery data if restoration cannot complete, preventing restored original input + routed duplicate input;
- runtime observation/diagnostics expose active layer, virtual slot, Router/takeover health, expected sources and recovery warnings without creating a virtual device merely for observation;
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

Hardware acceptance remains explicitly **blocked until a physical XInput controller is available**. Keep it as a parallel P0 validation lane; do not substitute more virtual devices and claim that as physical acceptance.

The active non-hardware engineering order is now:

1. **Virtual Output Backend abstraction** — introduce a ViGEm-independent output boundary with no behavior change; keep ViGEm as the only production backend initially.
2. **Persistent Device Identity / Device Manager** — establish durable `DeviceKey` + inventory/diagnostics so slot number becomes a transient attribute rather than identity.
3. **Router source selection / per-device policy** — explicitly select routed sources and prepare per-source transform/merge policy.
4. **Analog Transform Engine** — reusable deadzone, curve, scaling, inversion, zones and merge policies.
5. **Activator + Condition Engine** — unify press/release/held/short/long/double/toggle/turbo and deterministic Layer/device/app/axis conditions.
6. After those foundations, improve Layer ergonomics (Momentary/Hold-to-Layer before more complex modes), profile/context behavior, and then evaluate an `IInputProvider` architecture with SDL3 as the first broad-controller candidate.
7. Only after the output backend interface is stable should a second virtual-output backend such as HIDMaestro or the standalone VIIPER server/API be prototyped and compared.

When physical hardware becomes available in parallel:

1. Validate slot-acquisition + HidHide + runtime-health with one or more physical XInput controllers.
2. Prove selected originals disappear from an ordinary observer/game while InputStitch remains whitelisted/readable.
3. Exercise InputStitch starting slots 0/1/2/3 and intentional slot/Router/HidHide faults.
4. Confirm recovery avoids doubled original+routed input and preserves pre-existing HidHide state.
5. Only then call controller takeover “hardware-mature.”

## Current automated evidence

Latest clean regression on 2026-09-10:

- 323 keyboard/trigger checks;
- 58 Idle Gamepad assertions;
- 26 XInput input checks;
- 28 Gamepad Router checks;
- 52 Controlled Replacement transaction/runtime-health/recovery checks (fake backend; HidHide not invoked);
- 27 slot-acquisition/PnP recovery checks (fake PnP/XInput; no real device disabled);
- 15 optional-virtual-controller preference checks (`no ViGEm device created`);
- 39 flexible-Layer/XML/observation checks;
- 7 macro-timing checks;
- modifier safety suite: PASS;
- release-channel policy: PASS;
- 43 updater install/rollback checks;
- 18 update-network interruption/retry checks (injected failures only; no network request);
- 26 UI safety/diagnostics checks;
- 360 productivity/config/UI checks;
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

`v1.3.1-beta.1` and `v1.3.1-beta.2` are published **prereleases** and must remain immutable. `v1.4.0` is the previous Stable rollback reference; `v1.4.1` is the current Stable line and is expected to own `releases/latest` after publication.

Beta.2 release gates completed:

1. public Chinese text remains plain-language first, technical detail later;
2. PLAN / ROADMAP / HANDOFF / design notes match flexible Layer + takeover-health implementation;
3. x64/x86 and Source.zip are rebuilt from the final beta.2 tree;
4. full regression, settings smoke, Input Lab pre-XInput checks and neutral four-slot probe pass/retain their documented environment-only blocker;
5. local release verification checks ProductVersion/FileVersion, PE architecture, Beta manifest and SHA-256;
6. after the first publish attempt exposed nondeterministic backup retention, `ConfigStore` was fixed and deterministic same-clock + cold-start clock-rollback tests were added; non-publishing commit `cfe906c` then passed GitHub `windows-2022` verify;
7. final no-source-change publish commit `fbb9cef` passed workflow `34383569935` and created `v1.3.1-beta.2` as `prerelease=true`;
8. all five remote assets have GitHub SHA-256 digests, and `releases/latest` still resolves to Stable `v1.3.0`.

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
