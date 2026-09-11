# InputStitch

[English](README.md) | [简体中文](README.zh-CN.md)

**InputStitch — Deterministic Input Orchestration & Automation for Windows**

InputStitch is a visual input-orchestration and macro tool for Windows. It combines keyboard, mouse, physical/virtual controller triggers, concurrent macro execution, routing, layers, and optional virtual-controller output in one observable runtime.

The project is deliberately **not** trying to win a controller-model or hardware-feature-count race. Its core priorities are precise timing, deterministic concurrent ownership, explainable runtime state, safe failure/recovery, and keeping simple “press this → send that” workflows simple.

The application is built with Windows Forms and .NET Framework 4.7.2. Its interface can switch instantly between **English** and **Simplified Chinese** without restarting.

See [Product positioning and competitive strategy](docs/PRODUCT_STRATEGY.md) for the 2026 comparison with projects such as PadForge, reWASD, Joystick Gremlin, Steam Input and DS4Windows, and see [ROADMAP.md](ROADMAP.md) for the resulting development priorities.

## Download

**Stable: v1.4.3.** This release fixes a reliability issue where global keyboard hotkeys could stop responding after long-running sessions by adding an independent physical-keyboard fallback and automatic keyboard-hook recovery. The website-first update path introduced in 1.4.2 remains unchanged, with GitHub retained as an automatic fallback.

**Controller takeover remains Experimental in Stable 1.4.3.** Its transaction, rollback, recovery, slot-acquisition and runtime-health logic are implemented and heavily tested, but final physical-controller + HidHide + target-game acceptance remains pending because the development machine has no physical XInput test controller. The rest of the 1.4.x feature set is not blocked by that hardware-only validation gap.

The project website is the preferred Stable download path; GitHub Releases remain available as a fallback and release-history source:

| Windows architecture | Direct download |
| --- | --- |
| 64-bit Windows (x64) | [InputStitch-1.4.3-Windows-x64.exe](../../releases/latest/download/InputStitch-1.4.3-Windows-x64.exe) |
| 32-bit Windows (x86) | [InputStitch-1.4.3-Windows-x86.exe](../../releases/latest/download/InputStitch-1.4.3-Windows-x86.exe) |
| Complete source code | [InputStitch-1.4.3-Source.zip](../../releases/latest/download/InputStitch-1.4.3-Source.zip) |
| Checksums | [SHA256SUMS.txt](../../releases/latest/download/SHA256SUMS.txt) |

Previous `v1.3.1-beta.2` prerelease artifacts remain available for historical comparison/testing:

| Beta artifact | Direct download |
| --- | --- |
| 64-bit Windows (x64) | [InputStitch-1.3.1-beta.2-Windows-x64.exe](../../releases/download/v1.3.1-beta.2/InputStitch-1.3.1-beta.2-Windows-x64.exe) |
| 32-bit Windows (x86) | [InputStitch-1.3.1-beta.2-Windows-x86.exe](../../releases/download/v1.3.1-beta.2/InputStitch-1.3.1-beta.2-Windows-x86.exe) |
| Complete Beta source | [InputStitch-1.3.1-beta.2-Source.zip](../../releases/download/v1.3.1-beta.2/InputStitch-1.3.1-beta.2-Source.zip) |
| Beta manifest | [InputStitch-beta.xml](../../releases/download/v1.3.1-beta.2/InputStitch-beta.xml) |
| Beta checksums | [SHA256SUMS.txt](../../releases/download/v1.3.1-beta.2/SHA256SUMS.txt) |

InputStitch supports Windows only. There is no macOS or Linux executable. If you are unsure which Windows build to use, choose x64 on a modern 64-bit installation.

The executable is portable: download it, place it in a folder where you have write access, and run it. .NET Framework 4.7.2 or a compatible later release must be installed. Keyboard and mouse output needs no extra driver. Virtual gamepad output additionally requires the separately installed ViGEmBus driver described below.

