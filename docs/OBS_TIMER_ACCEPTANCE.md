# OBS Game Capture acceptance

The automated recorder uses an isolated portable OBS copy, a native x64 D3D9
target, and the current production `TimerController`. It uses only the
`game_capture` source. Display Capture and Window Capture are not substitutes.
The native target draws a blue game frame, a moving green marker and a changing
frame number; it contains no CLR or WinForms. The timer producer is separate.

## Environment

- Existing OBS and running RTSS 7.3.7 or newer.
- Enable **Use Microsoft Detours API hooking** in RTSS for
  `InputStitchNativeD3D9Target.exe`. The default hooking mode froze this machine's
  OBS Game Capture even with no InputStitch timer running. A dedicated test
  profile with `[Hooking] UseDetours=1` resolved the native dynamic baseline.
  This does not require changing the user's existing game profiles.
- TinyCC x64 for the test target, and Python with Pillow/websocket-client plus
  ffmpeg for recording analysis. These are test tools, never app dependencies.
- A desktop available for the short native target/overlay visibility checks.

The tested portable compiler is the [official TinyCC 0.9.27 Windows archive](https://download.savannah.gnu.org/releases/tinycc/tcc-0.9.27-win64-bin.zip),
SHA256 `34A721949A2583FDFF725312DA092FA0F5F1F284B702E6F811C6954714FAABB2`.
The script accepts an existing compiler path and does not install it.

## Automated run

```powershell
./tests/Run-ObsCaptureAcceptance.ps1 -CompilerPath $tcc
# Build only above; no OBS launch or RTSS profile change.
./tests/Run-ObsCaptureAcceptance.ps1 -CompilerPath $tcc `
  -ObsInstallPath $obs -PythonPath $python -FfmpegPath $ffmpeg -Run
```

All binaries, portable OBS settings, recordings and logs stay under
`artifacts/obs-capture` or the supplied `-OutputDirectory`. The recorder creates
a private WebSocket credential and clears it during cleanup. It does not modify
the existing OBS installation's settings or RTSS profiles.

First, a six-second recording with no timer must show a continuously moving
green marker and changing frame hashes. A frozen baseline stops the sequence.
The next three recordings check:

| OBS third-party overlays | Enabled timers | Required recording |
| --- | --- | --- |
| On | RTSS and desktop | RTSS visible; desktop absent |
| Off | RTSS and desktop | Both absent |
| On | Desktop | Desktop absent |

Each video contributes 12 actual decoded frames. Every frame must contain the
game background and native marker, each video's marker must move, and its
frames must change. A real desktop screenshot must contain both timer colors,
so absence from Game Capture cannot be credited merely because the desktop
timer was hidden. The recorded RTSS text is also checked independently: its
yellow-only masks must change across frames, excluding the native marker and
frame counter. Recordings, source settings, OBS logs and JSON judgments remain
available for review.

The 2026-10-24 native x64 D3D9 run passed all four recordings and 48 decoded
frames with OBS 32.2.2. This establishes the tested capture path; other games
and graphics APIs retain their own compatibility scope.

Sources: [OBS Game Capture](https://obsproject.com/kb/game-capture-source),
[OBS compatibility troubleshooting](https://obsproject.com/kb/game-capture-troubleshooting),
[OBS portable mode](https://obsproject.com/kb/portable-mode).
