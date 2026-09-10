# ROADMAP.md

# InputStitch Roadmap / 开发路线

Updated: 2026-09-10

## Current position / 当前阶段

Current Stable: **v1.4.1**
Previous prerelease: **v1.3.1-beta.1** — published and immutable
Latest historical prerelease: **v1.3.1-beta.2** — published and immutable

Stable `v1.4.0` promotes the validated controller-input/aggregation and flexible-Layer work from the 1.3.1 Beta line into the recommended Stable channel. Controller takeover remains explicitly Experimental until physical-controller + HidHide + target-game acceptance is available.

一句话：**1.3.0 解决“多个宏同时运行且互不误伤”；beta.1 把手柄触发、汇总和安全取得 0 号接入统一平台；beta.2 继续把映射层做成可自由管理的多层系统，并让实验性接管在运行中持续自检、异常时自动退出恢复。**

## Completed milestones / 已完成里程碑

### Stable 1.2.0 — reliability/productivity baseline

- safer configuration persistence and backups;
- Quick Create templates;
- step Undo/Redo;
- Held Mapping support for normal keys, mouse buttons and standalone modifiers;
- improved foreground/hotkey and Idle Gamepad behavior.

### Stable 1.3.0 — Input Ownership + universal concurrency

- multiple state-only Parallel Held Mappings;
- multiple distinct ordinary timed/Toggle macros;
- multiple Advanced/complex Hold timelines;
- independent RunId / SourceId / stop / timing / Hold-release state;
- source-local cleanup;
- deterministic digital/trigger/stick/D-pad merge;
- selected-macro Stop vs global Emergency Stop;
- Runtime Observation and runtime eligibility UI;
- modifier-chord Hold and Shift-friendly bare-key trigger fallback;
- Single-step diagnostic execution.

Historical `v1.3.0-beta.1` / `v1.3.0-beta.2` tags and releases remain immutable.

## v1.3.1-beta.2 — current prerelease / 当前测试版

### A. Physical XInput controller input — implemented

- visible XInput slots `0..3` become first-class input sources;
- own ViGEm Xbox slot is excluded dynamically;
- buttons, D-pad, LT and RT can trigger macros;
- LT/RT use hysteresis;
- reconnect baseline and disconnect release are fail-safe;
- Hold runs are pinned to the actual controller that started them.

### B. Controller → keyboard/mouse/hybrid macro mapping — implemented

A controller trigger can run the same macro types as keyboard/mouse triggers, including keyboard, mouse, virtual-gamepad and mixed output. No controller-specific second macro engine was added.

This is **augmentation** until device hiding is active: the original controller may still reach the game directly.

### C. Multi-controller aggregation Router — implemented for visible XInput slots

- every external XInput slot is an independent routing source;
- full button/D-pad/stick/trigger state is mirrored;
- whole-source state updates are atomic;
- existing Output Ownership merge rules combine multiple controllers and macros;
- disconnect/recenter clears only that controller's contribution;
- identical frames do not generate redundant output.

This solves the routing half of “several controllers → one InputStitch virtual Xbox controller,” but Router alone does not suppress the original devices.

### D. Optional virtual-controller creation — implemented

Virtual-controller type now supports:

- Xbox 360;
- PS4 / DualShock 4;
- **Do not create a virtual controller / 不创建虚拟手柄**.

The no-output choice is persistent and respected:

- no startup ViGEm creation merely because saved gamepad-output macros exist;
- no ViGEm creation merely from editing a gamepad step;
- keyboard/mouse macros remain available;
- physical/other XInput controller triggers remain available;
- incompatible runtime actions are refused rather than silently overriding the preference.

Fresh-install default remains Xbox360 unless a later product decision changes it.

### E. Layer / mapping layer — arbitrary named layers + switch bindings implemented

Current model:

- Base always active;
- any number of additional named layers can exist;
- one non-Base layer is active at a time;
- active layer is runtime-only;
- ordinary timed, Toggle, Advanced/complex Hold and Parallel Held Mapping can all belong to a layer;
- every layer, including Base, may have a keyboard/mouse/controller switch trigger;
- add / rename / delete are exposed in the main UI;
- deleting a layer moves its macros to Base; deleting the active layer first returns to Base-only;
- list priority remains authoritative inside the eligible set;
- leaving a layer stops only that layer's active sources;
- newly eligible triggers that were already held require release + press;
- modern XML uses an authoritative replacement list so deleting legacy Layer 1 survives restart, while truly old configs without layer data still migrate to Base + Layer 1.

