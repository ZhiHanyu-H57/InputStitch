# Screen crosshair — 2026-10-21

Local target: 1.5.0-beta.6, FileVersion 1.5.0.5. The user's explicit request
authorizes this feature; unrelated feature expansion remains paused.

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

The native target uses an isolated white window and a PerMonitorV2-aware test
process. It verifies window flags, native hit-test pass-through, unchanged
foreground, full-display center, exact pixel offsets, actual desktop colors and
50% alpha compositing, missing-display fallback and hide/disable. Desktop
presentation is asynchronous, so captures wait for presentation before sampling.
The sustained case repeatedly presents/hides all eight styles and changes
opacity on a real layered window at the screen edge, then checks GDI/USER/
process-handle/private-memory growth and disposal. Ten-second samples retain
the initialization phase; resource comparisons use a 30-second warmup baseline,
as with the macro sustained test. No forced garbage collection is used. It
reports measured duration and iterations, and retains failed-run metrics.

Final verification in progress. Routine completion has no user trial-run gate.
