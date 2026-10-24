Set-StrictMode -Version Latest
$script:Project = Split-Path $PSScriptRoot -Parent

function Get-VerificationPlan {
    param([string[]]$ChangedPaths, [string]$BaseRef, [ValidateSet('Development','Release')][string]$Mode='Development', [switch]$RequireSoak)
    if ($null -eq $ChangedPaths) {
        if (!$BaseRef) {
            $BaseRef = (& git -C $script:Project merge-base HEAD origin/main).Trim()
            if ($BaseRef -eq (& git -C $script:Project rev-parse HEAD).Trim()) { $BaseRef='HEAD^' }
        }
        $ChangedPaths = @(& git -C $script:Project diff --name-only --no-renames $BaseRef --)
        if ($LASTEXITCODE -ne 0) { throw "Cannot resolve verification baseline: $BaseRef" }
        $ChangedPaths += @(& git -C $script:Project ls-files --others --exclude-standard)
    }
    $checks = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $unknown = [Collections.Generic.List[string]]::new()
    $suites = @(& (Join-Path $script:Project 'tests/Run-Tests.ps1') -List)
    foreach ($raw in $ChangedPaths) {
        $path = $raw.Replace('\','/')
        if ($path -match '^tests/([^/]+Tests)\.cs$' -and $Matches[1] -in $suites) { [void]$checks.Add('suite:'+$Matches[1]); continue }
        if ($path -match '(^\.gitattributes$|^\.gitignore$|^scripts/Verification\.psm1$|^tests/(Run-Tests|Run-Verification|VerificationWorkflowTests|Record-VerificationEvidence)\.ps1$|^verification/|^\.github/workflows/)') { [void]$checks.Add('workflow'); continue }
        if ($path -match '^[^/]+\.md$|^(docs/|website/|LICENSE|\.gitignore)') { continue }
        if ($path -eq 'ReleaseInfo.cs') { foreach($id in @('suite:ReleasePolicyTests','ui:settings','ui:settings-native','ui:settings-smoke')){[void]$checks.Add($id)}; continue }
        if ($path -match '^(build\.ps1|app\.manifest|InputStitch\.csproj|InputStitch\.ico|Assets/|scripts/(Restore-|Get-|production-sources)|third-party/)') { [void]$checks.Add('suite:SourceManifestTests'); [void]$checks.Add('suite:ReleasePolicyTests'); continue }
        if ($path -match '^tests/(IsolatedUiDesktop\.cs|UiScriptHost\.cs|Run-IsolatedUiScript\.ps1)$') { foreach($id in @('suite:UiSafetyTests','ui:journey','ui:settings-smoke','workflow')){[void]$checks.Add($id)}; continue }
        if ($path -match '^tests/(Run-SecondaryLayout|Run-ProductAcceptance|SettingsSmoke|Run-PackagedSmoke)\.ps1$') { [void]$checks.Add('ui:journey'); [void]$checks.Add('ui:settings-smoke'); continue }
        if ($path -match '^tests/(obs-capture/|Run-Obs|TimerD3D|Run-TimerAcceptance|Compare-Timer)') { [void]$checks.Add('native:obs'); continue }
        if ($path -match '^tests/(isolated-network/|LiveNetwork|Run-(Live|Isolated)Network)') { [void]$checks.Add('native:network'); continue }
        if ($path -match '^tests/(fixtures/|Fixtures/)') { [void]$checks.Add('suite:ProductivityTests'); continue }
        if ($path -match '^tools/InputLab/') { continue } # Its own path-filtered workflow owns packaging/self-tests.
        if ($path -notmatch '^[^/]+\.cs$') { $unknown.Add($path); continue }
        [void]$checks.Add('suite:SourceManifestTests')
        if ($path -match '^(ConcurrentRuntime|MacroRunCoordinator|MacroWorkerPool|OutputOwnership|MainForm\.AdvancedTriggers|InputPipeline)\.cs$') {
            foreach($id in @('suite:MacroRunCoordinatorTests','suite:InputPipelineIntegrationTests','suite:OutputOwnershipTests','suite:AdvancedTriggerDispatchTests','runtime:soak')){[void]$checks.Add($id)}; continue
        }
        if ($path -match '^(Timer|TimerOverlay|TimerRtss|MainForm\.Tools|NetworkControl|NetworkPrivilegeHelper)\.cs$') {
            foreach($id in @('suite:TimerTests','suite:NetworkControlTests','suite:ToolIntegrationTests')){[void]$checks.Add($id)}
            if($path -match '^(Timer|TimerOverlay|TimerRtss)\.cs$'){[void]$checks.Add('native:obs')}
            if($path -match '^(NetworkControl|NetworkPrivilegeHelper)\.cs$'){[void]$checks.Add('native:network')}; continue
        }
        if ($path -match '^(ToolSettingsDialogs|NetworkSettingsDialog)\.cs$') { [void]$checks.Add('suite:ToolIntegrationTests'); [void]$checks.Add('ui:tools'); continue }
        if ($path -match '^Crosshair|^MainForm\.Crosshair') { [void]$checks.Add('suite:CrosshairTests'); [void]$checks.Add('suite:CornerCutCrosshairTests'); [void]$checks.Add('suite:CornerCutDialogTests'); if($path -eq 'CrosshairOverlay.cs'){[void]$checks.Add('native:crosshair')}; continue }
        if ($path -match '^(EditorPresentation|DialogPresentation|StepEditDialog|SettingsDialog|GamepadPreviewControl|Dialogs|VirtualKeyboard|QuickCreate|MouseInputDiagram|MainForm\.Ui|MainForm\.Layers)\.cs$') {
            foreach($id in @('suite:UiSafetyTests','ui:secondary','ui:tools','ui:journey')){[void]$checks.Add($id)}; continue
        }
        if ($path -match '^IdleGamepad\.cs$') { [void]$checks.Add('suite:IdleGamepadTests'); [void]$checks.Add('ui:settings'); [void]$checks.Add('ui:settings-native'); continue }
        if ($path -match '^(InputStitch|RuntimeDiagnostics)\.cs$') { foreach($id in @('suite:UiSafetyTests','suite:InputPipelineIntegrationTests','suite:MacroRunCoordinatorTests','suite:ProductivityTests','suite:ToolIntegrationTests','ui:journey','ui:settings-smoke')){[void]$checks.Add($id)}; continue }
        if ($path -match '^(Config|AtomicXml|ProfileCatalog|StepHistory|StepTimingEditor)') { [void]$checks.Add('suite:ProductivityTests'); [void]$checks.Add('suite:ToolIntegrationTests'); [void]$checks.Add('ui:settings-smoke'); continue }
        if ($path -match '^Update') { foreach($id in @('suite:UpdateInstallerTests','suite:UpdateNetworkTests','suite:ReleasePolicyTests')){[void]$checks.Add($id)}; continue }
        if ($path -match '^(InputProvider|CompositeInputProvider|Sdl3|RawKeyboard|XInput|WindowsDevice|DeviceIdentity|GamepadRouter|VirtualGamepad|ActivatorCondition|AnalogTransform|RouterSourcePolicy|ControlledReplacement|SlotAcquisition)') {
            foreach($id in @('suite:InputPipelineIntegrationTests','suite:InputProviderSeamTests','suite:DeviceIdentityTests','suite:GamepadRouterTests','suite:UiSafetyTests')){[void]$checks.Add($id)}; continue
        }
        if ($path -match 'Dialog\.cs$|^MainForm\.') { [void]$checks.Add('ui:secondary'); continue }
        $unknown.Add($path)
    }
    # Settings metadata has a separate fresh check. Other UI evidence need not be
    # invalidated by a version/channel promotion with unchanged feature code.
    if ($Mode -eq 'Release') { foreach($id in @('suite:ReleasePolicyTests','ui:settings-smoke')){[void]$checks.Add($id)} }
    if ($RequireSoak) { [void]$checks.Add('runtime:soak') }
    if($checks.Remove('suite:SecondaryLayoutTests')){[void]$checks.Add('ui:secondary')}
    if($checks.Remove('suite:ProductAcceptanceTests')){[void]$checks.Add('ui:journey')}
    [pscustomobject]@{Mode=$Mode;BaseRef=$BaseRef;ChangedPaths=@($ChangedPaths | Sort-Object -Unique);Checks=@($checks | Sort-Object);UnclassifiedPaths=@($unknown);SoakRequired=$checks.Contains('runtime:soak')}
}

function Get-VerificationInputs {
    param([string]$Check, [string]$Ref,[string]$Root=$script:Project)
    $tracked = if($Ref){@(& git -C $Root ls-tree -r --name-only $Ref)}else{@(& git -C $Root ls-files --cached --others --exclude-standard)}
    if($LASTEXITCODE -ne 0){throw 'Cannot enumerate verification inputs.'}
    if($Check -eq 'native:obs') { return @($tracked | Where-Object {$_ -match '^(InputStitch\.cs|Crosshair\.cs|CrosshairOverlay\.cs|Timer\.cs|TimerOverlay\.cs|TimerRtss\.cs|tests/(obs-capture/|Run-Obs|TimerD3D|Run-TimerAcceptance))'}) }
    if($Check -eq 'native:network') { return @($tracked | Where-Object {$_ -match '^(InputStitch\.cs|NetworkControl\.cs|NetworkPrivilegeHelper\.cs|tests/(isolated-network/|LiveNetwork|Run-(Live|Isolated)Network))'}) }
    if($Check -eq 'native:crosshair'){return @($tracked | Where-Object {$_ -match '^Crosshair(Overlay)?\.cs$|^tests/CrosshairTests\.cs$'})}
    if($Check -eq 'workflow'){return @($tracked | Where-Object {$_ -match '^(\.gitattributes$|\.gitignore$|scripts/Verification\.psm1|tests/(Run-Tests|Run-Verification|VerificationWorkflowTests|Record-VerificationEvidence)\.ps1|\.github/workflows/)'})}
    if($Check -eq 'runtime:soak') { return @($tracked | Where-Object {$_ -match '^(ConcurrentRuntime|MacroRunCoordinator|MacroWorkerPool|OutputOwnership|MainForm\.AdvancedTriggers|InputPipeline)\.cs$|^tests/(ProductAcceptanceTests\.cs|Run-ProductAcceptance\.ps1|IsolatedUiDesktop\.cs)$'}) }
    $sourceManifest = if($Ref){@(& git -C $Root show ($Ref+':scripts/production-sources.txt'))}else{Get-Content (Join-Path $Root 'scripts/production-sources.txt')}
    $inputs=@($sourceManifest | Where-Object {$_ -match '^[^#].*\.cs$' -and $_ -ne 'ReleaseInfo.cs'})
    $inputs+=@($tracked | Where-Object {$_ -match '^(Assets/|app\.manifest$|InputStitch\.ico$|third-party/Nefarius|scripts/(Restore-NetFramework472|Get-UiResourceArguments|Get-ProductionSources)\.ps1$|scripts/production-sources\.txt$)'})
    if($Check -match '^suite:(.+)$') {
        $suite=$Matches[1];$inputs+='tests/'+$suite+'.cs';$inputs+='tests/IsolatedUiDesktop.cs'
        if($suite -in @('ReleasePolicyTests','UpdateInstallerTests','UpdateNetworkTests')){$inputs+='ReleaseInfo.cs'}
        if($suite -in @('CornerCutDialogTests','ToolIntegrationTests')){$inputs+='tests/SecondaryLayoutTests.cs'}
        if($suite -eq 'CornerCutCrosshairTests'){$inputs+='tests/fixtures/LegacyCrosshair.cs'}
        if($suite -eq 'ProductivityTests'){$inputs+=@($tracked | Where-Object {$_ -match '^tests/fixtures/'})}
        if($suite -eq 'SourceManifestTests'){$inputs+=@('build.ps1','InputStitch.csproj','tests/Run-Tests.ps1','tools/InputLab/build-acceptance.ps1')}
        if($suite -eq 'ReleasePolicyTests'){$inputs+=@('build.ps1','scripts/Verify-Release.ps1','scripts/Publish-Stable.ps1','scripts/Publish-Beta.ps1','.github/workflows/release.yml')}
    } else {
        $inputs+='tests/IsolatedUiDesktop.cs'
        switch($Check){
            'ui:secondary' {$inputs+=@('tests/SecondaryLayoutTests.cs','tests/Run-SecondaryLayout.ps1')}
            'ui:tools' {$inputs+=@('tests/ToolIntegrationTests.cs','tests/SecondaryLayoutTests.cs')}
            'ui:journey' {$inputs+=@('tests/ProductAcceptanceTests.cs','tests/Run-ProductAcceptance.ps1')}
            'ui:settings' {$inputs+=@('ReleaseInfo.cs','tests/SecondaryLayoutTests.cs','tests/Run-SecondaryLayout.ps1')}
            'ui:settings-native' {$inputs+=@('ReleaseInfo.cs','tests/SecondaryLayoutTests.cs','tests/Run-SecondaryLayout.ps1')}
            'ui:settings-smoke' {$inputs+=@('ReleaseInfo.cs','tests/SettingsSmoke.ps1','tests/UiScriptHost.cs','tests/Run-IsolatedUiScript.ps1')}
            default {throw "Unknown check: $Check"}
        }
    }
    @($inputs | Sort-Object -Unique)
}

function Get-VerificationFingerprint {
    param([string]$Check,[string]$Ref,[string]$Root=$script:Project)
    $paths=@(Get-VerificationInputs -Check $Check -Ref $Ref -Root $Root)
    $rows=[Collections.Generic.List[string]]::new(); $rows.Add('verification-v1/'+$Check)
    if($Ref){
        $tree=@{}; foreach($line in @(& git -C $Root ls-tree -r $Ref)) {if($line -match '^\d+ blob ([0-9a-f]+)\t(.+)$'){$tree[$Matches[2]]=$Matches[1]}}
        foreach($path in $paths){$rows.Add($path+'='+$(if($tree.ContainsKey($path)){$tree[$path]}else{'missing'}))}
    }else{
        $existing=@($paths | Where-Object {Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf})
        $hashes=@($existing | & git -C $Root hash-object --stdin-paths)
        if($LASTEXITCODE -ne 0 -or $hashes.Count -ne $existing.Count){throw 'Cannot fingerprint verification inputs.'}
        $map=@{};for($i=0;$i -lt $existing.Count;$i++){$map[$existing[$i]]=$hashes[$i]}
        foreach($path in $paths){$rows.Add($path+'='+$(if($map.ContainsKey($path)){$map[$path]}else{'missing'}))}
    }
    $sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}

function Test-VerificationReceipt {
    param($Receipt,[string]$Check,[string]$Fingerprint,[string]$Root=$script:Project)
    $fields=@($Receipt.PSObject.Properties | ForEach-Object {$_.Name})
    foreach($field in @('schema','check','status','fingerprint','evidence','sourceCommit','qualification')){if($field -notin $fields){return $false}}
    if($Receipt.schema -ne 1 -or $Receipt.check -ne $Check -or $Receipt.status -ne 'PASS' -or $Receipt.fingerprint -ne $Fingerprint -or $Receipt.sourceCommit -notmatch '^[0-9a-f]{40}$'){return $false}
    foreach($item in $Receipt.evidence){
        $fields=@($item.PSObject.Properties | ForEach-Object {$_.Name})
        if('path' -notin $fields -or 'sha256' -notin $fields){return $false}
        if([IO.Path]::IsPathRooted($item.path) -or $item.path -match '(^|[\\/])\.\.([\\/]|$)'){return $false}
        $path=Join-Path $Root $item.path
        if(!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $item.sha256){return $false}
    }
    return @($Receipt.evidence).Count -gt 0
}

Export-ModuleMember -Function Get-VerificationPlan,Get-VerificationInputs,Get-VerificationFingerprint,Test-VerificationReceipt
