# Product refresh — 2026-10-20

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
