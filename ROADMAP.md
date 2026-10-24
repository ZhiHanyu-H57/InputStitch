# ROADMAP.md

## Native settings and timer editing corrections completed — 2026-10-24

Local beta.6 addresses actual user-reported rendering/selection regressions.
Keep native DPI verification alongside synthetic layouts; inspect content heights
and selection semantics, not only control containment. Current targeted checks,
configuration compatibility and both packaged architectures pass.
See docs/UI_REGRESSIONS_1_6_0_BETA6.md. No additional feature/publication scope.

## Popup performance and settings usability completed — 2026-10-24

Local beta.5 batches layout and caches unchanged label measurements, improving
settings/editor/crosshair and tool window opening. Timer preview and part surfaces
are reused within bounds; grouped moves and preset-to-custom editing are clearer.
Impact-selected automation, bilingual/DPI layouts, existing macro journey,
both packaged architectures, desktop display and actual OBS capture pass.
See docs/USABILITY_AND_PERFORMANCE_1_6_0_BETA5.md. This request is complete.

## Flexible timer and multi-application controls completed — 2026-10-24

Local beta.4 adds custom hotkey actions by timer state alongside presets,
batch formatting, multiline spacing/cadence, imported PCM cues/volume and
atomic multi-application WFP scope. Selected UI/runtime/config checks and actual
OBS/isolated recovery pass. See docs/OVERLAY_TOOLS_1_6_0_BETA4.md.
Stable 1.5.2 remains published. This requested follow-up is complete.

## Network shortcuts and timer customization completed — 2026-10-24

Local beta.3 adds independent block/restore chords and sounds, plus styled
hours/minutes/seconds/milliseconds/custom text parts. Old settings, macro
workflow, bilingual/DPI layouts, both packages, native drawing, OBS capture and
guest-wide crash recovery verified. See docs/OVERLAY_TOOLS_1_6_0_BETA3.md.
Keep Stable 1.5.2 unchanged; no follow-on feature or publication scope implied.

## Overlay acceptance follow-up completed — 2026-10-24

Local 1.6.0-beta.2 adds executed OBS Game Capture and isolated native dual-stack
disconnect/recovery evidence to beta.1. Four actual recordings/48 frames and
299 guest-network assertions pass, including actual parent/helper termination.
Only version/capture-help production edits; executable sizes unchanged.
See docs/OVERLAY_TOOLS_1_6_0_BETA2.md. Keep Stable 1.5.2 and the current feature scope.

## Authorized overlay tools beta — 2026-10-23

Completed as local 1.6.0-beta.1 with selected automated acceptance and both
architectures. Verification: docs/OVERLAY_TOOLS_1_6_0_BETA1.md. Keep Stable 1.5.2
published; no automatic follow-on feature work or formal release.

Corner-cut square crosshair; configurable desktop/RTSS stopwatch; reversible
network hotkey scoped to a program path or the whole computer. Stable 1.5.2
remains published. Full crosshair drawing is deferred pending broader need.

## 1.5.2 Stable released and verified — 2026-10-23

Published v1.5.2 at 2278a55730e05beebd2f6a63f51a9f1552567499 after complete
local and independent GitHub acceptance. All 37 suites pass in both environments;
three original-version upgrades pass 708 checks; real output 81/0; fresh soak
600.1 seconds / 4297 cycles with zero final outputs and three macro workers.
Actual GitHub/R2 files, both published architectures, legacy update parsers and
packaged upgrades are verified. Production remains frozen at beta.1 except
Stable ReleaseInfo metadata. No new feature, dependency or architecture work.
No manual-trial prerequisite. Keep expansion paused until a separate request.
Results: docs/RELEASE_1_5_2_VERIFICATION.md.

## Current status — 1.5.1 Stable released (2026-10-22)

Formal release and published-download verification completed. All local gates,
independent GitHub full regression and the website mirror pass. Retain the release
freeze; further work needs a measured issue or a separate user request.

Freeze the accepted beta.3 scope and complete formal automated release acceptance,
including old settings and in-app updates. Further architecture/performance work
is deferred beyond this release; reopen it only for measured issues and separate
authorization. No new features. See `docs/RELEASE_1_5_1_VERIFICATION.md`.

## Current P1 follow-up — beta.2 (2026-10-22)

