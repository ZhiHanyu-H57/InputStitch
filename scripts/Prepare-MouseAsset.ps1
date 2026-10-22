Add-Type -AssemblyName System.Drawing
$root=Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
$source=[Drawing.Bitmap]::FromFile((Join-Path $root 'mouse-selector.png'))
$color=New-Object Drawing.Bitmap($source.Width,$source.Height,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
$alpha=New-Object Drawing.Bitmap($source.Width,$source.Height,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g=[Drawing.Graphics]::FromImage($color)
try {
 $g.Clear([Drawing.Color]::Black); $g.DrawImage($source,(New-Object Drawing.Rectangle(0,0,$source.Width,$source.Height)),0,0,$source.Width,$source.Height,[Drawing.GraphicsUnit]::Pixel)
 for($y=0;$y -lt $source.Height;$y++) { for($x=0;$x -lt $source.Width;$x++) { $a=$source.GetPixel($x,$y).A; $alpha.SetPixel($x,$y,[Drawing.Color]::FromArgb($a,$a,$a)) } }
 $codec=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
 $enc=New-Object Drawing.Imaging.EncoderParameters(1)
 $enc.Param[0]=New-Object Drawing.Imaging.EncoderParameter([Drawing.Imaging.Encoder]::Quality,[long]84)
 try { $color.Save((Join-Path $root 'mouse-color.jpg'),$codec,$enc) } finally { $enc.Dispose() }
 $alpha.Save((Join-Path $root 'mouse-alpha.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose(); $source.Dispose(); $color.Dispose(); $alpha.Dispose() }
Get-ChildItem $root -File | Select-Object Name,Length
