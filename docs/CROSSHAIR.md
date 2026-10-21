# Screen crosshair — 2026-10-21

Local target: 1.5.0-beta.8, FileVersion 1.5.0.7. The user's explicit request
authorizes this feature; unrelated feature expansion remains paused.

## Desktop-only delivery scope (beta.8)

The user requested closure on windowed and borderless mode. This build restores
the beta.6 desktop renderer with no Game Bar backend, widget installation, package
dependency or fullscreen settings tab. All eight styles, editable appearance,
saved settings, live preview, physical offsets and visibility shortcut remain.
The UI explicitly directs games to windowed or borderless mode.

The beta.7 exclusive-fullscreen experiment was not accepted: the user saw no
crosshair. PNG transport and internal widget rendering were diagnostic evidence
only and did not establish physical visibility. Its source and failure evidence
were archived outside the shipping repository, and the local experimental widget
was uninstalled. Exclusive fullscreen is deferred and unsupported in beta.8.

## Product scope

- Eight vector styles: dot, square, cross, cross + dot, ring, ring + dot,
  T crosshair and diagonal cross.
- Size 1–200 px, line width 1–30 px, center gap 0–100 px, central dot 1–50 px,
  outline on/off and 1–12 px, two RGB colors, opacity 0–100%.
- Full-display center by default (not the work area). X/Y offsets have one-pixel
  increments; positive moves right/down. Positions beyond the display clamp
  to the nearest screen edge. UI DPI does not scale drawing sizes or offsets.
- Primary display automatic selection or saved monitor device name. A missing
  monitor falls back to primary without losing the original saved choice.
- Panel preview with light/dark background, optional live screen preview,
  center reset, Save and Cancel. Cancel restores the actual overlay and config.
- Off by default. Enabled state, show/hide state and all drawing/position/hotkey
  options persist in the normal atomic XML config. Old files default to off.
  Macro profile loading retains the crosshair as a global preference.
- Default Ctrl + Alt + F10 show/hide; editable through the existing key picker
  or clearable. Physical keyboard edges only; injected output and auto-repeat
  do not toggle it. Emergency Stop, capture/recording and editing protection
  retain priority. Conflicting macro/layer/panic chords are explained inline;
  a subsequently introduced conflict keeps the existing macro behavior.
- One sidebar entry; retained 12pt interface text. No new runtime macro mode.

## Research and implementation decisions

Reviewed primary product documentation on 2026-10-21:

