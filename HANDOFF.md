# HANDOFF.md

# Current Development State

Updated: 2026-09-10

## Current versions

- Current Stable: `v1.4.1`
- Previous prerelease: `v1.3.1-beta.1` — public GitHub prerelease, immutable
- Latest historical prerelease: `v1.3.1-beta.2` — published successfully and immutable
- Branch: `main`
- Previous Stable rollback reference: `v1.4.0`
- Historical `v1.3.0-beta.1` / `v1.3.0-beta.2` / `v1.3.1-beta.1` / `v1.3.1-beta.2` releases are immutable.

For the commit containing this handoff itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

Do not put a self-referential commit hash in this file.

## Working on

**`v1.4.1` is the current Stable line. It is a UI-lifecycle hotfix over 1.4.0: transient ContextMenuStrip instances are disposed only after the current ToolStrip message turn, and shutdown no longer synchronously disposes menus that WinForms may still reference. The post-1.4.x plan remains Virtual Output Backend abstraction → persistent Device Identity → Router source policy → Analog Transform → Activator/Condition. Physical controller + HidHide + real-game acceptance remains a parallel hardware-blocked lane.**

Beta.2 adds two major non-hardware improvements on top of beta.1:

1. flexible arbitrary named Layer management + keyboard/mouse/controller layer-switch bindings;
2. continuous Controlled Replacement health monitoring + automatic fail-safe disengage/recovery.

Physical-controller + HidHide + real-game acceptance is explicitly blocked because the development machine has no physical XInput controller and no HidHide environment. Do **not** substitute more virtual devices and claim that as physical acceptance.

Product strategy reference: `docs/PRODUCT_STRATEGY.md`. The project should compete on deterministic orchestration, observability and fail-safe behavior rather than broad controller-model/vendor-feature count.

## Architecture baseline inherited from v1.3.0 / beta.1

Stable `v1.3.0` completed Input Ownership + universal Concurrent Macro Runtime. The 1.3.1 Beta line extends the same common runtime rather than creating controller-specific or Layer-specific execution engines.

Core flow:

```text
physical keyboard/mouse/XInput edge
        ↓
trigger selection + Layer eligibility
        ↓
Concurrent Macro Runtime / Parallel Held Mapping
        ↓
Output Ownership
        ↓
keyboard / mouse / virtual controller
```

Router sources also enter Output Ownership independently:

```text
external XInput slot N
        ↓
router:xinput:N
        ↓
Output Ownership merge
        ↓
InputStitch virtual Xbox
```

## Completed in current beta.2 tree

### Physical / virtual XInput input

- `XInputInputService` polls XInput slots `0..3`.
- InputStitch's own ViGEm Xbox slot is dynamically excluded to prevent feedback.
- Startup/reconnect establishes a baseline before emitting edges.
- Disconnect generates release edges for held buttons/triggers.
- Digital buttons/D-pad and LT/RT are first-class macro trigger inputs.
- LT/RT use 80% activation / 70% release hysteresis.
- Configured controller trigger means “this control on any visible XInput controller.”
- Hold dispatch pins the run to the actual controller slot that started it.

### Controller → keyboard/mouse/hybrid mapping

Controller triggers reuse the ordinary Trigger → Runtime → Ownership path and can execute:

- keyboard output;
- mouse output;
- virtual-controller output;
- mixed keyboard + mouse + virtual-controller macros;
- timed / Toggle / finite or infinite Hold / lightweight Held Mapping behavior.

Without HidHide takeover this remains augmentation: the original controller can still be visible to the game.

### Gamepad Router / aggregation

- `GamepadRouterService` mirrors every visible non-own XInput source.
- Each source has an independent `router:xinput:N` ownership ID.
- Whole state includes buttons, D-pad, both sticks and both triggers.
- State replacement is atomic through Output Ownership.
- Identical frames are no-ops.
- Disconnect/recenter clears only that source's contribution.
- Emergency Stop disables Router so a later polling tick cannot immediately reassert state.

Router alone is **not interception**. Original + routed input may both reach a game until Controlled Replacement is active.

### Optional virtual-controller creation

Virtual-controller preference has three values:

1. Xbox 360
2. PS4 / DualShock 4
3. `None` / **Do not create a virtual controller / 不创建虚拟手柄**

When None is selected:

