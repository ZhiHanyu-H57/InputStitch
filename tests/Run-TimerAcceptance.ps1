[CmdletBinding()]
param([ValidateSet('Desktop','Rtss')][string]$Backend = 'Desktop', [ValidateSet('x64','x86')][string]$Architecture = 'x64')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
if ($Architecture -eq 'x86' -and [Environment]::Is64BitProcess) {
  & (Join-Path $env:WINDIR 'SysWOW64/WindowsPowerShell/v1.0/powershell.exe') -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath -Backend $Backend -Architecture $Architecture
  exit $LASTEXITCODE
}
$project=Split-Path $PSScriptRoot -Parent
$references=& (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources=@(& (Join-Path $project 'scripts/Get-ProductionSources.ps1'))
$output=Join-Path $project ('artifacts/timer-acceptance/'+$Architecture)
New-Item -ItemType Directory -Force -Path $output | Out-Null
$suite=if($Backend -eq 'Rtss'){'TimerD3DAcceptance'}else{'TimerTests'}
$exe=Join-Path $output ($suite+'.exe')
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$arguments=@('/nologo','/noconfig','/nostdlib+','/warnaserror+','/target:exe',('/platform:'+$Architecture),('/main:'+$suite),('/out:'+$exe))
foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments+='/reference:'+(Join-Path $references $name) }
$client=Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments+='/reference:'+$client
$arguments+='/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments+=@(& (Join-Path $project 'scripts/Get-UiResourceArguments.ps1'))
$arguments+=$sources
$arguments+=Join-Path $PSScriptRoot ($suite+'.cs')
& $compiler @arguments
if($LASTEXITCODE -ne 0) { throw 'Timer acceptance compilation failed.' }
$runArgs=@()
if($Backend -eq 'Desktop') { $runArgs+='--desktop' }
& $exe @runArgs | Tee-Object -FilePath (Join-Path $output ($Backend+'.log'))
if($LASTEXITCODE -eq 2) { Write-Output 'BLOCKED by optional RTSS environment; no product pass claimed.'; exit 2 }
if($LASTEXITCODE -ne 0) { throw 'Timer acceptance failed.' }
