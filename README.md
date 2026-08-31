# InputStitch

[English](README.md) | [简体中文](README.zh-CN.md)

InputStitch is a lightweight visual keyboard and mouse macro tool for Windows. It is designed for precise timing, game-friendly input, and a dependable emergency stop.

The application is built with Windows Forms and .NET Framework 4.7.2. Its interface can switch instantly between **English** and **Simplified Chinese** without restarting.

## Download

Download a ready-to-run executable from the [latest GitHub Release](../../releases/latest):

| Windows architecture | Direct download |
| --- | --- |
| 64-bit Windows (x64) | [InputStitch-1.0.0-Windows-x64.exe](../../releases/latest/download/InputStitch-1.0.0-Windows-x64.exe) |
| 32-bit Windows (x86) | [InputStitch-1.0.0-Windows-x86.exe](../../releases/latest/download/InputStitch-1.0.0-Windows-x86.exe) |
| Complete source code | [InputStitch-1.0.0-Source.zip](../../releases/latest/download/InputStitch-1.0.0-Source.zip) |
| Checksums | [SHA256SUMS.txt](../../releases/latest/download/SHA256SUMS.txt) |

InputStitch supports Windows only. There is no macOS or Linux executable. If you are unsure which Windows build to use, choose x64 on a modern 64-bit installation.

The executable is portable: download it, place it in a folder where you have write access, and run it. .NET Framework 4.7.2 or a compatible later release must be installed.

## Highlights

- Visual editing for keyboard, mouse-button, side-button, and wheel steps
- Per-step key-hold duration and delay controls
- Global hotkeys, press-to-toggle, and hold-to-run modes
- Finite repetition or infinite looping
- Physical input recording with automatic timing; mouse movement is intentionally not recorded
- Scan-code keyboard output for better compatibility with many games
- Macro packages for sharing selected macros
- Profiles for saving and switching complete setups
- Optional target-window activation and profile switching by foreground application
- Selective shortcut-conflict protection that allows ordinary gameplay such as holding `W + Shift`, while guarding high-risk unintended combinations
- Configurable global Emergency Stop, which remains available independently of normal macro triggers
- Instant Simplified Chinese / English interface switching
- DPI-aware, resizable Windows Forms interface

## Quick start

1. Download the executable that matches your Windows architecture.
2. Start InputStitch and create or select a macro.
3. Add steps manually or use **Record Macro**.
4. Capture a trigger key and choose the trigger mode.
5. Run the macro from the main window or enable its global trigger.
6. Before using a macro in another application, confirm the Emergency Stop hotkey shown in InputStitch.

Use the gear button to open settings, including **Language / 语言**, safety options, target-window behavior, diagnostics, and tray preferences.

## Safety notes

InputStitch sends synthetic keyboard and mouse input to Windows. Test new macros in a harmless application before using them with important work or a game.

- Keep an easy-to-reach Emergency Stop hotkey and test it first.
- Avoid running unreviewed macros or importing packages from sources you do not trust.
- Do not use automation where it violates software, service, game, workplace, or local rules.
- Online games and protected applications may prohibit automation or reject synthetic input. Compatibility is not guaranteed.
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

Configuration, profiles, macro packages, backups, and logs are stored locally. InputStitch does not require an online account. Review diagnostics and configuration files before sharing them because they may contain window titles, process names, macro names, or paths from your computer.

## Contributing

Bug reports and focused pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before contributing.

## License

This repository currently does **not** declare an open-source license. Public access to the source code does not by itself grant permission to copy, modify, redistribute, or use it beyond rights provided by applicable law. A license may be added by the repository owner later.