- startup does not create ViGEm just because gamepad-output macros are saved;
- editing/saving a gamepad step does not connect ViGEm;
- keyboard/mouse macros still work;
- external XInput controllers can still trigger keyboard/mouse macros;
- incompatible runtime actions are refused rather than silently overriding the preference;
- Idle Gamepad is disabled;
- Router/Controlled Replacement cannot stay in None because they require Xbox output.

Fresh-install default remains Xbox 360 for compatibility.

### Flexible Layer system — beta.2

Layer remains an eligibility/grouping feature on top of the common trigger/runtime/output architecture.

Current model:

- Base layer is mandatory and always eligible;
- any number of additional layers can exist;
- non-Base layers have stable IDs and editable user names;
- one non-Base layer is active at a time, so eligibility is Base + active layer;
- active layer remains runtime-only and resets to Base-only after restart;
- every macro type participates: ordinary timed, Toggle, Advanced/complex Hold, Parallel Held Mapping;
- main UI exposes **Manage layers... / 管理映射层…**;
- user can add, rename and delete layers;
- deleting the active layer first returns runtime state to Base-only;
- macros assigned to a deleted layer move safely back to Base;
- each layer, including Base, may have a keyboard/mouse/controller switch trigger;
- Base switch trigger means “return to Base-only”;
- layer-switch trigger has priority over ordinary macro matching but does not suppress the physical foreground-app input;
- leaving a layer stops/removes only old-layer sources;
- newly eligible triggers already physically held are blocked until release + fresh press;
- duplicate normal triggers retain normal macro-list priority inside the eligible set.

#### Layer XML compatibility fix

Old public collection-field XML behavior appended deserialized Layer data into constructor defaults. That meant deleting legacy Layer 1 could make it reappear after restart.

Current solution:

- runtime still uses `List<MappingLayerDefinition>`;
- XML uses a replacing array proxy;
- truly old XML with no Layer element still gets constructor default Base + Layer 1;
- once modern XML contains an explicit Layer list, that saved list replaces defaults and is authoritative;
- deleting Layer 1 survives a real serialize → deserialize → normalize round trip.

### Macro timing drift bug — fixed earlier in beta.1 line

`WaitOrStopWithUiSafety` now uses monotonic `Stopwatch` elapsed time rather than subtracting requested sleep slices. This fixed the case where nominal 900 ms output could become roughly 1.7 s on this Windows scheduler.

### Update network interruption handling — hardened earlier in beta.1 line

Stable-channel updater now uses bounded timeout/retry behavior:

- transient connect/DNS/send/receive/timeout failures: maximum 3 attempts total;
- 8 s connect/read-write inactivity timeout per attempt;
- synchronous WebClient request runs in background task so UI stays responsive;
- every retry deletes the previous partial executable;
- final failure leaves no partial executable and never enters install transaction;
- background check-stage failures remain silent;
- after user accepts an update, download failure is explicitly shown;
- once EXE is fully downloaded and SHA-256/version verified, installation is fully local and no longer needs GitHub/network access.

### Controlled Replacement transaction — beta.1 foundation

Implemented:

- `IDeviceHidingBackend` + HidHide CLI backend;
- device enumeration/parser;
- explicit user-selected devices; nothing hidden automatically;
- pre-hide Router requirement;
- separate `SlotAcquisition.cs` recoverable slot-0 transaction;
- InputStitch disconnects its own virtual Xbox once for self-identity filtering;
- if needed, every present external XUSB source is temporarily re-enumerated;
- InputStitch reconnects first and must hard-verify target slot 0;
- every external XUSB identity must return before hiding is reachable;
- slot-reordering scope can be broader than hiding scope, but only selected external identities reach HidHide;
- PnP disable is non-persistent;
- `slot-acquisition-recovery.xml` is separate from `controlled-replacement-recovery.xml`;
- >3 external XUSB sources is refused before PnP mutation because current XInput backend has four total slots;
- `ControlledTakeoverPipeline` makes HidHide Begin unreachable until acquisition, target-slot, Router/source preparation and external identity checks pass;
- post-hide Router/slot/source visibility validation;
- preserve pre-existing hidden-device/app-whitelist/cloak state;
- recovery journal is written before first HidHide mutation;
- rollback only InputStitch-owned changes;
- next-launch abnormal-exit recovery;
- Emergency Stop and normal shutdown attempt visibility restoration first.

### Continuous takeover health monitoring — beta.2

New `ControlledReplacementHealthMonitor` runs potentially slow HidHide health checks away from the WinForms/XInput polling thread.

