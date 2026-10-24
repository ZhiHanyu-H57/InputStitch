using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace InputStitch
{
    public enum NetworkBlockMode { EntireComputer, Executable }

    [Serializable]
    public sealed class NetworkControlOptions
    {
        // Permission to use the shortcut, never a request to disconnect on startup.
        public bool Enabled;
        public NetworkBlockMode Mode = NetworkBlockMode.Executable;
        public string ExecutablePath = "";
        [XmlIgnore] public List<string> ExecutablePaths;
        [XmlArray("ExecutablePaths"), XmlArrayItem("Path")]
        public string[] SerializedExecutablePaths
        {
            get { return ExecutablePaths == null ? null : ExecutablePaths.ToArray(); }
            set { ExecutablePaths = value == null ? null : new List<string>(value); }
        }
        public bool ShouldSerializeSerializedExecutablePaths() { return ExecutablePaths != null; }
        [XmlElement(IsNullable = true)]
        public TriggerSpec Hotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec BlockHotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec RestoreHotkey;
        public bool BlockSound = true;
        public bool RestoreSound = true;
        [DefaultValue("")] public string BlockSoundPath = "";
        [DefaultValue("")] public string RestoreSoundPath = "";
        [DefaultValue(100)] public int BlockSoundVolume = 100;
        [DefaultValue(100)] public int RestoreSoundVolume = 100;

        internal TriggerSpec BlockShortcut { get { return BlockHotkey ?? (RestoreHotkey == null ? Hotkey : null); } }
        internal TriggerSpec RestoreShortcut { get { return RestoreHotkey ?? (BlockHotkey == null ? Hotkey : null); } }

        public NetworkControlOptions Clone()
        {
            NetworkControlOptions copy = (NetworkControlOptions)MemberwiseClone();
            copy.Hotkey = Hotkey == null ? null : Hotkey.Clone();
            copy.BlockHotkey = BlockHotkey == null ? null : BlockHotkey.Clone();
            copy.RestoreHotkey = RestoreHotkey == null ? null : RestoreHotkey.Clone();
            copy.ExecutablePaths = ExecutablePaths == null ? null : new List<string>(ExecutablePaths);
            return copy;
        }

        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(NetworkBlockMode), Mode)) Mode = NetworkBlockMode.Executable;
            ExecutablePath = ExecutablePath ?? "";
            if (ExecutablePaths != null && ExecutablePaths.Count > 0) ExecutablePath = ExecutablePaths[0] ?? "";
            BlockSoundPath = BlockSoundPath ?? ""; RestoreSoundPath = RestoreSoundPath ?? "";
            BlockSoundVolume = Math.Max(0, Math.Min(100, BlockSoundVolume)); RestoreSoundVolume = Math.Max(0, Math.Min(100, RestoreSoundVolume));
            if (!ValidHotkey(Hotkey)) Hotkey = null;
            if (!ValidHotkey(BlockHotkey)) BlockHotkey = null;
            if (!ValidHotkey(RestoreHotkey)) RestoreHotkey = null;
        }
        internal string[] Targets()
        { return ExecutablePaths == null ? (string.IsNullOrWhiteSpace(ExecutablePath) ? new string[0] : new[] { ExecutablePath }) : ExecutablePaths.ToArray(); }
        internal bool SameTargets(NetworkControlOptions other)
        {
            if (other == null || Mode != other.Mode) return false;
            if (Mode == NetworkBlockMode.EntireComputer) return true;
            HashSet<string> a = new HashSet<string>(StringComparer.OrdinalIgnoreCase), b = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Targets()) a.Add((path ?? "").Trim()); foreach (string path in other.Targets()) b.Add((path ?? "").Trim());
            return a.SetEquals(b);
        }
        private static bool ValidHotkey(TriggerSpec key)
        { return key == null || (key.Kind == InputKind.Keyboard && key.VirtualKey > 0 && key.VirtualKey < 255 && !ModifierSafetyPolicy.IsModifierVirtualKey(key.VirtualKey)); }
    }

    internal enum NetworkSoundKind { Blocked, Restored }
    internal sealed class NetworkSoundCue
    {
        internal readonly NetworkSoundKind Kind;
        internal readonly string Path;
        internal readonly int Volume;
        internal NetworkSoundCue(NetworkSoundKind kind, string path, int volume) { Kind = kind; Path = path ?? ""; Volume = Math.Max(0, Math.Min(100, volume)); }
        internal static NetworkSoundCue From(NetworkSoundKind kind, NetworkControlOptions options)
        { return new NetworkSoundCue(kind, kind == NetworkSoundKind.Blocked ? options.BlockSoundPath : options.RestoreSoundPath, kind == NetworkSoundKind.Blocked ? options.BlockSoundVolume : options.RestoreSoundVolume); }
    }

    // State transitions drive cues; repeated commands and UI refreshes are silent.
    internal sealed class NetworkStateSounds
    {
        private readonly object gate = new object();
        private readonly Action<NetworkSoundCue> output;
        private bool lastBlocked;
        internal NetworkStateSounds(Action<NetworkSoundCue> play) { output = play; }
        internal void Observe(bool blocked, NetworkControlOptions options)
        {
            lock (gate)
            {
                if (lastBlocked == blocked) return;
                lastBlocked = blocked;
                if (options != null && (blocked ? options.BlockSound : options.RestoreSound) && output != null)
                    try { output(NetworkSoundCue.From(blocked ? NetworkSoundKind.Blocked : NetworkSoundKind.Restored, options)); } catch (Exception) { }
            }
        }
    }

    internal static class NetworkSounds
    {
        private static readonly NetworkCuePlayer player = new NetworkCuePlayer(new NativeNetworkAudio(), BuiltinPath);
        private static readonly string soundDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InputStitch-cues-" + System.Diagnostics.Process.GetCurrentProcess().Id);
        static NetworkSounds()
        {
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                player.Dispose();
                try
                {
                    foreach (NetworkSoundKind kind in new[] { NetworkSoundKind.Blocked, NetworkSoundKind.Restored })
                    { string file = System.IO.Path.Combine(soundDirectory, kind + ".wav"); if (File.Exists(file)) File.Delete(file); }
                    if (Directory.Exists(soundDirectory)) Directory.Delete(soundDirectory);
                }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
            };
        }
        internal static void Play(NetworkSoundKind kind)
        { Play(new NetworkSoundCue(kind, "", 100)); }
        internal static void Play(NetworkSoundCue cue)
        { Preview(cue, null); }
        internal static void Preview(NetworkSoundCue cue, Action<string> completed)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error = ""; try { player.Play(cue); } catch (Exception ex) { error = ex.Message; }
                if (completed != null) try { completed(error); } catch (Exception) { }
            });
        }
        private static string BuiltinPath(NetworkSoundKind kind)
        {
            Directory.CreateDirectory(soundDirectory); string file = System.IO.Path.Combine(soundDirectory, kind + ".wav");
            if (!File.Exists(file)) File.WriteAllBytes(file, Wave(kind)); return file;
        }
        internal static byte[] Wave(NetworkSoundKind kind)
        {
            const int rate = 16000, tone = 960, gap = 160, samples = tone * 2 + gap;
            using (MemoryStream stream = new MemoryStream(44 + samples * 2))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    bool second = i >= tone + gap; int position = second ? i - tone - gap : i;
                    double value = 0;
                    if (position >= 0 && position < tone)
                    {
                        double frequency = (kind == NetworkSoundKind.Blocked) != second ? 760 : 450;
                        double envelope = Math.Min(1, Math.Min(position, tone - 1 - position) / 80.0);
                        value = Math.Sin(2 * Math.PI * frequency * position / rate) * envelope * 4200;
                    }
                    writer.Write((short)value);
                }
                return stream.ToArray();
            }
        }
    }

    internal interface INetworkAudio
    {
        string Open(string path);
        void Volume(string alias, int volume);
        int Volume(string alias);
        void Play(string alias);
        string Mode(string alias);
        void Close(string alias);
    }

    // Only two short-lived cue slots are retained; the timer sleeps when playback ends.
    internal sealed class NetworkCuePlayer : IDisposable
    {
        private readonly object gate = new object();
        private readonly INetworkAudio audio;
        private readonly Func<NetworkSoundKind, string> builtin;
        private readonly Dictionary<NetworkSoundKind, string> active = new Dictionary<NetworkSoundKind, string>();
        private readonly Timer timer;
        private bool disposed;
        internal NetworkCuePlayer(INetworkAudio audio, Func<NetworkSoundKind, string> builtin)
        { this.audio = audio; this.builtin = builtin; timer = new Timer(delegate { Collect(); }, null, Timeout.Infinite, Timeout.Infinite); }
        internal int ActiveCount { get { lock (gate) return active.Count; } }
        internal int Volume(NetworkSoundKind kind) { lock (gate) return active.ContainsKey(kind) ? audio.Volume(active[kind]) : -1; }
        internal void Play(NetworkSoundCue cue)
        {
            if (cue == null) return;
            lock (gate)
            {
                if (disposed) return;
                Close(cue.Kind);
                if (cue.Volume == 0) return;
                string path = string.IsNullOrEmpty(cue.Path) ? builtin(cue.Kind) : cue.Path;
                ValidateFile(path);
                string alias = audio.Open(System.IO.Path.GetFullPath(path));
                try { audio.Volume(alias, cue.Volume * 10); audio.Play(alias); active[cue.Kind] = alias; }
                catch { try { audio.Close(alias); } catch (Exception) { } throw; }
                timer.Change(100, 100);
            }
        }
        internal static void ValidateFile(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(path) || !System.IO.Path.IsPathRooted(path) || !File.Exists(path) ||
                !string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose an existing PCM WAV file.");
        }
        private void Close(NetworkSoundKind kind)
        {
            string alias;
            if (!active.TryGetValue(kind, out alias)) return;
            active.Remove(kind); try { audio.Close(alias); } catch (Exception) { }
        }
        internal void Collect()
        {
            lock (gate)
            {
                if (disposed) return;
                foreach (NetworkSoundKind kind in new[] { NetworkSoundKind.Blocked, NetworkSoundKind.Restored })
                {
                    string alias; if (!active.TryGetValue(kind, out alias)) continue;
                    string mode = ""; try { mode = audio.Mode(alias); } catch (Exception) { }
                    if (!string.Equals(mode, "playing", StringComparison.OrdinalIgnoreCase) && !string.Equals(mode, "seeking", StringComparison.OrdinalIgnoreCase)) Close(kind);
                }
                if (active.Count == 0) timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return; disposed = true; timer.Dispose();
                Close(NetworkSoundKind.Blocked); Close(NetworkSoundKind.Restored);
            }
        }
    }

    internal sealed class NativeNetworkAudio : INetworkAudio
    {
        private sealed class Cue : IDisposable
        {
            internal byte[] Wave; internal int Gain = 1000; internal int Duration; internal long Until; internal SoundPlayer Player; internal MemoryStream Stream;
            public void Dispose() { if (Player != null) Player.Dispose(); if (Stream != null) Stream.Dispose(); }
        }
        private readonly Dictionary<string, Cue> cues = new Dictionary<string, Cue>();
        private string current;
        internal int OpenCount { get { return cues.Count; } }
        public string Open(string path)
        {
            byte[] wave = File.ReadAllBytes(path); int start, count, bits, bytesPerSecond; Parse(wave, out start, out count, out bits, out bytesPerSecond);
            using (MemoryStream stream = new MemoryStream(wave, false)) using (SoundPlayer parser = new SoundPlayer(stream)) parser.Load();
            string alias = "isnet" + Guid.NewGuid().ToString("N"); cues.Add(alias, new Cue { Wave = wave, Duration = Math.Max(1, (int)Math.Ceiling(count * 1000.0 / bytesPerSecond)) }); return alias;
        }
        public void Volume(string alias, int volume) { cues[alias].Gain = Math.Max(0, Math.Min(1000, volume)); }
        public int Volume(string alias) { return cues[alias].Gain; }
        public void Play(string alias)
        {
            Cue cue = cues[alias]; cue.Stream = new MemoryStream(Scale(cue.Wave, cue.Gain), false); cue.Player = new SoundPlayer(cue.Stream); cue.Player.Load();
            cue.Player.Play(); current = alias; cue.Until = System.Diagnostics.Stopwatch.GetTimestamp() + (long)Math.Ceiling(cue.Duration * (double)System.Diagnostics.Stopwatch.Frequency / 1000);
        }
        public string Mode(string alias) { return current == alias && System.Diagnostics.Stopwatch.GetTimestamp() < cues[alias].Until ? "playing" : "stopped"; }
        public void Close(string alias)
        {
            Cue cue; if (!cues.TryGetValue(alias, out cue)) return;
            cues.Remove(alias); if (current == alias) { if (cue.Player != null) cue.Player.Stop(); current = null; } cue.Dispose();
        }
        internal static byte[] Scale(byte[] source, int gain)
        {
            int offset, count, bits, rate; Parse(source, out offset, out count, out bits, out rate);
            byte[] wave = (byte[])source.Clone(); gain = Math.Max(0, Math.Min(1000, gain));
            for (int i = offset; i < offset + count; i += bits / 8)
            {
                if (bits == 8) wave[i] = (byte)(128 + (wave[i] - 128) * gain / 1000);
                else { short value = (short)(BitConverter.ToInt16(wave, i) * gain / 1000); wave[i] = (byte)value; wave[i + 1] = (byte)(value >> 8); }
            }
            return wave;
        }
        private static void Parse(byte[] wave, out int data, out int count, out int bits, out int bytesPerSecond)
        {
            data = count = bits = bytesPerSecond = 0; int channels = 0;
            if (wave == null || wave.Length < 44 || Encoding.ASCII.GetString(wave, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wave, 8, 4) != "WAVE") throw new ArgumentException("Choose an uncompressed 8-bit or 16-bit PCM WAV file.");
            for (int at = 12; at <= wave.Length - 8; )
            {
                uint size = BitConverter.ToUInt32(wave, at + 4); if (size > int.MaxValue || (long)at + 8 + size > wave.Length) throw new ArgumentException("Invalid WAV chunk length.");
                string kind = Encoding.ASCII.GetString(wave, at, 4);
                if (kind == "fmt ")
                {
                    if (size < 16 || BitConverter.ToInt16(wave, at + 8) != 1) throw new ArgumentException("Choose an uncompressed PCM WAV file.");
                    channels = BitConverter.ToInt16(wave, at + 10); bytesPerSecond = BitConverter.ToInt32(wave, at + 16); bits = BitConverter.ToInt16(wave, at + 22);
                }
                if (kind == "data") { data = at + 8; count = (int)size; }
                at = checked(at + 8 + (int)size + ((int)size & 1));
            }
            if ((bits != 8 && bits != 16) || channels < 1 || channels > 2 || bytesPerSecond < 1 || data == 0 || count == 0 || count % (channels * bits / 8) != 0)
                throw new ArgumentException("Choose an uncompressed mono/stereo 8-bit or 16-bit PCM WAV file.");
        }
    }
    internal interface INetworkHelperConnection : IDisposable
    {
        bool IsAlive { get; }
        event EventHandler Disconnected;
        string Request(string command);
    }

    internal interface INetworkHelperFactory
    {
        INetworkHelperConnection Start();
    }

    // Calls may wait for Windows' elevation dialog; invoke Toggle/Restore on a worker.
    // Dispose deliberately does not wait behind elevation or an outstanding request.
    internal sealed class NetworkController : IDisposable
    {
        private readonly object operation = new object();
        private readonly object state = new object();
        private readonly INetworkHelperFactory factory;
        private INetworkHelperConnection connection;
        private bool blocked;
        private bool disposed;
        private string lastError = "";

        internal NetworkController() : this(new NetworkHelperFactory()) { }
        internal NetworkController(INetworkHelperFactory factory)
        { if (factory == null) throw new ArgumentNullException("factory"); this.factory = factory; }

        public event EventHandler StateChanged;
        public bool IsBlocked
        {
            get { lock (state) return blocked && connection != null && connection.IsAlive; }
        }
        public string LastError { get { lock (state) return lastError; } }

        public bool Toggle(NetworkControlOptions options)
        {
            lock (operation)
            {
                if (IsBlocked) return RestoreCore();
                return BlockCore(options);
            }
        }
        public bool Block(NetworkControlOptions options)
        {
            lock (operation) return BlockCore(options);
        }
        private bool BlockCore(NetworkControlOptions options)
        {
            lock (state) { if (disposed) return false; }
            if (IsBlocked) return true;
            try
            {
                if (options == null || !options.Enabled) throw new InvalidOperationException("Network shortcut is disabled.");
                if (!Enum.IsDefined(typeof(NetworkBlockMode), options.Mode)) throw new ArgumentException("Invalid network scope.");
                string[] paths = NetworkPrivilegeHelper.ValidateExecutables(options.Mode, options.Targets());
                INetworkHelperConnection current;
                lock (state) current = connection;
                if (current == null || !current.IsAlive)
                {
                    if (current != null) ReleaseConnection(current);
                    current = factory.Start();
                    lock (state)
                    {
                        if (disposed) { current.Dispose(); return false; }
                        connection = current;
                        current.Disconnected += HelperDisconnected;
                    }
                }
                lock (state) { if (disposed) return false; }
                string command = paths.Length <= 1 ? "BLOCK\n" + ((int)options.Mode).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(paths.Length == 0 ? "" : paths[0])) : "BLOCKSET\n" + ((int)options.Mode).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                    string.Join("\n", Array.ConvertAll(paths, delegate(string p) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(p)); }));
                string response = current.Request(command);
                NetworkPrivilegeHelper.RequireResponse(response, "BLOCKED");
                lock (state)
                {
                    if (disposed || !ReferenceEquals(connection, current) || !current.IsAlive) return false;
                    blocked = true; lastError = "";
                }
                Notify();
                return true;
            }
            catch (Exception ex)
            {
                Fail(ex);
                return false;
            }
        }

        public bool Restore()
        {
            lock (operation) return RestoreCore();
        }

        private bool RestoreCore()
        {
            INetworkHelperConnection current;
            lock (state) current = connection;
            if (current == null || !current.IsAlive)
            {
                if (current != null) ReleaseConnection(current);
                lock (state) { blocked = false; lastError = ""; }
                Notify();
                return true;
            }
            try
            {
                NetworkPrivilegeHelper.RequireResponse(current.Request("RESTORE"), "RESTORED");
                lock (state) { blocked = false; lastError = ""; }
                Notify();
                return true;
            }
            catch (Exception ex) { Fail(ex); return false; }
        }

        private void Fail(Exception ex)
        {
            INetworkHelperConnection current;
            lock (state)
            {
                current = connection;
                connection = null; blocked = false;
                Win32Exception native = ex as Win32Exception;
                lastError = native != null && native.NativeErrorCode == 1223 ? "Administrator approval was cancelled." : ex.Message;
            }
            // Losing the pipe makes the helper close its dynamic WFP session.
            if (current != null) { current.Disconnected -= HelperDisconnected; current.Dispose(); }
            Notify();
        }

        private void HelperDisconnected(object sender, EventArgs args)
        {
            lock (state)
            {
                if (!ReferenceEquals(sender, connection)) return;
                blocked = false;
                lastError = "Network helper exited; its temporary block has been removed.";
            }
            Notify();
        }

        private void ReleaseConnection(INetworkHelperConnection current)
        {
            lock (state) { if (ReferenceEquals(connection, current)) { connection = null; blocked = false; } }
            current.Disconnected -= HelperDisconnected;
            current.Dispose();
        }

        private void Notify()
        {
            EventHandler handler = StateChanged;
            if (handler == null) return;
            // A view subscriber cannot change a successfully committed network transaction.
            foreach (EventHandler subscriber in handler.GetInvocationList())
                try { subscriber(this, EventArgs.Empty); } catch (Exception) { }
        }

        public void Dispose()
        {
            INetworkHelperConnection current;
            lock (state)
            {
                if (disposed) return;
                disposed = true; current = connection; connection = null; blocked = false;
            }
            if (current != null) { current.Disconnected -= HelperDisconnected; current.Dispose(); }
        }
    }
}
