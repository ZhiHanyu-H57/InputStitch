# Input Lab v0.3.0

Input Lab v0.3.0 makes the manual black-box tester much easier to use and adds a missing observation lane.

## What changed

- Added a target-window Win32 message lane for keyboard and mouse (`WM_KEY*` / `WM_MOUSE*`). This shows whether Input Lab itself, including focused child controls, actually received a normal window message.
- Kept the existing low-level hook, Raw Input and XInput lanes so the same input can be compared across APIs.
- Redesigned the keyboard view into a larger six-row layout that uses substantially more of the window and places navigation/arrow keys in more familiar positions.
- Increased keycap size and contrast.
- Fixed the practical visibility problem where a quick tap could change to the active color and return to idle too quickly to see. Held keys remain strongly highlighted; releases now retain a 180 ms visual afterglow.
- Added independent `0.3.0` file/product version metadata.
- Added built-in self-tests for target-window message observation and keyboard highlight behavior.
- Added a packaging script that creates a standalone x64 EXE, ZIP and SHA-256 checksums.

## Scope and limitations

Input Lab is still a developer/test utility, not a game input emulator. It observes low-level hooks, Raw Input, ordinary target-window messages and XInput, but it does not yet emulate or observe DirectInput/HID/DS4, GameInput, anti-cheat behavior, privilege-boundary behavior, exclusive-fullscreen behavior or game-specific input stacks.
