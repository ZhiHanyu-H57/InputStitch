using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace InputStitch
{
    // A small, non-activating per-pixel-alpha window. No game hooks or input output.
    internal sealed class CrosshairOverlay : NativeWindow, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { internal int X, Y; internal NativePoint(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { internal int Width, Height; internal NativeSize(int width, int height) { Width = width; Height = height; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { internal byte Operation, Flags, Alpha, Format; }
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [StructLayout(LayoutKind.Sequential)] private struct BitmapHeader
        { internal uint Size; internal int Width, Height; internal ushort Planes, Bits; internal uint Compression, ImageSize; internal int XResolution, YResolution; internal uint ColorsUsed, ColorsImportant; }
        [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapHeader header, uint usage, out IntPtr pixels, IntPtr section, uint offset);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destination, ref NativePoint position,
            ref NativeSize size, IntPtr source, ref NativePoint origin, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out RectangleNative rectangle);
        [StructLayout(LayoutKind.Sequential)] private struct RectangleNative { internal int Left, Top, Right, Bottom; }

        internal CrosshairOverlay()
        {
            CreateHandle(new CreateParams { Caption = "InputStitch Crosshair", Style = unchecked((int)0x80000000),
                ExStyle = 0x80000 | 0x20 | 0x80 | 0x08000000, Width = 1, Height = 1 });
        }
        internal bool Visible { get { return Handle != IntPtr.Zero && IsWindowVisible(Handle); } }
        internal Rectangle Bounds
        {
            get { RectangleNative rect; return GetWindowRect(Handle, out rect) ? Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom) : Rectangle.Empty; }
        }
        internal int Left { get { return Bounds.Left; } }
        internal int Top { get { return Bounds.Top; } }
        internal int Width { get { return Bounds.Width; } }
        internal int Height { get { return Bounds.Height; } }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; } // HTTRANSPARENT
            if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
            if (m.Msg == 0x2e0) { m.Result = IntPtr.Zero; return; } // Size/offsets are physical pixels, independent of monitor DPI.
            base.WndProc(ref m);
        }
        internal void Present(CrosshairOptions options, Rectangle screen)
        {
            using (Bitmap bitmap = CrosshairRenderer.Render(options))
            {
                Point center = CrosshairRenderer.Center(screen, options);
                NativePoint position = new NativePoint(center.X - bitmap.Width / 2, center.Y - bitmap.Height / 2);
                NativePoint origin = new NativePoint(0, 0);
                NativeSize size = new NativeSize(bitmap.Width, bitmap.Height);
                Blend blend = new Blend { Alpha = (byte)((options.Opacity * 255 + 50) / 100), Format = 1 };
                SetWindowPos(Handle, new IntPtr(-1), position.X, position.Y, bitmap.Width, bitmap.Height, 0x50);
                IntPtr dc = GetDC(IntPtr.Zero), memory = IntPtr.Zero, hbitmap = IntPtr.Zero, previous = IntPtr.Zero;
                try
                {
                    memory = CreateCompatibleDC(dc);
                    BitmapHeader header = new BitmapHeader { Size = 40, Width = bitmap.Width, Height = -bitmap.Height, Planes = 1, Bits = 32 };
                    IntPtr pixels;
                    hbitmap = CreateDIBSection(memory, ref header, 0, out pixels, IntPtr.Zero, 0);
                    if (hbitmap == IntPtr.Zero) throw new Win32Exception("Crosshair alpha surface allocation failed.");
                    BitmapData data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                    try
                    {
                        byte[] bytes = new byte[bitmap.Width * bitmap.Height * 4];
                        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                        Marshal.Copy(bytes, 0, pixels, bytes.Length);
                    }
                    finally { bitmap.UnlockBits(data); }
                    previous = SelectObject(memory, hbitmap);
                    if (!UpdateLayeredWindow(Handle, dc, ref position, ref size, memory, ref origin, 0, ref blend, 2))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Crosshair overlay rendering failed.");
                }
                finally
                {
                    if (previous != IntPtr.Zero) SelectObject(memory, previous);
                    if (hbitmap != IntPtr.Zero) DeleteObject(hbitmap);
                    if (memory != IntPtr.Zero) DeleteDC(memory);
                    if (dc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, dc);
                }
            }
        }
        internal void KeepOnTop() { if (Visible) SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x13); }
        internal void Hide() { if (Handle != IntPtr.Zero) ShowWindow(Handle, 0); }
        public void Dispose() { if (Handle != IntPtr.Zero) DestroyHandle(); }
    }

    internal sealed class CrosshairController : IDisposable
    {
        private readonly Control dispatcher;
        private CrosshairOverlay window;
        private CrosshairOptions options = new CrosshairOptions();
        private bool disposed;
        internal bool Visible { get { return window != null && window.Visible; } }
        internal CrosshairOverlay Window { get { return window; } }
        internal CrosshairController(Control owner)
        {
            dispatcher = owner;
            SystemEvents.DisplaySettingsChanged += DisplaysChanged;
        }
        internal void Apply(CrosshairOptions value)
        {
            if (disposed) return;
            options = (value ?? new CrosshairOptions()).Clone(); options.Normalize();
            Refresh();
        }
        internal void Refresh()
        {
            if (disposed) return;
            if (!options.Enabled || !options.Visible || options.Opacity == 0) { if (window != null) window.Hide(); return; }
            Screen selected = null;
            foreach (Screen screen in Screen.AllScreens)
                if (string.Equals(screen.DeviceName, options.MonitorDevice, StringComparison.OrdinalIgnoreCase)) selected = screen;
            if (selected == null) selected = Screen.PrimaryScreen;
            if (selected == null) return;
            if (window == null) window = new CrosshairOverlay();
            window.Present(options, selected.Bounds);
        }
        internal void KeepOnTop() { if (window != null) window.KeepOnTop(); }
        private void DisplaysChanged(object sender, EventArgs e)
        {
            if (!disposed && dispatcher.IsHandleCreated && !dispatcher.IsDisposed)
                try { dispatcher.BeginInvoke((MethodInvoker)Refresh); } catch (InvalidOperationException) { }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
            if (window != null) { window.Dispose(); window = null; }
        }
    }
}
