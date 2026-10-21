[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Package, [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Label = 'current', [ValidateRange(10,600)][int]$Seconds = 30)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$output = Join-Path $project ('artifacts/resources/' + $Label)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$exe = Join-Path $output 'ResourceProbe.exe'
$arguments = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/optimize+','/warnaserror+',"/out:$exe")
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll')) { $arguments += '/reference:' + (Join-Path $references $name) }
$arguments += Join-Path $PSScriptRoot 'ResourceProbe.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Resource probe compilation failed.' }
Get-FileHash -LiteralPath $Package -Algorithm SHA256 | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'package-hash.json') -Encoding UTF8
& $exe $Package $output $Seconds
if ($LASTEXITCODE -ne 0) { throw 'Resource probe failed.' }