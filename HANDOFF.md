# HANDOFF.md

# Current Development State

Updated: 2026-09-09

## Current versions

- Current Stable: `v1.3.0`
- Current prerelease target: `v1.3.1-beta.1`
- Branch: `main`
- Stable rollback baseline: `v1.3.0`
- Historical `v1.3.0-beta.1` / `v1.3.0-beta.2` releases are immutable.

For the commit containing this handoff itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

Do not put a self-referential commit hash in this file.

## Working on

**Close out and publish `v1.3.1-beta.1`, then validate the implemented controller takeover path on physical XInput hardware + HidHide + a real target game.**

Stable `v1.3.0` completed Input Ownership + universal Concurrent Macro Runtime. The current Beta extends that common architecture with physical XInput input, controller triggers, keyboard/mouse hybrid mapping, multi-controller aggregation, all-macro Layer eligibility, optional virtual-controller creation, and an experimental fail-safe Controlled Replacement foundation.

Safe target-slot acquisition is now implemented as a recoverable transaction. InputStitch never hides a physical slot-0 controller and merely hopes the virtual pad will move into slot 0. The next major engineering problem is **real-device acceptance**: prove the implemented PnP re-enumeration + HidHide + Router pipeline with physical controllers and a target game.

## Completed in the current 1.3.1-beta.1 tree

### Physical / virtual XInput input

- `XInputInputService` polls XInput user slots `0..3`.
- InputStitch's own ViGEm Xbox user slot is dynamically excluded to prevent feedback.
- Startup/reconnect uses a baseline-first rule so controls already held do not create phantom Down edges.
- Disconnect generates release edges for held buttons/triggers.
- Digital buttons/D-pad and LT/RT are first-class trigger inputs.
- LT/RT use 80% activation / 70% release hysteresis.
- Configured controller triggers default to “any visible XInput controller.”
- Hold dispatch pins the runtime trigger to the actual controller slot that started it so a different controller cannot stop it.

### Controller → keyboard/mouse/hybrid macro mapping

Controller triggers enter the same existing pipeline as keyboard/mouse triggers:

```text
XInput physical edge
    ↓
Trigger selection
    ↓
Concurrent Macro Runtime / Parallel Held
    ↓
Output Ownership
    ↓
keyboard / mouse / virtual controller
```

No controller-specific second macro engine exists. Controller triggers can run keyboard, mouse, virtual-gamepad or mixed macros in timed/Toggle/Hold forms.

Without hiding, this is augmentation: the original controller may still be visible to the game.

### Gamepad Router / aggregation

- `GamepadRouterService` mirrors every non-own visible XInput source.
- Each source uses its own `router:xinput:N` ownership ID.
- Whole-state routing covers buttons, D-pad, both sticks and both triggers.
- Source replacement is atomic through `OutputOwnershipManager.ReplacePersistentSource`.
- Identical frames are no-ops.
- Multiple controllers and macros merge through the existing Output Ownership rules.
- Disconnect/recenter only clears that controller's contribution.
- Emergency Stop disables Router so the next polling tick cannot immediately reassert routed state.

Router by itself does **not** hide original controller input. Until Controlled Replacement is active and verified, games can see original + routed input simultaneously.

### Optional virtual-controller creation

The virtual-controller type dropdown now has three choices:

1. Xbox 360
2. PS4 / DualShock 4
3. **Do not create a virtual controller / 不创建虚拟手柄**

`VirtualGamepadTypes.None` is a real persisted state, not a UI-only toggle.

When None is selected:

- saved gamepad-output macros do not cause ViGEm creation at startup;
- editing/saving a virtual-gamepad step does not connect ViGEm;
- keyboard/mouse macros remain available;
- external XInput controllers can still trigger keyboard/mouse macros;
- running a macro that actually requires virtual-controller output is refused with a user-facing explanation rather than silently creating ViGEm;
- Idle Gamepad is disabled because it requires virtual output;
- Router/Controlled Replacement requires Xbox output and therefore cannot stay in None.

Router temporarily forces Xbox in Settings. If the user had selected None or PS4 before checking Router, unchecking Router restores the previous selection.

Fresh-install default still remains Xbox 360 for backward compatibility; changing the default is a separate product decision.

### Layer v1 — all macro types

Implemented model:

- mandatory Base layer, always eligible;
- one additional active Layer 1;
- active layer is runtime-only and resets to Base-only after restart;
- `MacroDefinition.MappingLayerId` applies to ordinary timed/Toggle, Advanced/complex Hold and Parallel Held Mapping;
- duplicate triggers keep normal list priority inside the eligible set;
- switch removes/stops sources belonging only to the old layer before publishing new eligibility;
- worker-backed sources are suspended immediately during a layer stop transition so they cannot briefly reassert old-layer output;
- a trigger already held when its layer becomes newly eligible must release + press before activation.

