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
- [ ] Unified editor and copy.
- [ ] Workflow/layout and sustained-run automation.
- [ ] Regression, real output, release checks, and deliverable package.