While takeover is Active, it verifies:

- InputStitch virtual Xbox remains target slot 0;
- Router is still ready;
- all expected routed XInput source slots remain visible to InputStitch;
- HidHide backend remains available;
- HidHide cloak remains active;
- every selected original controller remains in hidden-device state;
- InputStitch remains in HidHide application whitelist.

Failure policy:

- one transient unhealthy sample is tolerated;
- a healthy sample resets failure streak;
- two consecutive failures signal exactly one fail-safe disengage;
- monitor stops before recovery to prevent duplicate rollback;
- `StopControlledReplacement()` restores InputStitch-owned HidHide state;
- Router is stopped and disabled after fail-safe restore;
- Output Ownership is cleared and virtual output is neutralized;
- disabling Router prevents restored original input + routed duplicate input;
- if restore fails, recovery journal remains and user receives an explicit warning.

Activation source-health still uses a short settle + active poll. Ongoing background health reads the XInput snapshot last updated by the normal 10 ms UI polling timer; it does not generate controller edges from the background thread.

### Runtime observation / diagnostics — beta.2

Runtime Observation now displays:

- user-facing active Layer name;
- virtual Xbox slot;
- Router status + routed source count;
- Controlled Replacement state;
- health-monitor active/inactive state;
- last health result;
- consecutive failure count;
- expected routed XInput source slots;
- normal macro runs / held mappings / ownership sources / merged output.

Diagnostics additionally list:

- every Layer ID, name and switch trigger;
- Layer count;
- Controlled Replacement state/message;
- health-monitor state/result;
- expected routed sources;
- replacement-recovery warning;
- slot-acquisition-recovery warning.

Observation is side-effect free. `ObservedVirtualGamepadConnected/Type/Slot` return neutral values in the isolated UI host instead of touching `GamepadOutput`; opening diagnostics in tests therefore does not load/initialize ViGEm.

## Hardware acceptance still blocked

The development machine has no physical XInput controller and no HidHide environment. Therefore the following remain unverified on real hardware:

- physical XInput PnP disable/enable;
- actual HidHide hiding of an original physical controller;
- InputStitch remaining whitelisted/readable while an ordinary target loses the original device;
- a real game seeing only the routed InputStitch slot-0 virtual controller;
- runtime health fail-safe under real physical slot/HidHide faults;
- safe behavior for games that cache controller index/device state.

Do not use more ViGEm virtual devices to claim these are complete.

## Broader platform work not yet implemented

- DirectInput / HID / GameInput input backends;
- >4 XInput-equivalent sources;
- Windows-connected DualShock/DualSense input backend outside current XInput-class scope;
- gyro-to-mouse;
- broader per-device identity UI;
- richer routing conditions/groups;
- optional Layer ordering/presets only if a concrete workflow justifies them.

## Known intentional limitations

- XInput exposes at most four user slots.
- Current controller input backend is XInput, not universal HID enumeration.
- Guide/Home behavior through ordinary XInput is not guaranteed across every controller/driver.
- Router aggregation is not interception without HidHide.
- Controlled Replacement may temporarily re-enumerate all external XUSB controllers to acquire slot 0 but only hides selected external identities.
- `VirtualGamepadTypes.None` disables virtual output only; external XInput triggers remain available.
- One `MacroDefinition` still has at most one active worker-backed run.
- Single-step remains exclusive diagnostic mode.
- Duplicate normal triggers use macro-list priority.
- Beta and Stable share `%APPDATA%\InputStitch`; do not run them simultaneously.

## Current verification evidence

Latest clean source regression after beta.2 runtime changes:

```text
Keyboard                 323 PASS
Idle Gamepad              58 PASS
XInput Input              26 PASS
Gamepad Router            28 PASS
Controlled Replacement    52 PASS (fake backend/parser; HidHide not invoked)
Slot Acquisition          27 PASS (fake PnP/XInput; no real device disabled)
No-Virtual preference     15 PASS (no ViGEm device created)
Flexible Layer            39 PASS (isolated UI/fake backend/XML round trip)
Macro timing               7 PASS
Modifier Safety              PASS
Release policy               PASS
Updater                    43 PASS
Update network             18 PASS (injected failures; no network request)
UI Safety / diagnostics    26 PASS
Productivity              360 PASS
Output Ownership      303,716 PASS
Settings smoke zh/en normal+narrow PASS
Legacy XML / gamepad-vector smoke PASS
```

