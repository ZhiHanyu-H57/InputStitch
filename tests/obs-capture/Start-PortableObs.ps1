param([string]$ObsInstallPath,[ValidateRange(1024,65535)][int]$Port=4467)
$ErrorActionPreference='Stop'
$portable=Join-Path $PSScriptRoot 'portable'
$binary=Join-Path $portable 'bin\64bit\obs64.exe'
if(-not (Test-Path -LiteralPath $binary)){
 if(-not $ObsInstallPath){throw 'Supply -ObsInstallPath for an existing OBS Studio installation'}
 $source=(Resolve-Path -LiteralPath $ObsInstallPath).Path
 if(-not(Test-Path -LiteralPath (Join-Path $source 'bin\64bit\obs64.exe'))){throw 'OBS installation root must contain bin\64bit\obs64.exe'}
 & robocopy $source $portable /E /XF *.pdb /R:1 /W:1 /NFL /NDL /NJH /NJS
 if($LASTEXITCODE -ge 8){throw 'Portable OBS copy failed'}
}
& (Join-Path $PSScriptRoot 'Prepare-Portable.ps1') -Port $Port
$process=Start-Process -FilePath $binary -WorkingDirectory (Split-Path $binary -Parent) -ArgumentList @('--portable','--minimize-to-tray','--profile','Acceptance','--collection','Acceptance','--disable-updater') -WindowStyle Hidden -PassThru
$process.Id | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'detours-obs-pid.txt')
Start-Sleep -Seconds 5
Write-Output ('Isolated portable OBS started, PID '+$process.Id)
