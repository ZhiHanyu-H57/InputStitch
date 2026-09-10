[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$DistDirectory,
    [string]$ExpectedVersion,
    [string]$ExpectedFileVersion,
    [switch]$SkipSourceArchive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $ExpectedVersion) {
    $info = & (Join-Path $PSScriptRoot 'Get-ReleaseInfo.ps1')
    $ExpectedVersion = $info.Version
    $ExpectedFileVersion = $info.FileVersion
}
if (-not $ExpectedFileVersion) {
    if($ExpectedVersion -match '^(\d+\.\d+\.\d+)-beta\.(\d+)$') { $ExpectedFileVersion = "$($Matches[1]).$($Matches[2])" }
    else { $ExpectedFileVersion = "$ExpectedVersion.0" }
}
$beta = $ExpectedVersion.Contains('-beta.')
$manifestName = if($beta){'InputStitch-beta.xml'}else{'InputStitch-update.xml'}
if ($beta -and (Test-Path -LiteralPath (Join-Path $DistDirectory 'InputStitch-update.xml'))) { throw 'Beta must never ship a stable updater manifest.' }

function Get-PeMachine {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    $stream = [IO.File]::OpenRead($LiteralPath)
    $reader = New-Object IO.BinaryReader($stream)
    try {
        if ($reader.ReadUInt16() -ne 0x5A4D) { throw "Not a valid PE file: $LiteralPath" }
        $stream.Position = 0x3C
        $peOffset = $reader.ReadUInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw "Invalid PE signature: $LiteralPath" }
        return $reader.ReadUInt16()
    } finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

$expectedFiles = @{
    "InputStitch-$ExpectedVersion-Windows-x64.exe" = 0x8664
    "InputStitch-$ExpectedVersion-Windows-x86.exe" = 0x014C
}

foreach ($entry in $expectedFiles.GetEnumerator()) {
    $path = Join-Path $DistDirectory $entry.Key
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing release file: $($entry.Key)" }

    $machine = Get-PeMachine -LiteralPath $path
    if ($machine -ne $entry.Value) {
        throw ('Unexpected PE machine for {0}: expected 0x{1:X4}, received 0x{2:X4}.' -f $entry.Key, $entry.Value, $machine)
    }

    $versionInfo = (Get-Item -LiteralPath $path).VersionInfo
    if ($versionInfo.FileVersion -ne $ExpectedFileVersion) {
        throw "Unexpected file version for $($entry.Key): $($versionInfo.FileVersion)"
    }
    if ($versionInfo.ProductVersion -ne $ExpectedVersion) {
        throw "Unexpected product version for $($entry.Key): $($versionInfo.ProductVersion)"
    }
}

$checksumPath = Join-Path $DistDirectory 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { throw 'SHA256SUMS.txt is missing.' }
$checksumLines = [IO.File]::ReadAllLines($checksumPath)
$updateManifestPath = Join-Path $DistDirectory $manifestName
if (-not (Test-Path -LiteralPath $updateManifestPath -PathType Leaf)) { throw "$manifestName is missing." }
[xml]$updateManifest = [IO.File]::ReadAllText($updateManifestPath)
if ($updateManifest.InputStitchUpdate.Version -ne $ExpectedVersion) { throw 'The update manifest version is invalid.' }
if ([string]$updateManifest.InputStitchUpdate.ReleaseUrl -ne "https://github.com/ZhiHanyu-H57/InputStitch/releases/tag/v$ExpectedVersion") { throw 'The release page URL is invalid.' }
foreach ($architecture in @('x64', 'x86')) {
    $asset = @($updateManifest.InputStitchUpdate.Asset) | Where-Object { $_.Architecture -eq $architecture } | Select-Object -First 1
    if (-not $asset) { throw "The update manifest is missing the $architecture asset." }
    $assetPath = Join-Path $DistDirectory ([string]$asset.FileName)
    if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) { throw "The update manifest references a missing file: $($asset.FileName)" }
    $assetHash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ([string]$asset.Sha256 -ne $assetHash) { throw "The update manifest hash is invalid for $architecture." }
    $downloadBase = if($beta){"https://github.com/ZhiHanyu-H57/InputStitch/releases/download/v$ExpectedVersion"}else{"https://download.zhihanyu.com/releases/v$ExpectedVersion"}
    if ([string]$asset.Url -ne "$downloadBase/$($asset.FileName)") {
        throw "The update manifest URL is invalid for $architecture."
    }
}
$requiredChecksumFiles = @($expectedFiles.Keys) + $manifestName
if(-not $SkipSourceArchive) { $requiredChecksumFiles += "InputStitch-$ExpectedVersion-Source.zip" }
if ($checksumLines.Count -ne $requiredChecksumFiles.Count) {
    throw "SHA256SUMS.txt contains an unexpected number of entries: $($checksumLines.Count)"
}
foreach ($fileName in $requiredChecksumFiles) {
    $path = Join-Path $DistDirectory $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing release file: $fileName" }
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    $expectedLine = "$actualHash  $fileName"
    if ($checksumLines -notcontains $expectedLine) { throw "Checksum entry is missing or invalid for $fileName." }
}

Write-Host 'Release verification passed for Windows x64 and Windows x86.' -ForegroundColor Green
