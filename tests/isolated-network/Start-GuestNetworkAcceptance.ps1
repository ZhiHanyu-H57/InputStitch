[CmdletBinding()]
param(
    [string]$Token,
    [string]$PeerIPv4,
    [ValidateRange(1,65535)][int]$Tcp4Port = 31941,
    [ValidateRange(1,65535)][int]$Udp4Port = 31942,
    [string]$PeerIPv6,
    [ValidateRange(1,65535)][int]$Tcp6Port = 31943,
    [ValidateRange(1,65535)][int]$Udp6Port = 31944,
    [switch]$Run,
    [switch]$PrecompileFixture
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $Run) { Write-Output 'No action. Use -Run only inside the marked isolated Windows guest. See GUEST-RUN.md.'; return }
if ([string]::IsNullOrWhiteSpace($Token) -or [string]::IsNullOrWhiteSpace($PeerIPv4) -or [string]::IsNullOrWhiteSpace($PeerIPv6)) { throw 'Token and both external peer addresses are required.' }
$hostPath = Join-Path $PSScriptRoot 'GuestNetworkAcceptance.exe'
if ($PrecompileFixture) {
    $assembly = [Reflection.Assembly]::LoadFrom($hostPath)
    $guard = $assembly.GetType('InputStitch.GuestNetworkGuard', $true)
    try { $guard.GetMethod('Demand', [Reflection.BindingFlags]'Static,NonPublic').Invoke($null, @($Token, $false)) | Out-Null }
    catch { throw ('Refusing guest precompilation: ' + $_.Exception.GetBaseException().Message) }
    $ngen = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/ngen.exe'
    & $ngen install $hostPath /NoDependencies
    if ($LASTEXITCODE -ne 0) { throw 'Guest fixture native-image generation failed.' }
}
& $hostPath --run $Token $PeerIPv4 $Tcp4Port $Udp4Port $PeerIPv6 $Tcp6Port $Udp6Port
if ($LASTEXITCODE -ne 0) { throw "Guest whole-computer acceptance refused/failed (exit $LASTEXITCODE). Read guest-network-evidence if the test started." }
