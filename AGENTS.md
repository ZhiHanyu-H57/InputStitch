# AGENTS.md

## Startup checklist

Before changing code in this repository, restore project context in this order:

1. Read `AGENTS.md`.
2. Read `PLAN.md`.
3. Read `ROADMAP.md`.
4. Read `HANDOFF.md`.
5. Run `git log -8 --oneline --decorate`.
6. Run `git status --short --branch`.
7. Confirm the local branch is based on the expected remote `main` before editing.

`HANDOFF.md` is the machine-to-machine continuation note. It describes the last safe development breakpoint and should be updated before handing development to another computer or agent.

## Development rules

- Preserve existing behavior unless a change is explicitly required.
- Reliability and safe input release take priority over feature count.
- Do not weaken Emergency Stop, fail-closed cleanup, UI editing safety, or ownership isolation.
- Do not regress existing Held Mapping behavior while changing concurrency or Layer logic.
- Preserve configuration/import compatibility unless a migration is deliberately designed and tested.
- Do not enable arbitrary parallel timed macros merely because the ownership core can merge outputs; the current concurrency boundary is intentional.
- Treat Stable and Beta release channels as separate. Beta must not replace `releases/latest` or `InputStitch-update.xml`.
- Beta and Stable currently share `%APPDATA%\InputStitch`; do not design workflows that assume they can safely run simultaneously.
- Do not commit build artifacts, temporary verification directories, or machine-specific paths.

## Verification before commit

For runtime or UI changes, run the full regression suite:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Run-Tests.ps1
```

For ownership, keyboard/mouse output, Held Mapping concurrency, Emergency Stop, or virtual Xbox behavior, also run the Input Lab black-box acceptance when ViGEmBus is available:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1
```

The Input Lab acceptance host uses isolated configuration and must not modify the user's normal `%APPDATA%\InputStitch\config.xml`.
Treat `SUMMARY: BLOCKED` / exit code 2 as an environment-preflight result (for example ViGEm/XInput report propagation unavailable), not as an InputStitch assertion failure. Do not bypass the preflight merely to force a green report.

For release-affecting changes, also build and run Release Verification using the repository scripts. Never publish a release only because compilation succeeded.

## Handoff rule

Before switching development machines:

1. Reach a safe breakpoint.
2. Run the relevant regression tests.
3. Commit and push completed code changes.
4. Update `HANDOFF.md` with the exact current state, remaining work, known issues, constraints, and last verified code commit.
5. Commit and push the handoff documentation.
6. Confirm the working tree is clean and `main` is synchronized with the remote.