Important new coverage:

- takeover runtime health: slot loss, Router loss, expected source loss, cloak off, hidden-device removal, whitelist loss;
- one transient failure tolerated; healthy sample resets streak; two consecutive failures signal one recovery;
- arbitrary named layers can coexist;
- keyboard layer-switch target selection;
- Base may have its own return shortcut;
- deleting active arbitrary layer returns to Base;
- deleting layer moves macros to Base;
- deleted legacy Layer 1 survives real XML save/reload;
- Runtime Observation/Diagnostics expose Layer + takeover platform state without initializing ViGEm in isolated host.

### Input Lab beta.2 release result

Latest run against the final `1.3.1-beta.2` release tree:

- all 18 pre-XInput real `SendInput` / ordinary-concurrency / complex-Hold checks PASS;
- Runtime Observation in the real-input lanes identifies the executable as `InputStitch 1.3.1-beta.2` and reports Base-only / no isolated virtual slot without initializing ViGEm just for observation;
- `failures=0`;
- controller preflight then hits the known laptop ViGEm→XInput observation environment blocker:

```text
SUMMARY: BLOCKED | checks=18 | failures=0
```

This remains environment `BLOCKED`, not a product pass/failure. Do not reinstall system drivers solely to force it green without a separate troubleshooting decision.

### Real neutral slot-order probe

`tools/InputLab/run-slot-order-probe.ps1` creates four temporary neutral ViGEm Xbox devices and submits no button/stick/trigger report.

Verified real ordering:

```text
initial: InputStitch test device = 3; external test devices = 0/1/2
re-enumerated with InputStitch first: InputStitch = 0; external = 1/2/3
EXPECTED_ORDER=True
```

This verifies the Windows/ViGEm connection-order mechanism at the current four-slot maximum but does not replace physical PnP + HidHide + game acceptance.

### Published beta.1 verification

`v1.3.1-beta.1` was published after local verification, a diagnostic GitHub CI pass, and a final `[publish-beta]` workflow pass.

- tag: `v1.3.1-beta.1`;
- tag commit: `303704878669ddad8f65bbcf2fe9374a2d83a00d`;
- Release is public, `prerelease=true`, `draft=false`;
- x64, x86, Source.zip, `InputStitch-beta.xml`, `SHA256SUMS.txt` present;
- GitHub reported SHA-256 digest for all five assets;
- `releases/latest` remains Stable `v1.3.0`.

Do not mutate this release/tag.

### beta.2 CI backup-ordering issue found and fixed

The first beta.2 non-publishing verification commit `7d17309` passed GitHub `windows-2022`. The first publish-trigger commit `f505ceb` changed no source but its repeated verify exposed a pre-existing nondeterministic `ProductivityTests.TestSave` failure: `Expected retained revision 3`.

Root cause was real product behavior, not Layer/Controller code. Valid configuration backups are retained by descending file name. Backup names used `DateTime.UtcNow` formatted with seven fractional digits plus a random GUID, but Windows wall-clock resolution can return the exact same `UtcNow` value for several rapid saves. When timestamps tied, random GUID ordering accidentally decided which backup was considered newer.

Fix in `ConfigStore`:

- valid-backup sort timestamps are allocated strictly monotonically;
- allocation considers current UTC, the latest in-process allocation for that backup directory, and the latest valid-backup timestamp already present on disk;
- this makes ordering stable across rapid same-clock saves, short clock rollback, and process restart;
- GUID remains only a uniqueness suffix, never a retention-order tie breaker.

Deterministic regression coverage now forces all rapid saves to observe one frozen UTC value and separately simulates a cold directory containing a future-dated valid backup while the new process clock is rolled back to 2001. Both paths retain chronological save order. Productivity coverage is now **360 PASS**.

A new non-publishing GitHub verification commit `cfe906c` passed the full `windows-2022` verify after this fix. The final no-source-change publish trigger `fbb9cef` then passed the same verify and published beta.2.

### Published beta.2 verification

All beta.2 release gates are satisfied:

- tag: `v1.3.1-beta.2`;
- tag commit: `fbb9cefc6efdbbd1e0f1d9efb036de019b07723f`;
- GitHub workflow `34383569935`: `completed / success`;
- Release is public, `prerelease=true`, `draft=false`;
- x64, x86, Source.zip, `InputStitch-beta.xml`, `SHA256SUMS.txt` are present;
- GitHub reports SHA-256 digests for all five assets;
- `releases/latest` still resolves to Stable `v1.3.0` (`prerelease=false`).

