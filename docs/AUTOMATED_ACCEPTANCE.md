# Automated product acceptance

Updated: 2026-10-20

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

Run from the repository in Windows PowerShell:

```powershell
./build.ps1
./tests/Run-Tests.ps1
./tests/Run-ProductAcceptance.ps1
./tests/Run-ProductAcceptance.ps1 -SoakMinutes 480
./tools/InputLab/run-acceptance.ps1 -AllowRealOutput
```

The build script verifies both x64/x86 artifacts, product/file versions, channel
manifest and SHA-256. The full regression includes the fast product workflow;
the standalone command is useful when editing the UI. The sustained run accepts
1–1440 minutes. Use the actual reported duration when describing evidence.
The manual GitHub Actions `Product sustained acceptance` workflow runs the same
host and uploads its reports; it does not publish a release.

Input Lab emits real keyboard/mouse and virtual-controller output. Its existing
idle-desktop and single-instance preflight applies. It keeps user configuration
isolated; do not run it over a live game. A driver preflight block is reported as
blocked, never relabeled as a pass.

## Coverage and evidence

| Layer | Exercises | Evidence |
| --- | --- | --- |
| Product workflow | Create, name/note edits, all trigger selections, actual step/timing dialog controls, XML save, reopened editor, macro/profile package round trips, run/stop/restart, global stop, copy | `ProductAcceptanceTests`, final assertion count |
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
