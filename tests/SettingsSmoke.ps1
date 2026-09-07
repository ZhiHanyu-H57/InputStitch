param()
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$info = & (Join-Path $project 'scripts/Get-ReleaseInfo.ps1')
$Exe = Join-Path $project ('dist/InputStitch-' + $info.Version + '-Windows-x64.exe')
$renderDirectory = Join-Path $project 'artifacts/tests'
New-Item -ItemType Directory -Path $renderDirectory -Force | Out-Null
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$null = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes((Join-Path $PSScriptRoot '../third-party/Nefarius.ViGEm.Client/Nefarius.ViGEm.Client.dll')))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $Exe))
function Descendants($c) { foreach($child in $c.Controls) { $child; Descendants $child } }
$serializer = New-Object System.Xml.Serialization.XmlSerializer([InputStitch.MacroConfig])
$old = $serializer.Deserialize((New-Object IO.StringReader('<MacroConfig><Language>en-US</Language></MacroConfig>')))
if($old.IdleGamepad.Enabled -or $old.IdleGamepad.IdleSeconds -ne 120 -or $old.IdleGamepad.HoldMilliseconds -ne 150 -or $old.IdleGamepad.Pulse.GamepadControl.ToString() -ne 'LeftStick' -or $old.IdleGamepad.Pulse.GamepadY -ne -100) { throw 'Legacy config must default to idle OFF / 120s / left-stick-down / 150ms' }
foreach($language in @('zh-CN','en-US')) {
  [InputStitch.Localizer]::SetLanguage($language)
  foreach($size in @(@(700,660),@(604,441))) {
    $config = New-Object InputStitch.MacroConfig
    $config.Language=$language
    $form = New-Object InputStitch.SettingsDialog($config)
    try {
      $form.ClientSize = New-Object Drawing.Size($size[0],$size[1])
      # Enable rendering visibility only; do not Show a desktop window.
      $setState = [Windows.Forms.Control].GetMethod('SetState',[Reflection.BindingFlags]'Instance,NonPublic')
      $setState.Invoke($form,@(2,$true)) | Out-Null
      $form.CreateControl()
      $tabs = @(Descendants $form | Where-Object {$_ -is [Windows.Forms.TabControl]})[0]
      if($tabs.TabCount -ne 3) { throw 'Expected three settings tabs' }
      for($index=0;$index -lt 3;$index++) {
        $tabs.SelectedIndex=$index
        $form.PerformLayout()
        $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
        try { $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$bitmap.Width,$bitmap.Height))); $bitmap.Save((Join-Path $renderDirectory ('settings-{0}-{1}-{2}.png' -f $language,$size[0],$index))) } finally { $bitmap.Dispose() }
      }
      $idle = $form.SelectedIdleOptions
      if($idle.Enabled) { throw 'Settings unexpectedly enable idle output' }
      if(-not $idle.TargetScopeInitialized -or $idle.TargetProcessName -or $idle.TargetWindowTitle -or $idle.TargetWindowClass) { throw 'Idle target defaults must be independently initialized and empty' }
      $private=[Reflection.BindingFlags]'Instance,NonPublic'
      $idlePanel=$form.GetType().GetField('idleSettings',$private).GetValue($form)
      $idleTargetButton=$idlePanel.GetType().GetField('lockTargetButton',$private).GetValue($idlePanel)
      if($null -eq $idleTargetButton) { throw 'Missing independent idle target picker' }
      if($size[0] -eq 604) {
        $longTarget=New-Object InputStitch.IdleGamepadOptions
        $longTarget.TargetScopeInitialized=$true
        $longTarget.TargetProcessName='GTA5_Enhanced_With_A_Long_Process_Name'
        $longTarget.TargetWindowTitle='Grand Theft Auto V - Deliberately Long Window Title For Narrow Layout Verification'
        $idlePanel.LoadOptions($longTarget)
        $tabs.SelectedIndex=2
        $form.PerformLayout(); $idlePanel.PerformLayout()
        $targetLabel=$idlePanel.GetType().GetField('targetBox',$private).GetValue($idlePanel)
        $clearTargetButton=$idlePanel.GetType().GetField('clearTargetButton',$private).GetValue($idlePanel)
        $targetTip=$idlePanel.GetType().GetField('targetToolTip',$private).GetValue($idlePanel)
        if(-not $targetLabel.AutoEllipsis) { throw 'Idle target display must ellipsize long text' }
        if($targetLabel.Width -le 20) { throw 'Idle target display lost all usable width in narrow layout' }
        if($targetLabel.Right -gt $idleTargetButton.Left) { throw 'Idle target display overlaps recent-window button' }
        if($idleTargetButton.Right -gt $clearTargetButton.Left) { throw 'Idle target buttons overlap in narrow layout' }
        if($idleTargetButton.Width -lt $idleTargetButton.GetPreferredSize([Drawing.Size]::Empty).Width) { throw 'Recent-window button is clipped in narrow layout' }
        if($clearTargetButton.Width -lt $clearTargetButton.GetPreferredSize([Drawing.Size]::Empty).Width) { throw 'Clear button is clipped in narrow layout' }
        if($targetTip.GetToolTip($targetLabel) -ne $targetLabel.Text) { throw 'Idle target tooltip must preserve full target text' }
      }
      $buttons = @(Descendants $form | Where-Object {$_ -is [Windows.Forms.Button] -and $_.DialogResult -eq 'OK'})
      if($buttons.Count -ne 1) { throw 'Missing OK action' }
      Write-Output ('PASS settings {0} {1}x{2}; 3 tabs, defaults, footer' -f $language,$form.ClientSize.Width,$form.ClientSize.Height)
    } finally { $form.Dispose() }
  }
}
Write-Output 'PASS old XML configuration defaults; settings rendered without showing windows or injecting input.'
$editorType=$assembly.GetType('InputStitch.StepEditDialog')
$private=[Reflection.BindingFlags]'Instance,NonPublic'
foreach($pair in @(@(100,100),@(37,-82),@(0,80))) {
  $step=New-Object InputStitch.MacroStep
  $step.Kind=[InputStitch.InputKind]::Gamepad
  $step.GamepadControl=[InputStitch.GamepadControl]::LeftStick
  $step.GamepadX=$pair[0]; $step.GamepadY=$pair[1]
  $editor=$editorType.GetConstructors()[0].Invoke([object[]]@($null,$step.PSObject.BaseObject))
  try {
    $input=$editorType.GetField('selectedInput',$private).GetValue($editor)
    if($input.GamepadX -ne $pair[0] -or $input.GamepadY -ne $pair[1]) { throw 'Opening the editor changed stored X/Y' }
    $editorType.GetField('stickAngleBox',$private).GetValue($editor).Value=0
    $editorType.GetField('stickStrengthBox',$private).GetValue($editor).Value=80
    if($input.GamepadX -ne 0 -or $input.GamepadY -ne 80) { throw 'Direction 0 / strength 80 did not produce forward 80' }
  } finally { $editor.Dispose() }
}
Write-Output 'PASS saved gamepad vector initialization and live direction/strength editing; no real output.'
