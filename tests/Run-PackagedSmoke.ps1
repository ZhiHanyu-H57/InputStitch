[CmdletBinding()]
param([ValidateSet('x64','x86')][string]$Architecture = 'x64')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (($Architecture -eq 'x86' -and [Environment]::Is64BitProcess) -or
    ($Architecture -eq 'x64' -and -not [Environment]::Is64BitProcess)) {
    $engine = if ($Architecture -eq 'x86') {
        Join-Path $env:WINDIR 'SysWOW64/WindowsPowerShell/v1.0/powershell.exe'
    } else { Join-Path $env:WINDIR 'Sysnative/WindowsPowerShell/v1.0/powershell.exe' }
    & $engine -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath -Architecture $Architecture
    exit $LASTEXITCODE
}
$project = Split-Path $PSScriptRoot -Parent
$info = & (Join-Path $project 'scripts/Get-ReleaseInfo.ps1')
$exe = Join-Path $project ("dist/InputStitch-{0}-Windows-{1}.exe" -f $info.Version, $Architecture)
$output = Join-Path $project ("artifacts/packaged-smoke/{0}" -f $Architecture)
New-Item -ItemType Directory -Path $output -Force | Out-Null
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$null = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes((Join-Path $project 'third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll')))
$assembly = [Reflection.Assembly]::LoadFrom($exe)
# Redirect the packaged assembly's paths in this isolated test process only.
# No environment-variable override of Windows known folders is assumed.
$paths = $assembly.GetType('InputStitch.AppPaths')
foreach ($entry in @{
    Root=$output; Config=(Join-Path $output 'config.xml'); DeviceRegistry=(Join-Path $output 'devices.xml');
    MacroPackages=(Join-Path $output 'macro-packages'); Profiles=(Join-Path $output 'profiles');
    Backups=(Join-Path $output 'backups'); Logs=(Join-Path $output 'logs')
}.GetEnumerator()) { $paths.GetField($entry.Key).SetValue($null, $entry.Value) }
$config = New-Object InputStitch.MacroConfig
$config.GamepadDeviceType = 'None'
$config.UpdateMode = 'Disabled'
$config.Sdl3InputEnabled = $false
$config.HasSeenWelcome = $true
$config.LastShownReleaseSummaryVersion = $info.Version
$config.Crosshair.Enabled = $true
$config.Crosshair.Visible = $true
$config.Crosshair.Size = 31
$config.Crosshair.OffsetX = 7
$config.Crosshair.OffsetY = -9
$macro = New-Object InputStitch.MacroDefinition
$macro.Name = 'Packaged startup check'
$macro.Enabled = $false
$config.Macros.Add($macro)
$serializer = New-Object System.Xml.Serialization.XmlSerializer([InputStitch.MacroConfig])
$stream = [IO.File]::Create((Join-Path $output 'config.xml'))
try { $serializer.Serialize($stream, $config) } finally { $stream.Dispose() }
$form = New-Object InputStitch.MainForm
$form.StartPosition = 'Manual'
$form.Location = New-Object Drawing.Point(-20000,-20000)
$form.ShowInTaskbar = $false
$timer = New-Object Windows.Forms.Timer
$timer.Interval = 2500
$script:smokeFailure = $null
$timer.add_Tick({
    $timer.Stop()
    try {
        if ($form.IsDisposed -or -not $form.IsHandleCreated) { throw 'Packaged main window did not start.' }
        $mode = $form.GetType().GetField('triggerModeBox',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($form)
        if ($mode.Items.Count -ne 7 -or $mode.SelectedIndex -ne 0) { throw 'Packaged unified editor did not initialize.' }
        $privateFlags = [Reflection.BindingFlags]'Instance,NonPublic'
        $controller = $form.GetType().GetField('crosshairController',$privateFlags).GetValue($form)
        if ($null -eq $controller -or -not $controller.GetType().GetProperty('Visible',$privateFlags).GetValue($controller)) { throw 'Saved enabled crosshair did not restore at startup.' }
        $window = $controller.GetType().GetProperty('Window',$privateFlags).GetValue($controller)
        $bounds = $window.GetType().GetProperty('Bounds',$privateFlags).GetValue($window)
        $screen = [Windows.Forms.Screen]::PrimaryScreen.Bounds
        if (($bounds.Left + [Math]::Floor($bounds.Width / 2)) -ne ($screen.Left + [Math]::Floor($screen.Width / 2) + 7) -or
            ($bounds.Top + [Math]::Floor($bounds.Height / 2)) -ne ($screen.Top + [Math]::Floor($screen.Height / 2) - 9)) { throw 'Packaged crosshair did not restore pixel offsets.' }
        $script:crosshairWindow = $window
        $bitmap = New-Object Drawing.Bitmap($form.Width,$form.Height)
        try { $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$bitmap.Width,$bitmap.Height))); $bitmap.Save((Join-Path $output 'startup.png')) } finally { $bitmap.Dispose() }
        [pscustomobject]@{ status='PASS'; version=$info.Version; architecture=$Architecture; seconds=2.5; packagedAssembly=$exe; configRoot=$output; mainWindow=$form.Text; crosshairRestored=$true; crosshairOffsetX=7; crosshairOffsetY=-9 } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'report.json') -Encoding UTF8
    } catch { $script:smokeFailure = $_ }
    finally { $form.Close() }
})
$timer.Start()
try { [Windows.Forms.Application]::Run($form) } finally { $timer.Dispose(); $form.Dispose() }
if ($script:smokeFailure) { throw $script:smokeFailure }
if ($script:crosshairWindow -and $script:crosshairWindow.Handle -ne [IntPtr]::Zero) { throw 'Crosshair native window survived application shutdown.' }
Write-Output ("PASS packaged {0} startup, live message loop and orderly shutdown; isolated configuration, disabled macros." -f $Architecture)
