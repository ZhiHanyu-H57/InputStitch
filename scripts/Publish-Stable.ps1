[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$info = & (Join-Path $PSScriptRoot 'Get-ReleaseInfo.ps1')
if($info.IsPrerelease) { throw 'Stable publisher refuses prerelease metadata.' }
if($env:GITHUB_REPOSITORY -ne 'ZhiHanyu-H57/InputStitch') { throw 'Unexpected repository; refusing to publish.' }
if($env:GITHUB_SHA -notmatch '^[0-9a-f]{40}$') { throw 'Missing verified workflow commit.' }
if($env:GITHUB_REF -like 'refs/tags/*' -and $env:GITHUB_REF -ne ('refs/tags/' + $info.Tag)) { throw 'Tag does not match release metadata.' }

& (Join-Path $PSScriptRoot 'Verify-Release.ps1') -DistDirectory (Join-Path $project 'dist') -ExpectedVersion $info.Version -ExpectedFileVersion $info.FileVersion

function Read-Api([string]$endpoint) {
  $json = & gh api $endpoint
  if($LASTEXITCODE -ne 0) { throw "GitHub read failed: $endpoint" }
  return ($json | ConvertFrom-Json)
}

$repository = $env:GITHUB_REPOSITORY
$existingTags = & gh api --paginate "repos/$repository/releases?per_page=100" --jq '.[] | .tag_name'
if($LASTEXITCODE -ne 0) { throw 'Could not enumerate existing releases.' }
if(@($existingTags) -contains $info.Tag) { throw "Release $($info.Tag) already exists; refusing to overwrite it." }

$tagRef = 'refs/tags/' + $info.Tag
$remoteTag = & git ls-remote --tags origin $tagRef
if($LASTEXITCODE -ne 0) { throw 'Could not inspect the remote tag.' }
if($remoteTag) {
  $tagCommit = ($remoteTag -split '\s+')[0]
  if($tagCommit -ne $env:GITHUB_SHA) { throw 'Existing tag does not point to this workflow commit.' }
} else {
  & git tag $info.Tag $env:GITHUB_SHA
  if($LASTEXITCODE -ne 0) { throw 'Could not create the Stable tag.' }
  & git push origin $tagRef
  if($LASTEXITCODE -ne 0) { throw 'Could not push the Stable tag.' }
}

$assets = @(
  (Join-Path $project "dist/InputStitch-$($info.Version)-Windows-x64.exe"),
  (Join-Path $project "dist/InputStitch-$($info.Version)-Windows-x86.exe"),
  (Join-Path $project "dist/InputStitch-$($info.Version)-Source.zip"),
  (Join-Path $project 'dist/InputStitch-update.xml'),
  (Join-Path $project 'dist/SHA256SUMS.txt')
)

& gh release create $info.Tag @assets --verify-tag --latest=true --title "InputStitch $($info.Version)" --notes-file (Join-Path $project 'RELEASE_NOTES.md')
if($LASTEXITCODE -ne 0) { throw 'Stable publication failed. Inspect the tag/release before retrying; do not overwrite anything.' }

$published = Read-Api "repos/$repository/releases/tags/$($info.Tag)"
if($published.prerelease -or $published.draft) { throw 'Published Stable release is not a public non-prerelease.' }
$latest = Read-Api "repos/$repository/releases/latest"
if($latest.tag_name -ne $info.Tag) { throw "Latest Stable release is $($latest.tag_name), expected $($info.Tag)." }

$expectedAssets = @(
  "InputStitch-$($info.Version)-Windows-x64.exe",
  "InputStitch-$($info.Version)-Windows-x86.exe",
  "InputStitch-$($info.Version)-Source.zip",
  'InputStitch-update.xml',
  'SHA256SUMS.txt'
)
$publishedAssets = @($published.assets | ForEach-Object { $_.name })
foreach($name in $expectedAssets) {
  if($publishedAssets -notcontains $name) { throw "Published release is missing asset: $name" }
}

Write-Output "Published $($info.Tag) as Stable and verified it is releases/latest with all expected assets."
