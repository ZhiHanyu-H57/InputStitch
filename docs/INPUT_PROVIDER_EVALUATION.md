# Input provider evaluation: GameInput vs SDL3

Status: decision document (2026-09-21). It answers the development plan H2.3 question: which provider to
adopt after defining `IInputProvider`. It also records the verified facts behind the choice. No runtime code
changed here.

状态：决策文档。回答开发计划 H2.3「输入 provider 抽象之后选谁」，并记录已验证的事实。

## Why this decision exists

Output already has a backend seam (`IVirtualGamepadBackend` / `IOutputBackend`), but the input side is
concrete: `HookManager` (keyboard/mouse), `RawKeyboardFallback` and `XInputInput`. Broad controller support
(PlayStation/Switch/HID gamepads, richer device metadata for `DeviceKey`) needs one provider interface first
(architecture-review finding F6), and then a second implementation to prove the seam.

## Requirements and constraints

* Portable distribution stays intact: one EXE per architecture, no installer, no admin requirement, no
  external runtime installation step.
* x86 and x64 both stay supported; the release pipeline builds both.
* The app manifest declares Windows 10 support today; raising the floor is allowed but must be deliberate.
* Deterministic behaviour, hot-plug support, per-device identity metadata (VID/PID/serial where available),
  low-latency polling, and no silent double-input when one device is readable through two lanes.
* License posture must not block a source-visible product with no open-source license declared.

## Verified facts (2026-09-21)

| Candidate | Facts | Consequence |
|---|---|---|
| SDL3 3.4.16 | zlib license; native `SDL3.dll` redistributable (x86/x64); all desktop Windows versions back to XP are still supported; wide gamepad database incl. HID/PlayStation/Switch; hot-plug plus gyro/haptics APIs; active project (latest release 2026-09-02) | Fits the portable model: ship one DLL, no install, no OS-floor change |
| ppy/SDL3-CS | MIT C# bindings, 184 stars, active (pushed 2026-09-10), packaged on NuGet | Useful reference, but modern-.NET-oriented: for .NET Framework 4.7.2 a small hand-written P/Invoke subset is the better fit |
| Microsoft GameInput 3.5.274 | NuGet package is native-only: `native/include/GameInput.h`, `native/lib/x64` plus `arm64` (no x86), `native/src/GameInput.cpp`, and `redist/GameInputRedist.msi`; PC access via NuGet; Windows 10 19H1+; license is the Microsoft GameInput Redistributable terms (installation rights, not a permissive licence) | Using it needs P/Invoke or a C++ shim, plus an installed/shipped MSI, plus a licence review, and it has no x86 native library: this breaks the portable no-installer model |
| Existing XInput path | Implemented and verified (26 checks, plus Input Lab acceptance history) | Stays as provider #1; no user-visible change |

## Decision

Adopt SDL3 as the second input provider behind a new `IInputProvider` seam. Keep GameInput as a later,
evidence-driven option.

Reasons:

1. SDL3 preserves the portable single-DLL, no-installer, x86+x64 model; GameInput needs an MSI install step
   and provides no x86 native library.
2. SDL3 is zlib-licensed and needs no legal review; GameInput's redistributable terms do.
3. The capability gap we actually need (more controller families plus per-device metadata) is covered by
   SDL3; GameInput's extra value (Microsoft-native haptics/sensors, GDK alignment) is not on the near-term
   roadmap.
4. If a concrete workflow later demands GameInput, the seam makes it additive, and the installer/licence
   questions become an explicit user decision instead of a silent architectural change.

## Integration design (proposed)

```text
IInputProvider
  string Name
  IReadOnlyList<InputDeviceDescriptor> EnumerateDevices()
  bool TryGetState(InputDeviceDescriptor device, out InputDeviceState state)   // poll, no per-call allocation
  events: DeviceArrived, DeviceRemoved

InputDeviceDescriptor: ProviderDeviceId, DisplayName, VendorId, ProductId, Serial, Kind
InputDeviceState: buttons bitmask, DPad, LX/LY/RX/RY/triggers, connected flag
```

* `XInputProvider` wraps today's `XInputInput` unchanged: the router, triggers and conditions keep consuming
  the same normalized state, so step 1 is behaviour-neutral.
