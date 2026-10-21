[CmdletBinding()]
param([switch]$Native, [switch]$SettingsOnly)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources = & (Join-Path $project 'scripts/Get-ProductionSources.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $project 'artifacts/secondary-tests'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output $(if ($Native) { 'SecondaryLayoutNativeTests.exe' } else { 'SecondaryLayoutTests.exe' })
$client = Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+','/main:SecondaryLayoutTests',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += '/reference:' + $client
$arguments += '/resource:' + $client + ',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments += $sources
$arguments += Join-Path $PSScriptRoot 'SecondaryLayoutTests.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Secondary layout compilation failed.' }
$testArgs = @()
if ($Native) { $testArgs += '--native' }
if ($SettingsOnly) { $testArgs += '--settings-only' }
& $exe @testArgs
if ($LASTEXITCODE -ne 0) { throw 'Secondary layouts failed. See artifacts/secondary-tests/secondary-layout*/issues.txt.' }