## Highlights

- Visual editing for keyboard, mouse-button, side-button, wheel, Xbox 360, and PS4 / DualShock 4 steps
- Virtual sticks (direction from `-180°..180°` and strength from `0%..100%`), analog triggers (`0%..100%`), face buttons, shoulders, stick clicks, D-pad, and menu buttons
- A live virtual-controller preview while editing, highlighting the selected button, trigger, or stick direction
- Per-step key-hold duration and delay controls
- Quick Create templates for Held Mapping, fixed-count repetition, and ordered sequences; standalone Ctrl/Shift/Alt/Win, normal keys, and mouse buttons can be Held Mapping triggers
- Input Ownership + Concurrent Macro Runtime: multiple qualifying Held Mappings and multiple distinct ordinary timed/Toggle/Advanced-Hold macros can stay active together in Stable 1.3.0
- Deterministic merged output: digital reference ownership, trigger maximum, circular stick-vector merge, and D-pad opposite-axis cancellation
- Lightweight Runtime observation for active sources, source contributions, merged state, ordinary macro step/phase, and stop reasons
- Single-step / Next Step execution for ordinary timed macros
- Step Undo/Redo with a bounded 50-operation history
- Safer staged configuration saves with verification and five recent valid-config backups
- Global hotkeys, press-to-toggle, and hold-to-run modes, including modifier-chord Hold triggers
- One-click **Open Project on GitHub** entry in the gear menu
- Finite repetition or infinite looping
- Physical input recording with automatic timing; mouse movement is intentionally not recorded
- Scan-code keyboard output for better compatibility with many games
- Macro packages for sharing selected macros
- Profiles for saving and switching complete setups
- Optional target-window activation and profile switching by foreground application
- Selective shortcut-conflict protection that allows ordinary gameplay such as holding `W + Shift`, while guarding high-risk unintended combinations
- Configurable global Emergency Stop, which remains available independently of normal macro triggers
- Instant Simplified Chinese / English interface switching
- Built-in update checking with Automatic, Manual, and Disabled modes; official downloads are verified against the release SHA-256 manifest before installation
- DPI-aware, resizable Windows Forms interface

## Quick start

1. Download the executable that matches your Windows architecture.
2. Start InputStitch and create or select a macro. The **New** menu can also create a Held Mapping, fixed-count repeat, or sequence template.
3. Add steps manually or use **Record Macro**. Recording captures physical keyboard and mouse input; virtual gamepad steps are added manually.
4. Capture a trigger key and choose the trigger mode.
5. Run the macro from the main window or enable its global trigger.
6. Before using a macro in another application, confirm the Emergency Stop hotkey shown in InputStitch.

Use the gear button to open settings, including **Language / 语言**, virtual controller type, update preferences, safety options, target-window behavior, runtime observation/diagnostics, and tray preferences. Xbox 360 remains the default virtual controller because it has the broadest compatibility with Windows/XInput games; choose PS4 / DualShock 4 for games that support that device path, or choose **Do not create a virtual controller** when you only need keyboard/mouse macros or controller-triggered keyboard/mouse output. That choice persists and prevents saved gamepad-output macros from creating ViGEm at startup. Stable builds can use the automatic SHA-256-verified Stable update path. Prerelease/Beta builds deliberately disable unattended automatic update checks; a manual Beta update opens GitHub Releases instead of touching the Stable manifest.

The stick editor uses direction and strength: `0°` is forward, `90°` is right, `-90°` is left, and `±180°` is backward. Existing X/Y values remain backward compatible and are preserved unless the user actually edits direction or strength. For a key-to-controller hold mapping, Quick Create → Held Mapping is the easiest path. In 1.3.0-beta.1, qualifying Held Mappings are independent ownership sources: releasing one removes only its contribution, while the remaining stick/trigger/button sources stay active and are re-merged.

## Experimental controller takeover in v1.3.1-beta.2

