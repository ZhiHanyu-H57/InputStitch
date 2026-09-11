[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $toolRoot -Parent) -Parent
$references = & (Join-Path $projectRoot 'scripts\Restore-NetFramework472.ps1')
$compiler = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = $compiler.Replace('Framework64', 'Framework') }
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler was not found.' }

$outputDir = Join-Path $projectRoot 'artifacts\InputLab'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$output = Join-Path $outputDir 'InputStitch-OwnershipAcceptance.exe'
$client = Join-Path $projectRoot 'third-party\Nefarius.ViGEm.Client\Nefarius.ViGEm.Client.dll'

$arguments = @(
    '/nologo',
    '/noconfig',
    '/nostdlib+',
    '/target:exe',
    '/platform:x64',
    '/optimize+',
    '/warn:4',
    '/warnaserror+',
    '/main:InputStitch.Tools.InputLab.AcceptanceProgram',
    "/out:$output"
)
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll','Facades\netstandard.dll')) {
    $arguments += '/reference:' + (Join-Path $references $name)
}
$arguments += '/reference:' + $client

foreach ($name in @(
    'InputStitch.cs','ReleaseInfo.cs','RawKeyboardFallback.cs','VirtualKeyboard.cs','IdleGamepad.cs',
    'XInputNative.cs','XInputInput.cs','GamepadRouter.cs','ControlledReplacement.cs','SlotAcquisition.cs',
    'UpdateInstaller.cs','RuntimeDiagnostics.cs','AtomicXmlFileStore.cs','ConfigPackageSerializer.cs',
    'UpdateSourcePolicy.cs','UpdateUiCoordinator.cs','ProfileCatalog.cs','TargetWindowPolicy.cs','ConfigStore.cs',
    'StepHistory.cs','QuickCreate.cs','ConcurrentRuntime.cs','OutputOwnership.cs','VirtualGamepadBackend.cs'
)) {
    $arguments += Join-Path $projectRoot $name
}
foreach ($name in @('NativeInput.cs','XInputReader.cs','ObservationSnapshot.cs','StickView.cs','InputLabForm.cs','AcceptanceRunner.cs')) {
    $arguments += Join-Path $toolRoot $name
}

& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Ownership acceptance compilation failed with exit code $LASTEXITCODE." }
Copy-Item -LiteralPath $client -Destination (Join-Path $outputDir 'Nefarius.ViGEm.Client.dll') -Force
Write-Output $output
