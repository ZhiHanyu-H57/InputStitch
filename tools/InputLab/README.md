# InputStitch Input Lab

Input Lab is a developer-only black-box input target for validating InputStitch without launching a game for every iteration.

## v0.1 scope

- Keyboard visualization through a Windows low-level keyboard hook.
- Physical/system vs injected (`SendInput`) keyboard event distinction.
- Mouse button, side-button, wheel, position and optional movement logging through a low-level mouse hook.
- XInput controller discovery (slots 0-3), buttons, D-pad, triggers and both stick vectors.
- High-resolution relative event log for press/release ordering and timing inspection.
- Foreground indicator so the operator can verify that Input Lab is the current target for foreground-directed keyboard/mouse output.

## Build

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\build.ps1
```

Output:

```text
artifacts\InputLab\InputStitch-InputLab.exe
```

The normal launch opens the **Keyboard** tab. Automated/manual validation can open another view directly:

```powershell
.\artifacts\InputLab\InputStitch-InputLab.exe --view devices
.\artifacts\InputLab\InputStitch-InputLab.exe --view log
```

## Typical use

1. Start InputStitch and configure the mappings under test.
2. Start `InputStitch-InputLab.exe`.
3. Bring Input Lab to the foreground; the header should say `TARGET: FOREGROUND`.
4. Trigger the InputStitch mapping.
5. Observe highlights and the event log.
6. For virtual Xbox output, verify the XInput panel values and release order.

Blue keyboard/mouse highlights mean Windows marked the event as injected. Green means the hook did not report an injected flag.

## Important limitation

v0.1 is a black-box Windows hook/XInput observer, not yet a complete game-API simulator. It does not currently provide separate Raw Input, DirectInput/HID, or GameInput lanes. A clean Input Lab result therefore reduces routine game-launch testing but does not replace final compatibility testing in real games.

## Planned next steps

- Raw Input comparison lane for keyboard/mouse.
- Target-window message lane so global hook observation can be compared with what the foreground target actually receives.
- DirectInput/HID/DS4 observation path.
- Scenario recorder/export and expected-vs-observed assertions for automated ownership acceptance.