Controller merging by itself does not suppress original devices. **Tools → Controller takeover (Experimental)** adds the controlled-replacement path. If the InputStitch virtual Xbox is not already XInput slot 0, takeover first stops managed controller output, temporarily re-enumerates all present external XUSB controllers, reconnects InputStitch first, and requires a hard slot-0 check. It then restores every external controller and re-verifies their device identities, Router visibility, and slot 0 before HidHide is allowed to hide any selected original device.

The slot-reordering scope and the hiding scope are intentionally different: every external XUSB controller may need a short re-enumeration so slot 0 becomes available, but only explicitly selected external device identities are passed to HidHide. Any failure stops the transaction and attempts recovery; InputStitch never hides the old slot-0 controller first and merely hopes the virtual controller will move into slot 0.

Beta.2 adds continuous takeover health monitoring after activation. A background monitor checks the target slot, Router readiness, the expected routed XInput sources, HidHide cloak state, selected hidden-device membership, and the InputStitch HidHide application whitelist. One transient failure is tolerated; two consecutive failures trigger a single fail-safe disengage. InputStitch then restores its HidHide changes, stops Router, clears managed routed output, and disables controller merging so restored original input is not doubled by routed input. Recovery data is retained if restoration cannot complete.

The current XInput backend supports at most **three external controllers + one InputStitch virtual Xbox** at the same time. A neutral four-controller ViGEm probe on the development machine verified the real ordering transition from `external 0/1/2 + InputStitch 3` to `InputStitch 0 + external 1/2/3` without submitting button, stick, or trigger input. Final physical-controller + HidHide + real-game takeover still requires hardware acceptance; the current development laptop does not have HidHide or a physical XInput test controller installed/connected.

## Flexible mapping layers in v1.3.1-beta.2

Base is always eligible, and you may now create any number of additional named layers. **Manage layers...** can add, rename, or delete layers; deleting one moves its macros safely back to Base and deleting the currently active layer first returns to Base-only.

Every layer, including Base, may have its own keyboard, mouse, or controller switch trigger. Base's switch trigger means “return to Base-only.” Only one non-Base layer is active at a time, so the eligible set is always Base plus the current named layer. Switching layers reuses the existing concurrent runtime: old-layer runs stop and release only their own sources, while Base and unrelated ownership remain intact. Newly eligible triggers that were already physically held still require release + press before they may start.

The Layer XML format now replaces the saved layer list instead of appending it into constructor defaults. Truly old configurations with no layer data still migrate to Base + Layer 1, but once a modern configuration has saved an explicit list, deleting legacy Layer 1 survives save/restart instead of being recreated by default initialization.

## Virtual keyboard and idle input

The arrow next to **Capture Trigger** or **Capture Input** opens a full-size virtual keyboard. Click a key to select it and click again to clear it; choose single-key or multi-key mode, then confirm. This also lets compact-keyboard users choose navigation, numpad and function keys. Left/right Shift, Ctrl, Alt, Win and Esc can be selected individually. Trigger combinations use modifiers plus one main key; multiple step keys are inserted as a chord with matching releases.

The controller preview follows the selected Xbox 360 or PS4 layout. A stick direction of 0° points forward, +90° right, -90° left and ±180° backward; strength controls its travel. Previewing a step does not send input.

**Settings → Automation → Idle gamepad input** can send a short, configurable controller action after inactivity. It is off by default; the formal default pulse is 120 seconds, left stick down for 150 ms. This section now has its own independent **Idle target**: switch to the game, return to Settings, then choose **Use recent window**. The idle target is used only for this timer and does **not** depend on or modify the main target window, UI-run activation, or automatic profile switching. With an idle target configured, keyboard/mouse activity resets that target's countdown only while the target process is foreground, so typing or moving the mouse in another app does not prevent a background-game pulse. If the configured idle target is currently absent, idle output pauses completely and sends no controller pulse; when the target returns, a fresh full idle interval starts. Without an idle target, keyboard/mouse inactivity remains global. Physical controller activity and macros restart the timer; editing dialogs pause it. Emergency Stop disables idle output until you explicitly enable it again. Physical mouse movement is detected with Raw Input so games that recenter/warp the cursor do not falsely keep the timer active. This uses the existing virtual controller without activating another window. A background game must already accept that controller and background input; InputStitch cannot force a game to do so. Connect before launching games that only detect controllers at startup.

