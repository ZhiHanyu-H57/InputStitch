param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'ReleaseInfo.cs'))
$version = [regex]::Match($source, 'const string Version = "([^"]+)"').Groups[1].Value
$fileVersion = [regex]::Match($source, 'const string FileVersion = "([^"]+)"').Groups[1].Value
$channel = [regex]::Match($source, 'const bool IsPrerelease = (true|false)').Groups[1].Value
if ($version -notmatch '^\d+\.\d+\.\d+(-beta\.[1-9]\d*)?$' -or $fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$' -or -not $channel) {
    throw 'Invalid release metadata in ReleaseInfo.cs.'
}
$beta = $channel -eq 'true'
if ($beta -ne $version.Contains('-beta.')) { throw 'Release version and channel disagree.' }
if (-not $fileVersion.StartsWith(($version -split '-')[0] + '.')) { throw 'File and product versions disagree.' }
[pscustomobject]@{ Version=$version; FileVersion=$fileVersion; IsPrerelease=$beta; Tag="v$version"; ManifestName=$(if($beta){'InputStitch-beta.xml'}else{'InputStitch-update.xml'}) }
