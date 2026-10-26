# HANDOFF.md

## Primary-mouse lockout recovered; local1.6.1-beta.1 correction — 2026-10-27

Affected machine recovered via AgentDock: stopped only InputStitch, backed up
the config and disabled exactly one enabled/plain-left/suppressed Toggle macro.
User confirmed normal left-click operation. All other XML values preserved;
no desktop executable replacement or relaunch was performed.

Source baseline3509197, branch fix/primary-mouse-recovery. Mouse edges preserve
native clicks on own interactive UI/caption/menus, excluding native click-through
overlays. Emergency Stop immediately pauses triggers until explicitly resumed;
ordinary Stop all keeps prior behavior. Capture/recording retain priority.
Targeted UI/mouse/editor/integration/advanced/modifier/source/channel checks,
real ViGEm/XInput emergency-only11 and both packaged startups pass privately.
No full suite, host keyboard/mouse injection or soak. Local beta only; Stable
downloads/website unchanged. See docs/MOUSE_TRIGGER_RECOVERY_1_6_1_BETA1.md.
Do not replace/relaunch the recovered user's program without their instruction.

## Website and user guide synchronized to1.6.0 — 2026-10-24

Source baseline8d5ca9615cfd2b4ff191db4c1f1ae7233ebad0cd. Website overview/download/
guide and zh/en READMEs explain timer controls, per-part/batch/multiline editing,
RTSS/OBS setup, reversible multi-app/computer network shortcuts and sounds, and
corner-cut crosshair settings. Copy uses released UI names and action steps;
obsolete release tables and internal runtime essays removed from the READMEs.

Website-only checks pass:246 static,46 README links/version,40 bilingual responsive
cases,295 browser assertions and12 focused preview/content checks. Impact selection
chooses0 product checks. No product sources, executable assets, application
settings, input hooks, network state or release workflow changed. Publish through
normal protected-main PR and Cloudflare Pages; no new desktop release needed.
See docs/WEBSITE.md. Future work remains limited to new user authorization.

## Stable1.6.0 published; impact-based verification complete — 2026-10-24

User authorized workflow/rules correction and Stable release of frozen beta.7.
Implementation f531daa plus documentation classification79ad6d4. Production
feature sources match c4cd0bf; only ReleaseInfo is now1.6.0/1.6.0.8/Stable.
No new product functionality in this closure. PR #7 merged normally to protected
main at41bf644eb4b56cc60de1dcc8b5e20d99486f0759; no protection bypass.

Local cumulative v1.5.2->candidate selects22 checks:9 reused/13 fresh/all PASS;
no soak. Final workflow29, manifest424, coreCrosshair162, corner1300, pipeline24,
coordinator55, network36(no native mutation), productivity802, timer134,
settings8864/8 synthetic and2208/2 native144-DPI, settings smoke and release policy.
Actual x64/x86 startup/cache/save-reopen pass. Official143/150/151 packaged
upgrades708; original143/150/151/152/current160 manifest parsers pass both arch.
Detailed record: docs/RELEASE_1_6_0_VERIFICATION.md.

Receipts bind Git-normalized feature/test/helper/dependency inputs and original
evidence hashes. Curated .log files are intentionally tracked under verification/
evidence; raw artifacts stay ignored. Candidate records preserve original scope.
Second selection and actual final publication CI reused all22/executed0.
Do not rerun large matrices without affected inputs. Docs-only follow-up selects
zero product checks.
All new UI checks are private-process desktop checks; CrosshairTests --core and
ProductivityTests now use that route. Actual native composition remains explicit.

Published Stable/latest v1.6.0 on2026-10-24 at23:03:31 Beijing time. Run40105043990
passed verify(94seconds), publish-stable and mirror-stable-r2. All5 GitHub assets,
four R2 aliases, R2 manifest/two pinned executables and66 frozen feature sources
match. x642178560B, x862008576B; no size increase from beta.7. Website fallback
release snapshot updated to1.6.0. See docs/RELEASE_1_6_0_VERIFICATION.md.
Authorized work is complete; no further feature/architecture work pending.
User app untouched. Future changes use actual-impact selection; future releases
use cumulative changes since the previous published Stable and valid receipts.

## Automation spacing, PrintScreen and private desktop verification — 2026-10-24

Verified implementation: c4cd0bf79ae8becaa37467f7fe3abf8704e36aa9.
Local 1.6.0-beta.7 on feature/overlay-tools; no push or publication.

Automation target delay numeric had native144-DPI margins72/72, row178px;
now4/7 and49px, width225->150, target section375->226px in both languages.
Settings groups normalize padding; idle panel computes explicit leaf heights
and wrapped analog/help content from available width. No model/runtime changes.
Main InputSinkControl and runButton pass PrintScreen; ordinary key absorption,
capture, global bindings and Emergency Stop remain.35 UI safety and214 integration
checks cover local events and unbound global forwarding. No physical PrintScreen
injection or OS Snipping Tool launch claim this turn.

EditorChoice caches scale by Font/DeviceDpi, defers monitor clamp to opening and
updates tooltips/accessibility only on change. Native144-DPI paired48 samples,
beta6/current preparation medians(ms): settings1102.6/756.4, step497.3/381.5,
crosshair578.8/480.7, timer952.4/753.1. Includes constructor+Show/Update+bitmap;
not physical input-to-presentation latency or full-application CPU reduction.

Six selected suites pass: source424, UiSafety35, Idle58, ToolIntegration214,
CornerCutDialog65+4672, ReleasePolicy. Full secondary scaled-font125683/220,
tool UI76814/72, native automation540/36, native settings2188/2 (zero issues),
journey142, oldXML/settings smoke and final x64/x86 startup/restore/cache pass.
Selected scope only; no full formal-release suite, OBS or native output/network
rerun, since backends unchanged.67 production sources, no dependency/polling.

Important verification fix: moving just one STA to a private desktop hung
WinForms close cleanup in both baseline and current. IsolatedUiDesktop now
restarts the entire test executable with STARTUPINFO.lpDesktop before framework
threads/windows are created. No SwitchDesktop import or access. Child lower
priority, bounded lifetime; parent samples foreground/input-desktop identity,
waits for process exit and closes its desktop handle. All final tests pass with
zero test-window foreground samples and successful desktop release. PowerShell
smokes use architecture-matched UiScriptHost on that private desktop. Never run
raw SettingsSmoke/packaged UI on user's active desktop during routine checks.
Actual desktop/OBS composition checks still require their dedicated environment.

No further Computer Use/desktop input during this continuation. Old app PID22984,
started19:42, remained untouched. Announce CU start/stop; do not leave inspectors
active until end of turn. A host-managed CU helper may remain resident; do not
mistake residency for active control or repeatedly kill its automatic respawns.

Outputs chat outputs/beta160-7: fixed InputStitch.exe, x642178560B
10C01E1F6EA014C6DB564B0E105F95BC54C0EA1F289436921F23DE338C4C66C4;
x862008576B 92E58DD160E1E18BC23F1A2AA367D72E939479A7AE7265A8A6A888121E4F2BFC.
Each+2560B vs beta6; Windows PowerShell compression preserves native payload size.
Report: docs/UI_LAYOUT_AND_INPUT_1_6_0_BETA7.md. This request complete.

## Native DPI and timer selection regressions fixed — 2026-10-24

Verified implementation: 7c99137300efca87365f6fb017420e200ad3e4dd.
Local 1.6.0-beta.6, feature/overlay-tools, no push/publication.

User reported Automation page rendering, bloated network Sounds rows, hard-to-find
batch editing, Shift visibility changes and slow/flickering timer part selection.
Root native144-DPI failure was nested IdleGamepadSettingsPanel Font autoscaling
on top of the form's DPI scaling. Panel now uses AutoScaleMode.None, inherits
one outer scale, has content-derived preferred height and no async height loop.
Target row is bounded, analog wrapping accounts for available width. General
settings rows shrink to content; target/run/profile settings now precede idle.
Network sound table uses content column sizing; flows dock Top, labels align Top,
sound panels cap400 logical pixels and small tool buttons min76 (text can grow).
TimerPartList prevents native row-selection/double-click checkbox side effects;
explicit state-icon/Space changes remain. Batch hint is above list. Deferred
selection update coalesces deselect/select; one fixed-height content slot keeps
format controls in place and retains preview cache. No model/runtime schema change.

Impact checks: SourceManifest424, ToolIntegration205, UiSafety26, IdleGamepad58,
ReleasePolicy pass. Tool full layout stage76374/72 passed before final small
button/idle refinements; final relevant regression network120/8 (combined704/24),
timer688/8 (320 selections with bound contents), settings216/8 pass.
Current native144-DPI Settings2188/2 and scaled-font Settings8784/8 pass with
zero issues (default/compact/restored/all tabs). Native first run caught237
layout issues; fix and complete native rerun passed. Do not substitute synthetic
scaled-font checks for native DPI acceptance. Timer selections~23.9–36.6ms each
including message processing,32 layouts per40, no transient editor disable,
unchanged preview identity. Selected checks only; no full formal release suite.
SettingsSmoke and both final Release Verification/packaged startup+restore pass.
No real output, network block or OBS rerun; macro/output/network/timer drawing
backends unchanged. Existing user InputStitch untouched; configs isolated.

Computer Use stopped immediately when the user pressed physical Escape during
final native timer inspection. No further desktop input; no manual acceptance
gate. The isolated inspector has a five-minute automatic close deadline and
never loads/saves user configuration. Current development request complete.

Outputs chat outputs/beta160-6: fixed InputStitch.exe, x642176000B
E60CB6549F2E6801A707E54B9F9577E035FCF634132EB189BB5E7F5B51006835;
x862006016B FED49EA39B41E399AAB69E9F0D367E970B424CE53FA97404BBCA80EDE8EEEFD8.
Each +512B vs beta.5;67 production sources, no dependency or background polling.
Use Windows PowerShell build compression as previous package: current pwsh
compressor produces larger embedded gzip streams with identical dependencies.
Report: docs/UI_REGRESSIONS_1_6_0_BETA6.md. No active task or publication request.

## Dialog responsiveness and usability completed — 2026-10-24

Verified implementation: 7cfa5dd72ea01afd295846e1a9e51936ab1ffa9f.
Local 1.6.0-beta.5 on feature/overlay-tools; no push/publication.
67 production sources, no added dependency. Main macro runtime, ownership,
Emergency Stop/editing guard, WFP filtering/helper and sound backend unchanged.

Dialog construction now batches nested layouts, theme application and Frame;
EditorChoice reuses label measurements while checking content/font changes.
IdleGamepad.cs changes only settings-panel construction. Same-host zh/en
3 samples each, constructor+Show/Update median: settings838->279ms,
step editor610->138ms, crosshair704->160ms, timer1534->256ms,
network784->120ms, virtual keyboard200->166ms. Raw logs retain all samples.
The pre-batching comparison already has the beta.5 usability edits; metadata
still read beta.4 during timing and was bumped after the final implementation.
No persistent hidden dialog reuse or delayed editing-protection shortcut.

Timer disabled actions now explain enablement; multi-selection moves together;
first switch from a bound preset to Custom retains bound keys/state actions,
without overwriting existing custom rules. Empty list/clear buttons follow state.
Network UI shows filename before path, uses dirty width measurement and explicit
Use built-in cue action. No serialized model change.

Timer renderer keeps unchanged part/line surfaces with a 4MiB retained budget
and temporary fallback. Preview reuses unchanged appearance. Actual beta.4 vs
beta.5 binaries match28 raw pixel frames (multiline/mixed alpha/negative gap/
rollover/rewind). Paired render medians: parts.247->.095ms,
large multiline1.781->.756ms; unchanged preview17.348->.585ms.
These are render microbenchmarks, not whole-process CPU reductions.
Quiet final60s soak: default30.004s/600frames/.984ms/1.562% one core;
large30.016s/600frames/3.140ms/6.559%; GDI3->3, no GC, bounds pass.

Impact checks pass: source424, timer134, integration205, UI safety26,
productivity802, corner dialog65+4672, release policy, input pipeline24,
network core36 (no native network mutations), full secondary layout125667/220,
tool layout75790/48, macro journey142, main density3328. SettingsSmoke,
both Release Verification and final packaged startup/config restoration pass.
Desktop native timer147 per x64/x86. Full42-suite formal release not run.

Current native x64 D3D9 OBS32.2.2:4 recordings/48frames pass; RTSS included,
desktop excluded,12 distinct timer masks. Current environment rechecked at
16:49+08:00: OBS17 baseline config hashes and RTSS2 config hashes unchanged;
test processes0, websocket disabled/password cleared, temp profile removed.
Use newly generated output OBS-evidence.zip; old cleanup/final-result/hash
files under the shared temporary OBS evidence directory were stale and excluded.
Network backend untouched: no guest/whole-host block test this turn; beta.4
native dual-stack330 remains historical evidence. Retained guests/tools preserved.

Outputs chat outputs/beta160-5: x642175488B,
SHA256 21510CFE5029405E08F81F746F7838D5D1E04ABF1444882DFCB44F11AAB8D6D3;
x862005504B, SHA256 965AE5F33C02A8645452467EFAF86B0B86C7D36075FFE92A51326BFC82081E39.
Each +4KiB vs beta.4, fixed InputStitch.exe names, sanitized evidence/screenshots.
See docs/USABILITY_AND_PERFORMANCE_1_6_0_BETA5.md. Current request complete;
no active task, no manual-trial prerequisite, no additional feature/release scope.

## Flexible timer and network customization — 2026-10-24

Verified implementation: 38a14a0dd49e2fd227bd5921e00994dd231d5e2f.
Local 1.6.0-beta.4; feature/overlay-tools; no push/publication.
67 production files, one new NetworkSettingsDialog source, no new dependency.
Root owns repo; independent network agent snapshot integrated and frozen.

Timer has four presets plus Custom: each hotkey maps Ready/Running/Paused to
reset + Keep/Start/Pause/Toggle + Keep/Show/Hide/Toggle. Editing and Emergency
Stop guards remain. Only active keys reserve/conflict/dispatch. Default settings
preserve old XML. Display interval16–5000ms and line spacing−64–160px; cadence
never counts elapsed time or rebuilds atlas. Ctrl/Shift style edits apply only
the changed property; Match styles deep copies first selected style. Enter and
Add line break support multiline. Presets are optional, not the custom limit.

Network keeps separate/same/legacy chords, adds PCM8/16 mono/stereo WAV import
and independent per-cue gains, no session/system volume changes. MCI WAV/MP3
failed natively, so no MP3 promise or decoder dependency. At most16 exact paths,
all validated before elevation; BLOCKSET uses one dynamic WFP transaction and
session, unified rollback/release. Full target set comparison restores existing
scope on any target change. Absent array retains legacy path; explicit empty
stays empty. Network dialog split into scope/hotkeys and sounds; row spacing and
populated lists verified at DPI. Startup never blocks; timer starts zero paused.

Nine selected suites pass (not full42), SettingsSmoke; Timer129, Network36 per
arch including native silent PCM/resource cleanup, integration186. Tool UI
75790/48 views zh/en100/125/150/200% and compact; macro journey142. Native desktop
142 per x64/x86. Both final package startup/reload/shutdown smokes pass with
custom rules/cadence/line gap, multiple paths and cue path/volumes. Cache60s:
default0.685ms/frame/1.145% one core, large3.561ms/7.395%, each600frames/GDI3->3.

Actual OBS32.2.2 native x64 D3D9: four recordings48frames, two rows/mixed fonts,
100ms updates/12px gap. RTSS included, desktop excluded, controls pass, yellow
text12distinct masks. User OBS17config hashes unchanged, dedicated temporary
RTSS profile removed. RTSS was started for acceptance and remains running;
its Config file was rewritten by RTSS (not claimed hash-identical). Portable
OBS/native target/producer all exited. Copy only this run's selected evidence;
old detours-final-result/video-hash files in temporary obs directory were stale.
Current OBS result/hashes regenerated in output archive.

Fresh marked Windows guest native WFP330 passes in103.7sec: A+B blocked together,
C stays connected for new/existing IPv4/IPv6 TCP/UDP; all four whole-guest
Restore/normal close/parent/helper TerminateProcess cases recover. Initial run
hit fixture command-file sharing contention; bounded3s fixture read retry fixed
it, full rerun passed. Production helper deadlines unchanged; guest-only NGEN.
Host whole-computer blocks0, host HTTPS200. Guest and loopback peer exited.
Retained offline disks/tools not deleted; no retry of previously rejected cleanup.

Outputs chat outputs/beta160-4: x642171392B/hash53C6E48A93CB3A5F021D8416CD02352DA358B72DC2AB42A150F6753AE355E4A5;
x862001408B/hash209D6FF19E25B14665F8D26EF886F1620A446FC07616CB62A37868576BDD2404.
Fixed InputStitch.exe names; sanitized evidence omits tokens and OBS credentials.
See docs/OVERLAY_TOOLS_1_6_0_BETA4.md. Current scope complete, no manual-trial gate.
Further feature work or formal release needs a new request.

## Independent network commands and styled timer parts — 2026-10-24

Verified implementation: 30f99034988ee6a6a081adf3b79a380cdf0aae31.
Local 1.6.0-beta.3 on feature/overlay-tools; no push/publication. Eight of 66
production files changed, no new source or dependency. Macro engine, ownership,
stop and WFP privilege-helper implementation unchanged.

Network has independent block/restore keys, repeat-block idempotence and two
optional generated PCM cues. Same-key and legacy Hotkey configurations toggle.
Restore works through editing guards/pending elevation. Cues follow confirmed
state edges; repeats/cancelled UAC/startup silent. Timer supports ordered visible
parts, individual font/style/color/alpha, cumulative minutes/seconds and precision.
Old templates retain Parts=null. XmlSerializer needs an explicit array proxy:
a public List alone changed absent old Parts to empty on reload. Actual
ConfigStore/clone/null/empty tests cover this. Baseline-aligned desktop glyph
cache and single RTSS PNG atlas retain bounded storage and no tick disk writes.

Nine selected suites and SettingsSmoke pass: timer 97, network 29, integration 153.
Tool UI 37689/32 views, zh/en 100-200% plus compact sizes; default controls/list
fully visible. Macro journey 142, desktop 109 per x64/x86 and packaged actual
startup/style/shortcut restoration/shutdown pass. Cache 60s: default 1.019ms/frame,
large 4.122ms/frame, GDI 3->3, no forced GC.

Current styled-parts OBS native x64 D3D9 Game Capture: four recordings/48 frames
pass, smaller italic milliseconds included by RTSS, desktop excluded, overlays
off/desktop-only controls pass; yellow masks 12/12 change. A direct D3D9 independent
marker did not render and remains BLOCKED; actual native OBS capture plus desktop
positive control establishes the pass. ffmpeg path corrected before final full
recording run. Dedicated UseDetours test profile removed; RTSS 2/OBS 17 unchanged.

Fresh marked QEMU guest 304 passes in 100.7s: app A-only/B negative control, whole
guest Restore/normal close/actual parent/helper TerminateProcess, IPv4+IPv6 TCP/UDP
new/existing flows; repeat Block stays blocked in all 5 cases. Host blocks 0,
HTTPS 200. Guest-only NGEN, unchanged production deadline. Guest, loopback peer,
portable OBS, timer producer/native target and elevated profile helper exited;
existing user Stable remains running. Retained offline guest disks/tools reused;
no delete retry or new cleanup approval. Some legacy host settings snapshots
predate the user's beta.2 use; do not present them as this turn's before hashes.
All tests redirect settings to isolated paths.

Outputs: chat outputs/beta160-3; x64 2188288B / x86 2003968B, fixed InputStitch.exe.
Report/hashes: docs/OVERLAY_TOOLS_1_6_0_BETA3.md. Sanitized evidence ZIPs exclude
permission tokens and OBS credentials. Further work or Stable release needs a
separate request; no manual-trial gate. Full 42-suite gate reserved for release.

## Overlay acceptance follow-up completed — 2026-10-24

Verified production/test implementation commit: 9c2f1d56387582918761cd6cd283cd707ee65bd3.

