[CmdletBinding()]
param(
    [switch]$SkipSourceArchive,
    [switch]$NoReferenceRestore,
    [switch]$SkipSdl3
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$releaseInfo = & (Join-Path $projectRoot 'scripts/Get-ReleaseInfo.ps1')
$releaseVersion = $releaseInfo.Version
$fileVersion = $releaseInfo.FileVersion
# Single authoritative production source list (scripts/production-sources.txt). The csproj list is kept
# explicit for Visual Studio users and is cross-checked by tests/SourceManifestTests.cs.
$productionSources = & (Join-Path $projectRoot 'scripts/Get-ProductionSources.ps1')
# Optional SDL3 runtime. Decision (b): the binary is never committed; it is fetched on demand by
# scripts/Restore-Sdl3.ps1 and embedded into each executable as a resource so the downloadable artifact stays
# a single self-contained EXE. Pass -SkipSdl3 to build without the lane (it then stays inert at runtime).
$sdl3Hashes = @{
    'x64' = '1f98969319302a100931f4385e5918a0bd53ab07773040682d22e7edb54858c0'
    'x86' = '47d508b4232cf9462096bdb6f613a330d9a94a7eb09c305fad073430c6531920'
}
if (-not $SkipSdl3) {
    $sdl3Root = Join-Path $projectRoot 'third-party\SDL3'
    $missing = $false
    foreach ($sdl3Architecture in @('x64', 'x86')) {
        if (-not (Test-Path -LiteralPath (Join-Path $sdl3Root ($sdl3Architecture + '\SDL3.dll')) -PathType Leaf)) { $missing = $true }
    }
    if ($missing) {
        Write-Host 'SDL3 runtime not found locally; restoring the pinned 3.4.16 assets ...'
        & (Join-Path $projectRoot 'scripts\Restore-Sdl3.ps1') -ProjectRoot $projectRoot | Out-Null
    }
    foreach ($sdl3Architecture in @('x64', 'x86')) {
        $sdl3Path = Join-Path $sdl3Root ($sdl3Architecture + '\SDL3.dll')
        if (-not (Test-Path -LiteralPath $sdl3Path -PathType Leaf)) { throw "SDL3 runtime for $sdl3Architecture is missing; run scripts/Restore-Sdl3.ps1 or pass -SkipSdl3." }
        $sdl3Hash = (Get-FileHash -LiteralPath $sdl3Path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($sdl3Hash -ne $sdl3Hashes[$sdl3Architecture]) { throw "SDL3 runtime hash mismatch for ${sdl3Architecture}: expected $($sdl3Hashes[$sdl3Architecture]) but found $sdl3Hash." }
    }
}
$distDirectory = Join-Path $projectRoot 'dist'
$sourcePath = Join-Path $projectRoot 'InputStitch.cs'
$manifestPath = Join-Path $projectRoot 'app.manifest'
$iconPath = Join-Path $projectRoot 'InputStitch.ico'
$restoreScript = Join-Path $projectRoot 'scripts\Restore-NetFramework472.ps1'
$verifyScript = Join-Path $projectRoot 'scripts\Verify-Release.ps1'
$viGEmClientPath = Join-Path $projectRoot 'third-party\Nefarius.ViGEm.Client\Nefarius.ViGEm.Client.dll'
$viGEmClientSha256 = '4458301000b732d115521e99f9936f4edb70d6ceb3036ef158715e0e6b8902e0'

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    if (-not (Test-Path -LiteralPath $LiteralPath -PathType Leaf)) {
        throw "Required file was not found: $LiteralPath"
    }
}

function Get-CSharpCompiler {
    $windowsDirectory = [Environment]::GetFolderPath('Windows')
    $candidates = @(
        (Join-Path $windowsDirectory 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $windowsDirectory 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    )

    $compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $compiler) {
        throw '.NET Framework C# compiler was not found. Install .NET Framework 4.7.2 Developer Pack.'
    }
    return $compiler
}

function Get-ReferenceDirectory {
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    $installedPath = Join-Path $programFilesX86 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
    if (Test-Path -LiteralPath (Join-Path $installedPath 'mscorlib.dll') -PathType Leaf) {
        return $installedPath
    }

    if ($NoReferenceRestore) {
        throw '.NET Framework 4.7.2 reference assemblies were not found and restore was disabled.'
    }

    Assert-FileExists -LiteralPath $restoreScript
    $restoredPath = & $restoreScript
    if (-not $restoredPath -or -not (Test-Path -LiteralPath (Join-Path $restoredPath 'mscorlib.dll') -PathType Leaf)) {
        throw '.NET Framework 4.7.2 reference assembly restore did not produce a valid directory.'
    }
    return $restoredPath
}

function Assert-SourceVersion {
    $sourceText = [IO.File]::ReadAllText($sourcePath)
    $requiredPatterns = @(
        'AssemblyVersion\(InputStitch.ReleaseInfo.FileVersion\)',
        'AssemblyFileVersion\(InputStitch.ReleaseInfo.FileVersion\)',
        'AssemblyInformationalVersion\(InputStitch.ReleaseInfo.Version\)',
        'const\s+string\s+Version\s*=\s*ReleaseInfo.Version'
    )

    foreach ($pattern in $requiredPatterns) {
        if ($sourceText -notmatch $pattern) {
            throw "InputStitch.cs does not contain the required release metadata pattern: $pattern"
        }
    }
}

function Invoke-ArchitectureBuild {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('x64', 'x86')][string]$Architecture,
        [Parameter(Mandatory = $true)][string]$Compiler,
        [Parameter(Mandatory = $true)][string]$ReferenceDirectory,
        [Parameter(Mandatory = $true)][string]$TargetFrameworkSource
    )

    $outputName = "InputStitch-$releaseVersion-Windows-$Architecture.exe"
    $outputPath = Join-Path $distDirectory $outputName
    $references = @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xml.dll')
    $compilerArguments = @(
        '/nologo',
        '/target:winexe',
        "/platform:$Architecture",
        '/optimize+',
        '/debug-',
        '/checked-',
        '/warn:4',
        '/noconfig',
        '/nostdlib+',
        "/out:$outputPath",
        "/win32manifest:$manifestPath",
        "/win32icon:$iconPath"
    )

    foreach ($reference in $references) {
        $referencePath = Join-Path $ReferenceDirectory $reference
        Assert-FileExists -LiteralPath $referencePath
        $compilerArguments += "/reference:$referencePath"
    }
    $netstandardPath = Join-Path $ReferenceDirectory 'Facades\netstandard.dll'
    Assert-FileExists -LiteralPath $netstandardPath
    Assert-FileExists -LiteralPath $viGEmClientPath
    $compilerArguments += "/reference:$netstandardPath"
    $compilerArguments += "/reference:$viGEmClientPath"
    $compilerArguments += "/resource:$viGEmClientPath,InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll"
    if (-not $SkipSdl3) {
        $sdl3ResourcePath = Join-Path $projectRoot ('third-party\SDL3\' + $Architecture + '\SDL3.dll')
        Assert-FileExists -LiteralPath $sdl3ResourcePath
        $compilerArguments += "/resource:$sdl3ResourcePath,InputStitch.Sdl3.$Architecture.dll"
    }
    $compilerArguments += $TargetFrameworkSource
    foreach ($productionSource in $productionSources) { $compilerArguments += $productionSource }

    Write-Host "Building $outputName ..."
    & $Compiler @compilerArguments 2>&1 | ForEach-Object { Write-Host $_ }
    $compilerExitCode = $LASTEXITCODE
    if ($compilerExitCode -ne 0) {
        throw "Compilation failed for $Architecture with exit code $compilerExitCode."
    }
    Assert-FileExists -LiteralPath $outputPath
    return $outputPath
}

