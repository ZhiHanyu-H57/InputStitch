$ErrorActionPreference='Stop'
$idFile=Join-Path $PSScriptRoot 'detours-obs-pid.txt'
if(Test-Path -LiteralPath $idFile){
 $obsId=[int](Get-Content -LiteralPath $idFile)
 $process=Get-Process -Id $obsId -ErrorAction SilentlyContinue
 if($process){
  $expected=Join-Path $PSScriptRoot 'portable\bin\64bit\obs64.exe'
  if(-not [string]::Equals($process.Path,$expected,[StringComparison]::OrdinalIgnoreCase)){throw 'Recorded PID is not this portable OBS instance'}
  Add-Type @"
using System;using System.Runtime.InteropServices;
public static class InputStitchObsClose {
 private delegate bool Callback(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Callback cb,IntPtr l);
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 public static void Close(uint pid){EnumWindows(delegate(IntPtr h,IntPtr l){uint p;GetWindowThreadProcessId(h,out p);if(p==pid)PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);return true;},IntPtr.Zero);}
}
"@
  [InputStitchObsClose]::Close([uint32]$obsId)
  $process | Wait-Process -Timeout 10
 }
}
$configFile=Join-Path $PSScriptRoot 'portable\config\obs-studio\plugin_config\obs-websocket\config.json'
if(Test-Path -LiteralPath $configFile){$config=Get-Content -Raw -LiteralPath $configFile | ConvertFrom-Json;$config.server_enabled=$false;$config.server_password='';$config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configFile -Encoding UTF8}
Write-Output 'Portable OBS stopped; generated websocket credential cleared'
