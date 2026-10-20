# Product refresh — 2026-10-20

## Third pass — remaining dialogs and first-run workflow

Local target: `1.5.0-beta.4`, FileVersion `1.5.0.3`.

- Shared dialog frame, heading, colors, tab styling and persistent footer across
  Settings, timing/conditions, keyboard selection, export, batch waits, device
  tools, controller options, diagnostics, About and group naming.
- Timing shows only fields used by the selected trigger. Inactive saved values
  remain intact. Optional analog limits expand when enabled; condition fields
  include examples and explain whether any limits are set.
- Keyboard selection is the primary action in the main editor, quick creation
  and step editor. Physical/mouse capture stays available in the dropdown.
  Empty/invalid chords are explained immediately and cannot be confirmed.
- Settings keep changes pending until Save; Cancel preserves the configuration.
  Restore defaults now includes the optional controller input setting.
- Export selection and batch waits give immediate readiness/invalid-range feedback.
- Main-window guidance explains the next action, automatic saving and Run/Stop all.
  Group-name editing retains modal output protection.
- Product acceptance drives the actual nested chooser, quick-create, keyboard,
  step editor and first Run button in Chinese and English, with isolated files
  and an output observer. No user trial-run gate and no new runtime features.

### Third-pass verification

- Verified production-code commit: `b06b0f2` on `ui/product-refresh`.
- Local package: `1.5.0-beta.4`, FileVersion `1.5.0.3`; no remote release and
  no replacement of the user-confirmed Stable 1.4.3 baseline.
- Complete regression: **30 suites PASS**, including **903 product workflow,
  interaction and layout checks**, 303,716 output-ownership checks, bilingual
  Settings smoke, old XML defaults and stored gamepad vector initialization.
- Actual first-user nested windows complete in Chinese and English: creation
  card, quick creation, shortcut keyboard, step editor, action keyboard, save,
  main Run button, completion and zero pressed keys. No manual trial gate.
- Settings, timing and keyboard dialogs checked at compact sizes and synthetic
  100/125/150/200% scales. Other secondary dialogs have bilingual default-layout
  and reachable-footer checks; group naming has actual modal protection checks.
- Real-output Input Lab: **77/77 PASS, failures=0**; actual SendInput and
  ViGEm/XInput reports. Physical-device hiding/takeover was not exercised.
- x64/x86 builds and Release Verification PASS; final executable normal startup,
  live message loop and orderly shutdown PASS in isolated directories.
- Current-build sustained observer: **300.1 seconds / 3,436 cycles PASS**;
  keyboard down/up 13,253/13,253; final output sources and pressed outputs zero.
  Max UI dispatch 73 ms; continuous-output gap 80 ms;
  private bytes 36241408 to 36491264; handles
  498 to 494; threads 15 to 8.
- Historical beta.2 1800.1-second / 21,023-cycle evidence stays historical. The
  executor, output ownership and XML format were not changed in this UI pass.
- Production sources remained unchanged during final acceptance; normal
  config.xml/devices.xml hashes unchanged. New feature work remains paused.

## Second pass — visual design and friendly interaction

Local version: `1.5.0-beta.3` / FileVersion `1.5.0.2`.
The user specifically requested further visual and interaction work after beta.2.

- New macro is the sidebar's primary action. Four keyboard-accessible purpose
  cards explain repeat, sequence, held controller input and blank starting points.
- Import/share and whole-profile backup/restore stay available in one menu;
  it describes appending macros versus replacing the full setup.
- A stitched vector mark, rounded surfaces, numbered sections, larger typography,
  consistent button states and calmer table styling establish a shared appearance.
- Empty steps offer a direct first action. Existing steps show the count and
  explain double-click editing.
- The step editor previews the selected keyboard/mouse input and explains the
  action, press duration and fixed/random wait in ordinary language as fields change.
  Controller inputs retain their existing diagram. The dialog explains milliseconds.
- Quick create keeps next-step guidance and Create/Cancel visible in the footer;
  Create becomes available once the trigger and action are present.
- The executor, output ownership, trigger policies and XML schema are unchanged.
  Feature expansion remains paused. No user trial-run gate is introduced.

### Second-pass verification

- Verified production-code commit: `d14c84f` on `ui/product-refresh`.
- Local version `1.5.0-beta.3`, FileVersion `1.5.0.2`; x64/x86 builds and
  Release Verification PASS. No remote release or Stable 1.4.3 replacement.
