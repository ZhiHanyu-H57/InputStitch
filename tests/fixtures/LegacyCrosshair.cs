using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace InputStitch
{
    public enum LegacyCrosshairStyle { Dot, Square, Cross, CrossDot, Ring, RingDot, TShape, Diagonal }

    [Serializable]
    public sealed class LegacyCrosshairOptions
    {
        public bool Enabled;
        public bool Visible = true;
        public LegacyCrosshairStyle Style = LegacyCrosshairStyle.CrossDot;
        public int Size = 24;
        public int LineWidth = 2;
        public int Gap = 4;
        public int DotSize = 4;
        public bool Outline = true;
        public int OutlineWidth = 1;
        public string Color = "#00FF65";
        public string OutlineColor = "#000000";
        public int Opacity = 100;
        // Empty follows the primary display. Device name survives monitor reordering.
        public string MonitorDevice = "";
        public int OffsetX;
        public int OffsetY;
        [XmlElement(IsNullable = true)]
        public TriggerSpec Hotkey = new TriggerSpec { Ctrl = true, Alt = true, VirtualKey = (int)Keys.F10 };

        public LegacyCrosshairOptions Clone()
        {
            LegacyCrosshairOptions copy = (LegacyCrosshairOptions)MemberwiseClone();
            copy.Hotkey = Hotkey == null ? null : Hotkey.Clone();
            return copy;
        }

        internal static int Limit(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        internal static System.Drawing.Color ParseColor(string value, string fallback)
        {
            int rgb;
            if (value == null || value.Length != 7 || value[0] != '#' ||
                !int.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                rgb = int.Parse(fallback.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return System.Drawing.Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        }
        internal static string Hex(System.Drawing.Color value) { return "#" + (value.ToArgb() & 0xffffff).ToString("X6", CultureInfo.InvariantCulture); }
        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(LegacyCrosshairStyle), Style)) Style = LegacyCrosshairStyle.CrossDot;
            Size = Limit(Size, 1, 200); LineWidth = Limit(LineWidth, 1, 30);
            Gap = Limit(Gap, 0, 100); DotSize = Limit(DotSize, 1, 50);
            OutlineWidth = Limit(OutlineWidth, 1, 12); Opacity = Limit(Opacity, 0, 100);
            OffsetX = Limit(OffsetX, -16384, 16384); OffsetY = Limit(OffsetY, -16384, 16384);
            MonitorDevice = MonitorDevice ?? "";
            Color = Hex(ParseColor(Color, "#00FF65")); OutlineColor = Hex(ParseColor(OutlineColor, "#000000"));
            if (Hotkey != null && (Hotkey.Kind != InputKind.Keyboard || Hotkey.VirtualKey < 1 || Hotkey.VirtualKey > 254 ||
                ModifierSafetyPolicy.IsModifierVirtualKey(Hotkey.VirtualKey))) Hotkey = null;
        }
    }

    internal static class LegacyCrosshairRenderer
    {
        internal static Point Center(Rectangle screen, LegacyCrosshairOptions options)
        {
            return new Point(LegacyCrosshairOptions.Limit(screen.Left + screen.Width / 2 + options.OffsetX, screen.Left, screen.Right - 1),
                LegacyCrosshairOptions.Limit(screen.Top + screen.Height / 2 + options.OffsetY, screen.Top, screen.Bottom - 1));
        }

        internal static Bitmap Render(LegacyCrosshairOptions source)
        {
            LegacyCrosshairOptions o = source.Clone(); o.Normalize();
            int extent = Math.Max(o.Size, Math.Max(o.DotSize, o.LineWidth)) + o.OutlineWidth * 2 + 8;
            if (extent % 2 == 0) extent++;
            Bitmap bitmap = new Bitmap(extent, extent, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (GraphicsPath path = new GraphicsPath(FillMode.Winding))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float c = extent / 2, half = o.Size / 2F, width = Math.Min(o.LineWidth, o.Size);
                float gap = Math.Min(o.Gap / 2F, Math.Max(0, half - 1));
                if (o.Style == LegacyCrosshairStyle.Dot) path.AddEllipse(c - half, c - half, o.Size, o.Size);
                else if (o.Style == LegacyCrosshairStyle.Square) path.AddRectangle(new RectangleF(c - half, c - half, o.Size, o.Size));
                else if (o.Style == LegacyCrosshairStyle.Ring || o.Style == LegacyCrosshairStyle.RingDot)
                {
                    path.FillMode = FillMode.Alternate;
                    path.AddEllipse(c - half, c - half, o.Size, o.Size);
                    float inside = o.Size - width * 2;
                    if (inside > 0) path.AddEllipse(c - inside / 2, c - inside / 2, inside, inside);
                }
                else
                {
                    if (o.Style == LegacyCrosshairStyle.Diagonal) g.TranslateTransform(c, c);
                    if (o.Style == LegacyCrosshairStyle.Diagonal) { g.RotateTransform(45); g.TranslateTransform(-c, -c); }
                    path.AddRectangle(new RectangleF(c - half, c - width / 2, half - gap, width));
                    path.AddRectangle(new RectangleF(c + gap, c - width / 2, half - gap, width));
                    if (o.Style != LegacyCrosshairStyle.TShape) path.AddRectangle(new RectangleF(c - width / 2, c - half, width, half - gap));
                    path.AddRectangle(new RectangleF(c - width / 2, c + gap, width, half - gap));
                }
                if (o.Style == LegacyCrosshairStyle.CrossDot)
                    path.AddEllipse(c - o.DotSize / 2F, c - o.DotSize / 2F, o.DotSize, o.DotSize);
                Paint(g, path, o);
                if (o.Style == LegacyCrosshairStyle.RingDot)
                using (GraphicsPath center = new GraphicsPath())
                { center.AddEllipse(c - o.DotSize / 2F, c - o.DotSize / 2F, o.DotSize, o.DotSize); Paint(g, center, o); }
            }
            return bitmap;
        }
        private static void Paint(Graphics g, GraphicsPath path, LegacyCrosshairOptions o)
        {
            if (o.Outline)
            using (Pen border = new Pen(LegacyCrosshairOptions.ParseColor(o.OutlineColor, "#000000"), o.OutlineWidth * 2F))
            { border.LineJoin = LineJoin.Round; g.DrawPath(border, path); }
            using (Brush fill = new SolidBrush(LegacyCrosshairOptions.ParseColor(o.Color, "#00FF65"))) g.FillPath(fill, path);
        }
    }
}