1. [CrossOver](https://github.com/lacymorrow/crossover) documents customizable
   size, color, opacity, center reticles, pixel nudges, monitor selection,
   persistence and shortcut visibility. It also describes image libraries,
   duplication and ADS behavior. We adopted the relevant core controls and
   pixel/monitor/reset/preview convenience; image libraries, multiple overlays
   and ADS automation are outside this request's implementation.
2. [Crosshair X centering](https://crosshairx.gg/how-to-center-crosshair.html)
   explicitly uses 0,0 offsets for exact screen centering. We use full display
   bounds with explicit signed pixel offsets and a reset action.
3. [HudSight Simple Overlay](https://hudsight.com/support/simple-overlay-crosshair-overlay/)
   explains display selection and differences between windowed/borderless and
   particular fullscreen configurations. [HudSight](https://hudsight.com/)
   also offers other rendering modes. This feature uses a desktop overlay and
   makes no universal exclusive-fullscreen or anti-cheat compatibility claim.

The implementation draws our own vectors; no external crosshair artwork/code
was imported. A small native layered window uses premultiplied BGRA pixels and
UpdateLayeredWindow. WS_EX_LAYERED + TRANSPARENT + TOOLWINDOW + NOACTIVATE keeps
it click-through, out of task switching and non-activating. No game injection,
driver, Game Bar extension or continuously repainting timer is introduced.
Display changes and foreground changes refresh placement/topmost as needed;
closing the application destroys the window and unsubscribes display events.

Native references: [layered window behavior](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features),
[extended styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles),
[UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow).

## Automated acceptance

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Run-CrosshairAcceptance.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Run-CrosshairAcceptance.ps1 -SoakMinutes 5
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Run-Tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tests/Run-ProductAcceptance.ps1 -SoakMinutes 5
```

Crosshair tests include old config/defaults, full XML round trips including a
cleared shortcut, bounded imported settings, each style's pixels/outline/center,
extreme sizes, negative monitor origins, bilingual compact/scaled dialogs,
actual main-entry Save/Cancel, screen-preview rollback, saved visibility hotkeys,
profile globals, conflict feedback and Emergency Stop priority.

The native targets use an isolated white window, a full-display borderless
window and a PerMonitorV2-aware test process. Each verifies window flags, native hit-test pass-through, unchanged
foreground, full-display center, exact pixel offsets, actual desktop colors and
50% alpha compositing, missing-display fallback and hide/disable. Desktop
presentation is asynchronous, so captures wait for presentation before sampling.
The sustained case repeatedly presents/hides all eight styles and changes
opacity on a real layered window at the screen edge, then checks GDI/USER/
process-handle/private-memory growth and disposal. Ten-second samples retain
the initialization phase; resource comparisons use a 30-second warmup baseline,
as with the macro sustained test. No forced garbage collection is used. It
reports measured duration and iterations, and retains failed-run metrics.

## Beta.8 closure verification

- 31 suites PASS: 903 macro workflow/layout checks, 172 crosshair checks,
  303,716 ownership assertions. Windowed and full-display borderless targets
  both pass actual desktop red/50%-alpha samples, hit-test pass-through,
  no focus theft, centering, physical offsets, hiding and disabling.
- Real SendInput and ViGEm/XInput: 77/77 PASS, zero failures.
- Current-build macro sustained observer: 300.0 seconds / 3,533 cycles;
  keyboard down/up 14,239/14,239, final sources/held outputs zero,
  max UI dispatch 131 ms, max continuous-output gap 159 ms.
- Current-build native overlay: 300.1 seconds / 9,880 changes;
  GDI 15 to 15, handles 287 to 284, USER 14 to 13,
  private bytes 29,044,736 to 30,461,952; max dispatch 133 ms.
  Resource baseline follows 30 seconds of warmup, retains startup samples,
  uses no forced GC and verifies native-window disposal.
- The 31-suite run passed, then packaged Settings smoke initially lacked the
  new beta.8 EXE. Building it and rerunning Settings smoke passed all bilingual
  compact layouts, XML defaults and saved gamepad-vector checks. The original
  failure and the focused retry remain in the delivery evidence.
- All 54 production sources stayed unchanged during acceptance; normal user
  configuration hashes match the snapshot taken before the fullscreen attempt.
- Final binaries, startup/restoration/shutdown checks and hashes are recorded
  in the delivered machine-readable verification report. No user trial gate.
- These measured five-minute runs and this host's native targets do not claim
  hours-long endurance, universal game support or physical multi-monitor coverage.

## Historical beta.6 verification

- Verified production-code commit: `ceb1320` on `ui/product-refresh`.
- Local `1.5.0-beta.6` / FileVersion `1.5.0.5`; no remote publication and no
  change to the published Stable 1.4.3 release.
- Full regression **31 suites PASS**: 903 existing macro workflow/layout checks,
  **162 crosshair checks**, 303,716 output-ownership assertions, bilingual
  Settings smoke, legacy config defaults and saved controller vectors.
- Native PerMonitorV2 target tests PASS: actual fill/50% alpha desktop sampling,
  hit-test click-through, no foreground activation, full-display centering and
  +13/-11 physical-pixel offsets. Native display presentation is awaited before
  capture. Negative monitor origins and edge clamps have deterministic checks.
- Actual main-entry Save/Cancel, screen preview rollback, saved visibility,
  cleared shortcuts, conflict feedback, editing protection, Emergency Stop
  priority and preserved global settings on macro-profile loading PASS.
- Real-output Input Lab **77/77 PASS, failures=0**, actual SendInput and
  ViGEm/XInput. No new physical-controller or game compatibility claim.
- Macro sustained observer **300.1 seconds / 3,400 cycles PASS**;
  keyboard down/up 13,092/13,092; final sources/pressed outputs zero;
  maximum UI dispatch 120 ms, continuous-output gap 99 ms.
- Actual native crosshair sustained run **300.1 seconds / 9,114 iterations PASS**;
  all styles, visibility and opacity changes at the screen edge. GDI objects
  15 to 15; handles 347 to 345;
  private bytes 29708288 to 30834688; max dispatch 12 ms.
  These resource comparisons use a 30-second warmup; ten-second samples include
  initialization. USER objects 34 to 32; native window
  destroyed at completion, with no forced garbage collection.
- x64/x86 build and Release Verification PASS; normal constructor/startup with
  saved crosshair enabled and X/Y offsets restores the window; orderly shutdown
  destroys it. Tests use isolated app directories and disabled macros.
- All 54 production sources unchanged during final acceptance; normal user
  config.xml/devices.xml hashes unchanged. No manual trial-run gate.
- Exclusive fullscreen remains outside the universal compatibility claim.
  Real additional-monitor hardware was not required to establish the ordinary
  feature behavior; simulated monitor geometry is reported as simulation.
