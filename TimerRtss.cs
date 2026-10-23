using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace InputStitch
{
    internal interface ITimerRtssMemory : IDisposable
    {
        long Length { get; }
        int ReadInt32(int offset);
        void WriteInt32(int offset, int value);
        byte[] Read(int offset, int count);
        void Write(int offset, byte[] bytes);
        bool TryAcquire();
        void Release();
    }

    // The official RTSS shared-memory interface, without a native DLL dependency.
    internal sealed class TimerRtssMemory : ITimerRtssMemory
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenFileMapping(uint access, bool inherit, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr MapViewOfFile(IntPtr mapping, uint access, uint high, uint low, UIntPtr length);
        [DllImport("kernel32.dll")] private static extern bool UnmapViewOfFile(IntPtr view);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] private static extern UIntPtr VirtualQuery(IntPtr address, out MemoryInfo info, UIntPtr length);
        [StructLayout(LayoutKind.Sequential)] private struct MemoryInfo
        { internal IntPtr BaseAddress, AllocationBase; internal uint AllocationProtect; internal UIntPtr RegionSize; internal uint State, Protect, Type; }
        private delegate int CompareExchange(IntPtr address, int value, int comparand);
        private static readonly CompareExchange Atomic = CreateAtomic();
        private IntPtr mapping, view;
        private long length;
        private static CompareExchange CreateAtomic()
        {
            // Emit the same Interlocked instruction used by the SDK. This is a local JIT helper,
            // not code injected into another process, and avoids enabling /unsafe for the application.
            DynamicMethod method = new DynamicMethod("TimerRtssCompareExchange", typeof(int), new Type[] { typeof(IntPtr), typeof(int), typeof(int) }, typeof(TimerRtssMemory), true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Conv_U); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, typeof(Interlocked).GetMethod("CompareExchange", new Type[] { typeof(int).MakeByRefType(), typeof(int), typeof(int) }));
            il.Emit(OpCodes.Ret);
            return (CompareExchange)method.CreateDelegate(typeof(CompareExchange));
        }
        internal static ITimerRtssMemory Open()
        {
            TimerRtssMemory memory = new TimerRtssMemory();
            memory.mapping = OpenFileMapping(0x000f001f, false, "RTSSSharedMemoryV2");
            if (memory.mapping == IntPtr.Zero) { if (Marshal.GetLastWin32Error() == 5) throw new UnauthorizedAccessException(); return null; }
            memory.view = MapViewOfFile(memory.mapping, 0x000f001f, 0, 0, UIntPtr.Zero);
            if (memory.view == IntPtr.Zero) { memory.Dispose(); return null; }
            MemoryInfo info;
            if (VirtualQuery(memory.view, out info, (UIntPtr)Marshal.SizeOf(typeof(MemoryInfo))) == UIntPtr.Zero) { memory.Dispose(); return null; }
            memory.length = (long)info.RegionSize.ToUInt64();
            return memory;
        }
        public long Length { get { return length; } }
        private void Check(int offset, int count)
        { if (view == IntPtr.Zero || offset < 0 || count < 0 || (long)offset + count > length) throw new InvalidDataException("Invalid RTSS shared-memory boundary."); }
        public int ReadInt32(int offset) { Check(offset, 4); return Marshal.ReadInt32(view, offset); }
        public void WriteInt32(int offset, int value) { Check(offset, 4); Marshal.WriteInt32(view, offset, value); }
        public byte[] Read(int offset, int count) { Check(offset, count); byte[] result = new byte[count]; Marshal.Copy(IntPtr.Add(view, offset), result, 0, count); return result; }
        public void Write(int offset, byte[] bytes) { Check(offset, bytes.Length); Marshal.Copy(bytes, 0, IntPtr.Add(view, offset), bytes.Length); }
        public bool TryAcquire()
        {
            Check(36, 4);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int current = Marshal.ReadInt32(view, 36); if ((current & 1) != 0) return false;
                if (Atomic(IntPtr.Add(view, 36), current | 1, current) == current) return true;
            }
            return false;
        }
        public void Release()
        {
            Check(36, 4);
            while (true)
            {
                int current = Marshal.ReadInt32(view, 36); if ((current & 1) == 0) return;
                if (Atomic(IntPtr.Add(view, 36), current & ~1, current) == current) return;
            }
        }
        public void Dispose()
        { if (view != IntPtr.Zero) { UnmapViewOfFile(view); view = IntPtr.Zero; } if (mapping != IntPtr.Zero) { CloseHandle(mapping); mapping = IntPtr.Zero; } length = 0; }
    }

    internal sealed class TimerGlyphAtlas : IDisposable
    {
        private sealed class Glyph { internal Rectangle Source; internal float Advance; internal int ImageIndex; }
        private readonly Dictionary<string, Glyph> glyphs = new Dictionary<string, Glyph>(StringComparer.Ordinal);
        private readonly string directory;
        internal string FilePath { get; private set; }
        internal float LineHeight { get; private set; }
        private readonly TimerOptions options;
        internal TimerGlyphAtlas(TimerOptions source, string root)
        {
            options = source.Clone(); options.Normalize();
            directory = Path.Combine(root ?? Path.GetTempPath(), "InputStitch.Timer." + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); FilePath = Path.Combine(directory, "glyphs.png");
            List<Bitmap> images = new List<Bitmap>();
            try
            {
                string characters = "0123456789:. -" + TimerText.Format(TimeSpan.Zero, options);
                TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(characters);
                int atlasWidth = 1024, x = 0, y = 0, rowHeight = 0;
                FontStyle style = (options.Bold ? FontStyle.Bold : FontStyle.Regular) | (options.Italic ? FontStyle.Italic : FontStyle.Regular);
                using (Font font = TimerRenderer.CreateFont(options.FontFamily, options.FontSize, style))
                using (StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
                using (Bitmap probe = new Bitmap(1, 1))
                using (Graphics measure = Graphics.FromImage(probe))
                {
                    format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;
                    LineHeight = font.GetHeight(96);
                    while (elements.MoveNext())
                    {
                        string element = elements.GetTextElement(); if (element == "\n" || glyphs.ContainsKey(element)) continue;
                        Bitmap bitmap = TimerRenderer.Render(options, element); images.Add(bitmap);
                        if (x + bitmap.Width + 14 > atlasWidth) { x = 0; y += rowHeight; rowHeight = 0; }
                        if (y + bitmap.Height + 14 > 8192) throw new InvalidOperationException("Timer glyph atlas is too large.");
                        glyphs.Add(element, new Glyph { Source = new Rectangle(x + 7, y + 7, bitmap.Width, bitmap.Height), Advance = measure.MeasureString(element, font, int.MaxValue, format).Width, ImageIndex = images.Count - 1 });
                        x += bitmap.Width + 14; rowHeight = Math.Max(rowHeight, bitmap.Height + 14);
                    }
                }
                int textureHeight = 512; while (textureHeight < y + rowHeight) textureHeight *= 2;
                using (Bitmap atlas = new Bitmap(atlasWidth, textureHeight, PixelFormat.Format32bppPArgb))
                using (Graphics graphics = Graphics.FromImage(atlas))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                    ColorMatrix matrix = new ColorMatrix(); matrix.Matrix33 = options.Opacity / 100f; attributes.SetColorMatrix(matrix);
                    foreach (KeyValuePair<string, Glyph> item in glyphs)
                    {
                        Bitmap bitmap = images[item.Value.ImageIndex];
                        graphics.DrawImage(bitmap, item.Value.Source, 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                    }
                    atlas.Save(FilePath, ImageFormat.Png);
                }
            }
            catch { Dispose(); throw; }
            finally { foreach (Bitmap image in images) image.Dispose(); }
        }
        internal string Hypertext(string text) { return Hypertext(text, 1); }
        internal string Hypertext(string text, int zoom)
        {
            StringBuilder tags = new StringBuilder();
            tags.Append("<LI=\"").Append(FilePath).Append("\"><C=FFFFFF><A=0><S=100>");
            float x = options.X, y = options.Y;
            int layer = 0;
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text ?? "");
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (element == "\n") { x = options.X; y += LineHeight; continue; }
                Glyph glyph; if (!glyphs.TryGetValue(element, out glyph)) throw new InvalidDataException("A timer glyph is absent from the atlas.");
                Rectangle rect = glyph.Source;
                int physicalX = (int)Math.Round(x), physicalY = (int)Math.Round(y);
                int cellX = (int)Math.Floor((double)physicalX / zoom), cellY = (int)Math.Floor((double)physicalY / zoom);
                int phaseX = physicalX - cellX * zoom, phaseY = physicalY - cellY * zoom;
                int sourceWidth = ((rect.Width + phaseX + zoom - 1) / zoom) * zoom, sourceHeight = ((rect.Height + phaseY + zoom - 1) / zoom) * zoom;
                tags.Append("<P=").Append(cellX.ToString(CultureInfo.InvariantCulture)).Append(',').Append(cellY.ToString(CultureInfo.InvariantCulture)).Append('>');
                tags.Append("<L").Append(layer++).Append('>');
                tags.Append("<I=").Append(sourceWidth / zoom).Append(',').Append(sourceHeight / zoom).Append(',').Append(rect.X - phaseX).Append(',').Append(rect.Y - phaseY).Append(',').Append(sourceWidth).Append(',').Append(sourceHeight).Append('>');
                x += glyph.Advance + options.LetterSpacing;
            }
            return tags.Append("<S><A><C>").ToString();
        }
        internal void SetPosition(int x, int y) { options.X = x; options.Y = y; }
        public void Dispose()
        {
            try { if (FilePath != null && File.Exists(FilePath)) File.Delete(FilePath); if (Directory.Exists(directory)) Directory.Delete(directory, false); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    internal sealed class TimerRtss : IDisposable
    {
        private readonly TimerOptions options;
        private readonly Func<ITimerRtssMemory> opener;
        private readonly string atlasRoot;
        private readonly string owner = "InputStitch.Timer." + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + "." + Guid.NewGuid().ToString("N");
        private ITimerRtssMemory memory;
        private TimerGlyphAtlas atlas;
        private int slot = -1;
        private bool disposed;
        internal string Status { get; private set; }
        internal string AtlasPath { get { return atlas == null ? null : atlas.FilePath; } }
        internal TimerRtss(TimerOptions value) : this(value, TimerRtssMemory.Open, null) { }
        internal TimerRtss(TimerOptions value, Func<ITimerRtssMemory> factory, string root)
        { options = value.Clone(); options.Normalize(); opener = factory; atlasRoot = root; Status = ""; }
        internal void UpdatePlacement(TimerOptions value)
        {
            options.X = value.X; options.Y = value.Y; options.MonitorDevice = value.MonitorDevice;
            options.Enabled = value.Enabled; options.Visible = value.Visible;
            if (atlas != null) atlas.SetPosition(options.X, options.Y);
        }
        private bool Layout(out int offset, out int size, out int count, out int version)
        {
            offset = size = count = version = 0;
            if (memory == null || memory.Length < 40 || memory.ReadInt32(0) != 0x52545353) { Status = "RtssUnavailable"; return false; }
            version = memory.ReadInt32(4);
            if (version < 0x00020015 || (version >> 16) != 2) { Status = "RtssUnsupported"; return false; }
            size = memory.ReadInt32(20); offset = memory.ReadInt32(24); count = memory.ReadInt32(28);
            if (size < 4608 || size > 1048576 || offset < 40 || count < 2 || count > 1024 || (long)offset + (long)count * size > memory.Length)
            { Status = "RtssInvalidMemory"; return false; }
            return true;
        }
        private int ReadZoom()
        {
            if (memory.Length < 72) return 1;
            int size = memory.ReadInt32(8), offset = memory.ReadInt32(12), count = memory.ReadInt32(16), index = memory.ReadInt32(64);
            if (size < 328 || size > 1048576 || offset < 40 || count < 1 || count > 4096 || index < 0 || index >= count || (long)offset + (long)count * size > memory.Length) return 1;
            int zoom = memory.ReadInt32(offset + index * size + 324);
            return zoom >= 1 && zoom <= 8 ? zoom : 1;
        }
        private string ReadOwner(int entry)
        { byte[] bytes = memory.Read(entry + 256, 256); int length = Array.IndexOf(bytes, (byte)0); return Encoding.ASCII.GetString(bytes, 0, length < 0 ? bytes.Length : length); }
        private bool IsStaleOwner(string value)
        {
            const string prefix = "InputStitch.Timer.";
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            int separator = value.IndexOf('.', prefix.Length), pid;
            if (separator < 0 || !int.TryParse(value.Substring(prefix.Length, separator - prefix.Length), out pid) || pid <= 0) return false;
            try { using (Process process = Process.GetProcessById(pid)) return process.HasExited; }
            catch (ArgumentException) { return true; }
            catch (InvalidOperationException) { return false; }
        }
        internal bool Present(string text)
        {
            if (disposed) return false;
            try
            {
                if (memory == null) memory = opener();
                if (memory == null) { Status = "RtssNotRunning"; return false; }
                int offset, size, count, version;
                if (!Layout(out offset, out size, out count, out version)) { memory.Dispose(); memory = null; slot = -1; return false; }
                if (atlas == null) atlas = new TimerGlyphAtlas(options, atlasRoot);
                string hypertext = atlas.Hypertext(text, ReadZoom());
                Encoding encoding = (Encoding)Encoding.Default.Clone(); encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
                byte[] payload = encoding.GetBytes(hypertext);
                int textOffset = 512, capacity = 4096;
                if (version >= 0x00020014 && size >= 299520) { textOffset = 266752; capacity = 32768; }
                if (payload.Length >= capacity) { Status = "RtssTextTooLong"; return false; }
                if (!memory.TryAcquire()) { Status = "RtssBusy"; return false; }
                try
                {
                    if (slot < 1 || slot >= count || ReadOwner(offset + slot * size) != owner)
                    {
                        slot = -1;
                        for (int index = 1; index < count; index++)
                        {
                            string existing = ReadOwner(offset + index * size);
                            if (existing.Length == 0 || IsStaleOwner(existing)) { slot = index; break; }
                        }
                        if (slot < 0) { Status = "RtssNoSlot"; return false; }
                        byte[] identity = new byte[256]; Encoding.ASCII.GetBytes(owner).CopyTo(identity, 0); memory.Write(offset + slot * size + 256, identity);
                    }
                    int entry = offset + slot * size;
                    memory.WriteInt32(entry, 0); memory.WriteInt32(entry + 512, 0);
                    if (version >= 0x00020014 && size >= 299520) memory.WriteInt32(entry + 266752, 0);
                    byte[] terminated = new byte[payload.Length + 1]; payload.CopyTo(terminated, 0); memory.Write(entry + textOffset, terminated);
                    memory.WriteInt32(32, unchecked(memory.ReadInt32(32) + 1));
                    Status = ""; return true;
                }
                finally { memory.Release(); }
            }
            catch (EncoderFallbackException) { Status = "RtssPathEncoding"; return false; }
            catch (UnauthorizedAccessException) { Status = "RtssAccessDenied"; return false; }
            catch (IOException) { Status = "RtssIoError"; return false; }
            catch (ArgumentException) { Status = "RtssRenderError"; return false; }
            catch (InvalidOperationException) { Status = "RtssRenderError"; return false; }
        }
        internal bool Hide()
        {
            if (memory == null || slot < 1) return true;
            int offset, size, count, version;
            if (!Layout(out offset, out size, out count, out version) || slot >= count) { slot = -1; return true; }
            for (int attempt = 0; attempt < 10; attempt++)
            {
                if (!memory.TryAcquire()) { Thread.Sleep(1); continue; }
                try
                {
                    int entry = offset + slot * size;
                    if (ReadOwner(entry) == owner)
                    {
                        memory.WriteInt32(entry, 0); memory.WriteInt32(entry + 512, 0);
                        if (version >= 0x00020014 && size >= 299520) memory.WriteInt32(entry + 266752, 0);
                        memory.Write(entry + 256, new byte[256]); memory.WriteInt32(32, unchecked(memory.ReadInt32(32) + 1));
                    }
                    slot = -1; Status = ""; return true;
                }
                finally { memory.Release(); }
            }
            Status = "RtssBusy"; return false;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            try { Hide(); }
            finally
            {
                if (memory != null) { memory.Dispose(); memory = null; }
                if (atlas != null) { atlas.Dispose(); atlas = null; }
            }
        }
    }
}
