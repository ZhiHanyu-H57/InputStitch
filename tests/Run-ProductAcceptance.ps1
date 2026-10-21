[CmdletBinding()]
param([ValidateRange(0, 1440)][int]$SoakMinutes = 0, [switch]$DialogsOnly, [switch]$DensityOnly, [switch]$JourneyOnly, [switch]$NativeJourney, [switch]$FieldsOnly, [switch]$NativeFields)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$productionSources = & (Join-Path $project 'scripts/Get-ProductionSources.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $project 'artifacts/product-tests'
if ($DensityOnly) { $output = Join-Path $project 'artifacts/product-density' }
if ($JourneyOnly -or $NativeJourney) { $output = Join-Path $project ('artifacts/product-journey' + $(if ($NativeJourney) { '-native' } else { '' })) }
if ($FieldsOnly -or $NativeFields) { $output = Join-Path $project ('artifacts/product-fields' + $(if ($NativeFields) { '-native' } else { '' })) }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output 'ProductAcceptanceTests.exe'
$client = Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+','/main:ProductAcceptanceTests',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += '/reference:' + $client
$arguments += '/resource:' + $client + ',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
$arguments += $productionSources
$arguments += Join-Path $PSScriptRoot 'ProductAcceptanceTests.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Product acceptance compilation failed.' }
if ($SoakMinutes -gt 0) { & $exe --soak-minutes $SoakMinutes } elseif ($NativeFields) { & $exe --fields-native } elseif ($FieldsOnly) { & $exe --fields } elseif ($NativeJourney) { & $exe --journey-native } elseif ($JourneyOnly) { & $exe --journey } elseif ($DensityOnly) { & $exe --density } elseif ($DialogsOnly) { & $exe --dialogs } else { & $exe }
if ($LASTEXITCODE -ne 0) { throw 'Product acceptance failed. See artifacts/product-tests/product-acceptance/failure.txt.' }