Progress publication, single-step commands and control-state capture now join
run lifecycle in the coordinator. Format diagnostic input names only on demand
and outside the runtime lock. The blocked-UI trigger sample confirms UI affinity
still needs a careful condition/commit boundary; do not bypass editing safety.
See `docs/RUNTIME_PROGRESS_BETA_1_5_1_2.md`. Keep feature expansion paused and use
impact-selected development checks, with complete acceptance at formal release.

## Incremental runtime coordination — 2026-10-22

Local 1.5.1-beta.1 follows P1: separate run lifecycle/indexes from MainForm and
eliminate released advanced-trigger idle UI dispatch, retaining workers, timing,
ownership and configuration. See `docs/RUNTIME_COORDINATION_BETA_1_5_1.md`.
Remaining execution/progress/condition coupling is intentional follow-up work,
not authorization for a wholesale runtime rewrite or new product features.
Develop with impact-based verification; complete regression remains a formal
release gate. Stable 1.5.0 stays published unchanged.

## Previous priority — measured performance and maintainability (2026-10-21)

Beta.15 follows the mature-tool and live architecture review in
`docs/MATURITY_AND_PERFORMANCE_REVIEW.md`. Fix measured background waste while
preserving the existing product scope and beta.14 UI. Next: repeatable resource
baselines, incremental separation of runtime coordination from UI, and clearer
configuration/runtime explanations. Evaluate SDL3 allocation and high-concurrency
work only with profiles; retain ownership safety and current compatible backends.
The dated feature-order sections below are historical and do not restart feature
expansion. Broad hardware features, scripting and exclusive fullscreen stay deferred.

## Authorized exception — screen crosshair (2026-10-21)

The user explicitly requested a screen crosshair after the readability pass.
Deliver this bounded feature while preserving the macro baseline and automated
acceptance policy. Other feature expansion remains paused. See `docs/CROSSHAIR.md`.
The beta.8 closure targets windowed and borderless mode only. Exclusive fullscreen
work is deferred after the user observed no reticle; no Game Bar component or
setup path ships in this build.

## Active priority override — 2026-10-20

2026-10-21: beta.14 completes the menu/choice/font audit and exposes both step-input
methods. Use the supplied DCET diamond direction with a continuous vivid gradient
and no lettering; preserve features, lightweight packaging and automated acceptance.

2026-10-21: beta.13 keeps trigger blocking visible, bounds excessive choice widths,
groups advanced controls in a clear disclosure and enlarges the step controller
diagram. Preserve readable density and keep packaging lightweight.

2026-10-21: beta.12 refines the beginner create-to-run flow, visible automatic-save
guidance, consistent step wording, shortcut-chord capture and inline correction
of invalid step settings. No new capabilities or dependencies.

2026-10-21: beta.11 extends the same readability/usability/clean-overview standard
to secondary dialogs: remove excess spacing, give lists readable rows, and
validate bilingual/native-DPI text and clipping. Keep runtime feature expansion
paused and retain the compressed single-file distribution.

2026-10-21: beta.10 follows the beta.9 density/footprint correction with compact
trigger selection and an adjacent repeat/infinite group. Measure bilingual text,
wrap whole groups on resize and verify native DPI without adding functions or
dependencies. Readability, usability and clean overview precede decoration.

Pause feature expansion. Complete product usability and repeatable automated
acceptance first: one trigger editor, ordinary-language labels, consistent
visuals, compatible old configurations, real editing/save/reopen/package flows,
real-output verification and sustained-run reports. Stable 1.4.3 is a
user-confirmed long-used baseline. Specific experimental takeover compatibility
claims are independent; they do not gate the ordinary macro product.

User manual testing is optional feedback, not a routine release prerequisite.
This priority supersedes older feature-order sections retained below.


# InputStitch Roadmap / 开发路线

Updated: 2026-09-21

## Current position / 当前阶段

Current Stable: **v1.4.3**
Current Beta: **v1.5.0-beta.1** — opt-in prerelease, independent from `releases/latest`
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
- any layer, including Base, can switch in **momentary / hold-to-layer** mode: it is active only while its
  own shortcut is physically held, releasing returns to the previous layer, and the persistent
  click-to-switch selection is never rewritten by a temporary hold;
- several momentary layers can be held at once (newest held wins), a missed release edge is recovered from
  the same physical release probe used by held mappings, and deleting or converting a held layer back to
  click-to-switch safely drops the hold.

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