Local 1.6.0-beta.2 on feature/overlay-tools. No push or publication requested.
Only ReleaseInfo and timer capture-help text changed among 66 production files;
all timer/network/macro implementation is identical to beta.1. EXE sizes stay
2117120 bytes x64 / 1947136 x86. Eight selected suites pass; after the metadata/help
stamp, SourceManifest/ReleasePolicy/ToolIntegration, UI 29933/32 and both packaged
smokes pass. No full 42-suite formal-release gate in this follow-up.

Actual OBS 32.2.2 game_capture, native x64 D3D9: four MKV recordings/48 decoded
frames pass. RTSS included with capture_overlays on, desktop excluded; overlays-off
and desktop-only controls pass; changing yellow text 12 distinct masks. Default
RTSS hooking froze even the native no-timer baseline; Microsoft Detours fixed it.
Dedicated temporary profile removed, existing RTSS and 17 OBS configuration hashes
unchanged; native target/producer/portable OBS exited. See docs/OBS_TIMER_ACCEPTANCE.md.

Native isolated network: QEMU TCG Windows Server 2022 guest, external nonce echo
through 10.0.3.2/fec1::2. Application A-only/B connected, then four EntireComputer
restore/normal-exit/actual parent TerminateProcess/actual helper TerminateProcess
cases pass 299 assertions in 99.6 seconds. Both independent executables, IPv4+IPv6,
TCP+UDP, new+existing flows and same UDP sockets after recovery. Helper-kill recovery
verified while parent still alive. Five host HTTPS checks returned 200 during guest
blocks; host-wide blocks=0. Guest guard firmware/WMI match, host self-test15 / guest14.

The TCG fixture required guest-only NGEN /NoDependencies to fit extra test identity
checks/JIT into the unchanged production 10-second startup deadline. Prior cold-start
failures are retained with evidence. Production timeout/code was not altered.
The probe now has explicit READY and bounded socket startup; unexpected exceptions
are ERROR rather than successful blocking. See docs/ISOLATED_NETWORK_ACCEPTANCE.md.

Windows guest and loopback echo peer exited normally. User InputStitch/RTSS remained
running; config.xml/devices.xml hashes unchanged. Automatic approval review rejected
deleting remaining guest disks/portable test tools (policy block, no detailed reason).
Those temporary files remain offline; no cleanup approval is pending. Evidence and
reproduction scripts are retained in the chat output/work folders, not the repository.

HIDMaestro author guidance from issue6 is recorded in ROADMAP: retain held state and
submit full frames on Send; xbox-360-wired / dualshock-4-v2 profiles. No adapter work
or GitHub reply was included. Stable remains 1.5.2; further features or publication
require a separate request. Report: docs/OVERLAY_TOOLS_1_6_0_BETA2.md.

## Overlay tools local beta completed — 2026-10-23

Verified production commit: 9040343736a316507099bbc5e219acec00bd69b2.

1.6.0-beta.1 on feature/overlay-tools, based on main 4e32bce. All 66 production
sources integrated. No remote publication. Defaults opt in; timer starts paused
at zero; only the WFP helper elevates. Parent/helper exit removes dynamic filters.
16 impact-selected suites pass, including new render/serialization/shortcut/
rollback/exit tests. Tool UI 29933/32, corner UI 65+4672, journey 142, density
3328; packaged x64/x86 restore settings, show timer paused, release native windows
and preserve normal config/devices. Desktop native 77/77 per architecture.
Actual D3D9 x64/x86: bilingual 36px text, zero one-pixel shift mismatch, zero
independent-client region changes. RTSS uses power-of-two atlas/gutters and
read-only target zoom; no FNT or profile changes. D3D preflight waits for an
independent visible marker, else BLOCKED/2; empty scenes need a real primitive.
Native network IPv4 TCP/UDP and existing-flow reauth, other-exe negative control,
parent/helper crash recovery pass. No whole-host interruption; no usable native
IPv6 address; OBS not installed. Do not claim these three as live-tested.
Source scopes from three isolated module agents were integrated by root.
Only work-only test issues and acceptance-found product issues were repaired;
all final feature behavior has automated evidence. Actual old Stable 1.5.2 reads
new config/macros via the new shape's ring fallback. 60-second cache benchmark
and binary hashes: docs/OVERLAY_TOOLS_1_6_0_BETA1.md. No manual trial gate.

Published Stable remains v1.5.2. Further formal publication requires a separate
request and the full release gate; this beta ran selected checks, not all 42.

## 1.5.2 Stable released and verified — 2026-10-23

GitHub latest Stable v1.5.2, tag/source 2278a55730e05beebd2f6a63f51a9f1552567499.
Verified code commit 8e0fc52b1d6ed07b7fd922686b3b9a9b77b78f9b; beta.1 freeze
baseline 5ba48ebb393f9ab2b083906b3a10b467c22cf60e. Only ReleaseInfo differs
among 59 production files: Stable 1.5.2 / FileVersion 1.5.2.1. No new feature,
dependency or architecture expansion during formal closure.

Local and independent GitHub full regression: all 37 suites plus SettingsSmoke.
Workflow local/CI 6639/6643; mouse/record/batch 209; scaled layout 109755/204;
actual 144 DPI layout 28024/54, journey 142, fields 1086; ownership 303716;
packaged x64/x86, original 1.4.3/1.5.0/1.5.1 upgrade/backup/rollback 708,
real output 81/0. Fresh soak 600.1 seconds / 4297 cycles, keyboard 22990/22990,
three workers, zero final outputs. Work-only PS5 array/JSON-encoding probe issues
were corrected and the affected audits rerun; no production/configuration change.

Release workflow 39818549517 succeeded: independent verify, Stable publish and R2
mirror. All five GitHub + versioned website assets and both latest EXEs match
API digests/checksums/manifests. Source ZIP matches 59 frozen production texts
and UI resource inputs. Actual downloaded packages pass both startup probes,
the 708-check upgrade again and all original-version update parsers. Normal
config/device hashes unchanged. Published x64/x86 hashes and complete evidence:
docs/RELEASE_1_5_2_VERIFICATION.md. Delivery uses downloaded published files.

Protected main remains enforced; source release merged normally through PR #4.
No manual-trial gate. Retain the feature/architecture freeze; further changes
need a measured issue or separate user request. No normal-config app is running.

## Recording / mouse / batch timing beta completed — 2026-10-23

Branch `fix/recording-batch-timing`, based on protected main `1eee151`.
The user authorized normal closure of the running Stable instance for actual
output acceptance; it saved/exited normally through an elevated close helper.
All test settings remain isolated. Do not restart a normal-config copy during
the output acceptance run. Stable/latest publication remains 1.5.1; local
development metadata is 1.5.2-beta.1, not a formal publication instruction.

Verified production commit: `132069ac9ab43a43cdb528b9e687ebafa46c1ef9`.
Fix scope, final hashes and selected evidence are recorded in
`docs/RECORDING_EDITING_1_5_2_BETA1.md`. Eight selected suites pass, including
174 recording/editing checks, 802 save/legacy checks and 303716 ownership checks;
dialog flow 512, complete journey/input switching 142, real output 81/0,
final mouse layouts 4453/12 (100–200%) and 1842/6 (native 150%), native photo
pixels 5, both packaged architectures pass. No user-trial gate.
The final mouse is a generated white product image with compact JPEG color and
lossless alpha (39390 embedded bytes), cached once. Its photo and hit areas keep
the same aspect ratio; right/middle callouts do not cross. The diagram selector
remains available after physical mouse capture. Default recording tests provide
the foreground boundary explicitly, using actual external-window identity;
`--recording-native` is the optional live foreground variant. Formal CI need not
fight the user's active desktop. Actual output is independently tested in Input Lab.
No speculative Raw Input lane, hook rewrite, scheduler or target policy change.
Use the normal PR workflow; do not bypass main protection or publish a version
tag to deliver a local beta.

## Main protection and website HSTS completed — 2026-10-22

User authorized these two changes first; Cloudflare account 2FA is a later,
separate task. Main protection is active and applies to administrators: PRs are
required, force pushes/deletion are denied, review conversations must be
resolved. Zero external approvals supports the single-maintainer workflow.
Impact-selected checks remain the development policy; the existing full software
release verification gate and tag/dispatch publishers are unchanged.

Actual administrator-authenticated direct push rejected with GH006; main stayed
at aaade7d. The same change merged normally through PR #1 without admin bypass,
source merge 6ce175f25921ba2629107d1afa7718c8703d0aa4. See AGENTS.md before
future commits/pushes; documentation also uses topic branches and PRs.

Published HSTS `max-age=31536000` on inputstitch.zhihanyu.com only, without
includeSubDomains/preload. Deployment f49e3dc7-2af6-478b-be7e-f7c47b44ec22
succeeded. Live document responses and fresh static-resource requests carry it;
some prior asset cache entries retain older header metadata. HTML delivery
teaches the policy for the entire host. Download and preview host policies are
unchanged, and all 13 served file contents match the existing website.

Acceptance PASS: 222 static/published-metadata checks; 33 focused live browser
assertions across Chinese/English and widths 390/1440, including HSTS, navigation,
language persistence and an actual download-button click yielding the verified
x64 EXE. Both x64/x86 R2 download hashes match GitHub digests and update manifests.
Canonical GitHub legacy URLs and derived R2 manifest validation pass. Stable
v1.5.1/tag, all 57 production sources, workflows and publishers are unchanged;
no new software release or unrelated product regression was run.
Detailed records are in this chat's outputs/website-security.

## Homepage refinement published and verified — 2026-10-22

Follow-up to published redesign: simplify the homepage to the actual software,
compact input/action examples and crosshair preview; remove the icon matrix,
decorative numbering/screenshot chrome and duplicate four-step overview. Keep
setup details in the existing guide. Remove forced headline wrapping and the
generic template sentence; use concrete button/action descriptions. Remove the
unused CSS; first-page assets are now 73,569 bytes uncompressed in this checkout.

Existing language logic is unchanged: valid saved zh/en preference wins, otherwise
zh-* browser locale selects Simplified Chinese and other locales select English.
Local acceptance PASS: 295 browser assertions / 40 responsive cases, seven added
automatic/saved-language scenarios and 222 static/published-version checks.
Reviewed actual desktop/mobile screenshots. No desktop source/build/test change.
Published source `51c65e1274ca87b37349472ae632ad6b29bb69bf` to the existing
Pages project using the authorized inputstitch-site profile. Production deployment
`ec4ac4ea-11c7-40dd-a137-f00972689c70` succeeded on the original custom domain.
All 13 served files/404/headers/MIME checks PASS; live homepage smoke PASS with
28 assertions across Chinese/English and widths 390/1440, including actual
first-visit language, manual switch/navigation/reload and screenshot loading.
No duplicated full live matrix: the local 40-case matrix already covers the
changed shared CSS. Evidence is in this chat's outputs/website-refinement.

## Website redesign published and verified — 2026-10-22

Branch `web/151-redesign`, based on main `365e75a`. Rebuilt overview/download/guide/
404, current diamond branding and actual 1.5.1 bilingual screenshots, responsive
light layout, keyboard-accessible image enlargement and crosshair preview.
Copy contains practical setup/usage/requirements, matching current template/UI
labels; old version history, internal mechanisms and defensive/test-process copy
are removed. Stable version/date/sizes refresh from the official release API with
a local snapshot; download buttons retain permanent website URLs.

Final website-only acceptance: 278 browser checks / 40 bilingual page-width cases
(320/390/768/1024/1440, DPR 1/2), 226 static/live metadata checks. Actual rendered
screenshots inspected; fixed active-download navigation contrast and decorative
diamond overlap. Four old screenshots removed, four current lossless WebP images
added. First-page local assets ~83 KB uncompressed. No desktop C# or release
artifact change, no unrelated desktop regression run. See `docs/WEBSITE.md`.

User authorized Pages publication and completed Cloudflare OAuth. Created the
separate `inputstitch-site` profile with account/user read + Pages write and
offline access; retained the existing default Workers login. Published the
existing `inputstitch` Pages project, branch `main`, source `bd5c9c2`, deployment
`17621dc1-4b07-45bd-9d6b-cb13a55c5800`. Cloudflare confirms production success
and the original custom domain https://inputstitch.zhihanyu.com/.

