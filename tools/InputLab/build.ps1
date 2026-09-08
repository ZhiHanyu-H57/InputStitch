[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $toolRoot -Parent) -Parent
$references = & (Join-Path $projectRoot 'scripts\Restore-NetFramework472.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = $compiler.Replace('Framework64', 'Framework')
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework C# compiler was not found.'
}

$outputDir = Join-Path $projectRoot 'artifacts\InputLab'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$output = Join-Path $outputDir 'InputStitch-InputLab.exe'

$arguments = @(
    '/nologo',
    '/noconfig',
    '/nostdlib+',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    '/warn:4',
    '/warnaserror+',
    "/out:$output"
)
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll')) {
    $arguments += '/reference:' + (Join-Path $references $name)
}
foreach ($name in @('Program.cs','NativeInput.cs','XInputReader.cs','ObservationSnapshot.cs','StickView.cs','InputLabForm.cs')) {
    $arguments += Join-Path $toolRoot $name
}

& $compiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Input Lab compilation failed with exit code $LASTEXITCODE."
}

Write-Output $output
