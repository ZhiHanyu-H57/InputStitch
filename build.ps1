[CmdletBinding()]
param(
    [switch]$SkipSourceArchive,
    [switch]$NoReferenceRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$releaseInfo = & (Join-Path $projectRoot 'scripts/Get-ReleaseInfo.ps1')
$releaseVersion = $releaseInfo.Version
$fileVersion = $releaseInfo.FileVersion
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
    $compilerArguments += $TargetFrameworkSource
    $compilerArguments += $sourcePath
    $compilerArguments += (Join-Path $projectRoot 'ReleaseInfo.cs')
    $compilerArguments += (Join-Path $projectRoot 'UpdateInstaller.cs')
    $compilerArguments += (Join-Path $projectRoot 'RuntimeDiagnostics.cs')
    $compilerArguments += (Join-Path $projectRoot 'RawKeyboardFallback.cs')
    $compilerArguments += (Join-Path $projectRoot 'AtomicXmlFileStore.cs')
    $compilerArguments += (Join-Path $projectRoot 'ConfigPackageSerializer.cs')
    $compilerArguments += (Join-Path $projectRoot 'UpdateSourcePolicy.cs')
    $compilerArguments += (Join-Path $projectRoot 'UpdateUiCoordinator.cs')
    $compilerArguments += (Join-Path $projectRoot 'ProfileCatalog.cs')
    $compilerArguments += (Join-Path $projectRoot 'TargetWindowPolicy.cs')
    $compilerArguments += (Join-Path $projectRoot 'ConfigStore.cs')
    $compilerArguments += (Join-Path $projectRoot 'StepHistory.cs')
    $compilerArguments += (Join-Path $projectRoot 'QuickCreate.cs')
    $compilerArguments += (Join-Path $projectRoot 'ConcurrentRuntime.cs')
    $compilerArguments += (Join-Path $projectRoot 'OutputOwnership.cs')
    $compilerArguments += (Join-Path $projectRoot 'VirtualGamepadBackend.cs')
    $compilerArguments += (Join-Path $projectRoot 'VirtualKeyboard.cs')
    $compilerArguments += (Join-Path $projectRoot 'IdleGamepad.cs')
    $compilerArguments += (Join-Path $projectRoot 'XInputNative.cs')
    $compilerArguments += (Join-Path $projectRoot 'XInputInput.cs')
    $compilerArguments += (Join-Path $projectRoot 'GamepadRouter.cs')
    $compilerArguments += (Join-Path $projectRoot 'ControlledReplacement.cs')
    $compilerArguments += (Join-Path $projectRoot 'SlotAcquisition.cs')

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
        'third-party',
        '.gitignore',
        'AGENTS.md',
        'app.manifest',
        'build.bat',
        'build.ps1',
        'CHANGELOG.md',
        'CONTRIBUTING.md',
        'HANDOFF.md',
        'InputStitch.cs',
        'ReleaseInfo.cs',
        'UpdateInstaller.cs',
        'RuntimeDiagnostics.cs',
        'RawKeyboardFallback.cs',
        'AtomicXmlFileStore.cs',
        'ConfigPackageSerializer.cs',
        'UpdateSourcePolicy.cs',
        'UpdateUiCoordinator.cs',
        'ProfileCatalog.cs',
        'TargetWindowPolicy.cs',
        'ConfigStore.cs',
        'StepHistory.cs',
        'QuickCreate.cs',
        'ConcurrentRuntime.cs',
        'OutputOwnership.cs',
        'VirtualGamepadBackend.cs',
        'VirtualKeyboard.cs',
        'IdleGamepad.cs',
        'XInputNative.cs',
        'XInputInput.cs',
        'GamepadRouter.cs',
        'ControlledReplacement.cs',
        'SlotAcquisition.cs',
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
