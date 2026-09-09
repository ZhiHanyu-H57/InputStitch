[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$projectRoot = Split-Path -Parent (Split-Path -Parent $toolRoot)
$restore = Join-Path $projectRoot 'scripts\Restore-NetFramework472.ps1'
$references = & $restore
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'C# compiler not found.' }

$client = Join-Path $projectRoot 'third-party\Nefarius.ViGEm.Client\Nefarius.ViGEm.Client.dll'
$outDir = Join-Path $projectRoot 'artifacts\InputLab\slot-order'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
Copy-Item -LiteralPath $client -Destination (Join-Path $outDir 'Nefarius.ViGEm.Client.dll') -Force

$exe = Join-Path $outDir 'SlotOrderProbe.exe'
$args = @('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64',('/out:' + $exe))
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','Facades/netstandard.dll')) {
    $args += '/reference:' + (Join-Path $references $name)
}
$args += '/reference:' + (Join-Path $outDir 'Nefarius.ViGEm.Client.dll')
$args += Join-Path $toolRoot 'SlotOrderProbe.cs'

& $compiler @args
if ($LASTEXITCODE -ne 0) { throw "Slot-order probe compilation failed with exit code $LASTEXITCODE." }

Write-Host 'This developer probe creates four neutral temporary ViGEm Xbox controllers.'
Write-Host 'It does not submit button, stick or trigger input and does not disable physical devices.'
& $exe
if ($LASTEXITCODE -ne 0) { throw "Slot-order probe failed with exit code $LASTEXITCODE." }
