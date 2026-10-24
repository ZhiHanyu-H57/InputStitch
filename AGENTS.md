# AGENTS.md

## Startup checklist

Before changing code in this repository, restore project context in this order:

1. Read `AGENTS.md`.
2. Read `PLAN.md`.
3. Read `ROADMAP.md`.
4. Read `docs/PRODUCT_STRATEGY.md` for current product positioning and priority rationale.
5. Read `HANDOFF.md`.
6. Run `git log -8 --oneline --decorate`.
7. Run `git status --short --branch`.
8. Confirm the local branch is based on the expected remote `main` before editing.

`HANDOFF.md` is the machine-to-machine continuation note. It describes the last safe development breakpoint and should be updated before handing development to another computer or agent.

## Development rules

- Local 1.6.0-beta.2 completed actual OBS Game Capture and isolated whole-guest
  IPv4/IPv6 recovery acceptance. See `docs/OVERLAY_TOOLS_1_6_0_BETA2.md` and the
  two dedicated acceptance guides. EntireComputer tests must stay inside the
  identity-gated Windows guest; never run them on this host. TCG fixture
  precompilation is guest-only and does not change production helper deadlines.

- The user authorized optional crosshair/timer/network tools on 2026-10-23.
  Local 1.6.0-beta.1 is separate from published Stable 1.5.2. Keep tools opt-in;
  timer starts paused, and application startup must never block the network.
  Only the disposable WFP helper elevates. Native timer/RTSS and executable-only
  network acceptance commands and environment limits are documented in
  `docs/OVERLAY_TOOLS_1_6_0_BETA1.md`; do not replace them with manual-trial gates.

- Production source files are listed once in `scripts/production-sources.txt`; add or rename files there.
  `build.ps1`, `tests/Run-Tests.ps1` and `tools/InputLab/build-acceptance.ps1` consume that manifest, and
  `tests/SourceManifestTests.cs` fails when the `InputStitch.csproj` list or a root-level `.cs` file diverges.
- The optional SDL3 input lane needs the native runtime, which is **not committed**. Run
  `scripts/Restore-Sdl3.ps1` to fetch the pinned 3.4.16 assets into the gitignored `third-party/SDL3/<arch>/`
  folder; `tests/Sdl3ProviderTests.cs` then runs the real-runtime smoke automatically (CI skips it).
- Preserve existing behavior unless a change is explicitly required.
- Reliability and safe input release take priority over feature count.
- Do not weaken Emergency Stop, fail-closed cleanup, UI editing safety, or ownership isolation.
- Do not regress existing Held Mapping behavior while changing concurrency or Layer logic.
- Preserve configuration/import compatibility unless a migration is deliberately designed and tested.
- Concurrent ordinary timed macros are now an explicit 1.3.0 priority. Implement them through a dedicated multi-run runtime design with independent RunId/SourceId/timing/stop state and deterministic overlap semantics; do not just start extra threads around the current singleton worker fields.
- Runtime category/concurrency eligibility must have one authoritative classifier shared by execution, diagnostics and UI. Do not duplicate eligibility rules in the editor; option-B live feedback must be a view of the Concurrent Macro Runtime capability model.
- Treat Stable and Beta release channels as separate. Beta must not replace `releases/latest` or `InputStitch-update.xml`.
- Input Lab is independently versioned. Its release tags use `inputlab-v*` and its GitHub releases must remain prereleases so the developer tool can never replace the Stable InputStitch `releases/latest` endpoint.
- Beta and Stable currently share `%APPDATA%\InputStitch`; do not design workflows that assume they can safely run simultaneously.
- Do not commit build artifacts, temporary verification directories, or machine-specific paths.

## Verification before commit

### Desktop use during UI verification

Layout, editor journey and dialog tests use `IsolatedUiDesktop`: a whole child
process starts on a private Windows desktop without activating it. Keep framework
helper threads on that same desktop; moving only the test STA can hang WinForms
cleanup. The parent checks foreground ownership and releases the desktop after
the child exits. Do not weaken these checks or switch the desktop to get a pass.

Run PowerShell UI smoke checks through `tests/Run-IsolatedUiScript.ps1`, including
`SettingsSmoke.ps1` and architecture-matched `Run-PackagedSmoke.ps1`. Native DPI
layout tests can run privately. Actual desktop composition/capture and real-input
acceptance retain their explicitly authorized dedicated environment requirements.

Announce before starting Computer Use and when it stops. Close test inspectors
and stop desktop interaction as soon as the inspection finishes; do not leave it
active until the conversation ends. Prefer private UI tests while the user is
working. ToolIntegration interactive inspectors require `--allow-host-desktop`.

Use impact-based verification in development AND formal releases (user instruction,
2026-10-24). This supersedes older unconditional-full-suite/soak release rules.
Select the suites and focused workflows affected by the actual change. List the
selection and results in the handoff. Do not rerun unrelated layout, provider or
update suites for every change. Examples:

```powershell
./tests/Run-Tests.ps1 -List
./tests/Run-Tests.ps1 -Suite MacroRunCoordinatorTests,InputPipelineIntegrationTests,UiSafetyTests,OutputOwnershipTests
./tests/Run-SecondaryLayout.ps1 -ScaledFonts -Focus QuickCreateDialog
```

