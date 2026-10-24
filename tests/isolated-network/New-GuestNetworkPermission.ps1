[CmdletBinding()]
param([string]$Token, [ValidateRange(1,1440)][int]$ValidMinutes = 120, [ValidateRange(30,120)][int]$MaximumBlockSeconds = 90, [switch]$ConfirmIsolatedGuest)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $ConfirmIsolatedGuest) { throw 'Explicit -ConfirmIsolatedGuest is required; this script refuses physical hosts.' }
$hostPath = Join-Path $PSScriptRoot 'GuestNetworkAcceptance.exe'
$assembly = [Reflection.Assembly]::LoadFrom($hostPath)
$guard = $assembly.GetType('InputStitch.GuestNetworkGuard', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
try {
    $identity = $guard.GetMethod('ReadMachine', $flags).Invoke($null, @())
    $guard.GetMethod('ValidateMachine', $flags).Invoke($null, @($identity)) | Out-Null
} catch { throw ('Refusing permission creation: ' + $_.Exception.GetBaseException().Message) }
if ([string]::IsNullOrEmpty($Token)) {
    $bytes = [byte[]]::new(32)
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    $Token = ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}
if ($Token -cnotmatch '^[0-9a-f]{64}$') { throw 'Token must contain exactly 64 lowercase hexadecimal characters.' }
$permission = [Activator]::CreateInstance($assembly.GetType('InputStitch.GuestNetworkPermission', $true))
$instanceFlags = [Reflection.BindingFlags]'Instance,NonPublic'
$permission.ComputerName = $identity.GetType().GetField('ComputerName', $instanceFlags).GetValue($identity)
$permission.GuestUuid = $identity.GetType().GetField('Uuid', $instanceFlags).GetValue($identity)
$permission.Token = $Token
$permission.ExpiresUtc = [DateTime]::UtcNow.AddMinutes($ValidMinutes)
$permission.MaximumBlockSeconds = $MaximumBlockSeconds
$serializer = [Xml.Serialization.XmlSerializer]::new($permission.GetType())
$marker = Join-Path $PSScriptRoot 'ALLOW-GUEST-NETWORK-ACCEPTANCE.xml'
$stream = [IO.File]::Create($marker)
try { $serializer.Serialize($stream, $permission) } finally { $stream.Dispose() }
[pscustomobject]@{ Permission = $marker; ComputerName = $permission.ComputerName; GuestUuid = $permission.GuestUuid; ExpiresUtc = $permission.ExpiresUtc; Token = $Token; MaximumBlockSeconds = $MaximumBlockSeconds }
