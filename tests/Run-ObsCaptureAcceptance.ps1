[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$CompilerPath,
    [string]$ObsInstallPath,
    [string]$PythonPath='python',
    [string]$FfmpegPath='ffmpeg',
    [string]$OutputDirectory,
    [switch]$Run
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$fixture=Join-Path $PSScriptRoot 'obs-capture'
if(-not $OutputDirectory) { $OutputDirectory=Join-Path $project 'artifacts/obs-capture' }
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$native=Join-Path $OutputDirectory 'native-game'
New-Item -ItemType Directory -Force -Path $OutputDirectory,$native,(Join-Path $OutputDirectory 'evidence'),(Join-Path $OutputDirectory 'recordings') | Out-Null
foreach($file in Get-ChildItem -LiteralPath $fixture -File) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $OutputDirectory $file.Name) -Force
}
$target=Join-Path $native 'InputStitchNativeD3D9Target.exe'
& $CompilerPath -Werror -Wall '-Wl,-subsystem=windows' -o $target (Join-Path $fixture 'InputStitchNativeD3D9Target.c') -luser32 -lgdi32
if($LASTEXITCODE -ne 0) { throw 'Native D3D9 capture target compilation failed.' }
$bytes=[IO.File]::ReadAllBytes($target); $pe=[BitConverter]::ToInt32($bytes,0x3c); $optional=$pe+24
if([BitConverter]::ToUInt16($bytes,$pe+4) -ne 0x8664 -or [BitConverter]::ToUInt16($bytes,$optional) -ne 0x20b -or [BitConverter]::ToUInt32($bytes,$optional+112+14*8) -ne 0) { throw 'Capture target must be native x64 with no CLR.' }
$refs=& (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources=@(& (Join-Path $project 'scripts/Get-ProductionSources.ps1'))
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$arguments=@('/nologo','/noconfig','/nostdlib+','/warnaserror+','/target:exe','/platform:x64','/main:TimerOsdProducer',('/out:'+(Join-Path $OutputDirectory 'TimerOsdProducer.exe')))
foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments+='/reference:'+(Join-Path $refs $name) }
$client=Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments+='/reference:'+$client; $arguments+='/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments+=@(& (Join-Path $project 'scripts/Get-UiResourceArguments.ps1'))
$arguments+=$sources; $arguments+=Join-Path $fixture 'TimerOsdProducer.cs'
& $compiler @arguments
if($LASTEXITCODE -ne 0) { throw 'Production timer capture producer compilation failed.' }
if(-not $Run) { Write-Output ('Built only; no OBS/window/profile changes: '+$OutputDirectory); return }
& $PythonPath -c 'import PIL, websocket'
if($LASTEXITCODE -ne 0) { throw 'Acceptance Python needs Pillow and websocket-client.' }
$mapping=[IO.MemoryMappedFiles.MemoryMappedFile]::OpenExisting('RTSSSharedMemoryV2'); $mapping.Dispose()
Write-Output 'RTSS Microsoft Detours compatibility must already be enabled for the dedicated capture target. The recorder does not change RTSS profiles.'
Push-Location $OutputDirectory
try {
    & (Join-Path $OutputDirectory 'Start-PortableObs.ps1') -ObsInstallPath $ObsInstallPath
    & $PythonPath (Join-Path $OutputDirectory 'run_detours_baseline.py') --target $target
    if($LASTEXITCODE -ne 0) { throw 'OBS baseline recording failed.' }
    & $PythonPath (Join-Path $OutputDirectory 'analyze_detours_baseline.py') --ffmpeg $FfmpegPath
    if($LASTEXITCODE -ne 0) { throw 'OBS game capture is not dynamic; timer cases were not executed.' }
    & $PythonPath (Join-Path $OutputDirectory 'run_detours_recordings.py') --target $target
    if($LASTEXITCODE -ne 0) { throw 'Timer capture recording failed.' }
    & $PythonPath (Join-Path $OutputDirectory 'analyze_detours_recordings.py') --ffmpeg $FfmpegPath
    if($LASTEXITCODE -ne 0) { throw 'Timer capture frame checks failed.' }
    & $PythonPath (Join-Path $OutputDirectory 'check_detours_timer_text.py')
    if($LASTEXITCODE -ne 0) { throw 'Recorded timer text did not change.' }
} finally {
    & (Join-Path $OutputDirectory 'Stop-PortableObs.ps1')
    Pop-Location
}
