[CmdletBinding()]
param([string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$fixtures=Join-Path $PSScriptRoot 'isolated-network'
if(-not $OutputDirectory) { $OutputDirectory=Join-Path $project 'artifacts/isolated-network' }
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not (Test-Path -LiteralPath $compiler)) { $compiler=$compiler.Replace('Framework64','Framework') }
$probe=Join-Path $OutputDirectory 'GuestNetworkProbeA.exe'
& $compiler /nologo /warnaserror+ /target:exe /platform:x64 ("/out:$probe") (Join-Path $fixtures 'GuestNetworkProbe.cs')
if($LASTEXITCODE -ne 0) { throw 'Isolated network probe compilation failed.' }
Copy-Item -LiteralPath $probe -Destination (Join-Path $OutputDirectory 'GuestNetworkProbeB.exe') -Force
$hostPath=Join-Path $OutputDirectory 'GuestNetworkAcceptance.exe'
$sources=@('NetworkControl.cs','NetworkPrivilegeHelper.cs') | ForEach-Object { Join-Path $project $_ }
$sources+=@('GuestNetworkGuard.cs','GuestNetworkModelStubs.cs','GuestNetworkAcceptance.cs') | ForEach-Object { Join-Path $fixtures $_ }
& $compiler /nologo /warnaserror+ /target:exe /platform:x64 /main:InputStitch.GuestNetworkAcceptance /r:System.Core.dll /r:System.Xml.dll /r:System.Management.dll ("/out:$hostPath") $sources
if($LASTEXITCODE -ne 0) { throw 'Isolated network acceptance compilation failed.' }
foreach($name in @('New-GuestNetworkPermission.ps1','Start-GuestNetworkAcceptance.ps1','echo_peer.py')) {
    Copy-Item -LiteralPath (Join-Path $fixtures $name) -Destination (Join-Path $OutputDirectory $name) -Force
}
Copy-Item -LiteralPath (Join-Path $project 'docs/ISOLATED_NETWORK_ACCEPTANCE.md') -Destination (Join-Path $OutputDirectory 'GUEST-RUN.md') -Force
& $hostPath --self-test | Tee-Object -FilePath (Join-Path $OutputDirectory 'guard-self-test.log')
if($LASTEXITCODE -ne 0) { throw 'Isolated guest identity self-test failed.' }
$hashes=@(foreach($path in $sources) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash+'  '+[IO.Path]::GetFileName($path) })
$hashes | Set-Content -Encoding ASCII -LiteralPath (Join-Path $OutputDirectory 'source-hashes.txt')
Write-Output ('Built guest-only acceptance: '+$OutputDirectory)
Write-Output 'No network operation executed. Transfer this folder to the dedicated isolated Windows guest; see docs/ISOLATED_NETWORK_ACCEPTANCE.md.'
