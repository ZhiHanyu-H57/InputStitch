using System;
using System.ComponentModel;
using System.IO;
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

        public NetworkControlOptions Clone()
        {
            NetworkControlOptions copy = (NetworkControlOptions)MemberwiseClone();
            copy.Hotkey = Hotkey == null ? null : Hotkey.Clone();
            return copy;
        }

        internal void Normalize()
        {
            if (!Enum.IsDefined(typeof(NetworkBlockMode), Mode)) Mode = NetworkBlockMode.Executable;
            ExecutablePath = ExecutablePath ?? "";
            if (Hotkey != null && (Hotkey.Kind != InputKind.Keyboard || Hotkey.VirtualKey < 1 || Hotkey.VirtualKey > 254 ||
                ModifierSafetyPolicy.IsModifierVirtualKey(Hotkey.VirtualKey))) Hotkey = null;
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
                lock (state) { if (disposed) return false; }
                if (IsBlocked) return RestoreCore();
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
