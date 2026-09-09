[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$references = & (Join-Path $project 'scripts/Restore-NetFramework472.ps1')
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

foreach($suite in @('KeyboardTests','IdleGamepadTests','XInputInputTests','GamepadRouterTests','ControlledReplacementTests','SlotAcquisitionTests','VirtualGamepadPreferenceTests','LayerTests','MacroTimingTests','ModifierSafetyPolicyTests','ReleasePolicyTests','UpdateInstallerTests','UpdateNetworkTests','UiSafetyTests','ProductivityTests','OutputOwnershipTests')) {
  $exe=Join-Path $output ($suite+'.exe')
  $arguments=@('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+',"/main:$suite","/out:$exe")
  foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:'+(Join-Path $references $name) }
  $arguments += '/reference:'+$client
  $arguments += '/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
  foreach($file in @('InputStitch.cs','ReleaseInfo.cs','VirtualKeyboard.cs','IdleGamepad.cs','XInputInput.cs','GamepadRouter.cs','ControlledReplacement.cs','SlotAcquisition.cs','UpdateInstaller.cs','RuntimeDiagnostics.cs','ConfigStore.cs','StepHistory.cs','QuickCreate.cs','ConcurrentRuntime.cs','OutputOwnership.cs')) { $arguments += Join-Path $project $file }
  $arguments += Join-Path $PSScriptRoot ($suite+'.cs')
  $compile = Invoke-NativeCaptured $compiler $arguments
  $compileReport = $compile.Output
  if($compile.ExitCode -ne 0) { $compileReport | Write-Output; Write-CiError "$suite compilation failed" $compileReport; throw "$suite compilation failed" }
  $execution = Invoke-NativeCaptured $exe @()
  $report = $execution.Output
  if($execution.ExitCode -ne 0) { $report | Write-Output; Write-CiError "$suite failed" $report; throw "$suite failed" }
  $report | Select-Object -Last 1 | Write-Output
}
Write-Output 'All regression suites passed. No real input or virtual devices were used.'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'SettingsSmoke.ps1')
if($LASTEXITCODE -ne 0) { Write-CiError 'Settings smoke tests failed' @('SettingsSmoke.ps1 returned a non-zero exit code.'); throw 'Settings smoke tests failed' }
