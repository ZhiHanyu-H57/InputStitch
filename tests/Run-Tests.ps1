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
foreach($suite in @('KeyboardTests','IdleGamepadTests','ModifierSafetyPolicyTests','ReleasePolicyTests','UpdateInstallerTests','UiSafetyTests','ProductivityTests','OutputOwnershipTests')) {
  $exe=Join-Path $output ($suite+'.exe')
  $arguments=@('/nologo','/noconfig','/nostdlib+','/target:exe','/platform:x64','/warnaserror+',"/main:$suite","/out:$exe")
  foreach($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades/netstandard.dll')) { $arguments += '/reference:'+(Join-Path $references $name) }
  $arguments += '/reference:'+$client
  $arguments += '/resource:'+$client+',InputStitch.ThirdParty.Nefarius.ViGEm.Client.dll'
  foreach($file in @('InputStitch.cs','ReleaseInfo.cs','VirtualKeyboard.cs','IdleGamepad.cs','UpdateInstaller.cs','RuntimeDiagnostics.cs','ConfigStore.cs','StepHistory.cs','QuickCreate.cs','OutputOwnership.cs')) { $arguments += Join-Path $project $file }
  $arguments += Join-Path $PSScriptRoot ($suite+'.cs')
  & $compiler @arguments
  if($LASTEXITCODE -ne 0) { throw "$suite compilation failed" }
  $report = & $exe 2>&1
  if($LASTEXITCODE -ne 0) { $report | Write-Output; throw "$suite failed" }
  $report | Select-Object -Last 1 | Write-Output
}
Write-Output 'All regression suites passed. No real input or virtual devices were used.'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'SettingsSmoke.ps1')
if($LASTEXITCODE -ne 0) { throw 'Settings smoke tests failed' }