UI-only edits need the affected bilingual/DPI layouts and workflow; runtime
coordination edits need run/stop, ownership, UI safety and sustained acceptance.
Serialization changes need old-configuration save/reopen coverage. Add source
manifest checks when files change; verify metadata/startup for packaged builds.
Broaden only when dependencies or failures establish a reason. Targeted runs do
not imply a full-suite pass. Tests use the same assertions in either mode.

Development defaults select from the branch/working-tree changes; use -BaseRef
when the boundary is known. Formal releases use the cumulative diff from the last
published Stable tag, not merely the last commit. The selector prints the plan,
deduplicates checks, and reuses PASS evidence only when its declared source,
assertion, helper and dependency inputs match and its evidence hashes are intact.
Unknown paths require classification; they do not silently skip or trigger all tests.

```powershell
./tests/Run-Tests.ps1 -BaseRef HEAD^ -PlanOnly
./tests/Run-Verification.ps1 -Mode Release -BaseRef <previous-published-stable-tag>
```

Full regression is an explicit diagnostic option (`Run-Tests.ps1 -Full`), never
the default or an unconditional publication gate. Keep effective assertions.
Reuse candidate evidence across metadata-only promotion; settings/channel tests
and final packaged startup, metadata/hashes and applicable update/migration
checks still run for the actual released artifacts. Record the reused source,
coverage and original environment instead of claiming a new execution.

Do not repeat sustained tests for wording, layout, packaging or a version bump.
Run them for changed scheduling/concurrency/ownership/input-output/resource
lifetimes, or a concrete time-dependent defect. Review relevant host lifecycle
changes in InputStitch.cs and use -RequireSoak when they affect those guarantees.
Choose duration for the defect; ten minutes is not a universal fixed gate.

For ownership, keyboard/mouse output, Held Mapping concurrency, Emergency Stop, or virtual Xbox behavior, also run the Input Lab black-box acceptance when ViGEmBus is available:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\InputLab\run-acceptance.ps1 -AllowRealOutput
```

The Input Lab acceptance host uses isolated configuration and must not modify the user's normal `%APPDATA%\InputStitch\config.xml`.
Treat `SUMMARY: BLOCKED` / exit code 2 as an environment-preflight result (for example ViGEm/XInput report propagation unavailable), not as an InputStitch assertion failure. Do not bypass the preflight merely to force a green report.

For Input Lab viewer changes, also run `tools/InputLab/package.ps1`; it builds the viewer and executes the target-window-message and keyboard-visual self-tests before producing versioned standalone artifacts.

For release-affecting changes, also build and run Release Verification using the repository scripts. Never publish a release only because compilation succeeded.

## Handoff rule

### Protected main workflow (2026-10-22)

`main` requires a pull request, including for administrators. Force pushes and
branch deletion are disabled; review conversations must be resolved. Use a topic
branch, run impact-selected checks, push that branch and merge its PR through the
normal merge API. Do not disable protection or use an administrator bypass to
complete routine work. This single-maintainer repository requires zero external
approvals; no other reviewer is needed. There is no unconditional desktop status
check on every documentation/website PR. Formal software releases retain the
impact-selected cumulative verification gate in the release workflow.

Use tag or workflow-dispatch publication as before. If publishing from a main
commit marker, preserve `[publish-stable]` / `[publish-beta]` in the merge commit
message. The publishers push version tags and release assets, not changes to
`main`. Documentation and handoff updates also follow this PR workflow.

Before switching development machines:

1. Reach a safe breakpoint.
2. Run the relevant regression tests.
3. Commit and push completed code changes.
4. Update `HANDOFF.md` with the exact current state, remaining work, known issues, constraints, and last verified code commit.
5. Commit and push the handoff documentation.
6. Confirm the working tree is clean and `main` is synchronized with the remote.

## Product acceptance policy (2026-10-20)

- New feature work is paused during the product refresh; see `docs/PRODUCT_REFRESH.md`.
- Stable 1.4.3 is a user-confirmed long-used baseline. Preserve its official behavior.
- Routine UI/editing/runtime completion is determined by regression, automated workflow,
  output acceptance and sustained-run evidence. Do not require another user trial run.
- Run focused editor workflows for affected editing behavior. Use sustained acceptance
  only for the time-dependent runtime changes above, including during releases; report
  its actual duration/backend and distinguish reused results from newly executed ones.
- Specific physical-device/driver/game compatibility claims retain their own evidence
  scope and do not become blanket manual gates for the ordinary macro product.
- Keep development history and compatibility adapters out of user-facing settings.
  See `docs/AUTOMATED_ACCEPTANCE.md` for commands and evidence limits.

## Explicit feature exception (2026-10-21)

The user authorized a screen-crosshair overlay after the readability pass. This
specific feature supersedes the earlier feature pause; unrelated feature work
remains paused. Preserve the Stable macro baseline and the automated acceptance
policy. See `docs/CROSSHAIR.md` for scope, research and verification.

## Visual and footprint priority (2026-10-21)

The user prioritizes readability, ease of use and a clean overview before
decoration. Use balanced text and spacing; ordinary editing should expose the
step list and its actions without outer scrolling, expansion or splitter
adjustment at normal window sizes. Validate actual visible rows, text height,
column fit and clipping by ancestor containers. Keep distribution lightweight;
measure embedded resources and preserve supported functionality when reducing
size. Compressed runtime packaging must retain verified extraction and startup.
