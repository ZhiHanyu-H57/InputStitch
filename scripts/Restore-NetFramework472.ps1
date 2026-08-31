[CmdletBinding()]
param(
    [string]$CacheRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'InputStitch\BuildCache')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$packageVersion = '1.0.3'
$packageName = 'microsoft.netframework.referenceassemblies.net472'
$packageUri = "https://api.nuget.org/v3-flatcontainer/$packageName/$packageVersion/$packageName.$packageVersion.nupkg"
$expectedHash = 'FFA0A5570A39F911399164D0581FFDDEF99B5E3DFBAA5F220E5CE22969BCF57C'
$packageRoot = Join-Path $CacheRoot "$packageName\$packageVersion"
$referenceDirectory = Join-Path $packageRoot 'build\.NETFramework\v4.7.2'
$referenceMarker = Join-Path $referenceDirectory 'mscorlib.dll'

if (Test-Path -LiteralPath $referenceMarker -PathType Leaf) {
    return $referenceDirectory
}

New-Item -ItemType Directory -Path $CacheRoot -Force | Out-Null
$downloadPath = Join-Path ([IO.Path]::GetTempPath()) ("InputStitch.net472.$PID.nupkg")
$stagingPath = Join-Path $CacheRoot ("restore-$PID-" + [Guid]::NewGuid().ToString('N'))

try {
    Write-Host 'Restoring .NET Framework 4.7.2 reference assemblies from NuGet...'
    Invoke-WebRequest -UseBasicParsing -Uri $packageUri -OutFile $downloadPath
    $actualHash = (Get-FileHash -LiteralPath $downloadPath -Algorithm SHA256).Hash
    if ($actualHash -ne $expectedHash) {
        throw "Reference assembly package checksum mismatch. Expected $expectedHash, received $actualHash."
    }

    New-Item -ItemType Directory -Path $stagingPath | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($downloadPath, $stagingPath)

    $stagedMarker = Join-Path $stagingPath 'build\.NETFramework\v4.7.2\mscorlib.dll'
    if (-not (Test-Path -LiteralPath $stagedMarker -PathType Leaf)) {
        throw 'The restored package does not contain the expected .NET Framework 4.7.2 references.'
    }

    $packageParent = Split-Path $packageRoot -Parent
    New-Item -ItemType Directory -Path $packageParent -Force | Out-Null
    if (Test-Path -LiteralPath $packageRoot) {
        $resolvedRoot = [IO.Path]::GetFullPath($packageRoot)
        $resolvedCache = [IO.Path]::GetFullPath($CacheRoot).TrimEnd('\') + '\'
        if (-not $resolvedRoot.StartsWith($resolvedCache, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to replace a cache path outside the build cache: $resolvedRoot"
        }
        Remove-Item -LiteralPath $packageRoot -Recurse -Force
    }
    Move-Item -LiteralPath $stagingPath -Destination $packageRoot
} finally {
    if (Test-Path -LiteralPath $downloadPath) { Remove-Item -LiteralPath $downloadPath -Force }
    if (Test-Path -LiteralPath $stagingPath) {
        $resolvedStaging = [IO.Path]::GetFullPath($stagingPath)
        $resolvedCache = [IO.Path]::GetFullPath($CacheRoot).TrimEnd('\') + '\'
        if ($resolvedStaging.StartsWith($resolvedCache, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force
        }
    }
}

if (-not (Test-Path -LiteralPath $referenceMarker -PathType Leaf)) {
    throw 'The .NET Framework 4.7.2 reference assemblies could not be restored.'
}
return $referenceDirectory
