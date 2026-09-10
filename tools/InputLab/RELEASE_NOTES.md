# Input Lab v0.3.1

Input Lab v0.3.1 combines automated-verification safety hardening with keyboard-visualization fixes found during hands-on testing.

## What changed

- Removed the system-wide `keybd_event` call from the keyboard-visual self-test. The self-test now exercises the Raw Input visual fallback in-process and emits no real keyboard input.
- Physical keyboard Raw Input now uses a passive background sink and drives the keyboard visualization when the low-level keyboard hook misses an event. Recent hook observations are de-duplicated so injected-event coloring is not overwritten by the Raw Input fallback.
- Normalized left/right Shift, Ctrl and Alt on the Raw Input visualization path.
- Rebuilt the keyboard on a shared quarter-key grid. Backspace/Backslash/Enter/Right Shift/Right Ctrl now share one right edge; Ins/Home/PgUp and Del/End/PgDn form a strict 3×2 block; the arrow keys use a true inverted-T layout.
- Removed the 180 ms release afterglow. Keyboard visualization now represents exact current state: key-down highlights, key-up clears immediately.
- Generalized automation-mode containment: all injected keyboard and mouse events observed by Input Lab's low-level hooks are swallowed after observation, instead of special-casing only `K` and mouse `X2`.
- Kept `package.ps1` limited to the self-contained window-message and keyboard-visual tests; packaging does not run the real-output ownership acceptance host.
- Added an explicit `-AllowRealOutput` opt-in gate to `run-acceptance.ps1` so the real-output black-box test cannot be launched accidentally.
- Hardened InputStitch Emergency Stop so a failure in the optional virtual-gamepad neutralization step is logged but cannot prevent the final hook-state reconciliation and status update.

## Safety boundary

`run-acceptance.ps1` remains an explicit developer black-box acceptance test. It intentionally exercises real `SendInput` and ViGEm/XInput output paths and now refuses to start unless `-AllowRealOutput` is supplied. Use that opt-in only while the desktop is idle and no other InputStitch instance is active.

The repeating-keystroke incident that prompted the initial safety review was later traced to a macro configured in the mouse driver rather than Input Lab. The containment changes remain in v0.3.1 because automated tests should not be able to leak injected desktop input even when an unrelated device or driver behaves unexpectedly.