function New-SourceArchive {
    $archivePath = Join-Path $distDirectory "InputStitch-$releaseVersion-Source.zip"
    $includedFiles = @(
        '.github',
        'docs',
        'scripts',
        'tests',
        'tools',
        # The optional SDL3 runtime is downloaded locally by scripts/Restore-Sdl3.ps1 and is intentionally
        # excluded from the source archive so release artifacts stay deterministic and small.
        'third-party\Nefarius.ViGEm.Client',
        '.gitignore',
        'AGENTS.md',
        'app.manifest',
        'build.bat',
        'build.ps1',
        'CHANGELOG.md',
        'CONTRIBUTING.md',
        'HANDOFF.md',
        'InputStitch.csproj',
        'InputStitch.ico',
        'README.md',
        'README.zh-CN.md',
        'RELEASE_NOTES.md',
        'PLAN.md',
        'ROADMAP.md',
        'SECURITY.md',
        'THIRD_PARTY_NOTICES.md'
    )

    foreach ($productionSource in $productionSources) { $includedFiles += (Split-Path $productionSource -Leaf) }

    $existingItems = foreach ($relativePath in $includedFiles) {
        $candidate = Join-Path $projectRoot $relativePath
        if (Test-Path -LiteralPath $candidate) { Get-Item -LiteralPath $candidate }
    }
    if (-not $existingItems) { throw 'No source files were found for the source archive.' }

    if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
    Compress-Archive -LiteralPath $existingItems.FullName -DestinationPath $archivePath -CompressionLevel Optimal
    return $archivePath
}

