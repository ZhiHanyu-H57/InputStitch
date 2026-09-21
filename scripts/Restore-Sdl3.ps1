[CmdletBinding()]
param(
    [ValidateSet('x64', 'x86', 'Both')][string]$Architecture = 'Both',
    [string]$Version = '3.4.16',
    [string]$ProjectRoot = '',
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $scriptDirectory = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptDirectory) -and $MyInvocation.MyCommand.Path) {
        $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    }
    if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
        throw 'Could not determine the script directory. Pass -ProjectRoot explicitly.'
    }
    $ProjectRoot = Split-Path $scriptDirectory -Parent
}

# Optional SDL3 input lane (development plan H2.3). The native runtime is intentionally NOT committed to the
# repository; this script downloads the pinned official release asset, verifies the archive hash, extracts
# SDL3.dll plus its LICENSE.txt and verifies the extracted DLL hash. The provider looks for the result at
# third-party/SDL3/<arch>/SDL3.dll, so running this script is all a developer machine needs.
#
# The shipping decision for end users (bundle the DLL in the release, embed it into the EXE, or keep SDL3
# optional) is still open - see docs/INPUT_PROVIDER_EVALUATION.md.

if ($Version -ne '3.4.16') {
    throw "No pinned SHA-256 is recorded for SDL3 $Version. Update this script deliberately before using another version."
}

$assets = @{
    x64 = @{
        Zip = 'SDL3-3.4.16-win32-x64.zip'
        ZipSha256 = '4217944b4e51457af4a59c82d883f8443b3e65964b2acd8943484c492756c4b6'
        DllSha256 = '1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0'
    }
    x86 = @{
        Zip = 'SDL3-3.4.16-win32-x86.zip'
        ZipSha256 = 'beb4e86e4a101f66556162c3efff9fd1ad88f1761b63be35867a3336a72f8c41'
        DllSha256 = '47d508b4232cf9462096bdb6f613a330d9a94a7eb09c305fad073430c6531920'
    }
}

$targets = if ($Architecture -eq 'Both') { @('x64', 'x86') } else { @($Architecture) }
$restored = New-Object System.Collections.Generic.List[string]

foreach ($arch in $targets) {
    $info = $assets[$arch]
    $targetDirectory = Join-Path $ProjectRoot (Join-Path 'third-party\SDL3' $arch)
    $targetDll = Join-Path $targetDirectory 'SDL3.dll'

    if (-not $Force -and (Test-Path -LiteralPath $targetDll -PathType Leaf)) {
        $existingHash = (Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($existingHash -eq $info.DllSha256) {
            Write-Host "SDL3 $arch is already restored and verified."
            $restored.Add($targetDll)
            continue
        }
        Write-Host "SDL3 $arch is present but does not match the pinned hash; re-downloading."
    }

    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    $url = "https://github.com/libsdl-org/SDL/releases/download/release-$Version/$($info.Zip)"
    $temporaryZip = Join-Path ([IO.Path]::GetTempPath()) ("SDL3-$Version-$arch-" + [Guid]::NewGuid().ToString('N') + '.zip')
    try {
        Write-Host "Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $temporaryZip -UseBasicParsing
        $zipHash = (Get-FileHash -LiteralPath $temporaryZip -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($zipHash -ne $info.ZipSha256) {
            throw "SDL3 $arch archive hash mismatch: expected $($info.ZipSha256) but found $zipHash."
        }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead($temporaryZip)
        try {
            $dllEntry = $archive.Entries | Where-Object { $_.FullName -eq 'SDL3.dll' } | Select-Object -First 1
            if (-not $dllEntry) { throw "SDL3 $arch archive does not contain SDL3.dll." }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($dllEntry, $targetDll, $true)
            $licenseEntry = $archive.Entries | Where-Object { $_.FullName -eq 'LICENSE.txt' } | Select-Object -First 1
            if ($licenseEntry) {
                [IO.Compression.ZipFileExtensions]::ExtractToFile($licenseEntry, (Join-Path $targetDirectory 'LICENSE.txt'), $true)
            }
        }
        finally {
            $archive.Dispose()
        }

        $dllHash = (Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($dllHash -ne $info.DllSha256) {
            Remove-Item -LiteralPath $targetDll -Force
            throw "SDL3 $arch DLL hash mismatch: expected $($info.DllSha256) but found $dllHash."
        }
        Write-Host "SDL3 $arch restored and verified: $targetDll" -ForegroundColor Green
        $restored.Add($targetDll)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryZip) { Remove-Item -LiteralPath $temporaryZip -Force }
    }
}

return $restored.ToArray()
