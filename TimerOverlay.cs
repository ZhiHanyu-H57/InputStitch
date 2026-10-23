using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace InputStitch
{
    internal sealed class TimerOverlay : NativeWindow, IDisposable
    {
        private byte[] pixelBuffer;
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { internal int X, Y; internal NativePoint(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { internal int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { internal byte Operation, Flags, Alpha, Format; }
        [StructLayout(LayoutKind.Sequential)] private struct BitmapHeader
        { internal uint Size; internal int Width, Height; internal ushort Planes, Bits; internal uint Compression, ImageSize; internal int XResolution, YResolution; internal uint ColorsUsed, ColorsImportant; }
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapHeader header, uint usage, out IntPtr pixels, IntPtr section, uint offset);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destination, ref NativePoint position,
            ref NativeSize size, IntPtr source, ref NativePoint origin, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        internal TimerOverlay()
        { CreateHandle(new CreateParams { Caption = "InputStitch Timer", Style = unchecked((int)0x80000000), ExStyle = 0x80000 | 0x20 | 0x80 | 0x08000000, Width = 1, Height = 1 }); }
        internal bool Visible { get { return Handle != IntPtr.Zero && IsWindowVisible(Handle); } }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x84) { message.Result = new IntPtr(-1); return; }
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            if (message.Msg == 0x2e0) { message.Result = IntPtr.Zero; return; }
            base.WndProc(ref message);
        }
        internal void Present(Bitmap bitmap, Point location, int opacity)
        {
            NativePoint position = new NativePoint(location.X, location.Y), origin = new NativePoint();
            NativeSize size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
            Blend blend = new Blend { Alpha = (byte)((opacity * 255 + 50) / 100), Format = 1 };
            IntPtr dc = GetDC(IntPtr.Zero), memory = IntPtr.Zero, hbitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(dc);
                BitmapHeader header = new BitmapHeader { Size = 40, Width = bitmap.Width, Height = -bitmap.Height, Planes = 1, Bits = 32 };
                IntPtr pixels; hbitmap = CreateDIBSection(memory, ref header, 0, out pixels, IntPtr.Zero, 0);
                if (hbitmap == IntPtr.Zero) throw new Win32Exception("Timer alpha surface allocation failed.");
                BitmapData data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                try
                {
                    int length = bitmap.Width * bitmap.Height * 4;
                    if (pixelBuffer == null || pixelBuffer.Length != length) pixelBuffer = new byte[length];
                    Marshal.Copy(data.Scan0, pixelBuffer, 0, length); Marshal.Copy(pixelBuffer, 0, pixels, length);
                }
                finally { bitmap.UnlockBits(data); }
                previous = SelectObject(memory, hbitmap);
                if (!UpdateLayeredWindow(Handle, dc, ref position, ref size, memory, ref origin, 0, ref blend, 2)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Timer overlay rendering failed.");
                SetWindowPos(Handle, new IntPtr(-1), location.X, location.Y, bitmap.Width, bitmap.Height, 0x50);
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (hbitmap != IntPtr.Zero) DeleteObject(hbitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (dc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, dc);
            }
        }
        internal void KeepOnTop() { if (Visible) SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x13); }
        internal void Hide() { if (Handle != IntPtr.Zero) ShowWindow(Handle, 0); }
        public void Dispose() { if (Handle != IntPtr.Zero) DestroyHandle(); pixelBuffer = null; }
    }

    internal sealed class TimerController : IDisposable
    {
        private readonly Control dispatcher;
        private readonly Func<TimerOptions, TimerRtss> rtssFactory;
        private readonly TimerEngine engine = new TimerEngine();
        private readonly System.Windows.Forms.Timer displayTimer = new System.Windows.Forms.Timer();
        private TimerOptions options = new TimerOptions();
        private TimerOverlay window;
        private TimerFrameCache frames;
        private TimerRtss rtss;
        private string lastText;
        private bool disposed;
        private DateTime nextRetry;
        internal TimeSpan Elapsed { get { return engine.Elapsed; } }
        internal bool IsRunning { get { return engine.IsRunning; } }
        internal string Status { get; private set; }
        internal bool Visible { get { return options.Enabled && options.Visible && options.Opacity > 0; } }
        internal TimerOverlay Window { get { return window; } }
        internal TimerRtss Rtss { get { return rtss; } }
        internal TimerController(Control owner) : this(owner, delegate(TimerOptions value) { return new TimerRtss(value); }) { }
        internal TimerController(Control owner, Func<TimerOptions, TimerRtss> factory)
        {
            dispatcher = owner; rtssFactory = factory; Status = "";
            displayTimer.Interval = 50; displayTimer.Tick += Tick;
            SystemEvents.DisplaySettingsChanged += DisplaysChanged;
        }
        internal void Apply(TimerOptions value)
        {
            if (disposed) return;
            TimerOptions next = (value ?? new TimerOptions()).Clone(); next.Normalize();
            bool renderingChanged = !options.SameRendering(next), backendChanged = options.Backend != next.Backend;
            bool placementChanged = options.X != next.X || options.Y != next.Y || options.MonitorDevice != next.MonitorDevice;
            bool visibilityChanged = options.Enabled != next.Enabled || options.Visible != next.Visible;
            options = next;
            if (!renderingChanged && !backendChanged && !placementChanged && !visibilityChanged) return;
            if (renderingChanged || backendChanged)
            {
                if (rtss != null) { rtss.Dispose(); rtss = null; }
                if (frames != null) { frames.Dispose(); frames = null; }
            }
            else if (rtss != null) rtss.UpdatePlacement(options);
            lastText = null; nextRetry = DateTime.MinValue;
            Refresh();
        }
        internal void StartPause() { if (engine.IsRunning) Pause(); else Start(); }
        internal void Start() { if (disposed) return; engine.Start(); Refresh(); }
        internal void Pause() { if (disposed) return; engine.Pause(); Refresh(); }
        internal void Reset() { if (disposed) return; engine.Reset(); lastText = null; Refresh(); }
        internal void SetVisible(bool visible) { if (disposed) return; options.Visible = visible; lastText = null; Refresh(); }
        internal void ToggleVisible() { SetVisible(!options.Visible); }
        private void Tick(object sender, EventArgs e) { Refresh(); }
        internal void Refresh()
        {
            if (disposed) return;
            bool visible = Visible;
            displayTimer.Enabled = visible && engine.IsRunning;
            if (!visible)
            {
                if (window != null) window.Hide();
                if (rtss != null && !rtss.Hide()) { displayTimer.Enabled = true; Status = rtss.Status; return; }
                lastText = null; Status = ""; return;
            }
            string text = TimerText.Format(engine.Elapsed, options);
            if (options.Backend == TimerBackend.Rtss)
            {
                if (window != null) window.Hide();
                if (DateTime.UtcNow < nextRetry) return;
                if (rtss == null) rtss = rtssFactory(options);
                if (!rtss.Present(text)) { Status = rtss.Status; nextRetry = Status == "RtssBusy" ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(1); return; }
                Status = ""; lastText = text;
            }
            else
            {
                if (rtss != null) { rtss.Dispose(); rtss = null; }
                if (text == lastText && window != null && window.Visible) return;
                Screen selected = null;
                foreach (Screen screen in Screen.AllScreens) if (string.Equals(screen.DeviceName, options.MonitorDevice, StringComparison.OrdinalIgnoreCase)) selected = screen;
                if (selected == null) selected = Screen.PrimaryScreen;
                if (selected == null) return;
                if (window == null) window = new TimerOverlay();
                if (frames == null) frames = new TimerFrameCache(options);
                using (Bitmap bitmap = frames.Render(text)) window.Present(bitmap, new Point(selected.Bounds.Left + options.X, selected.Bounds.Top + options.Y), options.Opacity);
                lastText = text; Status = "";
            }
        }
        internal void KeepOnTop() { if (options.Backend == TimerBackend.Rtss && Visible) Refresh(); else if (window != null) window.KeepOnTop(); }
        private void DisplaysChanged(object sender, EventArgs e)
        {
            if (!disposed && dispatcher != null && dispatcher.IsHandleCreated && !dispatcher.IsDisposed)
                try { dispatcher.BeginInvoke((MethodInvoker)delegate { lastText = null; Refresh(); }); } catch (InvalidOperationException) { }
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            displayTimer.Stop(); displayTimer.Tick -= Tick; displayTimer.Dispose(); SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
            if (window != null) { window.Dispose(); window = null; }
            if (rtss != null) { rtss.Dispose(); rtss = null; }
            if (frames != null) { frames.Dispose(); frames = null; }
        }
    }
}
