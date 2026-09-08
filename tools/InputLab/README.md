# InputStitch Input Lab

Input Lab is a developer-only black-box input target for validating InputStitch without launching a real game for every iteration. It is deliberately separate from the product runtime.

## v0.2 scope

Manual observation:

- Keyboard visualization through a Windows low-level keyboard hook.
- Physical/system vs injected (`SendInput`) keyboard event distinction.
- Mouse left/right/middle/X1/X2, wheel, position and optional movement logging through a low-level mouse hook.
- Foreground Raw Input comparison for keyboard and mouse.
- XInput controller observation for buttons, D-pad, LT/RT and both stick vectors.
- High-resolution ordered event log for press/release timing and API-lane comparison.
- Foreground indicator so the operator can verify that Input Lab is the current target for foreground-directed input.

Automated acceptance:

- Uses InputStitch's real `MainForm` trigger/runtime path with an isolated in-memory/test-directory configuration.
- Uses the real `OutputOwnershipManager`, `SendInput` and ViGEm/XInput output path rather than a fake output backend.
- Does not read or save the user's normal `%APPDATA%\InputStitch\config.xml`.
- Runs Input Lab in non-activating background mode. Raw Input uses `RIDEV_INPUTSINK`, and the acceptance-only injected `K` / mouse `X2` events are recorded by the low-level hooks and then swallowed so they do not reach the user's current foreground application.
- Performs a ViGEm/XInput preflight using a distinctive virtual-controller report before product assertions begin. If the controller enumerates but report state does not propagate, the run returns `SUMMARY: BLOCKED` instead of misreporting dozens of InputStitch failures.
- Produces explicit `PASS` / `FAIL` expected-vs-observed reports.
- Covers core ownership scenarios: WASD stick merge/release order, opposing-axis cancellation, trigger maximum merge, shared digital ownership, D-pad axis conflict, Held/timed coexistence, Emergency Stop cleanup, keyboard SendInput observation and mouse SendInput observation.
- The Stable-1.3.0 candidate adds a pre-XInput concurrent-timed lane: keyboard `K` and mouse `X2` ordinary macros must overlap, Runtime Observation must list both runs, stopping the keyboard run must leave the mouse run active, and both injected SendInput paths must be observed. A controller-backed keyboard+mouse+gamepad concurrent scenario follows when the ViGEm/XInput preflight is healthy.

## Build the viewer

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\build.ps1
```

Output:

```text
artifacts\InputLab\InputStitch-InputLab.exe
```

The normal launch opens the **Keyboard** tab. Other views can be opened directly:

```powershell
.\artifacts\InputLab\InputStitch-InputLab.exe --view devices
.\artifacts\InputLab\InputStitch-InputLab.exe --view log
```

## Run automated ownership acceptance

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1
```

This rebuilds the isolated acceptance host, runs the black-box scenarios and writes:

```text
artifacts\InputLab\acceptance-report.txt
```

A successful run ends with a line like:

```text
SUMMARY: PASS | checks=50 | failures=0
```

If the local ViGEm/XUSB/XInput stack cannot propagate the preflight report, the tool exits with code `2` and reports the run as environment-blocked, for example:

```text
SUMMARY: BLOCKED | checks=5 | failures=0
```

`BLOCKED` is deliberately distinct from `FAIL`: it means the black-box controller path could not be established on that Windows environment, not that the ownership expectations failed.

A custom report path may be supplied:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1 -ReportPath .\artifacts\InputLab\my-report.txt
```

The automated host creates a temporary ViGEm Xbox 360 virtual controller and cleans it up on exit, but it does **not** activate or bring Input Lab to the foreground. In the hardened v0.2 verification, three consecutive `50/50` runs were sampled every 50 ms and the acceptance process never became the foreground process. Do not run the acceptance host while another InputStitch instance is actively driving the same virtual-controller stack.

## Typical manual use

1. Start InputStitch and configure the mappings under test.
2. Start `InputStitch-InputLab.exe`.
3. Bring Input Lab to the foreground; the header should say `TARGET: FOREGROUND`.
4. Trigger the InputStitch mapping.
5. Compare the low-level hook and Raw Input event lanes in the Event log.
6. For virtual Xbox output, verify XInput buttons/triggers/sticks and release order.

Blue keyboard/mouse highlights mean the low-level hook reported the event as injected. Green means that hook did not report an injected flag. Raw Input is a separate API lane and may not report the same synthetic event in the same way; that difference is evidence, not automatically a failure.

## Important limitations

Input Lab is not a complete game-API simulator. v0.2 covers low-level hooks, foreground Raw Input and XInput, but not yet:

- target-window message delivery comparison;
- DirectInput/HID/DS4 observation;
- GameInput;
- anti-cheat behavior;
- privilege-boundary differences between InputStitch and the target;
- exclusive-fullscreen/game-specific input stacks.

Therefore a clean Input Lab run substantially reduces routine game-launch testing, but final compatibility acceptance still requires the real target game/application.

The automated acceptance host intentionally injects simulated trigger edges directly into InputStitch's internal trigger handler because InputStitch correctly ignores `LLKHF_INJECTED` / injected mouse events as physical macro triggers. Runtime execution, ownership merging and final Windows/ViGEm output remain real. The isolated host also verifies its target XInput slot by observing an actual distinctive ViGEm report rather than trusting slot metadata alone.

## Next useful extensions

- Target-window message lane to compare global observation with what the foreground window actually receives.
- DirectInput/HID/DS4 observation.
- Long-duration/repeated-cycle soak scenarios and exported machine-readable reports.
- Real foreground-transition / lost-KeyUp automation where Windows permits reliable physical-state simulation.
