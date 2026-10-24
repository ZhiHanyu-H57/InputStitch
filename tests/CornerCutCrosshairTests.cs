using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using InputStitch;

internal static class CornerCutCrosshairTests
{
    private static int checks;
    private static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); checks++; }
    private static byte[] Pixels(Bitmap bitmap)
    {
        Rectangle bounds = new Rectangle(Point.Empty, bitmap.Size);
        BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        try { byte[] bytes = new byte[data.Stride * bitmap.Height]; Marshal.Copy(data.Scan0, bytes, 0, bytes.Length); return bytes; }
        finally { bitmap.UnlockBits(data); }
    }
    private static int Colored(Bitmap image)
    { byte[] pixels = Pixels(image); int count = 0; for (int i = 3; i < pixels.Length; i += 4) if (pixels[i] > 0) count++; return count; }
    private static int Main()
    {
        Directory.CreateDirectory(Root);
        try
        {
            LegacyPixels(); Geometry(); Bounds(); Persistence();
            string summary = "PASS corner-cut crosshair: " + checks + " checks; legacy pixel parity, corner geometry, parameter bounds and isolated XML compatibility.";
            File.WriteAllText(Path.Combine(Root, "summary.txt"), summary);
            Console.WriteLine(summary); return 0;
        }
        catch (Exception ex)
        { File.WriteAllText(Path.Combine(Root, "failure.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }
    private static void LegacyPixels()
    {
        string[] names = { "Dot", "Square", "Cross", "CrossDot", "Ring", "RingDot", "TShape", "Diagonal" };
        for (int index = 0; index < names.Length; index++)
        {
            Check(Enum.GetName(typeof(CrosshairStyle), index) == names[index], "legacy enum numeric identity: " + names[index]);
            foreach (int size in new[] { 1, 24, 61, 200 })
            foreach (int line in new[] { 1, 3, 30 })
            foreach (int gap in new[] { 0, 4, 100 })
            foreach (bool outline in new[] { false, true })
            {
                CrosshairOptions current = new CrosshairOptions { Style = (CrosshairStyle)index, Size = size, LineWidth = line, Gap = gap,
                    DotSize = 50, Outline = outline, OutlineWidth = 12, Color = "#F23C91", OutlineColor = "#168FCB" };
                LegacyCrosshairOptions previous = new LegacyCrosshairOptions { Style = (LegacyCrosshairStyle)index, Size = size, LineWidth = line, Gap = gap,
                    DotSize = 50, Outline = outline, OutlineWidth = 12, Color = "#F23C91", OutlineColor = "#168FCB" };
                using (Bitmap actual = CrosshairRenderer.Render(current))
                using (Bitmap expected = LegacyCrosshairRenderer.Render(previous))
                    Check(actual.Size == expected.Size && Pixels(actual).SequenceEqual(Pixels(expected)), "legacy pixels: " + names[index] + " size=" + size + " line=" + line + " gap=" + gap + " outline=" + outline);
            }
        }
        Check((int)CrosshairStyle.CornerCutSquare == 8, "new style appended without moving old values");
    }
    private static void Geometry()
    {
        foreach (bool outline in new[] { false, true })
        {
            CrosshairOptions options = new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 4, Gap = 10,
                Outline = outline, OutlineWidth = 2, Color = "#FF0000", OutlineColor = "#0000FF" };
            using (Bitmap image = CrosshairRenderer.Render(options))
            {
                int c = image.Width / 2, edge = c - 20;
                Check(image.GetPixel(c, c).A == 0, "hollow center " + outline);
                Check(image.GetPixel(c, edge + 2).R > 240 && image.GetPixel(c, edge + 2).B < 10, "top side fill " + outline);
                Check(image.GetPixel(c, c + 18).R > 240 && image.GetPixel(c, c + 18).B < 10, "bottom side fill " + outline);
                Check(image.GetPixel(edge + 2, c).R > 240 && image.GetPixel(edge + 2, c).B < 10, "left side fill " + outline);
                Check(image.GetPixel(c + 18, c).R > 240 && image.GetPixel(c + 18, c).B < 10, "right side fill " + outline);
                foreach (int x in new[] { edge, c + 19 }) foreach (int y in new[] { edge, c + 19 })
                    Check(image.GetPixel(x, y).A == 0, "open corner " + x + "," + y + " outline=" + outline);
                int blue = 0; byte[] pixels = Pixels(image);
                for (int i = 0; i < pixels.Length; i += 4) if (pixels[i] > pixels[i + 2] && pixels[i + 3] > 0) blue++;
                Check(outline ? blue > 0 : blue == 0, "outline toggle and color " + outline);
                image.Save(Path.Combine(Root, outline ? "corner-cut-outlined.png" : "corner-cut.png"));
                if (!outline) Check(Components(image) == 4, "four disconnected side segments");
            }
        }
        using (Bitmap closed = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, Gap = 0, Outline = false }))
        {
            int c = closed.Width / 2;
            Check(closed.GetPixel(c - 20, c - 20).A > 0, "zero corner gap closes the square ring");
            Check(closed.GetPixel(c, c).A == 0, "zero gap retains hollow center");
        }
        using (Bitmap smallGap = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 4, Gap = 5, Outline = false }))
        using (Bitmap largerGap = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 4, Gap = 10, Outline = false }))
            Check(Colored(largerGap) < Colored(smallGap), "gap increases corner opening");
        using (Bitmap thin = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 2, Gap = 10, Outline = false }))
        using (Bitmap thick = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 5, Gap = 10, Outline = false }))
            Check(Colored(thick) > Colored(thin), "line width increases side thickness");
        using (Bitmap image = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = 40, LineWidth = 2, Gap = 100, Outline = false }))
            Check(Colored(image) > 0 && Components(image) == 4, "oversized gap retains four visible line-width segments");
    }
    private static int Components(Bitmap image)
    {
        bool[,] visited = new bool[image.Width, image.Height]; int components = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
        {
            if (visited[x, y] || image.GetPixel(x, y).A < 128) continue;
            components++; Queue<Point> queue = new Queue<Point>(); queue.Enqueue(new Point(x, y)); visited[x, y] = true;
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                foreach (Point n in new[] { new Point(p.X - 1, p.Y), new Point(p.X + 1, p.Y), new Point(p.X, p.Y - 1), new Point(p.X, p.Y + 1) })
                    if (n.X >= 0 && n.Y >= 0 && n.X < image.Width && n.Y < image.Height && !visited[n.X, n.Y] && image.GetPixel(n.X, n.Y).A >= 128)
                    { visited[n.X, n.Y] = true; queue.Enqueue(n); }
            }
        }
        return components;
    }
    private static void Bounds()
    {
        foreach (int size in new[] { 1, 2, 3, 4, 8, 24, 200 })
        foreach (int line in new[] { 1, 2, 30 })
        foreach (int gap in new[] { 0, 1, 4, 100 })
        foreach (int border in new[] { 1, 12 })
        foreach (bool outline in new[] { false, true })
        using (Bitmap image = CrosshairRenderer.Render(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare, Size = size,
            LineWidth = line, Gap = gap, Outline = outline, OutlineWidth = border }))
        {
            Check(Colored(image) > 0, "all allowed parameter extremes remain visible");
            Check(image.Width <= 233 && image.GetPixel(0, 0).A == 0 && image.GetPixel(image.Width - 1, image.Height - 1).A == 0, "outline contained in alpha canvas");
        }
    }
    private static void Persistence()
    {
        string file = Path.Combine(Root, "config.xml");
        File.WriteAllText(file, "<MacroConfig><Language>en-US</Language></MacroConfig>");
        MacroConfig old = ConfigPackageSerializer.DeserializeConfigFromFile(file);
        Check(old.Crosshair.Style == CrosshairStyle.CrossDot && !old.Crosshair.Enabled, "config predating crosshair retains defaults");
        for (int i = 0; i < 8; i++)
        {
            string style = ((CrosshairStyle)i).ToString();
            File.WriteAllText(file, "<MacroConfig><Crosshair><Style>" + style + "</Style></Crosshair></MacroConfig>");
            Check(ConfigPackageSerializer.DeserializeConfigFromFile(file).Crosshair.Style == (CrosshairStyle)i, "old XML style still loads: " + style);
        }
        CrosshairOptions expected = new CrosshairOptions { Enabled = true, Visible = false, Style = CrosshairStyle.CornerCutSquare,
            Size = 61, LineWidth = 3, Gap = 17, DotSize = 5, Outline = true, OutlineWidth = 4, Color = "#EF7512", OutlineColor = "#152637",
            Opacity = 67, OffsetX = -12, OffsetY = 31, MonitorDevice = "TEST-DISPLAY", Hotkey = new TriggerSpec { Ctrl = true, Alt = true, VirtualKey = 120 } };
        new ConfigStore().Save(file, new MacroConfig { Crosshair = expected });
        CrosshairOptions actual = ConfigPackageSerializer.DeserializeConfigFromFile(file).Crosshair;
        Check(actual.Style == CrosshairStyle.CornerCutSquare && actual.Size == 61 && actual.LineWidth == 3 && actual.Gap == 17, "new shape and geometry survive restart");
        Check(actual.Outline && actual.OutlineWidth == 4 && actual.Color == "#EF7512" && actual.OutlineColor == "#152637" && actual.Opacity == 67, "colors outline and opacity survive restart");
        Check(actual.Enabled && !actual.Visible && actual.OffsetX == -12 && actual.OffsetY == 31 && actual.MonitorDevice == "TEST-DISPLAY", "visibility monitor and precise position survive restart");
        Check(actual.Hotkey.Ctrl && actual.Hotkey.Alt && actual.Hotkey.VirtualKey == 120, "hotkey survives restart");
        Check(File.ReadAllText(file).Contains("<StyleVariant>CornerCutSquare</StyleVariant>"), "new style persists in optional compatible variant");
        using (StringWriter writer = new StringWriter())
        {
            new XmlSerializer(typeof(CrosshairOptions)).Serialize(writer, actual);
            using (StringReader reader = new StringReader(writer.ToString()))
                Check(((LegacyCrosshairOptions)new XmlSerializer(typeof(LegacyCrosshairOptions), new XmlRootAttribute("CrosshairOptions")).Deserialize(reader)).Style == LegacyCrosshairStyle.Ring, "old reader opens new corner-cut settings using ring fallback");
        }
        Check(CrosshairRenderer.Center(new Rectangle(-1920, -100, 1920, 1080), actual) == new Point(-972, 471), "new style retains precise display-center placement");
        actual.Style = CrosshairStyle.Dot;
        using (StringWriter writer = new StringWriter())
        {
            new XmlSerializer(typeof(CrosshairOptions)).Serialize(writer, actual);
            Check(!writer.ToString().Contains("<StyleVariant>"), "switching away from new style clears its optional extension");
        }
    }
}
