[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Package, [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Label = 'current')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$output = Join-Path $project ('artifacts/runtime-progress/' + $Label)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$exe = Join-Path $output 'RuntimeProgressProbe.exe'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/optimize+','/warnaserror+',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += Join-Path $PSScriptRoot 'RuntimeProgressProbe.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Runtime progress probe compilation failed.' }
Get-FileHash -LiteralPath $Package -Algorithm SHA256 | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'package-hash.json') -Encoding UTF8
& $exe $Package $output
if ($LASTEXITCODE -ne 0) { throw 'Runtime progress probe failed.' }
