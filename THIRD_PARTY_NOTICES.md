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
