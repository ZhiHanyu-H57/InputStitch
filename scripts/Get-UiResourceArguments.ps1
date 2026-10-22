param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
foreach ($resource in @('mouse-color.jpg','mouse-alpha.png')) {
    $path = Join-Path $ProjectRoot ('assets/' + $resource)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing UI resource: $resource" }
    '/resource:' + $path + ',InputStitch.Assets.' + $resource
}
