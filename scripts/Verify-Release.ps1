[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$DistDirectory,
    [string]$ExpectedVersion = '1.0.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
    if ($versionInfo.FileVersion -ne "$ExpectedVersion.0") {
        throw "Unexpected file version for $($entry.Key): $($versionInfo.FileVersion)"
    }
    if ($versionInfo.ProductVersion -ne $ExpectedVersion) {
        throw "Unexpected product version for $($entry.Key): $($versionInfo.ProductVersion)"
    }
}

$checksumPath = Join-Path $DistDirectory 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { throw 'SHA256SUMS.txt is missing.' }
$checksumLines = [IO.File]::ReadAllLines($checksumPath)
$requiredChecksumFiles = @($expectedFiles.Keys) + "InputStitch-$ExpectedVersion-Source.zip"
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