## Optional virtual gamepad driver

Virtual gamepad output, controller merging, idle gamepad input, and controller takeover use [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest). If you only need keyboard/mouse macros or use a physical/other XInput controller as a trigger for keyboard/mouse output, choose **Do not create a virtual controller** and ViGEm is not created for those features. The upstream project is retired and no longer receives updates. InputStitch never silently installs the driver: if it is missing, the app explains the requirement and offers to open the original author's official GitHub Release page. Download it only from that official page, install it yourself, and restart InputStitch.

ViGEm remains the current production backend, but it is no longer treated as the permanent architecture. The roadmap now prioritizes a virtual-output backend interface before any second backend is adopted, so macros, Output Ownership, Router, Idle and takeover logic can remain independent of the underlying emulation technology. HIDMaestro and the standalone VIIPER server/API are examples to evaluate only after that abstraction is stable; deployment, security, architecture support and license compatibility are part of that decision.

Connect the virtual controller from **Settings → Virtual Gamepad Output** before launching a game. InputStitch keeps one virtual controller connected until the app exits. Ordinary source completion now removes only that source's ownership; other active Held Mapping or Idle sources remain merged. **Emergency Stop** and application shutdown are the global boundaries that clear every source and force the virtual controller neutral without unplugging it. This avoids games that cache controllers at startup and ignore later hot-plugging. Changing between Xbox 360 and PS4 while a game is open may require restarting the game.

## Where InputStitch is going

After reviewing current controller-remapping and automation projects, the roadmap deliberately prioritizes **platform depth before hardware breadth**.

The next non-hardware foundation is:

1. virtual-output backend abstraction;
2. persistent Device Identity / Device Manager;
3. explicit Router source selection and per-device policy;
4. reusable analog transforms such as deadzones, curves, sensitivity, inversion and zones;
5. a generalized Activator + Condition engine for press/release/hold/long/double/turbo and deterministic context rules.

Broader controller input is planned through an `IInputProvider` architecture, with SDL3 as the first general-purpose provider candidate rather than adding one device family/API at a time. Gyro, touchpad, vendor-specific haptics, plugin scripting, complex Layer stacking and radial/overlay UI remain later, evidence-driven features rather than current success metrics.

Physical-controller + HidHide + real-game acceptance of the experimental takeover path remains a separate **hardware-blocked validation lane** until suitable hardware is available. See [ROADMAP.md](ROADMAP.md) and [the strategy review](docs/PRODUCT_STRATEGY.md) for details.

## Safety notes

InputStitch sends synthetic keyboard, mouse, and optional virtual-controller input to Windows. Test new macros in a harmless application before using them with important work or a game.

- Keep an easy-to-reach Emergency Stop hotkey and test it first.
- Avoid running unreviewed macros or importing packages from sources you do not trust.
- Do not use automation where it violates software, service, game, workplace, or local rules.
- Online games and protected applications may prohibit automation or reject synthetic input. Compatibility is not guaranteed.
- Other controller tools can consume virtual-device or XInput player slots. If a game sees the wrong controller, close competing tools, connect InputStitch first, and then start the game.
- InputStitch does not bypass anti-cheat, access controls, or application security.

See [SECURITY.md](SECURITY.md) for vulnerability reporting.

## Build from source

Requirements:

- Windows
- .NET Framework 4.7.2 Developer Pack or compatible MSBuild tooling
- Visual Studio with .NET desktop development support, or the matching Build Tools

Build with either:

```powershell
.\build.ps1
```