`v1.3.1-beta.2` is now immutable release history. Future work must use a new version/tag.

### Published Stable 1.4.0 verification

`v1.4.0` promotes the validated 1.3.1 Beta controller-input/routing and flexible-Layer tree into Stable while keeping Controller Takeover explicitly Experimental.

- non-publishing Stable candidate commit: `c998ddb`; GitHub workflow `34390506022`: `completed / success`;
- final no-source-change publish commit / tag target: `ab2639e2ac6354013f3182cf4e921dabe36ae6fb`;
- tag: `v1.4.0`;
- `releases/latest` resolves to `v1.4.0`;
- Release page and all five expected Stable assets return HTTP 200;
- x64 remote asset: 750,080 bytes, SHA-256 `39b0d9bb527ab195375694ac630461fe0aa0c423de4d7d2dac40e7cb80f365c8`;
- x86 remote asset: 750,592 bytes, SHA-256 `ca712a9fd57ab15271f57ef6582a650d13427ebb3bbbe78024497a33f4130a99`;
- Source.zip SHA-256 `ae768be60da7af484864ef312295607a6a73fdff6a1d981a63f5ea79ab194a20`;
- `InputStitch-update.xml` SHA-256 `6925987e5b69fb840249d67f5ebe581783dac54e7481e7138aaf903ce111a35f` and advertises `<Version>1.4.0</Version>` through the Stable `releases/latest/download` endpoints;
- downloaded remote x64/x86 EXEs report `ProductName=InputStitch`, `ProductVersion=1.4.0`, `FileVersion=1.4.0.0`;
- downloaded remote x64/x86/Source.zip/manifest hashes all match the published `SHA256SUMS.txt`;
- the main release workflow badge reports `passing` after publication.

`v1.4.0` is immutable Stable release history. Future release work must use a new version/tag. Physical controller + HidHide + target-game takeover acceptance remains blocked by missing hardware and does not change the Stable status of the rest of 1.4.0.

### Published Stable 1.4.1 hotfix verification

`v1.4.1` is a narrow UI-lifecycle hotfix prompted by a real post-update user log from 1.4.0. The update transaction itself completed successfully, but the new 1.4.0 process later raised `ObjectDisposedException` from WinForms `ContextMenuStrip` / `ModalMenuFilter` handling.

Root cause and fix:

- `ShowLayerManagementMenu()` synchronously disposed its temporary `ContextMenuStrip` from the menu `Closed` event while WinForms could still be completing `ToolStripDropDown.OnItemClicked` / visibility cleanup;
- Layer Management and Quick Create now share `DeferContextMenuDispose`, which posts disposal to the next UI message turn;
- MainForm shutdown no longer synchronously disposes `toolsMenu` / `trayMenu`, because a menu-triggered shutdown can leave WinForms' `ModalMenuFilter` holding a reference until the current click/close message fully unwinds;
- UI Safety / diagnostics coverage increased from 23 to **26 PASS**, directly checking deferred disposal and the disposed-dispatcher boundary;
- no feature or configuration-format changes were introduced.

Release evidence:

- non-publishing candidate commit: `d14b346`; GitHub workflow `34396249147`: `completed / success`;
- final no-source-change publish commit / tag target: `592e2febaab66636f8139d8264261c1906d74d99`;
- publish workflow `34396527972`: `completed / success`;
- tag: `v1.4.1`, exact target `592e2febaab66636f8139d8264261c1906d74d99`;
- `releases/latest` resolves to `v1.4.1`, `prerelease=false`, `draft=false`;
- x64 remote asset: 747,008 bytes, SHA-256 `69193ead5dcb37813a1e7350c1d0b34f4884512cc1d9d4cb29d1647859880c60`;
- x86 remote asset: 748,032 bytes, SHA-256 `8249848f98fd15c4547a25b9bdceb382435268c806ce89a27a7e9362f5b4258e`;
- Source.zip SHA-256 `31a2b93146c71e2e95d14738c62f39fff55b8d597c345b11995f4c93d4c98a2f`;
- `InputStitch-update.xml` SHA-256 `06c48ddbabd0cab9e5a71473c5aec1adf73bed3a432caee4c4f31da322789737` and advertises `<Version>1.4.1</Version>` through Stable `releases/latest/download` endpoints;
- remote-download verification independently downloaded x64/x86/Source.zip/manifest and matched all four hashes against `SHA256SUMS.txt`;
- downloaded x64/x86 EXEs report `ProductName=InputStitch`, `ProductVersion=1.4.1`, `FileVersion=1.4.1.0`;
- Source.zip contains the repaired `InputStitch.cs`, `tests/UiSafetyTests.cs`, release metadata and current documentation;
- local full regression, Input Lab `18 PASS / failures=0` before the known environment blocker, and neutral four-slot probe `EXPECTED_ORDER=True` all passed before publication.

