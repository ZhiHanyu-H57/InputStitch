# Automated product acceptance

Updated: 2026-10-22

## Completion policy

Routine macro editing and execution changes are accepted through automated
evidence. User trial runs are optional product feedback, not a completion gate.
Stable 1.4.3 is an established baseline: the user reports long-term use and use
by people they recommended it to, without problems in its official features.
This refresh preserves those behaviors and configuration compatibility.

Physical controller takeover, a particular driver's hardware behavior, and
target-game compatibility are separate claims. A missing environment limits
that claim; it does not block unrelated ordinary macro features.

## Repeatable commands

Runtime-progress comparison (specified real packages, isolated state publication,
no hooks/output/forced GC):

```powershell
./tests/Run-RuntimeProgressProbe.ps1 -Package <x64-exe> -Label <name>
```

This reports three 200,000-update microbenchmarks; it does not represent whole-app
CPU or macro throughput. `InputPipelineIntegrationTests` additionally records
`activator-ui-latency.json` for responsive/blocked UI via the production scheduler
and actual worker with an observer backend. Physical/game input latency is separate.

### Development: select by impact

Routine changes run relevant suites and focused workflow checks. A layout or
wording change runs its affected Chinese/English, resize and DPI checks; it does
not require controller, updater and sustained-output suites. A runtime lifecycle
change runs coordination, worker, timing, ownership, UI safety and sustained
checks, with applicable real-output acceptance. Configuration changes include
old XML save/reopen. Build-manifest and packaging changes include their guards.
Record the chosen scope and any exclusions. Broaden when failures or actual
dependencies justify it, rather than repeating the whole suite each iteration.

```powershell
./tests/Run-Tests.ps1 -List
./tests/Run-Tests.ps1 -Suite MacroRunCoordinatorTests,InputPipelineIntegrationTests,OutputOwnershipTests,UiSafetyTests
./tests/Run-SecondaryLayout.ps1 -ScaledFonts -Focus QuickCreateDialog
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
```

`-Suite` accepts names or comma-separated names, rejects unknown names before
compilation and runs unchanged assertions. Settings smoke requires a built
package; selected runs include it only with `-SettingsSmoke`. No arguments still
run all suites plus settings smoke, preserving the formal-release CI gate.

### Formal release: complete acceptance

Run from the repository in Windows PowerShell:

```powershell
./build.ps1
./tests/Run-Tests.ps1
./tests/Run-ProductAcceptance.ps1
./tests/Run-ProductAcceptance.ps1 -DialogsOnly
./tests/Run-ProductAcceptance.ps1 -JourneyOnly
./tests/Run-ProductAcceptance.ps1 -NativeJourney
./tests/Run-ProductAcceptance.ps1 -FieldsOnly
./tests/Run-ProductAcceptance.ps1 -NativeFields
./tests/Run-SecondaryLayout.ps1
./tests/Run-SecondaryLayout.ps1 -Native
./tests/Run-SecondaryLayout.ps1 -ScaledFonts
./tests/Run-SecondaryLayout.ps1 -ScaledFonts -Focus CrosshairSettingsDialog,QuickCreateDialog
./tests/Run-ProductAcceptance.ps1 -SoakMinutes 480
./tools/InputLab/run-acceptance.ps1 -AllowRealOutput
```

The build script verifies both x64/x86 artifacts, product/file versions, channel
manifest and SHA-256. The full regression includes the fast product workflow;
the standalone command is useful when editing the UI. The sustained run accepts
1–1440 minutes. Use the actual reported duration when describing evidence.
The manual GitHub Actions `Product sustained acceptance` workflow runs the same
host and uploads its reports; it does not publish a release.

The secondary-layout suite is part of the full regression. Its standalone runner
also supports native DPI, `-ScaledFonts`, `-SettingsOnly`, and a comma-separated
`-Focus` dialog filter for iteration. Full regression uses enlarged fonts as well
as scaled geometry. It checks live
default/tab/compact/restored windows, native checked-list input, actual visible
text and ancestor clipping. PNGs and issues/summary files are under
`artifacts/secondary-tests/secondary-layout*`; full-suite outputs use
`artifacts/tests/secondary-layout`. Native DPI is reported explicitly. Synthetic
windows that exceed the test desktop's track-size limit record CLAMPED; an
unavailable compact geometry is not counted as a successful compact test.