### Macro timing drift bug — fixed

Input Lab exposed an existing timer bug: `WaitOrStopWithUiSafety` subtracted the requested 10 ms wait slice instead of actual elapsed time. On this Windows machine, scheduler overshoot accumulated and a requested 900 ms Press could last around 1.7 s.

It now uses `Stopwatch` monotonic elapsed time while preserving the rule that UI-safety pause time does not count toward macro timing.

A dedicated regression suite (`MacroTimingTests`) locks this behavior.

### Update network interruption handling — hardened

Stable-channel built-in update networking now has bounded retry/timeout behavior:

- connection/DNS/receive/send/timeout failures use at most 3 attempts total (initial + 2 retries);
- each synchronous WebClient request runs on a background task and uses an 8-second connect/read-write inactivity timeout, so the UI stays responsive and a dead connection cannot wait indefinitely;
- every download retry deletes the previous partial executable before starting again;
- final download failure also removes the partial executable and never enters the install transaction;
- non-transient validation/TLS trust failures are not blindly retried;
- automatic background **check-stage** failures stay silent, but once the user has accepted an available update, download/install-preparation failures are explicitly shown;
- after the new executable is fully downloaded and SHA-256/version-verified, installation is local and no longer depends on GitHub/network availability.

`UpdateNetworkTests` locks these cases with injected network failures only; no real network request is made.

### Controlled Replacement safety foundation — experimental

Implemented in `ControlledReplacement.cs`:

- `IDeviceHidingBackend` abstraction;
- HidHide CLI implementation;
- game-controller device enumeration/parser;
- existing hidden-device/app-whitelist/cloak-state inspection;
- explicit app registration/unregistration;
- explicit selected-device hide/unhide;
- hard precondition: hiding backend available;
- hard precondition: Router ready;
- `SlotAcquisition.cs` owns a separate recoverable slot-0 transaction before HidHide;
- if InputStitch is not already slot 0, every present external XUSB controller may be temporarily re-enumerated, InputStitch reconnects first and must hard-verify slot 0, then every external XUSB identity must return before hiding is reachable;
- slot-reordering scope is deliberately broader than hide scope: unselected external XUSB devices can be temporarily cycled to free slot 0, while only explicitly selected external identities are forwarded to HidHide;
- PnP disable is non-persistent and protected by `slot-acquisition-recovery.xml`, independent from `controlled-replacement-recovery.xml`;
- more than three external XUSB controllers is refused before PnP mutation because current XInput routing must fit InputStitch + externals into four slots;
- `ControlledTakeoverPipeline` makes HidHide Begin unreachable unless acquisition succeeds, InputStitch is really still slot 0, Router/source preparation succeeds and selected external identities remain valid;
- hard precondition: one or more user-selected device instance paths;
- post-hide verification that Router still works, target slot is unchanged and expected XInput sources remain visible to InputStitch;
- rollback of only InputStitch-added hide/app/cloak changes;
- preservation of pre-existing user HidHide configuration;
- recovery journal persisted **before** the first hiding mutation;
- next-launch recovery of an abnormal-exit journal;
- Emergency Stop and normal shutdown attempt to restore controller visibility;
- Tools → **Controller takeover (Experimental) / 手柄接管（实验）** UI;
- nothing is hidden automatically and HidHide is never installed automatically.

This is not full takeover yet. Current developer machine has ViGEmBus but no HidHide, so real device hiding has not been accepted on this laptop.

## Not completed yet

### Controlled Replacement / physical acceptance

- physical XInput controller PnP disable/enable acceptance on real hardware;
- broader device identity correlation beyond the current XUSB/HidHide `xusbDeviceInstancePath` route;
- real HidHide hardware validation;
- continuous takeover health watchdog;
- proof against actual target games that original devices are hidden and only the routed virtual device remains game-visible;
- safe post-launch rebinding for games that cache controller indices.

### Broader controller platform

- DirectInput/HID/GameInput sources;
- >4 XInput-equivalent controller sources;
- Windows-connected DualShock/DualSense input backend beyond current XInput-class scope;
- gyro-to-mouse;
- arbitrary named Layer management / Layer-switch hotkeys;
- richer routing conditions/groups.

## Known limitations that are intentional

- XInput exposes at most four user slots.
- InputStitch currently uses XInput user slots as the first controller input backend; this is not a universal HID enumerator.
- Guide/Home behavior through ordinary XInput APIs is not promised across every controller/driver.
- Router aggregation does not equal interception; without HidHide the original source remains visible to games.
- Controlled Replacement may temporarily re-enumerate all present external XUSB controllers to acquire slot 0, but it hides only explicitly selected external identities; any failure before the final HidHide gate restores/stops instead of hiding first.
- “Do not create virtual controller” disables virtual output only; it intentionally does **not** disable external XInput controller triggers.
- One `MacroDefinition` still has at most one active worker-backed run.
- Single-step remains exclusive because it is a diagnostic mode.
- Duplicate normal triggers use macro-list priority.
- Beta and Stable share `%APPDATA%\InputStitch`; do not run them simultaneously.