### F. Controlled Replacement transaction + runtime health — experimental, hardware acceptance pending

Implemented:

- HidHide CLI backend abstraction;
- device enumeration/parsing;
- explicit user-selected device hiding;
- InputStitch whitelist management;
- pre-hide Router requirement;
- recoverable slot-0 acquisition before HidHide: all present external XUSB controllers may be temporarily re-enumerated, InputStitch reconnects first and must verify slot 0, then all external controllers are restored and re-verified;
- slot-reordering scope is separate from hiding scope, so only explicitly selected external identities proceed to HidHide;
- non-persistent Windows Configuration Manager disable/enable operations with a dedicated slot-acquisition recovery journal;
- independent hard gates after acquisition: target slot, Router and external source identity/visibility must all still be valid before HidHide Begin is reachable;
- post-hide Router/slot/source-health verification;
- continuous background health monitoring while takeover is active: target slot, Router readiness, expected XInput source visibility, HidHide cloak state, selected hidden-device membership and InputStitch whitelist;
- one transient failure is tolerated; two consecutive failures dispatch a single fail-safe recovery;
- confirmed runtime failure restores InputStitch-owned HidHide changes, stops/disables Router and clears managed routed output so restored original input is not doubled by routed input;
- Runtime Observation/diagnostics expose layer, virtual slot, Router/takeover health, expected sources and recovery warnings without initializing ViGEm merely for observation;
- rollback of InputStitch-owned changes;
- preservation of pre-existing HidHide state;
- crash-recovery journal written before mutation;
- next-launch recovery;
- Emergency Stop / shutdown recovery attempts;
- experimental Tools → Controller takeover UI.

Not completed:

- physical XInput controller PnP disable/enable acceptance on a real controller;
- reliable identity mapping beyond the current XUSB/HidHide `xusbDeviceInstancePath` path for arbitrary hardware;
- real HidHide hardware validation on the current development laptop;
- proof that a specific game sees only the routed controller;
- non-XInput controller backends.

Therefore **do not describe 1.3.1-beta.2 as fully hardware-validated controller takeover**.

## Product positioning / 产品定位

The 2026 competitive review changes the priority order, not the core architecture.

**InputStitch should compete as a deterministic input orchestration and macro platform, not as a hardware-feature-count remapper.** Mature projects such as PadForge, reWASD, Joystick Gremlin, Steam Input and DS4Windows already invest heavily in broad controller coverage, vendor-specific features, curves, gyro, menus and device ecosystems. InputStitch's strongest differentiators are instead:

- explicit Output Ownership and source-local cleanup;
- deterministic concurrent macro/runtime semantics;
- one common keyboard/mouse/controller orchestration pipeline;
- runtime observation and explainable state;
- fail-safe transactions, rollback and recovery;
- a useful keyboard/mouse-only mode that does not require virtual-controller creation.

See [`docs/PRODUCT_STRATEGY.md`](docs/PRODUCT_STRATEGY.md) for the detailed competitive comparison and rationale.

## Highest-priority next work / 下一步最高优先级

### P0 — Physical takeover acceptance — BLOCKED by hardware

The implemented slot-acquisition + HidHide + health-monitor pipeline still requires a real physical XInput controller and a HidHide test environment before full controller takeover can be claimed.

When hardware becomes available, verify:

1. physical PnP disable/enable and slot-0 acquisition from InputStitch starting slots 0/1/2/3;
2. selected originals disappear from an ordinary observer/game while InputStitch remains whitelisted/readable;
3. the target game sees the intended routed slot-0 virtual controller rather than the selected originals;
4. stop/crash recovery restores only InputStitch-owned changes and preserves pre-existing HidHide state;
5. deliberate slot/Router/HidHide faults trigger the runtime watchdog without leaving doubled original+routed input.

Do not substitute additional virtual devices and call this hardware acceptance.

### Completed foundation — Virtual Output Backend abstraction

Completed on `refactor/no-functional-change-1` as a no-feature-change refactor. `GamepadOutput` remains the stable synchronized facade used by macro execution, Output Ownership, Router, Idle and takeover code, while concrete virtual-controller operations now go through `IVirtualGamepadBackend`.