function New-UpdateManifest {
    param(
        [Parameter(Mandatory = $true)][string]$X64Path,
        [Parameter(Mandatory = $true)][string]$X86Path
    )
    $manifestOutputPath = Join-Path $distDirectory $releaseInfo.ManifestName
    # Keep the GitHub-published Stable manifest on GitHub asset URLs for legacy updaters
    # (including v1.4.1) that only trust the historical GitHub release path. The R2 mirror
    # rewrites its /latest manifest copy to version-pinned download.zhihanyu.com URLs.
    $downloadBase = if ($releaseInfo.IsPrerelease) { "https://github.com/ZhiHanyu-H57/InputStitch/releases/download/v$releaseVersion" } else { 'https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download' }
    $x64Name = Split-Path $X64Path -Leaf
    $x86Name = Split-Path $X86Path -Leaf
    $x64Hash = (Get-FileHash -LiteralPath $X64Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $x86Hash = (Get-FileHash -LiteralPath $X86Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $xml = @"
<?xml version="1.0" encoding="utf-8"?>
<InputStitchUpdate>
  <Version>$releaseVersion</Version>
  <ReleaseUrl>https://github.com/ZhiHanyu-H57/InputStitch/releases/tag/v$releaseVersion</ReleaseUrl>
  <Asset Architecture="x64" FileName="$x64Name" Url="$downloadBase/$x64Name" Sha256="$x64Hash" />
  <Asset Architecture="x86" FileName="$x86Name" Url="$downloadBase/$x86Name" Sha256="$x86Hash" />
</InputStitchUpdate>
"@
    [IO.File]::WriteAllText($manifestOutputPath, $xml.TrimStart(), (New-Object Text.UTF8Encoding($false)))
    return $manifestOutputPath
}

Assert-FileExists -LiteralPath $sourcePath
Assert-FileExists -LiteralPath $manifestPath
Assert-FileExists -LiteralPath $iconPath
Assert-FileExists -LiteralPath $viGEmClientPath
$actualViGEmClientSha256 = (Get-FileHash -LiteralPath $viGEmClientPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualViGEmClientSha256 -ne $viGEmClientSha256) {
    throw "Nefarius.ViGEm.Client.dll hash mismatch. Expected $viGEmClientSha256 but found $actualViGEmClientSha256."
}
Assert-SourceVersion

if (Test-Path -LiteralPath $distDirectory) {
    Get-ChildItem -LiteralPath $distDirectory -File | Remove-Item -Force
} else {
    New-Item -ItemType Directory -Path $distDirectory | Out-Null
}

$compilerPath = Get-CSharpCompiler
$referencePath = Get-ReferenceDirectory
$targetFrameworkSource = Join-Path ([IO.Path]::GetTempPath()) ("InputStitch.TargetFramework.$PID.g.cs")
$generatedManifest = Join-Path ([IO.Path]::GetTempPath()) ("InputStitch.Manifest.$PID.xml")

try {
    & (Join-Path $projectRoot 'scripts/Write-AppManifest.ps1') -OutputPath $generatedManifest
    $manifestPath = $generatedManifest
    [IO.File]::WriteAllText(
        $targetFrameworkSource,
        "using System.Runtime.Versioning;`r`n[assembly: TargetFramework(`".NETFramework,Version=v4.7.2`", FrameworkDisplayName = `".NET Framework 4.7.2`")]`r`n",
        (New-Object Text.UTF8Encoding($false))
    )

    $releaseFiles = @(
        (Invoke-ArchitectureBuild -Architecture 'x64' -Compiler $compilerPath -ReferenceDirectory $referencePath -TargetFrameworkSource $targetFrameworkSource),
        (Invoke-ArchitectureBuild -Architecture 'x86' -Compiler $compilerPath -ReferenceDirectory $referencePath -TargetFrameworkSource $targetFrameworkSource)
    )

    $releaseFiles += New-UpdateManifest -X64Path $releaseFiles[0] -X86Path $releaseFiles[1]

    if (-not $SkipSourceArchive) {
        $releaseFiles += New-SourceArchive
    }

    $checksumPath = Join-Path $distDirectory 'SHA256SUMS.txt'
    $checksumLines = foreach ($releaseFile in ($releaseFiles | Sort-Object { Split-Path $_ -Leaf })) {
        $hash = (Get-FileHash -LiteralPath $releaseFile -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $(Split-Path $releaseFile -Leaf)"
    }
    [IO.File]::WriteAllLines($checksumPath, $checksumLines, (New-Object Text.UTF8Encoding($false)))

    Assert-FileExists -LiteralPath $verifyScript
    & $verifyScript -DistDirectory $distDirectory -ExpectedVersion $releaseVersion -ExpectedFileVersion $fileVersion -SkipSourceArchive:$SkipSourceArchive

    Write-Host ''
    Write-Host "Release build succeeded: $distDirectory" -ForegroundColor Green
    Get-ChildItem -LiteralPath $distDirectory -File | Sort-Object Name | ForEach-Object {
        Write-Host ("  {0} ({1:N0} bytes)" -f $_.Name, $_.Length)
    }
} finally {
    if (Test-Path -LiteralPath $generatedManifest) { Remove-Item -LiteralPath $generatedManifest -Force }
    if (Test-Path -LiteralPath $targetFrameworkSource) {
        Remove-Item -LiteralPath $targetFrameworkSource -Force
    }
}