Live domain acceptance: 278 browser checks / 40 cases PASS, 226 static/version/
size checks PASS, all 13 served files match sources (HTML comparison excludes
only line endings and the hosting platform's existing injected beacon). Custom
404, response headers and SVG/WebP MIME checks PASS. Source ZIP, report and live
screenshots are in this chat's outputs/website-redesign; no manual-trial gate.

## 1.5.1 Stable published and verified — 2026-10-22

Published release/tag `v1.5.1`, source commit
`3e89cc62d4bb8ac59d1652b9fcf043735db68ee0`.
https://github.com/ZhiHanyu-H57/InputStitch/releases/tag/v1.5.1
GitHub workflow 39715748602: verify, publish-stable and mirror-stable-r2 all PASS;
both Beta jobs skipped. CI full 36 groups + SettingsSmoke PASS, product 6,643,
layouts 96,103 / 188 / zero issues. No production change after the local freeze.

All five downloaded GitHub assets match API digests/SHA256SUMS/manifest; website
versioned assets, latest manifest/version and both latest EXEs match. Source ZIP
matches all 57 frozen production texts. Actual published x64/x86 startup/cache/
SDL3/crosshair/exit PASS; published x64 upgrade probe 442 PASS. Original official
1.4.3 and 1.5.0 parsers accept published GitHub manifest for x64/x86; current parser
accepts GitHub and website. Final local delivery uses downloaded published bytes.

x64 SHA-256 `24b7590fb79ba4382f5165be0d4aeff1418acebe0954252baaea4721474d0056`;
x86 `84c37d8a19c386690d7ee1f2c2b97ce4168c6eddf7cfc64e28acc1394baf2c6e`.
See `docs/RELEASE_1_5_1_VERIFICATION.md` and chat delivery outputs/stable151 for
full counters/raw evidence. This follow-up changes release documentation only;
no new runtime build or regression is required. Feature work remains paused;
there is no outstanding manual-trial gate.

Final normal config/devices hashes remain unchanged. Git fast-forward checkout
only normalized LF -> CRLF in MacroRunCoordinator.cs and MainForm.AdvancedTriggers.cs;
reversing just line endings exactly reconstructs their raw acceptance freeze
hashes. The remaining 55 raw source hashes match; all 57 production texts match.
Both raw hash sets and mechanical reconstruction proof are in delivery evidence.

## Local 1.5.1 release closure evidence — 2026-10-22

User authorized full automated acceptance and GitHub Stable publication; freeze
features/architecture and fix only acceptance-found issues. Production delta from
final beta.3 is ReleaseInfo only: Version 1.5.1, FileVersion 1.5.1.3, prerelease
false. The other 56 production files retain final beta.3 hashes. No new dependency
or configuration field. Tests add original-EXE 1.5.0 fixture migration beside 1.4.3.

Local formal gates PASS: all 36 regression groups + SettingsSmoke, product 6,639,
layouts 96,103 / 188 windows / zero issues, productivity 802, ownership 303,716,
advanced dispatch 163, crosshair 172. Both packaged architectures start, repair
compressed caches, initialize SDL3, restore crosshair offsets and exit. Additional
actual-package upgrade: 442 checks from official 1.4.3/1.5.0 EXE/config, fixed
InputStitch.exe name, byte-exact backups, editor save/reopen and injected launch
failure rollback. Separate 76-check updater suite covers real isolated child
wait/restart, shortcuts and failures; update-network 29.

Real output: 81 checks / zero failures, prepared long-press 171 ms for 150 ms
threshold during 400 ms UI block (explicit delivered-input/focus seams).
Fresh sustained final-source observer PASS: 600.1 s / 4,520 cycles / 2,260 long
presses / 225 UI blocks, 3 workers, keyboard 25,941/25,941, final sources/pressed
0/0. No forced GC. See `docs/RELEASE_1_5_1_VERIFICATION.md` for counters and limits.

Local packages: x64 1,993,216 B / x86 1,823,232 B. Resource/config-hash closure PASS:
1,000 released requests queue zero UI work, idle one-core CPU off/on 3.280%/3.853%
in bounded 30-second phases; normal config/devices hashes unchanged. All 57
production hashes retain the freeze. Release through existing CI; it rebuilds/full-tests
before Stable/latest and website mirroring. Download/verify published artifacts
and smoke-test the actual released assemblies before final delivery. Normal
user configuration is protected; no manual-trial prerequisite.

Initial new 1.5.0 migration assertion wrongly assumed no-op saves created a
backup; corrected test now proves byte-exact original or changed-save backup.
Work-only packaged probe also needed explicit PowerShell reflection string
conversions. Neither required a runtime change; failed preliminary runs excluded.

## Completed local beta — 1.5.1-beta.3 (2026-10-22)

Branch `perf/runtime-coordination`; verified code/test commit `659e7f7defd9f29efa3942852135746259306c19`.
Version/FileVersion `1.5.1-beta.3` / `1.5.1.2`; manifest/csproj: 57 files.
No push/publication or Stable 1.5.0 main/tag/latest/mirror changes.

Prepared LongPress edge snapshots reuse the input pipeline for threshold/admission
and coordinator/worker pool for execution. Single-use tickets serialize admission
with release/reset/all-stop; selected stop also resets its activator. Received
panic events stop coordinated workers synchronously before original UI cleanup.
Own foreground timing/conditions, editor inspection and first device setup retain
a coalesced UI lane. Device prompts do not hold admission locks. Background own
caption reads fail closed; UI reads remain valid. Fast workers retain source-local
UI suspension and conservative own-window protection. Other activators, physical
hook registration/capture, XML and drivers retain their existing paths.

16 impact-selected suites PASS (36 available): SourceManifest, ForegroundIdentityCache,
InputPipelineScheduler/Integration, AdvancedTriggerDispatch, ActivatorCondition,
MacroRunCoordinator/WorkerPool/Timing, UiSafety, OutputOwnership, Layer/LayerContext,
Productivity, ModifierSafetyPolicy, ReleasePolicy. Latest focused dispatch 163,
caption cache 7. Beginner workflow 142, real SendInput/ViGEm/XInput 81/0 and final
x64/x86 packaged extraction/cache repair/SDL3/startup/crosshair/exit PASS.

Delivered-edge observer: 150 ms threshold / 400 ms UI block, beta.2 405 ms ->
integration 152 ms; final repeated 151..254 ms, median 152 ms.
Real XInput down at ~178 ms before UI resumes; stop releases correctly.
Physical state/focus are explicit seams, not physical-hook/game latency evidence.

Unforced observer: 600.1 s / 4503 cycles, 2252 long presses and
225 blocked UI checks. Keyboard 25704/25704, final sources/pressed 0/0,
3 worker threads created. Handles 375 -> 387, peak 387, warm/tail medians
375/378; GDI 26/26, USER 30/31, max UI dispatch 55 ms. No forced GC.
Pre-final interrupted observer excluded. Do not count preliminary runs as final passes.

After the soak, only MainForm.AdvancedTriggers.cs added current-config membership
validation, restoring the original UI rejection of late old-profile definitions.
A dedicated canary without just this guard reproduces the stale start. Final
163 dispatch checks plus affected pipeline/safety/cache/manifest checks and final
source acceptance output and packaged startup were reverified. The other 56 production files (including
engine/executor/timing) retain the soak hashes. Removing only the guard reconstructs
the exact old module hash; both source sets/delta are in verification. Per impact
policy, the unchanged 600-second valid-run path was not repeated. An earlier generic
150..299 ms canary bound failed before the profile check; it is excluded, superseded
by dedicated profile proof and output-before-400-ms-UI-recovery assertions.

x64 1,993,728 B / x86 1,823,744 B, +5,120/+4,608 B over beta.2.
No new dependency/timer/thread/configuration field. Final idle one-core CPU
3.645% / 3.230%, crosshair off/on ~30 s, 16 logical CPUs, zero visible devices;
no further idle CPU improvement claim. 1,000 released requests queue zero UI work.
Normal config/devices hashes unchanged. Chat delivery outputs/beta153 uses fixed
InputStitch.exe names; see docs/ADVANCED_TRIGGER_DISPATCH_BETA_1_5_1_3.md.

Do not claim full UI/physical-hook decoupling, all 36 suites, formal publication,
or universal long-term/game compatibility. Continue impact-based checks; formal
publication needs all release gates. No manual-trial prerequisite.


## Completed local beta — 1.5.1-beta.2 (2026-10-22)

Branch `perf/runtime-coordination`; verified code/test commit `587b3d859d248c971b239045a4c20255b547a032`.
Based on beta.1 (`1f673aa` / handoff `56e72aa`). No remote push/publication or
Stable main/tag/latest/mirror modification. Manifest/csproj still list 56 files.

Coordinator owns progress, single-step wait/advance and atomic control snapshots;
UI consumes detached diagnostic input snapshots. Per-edge synchronization no
longer builds key-name strings; names render on demand outside the run lock.
Execution, timing, ownership, device backends, UI editing safety and XML remain.
MainForm still owns the executor/condition input. UI-affine advanced triggers
remain debt: final source's 150-ms long-press sample is 187 ms with responsive UI
and 405 ms after blocking UI for 400 ms (input seam -> production scheduler ->
worker -> isolated observer, not physical/game latency). Keep the safety commit
boundary while pursuing that next P1 step.

13 impact-selected suites PASS. Coordinator 55, integration/bilingual diagnostics/
blocked UI 24, safety 26, ownership 303,716, worker reuse 1,010, productivity/old
config save 536, plus timing/layers/activators/metadata/source guards. The final
formatting lock-scope edit rechecked only affected integration and safety suites.
Final package workflow 142, x64/x86 extraction/cache repair/startup/restore/exit
and real-output 77/0 PASS. Final unforced observer: 600.0 s / 7,220 cycles / three
workers, keyboard 28,244/28,244 and zero final sources/pressed outputs. Handles
375 -> 377 (warm/tail medians 375/368), GDI 26/26, USER 30/30, max UI dispatch 69 ms.
Normal config/devices hashes unchanged. Pre-final interrupted run is excluded.

Final-package state-sync microbenchmark (3 x 200,000 updates, 8 held inputs/empty):
median 2347.42 -> 101.25 ms; Gen0 median 64 -> 6. Local path result, not whole-app
CPU/throughput. Final idle sample is 2.969% / 3.384% of one core, crosshair off/on,
~30 s each, zero visible sources; do not claim further idle CPU improvement.
x64 1,988,608 B / x86 1,819,136 B, +1,536/+2,048 B over beta.1, no new dependency.
See `docs/RUNTIME_PROGRESS_BETA_1_5_1_2.md` for hashes, scope and open boundary.

Continue impact-based development verification; no complete 35-suite claim.
Formal publication still requires the entire regression/release gates. No manual
trial prerequisite, no feature expansion or wholesale execution rewrite.

## Completed local beta — 1.5.1-beta.1 (2026-10-22)

Branch: `perf/runtime-coordination`. Verified production/test commit: `1f673aab029e94e6c5c588df22f1ff8808ba6d39`.
Based on unchanged Stable `v1.5.0` / `166faab` and main `bcb17c2`. This branch and
beta remain local; nothing was pushed or published and Stable latest/mirror were
not modified. Production manifest/csproj contain 56 files.

`MacroRunCoordinator` owns registration, RunId/SourceId indexes, single-step entry,
independent/all stop and completion with the original shared lock. Final stop
reason selection stays under that lock. MainForm still owns execution/progress,
conditions and UI safety; no whole-runtime rewrite or new feature. Advanced
activators suspend empty periodic UI dispatch after release while retaining
history. Dedicated workers, output ownership and XML rules are preserved.

Final acceptance: 13 impact-selected suites, beginner workflow 142, real output
77/0, x64/x86 packaged extraction/cache repair/startup/restore/exit PASS. Unforced
observer: 600.1 seconds / 7,040 cycles / 3 workers; keyboard 27,404/27,404 and zero
final sources/pressed outputs. Handles 365 -> 378 (warm/tail medians 373/369),
GDI 26/26, USER 29/30, maximum UI dispatch 118 ms. Normal config/devices hashes
were unchanged. A pre-final interrupted soak and overlapping initial resource
sample are excluded; do not count them as successful acceptance.

Sequential packaged comparison: 1,000 released-state requests enqueue 1,000 -> 0
UI ticks; CPU off 4.480% -> 2.969%, on 3.751% -> 2.918% of one core, about 30 s
per phase, zero visible controller sources. Bounded local samples, not universal
hardware gains. x64 1,987,072 B / x86 1,817,088 B, +512 B each, no new dependency.
Scope, final hashes and remaining work: `docs/RUNTIME_COORDINATION_BETA_1_5_1.md`.

Development now uses `tests/Run-Tests.ps1 -Suite <names>` / `-List`, plus affected
focused flows. No-argument formal CI still runs all 35 suites and SettingsSmoke.
This beta did not run the complete unrelated matrix; do not claim a 35-suite pass.
Future work follows P1: measure UI-blocked trigger latency before moving pure
condition/activator decisions. Keep UI safety, RunId/source-local stop and current
hardware/output support intact. No user trial is an acceptance prerequisite.

## Completed Stable release — 1.5.0 (2026-10-22)

Published `v1.5.0` from `166faab96f337b8164c729c1f233e6b2de294ff1` as GitHub latest Stable.
All hosted verification/publishing/mirror jobs succeeded; downloaded assets and
both official packages are verified. Local real-output 77 and 600.1-second /
6,927-cycle unforced observer PASS. 1.4.3 configuration persistence and fixed
InputStitch.exe update/restart are verified. Current source, final hashes, test
scope and excluded runs: `docs/RELEASE_1_5_0_VERIFICATION.md`.
The dated preparation notes below are historical. No manual trial gate remains.

## Stable promotion history — 1.5.0 (2026-10-22)

The user authorized a formal GitHub release of accepted beta.15. Target 1.5.0 /
FileVersion 1.5.0.15 on main. The initial promotion changed only ReleaseInfo;
the user then requested fixed InputStitch.exe naming after updates and verification
of painless 1.4.3 configuration upgrades. UpdateInstaller and its caller now install
to the fixed path transactionally, retain original/config backups, reject name
collisions, roll back rename/launch failures and retarget existing desktop/Start
menu links. Macro runtime, ownership and icon still match beta.15. Reuse the explicitly versioned beta.15 regression
and unforced soak evidence below; validate the official packages and use the
existing full-regression GitHub publishing workflow. The new request supersedes
earlier statements that beta.15 must remain local or leave Stable untouched.
The first hosted publication was blocked before any release by a synthetic-DPI
test window being clamped to the cloud desktop's native track size. Use native
offscreen sizing for synthetic viewports and assert their requested dimensions;
retain every clipping assertion and native work-area test. No product UI workaround.
Add a fixture generated by the original 1.4.3 EXE: actual isolated editor save and
reopen compare every old XML leaf and retain the original backup. Local full
34-suite regression now PASS: 6,639 product checks, 536 productivity checks,
76 updater checks including real isolated update/restart processes, 303,716
ownership assertions, 172 crosshair checks and 70,579 layout checks / 188 windows
with zero issues. Observe dropdown opening synchronously after settling activation;
retain row/width checks and run GUI acceptance serially.
Final-source observer PASS: 600.1 seconds / 6,927 cycles, 3 macro workers, no
forced GC, keyboard 26,446/26,446 and zero final sources/held outputs. Handles
369 baseline / 371 final, GDI 26/26, USER 30/30; max UI dispatch 88 ms.
Complete final package/real-output checks and hosted full regression before
publication; verify latest assets and the mirror. Failed runs remain saved and excluded.
The second hosted run likewise stopped before publication: secondary synthetic
150/200% dialogs inherited the cloud desktop's native track limit. Apply native
offscreen sizing to their exact requested client viewports and to compact sizes
derived from the original logical minimum; assert each actual size. Keep native
first-show/work-area checks separate. The enhanced local matrix passes 96,103
checks across the same 188 windows, with zero issues and additional compact
coverage. This last change is test-only; production, soak and real-output source
hashes remain frozen. Hosted full regression must pass before publishing.
The third hosted matrix had no clipping reports but four full-keyboard viewport
assertions: its 1,240-pixel constructor size had already been clamped before the
fixture captured it. The full-keyboard test now explicitly owns a 1240x600 logical
viewport, independent of desktop size. Native first-show/work-area tests are
unchanged. Focused keyboard validation PASS: 32,039 checks / 20 windows, zero
issues. This is still test-only; no further production rebuild or behavior change.
Release result and exact asset hashes will be recorded afterward.

## Completed pass — maturity and measured runtime performance (beta.15, 2026-10-22)

Local beta.15 / FileVersion 1.5.0.14 on `ui/product-refresh`. Keep Stable 1.4.3,
beta.14 visuals, existing features and XML semantics. The current official-source
comparison, gap assessment, fixes and next priorities are in
`docs/MATURITY_AND_PERFORMANCE_REVIEW.md`; dated earlier architecture gaps are
marked historical rather than being repeated as current missing features.

- Empty XInput slots retry every 500 ms; connected inputs remain at input cadence,
  disconnect release is immediate, reconnect establishes a held-input baseline,
  and own-slot changes invalidate the backoff. Explicit injected-clock coverage.
- Interruptible deadline input waiting with a per-object Windows high-resolution
  timer, no busy tail/global resolution change, and a tested framework fallback.
- Cache Composite membership by each child's topology version, atomically publish
  complete owner/index snapshots and preserve live state/edge polling. Stable SDL3
  metadata is not reread each frame; gamepad update pumps once with joystick fallback.
  Skip empty advanced-activator UI requests; retain active coalesced dispatch.
- The unforced soaks exposed per-run CLR thread bookkeeping costs. Add
  `MacroWorkerPool` / per-run `MacroWorkItem`: one dedicated worker per concurrent
  macro, at most four sleeping spares, no shared-CLR-pool throttling. Preserve
  independent snapshots/RunId/SourceId/stop signals. Config changes wait for run
  completion; shutdown/disposal stops and joins the owned workers within a bound.
  Production manifest and csproj now list 55 files. Module tests have 1,010 checks;
  isolated integration covers rapid restarts, source-local release and config changes.
- Keep forced GC out of acceptance. Retain the failed measurements and independent
  empty-Thread/Join reproduction. The final implementation restores the original
  end-point +32 handle gate, plus median/peak/GDI/USER limits and a worker-create cap.

Final-source sustained acceptance PASS: 600.1 seconds / 6,939 cycles, only 3 macro
worker threads created; keyboard down/up 26,571/26,571, zero final sources/held
output. Handles 368 baseline / 371 final / 371 peak; GDI 26/26, USER 30/30.
Private bytes 34,230,272 / 35,688,448; max UI dispatch 140 ms. No forced GC.
The observer backend is isolated; actual output is verified separately.

Packaged startup resource comparison, one source on this 16-logical-CPU host,
5-second warmup and about 30 seconds per phase: single-core CPU 23.229%→4.269%
with crosshair off and 23.188%→3.905% on. The minimal managed host loads the real
package and production providers/hooks; these are bounded local samples, not
competitor or whole-machine percentages. x64 1,984,000 bytes, x86 1,814,528 bytes;
about 1% larger than beta.14, no new third-party runtime. The CLR string heap
crosses the 64 KiB index boundary; see the review for download/cache/RAM distinctions.

Final-source full regression PASS: 34 suites, 6,623 product assertions, 303,716
ownership assertions, 172 crosshair checks, and 70,499 layout checks across 188
windows with zero issues. Final real-output InputLab PASS: 77 checks, after the
user authorized normal shutdown of their elevated beta.14 instance. Both final
packaged architectures pass startup, native extraction/cache repair, restored
crosshair and orderly shutdown checks. All 55 production source hashes and the
icon match the freeze; source-only repacking preserves the tested EXE hashes.

An earlier 77-check run occurred while the user's beta.14 was active and is
excluded. The runner now blocks both unversioned and versioned product processes
before emitting input. The active old app refreshed its derived device inventory
during that earlier virtual-device test; normal macro config stayed unchanged.
After its authorized normal close, the qualifying isolated rerun leaves both
normal config and device inventory unchanged. Do not overwrite the live inventory
with an old snapshot. Raw records and hashes accompany the local delivery receipt.
User trial remains optional; no remote release or Stable replacement.

## Previous completed pass — complete visual audit and DCET diamond (beta.14, 2026-10-21)

Local beta.14 / FileVersion 1.5.0.13 on `ui/product-refresh`. Preserve stable
macro/controller/crosshair behavior, XML compatibility and 10.5pt body text.

- Larger vector gear with an accent background and keyboard focus. Tools,
  language, library and input-method menus share readable body text and padding.
  Owner-drawn dropdowns retain readable selection contrast and scaled row height;
  measure all options, and expand editable device lists without restricting text.
- Expose diagram selection and physical key/mouse capture as two adjacent step
  buttons beside the current selected input. Restore listening on Escape,
  Emergency Stop and switching to the diagram; input selection emits no output.
  Preserve key chords and all existing execution semantics. Ordinary key previews
  are square; longer names/chords get bounded extra width. Owned dialogs reuse
  the application icon.
- Shorten Settings' language and idle-controller fields; controller choices no
  longer repeat direction/strength text. Keep English macro sort buttons on one
  row, scale their targets with DPI, and let Stop all fit its actual text.
  Retain the outlined, tinted advanced settings frame and visible blocking.
- Audit geometry and enlarged fonts at synthetic 100/125/150/200%. Size keyboard
  geometry from actual fonts; keep device grid rows fitted, useful router-list
  capacity and complete scroll extents for Quick Create and Crosshair settings.
  Fix Crosshair's implicit auto-size body row, which could extend its tabs beyond
  the viewport at large fonts. Default windows retain useful list space; compact
  constrained windows scroll internally with a reachable footer.
- Replace the cat with a DCET-inspired diamond using the supplied silhouette
  and pink-violet-cyan direction. Omit lettering. The final fill is one continuous
  bright gradient with light facet lines and no dark center. Seven native sizes
  (16 through 256 px), transparent exterior; 49,136 bytes versus beta.13's 51,620.
  Native Windows LoadImage/DrawIcon comparisons and PNG reconstruction prove the
  optional lossless optimization preserves pixels. Reproduction uses framework
  drawing APIs; the optional optimizer uses Python's standard library. Normal
  builds consume the committed ICO with no new dependency.
- Reuse/shorten private presentation names to avoid wider CLR metadata indexes;
  keep public and serialized names intact. Exact final package sizes are in the
  delivery receipt. No new product functions or runtime dependencies.

Final acceptance: 32 suites PASS, 6,623 product assertions, 172 actual
crosshair checks and 303,716 output-ownership assertions. Synthetic geometry plus
fonts: 70,499 checks / 188 windows;
native 144 DPI: 24,434 / 50.
Native field/menu checks 1,086; full beginner and both input
methods 142. Real SendInput and ViGEm/XInput 77/77 PASS.
Current-source sustained observer: 300.1 seconds /
3,441 cycles; keyboard down/up
13,435/13,435; zero final sources/held output.
Full regression includes `--scaled-fonts`; inaccessible oversized compact
geometries remain explicit CLAMPED records. Final startup/extraction/cleanup,
source/asset/config hashes, icon checks, sizes and commit are in verification.json.
No remote release or Stable replacement. User trials remain optional feedback.

## Previous completed pass — compact fields and clear disclosure (beta.13, 2026-10-21)

Local beta.13 / FileVersion 1.5.0.12 on `ui/product-refresh`. Preserve all
macro/controller/crosshair functions, configuration semantics and 10.5pt text.

- Keep trigger blocking beside repeat controls, outside More options. The
  tooltip retains the exact final-key/mouse-event semantics and existing Shift
  and controller pass-through exceptions. Move the disclosure button to the
  trigger row when necessary to preserve useful step rows in narrow English UI.
- Put both layer selectors, management and conditional run-target controls in
  one tinted, outlined More options frame. Its button has a matching expanded
  state and accessible description. Layer selectors size to their contents.
- Shared native choice controls measure their options, cap ordinary width,
  keep expanded dropdowns wide and provide full-text tooltips for unusually
  long selections. Native selection, keyboard navigation and accessibility
  remain intact. Free-text notes/process/title fields retain useful full width.
- Compact step/device/control choices and the read-only key field. Give the
  existing vector controller preview about 40% of the step-editor body width;
  keep direction/strength labels and values together when wrapping.
- Losslessly re-encode the six existing PNG icon images: 54,083 to 51,620
  bytes. All sizes and color profiles remain, decoded pixels match both the
  PNG rows and Windows Icon/Bitmap output. No new build/runtime dependency.
  Net EXE sizes and changes from beta.12 are measured in the delivery receipt.
- Shorten private UI identifiers and reuse existing metadata names to keep the
  CLR string heap below 64 KiB; this avoids 18,432 bytes of wider metadata-table
  indexes with the same functionality. UI text and serialized names are intact.

Final acceptance: all 32 suites PASS; 6,505 product checks, 172 actual
crosshair pixel checks and 303,716 ownership assertions. Bilingual native
144-DPI compact-field/disclosure/preview checks: 1000. Secondary layouts:
69,651 checks / 188 windows; native 144 DPI:
24,216 / 50. Synthetic scales 100/125/150/200% retain
explicit CLAMPED records for inaccessible compact geometries.
Real SendInput and ViGEm/XInput 77/77 PASS. Current-source sustained observer:
300.1 seconds / 3,658 cycles, keyboard down/up
14,113/14,113, zero final sources/held outputs.
Exact startup/extraction checks, source/asset/config hashes, package sizes and
source commit are in verification.json. No remote release or Stable replacement.
User trials remain optional feedback, not an acceptance prerequisite.

## Previous completed pass — beginner workflow (beta.12, 2026-10-21)

Local beta.12 / FileVersion 1.5.0.11 on `ui/product-refresh`. No new features,
dependencies or assets; retain 10.5pt text and compressed single-file packaging.

- Walk through both quick and blank creation, naming, shortcut selection and
  capture, trigger mode/repeat, nested step selection, edit/cancel, automatic
  persistence, reopening, Run and shortcut dispatch. Keep shortcut-off manual Run.
- Keep saving/run guidance visible in the existing readiness row after steps
  are added. Replace runtime implementation terms in ordinary explanations;
  use consistent step wording and explicit Create and save. Correct the empty
  keyboard hint to point above, and clarify the physical-input menu.
- Adding the first step could leave Run disabled until an unrelated focus or
  safety-state change. RefreshSteps now updates Run directly, also disabling it
  after the last step is deleted and restoring it on Undo. The full journey
  reproduced this defect; focused and full-suite evidence are retained.
- Quick create previously used single-step capture for shortcuts, losing
  modifier chords. Its capture now reuses the existing trigger conversion and
  modifier-release semantics, with a dialog-owned callback; it cannot overwrite
  the currently selected macro. Escape and Emergency Stop retain the previous
  shortcut and restore the capture button. Emergency Stop retains priority.
- Validate reversed random waits and unsupported wheel actions inline before
  saving; keep existing validation guards. Disable empty-list edit/delete,
  support the existing edit action on double click, and preserve the edited row.

Final-source acceptance: full 32 suites PASS, 5,487 product assertions,
172 actual crosshair pixel checks and 303,716 ownership assertions. Bilingual
beginner/capture journeys also PASS at native 144 DPI (110 checks).
The secondary layout matrix passes 69,571 checks / 188 windows,
plus 24,192 / 50 windows at native 144 DPI. Other scales are
synthetic 100/125/150/200%; unavailable compact sizes remain logged CLAMPED.
Real SendInput and ViGEm/XInput: 77/77 PASS. Current-source sustained observer:
300.1 seconds / 3,437 cycles, keyboard down/up
13,036/13,036, zero final output sources and
held outputs. Exact resource samples, package startup/extraction receipts,
source/config hashes, sizes and source commit are in the delivery receipt.
No remote publication or Stable replacement; user trial is optional feedback.

## Previous completed pass — secondary-window density and DPI (2026-10-21)

Local beta.11 / FileVersion 1.5.0.10 on `ui/product-refresh`. Inspect Settings,
timing/conditions, step editing, key selection, export, Router/analog controls,
device inventory/takeover, crosshair settings, diagnostics/about, quick/new
macro creation and the inline group-name editor. No new functions/dependencies
or raster assets; retain 10.5pt text and compressed runtimes.

- Measure intro height and tab titles; reduce section padding and excess space
  in simple dialogs. Paragraphs use available width. Device columns fill the
  viewport with wrapped headers. Native checked lists gain font-aware padding
  while retaining keyboard and mouse checking. Numeric fields fit supported
  values and spinners. Scope their sizing to secondary windows to preserve the
  accepted main-editor repeat group.
- Normalize initial logical window sizes after native DPI initialization and
  fit/center dialogs in the working area. Size section content explicitly and
  coalesce the idle-panel height update after native layout; switching tabs or
  resizing must not leave a stale card height. Small step-editor viewports have
  a complete internal scroll extent and a fixed reachable footer.
- Keyboard geometry follows font scale and actual key minimum heights. Default
  1240x600 client size shows the complete keyboard without scrolling, including
  the bottom modifiers; compact windows retain the keyboard's scroll viewport.
- Full 32 suites PASS: 5,377 original product workflow/layout checks, 172 native
  crosshair checks, 303,716 ownership assertions and the secondary-layout suite.
  Supplemental final harness adds the inline group-name cancel/visibility path.
- Final bilingual secondary matrix: 69,523 assertions across 188 windows;
  native 144 DPI: 24,174 assertions across 50 windows; zero issues. Check
  first show, every tab, compact/restored bounds, ancestor clipping, text height,
  closed dropdowns, number/spinner fit, list row capacity and actual native
  checkbox keyboard/mouse behavior. Synthetic scales: 100/125/150/200%.
  Oversized synthetic windows hit host track limits; unavailable compact cases
  are logged CLAMPED and are not counted as validated compact sizes.
- Real SendInput and ViGEm/XInput 77/77 PASS. Final-source macro observer:
  300.0 seconds / 3,472 cycles; keyboard down/up
  13,198/13,198, final output sources/held outputs zero.
  Max UI dispatch 100 ms, continuous gap 63 ms;
  private bytes 36,409,344 to 37,666,816, handles
  435 to 429, threads 13 to 6.
- All 54 production files frozen for final acceptance; normal user config and
  devices hashes unchanged. Renderer core is byte-identical to beta.8: retain
  its historical sustained overlay report and rerun current pixel checks.
- Final x64/x86 package startup, extraction, sizes, hashes and source commit are
  recorded in the delivered receipt. No remote release, Stable replacement or
  per-round manual trial gate. Windowed/borderless crosshair scope is retained.

## Previous completed pass — compact responsive macro settings (2026-10-21)

Local beta.10 / FileVersion 1.5.0.9 on `ui/product-refresh`. One selectable
shortcut control displays the current key and retains the existing capture
menu. Trigger modes use measured translated widths. Field labels and controls
wrap as complete groups at narrow widths. Count, unit and infinite repeat stay
adjacent; infinite repeat disables the count while preserving its finite value.
Readiness/errors appear directly in the execution area. Reduce title spacing
while retaining 10.5pt text, compressed runtimes and all current behavior.
No new features or dependencies; exclusive-fullscreen work stays deferred.

- Full 31 suites PASS: 5,377 workflow/interaction/layout assertions,
  172 crosshair checks and 303,716 output ownership assertions. Settings/XML
  smoke PASS. Initial full-suite attempt exposed a test helper calling a public
  method through a private-only reflection helper; fix the driver and rerun the
  entire suite, with production sources unchanged. Failed-attempt logs retained.
- Focused synthetic 100/125% density/live-transition checks: 3,316 PASS.
  Native 144 DPI: 1,660 PASS, including actual first-show working-area
  fit. The full bilingual layout matrix also covers synthetic 150/200%.
  Live resize/language transitions include a full Ctrl+Shift+Alt+Win+F8 chord,
  unset shortcut prompt, complete mode text, intact repeat grouping and parent
  clipping. Test renders create native handles and settle layout before capture.
- At 100%: 1000x700 exposes Chinese/English 6/5 complete step rows;
  1160x780 both 9, 1280x900 both 13. No normal outer/horizontal scrolling.
  Native 150% at physical 1500x1050 gives 11/9, 1740x1170 both 14 and
  1920x1350 both 16. Oversized synthetic windows may hit host track limits;
  actual tested dimensions are recorded, not universal monitor guarantees.
- Real SendInput and ViGEm/XInput: 77/77 PASS, zero failures.
- Final-source macro sustained observer: 300.1 seconds / 3,565 cycles;
  keyboard down/up 13,689/13,689, zero final output sources/held
  outputs. Max UI dispatch 147 ms; continuous gap 220 ms.
  Private bytes 35,901,440 to 36,679,680; handles
  434 to 430, threads 13 to 6.
- All 54 production files frozen for acceptance; normal config/devices hashes
  unchanged. Renderer files still match beta.8 exactly; its sustained overlay
  report is historical evidence, while current-version pixel tests are rerun.
- Final x64/x86 startup/extraction receipts, exact EXE sizes, source commit and
  hashes are in the delivery receipt. No remote release or Stable replacement.
  No per-round manual trial requirement.

## Previous completed pass — density and lightweight packaging (2026-10-21)

Local beta.9 / FileVersion 1.5.0.8 on `ui/product-refresh`. User priority is
readable, usable and clean before decoration. Restore balanced 10.5pt body text,
compact shared dialog headers/spacing and one ordinary settings block. Let the
step table use remaining space. Fix per-row text fit, measured header widths,
font-aware sidebar/list/status/toolbar sizing and clipping by parent containers.
Quick-create Actions uses available height and shows hold/wait timings inline.
Default main-window geometry fits the actual working area.

- Runtime feature expansion stays paused. Macro execution/ownership semantics,
  configuration formats, Emergency Stop and editing protection are retained.
  Crosshairs remain windowed/borderless only; exclusive fullscreen is deferred.
- SDL3 and managed ViGEm dependencies use gzip-compressed embedded resources
  with .NET Framework classes, raw-resource compatibility and native pinned-hash
  extraction. No dependency/function was removed. Extracted cache size stays
  unchanged; binary/download savings must not be described as cache savings.
- Full 31 suites PASS: 1,315 product checks, 172 crosshair checks, 303,716 output
  ownership assertions; bilingual Settings/XML/vector smoke PASS. Subsequent
  focused all-command density checks: 1,108 at synthetic 100/125%, and 556 at
  native 144 DPI, including actual first-show working-area fit.
- Full visible step rows at 100%: 5 / 7 / 11 for 1000x700 / 1160x780 / 1280x900;
  native 150%: 9 / 12 / 16 for physical 1500x1050 / 1740x1170 / 1920x1350.
  Tests check no ordinary outer/horizontal scroll, text height, clipping by
  every ancestor and direct access to all editing/record/run actions.
- Real SendInput and ViGEm/XInput: 77/77 PASS, zero failures.
- Final source sustained macro observer: 300.1 seconds / 3,421 cycles;
  keyboard down/up 12,948/12,948, final sources/held outputs zero;
  max UI dispatch 115 ms and continuous-output gap 59 ms.
- 54 production sources unchanged during final acceptance; normal user config
  hashes unchanged. Renderer code matches beta.8 exactly; reuse its sustained
  native-overlay report as historical evidence and rerun beta.9 pixel checks.
- Final x64/x86 packaged startup, saved crosshair restoration, shutdown,
  compressed dependency/native extraction checks, exact sizes and commit are
  recorded in the delivered verification receipt. No remote release or Stable
  1.4.3 replacement, no manual user-trial gate.

## Previous completed pass — windowed/borderless crosshair closure (2026-10-21)

User requested a usable beta limited to windowed and borderless modes after the
exclusive-fullscreen experiment failed physical visibility. Local beta.8 /
FileVersion 1.5.0.7 restores the beta.6 desktop implementation. It retains eight
styles, all appearance/placement controls, live preview, saved settings/visibility
and editable Ctrl+Alt+F10 shortcut. No Game Bar renderer, widget resources,
installation flow or fullscreen tab ships. The settings and release summary
explicitly state that exclusive fullscreen is unsupported in this build.

- Current branch: `ui/product-refresh`; exact delivery source commit is in the
  machine-readable verification receipt. No remote publication; Stable 1.4.3
  remains unchanged. Unrelated feature work is still paused.
- The failed fullscreen experiment was archived outside the shipping repository,
  and the local experimental widget was uninstalled. Internal widget rendering
  and PNG transport are diagnostic only; they are not fullscreen acceptance.
- 31 regression suites PASS: 903 macro workflow/layout checks, 172 crosshair
  checks, 303,716 output-ownership assertions. Actual desktop pixels over both
  windowed and full-display borderless native targets pass fill/50%-alpha,
  hit-test pass-through, focus, centering, +13/-11 offsets and visibility checks.
- Packaged Settings smoke initially lacked the newly named beta.8 EXE. It passed
  after building and a focused rerun; retain both logs. Production code stayed
  frozen throughout all acceptance, including the retry.
- Real SendInput and ViGEm/XInput acceptance: 77/77 PASS, failures=0.
- Macro sustained observer: 300.0 seconds / 3,533 cycles; keyboard down/up
  14,239/14,239; zero final output sources/held outputs; max UI dispatch 131 ms,
  max continuous-output gap 159 ms.
- Actual native overlay: 300.1 seconds / 9,880 changes; GDI 15 to 15, handles
  287 to 284, USER 14 to 13; private bytes 29,044,736 to 30,461,952;
  max dispatch 133 ms. Thirty-second warmup, startup samples retained, no forced
  GC in the overlay test, native-window disposal verified.
- x64/x86 build, Release Verification and isolated packaged startup, saved
  enabled crosshair/offset recovery, message loop and orderly cleanup PASS.
- All 54 production sources unchanged during acceptance; normal config.xml and
  devices.xml hashes match the pre-fullscreen snapshot. User trial is optional.
- Measured sustained tests are five minutes each; additional physical monitors,
  individual game compatibility and exclusive fullscreen are outside this result.

## Previous completed pass — screen crosshair (2026-10-21)

Explicit user request authorizes a bounded crosshair feature after beta.5.
Implemented eight vector styles, size/width/gap/dot controls, outline/colors,
opacity, monitor selection, full-display center + physical-pixel offsets, reset,
live panel/screen preview with rollback, persistent settings and visibility,
default Ctrl+Alt+F10 with editable/clearable shortcut, conflict feedback and
native click-through/non-activating layered drawing. Macro dispatch and existing
Emergency Stop/capture/recording/editing priority are retained. Other feature
work remains paused. See `docs/CROSSHAIR.md` for research, scope and commands.

- Verified production-code commit: `ceb1320` on `ui/product-refresh`.
- Local `1.5.0-beta.6` / FileVersion `1.5.0.5`; no remote publication and no
  change to the published Stable 1.4.3 release.
- Full regression **31 suites PASS**: 903 existing macro workflow/layout checks,
  **162 crosshair checks**, 303,716 output-ownership assertions, bilingual
  Settings smoke, legacy config defaults and saved controller vectors.
- Native PerMonitorV2 target tests PASS: actual fill/50% alpha desktop sampling,
  hit-test click-through, no foreground activation, full-display centering and
  +13/-11 physical-pixel offsets. Native display presentation is awaited before
  capture. Negative monitor origins and edge clamps have deterministic checks.
- Actual main-entry Save/Cancel, screen preview rollback, saved visibility,
  cleared shortcuts, conflict feedback, editing protection, Emergency Stop
  priority and preserved global settings on macro-profile loading PASS.
- Real-output Input Lab **77/77 PASS, failures=0**, actual SendInput and
  ViGEm/XInput. No new physical-controller or game compatibility claim.
- Macro sustained observer **300.1 seconds / 3,400 cycles PASS**;
  keyboard down/up 13,092/13,092; final sources/pressed outputs zero;
  maximum UI dispatch 120 ms, continuous-output gap 99 ms.
- Actual native crosshair sustained run **300.1 seconds / 9,114 iterations PASS**;
  all styles, visibility and opacity changes at the screen edge. GDI objects
  15 to 15; handles 347 to 345;
  private bytes 29708288 to 30834688; max dispatch 12 ms.
  These resource comparisons use a 30-second warmup; ten-second samples include
  initialization. USER objects 34 to 32; native window
  destroyed at completion, with no forced garbage collection.
- x64/x86 build and Release Verification PASS; normal constructor/startup with
  saved crosshair enabled and X/Y offsets restores the window; orderly shutdown
  destroys it. Tests use isolated app directories and disabled macros.
- All 54 production sources unchanged during final acceptance; normal user
  config.xml/devices.xml hashes unchanged. No manual trial-run gate.
- Exclusive fullscreen remains outside the universal compatibility claim.
  Real additional-monitor hardware was not required to establish the ordinary
  feature behavior; simulated monitor geometry is reported as simulation.

Current local target is beta.8 above; the beta.6 and UI records below are historical.

## Latest completed pass — larger text and readability (2026-10-20)

User feedback was that text was too small. Shared body/forms/buttons now use
12pt instead of 10pt, virtual key labels 10pt instead of 8pt, and diagnostics
11pt instead of 9pt. Buttons, rows, field columns, headings and default editor
sizes accommodate the larger text. Controller preview captions retain the
normal interface font and stay adjacent to the scaled diagram. This pass does
not change the executor, ownership rules, XML or existing macro semantics.

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

The readability pass is complete locally. Current local target is beta.5;
previous beta.4/beta.3/beta.2 evidence below remains historical.

## Latest completed pass — remaining dialogs and first run (2026-10-20)

Unified Settings, Timing/Conditions, key selection and secondary dialogs with a
shared frame and persistent actions. Relevant-only timing, optional analog limit
fields, examples, inline selection validation, staged settings/defaults and main
next-step guidance improve the existing product without adding runtime features.
Keyboard selection is the primary editing action; physical/mouse capture remains
available in the dropdown. Group-name prompts retain modal output protection.

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

The requested pass is complete and committed locally. Older beta.3/beta.2
sections below retain historical evidence; current local target is beta.4.

## Latest completed pass — 2026-10-20 visual design and interaction

The user's latest direction is further art and friendly interaction work.
Implemented purpose-card creation, prominent New macro, one import/backup menu,
stitched vector branding and rounded hierarchy, guided empty steps, real-time
step explanations and quick-create readiness guidance. Native button keyboard
and accessibility behavior are retained; underlying execution policies are intact.
The final empty-step view and table are mutually exclusive, including renders.

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

The new pass is complete and committed locally. The previous beta.2 sections
below are historical evidence; current runnable local target is beta.3.

## Completed locally — 2026-10-20 product refresh

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


## Active work — 2026-10-20 product refresh

Branch: `ui/product-refresh`, based on clean `7dcea05` / matching remote main.
The user authorized implementation and runnable artifacts with verification.
They confirm Stable 1.4.3 is long-used and reliable in official features; user
manual trial runs must no longer gate routine acceptance. New features paused.

Unified trigger selection, editor styling/copy and optional grouping are
implemented. XML and the execution/ownership design stay compatible. Isolated
test windows now establish their dispatcher handle before workers can update UI.
Product workflow/layout tests and a configurable sustained runtime host are in
`tests/ProductAcceptanceTests.cs`. The local package is `1.5.0-beta.2`, not a
published release. Final verification evidence is pending below; do not mistake
historical test counts in this file for this build's result.

See `docs/PRODUCT_REFRESH.md` and `docs/AUTOMATED_ACCEPTANCE.md` for the current
scope and repeatable commands. All older milestones below are history.


# Current Development State

Updated: 2026-09-21

## End of day 2026-09-21 — local-only development wave

The working tree is clean on `main` and the wave is published as the opt-in GitHub prerelease
**`v1.5.0-beta.1`**; ordinary users still run Stable `v1.4.3` and `releases/latest` remains `v1.4.3`. Every
commit passed the full `tests/Run-Tests.ps1` regression and the x64/x86 `build.ps1` release verification; the
refactor-only commits produced byte-identical executables.

The SDL3 runtime follows the agreed on-demand model: `scripts/Restore-Sdl3.ps1` fetches the pinned 3.4.16
assets during the build and `build.ps1` embeds the per-architecture `SDL3.dll` into the executable as
`InputStitch.Sdl3.<arch>.dll` (x64 build 3,729,408 bytes, x86 3,244,544 bytes). At runtime the provider
verifies the embedded copy against the pinned SHA-256, extracts it to
`%LOCALAPPDATA%\InputStitch\runtime\<arch>\SDL3.dll` and loads it from there; nothing is installed for the
user and the download remains a single EXE. `-SkipSdl3` builds a leaner executable with the lane disabled.

### Publication evidence (`v1.5.0-beta.1`, 2026-09-21)

- tag/release: `v1.5.0-beta.1`, `prerelease=true`, `draft=false`, target `main`, five expected assets;
  `releases/latest` still resolves to `v1.4.3` and the Stable fingerprint is unchanged;
- workflow run `35621100071`: `verify` PASS, `publish-beta` PASS, `mirror-beta-r2` PASS, while
  `publish-stable` and `mirror-stable-r2` were correctly skipped; Input Lab run `35621100228` also passed;
- published x64: 3,730,944 bytes, SHA-256 `a36e5cbc39efbbdc76cf66b643187acf814c9b6e652f1217af9e2c69ea28dafa`,
  ProductVersion `1.5.0-beta.1`, FileVersion `1.5.0.0`;
- published x86: 3,246,080 bytes, SHA-256 `e697c83b4545c08fc667b5ca127a0240bfd2ddf8f578f07b8377cb0a2cd63077`,
  same version metadata;
- published `InputStitch-beta.xml` SHA-256 `b2915382549d2673ad6948a6b00e9b20395cdf6309c1beeb23dffc8ef070d349`,
  `SHA256SUMS.txt` SHA-256 `8803085641fe25783c951beba0330db766465a45805386dd17ab87d37c4fb0c0`;
- the GitHub assets downloaded for verification match both the manifest and the checksum file, and the R2
  mirror serves the same manifest and file hashes at
  `https://download.zhihanyu.com/releases/v1.5.0-beta.1/...` (x64 download re-hashed to the same value);
- the Beta tag was created and pushed by `scripts/Publish-Beta.ps1` from the verified workflow commit, and no
  existing release was overwritten.

Highlights of the wave: architecture review + long-term plan, the single production-source manifest with its
drift guard, the foreground-identity cache, the dedicated input-pipeline thread with cadence statistics,
layers (momentary switching + context conditions) and the Short Press activator, five `MainForm` decomposition
steps (12,171 -> 8,126 lines in `InputStitch.cs`), and an end-to-end optional SDL3 input lane
(provider seam, provider logic, dynamic native binding verified against the real 3.4.16 runtime, MainForm
composite wiring, device-identity evidence, Device Manager labelling, an explicit Settings switch, and
`scripts/Restore-Sdl3.ps1` for the runtime).

Open items are listed in `docs/DEVELOPMENT_PLAN.md`: the SDL3.dll shipping decision, whether to push/publish,
code signing, repository licence, a physical controller for the hardware lane, and (low priority) whether
GameInput is ever adopted. The next non-decision work is splitting the remaining kernel types, an Input Lab
provider column, and the stage-2 activator designs.

## Current versions

- Current Stable: `v1.4.3`
- Current Beta: `v1.5.0-beta.1` — opt-in GitHub prerelease published 2026-09-21 from this wave; it never replaces `releases/latest` and does not touch the Stable updater channel.
- Previous prerelease: `v1.3.1-beta.1` — public GitHub prerelease, immutable
- Latest historical prerelease: `v1.3.1-beta.2` — published successfully and immutable
- Branch: `main` after the reviewed 1.4.3 keyboard-hotkey reliability release; the former `refactor/no-functional-change-1` work remains incorporated in this Stable line.
- Previous Stable rollback reference: `v1.4.2`
- Developer test tool: `Input Lab v0.3.1` — independent version; source-built/packageable x64 viewer.
- Historical `v1.3.0-beta.1` / `v1.3.0-beta.2` / `v1.3.1-beta.1` / `v1.3.1-beta.2` releases are immutable.

For the commit containing this handoff itself, use:

```bash
git log -1 --oneline -- HANDOFF.md
```

Do not put a self-referential commit hash in this file.

## 2026-09-11 keyboard hotkey reliability hotfix

A real GTA V session exposed a post-startup failure mode in Stable `v1.4.2`: keyboard-triggered macros could stop responding globally while the main-window Run button continued to execute the same macro normally. Diagnostics still reported `Hooks: 已安装`, but runtime trace stopped receiving new `trigger-accepted` events. The issue reproduced with GTA and InputStitch at the same High integrity level, so this was not an elevation/UIPI mismatch.

The fix was submitted in code commit `e0ef345a0d99df8a6b87fc172fdabe96a9a35c11`. The hotfix was deliberately rebuilt from the clean `v1.4.2` tag rather than from an unrelated local development tree; the submitted code changes are layered onto the current remote `main` tree so existing post-tag documentation/workflow updates are preserved.

### Root cause / failure model

InputStitch's keyboard trigger path uses `WH_KEYBOARD_LL`. Windows can stop delivering low-level-hook events without giving the application a reliable "hook was removed" notification, so a non-zero managed hook handle is not a health check. In the observed failure, the ordinary hook path stopped seeing real keyboard edges while an independent Raw Input lane continued to observe physical keyboard activity.

### Hotfix design

- Added `RawKeyboardFallback`, registered as passive keyboard Raw Input with `RIDEV_INPUTSINK`.
- The existing low-level keyboard hook remains the primary lane and continues to own trigger suppression when healthy.
- Raw Input edges are de-duplicated against recent low-level-hook observations. Only a missed physical edge enters the fallback path.
- Fallback input reuses the existing `HandleTerminalInputCore` / release path; it does not introduce a second macro engine or bypass Layer, UI safety, Emergency Stop, concurrency, or Output Ownership semantics.
- Key-repeat is filtered at the Raw Input edge tracker so one physical press does not become repeated fallback starts.
- After a missed release and only when all Raw Input keyboard keys are up, hook recovery is deferred to the next WinForms message turn and rate-limited with a 5 s cooldown.
- Recovery reinstalls only the keyboard hook. The mouse hook and mouse suppression/Held state remain intact.
- Diagnostics / Runtime Observation now expose Raw Input observations, fallback edges, keyboard-hook recoveries, and fallback cases where requested trigger suppression could not be applied.
- Important limitation: if the primary hook already missed a physical key edge, Raw Input can still recover macro triggering, but it cannot retroactively prevent that already-delivered original key from reaching the foreground application. Normal healthy-hook suppression behavior is unchanged.
- `tools/InputLab/build-acceptance.ps1` was also brought up to date with the current production source list. The upstream v1.4.2/current-main script had fallen behind later source-file splits and could no longer compile the acceptance host even before this hotfix.

### Verification evidence

Final clean-tag hotfix verification on 2026-09-11:

- x64 + x86 Release build and Release Verification: PASS;
- keyboard/trigger suite: **329 PASS**;
- Output Ownership: **303,716 PASS**;
- all remaining regression suites, updater/network tests, UI safety, productivity, zh-CN/en-US Settings smoke and legacy-config smoke: PASS;
- current-main Input Lab black-box acceptance: **77/77 PASS, failures=0** with real output enabled;
- GTA V physical-key acceptance: the affected physical `C` trigger successfully started the configured macro after the hardened build was launched;
- the earlier recovery-storm prototype was rejected during testing; the final design waits for all keys up, defers recovery, rate-limits it, and reinstalls only the keyboard hook.

No `v1.4.2` Release asset/tag was modified. This fix is now published as new immutable Stable `v1.4.3`; `v1.4.2` remains the rollback reference.

## 2026-09-11 controller hot-plug / takeover UI incident

A later GTA V session initially looked like a severe controller-takeover failure: the takeover dialog showed no controller entries, Settings reported the InputStitch virtual Xbox controller in XInput slot 0, yet the already-running GTA process ignored virtual-controller output.

Investigation separated two independent issues:

- the takeover dialog list is intentionally an **external/original-controller candidate list** populated by HidHide; it is not a list of every controller and deliberately excludes InputStitch's own ViGEm output. HidHide is not installed on the tested desktop, so that list was necessarily empty. The old UI did not make this distinction clear enough;
- the virtual controller itself was healthy. Windows PnP reported the ViGEm Xbox 360 device, an independent XInput probe saw it in slot 0, Idle Gamepad produced a real `LY=-32768` report, and directly starting the existing `手柄跑80%` macro produced `LY=26214` (~+80%) on XInput 0 and returned to neutral after stop;
- the failing GTA process had started at **19:49**, while InputStitch restarted at **23:05** after the 1.4.3 update and recreated its ViGEm controller. Steam detected the newly connected Xbox 360 virtual device, but the already-running GTA process still ignored it. After GTA was restarted at **23:23**, with InputStitch/XInput 0 already present, the user confirmed controller input was normal. This is therefore a target-game XInput hot-plug/reacquisition limitation, not a false slot report or broken ViGEm output path.

Post-release main commit `486b25000d2108b9abd9ca815dd09a0f9cef58ff` adds UX/diagnostic hardening without changing takeover/routing/output semantics:

- the takeover window now shows InputStitch virtual-output connection/type/XInput slot separately;
- the list is explicitly labeled as external controllers available for takeover and says the InputStitch virtual controller is intentionally excluded;
- when HidHide is absent, the window explicitly explains why the external-controller list is empty and that ordinary virtual output / non-hiding Router use can still work;
- when InputStitch creates/recreates a virtual controller while the configured target process is already running, status/diagnostics/logging warn that some games do not reacquire hot-plugged XInput devices and may need a game restart;
- diagnostics now expose `VirtualGamepadHotplug` for later support cases.

Verification for `486b2500`: local x64/x86 build + Release Verification PASS; full `tests/Run-Tests.ps1` PASS including 329 keyboard checks, 28 Router checks, 52 controlled-replacement checks, 27 slot-acquisition checks, 23 virtual-gamepad-preference checks, 303,716 Output Ownership checks and all Settings/UI smoke tests; GitHub workflow `34618780782` completed successfully and publish/mirror jobs were correctly skipped; Input Lab workflow `34618780820` also completed successfully. The real-output XInput evidence above was collected before the code-only UX change; real Input Lab output acceptance was not rerun while the user's restarted GTA session was active, to avoid injecting acceptance traffic into the live game.

This post-release commit is on `main` only. Stable `v1.4.3` remains immutable and unchanged; a future Stable version is required before ordinary users receive these UX/diagnostic improvements.

## 2026-09-17 persistent Device Identity / Device Manager foundation

The next non-hardware platform item is now implemented on post-`v1.4.3` `main`: InputStitch has a first-class persistent device-identity layer that is deliberately independent of transient XInput user slots.

### Identity model

- New `DeviceIdentity.cs` owns opaque versioned `DeviceKey` generation, evidence aliases, inventory snapshots and persistence.
- Persistent identity evidence is ordered from strongest to weakest as PnP Container ID → container/base path → VID/PID + serial → XUSB path → PnP/HID path. XInput slot number is never an identity input.
- Existing records retain the original `DeviceKey` when later discovery provides stronger evidence; the stronger alias is added and becomes the reported identity basis instead of churning the key.
- Device records persist separately in `%APPDATA%\InputStitch\devices.xml` through the existing atomic XML store. The main config/profile format is unchanged, so old configuration remains compatible.
- Corrupt/unreadable device-registry data fails read-only for that run instead of overwriting potentially recoverable identity history or preventing InputStitch startup.
- InputStitch-owned virtual Xbox 360 and DualShock 4 outputs use fixed product-owned keys. Provider-discovered virtual-bus devices are labeled separately and are not assumed to be InputStitch's own output merely because they are virtual.

### Discovery + UI

- New `WindowsDeviceDiscovery.cs` adds a Windows XUSB/PnP discovery provider using the XnaComposite device class, PnP instance information, Container ID and ancestor inspection for virtual-bus metadata.
- HidHide remains an optional enrichment provider. `serialNumber` is now preserved from HidHide discovery, and the composite provider merges complementary Windows/HidHide metadata without requiring HidHide to be installed.
- Current XInput slots remain a separate runtime observation lane. When InputStitch cannot prove a stable PnP/XUSB ↔ slot correlation, Device Manager explicitly shows an unresolved XInput source instead of inventing a DeviceKey from `slot N`.
- New read-only `Tools → Device Manager... / 工具 → 设备管理器...` shows DeviceKey, runtime XInput slot, role, identity basis, VID/PID, last-seen time and detailed PnP/XUSB/container metadata. A one-time background startup refresh populates persistence without blocking the main UI.
- Runtime Observation and Diagnostics expose Device Identity status and the `devices.xml` path.

### Compatibility boundary

This stage intentionally does **not** change Router source IDs/policy, controller-trigger semantics, Controlled Replacement behavior, macro runtime, Output Ownership or output timing. Router remains slot-oriented until the next planned **Router source selection / per-device policy** stage consumes `DeviceKey` explicitly.

### Verification evidence

Final local verification on 2026-09-17:

- x64 + x86 Release build and Release Verification: PASS;
- new Device Identity suite: **28 PASS** using fake metadata + temporary XML only;
- keyboard/trigger: **329 PASS**;
- XInput input: **26 PASS**;
- Router: **28 PASS**;
- Controlled Replacement: **52 PASS**;
- Slot Acquisition: **27 PASS**;
- Virtual Gamepad Preference: **23 PASS**;
- Layer: **39 PASS**;
- Output Ownership: **303,716 PASS**;
- all remaining updater/network, UI safety, productivity, Settings zh-CN/en-US and legacy-config suites: PASS;
- real-output Input Lab black-box acceptance: **77/77 PASS, failures=0**;
- native Windows XUSB discovery was also invoked directly on the current laptop and returned cleanly with `COUNT=0`, matching the no-present-XUSB-device environment rather than throwing.

Stable `v1.4.3` remains immutable. This is post-release `main` platform work and needs a future Stable version before ordinary users receive it.

## 2026-09-17 Router Source Policy / per-device routing stage 1

The platform foundation immediately following Device Identity is now implemented: Router can retain the historical “route every visible external XInput source” behavior or opt into stable-DeviceKey-selected routing without persisting XInput slot as identity.

### Policy model + compatibility

- New `RouterSourcePolicyConfig` has `AllVisible` and `SelectedDevices` modes.
- `AllVisible` is the normalization/default for old configs and preserves the existing Router behavior exactly.
- `SelectedDevices` persists only stable DeviceKey values. Existing transient Output Ownership source IDs remain `router:xinput:N`; they are execution channels, not durable identity.
- The Router policy remains a global preference during profile loading, matching the existing global Router enabled/type preference boundary.
- Settings now exposes `Choose routed controllers... / 选择汇总来源...`; known offline physical devices can remain selected for reconnect, while currently unresolved runtime sources are labeled explicitly.

### Conservative runtime correlation

XInput does not expose a durable device path, so stage 1 deliberately refuses to manufacture multi-controller identity from enumeration order. A DeviceKey↔runtime slot binding is created only when all of the following are true in the same refresh:

- exactly one external XInput source is visible;
- exactly one present, non-virtual persistent gaming-device identity is a candidate;
- that candidate carries **current native Windows XUSB metadata**. An XUSB-looking path supplied only by HidHide/enrichment is not enough.

`XInputInputService` now exposes a monotonic topology generation. External connect/disconnect, own-slot movement and source-count changes invalidate the previous Device Identity snapshot immediately. `SelectedDevices` therefore fails closed while identity refresh catches up, clears any stale Router-owned output, and resumes only after a fresh safe correlation. Multi-controller layouts remain unresolved rather than assigning devices to XInput slots by discovery order.

The native-XUSB proof bit is copied from the **current discovery descriptor** into the current inventory row. Historical registry knowledge does not keep a stale binding trusted if a later refresh can no longer observe native XUSB evidence.

### Takeover boundary

Experimental Controlled Replacement remains restricted to `AllVisible` policy for now. The takeover transaction can hide selected original devices, so allowing an independent Router filter at the same time could restore a dangerous failure mode where an original is hidden but intentionally omitted from routed output. Integrating selected-device policy into takeover requires a later identity-aware hardware transaction and real HidHide/controller acceptance; it is not guessed into this stage.

### Verification evidence

Final local verification for this stage on 2026-09-17:

- x64 + x86 Release build / Release Verification: PASS;
- keyboard/trigger: **329 PASS**;
- XInput input: **26 PASS**;
- Device Identity: **33 PASS**;
- Router Source Policy: **22 PASS** (old-config/default compatibility, real XML clone/round trip, selected/unselected filtering, stale-output release, topology invalidation/recovery, multi-controller unresolved fail-closed, `AllVisible` compatibility, and source-selection dialog refresh/edit behavior);
- Gamepad Router: **28 PASS**;
- Controlled Replacement: **52 PASS**;
- Slot Acquisition: **27 PASS**;
- Virtual Gamepad Preference: **23 PASS**;
- Layer: **39 PASS**;
- Output Ownership: **303,716 PASS**;
- updater/network, UI safety, productivity, zh-CN/en-US Settings at both smoke-test sizes, legacy XML and saved gamepad-vector tests: PASS;
- real-output Input Lab black-box acceptance: **77/77 PASS, failures=0** after the Router Source Policy implementation.

Stable `v1.4.3` remains immutable; this is post-release `main` work and is not yet in the public Stable updater line.

## 2026-09-17 Analog Transform Engine stage 1

The next platform layer is now implemented locally on post-`v1.4.3` `main`: routed analog input can be shaped by a reusable transform profile before it reaches Output Ownership, while legacy/default behavior remains exact identity.

### Runtime boundary + compatibility

- New `AnalogTransform.cs` owns reusable transform math and serializable config; Router only calls the engine and ViGEm/output backends remain unaware of transform policy.
- Runtime order is `XInput report → InputSpec conversion → AnalogTransformEngine → Output Ownership merge → virtual output`.
- `RouterSourcePolicyConfig` now owns one identity-by-default `AnalogTransformProfile`; old/missing XML normalizes to identity.
- Identity profiles bypass transform math exactly, including pre-merge stick contributions such as `(100,100)`, so historical Router/Output Ownership behavior is preserved rather than being normalized early.
- Custom transform state appears in Router diagnostics; identity state keeps the exact legacy Router summary text for compatibility.

### Stage-1 transform semantics

- Left/right sticks: radial inner deadzone, outer deadzone, response curve, sensitivity/scale, X/Y inversion and max-output clamp.
- Left/right triggers: inner deadzone, outer deadzone, response curve, sensitivity/scale and max-output clamp.
- Fixed tested order: **deadzone remap → response curve → scale → inversion → max-output clamp**.
- Curves use exponent percentage: `100%` linear, `200%` squared, `50%` square-root-like.
- Stick deadzone/remap is radial so diagonal direction is preserved.
- Reusable signed half-axis conversion and magnitude-zone classification primitives are included for the later Activator/Condition engine.
- Transform helpers normalize cloned config and do not mutate caller-owned policy objects.

### UI + ownership boundary

- Router source selection now exposes `模拟量处理... / Analog transform...`.
- Separate left/right stick and trigger tabs provide live 25/50/75/100% output previews plus reset-to-default.
- Cross-source merge is intentionally **not** duplicated here: Output Ownership still uses normalized-sum sticks, max triggers and source-owned digital state.
- Alternative latest/priority/average merge policies are deferred until multi-device identity/provider correlation can assign them safely.

### Verification evidence

Final local verification on 2026-09-17:

- x64 + x86 Release build / Release Verification: PASS;
- Analog Transform: **49 PASS** covering exact identity compatibility, deadzones, curves, scaling, inversion, clamps, radial direction preservation, half-axis/zone primitives, Router integration, XML round trip, diagnostics, non-mutating config and dialog smoke;
- keyboard/trigger: **329 PASS**;
- XInput input: **26 PASS**;
- Device Identity: **33 PASS**;
- Router Source Policy: **22 PASS**;
- Gamepad Router: **28 PASS**;
- Controlled Replacement: **52 PASS**;
- Slot Acquisition: **27 PASS**;
- Virtual Gamepad Preference: **23 PASS**;
- Layer: **39 PASS**;
- Output Ownership: **303,716 PASS**;
- updater/network, UI safety, productivity, zh-CN/en-US Settings smoke, legacy XML and saved gamepad-vector tests: PASS;
- real-output Input Lab black-box acceptance: **77/77 PASS, failures=0** after Analog Transform stage 1.

Stable `v1.4.3` remains immutable. This work needs a future Stable version before ordinary updater users receive it.

## 2026-09-17 Activator + Condition Engine stage 1

The next post-`v1.4.3` platform layer is now implemented on `main`: InputStitch has an explicit, opt-in activation/condition model without replacing the common macro executor.

### Compatibility + runtime boundary

- Every macro now carries serializable `ActivatorConfig` + `MacroConditionConfig`.
- Missing/old XML normalizes to `Activator=Legacy` and no additional conditions, so historical `TriggerRunMode.Toggle` / `TriggerRunMode.Hold` behavior remains the exact default path.
- `ActivatorRuntimeEngine` owns only physical-edge/timing/condition state. It never sends output itself.
- A resulting start/stop decision reuses the existing Concurrent Macro Runtime / Parallel Held Mapping and Output Ownership paths. No second macro executor, Router-specific executor or ViGEm-specific execution path was added.
- While an explicit advanced activator owns semantics, the old Toggle/Hold selector is visibly disabled in the macro editor instead of leaving two competing controls active.

### Stage-1 activators

- `Press` — starts once on the matching physical down edge;
- `Release` — starts once on the matching physical release, provided the activation was eligible at down and conditions still match at release;
- `WhileHeld` — uses the existing Hold lifecycle and stops on physical release;
- `LongPress` — fires once after the configured threshold while the same physical hold remains valid;
- `DoublePress` — fires on the second valid press inside the configured window;
- `Legacy` — delegates exactly to historical Toggle/Hold semantics.

Wheel directions are treated explicitly as edge-only input. `Press` and `DoublePress` can consume wheel edges repeatedly without an invented release. `Release`, `WhileHeld` and `LongPress` refuse to manufacture a persistent wheel-down state.

### Stage-1 conditions

Additional conditions are AND gates on top of the macro's existing Layer eligibility:

- foreground process name;
- optional foreground-window title substring;
- stable source `DeviceKey`;
- controller analog magnitude or signed half-axis percentage zone.

`DeviceKey` conditions consume the same topology-generation-aware Device Identity evidence as Router Source Policy. `DeviceIdentityService.TryResolveXInputSlotForDeviceKey(...)` fails closed when the current snapshot is stale/unresolved instead of trusting a remembered XInput slot. For controller-triggered macros, a DeviceKey condition also requires the trigger edge to originate from that resolved slot.

Analog conditions reuse the Analog Transform stage-1 signed-half-axis and magnitude-zone primitives. Stage 1 supports left/right trigger magnitude, left/right stick magnitude, and ±X/±Y half-axis bands as **additional conditions**. It does not yet synthesize trigger edges directly from entering/leaving a stick region.

For timing safety, advanced macros whose physical trigger matches but whose additional condition is currently false still notify the activator state machine. This prevents an invalid middle press from bridging two otherwise-valid `DoublePress` edges. `WhileHeld` also fails closed if an active condition later becomes false and stops only that macro's source. Emergency Stop/profile/config-reset paths clear activator timing state.

### UI + deliberately deferred semantics

- The macro editor exposes `激活与条件... / Activation & conditions...` and summarizes the active mode/condition count.
- The dialog exposes stable DeviceKey choices from Device Identity and explains unresolved/fail-closed behavior.
- It also makes the current macro Layer visible as the authoritative Layer condition.
- Stage 1 intentionally does **not** pretend to implement Short Press, Triple Press, Turbo/repeat, another-input-held, or arbitrary analog-region-as-trigger semantics. Those remain stage-2 candidates after their start/stop/repeat behavior is defined explicitly.
- One-shot advanced activators on an infinite macro require the normal Stop button or Emergency Stop; they do not silently reuse legacy Toggle-to-stop behavior.

### Verification evidence

Final local verification on 2026-09-17:

- x64 + x86 Release build / Release Verification: PASS;
- Activator/Condition: **55 PASS** covering legacy compatibility, Press/Release/WhileHeld/LongPress/DoublePress, timing boundaries, wheel edge-only behavior, invalid-condition disarming, condition-loss cleanup, analog zones, XML round trip, diagnostics/runtime-observation and editor/main-UI smoke;
- Device Identity: **36 PASS**, including reverse stable DeviceKey→current XInput-slot resolution and topology-generation invalidation/recovery;
- keyboard/trigger: **329 PASS**;
- Idle Gamepad: **58 PASS**;
- XInput input: **26 PASS**;
- Router Source Policy: **22 PASS**;
- Analog Transform: **49 PASS**;
- Gamepad Router: **28 PASS**;
- Controlled Replacement: **52 PASS**;
- Slot Acquisition: **27 PASS**;
- Virtual Gamepad Preference: **23 PASS**;
- Layer: **39 PASS**;
- macro timing: **7 PASS**;
- updater/network, UI safety, productivity, Settings zh-CN/en-US, legacy XML and saved gamepad-vector smoke: PASS;
- Output Ownership: **303,716 PASS**;
- real-output Input Lab black-box acceptance: **77/77 PASS, failures=0**.

Stable `v1.4.3` remains immutable; Activator + Condition stage 1 is post-release `main` work and requires a future Stable version before ordinary updater users receive it.

## 2026-09-21 platform hygiene: single source manifest + foreground identity cache

The 2026-09-21 architecture review (`docs/ARCHITECTURE_REVIEW.md`) and long-term plan
(`docs/DEVELOPMENT_PLAN.md`) are on `main`. Two review findings are already closed:

- **F3 — single production-source manifest.** `scripts/production-sources.txt` is now authoritative;
  `build.ps1` (compile list + Source.zip contents), `tests/Run-Tests.ps1` and
  `tools/InputLab/build-acceptance.ps1` consume it through `scripts/Get-ProductionSources.ps1`.
  `tests/SourceManifestTests.cs` is a drift guard (208 checks) that fails when a root-level `.cs` file is
  missing from the manifest, when the manifest is malformed, or when the `InputStitch.csproj` `<Compile>`
  list diverges. Adding or renaming a production file now requires one edit instead of five.
- **F2 — foreground identity cache.** Condition evaluation (trigger events and the 10 ms activator tick) now
  uses `DescribeForegroundIdentityCached()` — keyed by the foreground window handle with a 250 ms TTL — so
  the previous "one `Process.GetProcessById` per macro per tick" collapses to at most four lookups per
  second while window-title changes remain observable. `tests/ForegroundIdentityCacheTests.cs` (5 checks)
  verifies reuse, TTL refresh and concurrent safety from hook threads + the UI thread.
- **F1 — input pipeline off the UI thread.** XInput polling, device-topology refresh and router mirroring
  moved from `Windows.Forms.Timer` to `InputPipelineScheduler` (`InputPipeline.cs`): a background thread with
  deadline pacing, error containment and clean shutdown. Activator decisions stay UI-affine — the input
  thread only posts a coalesced activator-tick request that the UI thread performs, preserving the existing
  dispatch contract. `TickCadenceStats` records tick counts, stalls (gap > 5x interval), maximum gap and
  maximum/average tick duration for the input pipeline and the foreground timer and reports them in Runtime
  Observation plus Diagnostics, so real stalls are measurable.

Verification evidence (2026-09-21, local):

- full `tests/Run-Tests.ps1`: PASS — source manifest 214 checks, foreground cache 5 checks, tick cadence
  7 checks, input pipeline scheduler 9 checks, Layer 72 checks, 329 keyboard, 303,716 Output Ownership, all
  Settings/legacy-XML smoke;
- `build.ps1` x64/x86 Release build + Release Verification: PASS, with the manifest and helper script
  included in the Source.zip;
- local commits only — nothing pushed and no release created.

## 2026-09-21 profile/context integration — stage 1 core

`MappingLayerDefinition.ContextCondition` (optional `MacroConditionConfig`) makes a layer eligible while its
context matches. The evaluator is the same AND-condition implementation the per-macro gates use (foreground
process/title, stable `DeviceKey`, analog zone) — no parallel rule set, no application-specific switch.

Semantics: precedence `momentary hold > matching context layer > persistent selection`; the first matching
context layer in configured list order wins; a matching context never rewrites the persistent selection and
returns to it when the context stops matching; Base may carry a context (temporary Base-only); re-evaluation
runs on the foreground-tracking cadence plus every layer/condition edit; unresolvable device conditions
fail closed. Layer names, Runtime Observation and Diagnostics mark context layers and report the match.

The layer-management menu exposes "设置上下文条件… / Set context condition..." — the shared condition dialog in
conditions-only mode (no activator section; the layer's own label is shown) — plus "清除上下文条件 / Clear
context condition". Editing re-applies the effective layer immediately and saves the configuration.

Evidence: `tests/LayerContextTests.cs` (22 checks — XML round trip, precedence, list order, context loss,
Base context, condition clearing, diagnostics/observation, momentary override, conditions-only dialog title
and round trip) plus the full regression suite.

## 2026-09-21 input provider decision: SDL3, not GameInput

`docs/INPUT_PROVIDER_EVALUATION.md` records verified package/licence/OS facts and decides the second input
provider:

- **SDL3** (3.4.16, zlib, native DLL for x86+x64, desktop Windows back to XP) becomes the second provider
  behind a new `IInputProvider` seam; XInput stays provider #1 and is ported behind the same interface first,
  behaviour-neutral.
- **Microsoft GameInput** is deferred: the NuGet package is native-only (`native/include`, `native/lib/x64` +
  `arm64`, no x86) and ships `redist/GameInputRedist.msi` under the Microsoft GameInput Redistributable
  terms, which conflicts with the portable no-installer, x86+x64 distribution model. Adopting it later is an
  explicit user decision.
- `SDL3.dll` will be embedded as a resource and extracted at runtime, reusing the existing embedded
  ViGEm-client pattern, so the single-EXE distribution stays intact.

Next implementation step: `IInputProvider` plus the XInput adapter (behaviour-neutral, fake-provider tests),
then the SDL3 native subset.

**Implemented 2026-09-21:** `InputProvider.cs` defines `IGamepadInputProvider` (provider name, topology
version, edge callbacks, device indices, state access, trigger satisfaction, poll). `XInputInputService`
implements it with its historical behaviour (callbacks are now properties so existing assignment sites keep
working). `GamepadRouterService.Tick` takes the interface and iterates the provider's device set — classic
XInput slots plus synthetic indices above 3, which is where SDL3 devices will appear — and clears a source
exactly once when a device disappears. `tests/InputProviderSeamTests.cs` (15 checks) proves the seam with a
fake provider including a synthetic index, and the existing router suites (28 + 22 checks) are unchanged.
Next: the SDL3 native subset and its device-identity evidence.

**SDL3 provider logic implemented 2026-09-21:** `Sdl3Interop.cs` defines the normalized `ISdl3Api`
contract (initialize/shutdown, device ids, descriptors, open/close, snapshots with SDL button/axis indices)
and `Sdl3InputProvider.cs` implements the provider seam on top of it:

- devices are published at synthetic indices starting at 4, with stable indices per SDL device id and
  deterministic reuse after a disconnect;
- SDL state maps onto the app's pad state (button mask identical to the XInput lane, Y axes inverted because
  SDL reports positive-down, triggers scaled 0..32767 → 0..255);
- button and trigger edges are raised with the provider device index and the XInput trigger hysteresis
  (80% press / 70% release); a disconnected device releases its held buttons before its index is freed;
- Microsoft-family pads (vendor 0x045E) are skipped by default because the XInput lane already owns them;
  the suppression is a constructor switch;
- an unavailable SDL3 runtime leaves the provider inert instead of throwing.

Evidence: `tests/Sdl3ProviderTests.cs` (29 checks, fake SDL3 API) plus the full regression and x64/x86
release verification. Still to do: the P/Invoke implementation of `ISdl3Api`, SDL3.dll loading/embedding and
MainForm wiring; real-device verification remains on the hardware-blocked lane (no physical controller here).

**SDL3 native binding added 2026-09-21:** `Sdl3NativeApi.cs` implements `ISdl3Api` against the real runtime
with dynamic `LoadLibrary`/`GetProcAddress` resolution (a missing DLL leaves the provider inert instead of
breaking startup). It calls `SDL_Init(SDL_INIT_GAMEPAD)`, refreshes with `SDL_UpdateJoysticks` +
`SDL_UpdateGamepads`, enumerates through `SDL_GetGamepads` (pointer freed with `SDL_free`), describes devices
through the `...ForID` getters (name/path/vendor/product/type) and reads state with the SDL 3.4 per-control
`SDL_GetGamepadButton`/`SDL_GetGamepadAxis` calls.

Real-runtime evidence (2026-09-21): downloaded the official SDL3 3.4.16 win32-x64 release asset
(`SDL3-3.4.16-win32-x64.zip`), extracted `SDL3.dll` (2,844,160 bytes, SHA-256
`1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0`) and ran the suite with
`INPUTSTITCH_SDL3_DLL` pointing at it: `REAL-SDL3 smoke: device count=0; provider status=ready; devices=0`,
32 checks green (29 fake-API + 3 real-runtime). Without the environment variable the suite reports 29 checks
and skips the native smoke, so CI runs stay independent of the DLL.

Still to do: decide how SDL3.dll ships (committed `third-party/SDL3/<arch>/` folder vs a restore script vs an
embedded resource), wire the provider into MainForm, add device-identity evidence (VID/PID/serial), and run
Input Lab. Real-device verification stays on the hardware-blocked lane.

**SDL3 lane wired into MainForm 2026-09-21:** `CompositeInputProvider.cs` merges providers over one device
index space (XInput owns 0..3, SDL3 starts at 4), routes state reads to the owning provider, forwards child
edges unchanged, combines child topology versions and disposes children. `MainForm` now constructs
`XInputInputService` + `Sdl3InputProvider` and drives them through the composite:

- without SDL3.dll the SDL3 lane initializes once, reports unavailable and costs nothing, so behaviour is
  exactly the previous XInput-only path;
- with the runtime present the lane activates automatically and its devices appear at synthetic indices
  (routed by the router, usable as triggers/conditions through the shared index space);
- Runtime diagnostics gained `InputProviders: xinput=active; sdl3=<ready|unavailable>(<status>)`;
- SDL3 devices are baseline-safe (no synthetic edges on first observation) and pinned triggers are strict:
  an XInput pad can no longer satisfy a trigger pinned to a synthetic index.

Evidence: `CompositeInputProviderTests` (15 checks) plus the extended `Sdl3ProviderTests` (30 checks) and the
full regression + x64/x86 release verification. Remaining: how SDL3.dll ships, device-identity evidence,
provider column in Device Manager, Input Lab and real-device verification.

**Optional SDL3 runtime restore added 2026-09-21:** `scripts/Restore-Sdl3.ps1` downloads the pinned SDL3
3.4.16 release assets for x86/x64, verifies the archive SHA-256 and the extracted DLL SHA-256, and places
`SDL3.dll` plus `LICENSE.txt` under `third-party/SDL3/<arch>/` (idempotent: a second run reports "already
restored and verified"). Pinned values:

- x64 archive `4217944b4e51457af4a59c82d883f8443b3e65964b2acd8943484c492756c4b6`, DLL
  `1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0`;
- x86 archive `beb4e86e4a101f66556162c3efff9fd1ad88f1761b63be35867a3336a72f8c41`, DLL
  `47d508b4232cf9462096bdb6f613a330d9a94a7eb09c305fad073430c6531920`.

`third-party/SDL3/` is gitignored and deliberately excluded from the Source.zip (build.ps1 now packages the
committed ViGEm third-party folder explicitly), so release artifacts stay deterministic; `Sdl3ProviderTests`
auto-detects the restored DLL, which makes the local run execute the real-runtime smoke
(`REAL-SDL3 smoke: device count=0; provider status=ready; devices=0`, 33 checks) while CI keeps skipping it
(30 checks). The end-user shipping choice for SDL3.dll is still open.

**SDL3 device-identity evidence added 2026-09-21:** `Sdl3DeviceIdentity.cs` maps SDL3 gamepad descriptors onto
the persistent identity model used by `devices.xml`:

- a real reported device path (usually the HID path) becomes pnp evidence, so the record's basis is `pnp`;
- VID/PID plus name produce a provider-scoped alias that is explicitly ranked below every PnP basis, so a pad
  that moves to another USB port (and therefore gets a different HID path) still merges into the same record;
- a device reporting neither a path nor VID/PID is skipped instead of inventing an identity, and SDL3 devices
  never claim XInput slot correlation - that stays reserved for the XUSB/HidHide lanes;
- `DeviceIdentityKeys` gained the `provider` basis (weakest), `GamingDeviceDescriptor` gained provider fields,
  `DeviceIdentityService` gained an optional provider-device lane, and `MainForm` feeds the current SDL3
  devices into the registry so they appear in Device Manager with `XInputSlot = -1`.

Evidence: `tests/Sdl3DeviceIdentityTests.cs` (18 checks: mapping rules, basis ranking, registry merge
including the moved-port case, inventory integration) and the unchanged 36-check device-identity suite. The
Device Manager provider column is the remaining UI step.

**Device Manager presentation updated 2026-09-21:** the identity-basis cell now names the provider lane for
provider-sourced records ("PnP path · sdl3", "Provider-reported · sdl3"), which identifies the device without
opening the tooltip and without adding a ninth grid column (8 columns already fill the 1120 px window).
`BasisLabel` gained the `provider` basis, and metadata-only providers (HidHide, Windows XUSB/PnP) keep their
previous presentation. `tests/Sdl3DeviceIdentityTests.cs` grew to 23 checks by exercising the static basis
helper through the existing reflection pattern.

**SDL3 lane switch added 2026-09-21:** `MacroConfig.Sdl3InputEnabled` (default `true`) is exposed as a
Settings checkbox in the controller section with hint text explaining what the lane adds and that it applies
after the next start. `MainForm` builds the composite with the SDL3 provider only when the switch is on
(otherwise `gamepadInput` is the plain XInput service), profile loads preserve the global switch, and
diagnostics report `sdl3=disabled(setting)` while it is off. Evidence: `VirtualGamepadPreferenceTests` grew
from 23 to 27 checks (default value, dialog reflection in both states, real config clone) plus the
zh-CN/en-US Settings smoke tests and the full regression.

**UI dialog types extracted 2026-09-21:** the remaining self-contained UI classes left `InputStitch.cs`:
`StepEditDialog.cs` (571 lines), `GamepadPreviewControl.cs` (357, preview control plus the input-sink helper),
`Dialogs.cs` (336, macro export selection, batch delay, diagnostics and about dialogs) and `SettingsDialog.cs`
(591, settings dialog plus its gear button). `InputStitch.cs` is now 8,126 lines compared with 12,171 before
the decomposition. Evidence: identical x64/x86 binary sizes to the previous commit (883,712 / 884,224 bytes),
the full regression suite (source manifest drift guard now 298 checks, productivity 360 unchanged) and the
zh-CN/en-US Settings smoke tests.

## 2026-09-21 Activator stage 2 — Short Press

`ActivatorModes.ShortPress` ("Short Press" / 短按) is implemented with an explicit lifecycle:

- on press: record the press time and whether the conditions were satisfied at press time;
- on release: fire once (`StartOnce`) only when the press was eligible, conditions still hold at release, and
  the release lands inside `ActivatorConfig.ShortPressMs` (default 300 ms, normalized 50–2000 ms);
- held past the window: the next activator tick disarms the press, so a later release never fires — the same
  key stays usable for hold-style bindings;
- an ineligible press, or condition loss during the press, cancels without firing;
- wheel directions stay edge-only and never fire Short Press;
- the condition dialog exposes the mode and its window row; the main editor summary and diagnostics show
  "短按 / Short Press".

Evidence: `tests/ActivatorConditionTests.cs` grew from 55 to 73 checks (window fire, re-arm, hold disarm,
later tap, ineligible press, condition loss, wheel edge-only, bounds normalization, XML clone of the new
field) and the full suite plus x64/x86 Release verification passed.

Still deferred until their lifecycles are explicitly defined: Triple Press, Turbo/repeat,
another-input-held and richer analog-region triggers.

## 2026-09-21 MainForm decomposition — step 1

`MainForm` is now declared `partial` and the Layer subsystem lives in `MainForm.Layers.cs` (576 lines):
display/management helpers, the layer runtime state machine (toggle, momentary holds, context conditions),
eligibility helpers and both layer combo handlers. `InputStitch.cs` went from 12,171 to 11,614 lines.

Behaviour-neutrality evidence: the x64/x86 Release build produced **identical EXE sizes** to the
pre-extraction build (860,672 / 861,184 bytes) and the full regression suite (including the reflection-based
Layer and layer-context suites) passed unchanged; the source manifest drift guard now reports 220 checks.

Next per the plan: continue H1.4 with the diagnostics/observation extraction, then H2.2
(activator/condition stage 2) and H2.3 (`IInputProvider`). A real-world session should also read the cadence
statistics from Runtime Observation to confirm the dedicated input thread removed UI-stall delays.

Second extraction (same day): the Runtime Observation / Diagnostics text builders moved to
`MainForm.Diagnostics.cs` (248 lines). Third extraction (same day): controller-takeover coordination and
the gamepad-router readiness helpers moved to `MainForm.ControlledReplacement.cs` (295 lines).
Fourth extraction (same day): the main-window builder `BuildUi()` and `BuildTrayIcon()` moved to
`MainForm.Ui.cs` (1,233 lines). `InputStitch.cs` is now **9,885 lines** (from 12,171 before the
decomposition); the x64/x86 binaries are again identical in size, the full suite passed and the Settings
smoke tests still render the moved layout. Remaining cohesive split: editor/runtime logic inside the
remaining file.

## 2026-09-21 Layer ergonomics stage 1 — Momentary / Hold-to-Layer

Layer definitions now support an optional momentary mode: the layer is active only while its own switch
shortcut is physically held, and releasing the shortcut returns to the previously active layer instead of
unconditionally returning to Base. The persistent click-to-switch / UI selection is never rewritten by a
temporary hold.

Design boundary:

- `MappingLayerDefinition.MomentarySwitch` is a plain boolean inside the existing layer XML; old
  configuration files load as click-to-switch, and older builds ignore the new element.
- Runtime keeps the persistent (click-to-switch / UI) layer plus an ordered list of currently held
  momentary layers; the newest still-held entry is effective. Base can be momentary as well, so holding its
  shortcut temporarily returns to Base-only.
- Entering and leaving a momentary layer reuses `SwitchActiveMappingLayer`, so cleanup remains source-local,
  the pre-held-trigger release rule is unchanged, and Emergency Stop / UI safety / Output Ownership are not
  touched. Layer is still eligibility/grouping only.
- Duplicate press edges (key repeat or repeated controller reports) for an already-held momentary layer are
  ignored.
- Missed release edges (keyboard hook loss or a dropped controller edge) are recovered by the same physical
  release probe used by held mappings, so a momentary layer cannot stay stuck. Deleting a held layer, or
  converting it back to click-to-switch while held, safely drops the hold.
- Layer management exposes 按住生效（松开返回） / "Hold to activate (release returns)" beside the switch
  shortcut; layer names, runtime observation and diagnostics report the momentary mode.

Verification evidence (2026-09-21, local development-laptop session):

- x64 + x86 `build.ps1` Release build and Release Verification: PASS on the new code;
- full `tests/Run-Tests.ps1`: PASS, including **72 flexible-Layer checks** (39 pre-existing + 33 new
  momentary checks), 329 keyboard/trigger checks, 303,716 Output Ownership checks, and all
  zh-CN/en-US Settings plus legacy-XML smoke tests;
- real-output Input Lab acceptance was intentionally **not** rerun in this unattended session to avoid
  injecting real input on a desktop the user had left; record it as not-rerun rather than passed;
- this change is a **local commit only**: not pushed and not released in this session. A future Stable or
  Beta release still has to follow the normal publish flow before ordinary users receive it.

## Working on

**Stable `v1.4.3` is published and immutable. Post-release `main` now contains controller hot-plug/takeover-UI diagnostic hardening, Persistent Device Identity / Device Manager stage 1, Router Source Policy stage 1, Analog Transform Engine stage 1, Activator + Condition Engine stage 1, and Layer ergonomics stage 1 (Momentary / Hold-to-Layer).** The remaining Layer ergonomics work is profile/context integration built on the shared DeviceKey + Condition model. Physical controller + HidHide + real-game takeover acceptance remains a parallel hardware-blocked lane.

The same refactor branch now also completes the planned **Virtual Output Backend abstraction**. `GamepadOutput` remains the stable synchronized facade and preserves existing Xbox 360 / DualShock 4 / `None` semantics, while direct ViGEm controller creation, report mapping, connection lifecycle and driver-specific exception translation have moved behind `IVirtualGamepadBackend` into `VigemVirtualGamepadBackend`. ViGEm is still the only production backend; no second driver or new user-facing option was added.

A further low-risk `MainForm` cleanup moves target-window / Idle target-scope matching and resolution policy into `TargetWindowPolicy`, and moves bound-profile directory scanning into `ProfileCatalog`. The original private `MainForm` method signatures remain as thin wrappers for compatibility with existing reflection tests and internal call sites. The updater UI workflow is also moved out of `MainForm` into `UpdateUiCoordinator`; `MainForm.CheckForUpdatesAsync(...)` remains a thin entrypoint and Idle Gamepad still treats an active update check as a busy condition. These passes deliberately do not touch Macro Runtime, Held Mapping, Output Ownership, trigger dispatch or output timing.

Stable update transport now prefers the project download domain end-to-end. The manifest is checked first at `https://download.zhihanyu.com/latest/InputStitch-update.xml`; any download/parse/validation failure falls back to the official GitHub `latest` manifest. The R2 `/latest` manifest points executable assets at immutable `https://download.zhihanyu.com/releases/v<version>/...` objects. If that executable retrieval or its integrity/product validation fails, the updater retries the same checked version from the official GitHub Release asset. The GitHub-published `InputStitch-update.xml` intentionally keeps GitHub asset URLs so existing v1.4.1 clients—which only trust that historical URL shape—can still upgrade across the migration boundary. `Publish-R2.py` derives the R2-specific manifest copy during mirroring. Only after both sources fail does the pre-existing check/download failure UI run. SHA-256 and product/version validation remain mandatory, and third-party asset hosts remain rejected.

1.4.2 release verification requires the full `tests/Run-Tests.ps1` suite, including Layer/Productivity/Updater/UI safety, 303,716 Output Ownership checks, 29 UpdateNetwork checks and 23 VirtualGamepadPreference checks; all zh-CN/en-US Settings smoke tests; x64/x86 `build.ps1` Release verification; and a Python 3.12 CI gate that executes the real R2 manifest derivation against the built Stable manifest before publication. The GitHub Stable manifest must retain GitHub asset URLs, while the derived R2 manifest must retain the same version, file names and hashes but use version-pinned `download.zhihanyu.com` assets. No configuration format, macro semantics, controller routing policy or output timing is intentionally changed; updater transport/source behavior is the intentional user-visible change.

**`v1.4.3` is the current Stable line. Persistent Device Identity, Router Source Policy, Analog Transform stage 1, Activator + Condition stage 1 and Layer Momentary/Hold-to-Layer are complete on post-release `main`; the remaining Layer ergonomics half is profile/context integration. Physical controller + HidHide + real-game acceptance remains a parallel hardware-blocked lane.**

Beta.2 adds two major non-hardware improvements on top of beta.1:

1. flexible arbitrary named Layer management + keyboard/mouse/controller layer-switch bindings;
2. continuous Controlled Replacement health monitoring + automatic fail-safe disengage/recovery.

Physical-controller + HidHide + real-game acceptance is explicitly blocked because the development machine has no physical XInput controller and no HidHide environment. Do **not** substitute more virtual devices and claim that as physical acceptance.

Product strategy reference: `docs/PRODUCT_STRATEGY.md`. The project should compete on deterministic orchestration, observability and fail-safe behavior rather than broad controller-model/vendor-feature count.

## Architecture baseline inherited from v1.3.0 / beta.1

Stable `v1.3.0` completed Input Ownership + universal Concurrent Macro Runtime. The 1.3.1 Beta line extends the same common runtime rather than creating controller-specific or Layer-specific execution engines.

Core flow:

```text
physical keyboard/mouse/XInput edge
        ↓
trigger selection + Layer eligibility
        ↓
Concurrent Macro Runtime / Parallel Held Mapping
        ↓
Output Ownership
        ↓
keyboard / mouse / virtual controller
```

Router sources also enter Output Ownership independently:

```text
external XInput slot N
        ↓
router:xinput:N
        ↓
Output Ownership merge
        ↓
InputStitch virtual Xbox
```

## Completed in current beta.2 tree

### Physical / virtual XInput input

- `XInputInputService` polls XInput slots `0..3`.
- InputStitch's own ViGEm Xbox slot is dynamically excluded to prevent feedback.
- Startup/reconnect establishes a baseline before emitting edges.
- Disconnect generates release edges for held buttons/triggers.
- Digital buttons/D-pad and LT/RT are first-class macro trigger inputs.
- LT/RT use 80% activation / 70% release hysteresis.
- Configured controller trigger means “this control on any visible XInput controller.”
- Hold dispatch pins the run to the actual controller slot that started it.

### Controller → keyboard/mouse/hybrid mapping

Controller triggers reuse the ordinary Trigger → Runtime → Ownership path and can execute:

- keyboard output;
- mouse output;
- virtual-controller output;
- mixed keyboard + mouse + virtual-controller macros;
- timed / Toggle / finite or infinite Hold / lightweight Held Mapping behavior.

Without HidHide takeover this remains augmentation: the original controller can still be visible to the game.

### Gamepad Router / aggregation

- `GamepadRouterService` mirrors every visible non-own XInput source.
- Each source has an independent `router:xinput:N` ownership ID.
- Whole state includes buttons, D-pad, both sticks and both triggers.
- State replacement is atomic through Output Ownership.
- Identical frames are no-ops.
- Disconnect/recenter clears only that source's contribution.
- Emergency Stop disables Router so a later polling tick cannot immediately reassert state.

Router alone is **not interception**. Original + routed input may both reach a game until Controlled Replacement is active.

### Optional virtual-controller creation

Virtual-controller preference has three values:

1. Xbox 360
2. PS4 / DualShock 4
3. `None` / **Do not create a virtual controller / 不创建虚拟手柄**

When None is selected:

- startup does not create ViGEm just because gamepad-output macros are saved;
- editing/saving a gamepad step does not connect ViGEm;
- keyboard/mouse macros still work;
- external XInput controllers can still trigger keyboard/mouse macros;
- incompatible runtime actions are refused rather than silently overriding the preference;
- Idle Gamepad is disabled;
- Router/Controlled Replacement cannot stay in None because they require Xbox output.

Fresh-install default remains Xbox 360 for compatibility.

### Flexible Layer system — beta.2

Layer remains an eligibility/grouping feature on top of the common trigger/runtime/output architecture.

Current model:

- Base layer is mandatory and always eligible;
- any number of additional layers can exist;
- non-Base layers have stable IDs and editable user names;
- one non-Base layer is active at a time, so eligibility is Base + active layer;
- active layer remains runtime-only and resets to Base-only after restart;
- every macro type participates: ordinary timed, Toggle, Advanced/complex Hold, Parallel Held Mapping;
- main UI exposes **Manage layers... / 管理映射层…**;
- user can add, rename and delete layers;
- deleting the active layer first returns runtime state to Base-only;
- macros assigned to a deleted layer move safely back to Base;
- each layer, including Base, may have a keyboard/mouse/controller switch trigger;
- Base switch trigger means “return to Base-only”;
- layer-switch trigger has priority over ordinary macro matching but does not suppress the physical foreground-app input;
- leaving a layer stops/removes only old-layer sources;
- newly eligible triggers already physically held are blocked until release + fresh press;
- duplicate normal triggers retain normal macro-list priority inside the eligible set.

#### Layer XML compatibility fix

Old public collection-field XML behavior appended deserialized Layer data into constructor defaults. That meant deleting legacy Layer 1 could make it reappear after restart.

Current solution:

- runtime still uses `List<MappingLayerDefinition>`;
- XML uses a replacing array proxy;
- truly old XML with no Layer element still gets constructor default Base + Layer 1;
- once modern XML contains an explicit Layer list, that saved list replaces defaults and is authoritative;
- deleting Layer 1 survives a real serialize → deserialize → normalize round trip.

### Macro timing drift bug — fixed earlier in beta.1 line

`WaitOrStopWithUiSafety` now uses monotonic `Stopwatch` elapsed time rather than subtracting requested sleep slices. This fixed the case where nominal 900 ms output could become roughly 1.7 s on this Windows scheduler.

### Update network interruption handling — hardened earlier in beta.1 line

Stable-channel updater now uses bounded timeout/retry behavior:

- transient connect/DNS/send/receive/timeout failures: maximum 3 attempts total;
- 8 s connect/read-write inactivity timeout per attempt;
- synchronous WebClient request runs in background task so UI stays responsive;
- every retry deletes the previous partial executable;
- final failure leaves no partial executable and never enters install transaction;
- background check-stage failures remain silent;
- after user accepts an update, download failure is explicitly shown;
- once EXE is fully downloaded and SHA-256/version verified, installation is fully local and no longer needs GitHub/network access.

### Controlled Replacement transaction — beta.1 foundation

Implemented:

- `IDeviceHidingBackend` + HidHide CLI backend;
- device enumeration/parser;
- explicit user-selected devices; nothing hidden automatically;
- pre-hide Router requirement;
- separate `SlotAcquisition.cs` recoverable slot-0 transaction;
- InputStitch disconnects its own virtual Xbox once for self-identity filtering;
- if needed, every present external XUSB source is temporarily re-enumerated;
- InputStitch reconnects first and must hard-verify target slot 0;
- every external XUSB identity must return before hiding is reachable;
- slot-reordering scope can be broader than hiding scope, but only selected external identities reach HidHide;
- PnP disable is non-persistent;
- `slot-acquisition-recovery.xml` is separate from `controlled-replacement-recovery.xml`;
- >3 external XUSB sources is refused before PnP mutation because current XInput backend has four total slots;
- `ControlledTakeoverPipeline` makes HidHide Begin unreachable until acquisition, target-slot, Router/source preparation and external identity checks pass;
- post-hide Router/slot/source visibility validation;
- preserve pre-existing hidden-device/app-whitelist/cloak state;
- recovery journal is written before first HidHide mutation;
- rollback only InputStitch-owned changes;
- next-launch abnormal-exit recovery;
- Emergency Stop and normal shutdown attempt visibility restoration first.

### Continuous takeover health monitoring — beta.2

New `ControlledReplacementHealthMonitor` runs potentially slow HidHide health checks away from the WinForms/XInput polling thread.

While takeover is Active, it verifies:

- InputStitch virtual Xbox remains target slot 0;
- Router is still ready;
- all expected routed XInput source slots remain visible to InputStitch;
- HidHide backend remains available;
- HidHide cloak remains active;
- every selected original controller remains in hidden-device state;
- InputStitch remains in HidHide application whitelist.

Failure policy:

- one transient unhealthy sample is tolerated;
- a healthy sample resets failure streak;
- two consecutive failures signal exactly one fail-safe disengage;
- monitor stops before recovery to prevent duplicate rollback;
- `StopControlledReplacement()` restores InputStitch-owned HidHide state;
- Router is stopped and disabled after fail-safe restore;
- Output Ownership is cleared and virtual output is neutralized;
- disabling Router prevents restored original input + routed duplicate input;
- if restore fails, recovery journal remains and user receives an explicit warning.

Activation source-health still uses a short settle + active poll. Ongoing background health reads the XInput snapshot last updated by the normal 10 ms UI polling timer; it does not generate controller edges from the background thread.

### Runtime observation / diagnostics — beta.2

Runtime Observation now displays:

- user-facing active Layer name;
- virtual Xbox slot;
- Router status + routed source count;
- Controlled Replacement state;
- health-monitor active/inactive state;
- last health result;
- consecutive failure count;
- expected routed XInput source slots;
- normal macro runs / held mappings / ownership sources / merged output.

Diagnostics additionally list:

- every Layer ID, name and switch trigger;
- Layer count;
- Controlled Replacement state/message;
- health-monitor state/result;
- expected routed sources;
- replacement-recovery warning;
- slot-acquisition-recovery warning.

Observation is side-effect free. `ObservedVirtualGamepadConnected/Type/Slot` return neutral values in the isolated UI host instead of touching `GamepadOutput`; opening diagnostics in tests therefore does not load/initialize ViGEm.

## Hardware acceptance still blocked

The development machine has no physical XInput controller and no HidHide environment. Therefore the following remain unverified on real hardware:

- physical XInput PnP disable/enable;
- actual HidHide hiding of an original physical controller;
- InputStitch remaining whitelisted/readable while an ordinary target loses the original device;
- a real game seeing only the routed InputStitch slot-0 virtual controller;
- runtime health fail-safe under real physical slot/HidHide faults;
- safe behavior for games that cache controller index/device state.

Do not use more ViGEm virtual devices to claim these are complete.

## Broader platform work not yet implemented

- DirectInput / HID / GameInput input backends;
- >4 XInput-equivalent sources;
- Windows-connected DualShock/DualSense input backend outside current XInput-class scope;
- gyro-to-mouse;
- broader per-device identity UI;
- richer routing conditions/groups;
- optional Layer ordering/presets only if a concrete workflow justifies them.

## Known intentional limitations

- XInput exposes at most four user slots.
- Current controller input backend is XInput, not universal HID enumeration.
- Guide/Home behavior through ordinary XInput is not guaranteed across every controller/driver.
- Router aggregation is not interception without HidHide.
- Controlled Replacement may temporarily re-enumerate all external XUSB controllers to acquire slot 0 but only hides selected external identities.
- `VirtualGamepadTypes.None` disables virtual output only; external XInput triggers remain available.
- One `MacroDefinition` still has at most one active worker-backed run.
- Single-step remains exclusive diagnostic mode.
- Duplicate normal triggers use macro-list priority.
- Beta and Stable share `%APPDATA%\InputStitch`; do not run them simultaneously.

## Current verification evidence

Latest clean source regression after beta.2 runtime changes:

```text
Keyboard                 323 PASS
Idle Gamepad              58 PASS
XInput Input              26 PASS
Gamepad Router            28 PASS
Controlled Replacement    52 PASS (fake backend/parser; HidHide not invoked)
Slot Acquisition          27 PASS (fake PnP/XInput; no real device disabled)
No-Virtual preference     15 PASS (no ViGEm device created)
Flexible Layer            39 PASS (isolated UI/fake backend/XML round trip)
Macro timing               7 PASS
Modifier Safety              PASS
Release policy               PASS
Updater                    43 PASS
Update network             18 PASS (injected failures; no network request)
UI Safety / diagnostics    26 PASS
Productivity              360 PASS
Output Ownership      303,716 PASS
Settings smoke zh/en normal+narrow PASS
Legacy XML / gamepad-vector smoke PASS
```

Important new coverage:

- takeover runtime health: slot loss, Router loss, expected source loss, cloak off, hidden-device removal, whitelist loss;
- one transient failure tolerated; healthy sample resets streak; two consecutive failures signal one recovery;
- arbitrary named layers can coexist;
- keyboard layer-switch target selection;
- Base may have its own return shortcut;
- deleting active arbitrary layer returns to Base;
- deleting layer moves macros to Base;
- deleted legacy Layer 1 survives real XML save/reload;
- Runtime Observation/Diagnostics expose Layer + takeover platform state without initializing ViGEm in isolated host.

### Input Lab beta.2 release result

Latest run against the final `1.3.1-beta.2` release tree:

- all 18 pre-XInput real `SendInput` / ordinary-concurrency / complex-Hold checks PASS;
- Runtime Observation in the real-input lanes identifies the executable as `InputStitch 1.3.1-beta.2` and reports Base-only / no isolated virtual slot without initializing ViGEm just for observation;
- `failures=0`;
- controller preflight then hits the known laptop ViGEm→XInput observation environment blocker:

```text
SUMMARY: BLOCKED | checks=18 | failures=0
```

This remains environment `BLOCKED`, not a product pass/failure. Do not reinstall system drivers solely to force it green without a separate troubleshooting decision.

### Input Lab v0.3.0 developer-tool milestone

Input Lab now has its own explicit `0.3.0` version metadata and a versioned packaging path. This is separate from the InputStitch application version.

Manual viewer changes:

- added a target-window Win32 message lane that records `WM_KEY*` / `WM_MOUSE*` messages actually delivered to the Input Lab form or its child controls, including target HWND/control and message metadata;
- redesigned the keyboard from the compact seven-row v0.2 layout into a larger six-row layout with larger keycaps and more familiar navigation/arrow placement;
- strengthened held-key colors and added a 180 ms release afterglow so normal quick taps remain visibly highlighted instead of disappearing almost immediately;
- added built-in `--self-test-window-messages` and `--self-test-keyboard-visual` checks;
- added `tools/InputLab/package.ps1`, producing a versioned standalone x64 EXE, ZIP and SHA-256 checksums under `artifacts/InputLab/release/`;
- added a dedicated GitHub workflow for Input Lab verification/artifacts and `inputlab-v*` prerelease publication. Input Lab releases must stay prerelease so they do not replace the Stable InputStitch `releases/latest` endpoint.

Verification on 2026-09-10:

```text
window-message self-test:  PASS
keyboard-visual self-test: PASS
full InputStitch regression: PASS
Input Lab acceptance run 1: SUMMARY: PASS | checks=77 | failures=0
Input Lab acceptance run 2: SUMMARY: PASS | checks=77 | failures=0
Input Lab acceptance run 3: SUMMARY: PASS | checks=77 | failures=0
```

The older laptop ViGEm→XInput preflight blocker has therefore not reproduced in these three consecutive v0.3.0 verification runs. Keep the historical `BLOCKED` handling because that environment issue was intermittent; do not erase the distinction between `BLOCKED` and `FAIL`.

### Real neutral slot-order probe

`tools/InputLab/run-slot-order-probe.ps1` creates four temporary neutral ViGEm Xbox devices and submits no button/stick/trigger report.

Verified real ordering:

```text
initial: InputStitch test device = 3; external test devices = 0/1/2
re-enumerated with InputStitch first: InputStitch = 0; external = 1/2/3
EXPECTED_ORDER=True
```

This verifies the Windows/ViGEm connection-order mechanism at the current four-slot maximum but does not replace physical PnP + HidHide + game acceptance.

### Published beta.1 verification

`v1.3.1-beta.1` was published after local verification, a diagnostic GitHub CI pass, and a final `[publish-beta]` workflow pass.

- tag: `v1.3.1-beta.1`;
- tag commit: `303704878669ddad8f65bbcf2fe9374a2d83a00d`;
- Release is public, `prerelease=true`, `draft=false`;
- x64, x86, Source.zip, `InputStitch-beta.xml`, `SHA256SUMS.txt` present;
- GitHub reported SHA-256 digest for all five assets;
- `releases/latest` remains Stable `v1.3.0`.

Do not mutate this release/tag.

### beta.2 CI backup-ordering issue found and fixed

The first beta.2 non-publishing verification commit `7d17309` passed GitHub `windows-2022`. The first publish-trigger commit `f505ceb` changed no source but its repeated verify exposed a pre-existing nondeterministic `ProductivityTests.TestSave` failure: `Expected retained revision 3`.

Root cause was real product behavior, not Layer/Controller code. Valid configuration backups are retained by descending file name. Backup names used `DateTime.UtcNow` formatted with seven fractional digits plus a random GUID, but Windows wall-clock resolution can return the exact same `UtcNow` value for several rapid saves. When timestamps tied, random GUID ordering accidentally decided which backup was considered newer.

Fix in `ConfigStore`:

- valid-backup sort timestamps are allocated strictly monotonically;
- allocation considers current UTC, the latest in-process allocation for that backup directory, and the latest valid-backup timestamp already present on disk;
- this makes ordering stable across rapid same-clock saves, short clock rollback, and process restart;
- GUID remains only a uniqueness suffix, never a retention-order tie breaker.

Deterministic regression coverage now forces all rapid saves to observe one frozen UTC value and separately simulates a cold directory containing a future-dated valid backup while the new process clock is rolled back to 2001. Both paths retain chronological save order. Productivity coverage is now **360 PASS**.

A new non-publishing GitHub verification commit `cfe906c` passed the full `windows-2022` verify after this fix. The final no-source-change publish trigger `fbb9cef` then passed the same verify and published beta.2.

### Published beta.2 verification

All beta.2 release gates are satisfied:

- tag: `v1.3.1-beta.2`;
- tag commit: `fbb9cefc6efdbbd1e0f1d9efb036de019b07723f`;
- GitHub workflow `34383569935`: `completed / success`;
- Release is public, `prerelease=true`, `draft=false`;
- x64, x86, Source.zip, `InputStitch-beta.xml`, `SHA256SUMS.txt` are present;
- GitHub reports SHA-256 digests for all five assets;
- `releases/latest` still resolves to Stable `v1.3.0` (`prerelease=false`).

`v1.3.1-beta.2` is now immutable release history. Future work must use a new version/tag.

### Published Stable 1.4.0 verification

`v1.4.0` promotes the validated 1.3.1 Beta controller-input/routing and flexible-Layer tree into Stable while keeping Controller Takeover explicitly Experimental.

- non-publishing Stable candidate commit: `c998ddb`; GitHub workflow `34390506022`: `completed / success`;
- final no-source-change publish commit / tag target: `ab2639e2ac6354013f3182cf4e921dabe36ae6fb`;
- tag: `v1.4.0`;
- `releases/latest` resolves to `v1.4.0`;
- Release page and all five expected Stable assets return HTTP 200;
- x64 remote asset: 750,080 bytes, SHA-256 `39b0d9bb527ab195375694ac630461fe0aa0c423de4d7d2dac40e7cb80f365c8`;
- x86 remote asset: 750,592 bytes, SHA-256 `ca712a9fd57ab15271f57ef6582a650d13427ebb3bbbe78024497a33f4130a99`;
- Source.zip SHA-256 `ae768be60da7af484864ef312295607a6a73fdff6a1d981a63f5ea79ab194a20`;
- `InputStitch-update.xml` SHA-256 `6925987e5b69fb840249d67f5ebe581783dac54e7481e7138aaf903ce111a35f` and advertises `<Version>1.4.0</Version>` through the Stable `releases/latest/download` endpoints;
- downloaded remote x64/x86 EXEs report `ProductName=InputStitch`, `ProductVersion=1.4.0`, `FileVersion=1.4.0.0`;
- downloaded remote x64/x86/Source.zip/manifest hashes all match the published `SHA256SUMS.txt`;
- the main release workflow badge reports `passing` after publication.

`v1.4.0` is immutable Stable release history. Future release work must use a new version/tag. Physical controller + HidHide + target-game takeover acceptance remains blocked by missing hardware and does not change the Stable status of the rest of 1.4.0.

### Published Stable 1.4.1 hotfix verification

`v1.4.1` is a narrow UI-lifecycle hotfix prompted by a real post-update user log from 1.4.0. The update transaction itself completed successfully, but the new 1.4.0 process later raised `ObjectDisposedException` from WinForms `ContextMenuStrip` / `ModalMenuFilter` handling.

Root cause and fix:

- `ShowLayerManagementMenu()` synchronously disposed its temporary `ContextMenuStrip` from the menu `Closed` event while WinForms could still be completing `ToolStripDropDown.OnItemClicked` / visibility cleanup;
- Layer Management and Quick Create now share `DeferContextMenuDispose`, which posts disposal to the next UI message turn;
- MainForm shutdown no longer synchronously disposes `toolsMenu` / `trayMenu`, because a menu-triggered shutdown can leave WinForms' `ModalMenuFilter` holding a reference until the current click/close message fully unwinds;
- UI Safety / diagnostics coverage increased from 23 to **26 PASS**, directly checking deferred disposal and the disposed-dispatcher boundary;
- no feature or configuration-format changes were introduced.

Release evidence:

- non-publishing candidate commit: `d14b346`; GitHub workflow `34396249147`: `completed / success`;
- final no-source-change publish commit / tag target: `592e2febaab66636f8139d8264261c1906d74d99`;
- publish workflow `34396527972`: `completed / success`;
- tag: `v1.4.1`, exact target `592e2febaab66636f8139d8264261c1906d74d99`;
- `releases/latest` resolves to `v1.4.1`, `prerelease=false`, `draft=false`;
- x64 remote asset: 747,008 bytes, SHA-256 `69193ead5dcb37813a1e7350c1d0b34f4884512cc1d9d4cb29d1647859880c60`;
- x86 remote asset: 748,032 bytes, SHA-256 `8249848f98fd15c4547a25b9bdceb382435268c806ce89a27a7e9362f5b4258e`;
- Source.zip SHA-256 `31a2b93146c71e2e95d14738c62f39fff55b8d597c345b11995f4c93d4c98a2f`;
- `InputStitch-update.xml` SHA-256 `06c48ddbabd0cab9e5a71473c5aec1adf73bed3a432caee4c4f31da322789737` and advertises `<Version>1.4.1</Version>` through Stable `releases/latest/download` endpoints;
- remote-download verification independently downloaded x64/x86/Source.zip/manifest and matched all four hashes against `SHA256SUMS.txt`;
- downloaded x64/x86 EXEs report `ProductName=InputStitch`, `ProductVersion=1.4.1`, `FileVersion=1.4.1.0`;
- Source.zip contains the repaired `InputStitch.cs`, `tests/UiSafetyTests.cs`, release metadata and current documentation;
- local full regression, Input Lab `18 PASS / failures=0` before the known environment blocker, and neutral four-slot probe `EXPECTED_ORDER=True` all passed before publication.

The older log also contained historical 1.3.0 `Nefarius.ViGEm.Client` resolution failures. Both the saved 1.3.0 executable and current 1.4.x executable contain the embedded ViGEm client resource, and isolated `EmbeddedDependencyLoader.Register → GamepadOutput.NeutralizeAll` probes pass for both. That separate historical issue was therefore not modified without a current reproducer; investigate independently if it appears again under 1.4.1.

`v1.4.1` is now immutable Stable release history. Future release work must use a new version/tag.

### Published Stable 1.4.2 verification

`v1.4.2` moves Stable update transport to the project download domain first, preserves GitHub as automatic fallback, and ships the completed no-semantic-change refactor series (shared XInput/XML infrastructure, config/package serialization, virtual output backend boundary, target/profile policy extraction and update UI coordination).

Release evidence:

- non-publishing candidate commit: `2763bbc`; GitHub workflow `34485723355`: `completed / success`, including the new Python 3.12 R2-manifest derivation gate;
- final no-source-change publish commit / exact tag target: `47b0d9204de3674ff6d15c5bc685eb2288a4d6d6`;
- publish workflow `34486009244`: `completed / success`; `verify`, `publish-stable` and `mirror-stable-r2` all succeeded, while Beta jobs were skipped;
- tag/release: `v1.4.2`, `draft=false`, `prerelease=false`, five expected assets present;
- published x64 SHA-256 `d293f94def9f12e8af00fdf611aadc6fb167c030ff95460e6dfaf746d910faf2`, 756,224 bytes, `ProductVersion=1.4.2`, `FileVersion=1.4.2.0`;
- published x86 SHA-256 `cd62b48df3aa6d65df023f3773e1ac7667b3e8b517fe582e805b3c63dce91a4a`, 757,248 bytes, `ProductVersion=1.4.2`, `FileVersion=1.4.2.0`;
- Source.zip SHA-256 `2e7f4af5a2c57b6f0a8bb383753657b45239802f01ceef0e40936a0f8a94bafa`; GitHub `InputStitch-update.xml` SHA-256 `c798abba127087f1f8b0c18e2fcd1f0df82a27ec7350916004350a287437d69c`;
- live GitHub Stable manifest advertises 1.4.2 with GitHub `releases/latest/download` asset URLs, preserving the URL policy expected by the real 1.4.1 updater;
- live R2 `/latest/InputStitch-update.xml` advertises the same 1.4.2 file names and SHA-256 values but version-pinned `https://download.zhihanyu.com/releases/v1.4.2/...` asset URLs;
- direct downloads of both versioned R2 and versioned GitHub x64/x86 assets produced identical hashes; the website `/latest/InputStitch-Windows-x64.exe` alias also matches the x64 manifest hash and reports 1.4.2 / 1.4.2.0;
- a real in-memory `v1.4.1` x64 assembly probe calling its original `UpdateManager.CheckAsync()` returned `RemoteVersion=1.4.2`, `IsAvailable=True`, and an accepted official GitHub x64 asset URL;
- a real `v1.4.2` x64 assembly probe calling `UpdateManager.CheckAsync()` resolved the primary manifest/asset through `download.zhihanyu.com`; its compiled fallback manifest constant remains the official GitHub Stable manifest.

`v1.4.2` is now immutable Stable release history. Future release changes must use a new version/tag rather than replacing this release.

### Published Stable 1.4.3 verification

`v1.4.3` is the keyboard-hotkey reliability hotfix release. It contains the Raw Input fallback / keyboard-hook recovery work from code commit `e0ef345a0d99df8a6b87fc172fdabe96a9a35c11` plus the reviewed 1.4.3 metadata, public documentation and release notes.

Release evidence:

- non-publishing Stable candidate commit: `19c67d0371670d918a53b4ab72276069347a2715`; release workflow `34604398983`: `completed / success`, including x64/x86 verification, Python 3.12 R2-manifest derivation and the full regression suite; Input Lab workflow `34604399127`: `completed / success`;
- final no-source-change publish commit / exact tag target: `3db9fd0e4fb54a1966b121c9b0f34cc47de55992`; its Git tree is exactly the candidate tree `a72b3113fee6339274c75311b216d41fe0afcd5c`;
- publish workflow `34604611936`: `completed / success`; `verify`, `publish-stable` and `mirror-stable-r2` all succeeded; publish-commit Input Lab workflow `34604612018`: `completed / success`;
- tag/release: `v1.4.3`, `draft=false`, `prerelease=false`, five expected assets present, and `releases/latest` resolves to `v1.4.3`;
- published x64: 762,368 bytes, SHA-256 `a643c18b4cc4b0a5c5a862253cc1a9edc563bc46d6b88530a63e11b7415e7af9`, `ProductVersion=1.4.3`, `FileVersion=1.4.3.0`;
- published x86: 762,880 bytes, SHA-256 `60f8027d1236ee859d36db925b62b49d389f7d98ba2748a0d876b50ef39ec049`, `ProductVersion=1.4.3`, `FileVersion=1.4.3.0`;
- Source.zip: 602,093 bytes, SHA-256 `c9cc73f03060c72589013dc2946879c95abefceb6187f90702b298f8640c0bd2`; GitHub `InputStitch-update.xml` SHA-256 `36fd3997ddf4c5d335773ece270f231610a2c423c172b49053cf20cb363a3c35`; `SHA256SUMS.txt` SHA-256 `d6aed20782ed7e8d0fefbea3522721b6e0576bde4c932fa9a2aac85e48bd01a9`;
- live GitHub Stable manifest advertises `1.4.3` and official GitHub `releases/latest/download` assets; live R2 `/latest/InputStitch-update.xml` advertises `1.4.3`, the same x64/x86 hashes, and immutable `https://download.zhihanyu.com/releases/v1.4.3/...` asset URLs;
- direct downloads of GitHub x64/x86, versioned R2 x64/x86, and the website `/latest/InputStitch-Windows-x64.exe` alias all matched the published manifest hashes; all downloaded executables reported 1.4.3 / 1.4.3.0;
- a real saved `v1.4.2` x64 assembly invoked its own original `UpdateManager.CheckAsync()` and returned `RemoteVersion=1.4.3`, `IsAvailable=True`, `IsSameVersionReplacement=False`, with the primary x64 asset resolved to `https://download.zhihanyu.com/releases/v1.4.3/InputStitch-1.4.3-Windows-x64.exe`; this directly verifies that ordinary 1.4.2 users can discover the new Stable update;
- `v1.4.2` remained untouched: tag target is still `47b0d9204de3674ff6d15c5bc685eb2288a4d6d6`, release publication time is still 2026-09-10T14:00:19Z, and its published x64 remains 756,224 bytes with SHA-256 `d293f94def9f12e8af00fdf611aadc6fb167c030ff95460e6dfaf746d910faf2` and version 1.4.2 / 1.4.2.0.

`v1.4.3` is now immutable Stable release history and the recommended Stable line. Future release work must use a new version/tag.

## Public documentation rule

Public Simplified Chinese README/update text must be ordinary-user-first:

1. first sentence: what users can now do;
2. practical behavior and limitations in plain Chinese;
3. only then technical names such as XInput, ViGEm, Output Ownership, SourceId.

Technical design notes may use internal English type/API names freely.

## Next platform step after v1.4.3

The v1.4.3 keyboard-hotkey reliability incident is closed. Virtual Output Backend abstraction, persistent Device Identity, Router Source Policy, Analog Transform stage 1 and Activator + Condition stage 1 are now completed foundations on post-release `main`. Active non-hardware order is now:

1. Layer ergonomics: Momentary/Hold-to-Layer is **implemented** (2026-09-21, local commit); the remaining half is profile/context integration built on the shared DeviceKey + Condition model instead of adding new application-specific switches. A design proposal with the open product decisions is recorded in `docs/PROFILE_CONTEXT_DESIGN.md` (status: proposal, not implemented);
2. add Activator/Condition stage-2 semantics (Short Press, Triple Press, Turbo/repeat, another-input-held, richer analog regions) only where the lifecycle is explicitly defined;
3. evaluate `IInputProvider` + SDL3 after the action/condition boundary remains stable through those changes;
4. only after the output interface has remained stable through these changes, prototype/compare a second virtual-output backend when justified.

Hardware-dependent acceptance remains parked in parallel until a physical controller exists. When available:

1. validate physical starting layouts with InputStitch initially in slots 0/1/2/3;
2. validate HidHide while InputStitch remains whitelisted/readable;
3. prove a real game loses selected original devices and sees the routed slot-0 virtual controller;
4. intentionally break slot/Router/HidHide state and verify the new health monitor restores without doubled input;
5. only then call controller takeover hardware-mature.

Do not spend the next phase chasing controller-model count, gyro/touchpad/vendor haptics, complex Layer stacking or unrestricted plugins/scripts unless a concrete workflow justifies them.

## Important design constraints

- Do not regress Stable 1.3.0 source-local ownership semantics.
- Emergency Stop remains absolute priority.
- Output backend failure remains fail-closed.
- Macro/runtime/ownership/routing code must not become more tightly coupled to ViGEm; future output backends go behind a stable interface.
- XInput slot number is transient runtime state and must not become the durable user-facing device identity.
- InputStitch's own ViGEm output must never become an external trigger/router source.
- Respect `VirtualGamepadTypes.None`; incompatible features may refuse but must not silently recreate ViGEm.
- Controlled Replacement: **target slot not verified → no original-device hide**.
- Active takeover health failure must not leave Router generating duplicate routed input after originals are restored.
- Hiding must be explicit and recoverable; preserve pre-existing HidHide state.
- Do not write a kernel filter driver while a mature external hiding solution is sufficient.
- Layer remains eligibility/grouping on top of the common runtime, never a second execution engine.
- Analog transforms, activators and conditions must remain observable/testable and must not bypass Output Ownership or the common runtime.
- Observation/diagnostics must not create/connect virtual devices merely to inspect status.
- Stable and Beta release/update channels remain separate.
- Complete regression before every runtime-changing commit.

## Resume checklist on another computer

```text
1. Read AGENTS.md
2. Read PLAN.md
3. Read ROADMAP.md
4. Read docs/PRODUCT_STRATEGY.md
5. Read HANDOFF.md
6. git log -8 --oneline --decorate
7. git status --short --branch
8. git fetch / git pull as appropriate
9. Confirm expected version and working-tree baseline before editing
```

On a new development machine, verify Git, .NET Framework 4.7.2 reference assemblies/build path, embedded ViGEm client dependency and GitHub authentication before release work.
### Input Lab v0.3.1 interactive-safety hotfix (2026-09-10)

During local development verification, the operator observed unexpected repeating keyboard characters. Passive low-level-hook and Raw Input probes traced the repeating W/E/S events to the `HID\\VID_35BB&PID_D3FC&MI_00` HECATE HID path, and the operator then confirmed the actual cause was a macro configured in the mouse driver. This was not an InputStitch/InputLab SendInput loop. The safety changes below are retained because interactive automated tests should still contain injected desktop input.

Safety changes made in response:

- removed `keybd_event` from the Input Lab keyboard-visual self-test; the visual self-test is now entirely in-process and emits no Windows keyboard input;
- automation-mode Input Lab hooks now swallow every injected keyboard and mouse event after observing it, replacing the former K/X2-only special case;
- `run-acceptance.ps1` now requires explicit `-AllowRealOutput`; use it only with an idle desktop and no other InputStitch instance;
- the normal packaging path still runs only the self-contained window-message and keyboard-visual self-tests; it does not invoke the real-output ownership acceptance host;
- `MainForm.EmergencyStop` now isolates a final `GamepadOutput.NeutralizeAll()` exception so hook reconciliation/status completion still runs even if the optional virtual-gamepad backend is unavailable.

Verification after the code change: Input Lab builds successfully; `--self-test-window-messages` exits 0; `--self-test-keyboard-visual` exits 0; a source scan finds no remaining `keybd_event`/`KEYEVENTF_KEYUP` symbols under `tools/InputLab`; the normal InputStitch x64/x86 build passes. The first normal full-regression attempt encountered one transient fake-backend OutputOwnership concurrency assertion; the same 303,716-check ownership suite then passed three consecutive immediate reruns. A fresh full-regression rerun subsequently passed completely, including `PASS output ownership: 303716 checks`, all Settings Smoke cases, old-XML compatibility and gamepad-vector smoke; no real input or virtual devices were used.

### Input Lab v0.3.1 keyboard-visual follow-up (2026-09-10)

Hands-on testing then found a separate viewer bug: mouse buttons highlighted correctly, but most physical keyboard presses were visible in the observation lanes without visibly highlighting their keycaps; the first Windows-key press could also open Start without highlighting. The keyboard layout also used six percentage-height rows, causing large vertical gaps in a tall window.

Fixes:

- keyboard Raw Input now uses a passive `RIDEV_INPUTSINK` lane in normal viewer mode and drives keycap state when the low-level keyboard hook has no matching recent event;
- recent low-level-hook events are de-duplicated against the Raw Input fallback so hook-derived injected/physical coloring remains authoritative when available;
- Raw Input left/right Shift, Ctrl and Alt are normalized before visualization;
- the keyboard was rebuilt on a shared quarter-key grid rather than independent per-row flow layouts: Backspace/Backslash/Enter/Right Shift/Right Ctrl share one right edge, the navigation cluster is a strict 3×2 grid, and the arrow cluster is a true inverted-T;
- the 180 ms key-release afterglow was removed after hands-on testing showed it was undesirable for precise state observation; key-up now clears the keycap immediately;
- the keyboard-visual self-test now exercises the Raw Input fallback path entirely in-process and verifies immediate release-to-idle without emitting real keyboard input.

Verification: `--self-test-window-messages=0`; `--self-test-keyboard-visual=0`, with the visual self-test now requiring immediate idle on key-up; direct `PrintWindow` capture of the 1220x840 Input Lab window confirmed the shared right edge, strict 3×2 navigation block, centered inverted-T arrows, compact rows and no horizontal scrollbar; the complete `tests/Run-Tests.ps1` suite passed including 303,716 OutputOwnership checks and explicitly used no real input or virtual devices; the normal InputStitch 1.4.1 x64/x86 release build/verification also passed. Do not run `run-acceptance.ps1 -AllowRealOutput` merely to verify this viewer fix.
