[CmdletBinding()]
param(
    [switch]$SkipSourceArchive,
    [switch]$NoReferenceRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$releaseVersion = '1.0.0'
$fileVersion = '1.0.0.0'
$distDirectory = Join-Path $projectRoot 'dist'
$sourcePath = Join-Path $projectRoot 'InputStitch.cs'
$manifestPath = Join-Path $projectRoot 'app.manifest'
$iconPath = Join-Path $projectRoot 'InputStitch.ico'
$restoreScript = Join-Path $projectRoot 'scripts\Restore-NetFramework472.ps1'
$verifyScript = Join-Path $projectRoot 'scripts\Verify-Release.ps1'

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
        ('AssemblyVersion\("' + [regex]::Escape($fileVersion) + '"\)'),
        ('AssemblyFileVersion\("' + [regex]::Escape($fileVersion) + '"\)'),
        ('AssemblyInformationalVersion\("' + [regex]::Escape($releaseVersion) + '"\)'),
        ('const\s+string\s+Version\s*=\s*"' + [regex]::Escape($releaseVersion) + '"')
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
    $compilerArguments += $TargetFrameworkSource
    $compilerArguments += $sourcePath

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
        '.gitignore',
        'app.manifest',
        'build.bat',
        'build.ps1',
        'CHANGELOG.md',
        'CONTRIBUTING.md',
        'InputStitch.cs',
        'InputStitch.csproj',
        'InputStitch.ico',
        'README.md',
        'README.zh-CN.md',
        'RELEASE_NOTES.md',
        'SECURITY.md'
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
    $manifestOutputPath = Join-Path $distDirectory 'InputStitch-update.xml'
    $x64Name = Split-Path $X64Path -Leaf
    $x86Name = Split-Path $X86Path -Leaf
    $x64Hash = (Get-FileHash -LiteralPath $X64Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $x86Hash = (Get-FileHash -LiteralPath $X86Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $xml = @"
<?xml version="1.0" encoding="utf-8"?>
<InputStitchUpdate>
  <Version>$releaseVersion</Version>
  <ReleaseUrl>https://github.com/ZhiHanyu-H57/InputStitch/releases/tag/v$releaseVersion</ReleaseUrl>
  <Asset Architecture="x64" FileName="$x64Name" Url="https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download/$x64Name" Sha256="$x64Hash" />
  <Asset Architecture="x86" FileName="$x86Name" Url="https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download/$x86Name" Sha256="$x86Hash" />
</InputStitchUpdate>
"@
    [IO.File]::WriteAllText($manifestOutputPath, $xml.TrimStart(), (New-Object Text.UTF8Encoding($false)))
    return $manifestOutputPath
}

Assert-FileExists -LiteralPath $sourcePath
Assert-FileExists -LiteralPath $manifestPath
Assert-FileExists -LiteralPath $iconPath
Assert-SourceVersion

if (Test-Path -LiteralPath $distDirectory) {
    Get-ChildItem -LiteralPath $distDirectory -File | Remove-Item -Force
} else {
    New-Item -ItemType Directory -Path $distDirectory | Out-Null
}

$compilerPath = Get-CSharpCompiler
$referencePath = Get-ReferenceDirectory
$targetFrameworkSource = Join-Path ([IO.Path]::GetTempPath()) ("InputStitch.TargetFramework.$PID.g.cs")

try {
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
    & $verifyScript -DistDirectory $distDirectory -ExpectedVersion $releaseVersion

    Write-Host ''
    Write-Host "Release build succeeded: $distDirectory" -ForegroundColor Green
    Get-ChildItem -LiteralPath $distDirectory -File | Sort-Object Name | ForEach-Object {
        Write-Host ("  {0} ({1:N0} bytes)" -f $_.Name, $_.Length)
    }
} finally {
    if (Test-Path -LiteralPath $targetFrameworkSource) {
        Remove-Item -LiteralPath $targetFrameworkSource -Force
    }
}