or:

```bat
build.bat
```

You can also open `InputStitch.csproj` in Visual Studio. Release artifacts are built separately for x64 and x86.

## Data and privacy

Configuration, profiles, macro packages, backups, and logs are stored locally. InputStitch does not require an online account. Stable builds, by default, make an HTTPS request to this repository's latest Release manifest at startup to check for updates; this can be changed to manual or disabled in Settings. No configuration or macro content is uploaded. Review diagnostics and configuration files before sharing them because they may contain window titles, process names, macro names, or paths from your computer.

## Contributing

Bug reports and focused pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before contributing.

## License

This repository currently does **not** declare an open-source license. Public access to the source code does not by itself grant permission to copy, modify, redistribute, or use it beyond rights provided by applicable law. A license may be added by the repository owner later.

The embedded `Nefarius.ViGEm.Client` dependency is separately licensed under MIT; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

### Beta Shift compatibility

In beta.2, a standalone Shift trigger in **Hold** mode with **gamepad-only steps** automatically passes the physical Shift through to the game, preserving sprint alongside controller output. The suppression checkbox explains this exception; other macros retain their suppression preference. This fixes a confirmed suppression conflict; real-game acceptance still needs testing. Stable v1.1.0 users can first try unchecking trigger suppression on the affected macro.

### Foreground hotkeys and diagnostics (beta.3)

Hovering alone or selecting a non-editing button/list no longer blocks hotkey starts. Focused name/number/combo editors still pause hotkeys with a visible reason; click an empty area to finish editing. Running mouse macros retain hover protection to avoid clicking controls. Diagnostics include the last 64 macro lifecycle events in memory, not a continuous keyboard log. A submitted output does not prove that a game received it.
### Productivity and config safety (beta.4)

Beta.4 added staged/verified configuration saves with five recent valid backups, three Quick Create templates, and step Undo/Redo. Held Mapping accepts normal keyboard keys, standalone left/right Ctrl/Shift/Alt/Win, and mouse buttons including side buttons. Stable 1.3.0 additionally supports modifier-chord Hold triggers; wheel input remains unsupported as a physical Hold trigger because it has no persistent down state. Duplicate enabled triggers intentionally use macro-list order as priority. Idle gamepad detection ignores application-driven cursor recentering and uses its own independent Idle target; other-app typing does not block a background-game pulse, a missing configured Idle target pauses output completely, and target return starts a fresh full idle interval. Continue live game/controller feedback as normal post-release validation.

### Input Ownership and universal concurrency (1.3.x)

Beta 1 replaces the old single-owner output assumption with explicit source ownership. Qualifying Held Mappings can stay active together and one ordinary timed macro may coexist with them. Digital state uses reference ownership, triggers use the maximum requested value, sticks merge vectors with circular normalization, and opposite D-pad directions cancel per axis. Tools → **Runtime observation...** shows active sources and the merged result; ordinary timed macros also gain **Single-step / Next Step**.

`v1.3.0-beta.2` published the **unified Concurrent Macro Runtime**: multiple distinct ordinary timed/Toggle macros and Advanced/complex Hold macros can overlap with independent RunId/SourceId/stop/timing state while Parallel Held Mappings remain active. Complex Hold runs keep per-run physical-trigger/lost-KeyUp release tracking; releasing one Hold stops only that run. The editor's live runtime/concurrency eligibility display comes from the same classifier used by execution. Duplicate physical triggers still use macro-list priority, Single-step remains an intentionally exclusive diagnostic mode, and Emergency Stop remains the absolute global clear. At that milestone Layer was still deferred; the 1.3.1 Beta line subsequently implemented physical XInput routing/takeover foundations and then arbitrary named Layers with switch bindings. See [the gamepad input/routing design](docs/GAMEPAD_INPUT_ROUTING.md), [the Layer design note](docs/LAYER_DESIGN.md), and [the current roadmap](ROADMAP.md).
