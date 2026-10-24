[CmdletBinding()]
param([ValidateSet('Development','Release')][string]$Mode='Development',[string]$BaseRef,[switch]$PlanOnly,
    [switch]$RequireSoak,[ValidateRange(1,1440)][int]$SoakMinutes=10,[switch]$NoReuse,
    [string]$ReceiptDirectory,[string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $project 'scripts/Verification.psm1') -Force
if(!$ReceiptDirectory){$ReceiptDirectory=Join-Path $project 'verification/receipts'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $project 'artifacts/verification'}
if($Mode -eq 'Release' -and !$BaseRef){throw 'Release verification requires the previous published Stable ref in -BaseRef.'}
$plan=Get-VerificationPlan -Mode $Mode -BaseRef $BaseRef -RequireSoak:$RequireSoak
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$plan | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputDirectory 'plan.json') -Encoding UTF8
if($plan.UnclassifiedPaths.Count){throw ('Classify changed paths before verification: '+($plan.UnclassifiedPaths -join ', '))}
Write-Output ("{0} verification from {1}: {2} selected checks; soak={3}." -f $Mode,$plan.BaseRef,$plan.Checks.Count,$plan.SoakRequired)
if($PlanOnly){$plan.Checks;return}
$results=[Collections.Generic.List[object]]::new()
$compiledSuites=[Collections.Generic.HashSet[string]]::new()
$localReceipts=Join-Path $OutputDirectory 'receipts'
foreach($check in $plan.Checks){
    $fingerprint=Get-VerificationFingerprint -Check $check
    $id=$check.Replace(':','-'); $reused=$null
    if(!$NoReuse){
        $files=@(foreach($directory in @($ReceiptDirectory,$localReceipts)){if(Test-Path -LiteralPath $directory){Get-ChildItem -LiteralPath $directory -Filter '*.json' -File}})
        foreach($file in $files){
            try{$candidate=Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json}catch{Write-Warning ('Ignoring unreadable receipt '+$file.Name);continue}
            if(Test-VerificationReceipt -Receipt $candidate -Check $check -Fingerprint $fingerprint -Root $project){$reused=$candidate;break}
        }
    }
    if($reused){Write-Output ('REUSE '+$check+' from '+$reused.sourceCommit);$results.Add([pscustomobject]@{check=$check;status='PASS';action='reused';sourceCommit=$reused.sourceCommit;fingerprint=$fingerprint;qualification=$reused.qualification});continue}
    if($check -like 'native:*'){throw ('Fresh dedicated acceptance required for '+$check+'; prior evidence inputs differ. Use its documented isolated runner, then record that result.')}
    $log=Join-Path $OutputDirectory ($id+'.log')
    Write-Output ('RUN '+$check)
    if($check -like 'suite:*'){
        $suite=$check.Substring(6)
        & (Join-Path $PSScriptRoot 'Run-Tests.ps1') -Suite $suite | Tee-Object -FilePath $log
        [void]$compiledSuites.Add($suite)
    }else{
        switch($check){
            'workflow' {& (Join-Path $PSScriptRoot 'VerificationWorkflowTests.ps1') | Tee-Object -FilePath $log}
            'ui:secondary' {& (Join-Path $PSScriptRoot 'Run-SecondaryLayout.ps1') -ScaledFonts | Tee-Object -FilePath $log}
            'ui:settings' {& (Join-Path $PSScriptRoot 'Run-SecondaryLayout.ps1') -ScaledFonts -SettingsOnly | Tee-Object -FilePath $log}
            'ui:settings-native' {& (Join-Path $PSScriptRoot 'Run-SecondaryLayout.ps1') -Native -SettingsOnly | Tee-Object -FilePath $log}
            'ui:journey' {& (Join-Path $PSScriptRoot 'Run-ProductAcceptance.ps1') -JourneyOnly | Tee-Object -FilePath $log}
            'ui:settings-smoke' {& (Join-Path $PSScriptRoot 'Run-IsolatedUiScript.ps1') -Path (Join-Path $PSScriptRoot 'SettingsSmoke.ps1') | Tee-Object -FilePath $log}
            'ui:tools' {
                if(!$compiledSuites.Contains('ToolIntegrationTests')){& (Join-Path $PSScriptRoot 'Run-Tests.ps1') -Suite ToolIntegrationTests -CompileOnly | Out-Null}
                & (Join-Path $project 'artifacts/tests/ToolIntegrationTests.exe') --ui | Tee-Object -FilePath $log
                if($LASTEXITCODE -ne 0){throw 'Tool UI check failed.'}
            }
            'runtime:soak' {& (Join-Path $PSScriptRoot 'Run-ProductAcceptance.ps1') -SoakMinutes $SoakMinutes | Tee-Object -FilePath $log}
        }
    }
    $sourceCommit=(& git -C $project rev-parse HEAD).Trim()
    $relative=[Uri]::UnescapeDataString(([Uri]($project.TrimEnd('\')+'\')).MakeRelativeUri([Uri]$log).ToString())
    $receipt=[pscustomobject]@{schema=1;check=$check;status='PASS';sourceCommit=$sourceCommit;fingerprint=$fingerprint;qualification=('Windows '+[Environment]::OSVersion.Version+'; '+$env:PROCESSOR_ARCHITECTURE+'; .NET Framework 4.7.2 target; see original log.');evidence=@([pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $log).Hash})}
    New-Item -ItemType Directory -Force -Path $localReceipts | Out-Null
    $receipt | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $localReceipts ($id+'.json')) -Encoding UTF8
    $results.Add([pscustomobject]@{check=$check;status='PASS';action='executed';sourceCommit=$sourceCommit;fingerprint=$fingerprint;qualification=$receipt.qualification})
}
[pscustomobject]@{mode=$Mode;baseRef=$plan.BaseRef;checks=$results.ToArray();soakRequired=$plan.SoakRequired;status='PASS'} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $OutputDirectory 'result.json') -Encoding UTF8
Write-Output ('PASS selected verification: '+$results.Count+' checks; reused='+@($results | Where-Object action -eq 'reused').Count+'; executed='+@($results | Where-Object action -eq 'executed').Count)
