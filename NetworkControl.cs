using System;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Text;
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
        [XmlElement(IsNullable = true)]
        public TriggerSpec Hotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec BlockHotkey;
        [XmlElement(IsNullable = true)] public TriggerSpec RestoreHotkey;
        public bool BlockSound = true;
        public bool RestoreSound = true;

        internal TriggerSpec BlockShortcut { get { return BlockHotkey ?? (RestoreHotkey == null ? Hotkey : null); } }
        internal TriggerSpec RestoreShortcut { get { return RestoreHotkey ?? (BlockHotkey == null ? Hotkey : null); } }

        public NetworkControlOptions Clone()
        {
            NetworkControlOptions copy = (NetworkControlOptions)MemberwiseClone();
            copy.Hotkey = Hotkey == null ? null : Hotkey.Clone();
            copy.BlockHotkey = BlockHotkey == null ? null : BlockHotkey.Clone();
            copy.RestoreHotkey = RestoreHotkey == null ? null : RestoreHotkey.Clone();
            return copy;
        }

        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(NetworkBlockMode), Mode)) Mode = NetworkBlockMode.Executable;
            ExecutablePath = ExecutablePath ?? "";
            if (!ValidHotkey(Hotkey)) Hotkey = null;
            if (!ValidHotkey(BlockHotkey)) BlockHotkey = null;
            if (!ValidHotkey(RestoreHotkey)) RestoreHotkey = null;
        }
        private static bool ValidHotkey(TriggerSpec key)
        { return key == null || (key.Kind == InputKind.Keyboard && key.VirtualKey > 0 && key.VirtualKey < 255 && !ModifierSafetyPolicy.IsModifierVirtualKey(key.VirtualKey)); }
    }

    internal enum NetworkSoundKind { Blocked, Restored }

    // State transitions drive cues; repeated commands and UI refreshes are silent.
    internal sealed class NetworkStateSounds
    {
        private readonly object gate = new object();
        private readonly Action<NetworkSoundKind> output;
        private bool lastBlocked;
        internal NetworkStateSounds(Action<NetworkSoundKind> play) { output = play; }
        internal void Observe(bool blocked, NetworkControlOptions options)
        {
            lock (gate)
            {
                if (lastBlocked == blocked) return;
                lastBlocked = blocked;
                if (options != null && (blocked ? options.BlockSound : options.RestoreSound) && output != null)
                    try { output(blocked ? NetworkSoundKind.Blocked : NetworkSoundKind.Restored); } catch (Exception) { }
            }
        }
    }

    internal static class NetworkSounds
    {
        private static readonly object gate = new object();
        private static SoundPlayer blocked, restored;
        internal static void Play(NetworkSoundKind kind)
        {
            lock (gate)
            {
                SoundPlayer player;
                if (kind == NetworkSoundKind.Blocked)
                    player = blocked ?? (blocked = new SoundPlayer(new MemoryStream(Wave(kind), false)));
                else player = restored ?? (restored = new SoundPlayer(new MemoryStream(Wave(kind), false)));
                // Playback is asynchronous; these two in-memory players live for the process.
                try { player.Play(); } catch (InvalidOperationException) { } catch (TimeoutException) { }
            }
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
                string path = NetworkPrivilegeHelper.ValidateExecutable(options.Mode, options.ExecutablePath);
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
                string response = current.Request("BLOCK\n" + ((int)options.Mode).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    "\n" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(path)));
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
