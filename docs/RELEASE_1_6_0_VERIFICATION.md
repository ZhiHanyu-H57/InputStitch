# InputStitch 1.6.0 verification — 2026-10-24

The user authorized Stable publication of the frozen beta.7 functionality and
replacement of unconditional full-suite/soak gates with impact-based acceptance.
Production feature code matches `c4cd0bf79ae8becaa37467f7fe3abf8704e36aa9`;
only ReleaseInfo changes: version1.6.0, FileVersion1.6.0.8, Stable channel.
No added product feature, dependency or runtime architecture change in closure.

## Cumulative acceptance

Baseline: the previous published Stable tag **v1.5.2**. The new selector examines
the whole cumulative diff, including shared modules, rather than the last commit.
Local result: **22 selected checks;9 reused;13 executed;all pass**. The input-bound
records and their compact original evidence are under `verification/`.

Fresh scoped results at `79ad6d4e88caa99cfae63202eb88589d3c1286bc`:

| Check | Result |
|---|---|
| Verification workflow | 24 checks: actual/cumulative selection, no UI-to-soak expansion, required runtime/native obligations, deduplication, metadata separation, invalidation for changed assertions/evidence, malformed receipts and workflow gates |
| Source manifest / release policy | 424 plus Stable metadata/channel/update endpoint guards |
| Corner-cut crosshair / core editor | 1,300 raster/legacy compatibility checks;162 private-desktop core/editor checks |
| Input integration / coordinator | 24 /55 focused checks; observers, no real input |
| Network / timer logic | 36 fake-network cases, zero native network mutations;134 timer checks |
| Legacy configuration/editor | 802 productivity checks, including original1.4.3/1.5.0 fixtures |
| Stable settings metadata/layout | 8,864 checks/8 synthetic-font windows, 100/125/150/200%;2,208 checks/2 actual144-DPI windows;zero issues |
| Stable settings smoke | zh/en default/compact, old XML defaults and saved vector editing |

Reused with matching declared source/test/helper/dependency inputs and intact
evidence SHA-256: beta.7 UI safety35, idle58, integration214, corner dialog65+4672,
macro journey142, secondary layouts125683/220 and tool UI76814/72.
Version/channel-dependent settings receive the separate fresh checks above.

Dedicated reused results retain their **original scope and environment**:

- OBS32.2.2 native x64 D3D9:4 recordings/48 decoded frames; RTSS timer included,
  desktop timer excluded. Source: beta.5 `7cfa5dd72ea01afd295846e1a9e51936ab1ffa9f`.
- Identity-gated Windows guest:330 native WFP checks, multi-application and
  whole-guest IPv4/IPv6 TCP/UDP, restore/normal exit/forced main or helper exit.
  Source: beta.4 `38a14a0dd49e2fd227bd5921e00994dd231d5e2f`.

No repeat macro soak, full layout matrix, OBS capture, real output or whole-host
network disruption was performed. Macro engine/worker/ownership code is unchanged;
the changed optional-tool host coupling has focused integration coverage.
Manual user trials are not an acceptance prerequisite.

## Actual release package and updates

- Both x64/x86 packages build and pass Release Verification: architecture,
  ProductVersion/FileVersion, Stable manifest and SHA-256, plus source archive.
- Actual packaged startup, live message loop, configuration save/reopen, compressed
  dependency/runtime extraction, cache reuse/corruption repair and orderly exit
  pass on both architectures, using a private desktop and isolated configuration.
- **708 packaged upgrade checks** pass with official1.4.3/1.5.0/1.5.1 executable
  bytes: fixed InputStitch.exe name, exact executable/config backups, editor
  save/reopen preserving original XML values, and injected launch-failure rollback.
- Original official1.4.3,1.5.0,1.5.1,1.5.2 and candidate1.6.0 update parsers accept
  the1.6.0 canonical manifest for both architectures.
- Local executable sizes: x642,178,560B; x862,008,576B, unchanged from beta.7.

After recording these results, a second cumulative selection **reused all22
checks and executed0 tests**, demonstrating that unchanged valid evidence does
not cause repeated product verification.

## Publication flow

Protected main is updated through a PR. The release workflow selects development
changes for ordinary CI and the previous published Stable baseline for publication,
validates/reuses evidence, builds the actual final artifacts and checks packaged
startup before publishing. GitHub artifacts retain the selected plan, result and
logs. Stable publication preserves immutable tags/assets and refreshes R2 latest
download aliases. Published bytes and the mirror are checked against their hashes.

`Run-Tests.ps1 -Full` and dedicated real-input/native/soak runners remain available
for changes that need them. They are not unconditional release gates.
