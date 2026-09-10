# Input Lab v0.3.1

Input Lab v0.3.1 is a safety hotfix for automated verification on an interactive Windows desktop.

## What changed

- Removed the system-wide `keybd_event` call from the keyboard-visual self-test. The self-test now drives the viewer's observation/highlight state in-process and emits no real keyboard input.
- Generalized automation-mode containment: all injected keyboard and mouse events observed by Input Lab's low-level hooks are swallowed after observation, instead of special-casing only `K` and mouse `X2`.
- Kept `package.ps1` limited to the self-contained window-message and keyboard-visual tests; packaging does not run the real-output ownership acceptance host.
- Added an explicit `-AllowRealOutput` opt-in gate to `run-acceptance.ps1` so the real-output black-box test cannot be launched accidentally.
- Hardened InputStitch Emergency Stop so a failure in the optional virtual-gamepad neutralization step is logged but cannot prevent the final hook-state reconciliation and status update.

## Safety boundary

`run-acceptance.ps1` remains an explicit developer black-box acceptance test. It intentionally exercises real `SendInput` and ViGEm/XInput output paths and now refuses to start unless `-AllowRealOutput` is supplied. Use that opt-in only while the desktop is idle and no other InputStitch instance is active.

The interactive-session incident that prompted this hotfix did not yield enough evidence to attribute every observed keystroke to one exact code path. v0.3.1 therefore treats automated desktop-input containment as a general invariant rather than claiming a single proven root cause.
