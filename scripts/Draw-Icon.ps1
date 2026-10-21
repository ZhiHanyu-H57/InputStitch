# Rebuild the application icon using only Windows/.NET drawing APIs.
# DCET-inspired diamond: no lettering; pink-violet-cyan facets; transparent exterior.
[CmdletBinding()]
param([string]$Output)
$ErrorActionPreference = 'Stop'
if (-not $Output) { $Output = Join-Path (Split-Path $PSCommandPath -Parent | Split-Path -Parent) 'InputStitch.ico' }
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
public static class DiamondIcon
{
    public static byte[] Draw(int size)
    {
        using (Bitmap source = new Bitmap(size*4,size*4,PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(source))
        {
            g.SmoothingMode=SmoothingMode.AntiAlias; g.ScaleTransform(size*4/128F,size*4/128F);
            PointF[] outline = { new PointF(8,43),new PointF(34,17),new PointF(94,17),new PointF(120,43),new PointF(64,111) };
            using (LinearGradientBrush fill = new LinearGradientBrush(new PointF(8,20),new PointF(120,82),Color.FromArgb(255,49,181),Color.FromArgb(0,220,255)))
            {
                fill.InterpolationColors=new ColorBlend { Colors=new[]{Color.FromArgb(255,49,181),Color.FromArgb(157,76,255),Color.FromArgb(0,220,255)},Positions=new[]{0F,.52F,1F} };
                g.FillPolygon(fill,outline);
                using (Pen edge = new Pen(fill, size<=24 ? 5.4F : 4F)) { edge.LineJoin=LineJoin.Round;g.DrawPolygon(edge,outline); }
            }
            using (Brush highlight=new SolidBrush(Color.FromArgb(28,255,255,255)))
            {
                g.FillPolygon(highlight,new[]{new PointF(34,17),new PointF(64,17),new PointF(44,43)});
                g.FillPolygon(highlight,new[]{new PointF(64,17),new PointF(94,17),new PointF(84,43)});
            }
            using (LinearGradientBrush light = new LinearGradientBrush(new PointF(15,20),new PointF(114,65),Color.FromArgb(255,199,245),Color.FromArgb(163,251,255)))
            using (Pen line = new Pen(light, size<=24 ? 3.2F : 2.2F))
            {
                line.LineJoin=LineJoin.Round; g.DrawPolygon(line,outline);
                g.DrawLine(line,8,43,120,43);
                g.DrawLines(line,new[]{new PointF(34,17),new PointF(44,43),new PointF(64,111),new PointF(84,43),new PointF(94,17)});
                if(size>=24) g.DrawLines(line,new[]{new PointF(44,43),new PointF(64,17),new PointF(84,43)});
            }
            using (Bitmap result = new Bitmap(size,size,PixelFormat.Format32bppArgb))
            using (Graphics output = Graphics.FromImage(result))
            using (MemoryStream stream = new MemoryStream())
            {
                output.CompositingQuality=CompositingQuality.HighQuality;output.InterpolationMode=InterpolationMode.HighQualityBicubic;
                output.DrawImage(source,new Rectangle(0,0,size,size));result.Save(stream,ImageFormat.Png);return stream.ToArray();
            }
        }
    }
    public static void Save(string path)
    {
        int[] sizes={16,24,32,48,64,128,256}; byte[][] images=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++) images[i]=Draw(sizes[i]);
        using(BinaryWriter writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);
            int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++)
            {
                writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((ushort)0);
                writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;
            }
            foreach(byte[] image in images)writer.Write(image);
        }
    }
}
"@
[DiamondIcon]::Save([IO.Path]::GetFullPath($Output))
Write-Output "Diamond icon generated: $Output ($((Get-Item -LiteralPath $Output).Length) bytes)."