Input Lab emits real keyboard/mouse and virtual-controller output. Its existing
idle-desktop and single-instance preflight applies. It keeps user configuration
isolated; do not run it over a live game. A driver preflight block is reported as
blocked, never relabeled as a pass.

## Coverage and evidence

| Layer | Exercises | Evidence |
| --- | --- | --- |
| Product workflow | Create, name/note edits, all trigger selections, actual step/timing dialog controls, XML save, reopened editor, macro/profile package round trips, run/stop/restart, global stop, copy | `ProductAcceptanceTests`, final assertion count |
| Friendly interaction | Real new-macro button and modal cancel/blank-create; shortcut pause/restore; accessible purpose cards; empty steps; live press/hold/random timing explanation; quick-create next-step and readiness controls | `ProductAcceptanceTests.FriendlyInteractions`, Chinese/English and synthetic 100/125/150/200% dialog renders |
| Compact fields and disclosure | Blocking checkbox visible with either disclosure state; layer selectors inside the outlined frame; bilingual resize, no common-row overlap, bounded readable step choices, enlarged vector diagram and reachable footer | `BoundedFields`, `-FieldsOnly`, `-NativeFields`; PNGs under `artifacts/product-fields*/product-acceptance`; full regression also runs the matrix |
| Step input methods | Two immediately visible methods, physical keyboard/mouse, diagram single/chord, square A preview, Escape/Emergency Stop and switching restoration, no emitted output | `StepInputMethods`, normal regression and native `-NativeJourney` |
| Beginner workflow | Real blank-creation and nested key/step controls, edited/cancelled steps, invalid ranges/wheel actions, automatic XML persistence, reopen/Run/shortcut dispatch, shortcut-off manual Run; quick chord/modifier/mouse/numpad capture, Esc/Emergency Stop and existing-macro isolation | `BeginnerWorkflow`, `QuickShortcutCapture`; `-JourneyOnly` and native-DPI `-NativeJourney`; bilingual checks and PNGs under `artifacts/product-journey*/product-acceptance` |
| Remaining dialogs and first run | Persistent footer at small sizes and synthetic scales; only relevant timing fields; inline invalid-chord/batch-range feedback; Settings staging/defaults; actual nested create/choose/edit/save/Run buttons; group-name modal protection | `RemainingDialogs` and `NewUserJourney`; `-DialogsOnly` runs this subset during iteration; default regression includes it |
| Layout | Chinese/English, 1000×700 and 1280×900 layouts, 100/125/150/200% synthetic scaling, collapsed/expanded options, readable selection values and available controls | PNGs under `artifacts/.../product-acceptance/` |
| Sustained execution | Continuous timed macro + repeated finite runs + persistent gamepad Held Mapping; repeated stop/restart and configuration save/read cycles; UI dispatcher activity | Per-run `progress.json`, `samples.csv`, `report.json` |
| Runtime regression | Activation timing/conditions, output ownership, overlap, layers, fail-safe cleanup, UI editing safety, config/update recovery and input-provider tests | Full regression output |
| Windows output | Actual SendInput, low-level hook observation and actual ViGEm/XInput reports | Input Lab text report |

The sustained host observes output through the production ownership/execution
path with an isolated backend; it does not inject input into the user's desktop.
Input Lab independently covers the real Windows/ViGEm output boundary. Its
simulated trigger edges exercise the real handlers because the product ignores
injected events as physical macro shortcuts.

Sustained acceptance samples private bytes, handles, threads and down/up counts
every ten seconds. It requires complete source/output cleanup, matching keyboard
down/up counts, continuous output without a two-second gap, UI dispatch under two
seconds, and bounded final resource growth after a thirty-second warmup (128 MiB
private memory, 32 handles, 12 threads). Working-set fluctuations alone do not
establish a leak. Samples retain peak values for inspection.

Synthetic layout scaling is not a claim that every physical monitor/DPI setup was
tested. A passing duration is evidence for that duration, not an indefinite-run
guarantee. Extend the same host for longer unattended runs without involving the
user in routine manual acceptance.
