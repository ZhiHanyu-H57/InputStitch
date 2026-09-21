[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Returns the absolute path of every production source file listed in scripts/production-sources.txt.
# Build and test entry points share this list so that adding or renaming a source file only requires one
# edit; tests/SourceManifestTests.cs additionally checks the InputStitch.csproj <Compile> list.

$manifestPath = Join-Path $PSScriptRoot 'production-sources.txt'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Production source manifest was not found: $manifestPath"
}

$files = New-Object 'System.Collections.Generic.List[string]'
$seen = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList ([StringComparer]::OrdinalIgnoreCase)

foreach ($rawLine in [IO.File]::ReadAllLines($manifestPath)) {
    $line = $rawLine.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith('#')) { continue }
    if ($line.IndexOfAny([char[]]@('\', '/')) -ge 0) {
        throw "Production source entries must be repo-root file names without directories: $line"
    }
    if (-not $line.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Production source entries must be .cs files: $line"
    }
    if (-not $seen.Add($line)) {
        throw "Duplicate production source entry: $line"
    }
    $fullPath = Join-Path $ProjectRoot $line
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Production source listed in the manifest does not exist: $line"
    }
    $files.Add($fullPath)
}

if ($files.Count -eq 0) { throw 'Production source manifest is empty.' }
return $files.ToArray()
