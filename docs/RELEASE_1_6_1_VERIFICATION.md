# InputStitch 1.6.1 verification — 2026-10-27

User-authorized Stable repair update. Feature sources are frozen at
`d06bdf78f1d742f020b98b3432f04067f86310f6`; only ReleaseInfo changes to
1.6.1 / FileVersion1.6.1.2 / Stable. No new feature or architecture work.

## Cumulative scope and results

The previous published Stable baseline is v1.6.0. The release selector chose
14 checks, with no soak:6 reused valid records and8 executed missing checks.
The newly completed results are also recorded for reuse by publication CI.

- Reused candidate UI safety58, modifier policy185, recording/editing209,
  input pipeline24 and source manifest424; existing workflow29 also matches.
- Fresh coordinator55, productivity802, release-policy guards, tool core214,
  macro editor journey142, Stable settings8864/8 scaled-font windows and2208/2
  actual144-DPI windows, plus zh/en compact/default settings smoke. Zero issues.
- Additional candidate advanced-dispatch163 and real ViGEm/XInput emergency11
  retain their original scope. Native emergency evidence is from a private
  desktop and emits no keyboard/mouse input; final controller state is neutral.

All feature, assertion, helper and dependency fingerprints and original evidence
hashes are retained in `verification/`. The candidate checks are reused as
original executions, not presented as new Stable runs.

## Packages and update compatibility

Both Windows architectures build and pass release metadata, manifest, hash and
source-archive checks. Final publication CI also checks actual packaged startup
and shutdown privately before publication.

The official v1.6.0 x64/x86 executables are verified against their published
digests. Their original update parsers accept the new manifest. Config fixtures
are created through each original release's real load/save lifecycle, including
its initialization metadata. The new packaged updater installs verified1.6.1
bytes at InputStitch.exe, backs up the exact old executable/config, and rolls
back after an injected launch failure. The new packaged editor saves/reopens
the old configuration and preserves every original XML leaf value.

Actual update checks:231 per architecture,462 total. The old binaries are used
as update sources/fixtures; no normal user configuration or desktop program is
modified by these tests.

Frozen behavior includes interactive mouse/caption/menu pass-through, emergency
trigger suspension with explicit resume, ordinary Stop all semantics, Shift
fallback for bare non-modifier keys, and explicit chord priority. No repeated
OBS/network-disruption/layout-wide acceptance, full suite or long soak is run.

## Publication

Publish through a normal protected-main PR and the existing Stable workflow.
Retain the versioned assets, latest endpoint and R2 mirror. Check downloaded
public assets against their digests after publication. The recovered user's
desktop is not replaced or relaunched as part of publication.
