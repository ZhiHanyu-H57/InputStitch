[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $toolRoot -Parent) -Parent
$buildScript = Join-Path $toolRoot 'build.ps1'

$exe = & $buildScript | Select-Object -Last 1
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Input Lab build did not produce the expected executable: $exe"
}

& $exe --self-test-window-messages
if ($LASTEXITCODE -ne 0) {
    throw "Input Lab target-window message self-test failed with exit code $LASTEXITCODE."
}

& $exe --self-test-keyboard-visual
if ($LASTEXITCODE -ne 0) {
    throw "Input Lab keyboard visual self-test failed with exit code $LASTEXITCODE."
}

$version = (Get-Item -LiteralPath $exe).VersionInfo.ProductVersion
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Input Lab ProductVersion is missing.'
}

$releaseDir = Join-Path $projectRoot 'artifacts\InputLab\release'
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Get-ChildItem -LiteralPath $releaseDir -Force -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse

$exeName = "InputLab-$version-Windows-x64.exe"
$releaseExe = Join-Path $releaseDir $exeName
Copy-Item -LiteralPath $exe -Destination $releaseExe -Force
Copy-Item -LiteralPath (Join-Path $toolRoot 'README.md') -Destination (Join-Path $releaseDir 'README.md') -Force

$zipName = "InputLab-$version-Windows-x64.zip"
$zipPath = Join-Path $releaseDir $zipName
Compress-Archive -Path $releaseExe,(Join-Path $releaseDir 'README.md') -DestinationPath $zipPath -CompressionLevel Optimal -Force

$hashes = @()
foreach ($path in @($releaseExe, $zipPath)) {
    $hash = Get-FileHash -LiteralPath $path -Algorithm SHA256
    $hashes += ($hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($path))
}
$hashPath = Join-Path $releaseDir 'SHA256SUMS.txt'
[IO.File]::WriteAllLines($hashPath, $hashes, (New-Object Text.UTF8Encoding($false)))

Write-Output $releaseExe
Write-Output $zipPath
Write-Output $hashPath
