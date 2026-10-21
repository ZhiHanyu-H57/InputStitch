# Product refresh — 2026-10-20

## Current pass — compact responsive settings (beta.10, 2026-10-21)

Merge shortcut display and selection into one control, keeping the physical
capture dropdown. Fit trigger-mode fields to the longest translated choice;
keep count, unit and infinite repeat adjacent. Narrow windows wrap whole
label/field groups. The finite count is preserved while disabled. Readiness and
explanations are directly visible in the execution area. Keep 10.5pt text and
the compressed runtime packaging; no new features or dependencies.

Actual native control creation can change combo-box height, and nested auto-size
tables retain stale preferred heights after wrapping. Use font-aware field
heights and a small width-driven group row with deferred containing-table
layout. Acceptance checks actual visible bounds after layout settles, repeated
resize/language transitions, complete modifier chords and empty prompts.

Acceptance: all 31 suites PASS, 5,377 product assertions, 172 crosshair
checks, 77/77 real output. Focused density/live transitions: 3,316 synthetic
100/125% checks and 1,660 native 144 DPI checks. Full bilingual layout
also covers synthetic 150/200%; oversized synthetic windows can be clamped by
the host, so receipts record actual dimensions. At 1160x780/100%, both languages
show 9 complete step rows, with ordinary editing free of outer/horizontal
scrolling. At 1000x700, Chinese shows 6 and English 5; at 1280x900 both show 13.

Current-source macro observer: 300.1 seconds / 3,565 cycles,
keyboard down/up 13,689/13,689, zero residual sources/held outputs.
Production-source and user-config hashes remain unchanged throughout acceptance.
The unchanged renderer retains its explicitly historical beta.8 sustained
baseline, with current-version pixel checks. Final package sizes/startup and
source hashes are in verification.json. User trials remain optional feedback.

## Historical pass — balanced density and footprint (beta.9, 2026-10-21)

The user found the 12pt pass too large and cramped, particularly the Actions
list, and explicitly prioritizes readable, usable, clean overview before art.
Beta.9 uses 10.5pt body text, compact shared sections/dialog headers and a
combined ordinary settings block. The step list uses the remaining viewport.
Grid rows fit their font/padding, headers fit measured text, and all toolbar
actions are checked against every ancestor's client area. Native 144 DPI also
sizes the sidebar, toolbars, status area and macro rows to their text.

Quick-create Actions uses separated font-aware rows and the available vertical
space, with hold/wait timing visible inline. Initial main-window bounds fit the
current working area. Default settings remain directly editable. New runtime
features remain paused; windowed/borderless crosshair scope is retained.

The main binary had included an uncompressed 2,844,160-byte x64 SDL3 runtime.
Gzip embedding reduces it to 1,162,052 bytes; the 290,304-byte managed ViGEm
client becomes 143,531 bytes. Both use .NET Framework compression, with no new
library dependency or functionality removal. Native extraction retains the
pinned SHA-256. Packaged tests cover compressed managed loading, isolated native
cold extraction, verified target reuse, corrupt-target repair and actual SDL3
initialization. Extracted runtime size is unchanged; report EXE/download savings
separately from runtime-cache disk usage.

Current acceptance: 31 suites PASS, 1,315 workflow/layout checks, 172 crosshair
checks and 77 real-output assertions. Additional focused density checks cover
all toolbar actions (1,108 checks at 100/125% synthetic scale, 556 at native
144 DPI including actual initial-window fit). At 100%, 1000x700 / 1160x780 /
1280x900 expose 5 / 7 / 11 full step rows without outer/horizontal scrolling.
Native 150% viewports of 1500x1050 / 1740x1170 / 1920x1350 expose 9 / 12 / 16.
The existing broader dialog/layout matrix still includes synthetic 150/200%;
focused row-capacity claims apply only to the measured viewports above.

Final-build macro sustained observer: 300.1 seconds / 3,421 cycles, keyboard
down/up 12,948/12,948, zero final sources/held outputs, max UI dispatch 115 ms,
max continuous-output gap 59 ms. Private bytes 35,667,968 to 36,499,456; handles
436 to 430, threads 13 to 6. All 54 production sources remained frozen during
final acceptance and the normal user configuration hashes were unchanged.
The renderer sources match beta.8 byte for byte, so its five-minute native
overlay run remains an explicitly historical baseline; beta.9 reruns the actual
windowed/borderless pixel checks. Final EXE sizes, source hashes and both
architecture startup/extraction receipts are in the delivered verification.json.

## Historical fourth pass — larger text and readability

Local target: `1.5.0-beta.5`, FileVersion `1.5.0.4`.

- Shared body, form, button and helper text: 10pt to 12pt; virtual-keyboard
  labels: 8pt to 10pt. Headings retain their visual hierarchy.
- Increase button minimum height, macro/table row heights, sidebar width,
  field-label widths, persistent status height and dialog heading space.
- Give key selection, quick creation and step editing more default room.
  Smaller windows retain scrolling and reachable confirmation actions.
- Controller preview captions use the inherited interface font rather than
  shrinking with the drawing. Diagnostic monospace text grows from 9pt to 11pt.
- Preserve execution, output ownership, XML and existing macro behavior.
  New feature work remains paused; automated acceptance determines completion.

### Fourth-pass verification

- Verified production-code commit: `e24cc39` on `ui/product-refresh`.
- Local beta.5 / FileVersion 1.5.0.4; published Stable 1.4.3 is unchanged.
- **30 regression suites PASS**, including **903 workflow/interaction/layout
  checks** and 303,716 output-ownership assertions. Bilingual compact Settings
  smoke, old XML defaults and stored gamepad vectors PASS.
- Chinese/English layout checks cover main-window compact/default sizes and
  synthetic 100/125/150/200% scales. Creation cards, step explanations, Settings,
  timing and keyboard confirmation actions remain usable with the larger text.
- Actual nested new-user creation, key selection, editing, automatic saving and
  first Run complete in both languages with an isolated output observer.
- Real-output Input Lab **77/77 PASS, failures=0**, using SendInput and
  ViGEm/XInput. No new physical-device/game compatibility claim is made.
- x64/x86 build and Release Verification PASS; packaged startup, live message
  loop and orderly close PASS using isolated configuration.
- Current-build sustained observer: **300.1 seconds / 3,444 cycles PASS**;
  keyboard downs/ups 13,300/13,300; final sources and pressed outputs zero.
  Max UI dispatch 76 ms; maximum continuous-output gap 56 ms.
  Private bytes 36106240 to 37347328; handles
  499 to 494; threads 15 to 8.
- All 50 production sources unchanged during final acceptance. Normal user
  config.xml and devices.xml hashes unchanged. No user trial-run prerequisite.
- The beta.2 1800.1-second / 21,023-cycle report remains historical; the above
  five-minute sustained regression was measured on this beta.5 code.

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