## Current verification evidence

Clean regression after all current changes:

```text
Keyboard                 323 PASS
Idle Gamepad              58 PASS
XInput Input              26 PASS
Gamepad Router            28 PASS
Controlled Replacement    39 PASS (cross-stage fake backend; HidHide not invoked)
Slot Acquisition          27 PASS (fake PnP/XInput; no real device disabled)
No-Virtual preference     15 PASS (no ViGEm device created)
Layer                     24 PASS
Macro timing               7 PASS
Modifier Safety              PASS
Release policy               PASS
Updater                    43 PASS
Update network             18 PASS (injected failures; no network request)
UI Safety / diagnostics    23 PASS
Productivity              358 PASS
Output Ownership      303,716 PASS
Settings smoke zh/en normal+narrow PASS
Legacy XML / gamepad-vector smoke PASS
```

`v1.3.1-beta.1` x64/x86 release build verification also passes ProductVersion/FileVersion, PE architecture, Beta manifest and SHA-256 checks.

### Input Lab final result

Latest run on 2026-09-09:

- keyboard SendInput lane PASS;
- mouse X2 Press/Up timing PASS;
- ordinary F7→K + F8→X2 concurrency PASS;
- stopping keyboard run leaves mouse run active PASS;
- Advanced Hold F9→K + F10→X2 concurrency PASS;
- releasing F9 leaves F10/X2 active PASS;
- releasing F10 clears X2 PASS;
- all **18 pre-XInput checks PASS**;
- `failures=0`;
- controller preflight then returns the known laptop environment blocker because the ViGEm Xbox enumerates but local XInput does not reflect the submitted report:

```text
SUMMARY: BLOCKED | checks=18 | failures=0
```

This must remain an environment `BLOCKED`, not be converted to a fake product pass or failure.

### Real neutral slot-order probe

`tools/InputLab/run-slot-order-probe.ps1` creates four temporary neutral ViGEm Xbox devices and submits no button/stick/trigger report. Latest result on this laptop:

```text
initial: InputStitch test device = 3; external test devices = 0/1/2
re-enumerated with InputStitch first: InputStitch = 0; external = 1/2/3
EXPECTED_ORDER=True
```

This validates the real Windows/ViGEm connection-order mechanism used by slot acquisition at the current four-slot maximum. It does **not** replace physical PnP + HidHide + game acceptance.

## Public documentation rule

The user explicitly requested that public Simplified Chinese documentation and update notices avoid jargon-first writing.

Use this order:

1. first sentence: what ordinary users can now do;
2. practical behavior / limitations in plain Chinese;
3. only then technical names such as XInput, ViGEm, Output Ownership, SourceId, etc.

Technical design notes may use internal English API/type names freely.

## Next step

After `v1.3.1-beta.1` is committed/pushed/published as a prerelease:

1. validate the implemented slot-acquisition transaction with physical XInput controllers starting from InputStitch slots 0/1/2/3;
2. validate HidHide on a machine where it is installed and prove InputStitch remains whitelisted/readable while ordinary target applications lose the selected original controller;
3. verify a real target game sees the routed slot-0 virtual controller and no selected original device;
4. add continuous takeover health monitoring and fail-safe disengage;
5. only then consider full controller takeover mature;
6. later expand controller backends/Layers only from evidence-based need.

## Important design constraints

- Do not regress Stable 1.3.0 source-local ownership semantics.
- Emergency Stop remains absolute priority.
- Output backend failure remains fail-closed.
- InputStitch's own ViGEm output must never become an external trigger/router source.
- Respect `VirtualGamepadTypes.None`; no incompatible feature may silently recreate ViGEm.
- Controlled Replacement: **target slot not verified → no physical device hide**.
- Hiding must be explicit and recoverable; preserve user pre-existing HidHide state.
- Do not write a new kernel filter driver while a mature external solution is sufficient.
- Layer remains an eligibility/grouping layer on top of the common runtime, never a separate execution engine.
- Stable and Beta release/update channels remain separate.
- Run complete regression before every runtime-changing commit.

## Resume checklist on another computer

```text
1. Read AGENTS.md
2. Read PLAN.md
3. Read ROADMAP.md
4. Read HANDOFF.md
5. git log -8 --oneline --decorate
6. git status --short --branch
7. git fetch / git pull as appropriate
8. Confirm expected version and working-tree baseline before editing
```

If the machine is new to InputStitch development, verify Git, .NET Framework 4.7.2 reference assemblies/build path, embedded ViGEm client dependency and GitHub authentication before release work.
