[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BaselineExe, [Parameter(Mandatory=$true)][string]$CurrentExe, [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing,System.Windows.Forms
[Windows.Forms.Application]::EnableVisualStyles()
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$hidden=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$static=[Reflection.BindingFlags]'Static,NonPublic,Public'
function Sample-Assembly([string]$file,[string]$label) {
    $assembly=[Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $file))
    $assembly.GetType('InputStitch.EmbeddedDependencyLoader').GetMethod('Register').Invoke($null,@()) | Out-Null
    $rows=New-Object 'System.Collections.Generic.List[object]'
    $metrics=New-Object 'System.Collections.Generic.List[object]'
    foreach($case in @('legacy','parts','styled-multiline','negative-spacing')) {
        $options=$assembly.CreateInstance('InputStitch.TimerOptions')
        if($case -in @('styled-multiline','negative-spacing')) {
            $options.FontSize=96; $options.Bold=$true; $options.Italic=$true; $options.OutlineWidth=6; $options.LetterSpacing=2
            $options.TextTemplate="计时 Timer {time}`n阶段 Stage 2"; $options.LineSpacing=$(if($case -eq 'negative-spacing'){-8}else{12})
        }
        if($case -ne 'legacy') {
            $options.Parts=$assembly.GetType('InputStitch.TimerText').GetMethod('CreateParts',$static).Invoke($null,@($options))
            foreach($part in $options.Parts) { if($part.Kind.ToString() -eq 'Milliseconds') {$part.Style.FontSize=36; $part.Style.Color='#22FFAA';$part.Style.Opacity=65} }
        }
        $type=$assembly.GetType('InputStitch.TimerFrameCache')
        $ctor=@($type.GetConstructors($hidden) | Where-Object {$_.GetParameters().Count -eq 1})[0]
        $cache=$ctor.Invoke(@($options));$render=$type.GetMethod('Render',$hidden,$null,[Type[]]@([TimeSpan]),$null)
        try {
            foreach($ms in @(0,12345,12346,72346,3661123,604800123,12345)) {
                $bitmap=$render.Invoke($cache,@([TimeSpan]::FromMilliseconds($ms)))
                try {
                    $bits=$bitmap.LockBits((New-Object Drawing.Rectangle(0,0,$bitmap.Width,$bitmap.Height)),[Drawing.Imaging.ImageLockMode]::ReadOnly,[Drawing.Imaging.PixelFormat]::Format32bppPArgb)
                    try {$bytes=New-Object byte[] ([Math]::Abs($bits.Stride)*$bitmap.Height);[Runtime.InteropServices.Marshal]::Copy($bits.Scan0,$bytes,0,$bytes.Length)} finally {$bitmap.UnlockBits($bits)}
                    $sha=[Security.Cryptography.SHA256]::Create()
                    try {$hash=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','')} finally {$sha.Dispose()}
                    $rows.Add([pscustomobject]@{case=$case;ms=$ms;width=$bitmap.Width;height=$bitmap.Height;sha256=$hash})
                } finally {$bitmap.Dispose()}
            }
            $samples=@(foreach($repeat in 1..3) {
                $watch=[Diagnostics.Stopwatch]::StartNew()
                foreach($i in 0..199) {$bitmap=$render.Invoke($cache,@([TimeSpan]::FromMilliseconds(12345+$i*50)));$bitmap.Dispose()}
                $watch.Elapsed.TotalMilliseconds/200
            })
            $metrics.Add([pscustomobject]@{case=$case;framesPerSample=200;samples=3;medianMsPerFrame=(@($samples|Sort-Object)[1])})
        } finally {$cache.Dispose()}
        if($case -eq 'styled-multiline') {
            $dialogType=$assembly.GetType('InputStitch.TimerSettingsDialog');$ctor=@($dialogType.GetConstructors($hidden)|Where-Object {$_.GetParameters().Count -eq 3})[0]
            $dialog=$ctor.Invoke(@($options,$null,$null))
            try {
                $paint=$dialogType.GetMethod('PaintPreview',$hidden);$bitmap=New-Object Drawing.Bitmap(800,180);$graphics=[Drawing.Graphics]::FromImage($bitmap)
                try {
                    $argsForPaint=[object[]]@($null,[Windows.Forms.PaintEventArgs]::new($graphics,[Drawing.Rectangle]::new(0,0,800,180)))
                    foreach($i in 1..3) {$paint.Invoke($dialog,$argsForPaint)|Out-Null}
                    $watch=[Diagnostics.Stopwatch]::StartNew();foreach($i in 1..50) {$paint.Invoke($dialog,$argsForPaint)|Out-Null}
                    $metrics.Add([pscustomobject]@{case='unchanged-preview';framesPerSample=50;samples=1;medianMsPerFrame=$watch.Elapsed.TotalMilliseconds/50})
                } finally {$graphics.Dispose();$bitmap.Dispose()}
            } finally {$dialog.Dispose()}
        }
    }
    [pscustomobject]@{label=$label;sha256=(Get-FileHash -LiteralPath $file).Hash;frames=@($rows.ToArray());metrics=@($metrics.ToArray())}
}
$baseline=Sample-Assembly $BaselineExe 'baseline';$current=Sample-Assembly $CurrentExe 'current'
if((ConvertTo-Json -Depth 6 -Compress $baseline.frames) -cne (ConvertTo-Json -Depth 6 -Compress $current.frames)) {throw 'Timer pixels changed relative to the baseline.'}
[pscustomobject]@{pass=$true;matchingFrames=$current.frames.Count;baseline=$baseline;current=$current} | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $OutputDirectory 'timer-render-comparison.json')
Write-Output ('PASS timer render parity: '+$current.frames.Count+' frames; elapsed changes, rollover, mixed alpha, multiline, negative line spacing and rewind.')
$current.metrics|Format-Table -AutoSize
