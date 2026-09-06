param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$info = & (Join-Path $PSScriptRoot 'Get-ReleaseInfo.ps1')
[xml]$manifest = [IO.File]::ReadAllText((Join-Path (Split-Path $PSScriptRoot -Parent) 'app.manifest'))
$manifest.assembly.assemblyIdentity.version = $info.FileVersion
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutput)) | Out-Null
$manifest.Save($resolvedOutput)
