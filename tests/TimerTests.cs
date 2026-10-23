using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using InputStitch;

internal static class TimerTests
{
    private static int passed;
    private static string output;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    private sealed class FakeClock : ITimerClock { internal long Now; public long Timestamp { get { return Now; } } public long Frequency { get { return 1000000; } } }
    private sealed class FakeMemory : ITimerRtssMemory
    {
        internal readonly byte[] Bytes = new byte[128 + 299520 * 4];
        internal bool Locked, Disposed, FailWrite;
        internal FakeMemory() { WriteInt32(0, 0x52545353); WriteInt32(4, 0x20015); WriteInt32(20, 299520); WriteInt32(24, 128); WriteInt32(28, 4); }
        public long Length { get { return Bytes.Length; } }
        public int ReadInt32(int offset) { return BitConverter.ToInt32(Bytes, offset); }
        public void WriteInt32(int offset, int value) { BitConverter.GetBytes(value).CopyTo(Bytes, offset); }
        public byte[] Read(int offset, int count) { byte[] value = new byte[count]; Buffer.BlockCopy(Bytes, offset, value, 0, count); return value; }
        public void Write(int offset, byte[] bytes) { if (FailWrite) throw new IOException("test write failure"); Buffer.BlockCopy(bytes, 0, Bytes, offset, bytes.Length); }
        public bool TryAcquire() { if (Locked) return false; Locked = true; return true; }
        public void Release() { Locked = false; }
        public void Dispose() { Disposed = true; }
        internal void Owner(int slot, string text) { Write(128 + slot * 299520 + 256, Encoding.ASCII.GetBytes(text + "\0")); }
        internal string Text(int slot) { byte[] b = Read(128 + slot * 299520 + 266752, 32768); return Encoding.Default.GetString(b, 0, Array.IndexOf(b, (byte)0)); }
    }
    [STAThread] private static int Main(string[] args)
    {
        output = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); Directory.CreateDirectory(output);
        try
        {
            if (args.Length > 0 && args[0] == "--performance") { Performance(); return 0; }
            if (args.Length > 0 && args[0] == "--rtss-live") { RtssLive(); return 0; }
            Core(); Options(); Rendering(); Protocol(); Atomic(); Reuse();
            if (args.Length > 0 && args[0] == "--desktop") Desktop();
            Console.WriteLine("PASS TimerTests " + passed + " checks"); return 0;
        }
        catch (Exception e) { Console.WriteLine(e); return 1; }
    }
    private static void Core()
    {
        FakeClock clock = new FakeClock(); TimerEngine timer = new TimerEngine(clock);
        Check(timer.Elapsed == TimeSpan.Zero && !timer.IsRunning, "timer starts at zero paused");
        timer.Start(); clock.Now = 1234567; Check(Math.Abs(timer.Elapsed.TotalMilliseconds - 1234.567) < 0.001, "monotonic fractional precision");
        timer.Start(); clock.Now += 2000000; Check(timer.Elapsed.TotalMilliseconds > 3234, "start is idempotent");
        timer.Pause(); TimeSpan paused = timer.Elapsed; clock.Now += 100000000; Check(timer.Elapsed == paused, "pause excludes wall time");
        timer.Start(); clock.Now += 765433; timer.Pause(); Check(timer.Elapsed == TimeSpan.FromSeconds(4), "resume accumulates precisely");
        timer.Reset(); Check(timer.Elapsed == TimeSpan.Zero && !timer.IsRunning, "reset returns zero paused");
        timer.Start(); clock.Now += 86400000000L * 7; Check(timer.Elapsed.TotalDays == 7, "long running seven days without display ticks");
        TimerOptions options = new TimerOptions();
        Check(TimerText.Format(TimeSpan.FromMilliseconds(3723456), options) == "62:03.456", "minute display does not wrap after one hour");
        options.TimeFormat = TimerTimeFormat.HoursMinutesSecondsMilliseconds;
        Check(TimerText.Format(TimeSpan.FromMilliseconds(3723456), options) == "01:02:03.456", "hour minute second millisecond format");
        options.TimeFormat = TimerTimeFormat.SecondsMilliseconds;
        Check(TimerText.Format(TimeSpan.FromMilliseconds(3723456), options) == "3723.456", "total seconds format");
        options.TextTemplate = "耗时 Time {hh}:{mm}:{ss}.{fff} {{time}} {unknown}";
        Check(TimerText.Format(TimeSpan.FromMilliseconds(3723456), options) == "耗时 Time 01:02:03.456 {time} {unknown}", "bilingual templates escape braces and retain unknown tokens");
        options.TextTemplate = "{time}"; Check(TimerText.Format(TimeSpan.FromTicks(-1), options) == "0.000", "negative input is clamped");
        TimerEngine real = new TimerEngine(); real.Start(); Stopwatch reference = Stopwatch.StartNew(); Thread.Sleep(120);
        Check(Math.Abs(real.Elapsed.TotalMilliseconds - reference.Elapsed.TotalMilliseconds) < 5, "real Stopwatch precision within five milliseconds");
    }
    private static void Options()
    {
        TimerOptions options = new TimerOptions { Enabled = true, Visible = false, FontFamily = "Microsoft YaHei UI", FontSize = 31.5f, Bold = true, Italic = true,
            Outline = false, OutlineWidth = 3.25f, LetterSpacing = 1.5f, Color = "#1234ab", OutlineColor = "#012345", Opacity = 57,
            X = -50, Y = 113, MonitorDevice = "DISPLAY2", TextTemplate = "用时 Time {time}", Backend = TimerBackend.Rtss,
            StartPauseHotkey = new TriggerSpec { Ctrl = true, VirtualKey = (int)Keys.F5 }, ResetHotkey = new TriggerSpec { VirtualKey = (int)Keys.F6 }, VisibilityHotkey = new TriggerSpec { VirtualKey = (int)Keys.F7 } };
        options.Normalize(); TimerOptions clone = options.Clone(); clone.StartPauseHotkey.VirtualKey = (int)Keys.F8;
        Check(options.StartPauseHotkey.VirtualKey == (int)Keys.F5, "hotkeys clone independently");
        XmlSerializer serializer = new XmlSerializer(typeof(TimerOptions)); string xml;
        using (StringWriter writer = new StringWriter()) { serializer.Serialize(writer, options); xml = writer.ToString(); }
        TimerOptions read; using (StringReader reader = new StringReader(xml)) read = (TimerOptions)serializer.Deserialize(reader);
        Check(read.Enabled && !read.Visible && read.Backend == TimerBackend.Rtss, "enabled visibility backend XML roundtrip");
        Check(read.FontSize == 31.5f && read.Bold && read.Italic && !read.Outline && read.OutlineWidth == 3.25f && read.LetterSpacing == 1.5f, "font and outline XML roundtrip");
        Check(read.X == -50 && read.Y == 113 && read.Opacity == 57 && read.MonitorDevice == "DISPLAY2" && read.TextTemplate == "用时 Time {time}", "position custom bilingual template XML roundtrip");
        Check(read.Color == "#1234AB" && read.ResetHotkey.VirtualKey == (int)Keys.F6 && read.VisibilityHotkey.VirtualKey == (int)Keys.F7, "colors three hotkeys XML roundtrip");
        using (StringReader reader = new StringReader("<TimerOptions />")) read = (TimerOptions)serializer.Deserialize(reader);
        Check(!read.Enabled && read.Visible && read.FontSize == 28 && read.StartPauseHotkey == null, "absent fields preserve opt-in defaults");
        options.FontSize = float.NaN; options.OutlineWidth = float.PositiveInfinity; options.LetterSpacing = 999; options.X = int.MinValue; options.Color = "bad"; options.Opacity = 500;
        options.ResetHotkey = new TriggerSpec { VirtualKey = (int)Keys.ShiftKey }; options.Normalize();
        Check(options.FontSize == 28 && options.OutlineWidth == 2 && options.LetterSpacing == 40 && options.X == -16384 && options.Opacity == 100 && options.Color == "#FFFFFF", "invalid numeric and color settings normalize");
        Check(options.ResetHotkey == null, "modifier-only timer hotkey rejected");
    }
    private static long Alpha(Bitmap bitmap)
    {
        long sum = 0;
        for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++) sum += bitmap.GetPixel(x, y).A;
        return sum;
    }
    private static void Rendering()
    {
        TimerOptions options = new TimerOptions { TextTemplate = "耗时 Time {time}", FontSize = 34, Bold = true, Color = "#22FFAA", OutlineColor = "#8A1234" };
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.FromMilliseconds(12345)))
        { Check(bitmap.Width > 200 && bitmap.Height > 30 && Alpha(bitmap) > 10000, "Chinese English timer renders with transparency"); bitmap.Save(Path.Combine(output, "timer-bilingual.png"), ImageFormat.Png); }
        options.Outline = false; long plain;
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) plain = Alpha(bitmap);
        options.Outline = true;
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) Check(Alpha(bitmap) > plain, "outline adds visible alpha pixels");
        options.Outline = false; int before;
        using (Bitmap bitmap = TimerRenderer.Render(options, "ABCD")) before = bitmap.Width;
        options.LetterSpacing = 4;
        using (Bitmap bitmap = TimerRenderer.Render(options, "ABCD")) Check(bitmap.Width >= before + 11, "letter spacing changes measured width");
        options.LetterSpacing = 0; options.Italic = true; options.TextTemplate = "中文\nEnglish {time}";
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) Check(bitmap.Height > 80 && Alpha(bitmap) > 10000, "multiline italic bilingual rendering");
        options.FontFamily = "ThisFontDoesNotExist";
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) Check(Alpha(bitmap) > 1000, "missing font falls back");
        options.FontSize = 8;
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) Check(bitmap.Width > 1 && bitmap.Height > 1, "minimum font size renders");
        options.FontSize = 160;
        using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.Zero)) Check(bitmap.Width <= 4096 && bitmap.Height <= 2048, "maximum font size is bounded");
        options.FontSize = 28; Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++) using (Bitmap bitmap = TimerTextRenderer.Render(options, TimeSpan.FromMilliseconds(i * 50))) { }
        Console.WriteLine("METRIC 100 timer renders ms=" + watch.ElapsedMilliseconds);
        Check(watch.ElapsedMilliseconds < 10000, "render throughput stays below one hundred milliseconds per frame");
    }
    private static void Protocol()
    {
        TimerOptions options = new TimerOptions { Backend = TimerBackend.Rtss, TextTemplate = "耗时 Time {time}" };
        using (TimerRtss none = new TimerRtss(options, delegate { return null; }, output)) Check(!none.Present("00:00.000") && none.Status == "RtssNotRunning", "missing RTSS is optional");
        FakeMemory memory = new FakeMemory(); memory.Owner(0, "Primary"); memory.Owner(1, "ForeignClient"); byte[] before = memory.Read(128 + 299520, 299520); string path;
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output))
        {
            Check(client.Present(TimerText.Format(TimeSpan.Zero, options)), "RTSS claims an unused nonprimary slot");
            Check(!memory.Locked && memory.ReadInt32(32) == 1, "RTSS commits frame and releases lock");
            Check(memory.Text(2).Contains("<LI=\"") && memory.Text(2).Contains("<I=") && !memory.Text(2).Contains("耗时"), "RTSS custom Chinese text uses raster glyphs instead of ANSI text");
            path = client.AtlasPath; Check(File.Exists(path), "RTSS glyph atlas exists"); DateTime modified = File.GetLastWriteTimeUtc(path);
            using (Bitmap atlasImage = new Bitmap(path)) Check((atlasImage.Width & (atlasImage.Width - 1)) == 0 && (atlasImage.Height & (atlasImage.Height - 1)) == 0 && atlasImage.Height >= 512, "RTSS glyph texture has power-of-two dimensions preserving source coordinates");
            Check(!memory.Text(2).Contains("<FNT"), "RTSS timer never changes the shared font zoom declaration");
            bool updates = true; for (int i = 0; i < 100; i++) updates &= client.Present(TimerText.Format(TimeSpan.FromMilliseconds(i * 50), options));
            Check(updates, "RTSS hundred successive updates");
            Check(File.GetLastWriteTimeUtc(path) == modified, "RTSS ticks do not rewrite atlas PNG");
            Check(Convert.ToBase64String(before) == Convert.ToBase64String(memory.Read(128 + 299520, 299520)), "RTSS leaves foreign slot byte identical");
            memory.Locked = true; Check(!client.Present("00:00.000") && client.Status == "RtssBusy", "RTSS busy lock is not stolen"); memory.Locked = false;
            memory.FailWrite = true; Check(!client.Present("00:00.000") && client.Status == "RtssIoError" && !memory.Locked, "RTSS write failure releases lock"); memory.FailWrite = false;
        }
        Check(memory.Disposed && memory.Bytes[128 + 2 * 299520 + 256] == 0 && memory.Bytes[128 + 2 * 299520 + 266752] == 0, "RTSS dispose clears owned text owner and mapping");
        Check(!File.Exists(path) && !Directory.Exists(Path.GetDirectoryName(path)), "RTSS dispose removes temporary atlas");
        memory = new FakeMemory(); for (int i = 1; i < 4; i++) memory.Owner(i, "Other" + i);
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output)) Check(!client.Present("00:00.000") && client.Status == "RtssNoSlot", "RTSS slot exhaustion preserves clients");
        memory = new FakeMemory(); memory.WriteInt32(4, 0x2000d);
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output)) Check(!client.Present("00:00.000") && client.Status == "RtssUnsupported", "RTSS old unsupported protocol rejected");
        memory = new FakeMemory(); memory.WriteInt32(4, 0x20014);
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output)) Check(!client.Present("00:00.000") && client.Status == "RtssUnsupported", "RTSS protocol before 2.21 rejects fixed-pixel font override capability");
        memory = new FakeMemory(); memory.WriteInt32(28, int.MaxValue);
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output)) Check(!client.Present("00:00.000") && client.Status == "RtssInvalidMemory", "RTSS malformed layout rejected");
        memory = new FakeMemory(); memory.Owner(1, "InputStitch.Timer.2147483647.stale");
        using (TimerRtss client = new TimerRtss(options, delegate { return memory; }, output)) Check(client.Present("00:00.000") && memory.Text(1).Contains("<I="), "RTSS reclaims only terminated InputStitch owners");
    }
    private static void Atomic()
    {
        MethodInfo factory = typeof(TimerRtssMemory).GetMethod("CreateAtomic", BindingFlags.Static | BindingFlags.NonPublic);
        Delegate atomic = (Delegate)factory.Invoke(null, null); IntPtr word = Marshal.AllocHGlobal(4);
        try
        {
            Marshal.WriteInt32(word, 0);
            Check((int)atomic.DynamicInvoke(word, 1, 0) == 0 && Marshal.ReadInt32(word) == 1, "real native memory atomic lock claim");
            Check((int)atomic.DynamicInvoke(word, 1, 0) == 1 && Marshal.ReadInt32(word) == 1, "real native memory atomic contention");
            Check((int)atomic.DynamicInvoke(word, 0, 1) == 1 && Marshal.ReadInt32(word) == 0, "real native memory atomic lock release");
        }
        finally { Marshal.FreeHGlobal(word); }
        TimerRtssMemory memory = (TimerRtssMemory)Activator.CreateInstance(typeof(TimerRtssMemory), true);
        word = Marshal.AllocHGlobal(40);
        FieldInfo address = typeof(TimerRtssMemory).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            address.SetValue(memory, word); typeof(TimerRtssMemory).GetField("length", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(memory, 40L);
            Marshal.WriteInt32(word, 36, 0x80);
            Check(memory.TryAcquire() && Marshal.ReadInt32(word, 36) == 0x81, "native RTSS claim preserves unrelated busy bits");
            memory.Release(); Check(Marshal.ReadInt32(word, 36) == 0x80, "native RTSS release preserves unrelated busy bits");
        }
        finally { address.SetValue(memory, IntPtr.Zero); memory.Dispose(); Marshal.FreeHGlobal(word); }
    }
    private static void Reuse()
    {
        FakeMemory memory = new FakeMemory(); int creates = 0;
        TimerOptions options = new TimerOptions { Enabled = true, Backend = TimerBackend.Rtss, TextTemplate = "用时 Time {time}" };
        using (TimerController controller = new TimerController(null, delegate(TimerOptions o) { creates++; return new TimerRtss(o, delegate { return memory; }, output); }))
        {
            controller.Apply(options); TimerRtss original = controller.Rtss; string path = original.AtlasPath;
            DateTime modified = File.GetLastWriteTimeUtc(path); int frame = memory.ReadInt32(32);
            controller.Apply(options.Clone());
            Check(creates == 1 && object.ReferenceEquals(original, controller.Rtss) && memory.ReadInt32(32) == frame, "identical Apply skips RTSS output and cache rebuilding");
            options.StartPauseHotkey = new TriggerSpec { VirtualKey = (int)Keys.F8 }; controller.Apply(options);
            Check(creates == 1 && memory.ReadInt32(32) == frame && controller.Rtss.AtlasPath == path, "hotkey-only Apply leaves timer rendering untouched");
            for (int i = 0; i < 30; i++) { options.X = 100 + i; options.Y = 80 + i; controller.Apply(options); }
            Check(creates == 1 && controller.Rtss.AtlasPath == path && File.GetLastWriteTimeUtc(path) == modified, "position slider reuses RTSS atlas without disk writes");
            Check(memory.Text(1).Contains("<P=129,109>"), "position-only Apply updates RTSS image placement");
            options.Visible = false; controller.Apply(options);
            Check(memory.Bytes[128 + 299520 + 256] == 0 && File.Exists(path), "hide releases RTSS owner while retaining atlas");
            options.Visible = true; controller.Apply(options);
            Check(controller.Rtss.AtlasPath == path && File.GetLastWriteTimeUtc(path) == modified && creates == 1, "show reuses unchanged RTSS atlas");
            options.Enabled = false; controller.Apply(options); options.Enabled = true; controller.Apply(options);
            Check(controller.Rtss.AtlasPath == path && File.GetLastWriteTimeUtc(path) == modified && creates == 1, "enable toggle reuses unchanged RTSS atlas");
            options.FontSize = 32; controller.Apply(options);
            Check(creates == 2 && controller.Rtss.AtlasPath != path && !File.Exists(path), "font change rebuilds and cleans old RTSS atlas");
            memory.Locked = true; controller.Refresh(); Check(controller.Status == "RtssBusy", "busy RTSS reports temporary contention");
            memory.Locked = false; frame = memory.ReadInt32(32); controller.Refresh();
            Check(controller.Status.Length == 0 && memory.ReadInt32(32) == frame + 1, "busy RTSS retries next refresh without one-second backoff");
            memory.WriteInt32(8, 336); memory.WriteInt32(12, 80); memory.WriteInt32(16, 1); memory.WriteInt32(64, 0); memory.WriteInt32(404, 2);
            options.X = 41; options.Y = 73; controller.Apply(options); path = controller.Rtss.AtlasPath;
            Check(memory.Text(1).Contains("<P=20,36>") && !memory.Text(1).Contains("<FNT"), "RTSS reads target zoom and compensates position without changing other clients");
            memory.WriteInt32(404, 3); controller.KeepOnTop();
            Check(memory.Text(1).Contains("<P=13,24>") && controller.Rtss.AtlasPath == path, "paused foreground notification refreshes RTSS zoom using existing atlas");
        }
        using (TimerGlyphAtlas atlas = new TimerGlyphAtlas(new TimerOptions { TextTemplate = "计", X = 41, Y = 73 }, output))
        {
            bool mapped = true;
            for (int zoom = 1; zoom <= 8; zoom++)
            {
                string tags = atlas.Hypertext("计", zoom); int start = tags.IndexOf("<I=", StringComparison.Ordinal) + 3, end = tags.IndexOf('>', start);
                string[] values = tags.Substring(start, end - start).Split(',');
                int targetWidth = int.Parse(values[0]), targetHeight = int.Parse(values[1]), sourceWidth = int.Parse(values[4]), sourceHeight = int.Parse(values[5]);
                mapped &= targetWidth * zoom == sourceWidth && targetHeight * zoom == sourceHeight;
            }
            Check(mapped, "RTSS zoom one through eight preserves one physical pixel per source pixel");
        }
    }
    private static void Performance()
    {
        TimerOptions[] styles = new TimerOptions[] { new TimerOptions(), new TimerOptions { FontSize = 96, Bold = true, Italic = true, OutlineWidth = 6, LetterSpacing = 2, TextTemplate = "任务计时 Mission timer {time}\n当前阶段 Stage 2" } };
        for (int sample = 0; sample < styles.Length; sample++)
        {
            TimerOptions options = styles[sample]; Process process = Process.GetCurrentProcess();
            using (TimerFrameCache cache = new TimerFrameCache(options))
            {
                for (int i = 0; i < 20; i++) using (Bitmap image = cache.Render(TimerText.Format(TimeSpan.FromMilliseconds(i * 50), options))) { }
                process.Refresh(); TimeSpan cpu = process.TotalProcessorTime; long privateStart = process.PrivateMemorySize64, privatePeak = privateStart;
                int handlesStart = GetGuiResources(process.Handle, 0), gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2), frames = 0;
                Stopwatch elapsed = Stopwatch.StartNew(); double rendering = 0;
                while (elapsed.ElapsedMilliseconds < 30000)
                {
                    long start = Stopwatch.GetTimestamp(); using (Bitmap image = cache.Render(TimerText.Format(elapsed.Elapsed, options))) { }
                    rendering += (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency; frames++;
                    if (frames % 20 == 0) { process.Refresh(); privatePeak = Math.Max(privatePeak, process.PrivateMemorySize64); }
                    int sleep = (int)(frames * 50 - elapsed.ElapsedMilliseconds); if (sleep > 0) Thread.Sleep(sleep);
                }
                process.Refresh(); double cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
                Console.WriteLine("PERF " + (sample == 0 ? "default" : "large_bilingual") + " duration_ms=" + elapsed.ElapsedMilliseconds + " frames=" + frames + " cpu_one_core_percent=" + (cpuMs / elapsed.Elapsed.TotalMilliseconds * 100).ToString("F3") +
                    " render_ms_per_frame=" + (rendering / frames).ToString("F3") + " private_start=" + privateStart + " private_end=" + process.PrivateMemorySize64 + " private_peak=" + privatePeak +
                    " gdi_start=" + handlesStart + " gdi_end=" + GetGuiResources(process.Handle, 0) + " gen0=" + (GC.CollectionCount(0) - gen0) + " gen1=" + (GC.CollectionCount(1) - gen1) + " gen2=" + (GC.CollectionCount(2) - gen2) + " cached_parts=" + cache.CachedParts);
                Check(GetGuiResources(process.Handle, 0) <= handlesStart + 5, "sustained cache rendering does not accumulate GDI resources");
                Check(cache.CachedParts < 32, "sustained text cache stays bounded");
            }
        }
        Console.WriteLine("PASS 60-second timer cache performance without forced GC");
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int msg, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flag);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint flags);
    private static void Capture(Graphics graphics, Point location, Size size)
    {
        IntPtr source = GetDC(IntPtr.Zero), target = graphics.GetHdc();
        try { if (!BitBlt(target, 0, 0, size.Width, size.Height, source, location.X, location.Y, 0x40cc0020)) throw new Exception("Desktop BitBlt capture failed."); }
        finally { graphics.ReleaseHdc(target); if (source != IntPtr.Zero) ReleaseDC(IntPtr.Zero, source); }
    }
    private static void Desktop()
    {
        SetProcessDPIAware();
        IntPtr foreground = GetForegroundWindow(); int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
        Check(foreground != IntPtr.Zero, "interactive desktop foreground is available");
        using (Control control = new Control())
        using (TimerController controller = new TimerController(control))
        {
            control.CreateControl(); controller.Apply(new TimerOptions { Enabled = true, TextTemplate = "计时 Time {time}", X = 60, Y = 60 }); Application.DoEvents();
            Check(controller.Window != null && controller.Window.Visible, "native timer overlay is visible");
            Check(GetForegroundWindow() == foreground, "native timer overlay never activates");
            Check(SendMessage(controller.Window.Handle, 0x84, IntPtr.Zero, IntPtr.Zero).ToInt64() == -1, "native timer hit test passes through");
            Check((GetWindowLong(controller.Window.Handle, -20) & 0x080800a0) == 0x080800a0, "native timer layered transparent noactivate toolwindow styles");
            controller.Start(); Thread.Sleep(120); controller.Refresh(); Check(controller.Elapsed.TotalMilliseconds >= 100, "controller catches up after blocked UI");
            controller.Pause(); TimeSpan elapsed = controller.Elapsed; Thread.Sleep(50); controller.Refresh(); Check(controller.Elapsed == elapsed, "paused controller does not accumulate");
            for (int i = 0; i < 50; i++) { controller.Reset(); controller.Refresh(); }
            controller.SetVisible(false); Check(!controller.Window.Visible, "hide removes native timer overlay");
            controller.SetVisible(true); Check(controller.Window.Visible, "show restores native timer overlay");
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            using (Bitmap desktop = new Bitmap(600, 160))
            using (Graphics graphics = Graphics.FromImage(desktop))
            {
                controller.SetVisible(false); Thread.Sleep(80); Application.DoEvents();
                Capture(graphics, new Point(screen.Left + 40, screen.Top + 40), desktop.Size);
                using (Bitmap baseline = (Bitmap)desktop.Clone())
                {
                    controller.SetVisible(true); Thread.Sleep(80); Application.DoEvents();
                    Capture(graphics, new Point(screen.Left + 40, screen.Top + 40), desktop.Size);
                    int changed = 0; for (int y = 20; y < 80; y++) for (int x = 20; x < 500; x++) if (baseline.GetPixel(x, y) != desktop.GetPixel(x, y)) changed++;
                    Check(changed > 100, "layered timer changes actual desktop pixels");
                }
                desktop.Save(Path.Combine(output, "timer-native-desktop.png"));
            }
        }
        Check(GetGuiResources(Process.GetCurrentProcess().Handle, 0) <= before + 5, "native timer repeated rendering disposes GDI objects");
    }
    private static void RtssLive()
    {
        ITimerRtssMemory shared = TimerRtssMemory.Open();
        Check(shared != null, "actual RTSS mapping opens");
        int offset = shared.ReadInt32(24), size = shared.ReadInt32(20), count = shared.ReadInt32(28), initialFrame = shared.ReadInt32(32);
        byte[][] snapshots = new byte[count][]; for (int i = 0; i < count; i++) snapshots[i] = shared.Read(offset + i * size, size);
        Console.WriteLine("LIVE version=" + shared.ReadInt32(4).ToString("X8") + " slots=" + count + " entry_size=" + size);
        TimerOptions options = new TimerOptions { Backend = TimerBackend.Rtss, TextTemplate = "输入缝合 InputStitch {time}", Color = "#22FFAA", FontSize = 32, X = 60, Y = 60 };
        TimerRtss client = new TimerRtss(options); string atlas;
        try
        {
            Check(client.Present(TimerText.Format(TimeSpan.FromMilliseconds(12345), options)), "actual RTSS owns a slot and publishes Chinese glyph image tags status=" + client.Status);
            atlas = client.AtlasPath;
            Check(File.Exists(atlas), "actual RTSS atlas was generated");
            for (int i = 0; i < 100; i++) { if (!client.Present(TimerText.Format(TimeSpan.FromMilliseconds(12345 + i * 50), options))) throw new Exception(client.Status); Thread.Sleep(1); }
            Check((shared.ReadInt32(36) & 1) == 0, "actual RTSS busy bit released after updates");
            Check(shared.ReadInt32(32) >= initialFrame + 101, "actual RTSS OSD frame advances");
        }
        finally { client.Dispose(); }
        Check(!File.Exists(atlas), "actual RTSS atlas removed on exit");
        bool preserved = true;
        for (int i = 0; i < count; i++)
        {
            int ownerLength = Array.IndexOf(snapshots[i], (byte)0, 256, 256);
            if (ownerLength > 256) preserved &= Convert.ToBase64String(snapshots[i]) == Convert.ToBase64String(shared.Read(offset + i * size, size));
        }
        Check(preserved, "actual RTSS prior occupied slots preserved byte for byte"); shared.Dispose();
        Console.WriteLine("PASS RTSS live shared memory tests " + passed + " checks; graphics composition is a separate D3D capture check");
    }
}
