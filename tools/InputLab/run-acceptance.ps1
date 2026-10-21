[CmdletBinding()]
param(
    [string]$ReportPath = '',
    [switch]$AllowRealOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $AllowRealOutput) {
    throw 'Input Lab ownership acceptance intentionally emits real SendInput/virtual-controller output. Re-run with -AllowRealOutput only when the desktop is idle and no other InputStitch instance is active.'
}

$toolRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $toolRoot -Parent) -Parent
$outputDir = Join-Path $projectRoot 'artifacts\InputLab'
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $outputDir 'acceptance-report.txt'
} elseif (-not [System.IO.Path]::IsPathRooted($ReportPath)) {
    $ReportPath = Join-Path $projectRoot $ReportPath
}
$ReportPath = [System.IO.Path]::GetFullPath($ReportPath)

# Versioned portable EXEs are product instances too. Do not emit input into a
# desktop where another copy may consume it or start the user's configured macros.
$runningProduct = @(Get-Process | Where-Object { $_.ProcessName -eq 'InputStitch' -or $_.ProcessName -match '^InputStitch-\d' })
if ($runningProduct.Count -ne 0) {
    [IO.Directory]::CreateDirectory((Split-Path $ReportPath -Parent)) | Out-Null
    $reason = 'Another InputStitch instance is active: ' + (($runningProduct | Select-Object -ExpandProperty ProcessName) -join ', ')
    [IO.File]::WriteAllText($ReportPath, "BLOCKED | environment-preflight | $reason`r`nSUMMARY: BLOCKED | checks=0 | failures=0`r`n", (New-Object Text.UTF8Encoding($false)))
    Write-Output "BLOCKED | environment-preflight | $reason"
    Write-Output 'SUMMARY: BLOCKED | checks=0 | failures=0'
    exit 2
}

$exe = & (Join-Path $toolRoot 'build-acceptance.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Ownership acceptance build failed.' }

& $exe --report $ReportPath
$exitCode = $LASTEXITCODE
if (Test-Path -LiteralPath $ReportPath) {
    Get-Content -LiteralPath $ReportPath | Select-String -Pattern '^SUMMARY:' | ForEach-Object { $_.Line }
}
if ($exitCode -eq 2) {
    [Console]::Error.WriteLine("Input Lab ownership acceptance is BLOCKED by the local ViGEm/XInput environment. This is not an InputStitch product failure. Report: $ReportPath")
    exit 2
}
if ($exitCode -ne 0) {
    throw "Input Lab ownership acceptance failed with exit code $exitCode. Report: $ReportPath"
}
Write-Output "Acceptance report: $ReportPath"
