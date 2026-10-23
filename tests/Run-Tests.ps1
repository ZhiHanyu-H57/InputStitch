[CmdletBinding()]
param([string[]]$Suite, [switch]$List, [switch]$SettingsSmoke)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$allSuites = @('SourceManifestTests','ForegroundIdentityCacheTests','TickCadenceTests','InputPipelineSchedulerTests','InputPipelineIntegrationTests','InputProviderSeamTests','Sdl3ProviderTests','CompositeInputProviderTests','Sdl3DeviceIdentityTests','KeyboardTests','IdleGamepadTests','XInputInputTests','DeviceIdentityTests','RouterSourcePolicyTests','AnalogTransformTests','ActivatorConditionTests','GamepadRouterTests','ControlledReplacementTests','SlotAcquisitionTests','VirtualGamepadPreferenceTests','LayerTests','LayerContextTests','MacroTimingTests','MacroWorkerPoolTests','MacroRunCoordinatorTests','ModifierSafetyPolicyTests','ReleasePolicyTests','UpdateInstallerTests','UpdateNetworkTests','UiSafetyTests','ProductivityTests','OutputOwnershipTests','ProductAcceptanceTests','CrosshairTests','SecondaryLayoutTests')
$allSuites += 'AdvancedTriggerDispatchTests'
$allSuites += 'RecordingEditingTests'
$allSuites += @('CornerCutCrosshairTests','CornerCutDialogTests','TimerTests','NetworkControlTests','ToolIntegrationTests')
if ($List) { $allSuites; return }
$fullRun = -not $Suite
$selectedSuites = @(if ($fullRun) { $allSuites } else { $Suite | ForEach-Object { $_.Split(',') } | Select-Object -Unique })
foreach ($name in $selectedSuites) {
  if ($name -notin $allSuites) { throw "Unknown suite '$name'. Use -List to see accepted suite names." }
}
Write-Output ("Running {0} of {1} suites ({2})." -f $selectedSuites.Count, $allSuites.Count, $(if ($fullRun) { 'full' } else { 'targeted' }))
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
$productionSources = & (Join-Path $project 'scripts/Get-ProductionSources.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not (Test-Path -LiteralPath $compiler)) { $compiler = $compiler.Replace('Framework64','Framework') }
$output = Join-Path $project 'artifacts/tests'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$client = Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll'

function Write-CiError([string]$title, $lines) {
  $text = (@($lines) | Select-Object -Last 12 | Out-String).Trim()
  if([string]::IsNullOrWhiteSpace($text)) { $text = 'No detailed test output was captured.' }
  $safe = $text.Replace('%','%25').Replace("`r",'%0D').Replace("`n",'%0A')
  Write-Output ("::error title={0}::{1}" -f $title,$safe)
}

function Invoke-NativeCaptured([string]$file, [object[]]$arguments) {
  $previous = $ErrorActionPreference
  try {
    # Windows PowerShell 5.1 can promote any native stderr line to a terminating
    # NativeCommandError while ErrorActionPreference=Stop, before LASTEXITCODE can
    # be inspected. Capture both streams first, then decide solely from exit code.
    $ErrorActionPreference = 'Continue'
    $captured = & $file @arguments 2>&1
    $exitCode = $LASTEXITCODE
  } finally {
    $ErrorActionPreference = $previous
  }
  return [pscustomobject]@{ ExitCode = $exitCode; Output = @($captured) }
}

foreach($testSuite in $selectedSuites) {
  $exe=Join-Path $output ($testSuite+'.exe')
  $entryPoint = if ($testSuite -eq 'NetworkControlTests') { 'InputStitch.NetworkControlTests' } else { $testSuite }
  $arguments=@('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+',"/main:$entryPoint","/out:$exe")
  foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:'+(Join-Path $references $name) }
  $arguments += '/win32icon:' + (Join-Path $project 'InputStitch.ico')
$arguments += '/reference:'+$client
  $arguments += '/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
  $arguments += @(& (Join-Path $project 'scripts/Get-UiResourceArguments.ps1'))
  foreach($file in $productionSources) { $arguments += $file }
  $arguments += Join-Path $PSScriptRoot ($testSuite+'.cs')
  if ($testSuite -eq 'CornerCutCrosshairTests') { $arguments += Join-Path $PSScriptRoot 'Fixtures/LegacyCrosshair.cs' }
  if ($testSuite -in @('CornerCutDialogTests','ToolIntegrationTests')) { $arguments += Join-Path $PSScriptRoot 'SecondaryLayoutTests.cs' }
  $compile = Invoke-NativeCaptured $compiler $arguments
  $compileReport = $compile.Output
  if($compile.ExitCode -ne 0) { $compileReport | Write-Output; Write-CiError "$testSuite compilation failed" $compileReport; throw "$testSuite compilation failed" }
  $testArguments = if ($testSuite -eq 'SecondaryLayoutTests') { @('--scaled-fonts') } elseif ($testSuite -eq 'ToolIntegrationTests') { if ($fullRun) { @('--core','--ui') } else { @('--core') } } else { @() }
  $execution = Invoke-NativeCaptured $exe $testArguments
  $report = $execution.Output
  $report | Set-Content -LiteralPath (Join-Path $output ($testSuite+'.log')) -Encoding UTF8
  if($execution.ExitCode -ne 0) { $report | Write-Output; Write-CiError "$testSuite failed" $report; throw "$testSuite failed" }
  $report | Select-Object -Last 1 | Write-Output
}
Write-Output ("PASS {0} selected regression suites. Tests use isolated observers/native target windows; real output acceptance is separate." -f $selectedSuites.Count)
if ($fullRun -or $SettingsSmoke) {
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'SettingsSmoke.ps1')
  if($LASTEXITCODE -ne 0) { Write-CiError 'Settings smoke tests failed' @('SettingsSmoke.ps1 returned a non-zero exit code.'); throw 'Settings smoke tests failed' }
}
