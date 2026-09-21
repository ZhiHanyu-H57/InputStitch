# Third-party notices

## Nefarius.ViGEm.Client 1.21.256

InputStitch embeds the official `Nefarius.ViGEm.Client` managed NuGet package so
the downloadable InputStitch executable remains self-contained. The library is
licensed under the MIT License. It includes the native ViGEmClient library and
Costura loader. Their complete notices are retained in:

- `third-party/Nefarius.ViGEm.Client/LICENSE.txt` (ViGEm.NET managed library)
- `third-party/Nefarius.ViGEm.Client/ViGEmClient-LICENSE.txt` (native client)
- `third-party/Nefarius.ViGEm.Client/Costura-LICENSE.txt` (embedded loader)

- Package: https://www.nuget.org/packages/Nefarius.ViGEm.Client/1.21.256
- Source: https://github.com/nefarius/ViGEm.NET
- Embedded DLL SHA-256: `4458301000b732d115521e99f9936f4edb70d6ceb3036ef158715e0e6b8902e0`

Virtual controller output also requires the separately installed ViGEmBus
driver. InputStitch does not redistribute or silently install that driver.
ViGEmBus is a retired upstream project; obtain the last official signed release
only from https://github.com/nefarius/ViGEmBus/releases/latest .

## SDL 3.4.16 (optional input lane)

The optional SDL3 input lane embeds the official SDL3 runtime (`SDL3.dll`) so the
download stays a single executable. SDL is licensed under the zlib licence; the
complete notice ships inside the official archive and is extracted next to the
developer copy at `third-party/SDL3/<arch>/LICENSE.txt`.

- Project: https://github.com/libsdl-org/SDL
- Release asset: `SDL3-3.4.16-win32-<arch>.zip`
- Embedded x64 SHA-256: `1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0`
- Embedded x86 SHA-256: `47d508b4232cf9462096bdb6f613a330d9a94a7eb09c305fad073430c6531920`

The binary is deliberately **not** committed to the repository:
`scripts/Restore-Sdl3.ps1` fetches and verifies it during the build, and
`build.ps1` embeds the per-architecture copy into the executable as
`InputStitch.Sdl3.<arch>.dll`. Without the lane the application behaves exactly
like the XInput-only build.
