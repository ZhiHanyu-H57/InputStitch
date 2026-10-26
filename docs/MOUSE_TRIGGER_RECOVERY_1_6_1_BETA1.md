# Mouse and Shift-trigger safety — 1.6.1-beta.1

## Confirmed incident

An enabled Toggle macro used an unmodified left mouse button with suppression
enabled. Its ordinary clicks were consumed globally. Emergency Stop stopped
the current run but did not suspend the trigger, so another click could restart
the macro. Optional editor protection did not cover all application controls
and non-client areas such as the close button.

The affected user recovered control after the product process was stopped.
The original configuration was backed up, then exactly that macro's Enabled
flag was changed to false. An XML round-trip comparison confirmed all other
values remained unchanged. The existing desktop executable was not replaced.

## Local correction

- Physical mouse edges identify their native target. InputStitch controls,
  menus and caption/close buttons retain clicks regardless of the optional
  editor pause setting. Native click-through crosshair/timer overlays are
  excluded from that classification.
- Explicit capture and recording retain priority. External mouse triggers,
  suppression and release handling remain available.
- The emergency chord immediately latches the existing global-trigger pause,
  before queued UI cleanup. Tray Emergency Stop also latches it. The gear menu
  shows the paused state and lets the user resume explicitly.
- Ordinary Stop all retains its existing ability to run another macro later.
- No new polling loop, dependency, configuration format or macro runtime mode.

## Shift with navigation and other ordinary trigger keys

The prior Shift fallback whitelist admitted letters/digits/Space but excluded
PageDown, PageUp, arrows, editing/navigation and punctuation keys. The shared
matching policy now admits an extra Shift for every bare non-modifier key.
Explicit chords still win before fallback. Existing Ctrl/Alt/Win restrictions,
standalone modifier identity, required modifiers and terminal release rules
remain unchanged. The same policy serves macro and layer matching.

Regression cases cover navigation/editing/punctuation families, Ctrl/Alt/Win
non-expansion, Shift-release versus terminal-release lifecycle, exact chord
priority, disabled-chord fallback and an actual Shift+PageDown admitted run
with balanced observer output. Existing advanced timing/stop tests remain valid.

## Targeted verification

UI safety includes the accidental-left-button scenario, native client/caption
hit testing, click-through exclusion, cancelled queued starts, pause visibility,
explicit resume, ordinary stop and balanced output through an isolated observer.
Mouse recording/editing, input integration, advanced trigger dispatch, modifier
priority, source manifest and release policy also pass.

Current counts: UI safety58, recording/editing209, input integration24, advanced
dispatch163, modifier policy185 and source manifest424, plus release-policy guards.

Input Lab has a focused `-EmergencyStopOnly` route. In a private desktop it
observes real ViGEm/XInput output before/after panic, prevents immediate restart,
resumes through the existing menu and checks final neutral state. It sends no
keyboard or mouse input in that route. All 11 checks pass.

Both packaged architectures pass startup/live loop/configuration/cache/shutdown
smoke checks on private desktops. No desktop switch, user-input injection,
full-suite run or long soak is needed for this correction. This is a local beta;
published Stable1.6.0 and its website/downloads remain unchanged.
