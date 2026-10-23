[CmdletBinding()]
param([switch]$Run)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$refs=& (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources=@(& (Join-Path $project 'scripts/Get-ProductionSources.ps1'))
$output=Join-Path $project 'artifacts/network-acceptance'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$probe=Join-Path $output 'NetworkProbe.exe'
& $compiler /nologo /warnaserror+ /target:exe /platform:x64 /main:InputStitch.NetworkAcceptance.NetworkProbe ("/out:$probe") (Join-Path $PSScriptRoot 'LiveNetworkProbe.cs')
if($LASTEXITCODE -ne 0) { throw 'Network probe compilation failed.' }
Copy-Item -LiteralPath $probe -Destination (Join-Path $output 'NegativeProbe.exe') -Force
$hostPath=Join-Path $output 'LiveNetworkAcceptance.exe'
$arguments=@('/nologo','/noconfig','/nostdlib+','/warnaserror+','/target:exe','/platform:x64','/main:InputStitch.LiveNetworkAcceptance',('/out:'+$hostPath))
foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments+='/reference:'+(Join-Path $refs $name) }
$client=Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments+='/reference:'+$client
$arguments+='/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments+=@(& (Join-Path $project 'scripts/Get-UiResourceArguments.ps1'))
$arguments+=$sources
$arguments+=Join-Path $PSScriptRoot 'LiveNetworkAcceptance.cs'
& $compiler @arguments
if($LASTEXITCODE -ne 0) { throw 'Live network host compilation failed.' }
if(-not $Run) { Write-Output 'Live network harness built; zero network changes. Use -Run for executable-only acceptance with UAC.'; return }
& $hostPath --allow-process-network-test | Tee-Object -FilePath (Join-Path $output 'live-network-results.txt')
if($LASTEXITCODE -ne 0) { throw 'Executable-only live network acceptance failed.' }
