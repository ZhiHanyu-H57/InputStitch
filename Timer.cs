using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace InputStitch
{
    public enum TimerBackend { Desktop, Rtss }
    public enum TimerTimeFormat { MinutesSecondsMilliseconds, HoursMinutesSecondsMilliseconds, SecondsMilliseconds }
    public enum TimerPartKind { Hours, Minutes, Seconds, Milliseconds, Text }

    [Serializable]
    public sealed class TimerPartStyle
    {
        public string FontFamily = "Microsoft YaHei UI";
        public float FontSize = 28;
        public bool Bold, Italic;
        public bool Outline = true;
        public float OutlineWidth = 2, LetterSpacing;
        public string Color = "#FFFFFF", OutlineColor = "#000000";
        public int Opacity = 100;
        public TimerPartStyle Clone() { return (TimerPartStyle)MemberwiseClone(); }
        internal TimerOptions ToOptions()
        { return new TimerOptions { FontFamily = FontFamily, FontSize = FontSize, Bold = Bold, Italic = Italic, Outline = Outline, OutlineWidth = OutlineWidth, LetterSpacing = LetterSpacing, Color = Color, OutlineColor = OutlineColor, Opacity = Opacity }; }
        public static TimerPartStyle FromOptions(TimerOptions value)
        {
            TimerOptions o = value ?? new TimerOptions();
            return new TimerPartStyle { FontFamily = o.FontFamily, FontSize = o.FontSize, Bold = o.Bold, Italic = o.Italic, Outline = o.Outline, OutlineWidth = o.OutlineWidth, LetterSpacing = o.LetterSpacing, Color = o.Color, OutlineColor = o.OutlineColor, Opacity = o.Opacity };
        }
        public void Normalize()
        {
            TimerOptions o = ToOptions(); o.Normalize();
            FontFamily = o.FontFamily; FontSize = o.FontSize; Bold = o.Bold; Italic = o.Italic; Outline = o.Outline; OutlineWidth = o.OutlineWidth; LetterSpacing = o.LetterSpacing; Color = o.Color; OutlineColor = o.OutlineColor; Opacity = o.Opacity;
        }
        internal bool Same(TimerPartStyle other) { return other != null && ToOptions().SameRendering(other.ToOptions()); }
    }

    [Serializable]
    public sealed class TimerPart
    {
        public TimerPartKind Kind;
        public bool Visible = true;
        public string Text = "";
        public int Digits = 2;
        public TimerPartStyle Style = new TimerPartStyle();
        public TimerPart Clone() { TimerPart p = (TimerPart)MemberwiseClone(); p.Style = (Style ?? new TimerPartStyle()).Clone(); return p; }
        public void Normalize()
        {
            if (!Enum.IsDefined(typeof(TimerPartKind), Kind)) Kind = TimerPartKind.Text;
            Text = (Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n'); if (Text.Length > 512) Text = Text.Substring(0, 512);
            Digits = CrosshairOptions.Limit(Digits, 1, Kind == TimerPartKind.Milliseconds ? 3 : 6);
            if (Style == null) Style = new TimerPartStyle(); Style.Normalize();
        }
    }

    [Serializable]
    public sealed class TimerOptions
    {
        public bool Enabled;
        public bool Visible = true;
        public TimerBackend Backend;
        public string FontFamily = "Microsoft YaHei UI";
        public float FontSize = 28;
        public bool Bold;
        public bool Italic;
        public bool Outline = true;
        public float OutlineWidth = 2;
        public float LetterSpacing;
        public string Color = "#FFFFFF";
        public string OutlineColor = "#000000";
        public int Opacity = 100;
        public string MonitorDevice = "";
        public int X = 24;
        public int Y = 24;
        public string TextTemplate = "{time}";
        public TimerTimeFormat TimeFormat;
        [XmlElement(IsNullable = true)] public TriggerSpec StartPauseHotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec ResetHotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec VisibilityHotkey;
        [XmlIgnore] public List<TimerPart> Parts;
        [XmlArray("Parts", IsNullable = false), XmlArrayItem("Part")]
        public TimerPart[] SerializedParts
        {
            get { return Parts == null ? null : Parts.ToArray(); }
            set { Parts = value == null ? null : new List<TimerPart>(value); }
        }
        public bool ShouldSerializeSerializedParts() { return Parts != null; }

        public TimerOptions Clone()
        {
            TimerOptions copy = (TimerOptions)MemberwiseClone();
            copy.StartPauseHotkey = StartPauseHotkey == null ? null : StartPauseHotkey.Clone();
            copy.ResetHotkey = ResetHotkey == null ? null : ResetHotkey.Clone();
            copy.VisibilityHotkey = VisibilityHotkey == null ? null : VisibilityHotkey.Clone();
            if (Parts != null) { copy.Parts = new List<TimerPart>(); foreach (TimerPart p in Parts) if (p != null) copy.Parts.Add(p.Clone()); }
            return copy;
        }
        internal bool SameRendering(TimerOptions other)
        {
            return other != null && FontFamily == other.FontFamily && FontSize == other.FontSize && Bold == other.Bold && Italic == other.Italic &&
                Outline == other.Outline && OutlineWidth == other.OutlineWidth && LetterSpacing == other.LetterSpacing && Color == other.Color &&
                OutlineColor == other.OutlineColor && Opacity == other.Opacity && TextTemplate == other.TextTemplate && TimeFormat == other.TimeFormat && SameParts(Parts, other.Parts);
        }
        private static bool SameParts(List<TimerPart> a, List<TimerPart> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) { TimerPart x = a[i], y = b[i]; if (x == null || y == null) { if (x != y) return false; continue; } if (x.Kind != y.Kind || x.Visible != y.Visible || x.Text != y.Text || x.Digits != y.Digits || !x.Style.Same(y.Style)) return false; }
            return true;
        }
        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(TimerBackend), Backend)) Backend = TimerBackend.Desktop;
            if (!Enum.IsDefined(typeof(TimerTimeFormat), TimeFormat)) TimeFormat = TimerTimeFormat.MinutesSecondsMilliseconds;
            FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "Microsoft YaHei UI" : FontFamily;
            if (FontFamily.Length > 128) FontFamily = FontFamily.Substring(0, 128);
            FontSize = Limit(FontSize, 8, 160, 28);
            OutlineWidth = Limit(OutlineWidth, 0, 12, 2);
            LetterSpacing = Limit(LetterSpacing, -4, 40, 0);
            Opacity = CrosshairOptions.Limit(Opacity, 0, 100);
            X = CrosshairOptions.Limit(X, -16384, 16384); Y = CrosshairOptions.Limit(Y, -16384, 16384);
            MonitorDevice = MonitorDevice ?? "";
            TextTemplate = (TextTemplate ?? "{time}").Replace("\r\n", "\n").Replace('\r', '\n');
            if (TextTemplate.Length > 512) TextTemplate = TextTemplate.Substring(0, 512);
            Color = CrosshairOptions.Hex(CrosshairOptions.ParseColor(Color, "#FFFFFF"));
            OutlineColor = CrosshairOptions.Hex(CrosshairOptions.ParseColor(OutlineColor, "#000000"));
            if (!ValidHotkey(StartPauseHotkey)) StartPauseHotkey = null;
            if (!ValidHotkey(ResetHotkey)) ResetHotkey = null;
            if (!ValidHotkey(VisibilityHotkey)) VisibilityHotkey = null;
            if (Parts != null)
            {
                List<TimerPart> normalized = new List<TimerPart>(); int remaining = 512;
                foreach (TimerPart part in Parts)
                {
                    if (part == null) continue; if (normalized.Count == 32) break; part.Normalize();
                    if (part.Text.Length > remaining) part.Text = part.Text.Substring(0, remaining); remaining -= part.Text.Length; normalized.Add(part);
                }
                Parts = normalized;
            }
        }
        private static float Limit(float value, float min, float max, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value)); }
        private static bool ValidHotkey(TriggerSpec key)
        { return key == null || (key.Kind == InputKind.Keyboard && key.VirtualKey > 0 && key.VirtualKey < 255 && !ModifierSafetyPolicy.IsModifierVirtualKey(key.VirtualKey)); }
    }

    internal interface ITimerClock { long Timestamp { get; } long Frequency { get; } }
    internal sealed class TimerStopwatchClock : ITimerClock
    {
        public long Timestamp { get { return Stopwatch.GetTimestamp(); } }
        public long Frequency { get { return Stopwatch.Frequency; } }
    }

    // Elapsed time comes from the monotonic clock, never from display ticks.
    internal sealed class TimerEngine
    {
        private readonly ITimerClock clock;
        private readonly object gate = new object();
        private long accumulated;
        private long started;
        private bool running;
        internal TimerEngine() : this(new TimerStopwatchClock()) { }
        internal TimerEngine(ITimerClock source)
        { if (source == null || source.Frequency <= 0) throw new ArgumentException("A monotonic clock is required."); clock = source; }
        internal bool IsRunning { get { lock (gate) return running; } }
        internal TimeSpan Elapsed
        {
            get
            {
                lock (gate)
                {
                    long delta = running ? Math.Max(0, clock.Timestamp - started) : 0;
                    double ticks = ((double)accumulated + delta) * TimeSpan.TicksPerSecond / clock.Frequency;
                    return ticks >= TimeSpan.MaxValue.Ticks ? TimeSpan.MaxValue : TimeSpan.FromTicks((long)ticks);
                }
            }
        }
        internal void Start() { lock (gate) { if (running) return; started = clock.Timestamp; running = true; } }
        internal void Pause()
        { lock (gate) { if (!running) return; accumulated += Math.Max(0, clock.Timestamp - started); running = false; } }
        internal void Reset() { lock (gate) { accumulated = 0; started = 0; running = false; } }
    }

    internal static class TimerText
    {
        internal static string Format(TimeSpan elapsed, TimerOptions options)
        {
            if (options.Parts != null) { StringBuilder joined = new StringBuilder(); foreach (TimerPart part in options.Parts) if (part != null && part.Visible) joined.Append(FormatPart(elapsed, part, options.Parts)); return joined.ToString(); }
            long milliseconds = Math.Max(0, elapsed.Ticks / TimeSpan.TicksPerMillisecond);
            long totalSeconds = milliseconds / 1000, totalMinutes = totalSeconds / 60;
            string hours = (totalSeconds / 3600).ToString("00", CultureInfo.InvariantCulture);
            string minutes = (totalMinutes % 60).ToString("00", CultureInfo.InvariantCulture);
            string seconds = (totalSeconds % 60).ToString("00", CultureInfo.InvariantCulture);
            string fraction = (milliseconds % 1000).ToString("000", CultureInfo.InvariantCulture);
            string time = options.TimeFormat == TimerTimeFormat.HoursMinutesSecondsMilliseconds ? hours + ":" + minutes + ":" + seconds + "." + fraction :
                options.TimeFormat == TimerTimeFormat.SecondsMilliseconds ? totalSeconds.ToString(CultureInfo.InvariantCulture) + "." + fraction :
                totalMinutes.ToString("00", CultureInfo.InvariantCulture) + ":" + seconds + "." + fraction;
            string template = options.TextTemplate ?? "{time}";
            StringBuilder result = new StringBuilder(template.Length + 16);
            for (int i = 0; i < template.Length; i++)
            {
                if (i + 1 < template.Length && template[i] == '{' && template[i + 1] == '{') { result.Append('{'); i++; continue; }
                if (i + 1 < template.Length && template[i] == '}' && template[i + 1] == '}') { result.Append('}'); i++; continue; }
                if (template[i] == '{')
                {
                    int end = template.IndexOf('}', i + 1);
                    if (end >= 0)
                    {
                        string token = template.Substring(i + 1, end - i - 1), value = null;
                        if (token == "time") value = time;
                        else if (token == "hh") value = hours;
                        else if (token == "mm") value = minutes;
                        else if (token == "ss") value = seconds;
                        else if (token == "fff") value = fraction;
                        if (value != null) { result.Append(value); i = end; continue; }
                    }
                }
                result.Append(template[i]);
            }
            return result.ToString();
        }
        internal static string FormatPart(TimeSpan elapsed, TimerPart part, List<TimerPart> all)
        {
            if (part.Kind == TimerPartKind.Text) return part.Text ?? "";
            long ms = Math.Max(0, elapsed.Ticks / TimeSpan.TicksPerMillisecond), seconds = ms / 1000;
            bool hours = false, minutes = false;
            foreach (TimerPart p in all) if (p != null && p.Visible) { hours |= p.Kind == TimerPartKind.Hours; minutes |= p.Kind == TimerPartKind.Minutes; }
            long value = part.Kind == TimerPartKind.Hours ? seconds / 3600 : part.Kind == TimerPartKind.Minutes ? (hours ? seconds / 60 % 60 : seconds / 60) : part.Kind == TimerPartKind.Seconds ? (hours || minutes ? seconds % 60 : seconds) : ms % 1000;
            int digits = Math.Max(1, Math.Min(part.Kind == TimerPartKind.Milliseconds ? 3 : 6, part.Digits));
            if (part.Kind == TimerPartKind.Milliseconds) { if (digits == 1) value /= 100; else if (digits == 2) value /= 10; }
            return value.ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }
        internal static List<TimerPart> CreatePreset(TimerTimeFormat format, TimerOptions styleSource)
        {
            List<TimerPart> parts = new List<TimerPart>(); TimerPartStyle style = TimerPartStyle.FromOptions(styleSource); style.Opacity = 100;
            if (format == TimerTimeFormat.HoursMinutesSecondsMilliseconds) { Add(parts, TimerPartKind.Hours, "", 2, style); Add(parts, TimerPartKind.Text, ":", 2, style); }
            if (format != TimerTimeFormat.SecondsMilliseconds) { Add(parts, TimerPartKind.Minutes, "", 2, style); Add(parts, TimerPartKind.Text, ":", 2, style); }
            Add(parts, TimerPartKind.Seconds, "", format == TimerTimeFormat.SecondsMilliseconds ? 1 : 2, style); Add(parts, TimerPartKind.Text, ".", 2, style); Add(parts, TimerPartKind.Milliseconds, "", 3, style); return parts;
        }
        private static void Add(List<TimerPart> parts, TimerPartKind kind, string text, int digits, TimerPartStyle style)
        { parts.Add(new TimerPart { Kind = kind, Text = text, Digits = digits, Style = style.Clone() }); }
        internal static List<TimerPart> CreateParts(TimerOptions legacy)
        {
            TimerOptions o = legacy ?? new TimerOptions(); List<TimerPart> parts = new List<TimerPart>();
            if (o.Parts != null) { foreach (TimerPart part in o.Parts) if (part != null) parts.Add(part.Clone()); return parts; }
            TimerPartStyle style = TimerPartStyle.FromOptions(o); style.Opacity = 100; StringBuilder literal = new StringBuilder(); string template = o.TextTemplate ?? "{time}";
            for (int i = 0; i < template.Length; i++)
            {
                if (i + 1 < template.Length && template[i] == '{' && template[i + 1] == '{') { literal.Append('{'); i++; continue; }
                if (i + 1 < template.Length && template[i] == '}' && template[i + 1] == '}') { literal.Append('}'); i++; continue; }
                int end = template[i] == '{' ? template.IndexOf('}', i + 1) : -1;
                string token = end >= 0 ? template.Substring(i + 1, end - i - 1) : "";
                if (token == "time" || token == "hh" || token == "mm" || token == "ss" || token == "fff")
                {
                    if (literal.Length > 0) { Add(parts, TimerPartKind.Text, literal.ToString(), 2, style); literal.Length = 0; }
                    if (token == "time") parts.AddRange(CreatePreset(o.TimeFormat, o));
                    else Add(parts, token == "hh" ? TimerPartKind.Hours : token == "mm" ? TimerPartKind.Minutes : token == "ss" ? TimerPartKind.Seconds : TimerPartKind.Milliseconds, "", token == "fff" ? 3 : 2, style);
                    i = end;
                }
                else literal.Append(template[i]);
            }
            if (literal.Length > 0) Add(parts, TimerPartKind.Text, literal.ToString(), 2, style); return parts;
        }
    }

    internal static class TimerTextRenderer
    {
        internal static Bitmap Render(TimerOptions options, TimeSpan elapsed)
        { if (options.Parts == null) return TimerRenderer.Render(options, TimerText.Format(elapsed, options)); using (TimerPartsFrameCache cache = new TimerPartsFrameCache(options)) return cache.Render(elapsed); }
    }

    internal static class TimerRenderer
    {
        internal static Bitmap Render(TimerOptions source, string text)
        {
            TimerOptions o = source.Clone(); o.Normalize();
            FontStyle style = (o.Bold ? FontStyle.Bold : FontStyle.Regular) | (o.Italic ? FontStyle.Italic : FontStyle.Regular);
            using (Font font = CreateFont(o.FontFamily, o.FontSize, style))
            using (StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
            using (Bitmap probe = new Bitmap(1, 1))
            using (Graphics measure = Graphics.FromImage(probe))
            {
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;
                string[] lines = (text ?? "").Split('\n');
                int lineCount = Math.Min(8, lines.Length);
                float lineHeight = font.GetHeight(96);
                float width = 1;
                for (int i = 0; i < lineCount; i++) width = Math.Max(width, MeasureLine(measure, lines[i], font, format, o.LetterSpacing));
                int padding = (int)Math.Ceiling(o.Outline ? o.OutlineWidth : 0) + (int)Math.Ceiling(o.FontSize * 0.25f) + 2;
                int bitmapWidth = Math.Min(4096, Math.Max(1, (int)Math.Ceiling(width) + padding * 2));
                int bitmapHeight = Math.Min(2048, Math.Max(1, (int)Math.Ceiling(lineHeight * lineCount) + padding * 2));
                Bitmap mask = new Bitmap(bitmapWidth, bitmapHeight, PixelFormat.Format32bppPArgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(mask))
                    {
                        graphics.Clear(System.Drawing.Color.Transparent);
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        using (Brush white = new SolidBrush(System.Drawing.Color.White))
                        for (int i = 0; i < lineCount; i++) DrawLine(graphics, lines[i], font, format, white, padding, padding + i * lineHeight, o.LetterSpacing);
                    }
                    PaintMask(mask, o);
                    return mask;
                }
                catch { mask.Dispose(); throw; }
            }
        }
        internal static Font CreateFont(string family, float size, FontStyle style)
        {
            try { return new Font(family, size, style, GraphicsUnit.Pixel); }
            catch (ArgumentException) { return new Font(System.Drawing.FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel); }
        }
        internal static float MeasureLine(Graphics g, string text, Font font, StringFormat format, float spacing)
        {
            if (spacing == 0) return g.MeasureString(text, font, int.MaxValue, format).Width;
            float width = 0; TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text); int count = 0;
            while (elements.MoveNext()) { width += g.MeasureString(elements.GetTextElement(), font, int.MaxValue, format).Width + spacing; count++; }
            return Math.Max(1, width - (count > 0 ? spacing : 0));
        }
        private static void DrawLine(Graphics g, string text, Font font, StringFormat format, Brush brush, float x, float y, float spacing)
        {
            if (spacing == 0) { g.DrawString(text, font, brush, new PointF(x, y), format); return; }
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement(); g.DrawString(element, font, brush, new PointF(x, y), format);
                x += g.MeasureString(element, font, int.MaxValue, format).Width + spacing;
            }
        }
        // The alpha-mask path keeps Windows font fallback for Chinese and supplementary glyphs.
        // A separable maximum filter builds the outline in linear time, independent of its radius.
        private static void PaintMask(Bitmap bitmap, TimerOptions o)
        {
            int width = bitmap.Width, height = bitmap.Height;
            BitmapData data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            try
            {
                byte[] pixels = new byte[data.Stride * height]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                byte[] alpha = new byte[width * height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) alpha[y * width + x] = pixels[y * data.Stride + x * 4 + 3];
                int radius = o.Outline ? (int)Math.Ceiling(o.OutlineWidth) : 0;
                byte[] border = radius == 0 ? alpha : ExpandAlpha(alpha, width, height, radius);
                System.Drawing.Color fill = CrosshairOptions.ParseColor(o.Color, "#FFFFFF"), stroke = CrosshairOptions.ParseColor(o.OutlineColor, "#000000");
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    int index = y * width + x, offset = y * data.Stride + x * 4;
                    int a = alpha[index], b = radius == 0 ? 0 : border[index] * (255 - a) / 255, total = a + b;
                    pixels[offset] = (byte)((fill.B * a + stroke.B * b) / 255);
                    pixels[offset + 1] = (byte)((fill.G * a + stroke.G * b) / 255);
                    pixels[offset + 2] = (byte)((fill.R * a + stroke.R * b) / 255);
                    pixels[offset + 3] = (byte)total;
                }
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally { bitmap.UnlockBits(data); }
        }
        private static byte[] ExpandAlpha(byte[] source, int width, int height, int radius)
        {
            byte[] horizontal = new byte[source.Length], result = new byte[source.Length];
            int[] deque = new int[Math.Max(width, height)];
            for (int y = 0; y < height; y++)
            {
                int head = 0, tail = 0, next = 0;
                for (int x = 0; x < width; x++)
                {
                    while (next <= x + radius && next < width) { while (tail > head && source[y * width + deque[tail - 1]] <= source[y * width + next]) tail--; deque[tail++] = next++; }
                    while (tail > head && deque[head] < x - radius) head++;
                    horizontal[y * width + x] = source[y * width + deque[head]];
                }
            }
            for (int x = 0; x < width; x++)
            {
                int head = 0, tail = 0, next = 0;
                for (int y = 0; y < height; y++)
                {
                    while (next <= y + radius && next < height) { while (tail > head && horizontal[deque[tail - 1] * width + x] <= horizontal[next * width + x]) tail--; deque[tail++] = next++; }
                    while (tail > head && deque[head] < y - radius) head++;
                    result[y * width + x] = horizontal[deque[head] * width + x];
                }
            }
            return result;
        }
    }

    // Cache the static words and individual changing digits. Large fonts and outlines are
    // rasterized once per style; animation only composites these small immutable surfaces.
    internal sealed class TimerFrameCache : IDisposable
    {
        private sealed class Part { internal Bitmap Image; internal float Advance; }
        private readonly Dictionary<string, Part> parts = new Dictionary<string, Part>(StringComparer.Ordinal);
        private readonly TimerOptions options;
        private readonly Font font;
        private readonly Bitmap probe = new Bitmap(1, 1);
        private readonly Graphics measure;
        private readonly StringFormat format;
        private readonly int padding;
        private readonly float lineHeight;
        private readonly TimerPartsFrameCache compound;
        private readonly bool perGlyph;
        internal int CachedParts { get { return compound == null ? parts.Count : compound.CachedParts; } }
        internal TimerFrameCache(TimerOptions value) : this(value, false) { }
        internal TimerFrameCache(TimerOptions value, bool glyphLayout)
        {
            perGlyph = glyphLayout;
            options = value.Clone(); options.Normalize();
            if (options.Parts != null) { compound = new TimerPartsFrameCache(options); return; }
            FontStyle style = (options.Bold ? FontStyle.Bold : FontStyle.Regular) | (options.Italic ? FontStyle.Italic : FontStyle.Regular);
            font = TimerRenderer.CreateFont(options.FontFamily, options.FontSize, style);
            measure = Graphics.FromImage(probe); format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;
            lineHeight = font.GetHeight(96); padding = (int)Math.Ceiling(options.Outline ? options.OutlineWidth : 0) + (int)Math.Ceiling(options.FontSize * .25f) + 2;
        }
        private Part Get(string text)
        {
            Part part;
            if (!parts.TryGetValue(text, out part))
            {
                part = new Part { Image = TimerRenderer.Render(options, text), Advance = TimerRenderer.MeasureLine(measure, text, font, format, options.LetterSpacing) };
                parts.Add(text, part);
            }
            return part;
        }
        private List<Part> Line(string text)
        {
            List<Part> line = new List<Part>(); int start = 0;
            if (perGlyph) { TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text); while (elements.MoveNext()) line.Add(Get(elements.GetTextElement())); return line; }
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9') continue;
                if (i > start) line.Add(Get(text.Substring(start, i - start)));
                line.Add(Get(text.Substring(i, 1))); start = i + 1;
            }
            if (start < text.Length) line.Add(Get(text.Substring(start)));
            return line;
        }
        internal Bitmap Render(string text)
        {
            string[] lines = (text ?? "").Split('\n'); int count = Math.Min(8, lines.Length); float width = 1;
            List<Part>[] layout = new List<Part>[count];
            for (int i = 0; i < count; i++)
            {
                layout[i] = Line(lines[i]); float lineWidth = 0;
                foreach (Part part in layout[i]) lineWidth += part.Advance + options.LetterSpacing;
                if (layout[i].Count > 0) lineWidth -= options.LetterSpacing;
                width = Math.Max(width, lineWidth);
            }
            Bitmap bitmap = new Bitmap(Math.Min(4096, Math.Max(1, (int)Math.Ceiling(width) + padding * 2)),
                Math.Min(2048, Math.Max(1, (int)Math.Ceiling(lineHeight * count) + padding * 2)), PixelFormat.Format32bppPArgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                    for (int i = 0; i < count; i++)
                    {
                        float x = 0;
                        foreach (Part part in layout[i])
                        { graphics.DrawImageUnscaled(part.Image, (int)Math.Round(x), (int)Math.Round(i * lineHeight)); x += part.Advance + options.LetterSpacing; }
                    }
                }
                return bitmap;
            }
            catch { bitmap.Dispose(); throw; }
        }
        internal Bitmap Render(TimeSpan elapsed) { return compound == null ? Render(TimerText.Format(elapsed, options)) : compound.Render(elapsed); }
        internal float Measure(string text)
        { float width = 0; List<Part> line = Line(text); foreach (Part p in line) width += p.Advance + options.LetterSpacing; return Math.Max(0, width - (line.Count > 0 ? options.LetterSpacing : 0)); }
        public void Dispose()
        { if (compound != null) compound.Dispose(); foreach (Part part in parts.Values) part.Image.Dispose(); parts.Clear(); if (format != null) format.Dispose(); if (measure != null) measure.Dispose(); probe.Dispose(); if (font != null) font.Dispose(); }
    }

    internal sealed class TimerPartMetrics
    {
        internal readonly TimerOptions Style;
        internal readonly int Padding;
        internal readonly float Baseline, Height;
        internal TimerPartMetrics(TimerPart part)
        {
            Style = part.Style.ToOptions(); Style.Normalize();
            Padding = (int)Math.Ceiling(Style.Outline ? Style.OutlineWidth : 0) + (int)Math.Ceiling(Style.FontSize * .25f) + 2;
            FontStyle flags = (Style.Bold ? FontStyle.Bold : FontStyle.Regular) | (Style.Italic ? FontStyle.Italic : FontStyle.Regular);
            using (Font font = TimerRenderer.CreateFont(Style.FontFamily, Style.FontSize, flags))
            {
                FontStyle actual = font.Style;
                Baseline = Padding + font.Size * font.FontFamily.GetCellAscent(actual) / font.FontFamily.GetEmHeight(actual);
                Height = font.GetHeight(96) + Padding * 2;
            }
        }
    }

    // Each changing number reuses its digit surfaces. One short-lived composition surface
    // per visible part avoids rerasterizing fonts and outlines on animation ticks.
    internal sealed class TimerPartsFrameCache : IDisposable
    {
        private sealed class Entry : IDisposable
        {
            internal TimerPart Part; internal TimerPartMetrics Metrics; internal TimerFrameCache Cache; internal ImageAttributes Alpha; internal bool OwnCache;
            public void Dispose() { if (OwnCache) Cache.Dispose(); if (Alpha != null) Alpha.Dispose(); }
        }
        private sealed class Sprite { internal Bitmap Image; internal Entry Entry; internal float X, Y; }
        private readonly TimerOptions options;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly int padding;
        private readonly float baseline, lineHeight;
        internal int CachedParts { get { int count = 0; foreach (Entry e in entries) if (e.OwnCache) count += e.Cache.CachedParts; return count; } }
        internal TimerPartsFrameCache(TimerOptions source)
        {
            options = source.Clone(); options.Normalize();
            try
            {
                foreach (TimerPart part in options.Parts)
                {
                    if (!part.Visible) continue; TimerPartMetrics metrics = new TimerPartMetrics(part);
                    Entry e = new Entry { Part = part, Metrics = metrics };
                    foreach (Entry prior in entries) if (prior.Part.Style.Same(part.Style)) { e.Cache = prior.Cache; break; }
                    if (e.Cache == null) { e.Cache = new TimerFrameCache(metrics.Style, true); e.OwnCache = true; } entries.Add(e);
                    if (part.Style.Opacity < 100) { e.Alpha = new ImageAttributes(); ColorMatrix matrix = new ColorMatrix(); matrix.Matrix33 = part.Style.Opacity / 100f; e.Alpha.SetColorMatrix(matrix); }
                    padding = Math.Max(padding, metrics.Padding); baseline = Math.Max(baseline, metrics.Baseline);
                }
                foreach (Entry e in entries) lineHeight = Math.Max(lineHeight, baseline - e.Metrics.Baseline + e.Metrics.Height);
            }
            catch { Dispose(); throw; }
        }
        internal Bitmap Render(TimeSpan elapsed)
        {
            List<Sprite> sprites = new List<Sprite>(); float x = 0, width = 1; int line = 0;
            try
            {
                foreach (Entry e in entries)
                {
                    string[] lines = TimerText.FormatPart(elapsed, e.Part, options.Parts).Split('\n');
                    for (int n = 0; n < lines.Length && line < 8; n++)
                    {
                        if (n > 0) { width = Math.Max(width, x); x = 0; line++; if (line == 8) break; }
                        if (lines[n].Length == 0 || x >= 4096 || line * lineHeight >= 2048) continue;
                        sprites.Add(new Sprite { Image = e.Cache.Render(lines[n]), Entry = e, X = x + padding - e.Metrics.Padding, Y = line * lineHeight + baseline - e.Metrics.Baseline });
                        x += e.Cache.Measure(lines[n]) + e.Part.Style.LetterSpacing; width = Math.Max(width, x);
                    }
                }
                Bitmap result = new Bitmap(Math.Min(4096, Math.Max(1, (int)Math.Ceiling(width) + padding * 2)), Math.Min(2048, Math.Max(1, (int)Math.Ceiling(lineHeight * Math.Min(8, line + 1)))), PixelFormat.Format32bppPArgb);
                try
                {
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        g.Clear(System.Drawing.Color.Transparent);
                        foreach (Sprite s in sprites)
                        {
                            int left = (int)Math.Round(s.X), top = (int)Math.Round(s.Y);
                            if (s.Entry.Alpha == null) g.DrawImageUnscaled(s.Image, left, top);
                            else g.DrawImage(s.Image, new Rectangle(left, top, s.Image.Width, s.Image.Height), 0, 0, s.Image.Width, s.Image.Height, GraphicsUnit.Pixel, s.Entry.Alpha);
                        }
                    }
                    return result;
                }
                catch { result.Dispose(); throw; }
            }
            finally { foreach (Sprite s in sprites) s.Image.Dispose(); }
        }
        public void Dispose() { foreach (Entry e in entries) e.Dispose(); entries.Clear(); }
    }
}