- Complete regression: **30 suites PASS**, including **501 workflow/interaction/
  layout checks**, 303,716 ownership checks and bilingual Settings/old XML smoke.
- Real-output Input Lab: **77/77 PASS, failures=0**; actual SendInput and
  ViGEm/XInput observations on the final production sources.
- Final-package x64/x86 normal startup, live message loop and orderly shutdown
  PASS in isolated directories, with macros disabled.
- Current-build sustained observer: **300.1 seconds / 3,509 cycles PASS**;
  keyboard down/up 13,250/13,250; final sources and pressed outputs zero.
  Max UI dispatch 53 ms, continuous-output gap 56 ms;
  private bytes 35758080 to 37814272, handles
  474 to 465, threads 13 to 6.
- Beta.2's 1800.1-second / 21,023-cycle run remains a historical runtime baseline.
  It is not relabeled as a new beta.3 run. The executor/ownership/XML schema did
  not change in this visual pass. Report actual duration and backend.
- Normal config.xml/devices.xml hashes unchanged. No manual user trial is
  required for completion. Feature expansion remains paused.

## Authorized scope and stable baseline

The user confirms that Stable 1.4.3 has been used for a long period, recommended to
other users, and has no known problems in its official features. Preserve that
behavior. Existing experimental controller takeover has its own compatibility
scope; it does not block the ordinary macro product.

Pause new features. Deliver a coherent Windows editor, plain Chinese/English
copy, one trigger-mode selector, compatible saved configurations, automated
workflow and sustained-run acceptance, and runnable x64/x86 artifacts.

## Implementation order

1. Unify trigger-mode selection without changing the executor, timing, ownership,
   stop semantics, or XML compatibility. Keep conditions and timing as optional
   details; do not expose a legacy mode or competing settings.
2. Apply a shared restrained visual style to the editor and editing dialogs.
   Make the macro list, trigger setup, steps, and run/stop state easy to scan.
   Put grouping/target restrictions under optional settings.
3. Test the real editor's create/edit/save/reload/package/run/stop flow in an
   isolated configuration, test old configuration presentation and all modes,
   and check layout at multiple sizes/scales in Chinese and English.
4. Add repeatable sustained-run acceptance with wall-clock duration, cycle count,
   resource samples, output cleanup, responsiveness, and a machine-readable
   report. Run the complete regression, real-output Input Lab, and release
   verification. Package results and screenshots with exact hashes.

## Acceptance policy

Routine product acceptance is automated. User playtesting is optional feedback,
never a per-iteration completion gate. Tests must cover the supported editing and
runtime contract, not preserve historical UI controls or developer explanations.
Specific physical-device/driver/game compatibility claims need evidence from that
environment; those claims are separate from routine macro acceptance. Do not
claim an unobserved duration or device test as passed.

## Progress

- [x] Inspect clean local/remote baseline and preserve normal config hashes.
- [x] Unified editor and copy.
- [x] Workflow/layout and sustained-run automation.
- [x] Regression, real output, release checks, and deliverable package.

## Verification outcome

- Verified production-code commit: `f6dfab9` on `ui/product-refresh`.
- Local runnable package: `1.5.0-beta.2`, FileVersion `1.5.0.1`; no remote
  Release publication and no replacement of Stable 1.4.3.
- Complete regression: **30 suites PASS**, including **389 workflow/layout
  checks**, **303,716 ownership checks**, bilingual Settings and old XML smoke.
- Real-output Input Lab: **77/77 PASS, failures=0** (actual SendInput and
  ViGEm/XInput reports).
- Final x64/x86 executable startup/message-loop/shutdown: **PASS** in isolated
  directories; macros disabled, normal user configuration untouched.
- Sustained runtime: **1800.1 seconds / 21,023 cycles PASS**,
  80,226 keyboard downs and matching ups; zero final sources
  and pressed outputs. Backend: isolated observer; real output boundary verified
  separately by Input Lab.
- Private bytes: 35606528 → 36544512;
  handles 435 → 426;
  threads 13 → 4;
  max UI dispatch 51 ms; max continuous-output gap 93 ms.
- Normal config.xml and devices.xml hashes remained unchanged.
- No in-scope acceptance blocker and no required user trial run remain.
  New feature work stays paused until a new user instruction.
- The sustained host supports longer runs through `-SoakMinutes` (up to 1440).
  Report actual measured duration. The new manual GitHub workflow has not been
  run remotely in this local task; do not claim remote CI evidence.
