[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Path,
    [ValidateSet('x64','x86')][string]$Architecture='x64',
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$ScriptArguments)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$output=Join-Path $project 'artifacts/private-ui-host'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$windowsPowerShell=Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe'
$automation=(& $windowsPowerShell -NoProfile -Command '[System.Management.Automation.PSObject].Assembly.Location').Trim()
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$hostExe=Join-Path $output ('UiScriptHost-'+$Architecture+'.exe')
& $compiler /nologo /target:exe /warnaserror+ /reference:System.Windows.Forms.dll ('/platform:'+$Architecture) ('/out:'+$hostExe) ('/reference:'+$automation) (Join-Path $PSScriptRoot 'UiScriptHost.cs') (Join-Path $PSScriptRoot 'IsolatedUiDesktop.cs')
if($LASTEXITCODE -ne 0){throw 'Isolated UI script host compilation failed.'}
& $hostExe ([IO.Path]::GetFullPath($Path)) @ScriptArguments
if($LASTEXITCODE -ne 0){throw 'Isolated UI script verification failed.'}
