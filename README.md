# InputStitch

English | [简体中文](README.zh-CN.md)

Keyboard, mouse and controller macros for Windows. Record actions, edit steps and assign shortcuts. Optional tools include a screen crosshair, timer and network controls. The interface supports English and Simplified Chinese.

## Download 1.6.1

| System | Download |
| --- | --- |
| 64-bit Windows (usual choice) | [Windows x64](https://download.zhihanyu.com/latest/InputStitch-Windows-x64.exe) |
| 32-bit Windows | [Windows x86](https://download.zhihanyu.com/latest/InputStitch-Windows-x86.exe) |
| Source and other versions | [GitHub Releases](https://github.com/ZhiHanyu-H57/InputStitch/releases) |

Place the downloaded EXE in your tools folder and double-click it. Requires .NET Framework 4.7.2 or a compatible later version.

Version 1.6.1 fixes mouse triggers blocking application controls and bare keys such as PageDown failing while Shift is held. [Release notes](RELEASE_NOTES.md).

## Create your first macro

This example uses F8 to start repeated clicks and F8 again to stop:

1. Click ＋ New macro on the left, choose Repeat an action and name it.
2. Click Trigger and press F8, or open the virtual keyboard with the adjacent dropdown.
3. Choose Press to start / again to stop and enable Repeat until stopped.
4. Add a left mouse-button Press step, for example with a 25 ms hold and a 120 ms delay.
5. Edits save automatically. Switch to the application and press F8 to start, then again to stop.

Stop all at the bottom, or the default **Ctrl + Shift + F12** Emergency Stop, stops active macros and releases their output. Emergency Stop also pauses global triggers; uncheck Pause global macro triggers in the gear menu to resume.

## Record and edit

- Click Record Macro, switch to the application and perform keyboard, mouse-button or wheel actions. Return to InputStitch to stop recording. Target-window setup is optional; mouse movement is not recorded.
- Add steps by pressing a key or choosing it on the virtual keyboard or mouse diagram. X1/X2 are the two mouse side buttons.
- Double-click a step to edit it. Use Ctrl / Shift to select several steps, then Batch timing to change hold times, delays or both.
- Use Hold to run / release to stop for held mappings, or Run a sequence for ordered actions. Set the repeat count or continuous repeat in the main window.

[Macro setup and editing](https://inputstitch.zhihanyu.com/guide#steps)

## Screen timer

1. Click Timer… on the left and select Enable timer and Show timer.
2. In Content & style, choose a preset such as Minutes:seconds.ms. Checkboxes on the left control visibility; select a row to edit its content and formatting on the right.
3. In Output & position, choose Desktop overlay. Use the sliders for rough placement and X/Y fields for one-pixel adjustments.
4. In Hotkeys, choose a control mode, assign keys and Save.

One-key cycle runs start/show → pause → reset/hide. You can also choose two keys, three independent keys, or Custom actions for the Ready, Running and Paused states.

Use Ctrl / Shift or Select all to format several parts together. Use first style copies the first selected part’s complete formatting. Add text accepts Chinese and English. Use New line for multiple lines, then adjust line spacing and refresh interval in Output & position.

For OBS **Game Capture**, choose RTSS in-game overlay: install and start [RTSS 7.3.7+](https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download/), enable **Use Microsoft Detours API hooking** in RTSS Setup, and enable Capture third-party overlays in OBS Game Capture properties. Use Desktop overlay for viewing on your screen; OBS Display Capture also records it.

[Timer setup and shortcut modes](https://inputstitch.zhihanyu.com/guide#timer)

## Network control

1. Open the gear menu → Network control… and select Enable network hotkeys.
2. Choose Selected applications and Add apps… to select the EXE files that run the applications; you can add several. Alternatively, choose Whole computer.
3. Assign block and restore shortcuts, then Save. Using the same shortcut for both toggles blocking and restoring. Allow the Windows administrator prompt on first use.
4. In Sounds, enable or disable each cue and adjust its volume. You can import and preview 8/16-bit PCM WAV files.

Use the restore shortcut or Restore network in the gear menu to reconnect. Emergency Stop and exiting InputStitch also remove the block. The next launch starts connected.

[Network setup](https://inputstitch.zhihanyu.com/guide#network)

## Screen crosshair

Open Screen crosshair… on the left, enable it and choose a style. **Corner-cut square** is a square ring with a gap at each corner. Adjust Size, Line width and Corner gap, then colors, outline and opacity.

The crosshair starts at the center of the selected display. X/Y offsets adjust it by the pixel. Click Save crosshair to keep the settings; the default show/hide shortcut is **Ctrl + Alt + F10**. Set the game to windowed or borderless mode.

[Crosshair setup](https://inputstitch.zhihanyu.com/guide#crosshair)

## Controllers, backups and updates

- Keyboard and mouse macros are ready to use. Virtual Xbox 360 / DualShock 4 output requires [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest). Select a type in Settings and connect it; choose Do not create a virtual controller for keyboard/mouse-only use. [Controller setup](https://inputstitch.zhihanyu.com/guide#gamepad)
- Use Import & backup to export macros or profiles for backup, sharing or another PC. Configuration lives in `%APPDATA%\InputStitch`.
- Check for updates in Settings, or close the old program and run the new EXE. Macros and settings from 1.4.3 and 1.5.x load automatically. In-app updates use the fixed name `InputStitch.exe` and back up the previous program and configuration.

[Website and user guide](https://inputstitch.zhihanyu.com/guide) · [Report an issue](https://github.com/ZhiHanyu-H57/InputStitch/issues) · [Contributing](CONTRIBUTING.md)

Configuration, macros and logs stay on your PC. Set update checking to Automatic, Manual or Disabled in Settings.

## License

This repository has no declared open-source license. Third-party licenses and notices are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