HIDMaestro's author confirmed the adapter contract in
[issue #6](https://github.com/ZhiHanyu-H57/InputStitch/issues/6) on 2026-10-24:
`SubmitState` submits a whole controller frame, so an eventual adapter must retain
held state and submit the complete state on every `Send`. Profiles are
`xbox-360-wired` and `dualshock-4-v2`. Future acceptance must cover overlapping
held inputs, independent macro ownership, and neutral state on stop/disconnect.
This is integration guidance; the current production backend remains ViGEm.

### Completed foundation — Persistent Device Identity / Device Manager stage 1

Completed on post-`v1.4.3` `main`. XInput slot numbers remain transient runtime state and are never serialized as device identity.

The new identity layer provides:

- opaque versioned `DeviceKey` records persisted separately in atomic `devices.xml`;
- non-slot evidence ordered as PnP Container ID → container/base path → VID/PID + serial → XUSB path → PnP/HID path;
- alias retention so later stronger metadata enriches an existing record without changing its DeviceKey;
- native Windows XUSB/PnP discovery that does not require HidHide;
- optional HidHide metadata enrichment including preserved serial number;
- fixed product-owned identities for InputStitch virtual Xbox/DS4 output;
- separate labeling for provider-discovered virtual-bus devices;
- read-only Device Manager + diagnostics showing stable identity beside current XInput slot and explicitly unresolved slot rows when correlation cannot be proven.

The main configuration/profile format is unchanged. This stage deliberately does not guess multi-device PnP ↔ XInput slot ordering and therefore provides the safe base for Router source selection, future per-device conditions/profiles and eventual takeover identity improvements.

### Completed foundation — Router source selection and per-device routing policy stage 1

Completed on post-`v1.4.3` `main` with the historical behavior preserved as the default. Settings now offers an explicit source-selection model:

```text
[x] Controller A
[ ] Controller B
[x] Controller C
```

Stage 1 provides:

- `AllVisible`: backward-compatible routing of every visible non-own XInput source;
- `SelectedDevices`: persistence by stable `DeviceKey`, never by XInput slot;
- a one-to-one runtime correlation only when exactly one external XInput source and exactly one currently observed non-virtual native Windows XUSB identity are present;
- topology generation invalidation so disconnect/reconnect, source-count change or own-slot movement immediately invalidates stale bindings;
- fail-closed behavior for unresolved or multi-controller layouts instead of discovery-order/slot guessing;
- Router diagnostics showing policy, blocked count and unresolved count;
- Output Ownership/source IDs remain unchanged (`router:xinput:N`) so policy filtering does not create a second execution/merge engine;
- Experimental Controller Takeover is temporarily restricted to `AllVisible` until its selected-hidden-device transaction can consume the same identity policy safely.

Later per-source work belongs in the next layers rather than this stage: Analog Transform owns per-source analog shaping, while future multi-target output policy should arrive only when a second output target/backend exists.

Do not make “support more controllers” an independent KPI; make additional sources a consequence of provider/backend architecture.

### Completed foundation — Analog Transform Engine stage 1

Completed on post-`v1.4.3` `main` as a reusable layer between routed-source conversion and Output Ownership rather than as Router-specific math.

Stage 1 provides:

- an identity-by-default serializable `AnalogTransformProfile` with independent left/right stick and trigger settings;
- radial stick inner/outer deadzone remapping that preserves direction;
- response curves expressed as a stable exponent percentage (`100%` linear, `200%` squared, `50%` square-root-like);
- post-curve sensitivity/scaling;
- per-axis stick inversion;
- final maximum-output clamp;
- trigger inner/outer deadzone, curve, scaling and final clamp;
- reusable signed half-axis conversion and magnitude-zone classification primitives for later Activator/Condition work;
- a Settings editor with live 25/50/75/100% preview and an explicit reset-to-identity action;
- diagnostics that show custom transform state without changing the legacy Router summary when the transform is identity.

The operation order is fixed and tested: **deadzone remap → response curve → scale → inversion → max-output clamp**. The default/legacy profile bypasses all transform math exactly so existing Router contributions are unchanged.

Cross-source merge policy deliberately remains in Output Ownership at this stage: sticks still use normalized sum, triggers still use maximum, and digital state remains ownership-based. Alternative merge strategies such as latest-source/priority/average are deferred until there is a real multi-device identity/provider path that can assign those policies without creating a second merge engine.

### P2 — Activator + Condition Engine

Stage 1 is complete on post-`v1.4.3` `main`. Existing Toggle/Hold remains the exact `Legacy` compatibility path, while explicit advanced activation/condition state is now separate from macro execution and feeds the same Concurrent Macro Runtime / Parallel Held Mapping + Output Ownership paths.

Stage-1 activators:

- On Press;
- On Release;
- While Held;
- Long Press;
- Double Press.

Stage-1 deterministic conditions:

- active Layer (the existing macro Layer remains authoritative);
- foreground process and optional title substring;
- stable source `DeviceKey` with fail-closed topology resolution;
- controller analog magnitude or signed half-axis percentage zone.

Wheel directions are explicitly edge-only: Press and Double Press can consume wheel edges, while Release/While Held/Long Press refuse to invent a persistent held state. While Held stops its own source if an active condition becomes false. A condition-invalid press also disarms Double Press history so two valid presses cannot be bridged across an invalid one.

The editor exposes the rule as an opt-in **Activation & conditions / 激活与条件** dialog. When an advanced activator is selected, the old Toggle/Hold selector is visibly disabled rather than left as a second competing semantic owner.

The following targets remain stage-2 candidates, not partially implemented aliases:

- Short Press;
- Triple Press;
- an explicit advanced Toggle only if it adds value beyond `Legacy` Toggle;
- Turbo/repeat.

Remaining condition candidates:

- another input currently held;
- richer profile/manual-override/fallback state;
- richer analog regions/direct analog-as-trigger semantics;
- other deterministic runtime state that can be observed and tested.

The engine must continue to use the common Concurrent Macro Runtime and Output Ownership rather than becoming a second executor. Stage 1 satisfies that boundary.

### P2 — Layer ergonomics, not Layer complexity

Current Base + one active named Layer is intentionally easy to reason about. Extend ergonomics in this order:

1. Momentary / Hold-to-Layer — **implemented 2026-09-21** (local commit; 72 Layer checks);
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

Latest clean 2026-09-21 automated regression:

| Area | Result |
|---|---:|
| Keyboard / trigger | 329 pass |
| Idle Gamepad | 58 pass |
| XInput input | 26 pass |
| Persistent Device Identity | 36 pass; temporary XML/fake metadata only |
| Router Source Policy | 22 pass; fake XInput/device metadata/output only |
| Analog Transform | 49 pass; pure math/fake XInput/output only |
| Activator / Condition | 55 pass; pure state/fake config only |
| Gamepad Router | 28 pass |
| Controlled Replacement transaction/runtime health/recovery | 52 pass |
| Slot acquisition / PnP recovery | 27 pass; fake PnP/XInput, no real device disabled |
| Optional virtual-controller preference | 23 pass; no ViGEm device created |
| Flexible Layer / XML / observation / momentary | 72 pass |
| Macro timing | 7 pass |
| Updater | 43 pass |
| Update network | 29 pass; injected failures only |
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
v1.4.0 Stable
   ↓
v1.4.1 Stable   ← previous Stable / rollback reference; UI lifecycle hotfix
   ↓
v1.4.2 Stable   ← previous Stable; website-first updater + architecture cleanup
   ↓
v1.4.3 Stable   ← current recommended Stable; keyboard hotkey reliability recovery, takeover remains Experimental
   ↓
Device Identity + Router source policy + Analog Transform
   ↓
Activator / Condition stage 1
   ↓
Layer ergonomics + profile/context improvements
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
- Keep historical Stable releases immutable. `v1.4.2` is the previous Stable rollback reference; `v1.4.3` is the current Stable line and owns `releases/latest`.
- Every release build must verify x64/x86 architecture, ProductVersion/FileVersion, release manifest and SHA-256.
- Public Simplified Chinese text should begin with an understandable user-level summary; internal API/type names belong later in technical details.
- Runtime changes require the complete regression suite before commit.
- Routine macro acceptance uses automated workflow/output/soak evidence. A specific game compatibility claim needs evidence from that target environment; it is not a blanket user-testing gate.

## Long-term product goal / 长期目标

> **Common tasks simple, advanced tasks explicit, every task reliable.**
>
> **常用的足够简单，复杂的足够明确，所有功能都足够可靠。**

> **Compete on deterministic orchestration, not on device-feature count.**
>
> **竞争重点是确定、可解释的输入编排，而不是硬件功能数量。**
