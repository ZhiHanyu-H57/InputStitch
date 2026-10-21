[CmdletBinding()]
param([ValidateRange(2, 120)][int]$Seconds = 6, [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Label = 'current')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$sources = & (Join-Path $project 'scripts/Get-ProductionSources.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $project ('artifacts/performance/' + $Label)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$exe = Join-Path $output 'PerformanceProbe.exe'
$client = Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/optimize+','/warnaserror+','/main:PerformanceProbe',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += '/reference:' + $client
$arguments += $sources
$arguments += Join-Path $PSScriptRoot 'PerformanceProbe.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Performance probe compilation failed.' }
$manifest = foreach ($source in $sources) { Get-FileHash -LiteralPath $source -Algorithm SHA256 | Select-Object Path, Hash }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'source-hashes.json') -Encoding UTF8
& $exe $Seconds | Tee-Object -FilePath (Join-Path $output 'samples.csv')
if ($LASTEXITCODE -ne 0) { throw 'Performance probe failed.' }
