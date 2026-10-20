[CmdletBinding()]
param([ValidateRange(0, 1440)][int]$SoakMinutes = 0)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources = & (Join-Path $project 'scripts/Get-ProductionSources.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $project 'artifacts/crosshair-tests'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output 'CrosshairTests.exe'
$client = Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+','/main:CrosshairTests',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += '/reference:' + $client
$arguments += '/resource:' + $client + ',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments += $sources
$arguments += Join-Path $PSScriptRoot 'CrosshairTests.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Crosshair acceptance compilation failed.' }
if ($SoakMinutes -gt 0) { & $exe --soak-minutes $SoakMinutes } else { & $exe }
if ($LASTEXITCODE -ne 0) { throw 'Crosshair acceptance failed. See artifacts/crosshair-tests/crosshair/failure.txt.' }
