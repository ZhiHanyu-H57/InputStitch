using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using InputStitch;

internal static class TimerD3DAcceptance
{
    [DllImport("d3d9.dll")] static extern IntPtr Direct3DCreate9(uint version);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateDevice(IntPtr self, uint adapter, uint type, IntPtr hwnd, uint flags, ref Parameters pp, out IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Clear(IntPtr self, uint count, IntPtr rects, uint flags, uint color, float z, uint stencil);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Present(IntPtr self, IntPtr source, IntPtr dest, IntPtr hwnd, IntPtr dirty);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint Release(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Scene(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int SetFVF(IntPtr self, uint format);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DrawPrimitiveUP(IntPtr self, uint type, uint count, ref Vertex vertex, uint stride);
    [StructLayout(LayoutKind.Sequential)] struct Vertex { internal float X, Y, Z, RHW; internal uint Color; }
    [StructLayout(LayoutKind.Sequential)] struct Parameters
    {
        internal uint Width, Height, Format, Count, MultiSample, Quality, SwapEffect;
        internal IntPtr Hwnd;
        internal int Windowed, AutoDepth;
        internal uint DepthFormat, Flags, Refresh, Interval;
    }
    static T Method<T>(IntPtr obj, int index) { return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), index * IntPtr.Size), typeof(T)); }
    static int Pixels(Form form, string path)
    {
        using (Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
        {
            using (Graphics g = Graphics.FromImage(bitmap)) g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            bitmap.Save(path); int count = 0;
            int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
            for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                if (c.R > 180 && c.G > 150 && c.B < 100)
                { count++; if (y < 140) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); } }
            }
            if (Path.GetFileName(path) == "shown.png")
            {
                Rectangle bounds = Rectangle.FromLTRB(left, top, right + 1, bottom + 1); Console.WriteLine("TIMER_BOUNDS " + bounds);
                if (bounds.Left < 35 || bounds.Left > 90 || bounds.Top < 70 || bounds.Top > 120 || bounds.Width < 250 || bounds.Width > 600 || bounds.Height < 20 || bounds.Height > 55)
                    throw new Exception("Timer image coordinates or font scale differ from physical-pixel settings");
            }
            return count;
        }
    }
    [STAThread] static int Main()
    {
        using (ITimerRtssMemory preflight = TimerRtssMemory.Open())
            if (preflight == null) { Console.WriteLine("BLOCKED: Start RTSS 7.3.7+ before this optional test."); return 2; }
        string output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rtss-live"); Directory.CreateDirectory(output);
        SetProcessDpiAwarenessContext(new IntPtr(-4)); Application.EnableVisualStyles(); IntPtr d3d = IntPtr.Zero, device = IntPtr.Zero;
        ITimerRtssMemory markerMemory = null; int markerEntry = -1;
        try
        {
            using (Form form = new Form { Text = "InputStitch RTSS D3D9 acceptance", ClientSize = new Size(800, 400), StartPosition = FormStartPosition.Manual, Location = new Point(90, 90), TopMost = true })
            using (TimerController timer = new TimerController(form))
            {
                form.Show(); form.Activate(); Application.DoEvents();
                // Managed P/Invoke loads D3D lazily. Let RTSS observe the module before
                // creating the first device, as it does for a game with static imports.
                IntPtr d3dModule = LoadLibrary("d3d9.dll");
                Stopwatch ready = Stopwatch.StartNew(); while (ready.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(10); }
                try { d3d = Direct3DCreate9(32); } finally { if (d3dModule != IntPtr.Zero) FreeLibrary(d3dModule); }
                if (d3d == IntPtr.Zero) throw new Exception("D3D9 unavailable");
                Parameters pp = new Parameters { Width = 800, Height = 400, Count = 1, SwapEffect = 1, Hwnd = form.Handle, Windowed = 1, Interval = 0x80000000 };
                int hr = Method<CreateDevice>(d3d, 16)(d3d, 0, 1, form.Handle, 0x20, ref pp, out device); Marshal.ThrowExceptionForHR(hr);
                Clear clear = Method<Clear>(device, 43); Present present = Method<Present>(device, 17); Scene begin = Method<Scene>(device, 41), end = Method<Scene>(device, 42);
                SetFVF setFVF = Method<SetFVF>(device, 89); DrawPrimitiveUP drawPrimitive = Method<DrawPrimitiveUP>(device, 83);
                Vertex vertex = new Vertex { X = 0, Y = 0, Z = 0, RHW = 1, Color = 0xff143252 };
                // SDK DesktopOverlayHost/RendererD3D9.cpp explicitly requires a real primitive for RTSS OSD.
                Action<int> frames = delegate(int ms) { Stopwatch watch = Stopwatch.StartNew(); while (watch.ElapsedMilliseconds < ms) { Marshal.ThrowExceptionForHR(clear(device, 0, IntPtr.Zero, 1, 0xff143252, 1, 0)); Marshal.ThrowExceptionForHR(begin(device)); Marshal.ThrowExceptionForHR(setFVF(device, 0x44)); Marshal.ThrowExceptionForHR(drawPrimitive(device, 1, 1, ref vertex, 20)); Marshal.ThrowExceptionForHR(end(device)); Marshal.ThrowExceptionForHR(present(device, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero)); Application.DoEvents(); Thread.Sleep(16); } };
                frames(2500); int baseline = Pixels(form, Path.Combine(output, "before.png"));
                foreach (ProcessModule module in Process.GetCurrentProcess().Modules) if (module.ModuleName.IndexOf("RTSS", StringComparison.OrdinalIgnoreCase) >= 0) Console.WriteLine("HOOK " + module.ModuleName);
                markerMemory = TimerRtssMemory.Open();
                if (!markerMemory.TryAcquire()) throw new Exception("Marker slot lock unavailable");
                try
                {
                    int offset = markerMemory.ReadInt32(24), size = markerMemory.ReadInt32(20), count = markerMemory.ReadInt32(28);
                    for (int i = 1; i < count; i++) if (markerMemory.Read(offset + i * size + 256, 1)[0] == 0) { markerEntry = offset + i * size; break; }
                    if (markerEntry < 0) throw new Exception("No unused marker slot");
                    markerMemory.Write(markerEntry + 256, Encoding.ASCII.GetBytes("InputStitch.TimerProbe.Marker." + Process.GetCurrentProcess().Id + "\0"));
                    markerMemory.WriteInt32(markerEntry, 0); markerMemory.WriteInt32(markerEntry + 266752, 0);
                    byte[] marker = Encoding.ASCII.GetBytes("<P=10,150><C=FFE020>OTHER CLIENT<C>\0");
                    markerMemory.Write(markerEntry + 512, marker); markerMemory.Write(markerEntry + 266752, marker);
                    markerMemory.WriteInt32(32, markerMemory.ReadInt32(32) + 1);
                }
                finally { markerMemory.Release(); }
                Stopwatch markerReady = Stopwatch.StartNew(); bool markerVisible = false;
                do { frames(200); markerVisible = Pixels(form, Path.Combine(output, "marker-before.png")) > 100; }
                while (!markerVisible && markerReady.ElapsedMilliseconds < 15000);
                if (!markerVisible) { Console.WriteLine("BLOCKED: RTSS did not render the independent baseline; product output was not started."); return 2; }
                TimerOptions options = new TimerOptions { Enabled = true, Visible = true, Backend = TimerBackend.Rtss, FontSize = 36, Color = "#FFE020", Outline = true, X = 40, Y = 70, TextTemplate = "计时 Timer {time}", Bold = true, Italic = true, LetterSpacing = 2 };
                options.Parts = TimerText.CreateParts(options);
                foreach (TimerPart part in options.Parts) if (part.Kind == TimerPartKind.Milliseconds) { part.Style.FontSize = 18; part.Style.Color = "#22FFFF"; part.Style.Bold = false; }
                timer.Apply(options); timer.Start(); frames(3000);
                int shown = Pixels(form, Path.Combine(output, "shown.png")); string status = timer.Status;
                using (Bitmap styled = new Bitmap(Path.Combine(output, "shown.png")))
                {
                    int cyan = 0; for (int yy = 50; yy < 140; yy++) for (int xx = 30; xx < 750; xx++)
                    { Color pixel = styled.GetPixel(xx, yy); if (pixel.R < 100 && pixel.G > 170 && pixel.B > 170) cyan++; }
                    Console.WriteLine("PARTS smaller cyan milliseconds pixels=" + cyan);
                    if (cyan < 20) throw new Exception("RTSS did not render independently styled millisecond part");
                }
                using (Bitmap beforeMarker = new Bitmap(Path.Combine(output, "marker-before.png")))
                using (Bitmap afterMarker = new Bitmap(Path.Combine(output, "shown.png")))
                {
                    int changed = 0; for (int y = 140; y < 390; y++) for (int x = 0; x < 790; x++) if (beforeMarker.GetPixel(x, y) != afterMarker.GetPixel(x, y)) changed++;
                    Console.WriteLine("OTHER_CLIENT changedPixels=" + changed); if (changed != 0) throw new Exception("Timer changed the independent client's profile-scale rendering");
                }
                File.Copy(timer.Rtss.AtlasPath, Path.Combine(output, "atlas.png"), true);
                timer.Pause();
                frames(100); Pixels(form, Path.Combine(output, "paused.png"));
                options.X++; options.Y++; timer.Apply(options); frames(400); Pixels(form, Path.Combine(output, "shifted.png"));
                using (Bitmap before = new Bitmap(Path.Combine(output, "paused.png")))
                using (Bitmap after = new Bitmap(Path.Combine(output, "shifted.png")))
                {
                    int changed = 0;
                    for (int y = 50; y < 138; y++) for (int x = 0; x < 750; x++)
                        if (before.GetPixel(x,y) != after.GetPixel(x+1,y+1)) changed++;
                    Console.WriteLine("ONE_PIXEL_SHIFT mismatch=" + changed);
                    if (changed != 0) throw new Exception("RTSS position did not move exactly one physical pixel");
                }
                if (!markerMemory.TryAcquire()) throw new Exception("Marker cleanup lock unavailable");
                try { markerMemory.WriteInt32(markerEntry + 512, 0); markerMemory.WriteInt32(markerEntry + 266752, 0); markerMemory.Write(markerEntry + 256, new byte[256]); markerMemory.WriteInt32(32, markerMemory.ReadInt32(32) + 1); markerEntry = -1; }
                finally { markerMemory.Release(); }
                timer.Pause(); timer.SetVisible(false); frames(800); int hidden = Pixels(form, Path.Combine(output, "hidden.png"));
                string result = "baselineYellow=" + baseline + "; shownYellow=" + shown + "; hiddenYellow=" + hidden + "; status=" + status + "; elapsed=" + timer.Elapsed;
                File.WriteAllText(Path.Combine(output, "result.txt"), result); Console.WriteLine(result);
                if (status.Length > 0 || shown < baseline + 100 || hidden > baseline + 40) throw new Exception("Actual RTSS D3D9 composition failed");
                Console.WriteLine("PASS RTSS actual D3D9 frame: bilingual custom font/style glyphs present and hidden");
            }
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
        finally
        {
            if (markerMemory != null)
            {
                if (markerEntry >= 0 && markerMemory.TryAcquire())
                    try { markerMemory.WriteInt32(markerEntry + 512, 0); markerMemory.WriteInt32(markerEntry + 266752, 0); markerMemory.Write(markerEntry + 256, new byte[256]); markerMemory.WriteInt32(32, markerMemory.ReadInt32(32) + 1); }
                    finally { markerMemory.Release(); }
                markerMemory.Dispose();
            }
            if (device != IntPtr.Zero) Method<Release>(device, 2)(device); if (d3d != IntPtr.Zero) Method<Release>(d3d, 2)(d3d);
        }
    }
}