`VigemVirtualGamepadBackend` is the only production implementation and owns all direct `Nefarius.ViGEm.Client` types, Xbox/DS4 report mapping, connection lifecycle and ViGEm-specific failure translation. Outside the embedded dependency loader and user-facing driver guidance, higher-level product code no longer needs concrete ViGEm controller types.

Regression coverage uses an injected fake backend to verify first connection, same-type reuse, Xbox→DS4 switching, unchanged `InputSpec` forwarding, neutralization, send-failure fail-closed disconnect and `VirtualGamepadTypes.None` refusal without creating a real ViGEm device. Full regression and x64/x86 Release verification pass with ViGEm still the sole shipped backend.

Do not add a second backend merely to prove the interface exists. When a concrete need justifies it, evaluate alternatives such as HIDMaestro or VIIPER's standalone server/API with deployment, signing/security, x86/x64 support and licensing considered alongside device features.

### P1 — Persistent Device Identity / Device Manager

XInput slot numbers are runtime state, not durable device identity. Establish a stable `DeviceKey` and a device inventory/diagnostic model that can correlate, where available:

- input provider;
- PnP/container identity;
- device instance path;
- VID/PID;
- serial or stable hardware identifier;
- XUSB identity;
- current XInput slot as a transient attribute;
- InputStitch-owned virtual-device identity.

This becomes the foundation for per-device triggers, Router selection, profiles and safe takeover. The UI must never hide a device based only on a guessed slot association.

### P1 — Router source selection and per-device routing policy

The current Router aggregates every visible non-own XInput source. Evolve it into an explicit source-selection model:

```text
[x] Controller A
[ ] Controller B
[x] Controller C
```

Then support per-source routing policy without breaking Output Ownership:

- enabled/disabled;
- selected output target when multiple backends/targets eventually exist;
- transform chain;
- merge policy;
- diagnostics showing why a source is or is not routed.

Do not make “support more controllers” an independent KPI; make additional sources a consequence of provider/backend architecture.

### P1 — Analog Transform Engine

This is now a higher-priority platform gap than another controller model. Build reusable transforms for analog input/output:

- inner/outer deadzone;
- response curves;
- sensitivity/scaling;
- inversion;
- half-axis conversion;
- threshold/zone bands;
- clamping;
- explicit merge policies such as maximum magnitude, priority/latest source, average or normalized sum where appropriate.

Transforms should be observable and composable, not buried inside one Router special case.

### P2 — Activator + Condition Engine

Unify today's Press/Toggle/Hold/modifier/Layer/application-context behavior into reusable concepts instead of accumulating special-case trigger modes.

Target activators:

- On Press;
- On Release;
- While Held;
- Short Press;
- Long Press;
- Double/Triple Press;
- Toggle;
- Turbo/repeat.

Target conditions:

- active Layer;
- source `DeviceKey`;
- analog threshold/zone;
- another input currently held;
- foreground application/profile;
- other deterministic runtime state that can be observed and tested.

The engine must continue to use the common Concurrent Macro Runtime and Output Ownership rather than becoming a second executor.

### P2 — Layer ergonomics, not Layer complexity

Current Base + one active named Layer is intentionally easy to reason about. Extend ergonomics in this order:

1. Momentary / Hold-to-Layer;
2. Toggle/Latch/Cycle if real workflows justify them;
3. profile/context integration.

Arbitrary simultaneous Layer stacking, inheritance trees and precedence graphs remain low priority because they increase ambiguity and reduce observability.

### P2 — Profile/context improvements

Build profile auto-switch/manual override/fallback behavior on top of `DeviceKey` + Condition rather than adding more ad-hoc application switches. Profiles should remain deterministic and explainable in diagnostics.

### P3 — Input Provider abstraction and broader controller input

Move from “add one API/controller family at a time” to a provider architecture:

```text
IInputProvider
  ├─ Keyboard / Mouse Raw Input
  ├─ XInput
  ├─ future SDL3 provider
  └─ specialized Raw HID/provider only when needed
```

SDL3 is the preferred first candidate for broad controller input evaluation because current general-purpose remappers successfully use it as a cross-device input layer. Keep specialized Raw HID/device-specific providers for capabilities the general provider cannot expose cleanly.

### P3 — Second virtual-output backend evaluation

Only after `IGamepadOutputBackend` is stable, prototype one alternative backend and compare:

- output fidelity/API visibility;
- deployment friction;
- driver/security model;
- hot-plug behavior;
- supported virtual identities;
- latency/reliability;
- license compatibility with InputStitch's project-governance decision.

