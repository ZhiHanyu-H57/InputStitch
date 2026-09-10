# InputStitch Input Lab

Input Lab is a developer-only black-box input target for validating InputStitch without launching a real game for every iteration. It is deliberately separate from the product runtime.

## v0.3.1 safety hotfix

v0.3.1 hardens automated testing and fixes keyboard visualization issues found during hands-on testing. The unexpected repeating keystrokes seen during development were later traced to a macro configured in the mouse driver, not Input Lab; the desktop-isolation hardening is retained as a safety invariant.

- `--self-test-keyboard-visual` is now fully in-process: it exercises the Raw Input keyboard-highlight fallback and does **not** call `keybd_event`, `SendInput`, or another system-wide keyboard injection API.
- Physical keyboard Raw Input now runs as a passive background sink and can drive the keyboard visualization when the low-level keyboard hook misses an event; recent hook events still take precedence so injected-event coloring is preserved.
- Left/right Shift, Ctrl and Alt are normalized for the Raw Input visualization path.
- The six keyboard rows now use compact fixed heights instead of stretching to divide the entire tab height, so row spacing stays close to a physical keyboard when the window is enlarged.
- In automation mode, the Input Lab low-level hooks swallow **all injected keyboard and mouse events** after observing them, instead of special-casing only keyboard `K` and mouse `X2`.
- `package.ps1` remains safe for an interactive desktop: it builds the viewer and runs only the self-contained window-message and keyboard-visual self-tests.
- `run-acceptance.ps1` remains a real-output black-box developer test. It intentionally exercises InputStitch's `SendInput` and ViGEm paths, refuses to start unless `-AllowRealOutput` is supplied, and should only be launched while the desktop is idle and no other InputStitch instance is active.

## v0.3.x scope

Manual observation:

- Larger six-row keyboard visualization through a Windows low-level keyboard hook. Held keys use a stronger highlight; quick taps keep a 180 ms release afterglow so the state change remains visible.
- Physical/system vs injected (`SendInput`) keyboard event distinction.
- Mouse left/right/middle/X1/X2, wheel, position and optional movement logging through a low-level mouse hook.
- Raw Input comparison for keyboard and mouse; keyboard uses a passive background sink so it can also keep the visualization alive when foreground changes.
- Target-window Win32 message lane for keyboard and mouse. It observes the `WM_KEY*` / `WM_MOUSE*` messages actually delivered to Input Lab or one of its child controls, including the destination control/HWND and message-specific metadata.
- XInput controller observation for buttons, D-pad, LT/RT and both stick vectors.
- High-resolution ordered event log for press/release timing and API-lane comparison.
- Foreground indicator so the operator can verify that Input Lab is the current target for foreground-directed input.

Automated acceptance:

- Uses InputStitch's real `MainForm` trigger/runtime path with an isolated in-memory/test-directory configuration.
- Uses the real `OutputOwnershipManager`, `SendInput` and ViGEm/XInput output path rather than a fake output backend.
- Does not read or save the user's normal `%APPDATA%\InputStitch\config.xml`.
- Runs Input Lab in non-activating background mode. Raw Input uses `RIDEV_INPUTSINK`, and automation-mode low-level hooks record then swallow all injected keyboard/mouse events before forwarding them to the rest of the normal desktop input chain.
- Performs a ViGEm/XInput preflight using a distinctive virtual-controller report before product assertions begin. If the controller enumerates but report state does not propagate, the run returns `SUMMARY: BLOCKED` instead of misreporting dozens of InputStitch failures.
- Produces explicit `PASS` / `FAIL` expected-vs-observed reports.
- Covers core ownership scenarios: WASD stick merge/release order, opposing-axis cancellation, trigger maximum merge, shared digital ownership, D-pad axis conflict, Held/timed coexistence, Emergency Stop cleanup, keyboard SendInput observation and mouse SendInput observation.
- The Stable-1.3.0 candidate adds a pre-XInput concurrent-timed lane: keyboard `K` and mouse `X2` ordinary macros must overlap, Runtime Observation must list both runs, stopping the keyboard run must leave the mouse run active, and both injected SendInput paths must be observed. A controller-backed keyboard+mouse+gamepad concurrent scenario follows when the ViGEm/XInput preflight is healthy.

Neutral slot-order probe:

- `run-slot-order-probe.ps1` creates four temporary neutral ViGEm Xbox controllers and never submits a button, stick or trigger report.
- It verifies the Windows/ViGEm connection-order behavior used by InputStitch's experimental slot-0 acquisition transaction.
- The expected maximum-layout transition is `external 0/1/2 + InputStitch 3 → InputStitch 0 + external 1/2/3` after all devices are removed and InputStitch reconnects first.
- It does not disable or hide physical devices and does not require HidHide.

## Build the viewer

Input Lab is a developer tool and is **not currently included in the normal InputStitch GitHub release assets**. The repository intentionally ignores `artifacts/`, so a freshly cloned/pulled tree may contain no viewer EXE until it is built locally.

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

The v0.3.1 viewer carries independent file/product version metadata (`0.3.1`).

To verify the new target-window message lane without sending real input to another application:

```powershell
.\artifacts\InputLab\InputStitch-InputLab.exe --self-test-window-messages
echo $LASTEXITCODE
```

Exit code `0` means the viewer's message filter observed the synthetic keyboard down/up and mouse button down/up messages posted to its own window.

To verify the keyboard visualization path itself **without injecting a real key into Windows**:

```powershell
.\artifacts\InputLab\InputStitch-InputLab.exe --self-test-keyboard-visual
echo $LASTEXITCODE
```

This self-test drives the Raw Input keyboard-visual fallback entirely inside Input Lab and checks the pressed highlight, release afterglow and eventual return to idle. It does not inject a real key into Windows. Exit code `0` means all three phases behaved as expected.

## Package a standalone download

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\package.ps1
```

This rebuilds the viewer, runs both built-in self-tests, then writes versioned standalone artifacts under:

```text
artifacts\InputLab\release\InputLab-0.3.1-Windows-x64.exe
artifacts\InputLab\release\InputLab-0.3.1-Windows-x64.zip
artifacts\InputLab\release\SHA256SUMS.txt
```

## Run automated ownership acceptance

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1 -AllowRealOutput
```

This rebuilds the isolated acceptance host, runs the black-box scenarios and writes:

```text
artifacts\InputLab\acceptance-report.txt
```

A successful run ends with a line like:

```text
SUMMARY: PASS | checks=77 | failures=0
```

If the local ViGEm/XUSB/XInput stack cannot propagate the preflight report, the tool exits with code `2` and reports the run as environment-blocked, for example:

```text
SUMMARY: BLOCKED | checks=5 | failures=0
```

`BLOCKED` is deliberately distinct from `FAIL`: it means the black-box controller path could not be established on that Windows environment, not that the ownership expectations failed.

A custom report path may be supplied:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1 -AllowRealOutput -ReportPath .\artifacts\InputLab\my-report.txt
```

The automated host creates a temporary ViGEm Xbox 360 virtual controller and cleans it up on exit, but it does **not** activate or bring Input Lab to the foreground. In the v0.3.0 verification on 2026-09-10, three consecutive full runs completed `77/77` with zero failures. Earlier runs on this laptop intermittently hit the documented ViGEm→XInput environment preflight blocker, so a future `BLOCKED` result should still be interpreted as an environment result rather than rewritten as a product pass/failure. Do not run the acceptance host while another InputStitch instance is actively driving the same virtual-controller stack.

## Run the neutral XInput slot-order probe

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-slot-order-probe.ps1
```

This probe is deliberately narrower than Controlled Replacement acceptance. It validates only the real ViGEm/XInput ordering mechanism and cleans up every temporary virtual controller on exit. A passing result ends with:

```text
EXPECTED_ORDER=True
```

It does **not** prove physical PnP cycling, HidHide suppression, or game-specific controller visibility; those remain separate real-hardware acceptance requirements.

## Typical manual use

1. Start InputStitch and configure the mappings under test.
2. Start `InputStitch-InputLab.exe`.
3. Bring Input Lab to the foreground; the header should say `TARGET: FOREGROUND`.
4. Trigger the InputStitch mapping.
5. Compare the low-level hook, Raw Input and Window Keyboard/Window Mouse event lanes in the Event log.
6. For virtual Xbox output, verify XInput buttons/triggers/sticks and release order.

Blue keyboard/mouse highlights mean the low-level hook reported the event as injected. Green means that hook did not report an injected flag. Raw Input is a separate API lane and may not report the same synthetic event in the same way; that difference is evidence, not automatically a failure.

The **Window Keyboard / Window Mouse** rows in the Event log answer a different question: did the actual Input Lab WinForms target receive a normal window message? Windows does not include the low-level injected flag in `WM_KEY*` / `WM_MOUSE*`, so use timestamps and input identity to compare this lane with the global low-level-hook lane. The window lane follows child controls as well as the top-level form, so clicking/focusing a tab, list or button does not make those messages disappear from the observation stream.

## Important limitations

Input Lab is not a complete game-API simulator. v0.3.1 covers low-level hooks, Raw Input (including background keyboard observation), target-window Win32 messages and XInput, but not yet:

- DirectInput/HID/DS4 observation;
- GameInput;
- anti-cheat behavior;
- privilege-boundary differences between InputStitch and the target;
- exclusive-fullscreen/game-specific input stacks.

Therefore a clean Input Lab run substantially reduces routine game-launch testing, but final compatibility acceptance still requires the real target game/application.

The automated acceptance host intentionally injects simulated trigger edges directly into InputStitch's internal trigger handler because InputStitch correctly ignores `LLKHF_INJECTED` / injected mouse events as physical macro triggers. Runtime execution, ownership merging and final Windows/ViGEm output remain real. The isolated host also verifies its target XInput slot by observing an actual distinctive ViGEm report rather than trusting slot metadata alone.

## Next useful extensions

- DirectInput/HID/DS4 observation.
- Long-duration/repeated-cycle soak scenarios and exported machine-readable reports.
- Real foreground-transition / lost-KeyUp automation where Windows permits reliable physical-state simulation.