* `Sdl3InputProvider` uses a hand-written P/Invoke subset (`SDL_InitSubSystem`, `SDL_GetGamepads`,
  `SDL_OpenGamepad`, `SDL_GetGamepadState`, `SDL_GetGamepadName`, hot-plug event watch); no NuGet dependency,
  roughly 200 lines of interop.
* `SDL3.dll` is embedded as a resource and extracted at runtime, exactly like the existing embedded ViGEm
  client pattern, so distribution stays a single EXE.
* `DeviceIdentityService` gains a provider-derived evidence source (VID/PID/serial/path) so SDL3 devices get
  stable `DeviceKey` records; unresolved mappings stay fail-closed as today.
* Provider selection is internal (XInput first for Xbox-family pads, SDL3 for everything else) with explicit
  duplicate suppression when both lanes see the same physical device.

## Staged work

1. **Done 2026-09-21** — seam plus XInput adapter: `IInputProvider` (`InputProvider.cs`) exists,
   `XInputInputService` implements it with its historical behaviour, the router now iterates the provider's
   device set instead of a fixed 0..3 range, and `tests/InputProviderSeamTests.cs` (15 checks) proves the
   seam with a fake provider, including a synthetic index above the XInput range. Existing router suites
   (28 + 22 checks) are unchanged and green.
2. **Provider logic done 2026-09-21; native layer done 2026-09-21** — `ISdl3Api` (`Sdl3Interop.cs`) and
   `Sdl3InputProvider.cs` implement enumeration, synthetic index assignment above the XInput range,
   state mapping (button mask, Y inversion, trigger scaling), button/trigger edges with XInput hysteresis,
   hot-plug with index reuse plus release-on-disconnect, and Microsoft-family duplicate suppression.
   `tests/Sdl3ProviderTests.cs` (29 checks) covers the logic with a fake SDL3 API, and `Sdl3NativeApi.cs`
   binds the real runtime dynamically (missing DLL degrades gracefully) using `SDL_Init(SDL_INIT_GAMEPAD)`,
   `SDL_GetGamepads`, the `...ForID` descriptor getters and per-control `SDL_GetGamepadButton`/`Axis` reads.
   An optional smoke (enabled by `INPUTSTITCH_SDL3_DLL`) ran against the official SDL3 3.4.16 x64 runtime and
   reported `device count=0; provider status=ready` with no controller attached. Still to do: decide how
   SDL3.dll ships (committed third-party folder vs restore script vs embedded resource), wire the provider
   into MainForm, add device-identity evidence and run Input Lab; real-device verification stays on the
   hardware-blocked lane.
3. **Wiring done 2026-09-21** — `CompositeInputProvider` merges XInput (0..3) and SDL3 (≥4) into one device
   index space; `MainForm` runs the composite so the SDL3 lane activates automatically when the runtime is
   present and stays inert otherwise; diagnostics report the provider status. Remaining: how SDL3.dll ships,
   device-identity evidence (VID/PID/serial) and a provider column in Device Manager; real-device
   verification stays on the hardware-blocked lane.

   Developer enablement is in place: `scripts/Restore-Sdl3.ps1` fetches and verifies the pinned 3.4.16
   assets into `third-party/SDL3/<arch>/` (gitignored, excluded from the source archive), and the SDL3 test
   suite picks the restored DLL up automatically. The end-user shipping choice (bundle the DLL with the
   release, embed it into the EXE like the ViGEm client, or keep SDL3 strictly optional) is still open.

## Risks and mitigations

* Extracted DLL flagged by antivirus: keep the existing embedded-resource pattern (already shipped for the
  ViGEm client) and document it; optionally allow a side-by-side `SDL3.dll` instead.
* Double input from a device visible through XInput and SDL3: prefer XInput for Xbox-family devices based on
  SDL's own gamepad type, and suppress the other lane for the same VID/PID/serial.
* SDL3 update cadence: pin a version, verify its SHA-256 in the build, and treat updates like any other
  third-party dependency (the repository already hash-checks the ViGEm client).

## Open decision for the user (not blocking stage 1)

Whether GameInput, and therefore an installer/redistributable step, should ever be adopted if a future
workflow needs Microsoft-native haptics or sensors. Default: no, until a concrete need appears.
