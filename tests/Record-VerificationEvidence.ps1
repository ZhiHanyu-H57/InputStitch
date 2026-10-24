[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Check,[Parameter(Mandatory=$true)][string]$SourceCommit,
    [Parameter(Mandatory=$true)][string]$EvidencePath,[Parameter(Mandatory=$true)][string]$Qualification)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $project 'scripts/Verification.psm1') -Force
$commit=(& git -C $project rev-parse ($SourceCommit+'^{commit}')).Trim()
if($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$'){throw 'A verified source commit is required.'}
$evidence=Get-Item -LiteralPath $EvidencePath
$text=Get-Content -LiteralPath $evidence.FullName -Raw
if($text -notmatch 'PASS|通过' -or $text -match '(?m)^FAIL\b'){throw 'Evidence does not record a passing acceptance result.'}
$fingerprint=Get-VerificationFingerprint -Check $Check -Ref $commit
$name=$Check.Replace(':','-')+'-'+$commit.Substring(0,12)
$relative='verification/evidence/'+$name+$evidence.Extension
New-Item -ItemType Directory -Force -Path (Join-Path $project 'verification/evidence'),(Join-Path $project 'verification/receipts')|Out-Null
Copy-Item -LiteralPath $evidence.FullName -Destination (Join-Path $project $relative)
$receipt=[ordered]@{schema=1;check=$Check;status='PASS';sourceCommit=$commit;fingerprint=$fingerprint;qualification=$Qualification;evidence=@(@{path=$relative;sha256=(Get-FileHash -LiteralPath (Join-Path $project $relative)).Hash})}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $project ('verification/receipts/'+$name+'.json')) -Encoding UTF8
Write-Output ('RECORDED '+$Check+' from '+$commit+'; original coverage retained, no new test execution claimed.')
