[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$info = & (Join-Path $PSScriptRoot 'Get-ReleaseInfo.ps1')
if(-not $info.IsPrerelease) { throw 'This publisher accepts Beta builds only. Stable publishing requires a separate explicit review.' }
if($env:GITHUB_REPOSITORY -ne 'ZhiHanyu-H57/InputStitch') { throw 'Unexpected repository; refusing to publish.' }
if($env:GITHUB_SHA -notmatch '^[0-9a-f]{40}$') { throw 'Missing verified workflow commit.' }
if($env:GITHUB_REF -like 'refs/tags/*' -and $env:GITHUB_REF -ne ('refs/tags/' + $info.Tag)) { throw 'Tag does not match release metadata.' }
& (Join-Path $PSScriptRoot 'Verify-Release.ps1') -DistDirectory (Join-Path $project 'dist') -ExpectedVersion $info.Version -ExpectedFileVersion $info.FileVersion

function Read-Api([string]$endpoint) {
  $json = & gh api $endpoint
  if($LASTEXITCODE -ne 0) { throw "GitHub read failed: $endpoint" }
  return ($json | ConvertFrom-Json)
}
function Stable-Fingerprint($release) {
  $assets = @($release.assets | Sort-Object name | ForEach-Object { '{0}|{1}|{2}|{3}' -f $_.id,$_.name,$_.size,$_.digest })
  return (@($release.id,$release.tag_name,$release.target_commitish,$release.prerelease,$release.draft) + $assets) -join "`n"
}
$repository = $env:GITHUB_REPOSITORY
$stableBefore = Read-Api "repos/$repository/releases/latest"
$fingerprint = Stable-Fingerprint $stableBefore
# Authenticated reads must succeed before any write. A failed lookup is never
# evidence that a release is absent. Existing release assets are never replaced.
$existingJson = & gh api --paginate "repos/$repository/releases?per_page=100" --jq '.[] | .tag_name'
if($LASTEXITCODE -ne 0) { throw 'Could not enumerate existing releases.' }
if(@($existingJson) -contains $info.Tag) { throw "Release $($info.Tag) already exists; refusing to overwrite it." }
$tagRef = 'refs/tags/' + $info.Tag
$remoteTag = & git ls-remote --tags origin $tagRef
if($LASTEXITCODE -ne 0) { throw 'Could not inspect the remote tag.' }
if($remoteTag) {
  $tagCommit = ($remoteTag -split '\s+')[0]
  if($tagCommit -ne $env:GITHUB_SHA) { throw 'Existing tag does not point to this workflow commit.' }
} else {
  & git tag $info.Tag $env:GITHUB_SHA
  if($LASTEXITCODE -ne 0) { throw 'Could not create the Beta tag.' }
  & git push origin $tagRef
  if($LASTEXITCODE -ne 0) { throw 'Could not push the Beta tag.' }
}
$assets = @(
  (Join-Path $project "dist/InputStitch-$($info.Version)-Windows-x64.exe"),
  (Join-Path $project "dist/InputStitch-$($info.Version)-Windows-x86.exe"),
  (Join-Path $project "dist/InputStitch-$($info.Version)-Source.zip"),
  (Join-Path $project 'dist/InputStitch-beta.xml'),
  (Join-Path $project 'dist/SHA256SUMS.txt')
)
& gh release create $info.Tag @assets --verify-tag --prerelease --latest=false --title "InputStitch $($info.Version) (Beta)" --notes-file (Join-Path $project 'RELEASE_NOTES.md')
if($LASTEXITCODE -ne 0) { throw 'Beta publication failed. Inspect the tag/release before retrying; do not overwrite anything.' }
$published = Read-Api "repos/$repository/releases/tags/$($info.Tag)"
if(-not $published.prerelease -or $published.draft) { throw 'Published release is not a public prerelease.' }
$stableAfter = Read-Api "repos/$repository/releases/latest"
if((Stable-Fingerprint $stableAfter) -cne $fingerprint) { throw 'Stable release changed during publication; investigate without overwriting it.' }
Write-Output "Published $($info.Tag) as an opt-in Beta. Stable $($stableAfter.tag_name) and its asset fingerprints are unchanged."
