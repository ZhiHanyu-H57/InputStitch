param([ValidateRange(1024,65535)][int]$Port=4467)
$ErrorActionPreference='Stop'
$base=$PSScriptRoot
$portable=Join-Path $base 'portable'
$config=Join-Path $portable 'config\obs-studio'
New-Item -ItemType Directory -Force -Path $config,(Join-Path $config 'basic\profiles\Acceptance'),(Join-Path $config 'basic\scenes'),(Join-Path $config 'plugin_config\obs-websocket'),(Join-Path $base 'recordings'),(Join-Path $base 'evidence') | Out-Null
[IO.File]::WriteAllText((Join-Path $portable 'portable_mode'),'')
$global=@'
[General]
FirstRun=false
LastVersion=536870912
LicenseAccepted=true
EnableAutoUpdates=false

[Basic]
Profile=Acceptance
ProfileDir=Acceptance
SceneCollection=Acceptance
SceneCollectionFile=Acceptance

[BasicWindow]
WarnBeforeStartingStream=false
WarnBeforeStoppingStream=false
WarnBeforeStoppingRecord=false
ShowWelcomeDialog=false
'@
[IO.File]::WriteAllText((Join-Path $config 'global.ini'),$global)
$recordings=(Join-Path $base 'recordings').Replace('\','/')
$profile=@"
[General]
Name=Acceptance

[Video]
BaseCX=800
BaseCY=400
OutputCX=800
OutputCY=400
FPSType=0
FPSCommon=30
ScaleType=bicubic
ColorFormat=NV12
ColorSpace=709
ColorRange=Partial

[Output]
Mode=Simple
FilenameFormatting=Acceptance_%CCYY-%MM-%DD_%hh-%mm-%ss

[SimpleOutput]
FilePath=$recordings
RecFormat2=mkv
RecEncoder=x264
RecQuality=Small
VBitrate=8000
ABitrate=128
UseAdvanced=false

[Audio]
SampleRate=48000
ChannelSetup=Stereo
"@
[IO.File]::WriteAllText((Join-Path $config 'basic\profiles\Acceptance\basic.ini'),$profile)
$password=[Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')
@{first_load=$false;server_enabled=$true;server_port=$Port;alerts_enabled=$false;auth_required=$true;server_password=$password} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $config 'plugin_config\obs-websocket\config.json') -Encoding UTF8
$userConfig=Join-Path $env:APPDATA 'obs-studio'
$snapshot=@(if(Test-Path -LiteralPath $userConfig){Get-ChildItem -LiteralPath $userConfig -Recurse -File | Where-Object {$_.Extension -in @('.ini','.json')} | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}})
$snapshot | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $base 'evidence\user-config-before.json') -Encoding UTF8
Write-Output ('Portable configuration prepared; existing user configuration files: '+$snapshot.Count)
