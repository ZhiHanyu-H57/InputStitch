# ROADMAP.md

# InputStitch Roadmap / 开发路线

Updated: 2026-09-10

## Current position / 当前阶段

Current Stable: **v1.3.0**
Previous prerelease: **v1.3.1-beta.1** — published and immutable
Current prerelease: **v1.3.1-beta.2** — published and immutable

Stable `v1.3.0` completed the Input Ownership + universal Concurrent Macro Runtime milestone. The current `1.3.1-beta.2` tree keeps the controller-input/aggregation/slot-acquisition foundation from beta.1, expands Layer into arbitrary named layers with switch bindings, and adds continuous takeover health monitoring with automatic fail-safe disengage/recovery.

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

## Highest-priority next work / 下一步最高优先级

### 1. Real-device acceptance for the implemented slot-acquisition transaction

The unsafe “hide slot 0 first and hope” design is no longer used. Current implementation follows:

```text
stop managed output
        ↓
disconnect InputStitch virtual Xbox once for self-identity filtering
        ↓
temporarily re-enumerate all present external XUSB sources if slot 0 must be freed
        ↓
reconnect InputStitch first and hard-verify XInput 0
        ↓
restore every external XUSB source and verify all identities returned
        ↓
re-establish Router/source state and re-check slot 0
        ↓
only now can HidHide Begin run for the explicitly selected external identities
```

The remaining task is physical-controller validation of this already-implemented transaction. A neutral four-device ViGEm probe has verified the ordering mechanism from `external 0/1/2 + InputStitch 3` to `InputStitch 0 + external 1/2/3`.

### 2. Broader stable device identity

XInput slot number is not durable physical identity. Current XInput takeover uses HidHide's XUSB identity and re-enumeration-based self filtering. Broader hardware support will need correlation between:

- HidHide / PnP device instance path;
- physical controller/container identity;
- XUSB/XInput-visible source;
- InputStitch's own ViGEm device.

The UI must never hide a device based only on a guessed slot association.

### 3. Real HidHide acceptance

On a machine with HidHide installed, prove all of the following:

- selected original controller is hidden from an ordinary observer/target;
- InputStitch remains whitelisted and can still read it;
- virtual routed controller remains available in the required target slot;
- stopping takeover restores original visibility;
- crash/restart recovery restores InputStitch-owned changes;
- pre-existing HidHide configuration is preserved.

### 4. Physical fault-injection acceptance for the implemented health monitor

The runtime health watchdog is now implemented and automatically disengages after confirmed consecutive failures. When physical hardware becomes available, validate that real-world failures behave the same way:

- target virtual slot changes unexpectedly;
- Router becomes unavailable;
- a whitelisted original XInput source stops being visible to InputStitch;
- HidHide cloak/hidden-device/application-whitelist state is changed externally.

Each confirmed fault must restore InputStitch-owned hiding changes when possible, stop routed output, and avoid original + routed doubled input.

### 5. Broader controller platform — later / 按真实需求推进

After XInput takeover is mature, consider:

- more than four sources;
- DirectInput / HID / GameInput;
- Windows-connected DualShock / DualSense;
- per-device identity UI;
- optional Layer ordering/presets only if a concrete workflow demonstrates a need;
- conditions / groups / richer routing policies.

Gyro-to-mouse is a separate later project because it requires sensor calibration/filtering and should not be mixed into basic routing.

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
| UI safety / diagnostics | 23 pass |
| Productivity/config/UI | 360 pass |
| Output Ownership | 303,716 pass |
| Settings smoke | zh-CN/en-US, normal+narrow pass |
| Legacy XML / gamepad-vector editor | pass |

Input Lab final acceptance on this laptop:

- 18 pre-XInput real `SendInput` / timed-concurrency / complex-Hold checks: **PASS**;
- product assertion failures: **0**;
- controller lane then returns the known local environment condition: `SUMMARY: BLOCKED | checks=18 | failures=0` because the ViGEm Xbox enumerates but its report is not reflected through local XInput.

Do not convert that environment blocker into a fake product pass, and do not reinstall system drivers solely to force the test green without a separate troubleshooting decision.

## Release path / 版本路径

```text
v1.2.0 Stable
   ↓
v1.3.0-beta.1
   ↓
v1.3.0-beta.2
   ↓
v1.3.0 Stable   ← current recommended Stable
   ↓
v1.3.1-beta.1  ← published prerelease; slot acquisition implemented
   ↓
v1.3.1-beta.2  ← current published prerelease; flexible layers + takeover health monitoring
   ↓
physical PnP + real HidHide + target-game acceptance (blocked until hardware exists)
   ↓
controlled replacement hardware maturity
   ↓
broader controller platform / richer routing as justified
```

## Release discipline / 发布纪律

- Beta uses the isolated Beta manifest/channel.
- `v1.3.1-beta.1` is published as a GitHub **prerelease**; keep it immutable.
- `v1.3.1-beta.2` is published as a GitHub **prerelease**; keep it immutable.
- Do not change `v1.3.0` or `releases/latest`.
- Every release build must verify x64/x86 architecture, ProductVersion/FileVersion, release manifest and SHA-256.
- Public Simplified Chinese text should begin with an understandable user-level summary; internal API/type names belong later in technical details.
- Runtime changes require the complete regression suite before commit.
- API/output submission success is never a substitute for real-game acceptance.

## Long-term product goal / 长期目标

> **Common tasks simple, advanced tasks explicit, every task reliable.**  
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**
