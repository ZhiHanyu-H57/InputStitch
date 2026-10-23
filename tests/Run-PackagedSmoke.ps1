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
$assembly = [Reflection.Assembly]::LoadFrom($exe)
$assembly.GetType('InputStitch.EmbeddedDependencyLoader').GetMethod('Register').Invoke($null,@()) | Out-Null
$client = [Reflection.Assembly]::Load('Nefarius.ViGEm.Client')
if ($client.Location) { throw 'The packaged managed dependency was not loaded from its embedded resource.' }
$native = $assembly.GetType('InputStitch.Sdl3NativeApi')
$hidden = [Reflection.BindingFlags]'Static,NonPublic'
$mouseArtwork = $assembly.GetType('InputStitch.MouseInputDiagram').GetMethod('LoadPhoto',$hidden).Invoke($null,@())
try {
    if ($mouseArtwork.Width -ne 320 -or $mouseArtwork.Height -ne 480 -or $mouseArtwork.GetPixel(0,0).A -ne 0 -or $mouseArtwork.GetPixel(160,240).A -lt 240) { throw 'Embedded mouse artwork dimensions/transparency did not restore.' }
} finally { $mouseArtwork.Dispose() }
$extract = $native.GetMethod('TryExtractEmbeddedRuntimeTo',$hidden)
[string]$runtimeDirectory = Join-Path $output ('runtime-'+[Guid]::NewGuid().ToString('N'))
[string]$runtime = $extract.Invoke($null,[object[]]@([string]$runtimeDirectory))
if (!$runtime -or !(Test-Path -LiteralPath $runtime)) { throw 'Compressed SDL3 cold-cache extraction failed.' }
$expectedHash = $native.GetField($(if($Architecture -eq 'x64'){'X64DllSha256'}else{'X86DllSha256'}),$hidden).GetRawConstantValue()
if ((Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Extracted runtime hash mismatch.' }
$written = (Get-Item -LiteralPath $runtime).LastWriteTimeUtc
if ($extract.Invoke($null,[object[]]@([string]$runtimeDirectory)) -ne $runtime -or (Get-Item -LiteralPath $runtime).LastWriteTimeUtc -ne $written) { throw 'Verified runtime was not reused.' }
[IO.File]::WriteAllBytes($runtime,[byte[]]@(9,9,9))
if ($extract.Invoke($null,[object[]]@([string]$runtimeDirectory)) -ne $runtime -or (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Corrupt runtime cache was not repaired.' }
$api = [Activator]::CreateInstance($native,[object[]]@([string]$runtime))
try { if (!$native.GetMethod('Initialize').Invoke($api,@())) { throw 'Extracted SDL3 runtime did not initialize.' } }
finally { $native.GetMethod('Dispose').Invoke($api,@()) | Out-Null }
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
$config.Timer.Enabled = $true
$config.Timer.TextTemplate = 'Timer {time}'
$config.Timer.FontSize = 32
$config.Timer.X = 31
$config.Timer.Y = 43
$timerPart = New-Object InputStitch.TimerPart
$timerPart.Kind = 'Minutes'
$timerPart.Style.FontSize = 31
$timerPart2 = New-Object InputStitch.TimerPart
$timerPart2.Kind = 'Milliseconds'
$timerPart2.Style.FontSize = 17
$timerPart2.Style.Color = '#22FFFF'
$timerPart2.Digits = 3
$config.Timer.SerializedParts = [InputStitch.TimerPart[]]@($timerPart,$timerPart2)
$config.NetworkControl.Enabled = $true
$blockKey = New-Object InputStitch.TriggerSpec
$blockKey.Kind = 'Keyboard'; $blockKey.VirtualKey = 120; $blockKey.Ctrl = $true; $blockKey.Alt = $true
$restoreKey = New-Object InputStitch.TriggerSpec
$restoreKey.Kind = 'Keyboard'; $restoreKey.VirtualKey = 121; $restoreKey.Ctrl = $true; $restoreKey.Alt = $true
$config.NetworkControl.BlockHotkey = $blockKey; $config.NetworkControl.RestoreHotkey = $restoreKey
$config.NetworkControl.BlockSound = $false; $config.NetworkControl.RestoreSound = $true
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
        $timerController = $form.GetType().GetField('timerController',$privateFlags).GetValue($form)
        if ($null -eq $timerController -or !$timerController.GetType().GetProperty('Visible',$privateFlags).GetValue($timerController)) { throw 'Saved timer did not restore at startup.' }
        if ($timerController.GetType().GetProperty('IsRunning',$privateFlags).GetValue($timerController)) { throw 'Timer must start paused at zero.' }
        $script:timerWindow = $timerController.GetType().GetProperty('Window',$privateFlags).GetValue($timerController)
        if ($null -eq $script:timerWindow -or !$script:timerWindow.GetType().GetProperty('Visible',$privateFlags).GetValue($script:timerWindow)) { throw 'Packaged timer window is not visible.' }
        if ($null -ne $form.GetType().GetField('networkController',$privateFlags).GetValue($form)) { throw 'Network controller must remain unused at startup.' }
        $restored = $form.GetType().GetField('config',$privateFlags).GetValue($form)
        if ($restored.Timer.Parts.Count -ne 2 -or $restored.Timer.Parts[0].Style.FontSize -ne 31 -or $restored.Timer.Parts[1].Style.FontSize -ne 17 -or $restored.Timer.Parts[1].Style.Color -ne '#22FFFF') { throw 'Packaged timer part styles did not restore.' }
        if ($restored.NetworkControl.BlockHotkey.VirtualKey -ne 120 -or $restored.NetworkControl.RestoreHotkey.VirtualKey -ne 121 -or $restored.NetworkControl.BlockSound -or -not $restored.NetworkControl.RestoreSound) { throw 'Packaged independent network controls did not restore.' }
        $bitmap = New-Object Drawing.Bitmap($form.Width,$form.Height)
        try { $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$bitmap.Width,$bitmap.Height))); $bitmap.Save((Join-Path $output 'startup.png')) } finally { $bitmap.Dispose() }
        [pscustomobject]@{ status='PASS'; version=$info.Version; architecture=$Architecture; seconds=2.5; packagedAssembly=$exe; configRoot=$output; mainWindow=$form.Text; timerRestored=$true; timerStartsPaused=$true; networkUnusedAtStartup=$true; crosshairRestored=$true; crosshairOffsetX=7; crosshairOffsetY=-9; compressedManagedDependencyLoaded=$true; compressedSdlColdExtraction=$true; verifiedCacheReused=$true; corruptedCacheRepaired=$true; actualSdlInitialized=$true } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'report.json') -Encoding UTF8
    } catch { $script:smokeFailure = $_ }
    finally { $form.Close() }
})
$timer.Start()
try { [Windows.Forms.Application]::Run($form) } finally { $timer.Dispose(); $form.Dispose() }
if ($script:smokeFailure) { throw $script:smokeFailure }
if ($script:crosshairWindow -and $script:crosshairWindow.Handle -ne [IntPtr]::Zero) { throw 'Crosshair native window survived application shutdown.' }
if ($script:timerWindow -and $script:timerWindow.Handle -ne [IntPtr]::Zero) { throw 'Timer native window survived application shutdown.' }
Write-Output ("PASS packaged {0} startup, live message loop and orderly shutdown; isolated configuration, disabled macros." -f $Architecture)
