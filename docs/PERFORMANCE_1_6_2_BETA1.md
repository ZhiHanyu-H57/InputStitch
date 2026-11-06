# 1.6.2-beta.1 performance maintenance

Baseline: Stable 1.6.1, main `dada9926a0ea3201a1e07c97fa1bdd4d17b1ff5b`.
Scope: reduce recurring input-processing work and fix defects found along those
paths. No new feature, dependency, driver or configuration format.

## Primary-source research

- [AutoHotkey v2 hook implementation](https://github.com/AutoHotkey/AutoHotkey/blob/v2.0/source/hook.cpp)
  uses modifier/key lookup tables before posting matched hotkeys to its main
  window (lookup definitions near lines 53–71, dispatch near 1702 and 1942).
  Applicable principle: reject unrelated input cheaply before running expensive
  action-specific work. This pass keeps InputStitch's existing conflict checks
  for matched tool keys without adding a configuration cache.
- [Microsoft LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)
  documents callback timeouts and silent hook removal after timeout. Reducing
  synchronous work in the callback improves its margin; the deadline itself is
  not a target latency. Existing input suppression, capture and Emergency Stop
  ordering remain authoritative.
- [SDL_GetGamepads](https://wiki.libsdl.org/SDL3/SDL_GetGamepads) returns a
  caller-owned list that must be released with SDL_free.
  [SDL_UpdateGamepads](https://wiki.libsdl.org/SDL3/SDL_UpdateGamepads) permits
  explicit state updates without an event loop. Keep those native lifetime and
  update contracts; reuse only private managed scratch collections under the
  provider lock. External edge objects remain independent.
- [G-Helper Settings](https://github.com/seerge/g-helper/blob/main/app/Settings.cs)
  enables its sensor timer only when needed or explicitly configured for
  continuous sampling. InputStitch already stops paused/hidden timer painting
  and empty advanced-trigger UI dispatch. Those paths are retained rather than
  replacing them with a new scheduling layer.

## Changes

`MainForm.Tools.cs` checks the incoming key against active timer/network keys
before running the existing complete conflict validation. Nonmatching input
allocates no temporary arrays or queued-action closures. There is no binding
cache to invalidate: macro, layer and tool edits still apply on the next event.
Network restoration retains priority while blocked or an operation is pending.

`Sdl3InputProvider.cs` reuses private membership/removal scratch collections under
its existing lock, creates edge lists only for actual changes, and formats status
only when device count changes. Suppressed Xbox metadata is read once per SDL
connection ID, removed on disconnect; [SDL documents that reconnect gets a new
ID](https://wiki.libsdl.org/SDL3/SDL_JoystickID). Update, enumeration and connected
state reads still run every input tick. Native allocations and SDL_free remain
unchanged; published edge objects are never recycled.

Disconnect and disposal now emit releases for latched left/right triggers as
well as ordinary buttons, after clearing state and outside the lock. The original
provider reproduced 3 presses but only 1 release (button + LT + RT); the fixed
provider emits all 3, once. Repeated Dispose/Poll stays inert and metadata and
topology are cleared. Enumeration failure no longer overwrites its diagnostic
with a ready message in the same tick.

## Same-host measurements

Both runners warm up before sampling, use three samples, emit no input and touch
no user configuration. Allocation is measured with the runtime's per-thread byte
counter. Native SDL costs are excluded from the provider microbenchmark. Medians:

| Path | Work/sample | Stable 1.6.1 | Beta | Allocated bytes, old → beta |
| --- | ---: | ---: | ---: | ---: |
| Unrelated tool key, 0 macros | 20,000 events | 6.801 ms | 1.269 ms | 16,480,000 → 0 |
| Unrelated tool key, 100 macros | 20,000 events | 66.638 ms | 1.174 ms | 16,480,000 → 0 |
| Unrelated tool key, 1,000 macros | 20,000 events | 642.205 ms | 1.129 ms | 16,480,000 → 0 |
| SDL provider, no devices | 300,000 polls | 29.10 ms | 14.60 ms | 96,000,000 → 0 |
| SDL provider, one active device | 300,000 polls | 60.69 ms | 46.44 ms | 136,800,000 → 0 |
| SDL provider, one suppressed Xbox | 300,000 polls | 34.66 ms | 25.30 ms | 127,200,000 → 0 |

Every provider sample still makes 300,000 Update calls; active samples make
300,000 snapshot reads. Its Gen0 collections fall from 15–22 to 0. This is a local
hot-path measurement; other application/native paths still allocate and run.

## Automated verification

- 14 impact-selected suites pass: provider/SDL/composite/device identity, input
  pipeline, router, UI safety, ownership, timer/network/tool integration, source
  manifest and release policy. Includes 66 SDL checks, 282 tool checks, 58 UI
  safety checks and 303,716 ownership checks.
- An added provider → actual MainForm controller callbacks → held mappings →
  output-ownership workflow passes 100 disconnect/reconnect cycles. Both trigger
  outputs release, unrelated ownership survives, held reconnect establishes a
  baseline, and disposal leaves balanced output. No periodic release fallback is
  needed. Existing UI-blocked long-press sample remains 150 ms with 400 ms UI
  blocking (delivered input seam, observer output).
- Native SDL3 loads successfully; current host has zero physical gamepads.
  This is library loading/enumeration evidence, not physical-device acceptance.
- Private-desktop real ViGEm/XInput Emergency Stop: 11/11 pass, including no
  immediate retrigger, explicit menu resume and final neutral state. No keyboard
  or mouse SendInput scenarios ran.
- Both final x64/x86 packages pass metadata/hash/resource verification, startup,
  compressed-runtime cold extraction/cache repair/reuse, saved overlay state and
  orderly shutdown. Settings smoke passes zh/en default/compact windows and old
  XML defaults. No configuration schema change or updater change.

All window/resource/native-controller checks use private desktops without
activating them. No host network interruption, user configuration modification,
unrelated OBS/network retest, full suite, repeated macro soak or manual gate.

Final packages: x64 2,180,096 bytes; x86 2,010,112 bytes. Each is exactly 1,024 bytes
larger than Stable 1.6.1, with no new dependency. Delivery and raw verification
are under `outputs/beta162-1` in the task workspace. Published Stable remains
1.6.1; this task does not publish a new Stable version.