The older log also contained historical 1.3.0 `Nefarius.ViGEm.Client` resolution failures. Both the saved 1.3.0 executable and current 1.4.x executable contain the embedded ViGEm client resource, and isolated `EmbeddedDependencyLoader.Register → GamepadOutput.NeutralizeAll` probes pass for both. That separate historical issue was therefore not modified without a current reproducer; investigate independently if it appears again under 1.4.1.

`v1.4.1` is now immutable Stable release history. Future release work must use a new version/tag.

## Public documentation rule

Public Simplified Chinese README/update text must be ordinary-user-first:

1. first sentence: what users can now do;
2. practical behavior and limitations in plain Chinese;
3. only then technical names such as XInput, ViGEm, Output Ownership, SourceId.

Technical design notes may use internal English type/API names freely.

## Next step after beta.2

Active non-hardware order:

1. introduce a virtual-output backend boundary and keep ViGEm as the first implementation with behavior unchanged;
2. build persistent `DeviceKey` / Device Manager inventory and diagnostics;
3. change Router from “all visible external XInput sources” to explicit source selection + per-device policy;
4. introduce reusable Analog Transform primitives (deadzone/curve/scaling/inversion/zones/merge policy);
5. generalize Activator + Condition semantics rather than adding more one-off trigger modes;
6. then improve Layer ergonomics, profile/context behavior and evaluate `IInputProvider` + SDL3;
7. only after the output interface is stable, prototype/compare a second virtual-output backend.

Hardware-dependent acceptance remains parked in parallel until a physical controller exists. When available:

1. validate physical starting layouts with InputStitch initially in slots 0/1/2/3;
2. validate HidHide while InputStitch remains whitelisted/readable;
3. prove a real game loses selected original devices and sees the routed slot-0 virtual controller;
4. intentionally break slot/Router/HidHide state and verify the new health monitor restores without doubled input;
5. only then call controller takeover hardware-mature.

Do not spend the next phase chasing controller-model count, gyro/touchpad/vendor haptics, complex Layer stacking or unrestricted plugins/scripts unless a concrete workflow justifies them.

## Important design constraints

- Do not regress Stable 1.3.0 source-local ownership semantics.
- Emergency Stop remains absolute priority.
- Output backend failure remains fail-closed.
- Macro/runtime/ownership/routing code must not become more tightly coupled to ViGEm; future output backends go behind a stable interface.
- XInput slot number is transient runtime state and must not become the durable user-facing device identity.
- InputStitch's own ViGEm output must never become an external trigger/router source.
- Respect `VirtualGamepadTypes.None`; incompatible features may refuse but must not silently recreate ViGEm.
- Controlled Replacement: **target slot not verified → no original-device hide**.
- Active takeover health failure must not leave Router generating duplicate routed input after originals are restored.
- Hiding must be explicit and recoverable; preserve pre-existing HidHide state.
- Do not write a kernel filter driver while a mature external hiding solution is sufficient.
- Layer remains eligibility/grouping on top of the common runtime, never a second execution engine.
- Analog transforms, activators and conditions must remain observable/testable and must not bypass Output Ownership or the common runtime.
- Observation/diagnostics must not create/connect virtual devices merely to inspect status.
- Stable and Beta release/update channels remain separate.
- Complete regression before every runtime-changing commit.

## Resume checklist on another computer

```text
1. Read AGENTS.md
2. Read PLAN.md
3. Read ROADMAP.md
4. Read docs/PRODUCT_STRATEGY.md
5. Read HANDOFF.md
6. git log -8 --oneline --decorate
7. git status --short --branch
8. git fetch / git pull as appropriate
9. Confirm expected version and working-tree baseline before editing
```

On a new development machine, verify Git, .NET Framework 4.7.2 reference assemblies/build path, embedded ViGEm client dependency and GitHub authentication before release work.