Do not replace ViGEm merely because it is retired; replace or supplement it only when the new backend is measurably safer or more sustainable.

### P4 — Evidence-driven advanced features

Keep these later unless a concrete user workflow makes them important:

- gyro/touchpad/vendor-specific haptics;
- deeper DualSense-specific behavior;
- richer Raw HID support;
- plugin/script API after internal action/condition/transform interfaces are stable;
- Layer stacking/inheritance;
- radial menus/overlays;
- more than four sources as a natural consequence of broader providers/backends.

### Governance — License decision

InputStitch is source-visible but currently declares no open-source license. That is acceptable for the current project, but it becomes a strategic decision before a third-party backend/plugin ecosystem, broad redistribution or license-constrained dependency integration. Treat license selection as a separate governance task; do not silently change it as part of implementation work.

## Validation status / 验证状态

Latest clean 2026-09-10 automated regression:

| Area | Result |
|---|---:|
| Keyboard / trigger | 323 pass |
| Idle Gamepad | 58 pass |
| XInput input | 26 pass |
| Gamepad Router | 28 pass |
| Controlled Replacement transaction/runtime health/recovery | 52 pass |
| Slot acquisition / PnP recovery | 27 pass; fake PnP/XInput, no real device disabled |
| Optional virtual-controller preference | 15 pass; no ViGEm device created |
| Flexible Layer / XML / observation | 39 pass |
| Macro timing | 7 pass |
| Updater | 43 pass |
| UI safety / diagnostics | 26 pass |
| Productivity/config/UI | 360 pass |
| Output Ownership | 303,716 pass |
| Settings smoke | zh-CN/en-US, normal+narrow pass |
| Legacy XML / gamepad-vector editor | pass |

Input Lab final acceptance on this laptop:

- manual viewer: **v0.3.0**, independently versioned from InputStitch;
- larger six-row keyboard, stronger held-key indication and 180 ms release afterglow;
- target-window `WM_KEY*` / `WM_MOUSE*` observation added beside low-level hook, Raw Input and XInput lanes;
- built-in window-message and keyboard-visual self-tests: **PASS**;
- three consecutive full black-box runs on 2026-09-10: **77/77 PASS, failures=0** each.

The older intermittent ViGEm→XInput environment preflight blocker did not reproduce in those three runs. If it returns, keep treating `SUMMARY: BLOCKED` as environment-only rather than converting it into either a fake pass or a product failure.

## Release path / 版本路径

```text
v1.2.0 Stable
   ↓
v1.3.0-beta.1
   ↓
v1.3.0-beta.2
   ↓
v1.3.0 Stable   ← previous Stable / rollback reference
   ↓
v1.3.1-beta.1  ← published prerelease; slot acquisition implemented
   ↓
v1.3.1-beta.2  ← current published prerelease; flexible layers + takeover health monitoring
   ↓
v1.4.0 Stable   ← previous Stable / rollback reference
   ↓
v1.4.1 Stable   ← current recommended Stable; UI lifecycle hotfix, takeover remains Experimental
   ↓
output backend abstraction + Device Identity + Router source policy + Analog Transform
   ↓
Activator / Condition + profile/context improvements
   ↓
IInputProvider + broader controller input / second output backend as justified

parallel hardware lane:
physical PnP + real HidHide + target-game acceptance (BLOCKED until hardware exists)
    ↓
controlled replacement hardware maturity
```

## Release discipline / 发布纪律

- Beta uses the isolated Beta manifest/channel.
- `v1.3.1-beta.1` is published as a GitHub **prerelease**; keep it immutable.
- `v1.3.1-beta.2` is published as a GitHub **prerelease**; keep it immutable.
- Keep historical Stable releases immutable. `v1.4.0` is the previous Stable rollback reference; `v1.4.1` is the current Stable line and now owns `releases/latest`.
- Every release build must verify x64/x86 architecture, ProductVersion/FileVersion, release manifest and SHA-256.
- Public Simplified Chinese text should begin with an understandable user-level summary; internal API/type names belong later in technical details.
- Runtime changes require the complete regression suite before commit.
- API/output submission success is never a substitute for real-game acceptance.

## Long-term product goal / 长期目标

> **Common tasks simple, advanced tasks explicit, every task reliable.**
>
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**

> **Compete on deterministic orchestration, not on device-feature count.**
>
> **竞争重点是确定、可解释的输入编排，而不是硬件功能数量。**
