[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $project 'scripts/Verification.psm1') -Force
$script:checks=0
function Check([bool]$value,[string]$message){if(!$value){throw $message};$script:checks++}
foreach($file in @('scripts/Verification.psm1','tests/Run-Verification.ps1','tests/Run-Tests.ps1','tests/Record-VerificationEvidence.ps1')){
    $tokens=$null;$errors=$null;[Management.Automation.Language.Parser]::ParseFile((Join-Path $project $file),[ref]$tokens,[ref]$errors)|Out-Null
    Check ($errors.Count -eq 0) ('PowerShell parse: '+$file)
}
$p=Get-VerificationPlan -ChangedPaths @('docs/README.md')
Check ($p.Checks.Count -eq 0) 'Docs-only development must not run product tests.'
$p=Get-VerificationPlan -ChangedPaths @('EditorPresentation.cs')
Check ('ui:secondary' -in $p.Checks -and !$p.SoakRequired -and 'suite:MacroRunCoordinatorTests' -notin $p.Checks) 'Shared UI needs layouts without macro/soak expansion.'
$p=Get-VerificationPlan -ChangedPaths @('MacroWorkerPool.cs')
Check ($p.SoakRequired -and 'suite:OutputOwnershipTests' -in $p.Checks) 'Worker lifetime change must select runtime/ownership and soak.'
$p=Get-VerificationPlan -ChangedPaths @('ReleaseInfo.cs') -Mode Release
Check ('suite:ReleasePolicyTests' -in $p.Checks -and 'ui:settings-native' -in $p.Checks -and !$p.SoakRequired) 'Stable promotion checks metadata/settings, not a repeated soak.'
$p=Get-VerificationPlan -ChangedPaths @('NetworkControl.cs','TimerRtss.cs') -Mode Release
Check ('native:network' -in $p.Checks -and 'native:obs' -in $p.Checks) 'Cumulative feature changes retain dedicated evidence obligations.'
$p=Get-VerificationPlan -ChangedPaths @('CrosshairOverlay.cs')
Check ('native:crosshair' -in $p.Checks) 'Native overlay changes retain actual composition acceptance.'
$p=Get-VerificationPlan -ChangedPaths @('NewUnclassifiedEngine.cs')
Check ($p.UnclassifiedPaths.Count -eq 1) 'Unclassified source cannot silently pass or trigger a blind full run.'
$p=Get-VerificationPlan -ChangedPaths @('SettingsDialog.cs','SettingsDialog.cs')
Check (@($p.Checks|Sort-Object -Unique).Count -eq $p.Checks.Count) 'Overlapping changes are deduplicated.'
$p=Get-VerificationPlan -ChangedPaths @('tests/SecondaryLayoutTests.cs','EditorPresentation.cs','tests/ProductAcceptanceTests.cs')
Check ('suite:SecondaryLayoutTests' -notin $p.Checks -and 'suite:ProductAcceptanceTests' -notin $p.Checks -and @($p.Checks|Where-Object {$_ -eq 'ui:journey'}).Count -eq 1) 'Aliases cannot rerun the same matrix/journey through another entry point.'
$fixture=Join-Path $project ('artifacts/verification-workflow-tests/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'scripts'),(Join-Path $fixture 'tests')|Out-Null
& git -C $fixture init -q
@('Example.cs','ReleaseInfo.cs') | Set-Content (Join-Path $fixture 'scripts/production-sources.txt')
'class Example {}' | Set-Content (Join-Path $fixture 'Example.cs')
'class ReleaseInfo { const string Version="beta"; }' | Set-Content (Join-Path $fixture 'ReleaseInfo.cs')
'test body' | Set-Content (Join-Path $fixture 'tests/UiSafetyTests.cs')
'release policy body' | Set-Content (Join-Path $fixture 'tests/ReleasePolicyTests.cs')
& git -C $fixture add .
& git -C $fixture -c user.name=VerificationFixture -c user.email=fixture@example.invalid commit -qm fixture
if($LASTEXITCODE -ne 0){throw 'Fixture git commit failed.'}
$original=Get-VerificationFingerprint -Check 'suite:UiSafetyTests' -Root $fixture
Check ($original -eq (Get-VerificationFingerprint -Check 'suite:UiSafetyTests' -Ref HEAD -Root $fixture)) 'Git-object fingerprints match clean working-tree inputs.'
$metadata=Get-VerificationFingerprint -Check 'suite:ReleasePolicyTests' -Root $fixture
'class ReleaseInfo { const string Version="stable"; }' | Set-Content (Join-Path $fixture 'ReleaseInfo.cs')
Check ($original -eq (Get-VerificationFingerprint -Check 'suite:UiSafetyTests' -Root $fixture)) 'Metadata-only promotion reuses unchanged feature checks.'
Check ($metadata -ne (Get-VerificationFingerprint -Check 'suite:ReleasePolicyTests' -Root $fixture)) 'Metadata change invalidates release-policy evidence.'
'changed test assertion' | Set-Content (Join-Path $fixture 'tests/UiSafetyTests.cs')
Check ($original -ne (Get-VerificationFingerprint -Check 'suite:UiSafetyTests' -Root $fixture)) 'Changed assertions invalidate old results.'
'PASS fixture' | Set-Content (Join-Path $fixture 'evidence.log')
$receipt=[pscustomobject]@{schema=1;check='fixture';status='PASS';fingerprint='abc';sourceCommit=('a'*40);qualification='fixture';evidence=@([pscustomobject]@{path='evidence.log';sha256=(Get-FileHash (Join-Path $fixture 'evidence.log')).Hash})}
Check (Test-VerificationReceipt -Receipt $receipt -Check fixture -Fingerprint abc -Root $fixture) 'Matching inputs and intact evidence can be reused.'
Check (!(Test-VerificationReceipt -Receipt $receipt -Check fixture -Fingerprint changed -Root $fixture)) 'Different inputs reject reuse.'
'changed evidence' | Set-Content (Join-Path $fixture 'evidence.log')
Check (!(Test-VerificationReceipt -Receipt $receipt -Check fixture -Fingerprint abc -Root $fixture)) 'Changed evidence rejects reuse.'
Check (!(Test-VerificationReceipt -Receipt ([pscustomobject]@{}) -Check fixture -Fingerprint abc -Root $fixture)) 'Malformed receipt rejects reuse.'
$receipt.evidence[0].path='../outside.log'
Check (!(Test-VerificationReceipt -Receipt $receipt -Check fixture -Fingerprint abc -Root $fixture)) 'Evidence must stay inside its repository.'
foreach($name in @('release.yml','inputlab-release.yml')){
    $workflow=Get-Content (Join-Path $project ('.github/workflows/'+$name)) -Raw
    Check ($workflow -notmatch '(?m)^\s*(run: )?\./tests/Run-Tests\.ps1\s*$') ('No unconditional full regression in '+$name)
}
Write-Output ('PASS verification workflow: '+$script:checks+' checks; selection, cumulative obligations, deduplication, cache invalidation and workflow gates; no product windows or input.')
