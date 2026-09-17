using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace InputStitch
{
    // Device identity is deliberately separate from XInput user index. Slots are transient runtime
    // state and may change after reconnect/re-enumeration; they must never be serialized as identity.
    internal static class DeviceIdentityKeys
    {
        internal const string VirtualXbox360 = "inputstitch:virtual:xbox360";
        internal const string VirtualDualShock4 = "inputstitch:virtual:dualshock4";
        internal const string RegistryFormatVersion = "1";

        internal sealed class Evidence
        {
            public string Alias = "";
            public string Basis = "";
        }

        internal static string VirtualKey(string type)
        {
            return string.Equals(type, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase)
                ? VirtualDualShock4 : VirtualXbox360;
        }

        internal static List<Evidence> EvidenceFor(GamingDeviceDescriptor device)
        {
            List<Evidence> result = new List<Evidence>();
            if (device == null) return result;

            AddEvidence(result, "container-id", device.ContainerId);
            AddEvidence(result, "container-path", device.BaseContainerDeviceInstancePath);

            string vid;
            string pid;
            TryGetVidPid(device, out vid, out pid);
            if (!string.IsNullOrWhiteSpace(device.SerialNumber))
            {
                string serialSeed = (vid.Length == 0 ? "????" : vid) + "|" +
                    (pid.Length == 0 ? "????" : pid) + "|" + Normalize(device.SerialNumber);
                AddAlias(result, "serial", "serial|" + serialSeed);
            }

            AddEvidence(result, "xusb", device.XusbDeviceInstancePath);
            AddEvidence(result, "pnp", device.DeviceInstancePath);
            return result;
        }

        internal static string CreatePersistentKey(GamingDeviceDescriptor device)
        {
            List<Evidence> evidence = EvidenceFor(device);
            return evidence.Count == 0 ? "" : CreatePersistentKeyFromAlias(evidence[0].Alias);
        }

        internal static string StrongestBasis(GamingDeviceDescriptor device)
        {
            List<Evidence> evidence = EvidenceFor(device);
            return evidence.Count == 0 ? "unresolved" : evidence[0].Basis;
        }

        internal static bool IsStrongerBasis(string candidate, string current)
        {
            return BasisRank(candidate) < BasisRank(current);
        }

        private static int BasisRank(string basis)
        {
            if (string.Equals(basis, "container-id", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(basis, "container-path", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(basis, "serial", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(basis, "xusb", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(basis, "pnp", StringComparison.OrdinalIgnoreCase)) return 4;
            return 100;
        }

        internal static string CreatePersistentKeyFromAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return "";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(alias.Trim().ToUpperInvariant()));
                StringBuilder text = new StringBuilder(32);
                // 128 bits is plenty for a local device registry while keeping diagnostics readable.
                for (int i = 0; i < 16; i++) text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return "dev:v1:" + text.ToString();
            }
        }

        internal static bool TryGetVidPid(GamingDeviceDescriptor device, out string vid, out string pid)
        {
            vid = "";
            pid = "";
            if (device == null) return false;
            string[] candidates = new string[]
            {
                device.XusbDeviceInstancePath ?? "",
                device.DeviceInstancePath ?? "",
                device.BaseContainerDeviceInstancePath ?? ""
            };
            foreach (string candidate in candidates)
            {
                Match match = Regex.Match(candidate, "VID_([0-9A-Fa-f]{4}).*?PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                vid = match.Groups[1].Value.ToUpperInvariant();
                pid = match.Groups[2].Value.ToUpperInvariant();
                return true;
            }
            return false;
        }

        internal static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return value.Trim().Replace('/', '\\').ToUpperInvariant();
        }

        private static void AddEvidence(List<Evidence> result, string basis, string value)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0) return;
            AddAlias(result, basis, basis + "|" + normalized);
        }

        private static void AddAlias(List<Evidence> result, string basis, string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return;
            foreach (Evidence existing in result)
                if (string.Equals(existing.Alias, alias, StringComparison.OrdinalIgnoreCase)) return;
            result.Add(new Evidence { Alias = alias, Basis = basis });
        }
    }

    [Serializable]
    public sealed class PersistentDeviceRecord
    {
        public string DeviceKey = "";
        public string Provider = "";
        public string IdentityBasis = "";
        public string FriendlyName = "";
        public string Vendor = "";
        public string Product = "";
        public string SerialNumber = "";
        public string DeviceInstancePath = "";
        public string XusbDeviceInstancePath = "";
        public string BaseContainerDeviceInstancePath = "";
        public string ContainerId = "";
        public bool VirtualBus;
        public string Vid = "";
        public string Pid = "";
        public List<string> IdentityAliases = new List<string>();
        public DateTime FirstSeenUtc;
        public DateTime LastSeenUtc;

        internal PersistentDeviceRecord Clone()
        {
            return new PersistentDeviceRecord
            {
                DeviceKey = DeviceKey ?? "",
                Provider = Provider ?? "",
                IdentityBasis = IdentityBasis ?? "",
                FriendlyName = FriendlyName ?? "",
                Vendor = Vendor ?? "",
                Product = Product ?? "",
                SerialNumber = SerialNumber ?? "",
                DeviceInstancePath = DeviceInstancePath ?? "",
                XusbDeviceInstancePath = XusbDeviceInstancePath ?? "",
                BaseContainerDeviceInstancePath = BaseContainerDeviceInstancePath ?? "",
                ContainerId = ContainerId ?? "",
                VirtualBus = VirtualBus,
                Vid = Vid ?? "",
                Pid = Pid ?? "",
                IdentityAliases = IdentityAliases == null ? new List<string>() : new List<string>(IdentityAliases),
                FirstSeenUtc = FirstSeenUtc,
                LastSeenUtc = LastSeenUtc
            };
        }
    }

    [Serializable]
    public sealed class DeviceRegistryDocument
    {
        public string FormatVersion = DeviceIdentityKeys.RegistryFormatVersion;
        public List<PersistentDeviceRecord> Devices = new List<PersistentDeviceRecord>();
    }

    internal sealed class PersistentDeviceRegistry
    {
        private readonly AtomicXmlFileStore<DeviceRegistryDocument> store;
        private readonly List<PersistentDeviceRecord> records = new List<PersistentDeviceRecord>();
        private bool dirty;
        private bool writable = true;
        private string status = "ready";

        internal PersistentDeviceRegistry(string path)
        {
            store = new AtomicXmlFileStore<DeviceRegistryDocument>(path);
            try
            {
                DeviceRegistryDocument loaded = store.Load();
                if (loaded != null && loaded.Devices != null)
                {
                    foreach (PersistentDeviceRecord record in loaded.Devices)
                    {
                        if (record == null || string.IsNullOrWhiteSpace(record.DeviceKey)) continue;
                        NormalizeRecord(record);
                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                // A corrupt registry must never stop InputStitch or damage the user's main config.
                // Keep it read-only for this run rather than overwriting potentially recoverable data.
                writable = false;
                status = "load-failed: " + ex.Message;
                try { AppLog.Write("Device identity registry load failed; persistence disabled for this run", ex); } catch { }
            }
        }

        internal bool Writable { get { return writable; } }
        internal string Status { get { return status; } }

        internal PersistentDeviceRecord Merge(GamingDeviceDescriptor device, string provider, DateTime nowUtc)
        {
            if (device == null) return null;
            List<DeviceIdentityKeys.Evidence> evidence = DeviceIdentityKeys.EvidenceFor(device);
            if (evidence.Count == 0) return null;

            PersistentDeviceRecord record = FindByEvidence(evidence);
            if (record == null)
            {
                record = new PersistentDeviceRecord();
                record.DeviceKey = DeviceIdentityKeys.CreatePersistentKeyFromAlias(evidence[0].Alias);
                record.FirstSeenUtc = nowUtc;
                records.Add(record);
                dirty = true;
            }

            bool changed = false;
            changed |= SetIfDifferent(ref record.Provider, provider ?? "");
            if (string.IsNullOrWhiteSpace(record.IdentityBasis) || DeviceIdentityKeys.IsStrongerBasis(evidence[0].Basis, record.IdentityBasis))
                changed |= SetIfDifferent(ref record.IdentityBasis, evidence[0].Basis);
            changed |= SetIfDifferent(ref record.FriendlyName, device.DisplayName ?? "");
            changed |= SetIfDifferent(ref record.Vendor, device.Vendor ?? "");
            changed |= SetIfDifferent(ref record.Product, device.Product ?? "");
            changed |= SetIfDifferent(ref record.SerialNumber, device.SerialNumber ?? "");
            changed |= SetIfDifferent(ref record.DeviceInstancePath, device.DeviceInstancePath ?? "");
            changed |= SetIfDifferent(ref record.XusbDeviceInstancePath, device.XusbDeviceInstancePath ?? "");
            changed |= SetIfDifferent(ref record.BaseContainerDeviceInstancePath, device.BaseContainerDeviceInstancePath ?? "");
            changed |= SetIfDifferent(ref record.ContainerId, device.ContainerId ?? "");
            if (device.VirtualBus && !record.VirtualBus) { record.VirtualBus = true; changed = true; }
            string vid;
            string pid;
            DeviceIdentityKeys.TryGetVidPid(device, out vid, out pid);
            changed |= SetIfDifferent(ref record.Vid, vid);
            changed |= SetIfDifferent(ref record.Pid, pid);

            if (record.IdentityAliases == null) { record.IdentityAliases = new List<string>(); changed = true; }
            foreach (DeviceIdentityKeys.Evidence item in evidence)
            {
                if (ContainsAlias(record.IdentityAliases, item.Alias)) continue;
                record.IdentityAliases.Add(item.Alias);
                changed = true;
            }

            if (record.FirstSeenUtc == default(DateTime)) { record.FirstSeenUtc = nowUtc; changed = true; }
            if (record.LastSeenUtc != nowUtc) { record.LastSeenUtc = nowUtc; changed = true; }
            if (changed) dirty = true;
            return record.Clone();
        }

        internal List<PersistentDeviceRecord> SnapshotRecords()
        {
            List<PersistentDeviceRecord> copy = new List<PersistentDeviceRecord>();
            foreach (PersistentDeviceRecord record in records) copy.Add(record.Clone());
            return copy;
        }

        internal void SaveIfDirty()
        {
            if (!dirty || !writable) return;
            DeviceRegistryDocument document = new DeviceRegistryDocument();
            document.Devices = SnapshotRecords();
            try
            {
                store.Save(document);
                dirty = false;
                status = "ready";
            }
            catch (Exception ex)
            {
                status = "save-failed: " + ex.Message;
                try { AppLog.Write("Device identity registry save failed", ex); } catch { }
            }
        }

        private PersistentDeviceRecord FindByEvidence(List<DeviceIdentityKeys.Evidence> evidence)
        {
            foreach (PersistentDeviceRecord record in records)
            {
                if (record.IdentityAliases == null || record.IdentityAliases.Count == 0)
                    record.IdentityAliases = EvidenceFromStoredFields(record);
                foreach (DeviceIdentityKeys.Evidence candidate in evidence)
                    if (ContainsAlias(record.IdentityAliases, candidate.Alias)) return record;
            }
            return null;
        }

        private static List<string> EvidenceFromStoredFields(PersistentDeviceRecord record)
        {
            GamingDeviceDescriptor descriptor = new GamingDeviceDescriptor
            {
                Vendor = record.Vendor ?? "",
                Product = record.Product ?? "",
                SerialNumber = record.SerialNumber ?? "",
                DeviceInstancePath = record.DeviceInstancePath ?? "",
                XusbDeviceInstancePath = record.XusbDeviceInstancePath ?? "",
                BaseContainerDeviceInstancePath = record.BaseContainerDeviceInstancePath ?? "",
                ContainerId = record.ContainerId ?? "",
                VirtualBus = record.VirtualBus
            };
            List<string> aliases = new List<string>();
            foreach (DeviceIdentityKeys.Evidence item in DeviceIdentityKeys.EvidenceFor(descriptor)) aliases.Add(item.Alias);
            return aliases;
        }

        private static bool ContainsAlias(List<string> aliases, string value)
        {
            if (aliases == null || string.IsNullOrWhiteSpace(value)) return false;
            foreach (string alias in aliases)
                if (string.Equals(alias, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void NormalizeRecord(PersistentDeviceRecord record)
        {
            if (record.IdentityAliases == null) record.IdentityAliases = new List<string>();
            record.DeviceKey = (record.DeviceKey ?? "").Trim();
            record.Provider = record.Provider ?? "";
            record.IdentityBasis = record.IdentityBasis ?? "";
            record.FriendlyName = record.FriendlyName ?? "";
            record.Vendor = record.Vendor ?? "";
            record.Product = record.Product ?? "";
            record.SerialNumber = record.SerialNumber ?? "";
            record.DeviceInstancePath = record.DeviceInstancePath ?? "";
            record.XusbDeviceInstancePath = record.XusbDeviceInstancePath ?? "";
            record.BaseContainerDeviceInstancePath = record.BaseContainerDeviceInstancePath ?? "";
            record.ContainerId = record.ContainerId ?? "";
            record.Vid = record.Vid ?? "";
            record.Pid = record.Pid ?? "";
        }

        private static bool SetIfDifferent(ref string target, string value)
        {
            value = value ?? "";
            if (string.Equals(target ?? "", value, StringComparison.Ordinal)) return false;
            target = value;
            return true;
        }
    }

    internal enum DeviceInventoryRole
    {
        InputStitchVirtual,
        PersistentGamingDevice,
        DiscoveredVirtualBusDevice,
        UnresolvedXInputSource,
        KnownOffline
    }

    internal sealed class DeviceInventoryItem
    {
        public DeviceInventoryRole Role;
        public string DeviceKey = "";
        public string DisplayName = "";
        public string Provider = "";
        public string IdentityBasis = "";
        public bool Present;
        public bool Persistent;
        public int XInputSlot = -1;
        public string Vid = "";
        public string Pid = "";
        public string SerialNumber = "";
        public string DeviceInstancePath = "";
        public string XusbDeviceInstancePath = "";
        public string BaseContainerDeviceInstancePath = "";
        public string ContainerId = "";
        public bool VirtualBus;
        public DateTime FirstSeenUtc;
        public DateTime LastSeenUtc;
    }

    internal sealed class DeviceInventorySnapshot
    {
        public DateTime CapturedUtc;
        public bool StableMetadataAvailable;
        public bool RegistryWritable;
        public string DiscoveryStatus = "";
        public string RegistryStatus = "";
        public List<DeviceInventoryItem> Items = new List<DeviceInventoryItem>();
    }

    // First-stage inventory service. It intentionally does NOT guess a persistent physical identity
    // from XInput slot N. Windows PnP/HidHide metadata and XInput runtime slots are shown side-by-side
    // until a provider can prove their correlation. This is the safe base for Router source policy.
    internal sealed class DeviceIdentityService
    {
        private readonly IGamingDeviceDiscovery discovery;
        private readonly Func<int[]> externalSlots;
        private readonly Func<int> ownSlot;
        private readonly Func<bool> ownConnected;
        private readonly Func<string> ownType;
        private readonly Func<DateTime> utcNow;
        private readonly PersistentDeviceRegistry registry;
        private readonly object sync = new object();
        private DeviceInventorySnapshot lastSnapshot;

        internal DeviceIdentityService(string registryPath, IGamingDeviceDiscovery deviceDiscovery,
            Func<int[]> currentExternalSlots, Func<int> currentOwnSlot,
            Func<bool> isOwnConnected, Func<string> currentOwnType)
            : this(registryPath, deviceDiscovery, currentExternalSlots, currentOwnSlot,
                isOwnConnected, currentOwnType, delegate { return DateTime.UtcNow; })
        {
        }

        internal DeviceIdentityService(string registryPath, IGamingDeviceDiscovery deviceDiscovery,
            Func<int[]> currentExternalSlots, Func<int> currentOwnSlot,
            Func<bool> isOwnConnected, Func<string> currentOwnType, Func<DateTime> clock)
        {
            if (string.IsNullOrWhiteSpace(registryPath)) throw new ArgumentException("A device registry path is required.");
            discovery = deviceDiscovery;
            externalSlots = currentExternalSlots;
            ownSlot = currentOwnSlot;
            ownConnected = isOwnConnected;
            ownType = currentOwnType;
            utcNow = clock ?? delegate { return DateTime.UtcNow; };
            registry = new PersistentDeviceRegistry(registryPath);
        }

        internal DeviceInventorySnapshot LastSnapshot { get { return lastSnapshot; } }

        internal DeviceInventorySnapshot Refresh()
        {
            lock (sync) return RefreshCore();
        }

        private DeviceInventorySnapshot RefreshCore()
        {
            DateTime now = utcNow();
            DeviceInventorySnapshot snapshot = new DeviceInventorySnapshot();
            snapshot.CapturedUtc = now;
            snapshot.RegistryWritable = registry.Writable;
            snapshot.RegistryStatus = registry.Status;

            bool ownIsConnected = SafeOwnConnected();
            int ownUserIndex = ownIsConnected ? SafeOwnSlot() : -1;
            string ownControllerType = SafeOwnType();
            if (ownIsConnected)
            {
                bool xbox = !string.Equals(ownControllerType, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase);
                snapshot.Items.Add(new DeviceInventoryItem
                {
                    Role = DeviceInventoryRole.InputStitchVirtual,
                    DeviceKey = DeviceIdentityKeys.VirtualKey(ownControllerType),
                    DisplayName = xbox ? "InputStitch Virtual Xbox 360" : "InputStitch Virtual DualShock 4",
                    Provider = "InputStitch virtual output",
                    IdentityBasis = "product-owned virtual identity",
                    Present = true,
                    Persistent = true,
                    XInputSlot = xbox ? ownUserIndex : -1,
                    LastSeenUtc = now,
                    FirstSeenUtc = now
                });
            }

            HashSet<string> presentPersistentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IList<GamingDeviceDescriptor> discovered = null;
            if (discovery != null && SafeDiscoveryAvailable())
            {
                try
                {
                    discovered = discovery.EnumerateGamingDevices();
                    snapshot.StableMetadataAvailable = true;
                    snapshot.DiscoveryStatus = discovery.ProviderName + " metadata available. Persistent DeviceKey never uses XInput slot.";
                }
                catch (Exception ex)
                {
                    snapshot.StableMetadataAvailable = false;
                    snapshot.DiscoveryStatus = "Gaming-device metadata query failed: " + ex.Message;
                    try { AppLog.Write("Device identity discovery failed", ex); } catch { }
                }
            }
            else
            {
                snapshot.StableMetadataAvailable = false;
                snapshot.DiscoveryStatus = "Stable gaming-device metadata unavailable. XInput sources remain transient/unresolved; no slot is persisted as device identity.";
            }

            if (discovered != null)
            {
                foreach (GamingDeviceDescriptor descriptor in discovered)
                {
                    if (descriptor == null || !descriptor.Present || !descriptor.GamingDevice) continue;
                    PersistentDeviceRecord record = registry.Merge(descriptor, discovery == null ? "device metadata" : discovery.ProviderName, now);
                    if (record == null || string.IsNullOrWhiteSpace(record.DeviceKey)) continue;
                    if (!presentPersistentKeys.Add(record.DeviceKey)) continue;
                    snapshot.Items.Add(ToInventory(record,
                        record.VirtualBus ? DeviceInventoryRole.DiscoveredVirtualBusDevice : DeviceInventoryRole.PersistentGamingDevice,
                        true));
                }
            }

            int[] slots = SafeExternalSlots();
            HashSet<int> seenSlots = new HashSet<int>();
            foreach (int slot in slots)
            {
                if (slot < 0 || slot > 3 || slot == ownUserIndex || !seenSlots.Add(slot)) continue;
                snapshot.Items.Add(new DeviceInventoryItem
                {
                    Role = DeviceInventoryRole.UnresolvedXInputSource,
                    DeviceKey = "",
                    DisplayName = "XInput source " + slot.ToString(CultureInfo.InvariantCulture),
                    Provider = "XInput",
                    IdentityBasis = "unresolved runtime slot (not persisted as identity)",
                    Present = true,
                    Persistent = false,
                    XInputSlot = slot,
                    LastSeenUtc = now,
                    FirstSeenUtc = now
                });
            }

            List<PersistentDeviceRecord> known = registry.SnapshotRecords();
            foreach (PersistentDeviceRecord record in known)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.DeviceKey) || presentPersistentKeys.Contains(record.DeviceKey)) continue;
                snapshot.Items.Add(ToInventory(record, DeviceInventoryRole.KnownOffline, false));
            }

            registry.SaveIfDirty();
            snapshot.RegistryStatus = registry.Status;
            snapshot.Items.Sort(CompareInventoryItems);
            lastSnapshot = snapshot;
            return snapshot;
        }

        internal string DiagnosticsSummary()
        {
            DeviceInventorySnapshot snapshot = lastSnapshot;
            if (snapshot == null) return "not-refreshed";
            int persistent = 0;
            int unresolved = 0;
            int virtualCount = 0;
            foreach (DeviceInventoryItem item in snapshot.Items)
            {
                if (item.Role == DeviceInventoryRole.InputStitchVirtual) virtualCount++;
                else if (item.Role == DeviceInventoryRole.UnresolvedXInputSource) unresolved++;
                else if (item.Persistent) persistent++;
            }
            return "persistent=" + persistent.ToString(CultureInfo.InvariantCulture) +
                "; unresolvedXInput=" + unresolved.ToString(CultureInfo.InvariantCulture) +
                "; ownVirtual=" + virtualCount.ToString(CultureInfo.InvariantCulture) +
                "; metadata=" + (snapshot.StableMetadataAvailable ? "available" : "unavailable") +
                "; registry=" + snapshot.RegistryStatus;
        }

        private bool SafeDiscoveryAvailable()
        {
            try { return discovery != null && discovery.IsAvailable; }
            catch { return false; }
        }

        private int[] SafeExternalSlots()
        {
            try { return externalSlots == null ? new int[0] : (externalSlots() ?? new int[0]); }
            catch { return new int[0]; }
        }

        private int SafeOwnSlot()
        {
            try { return ownSlot == null ? -1 : ownSlot(); }
            catch { return -1; }
        }

        private bool SafeOwnConnected()
        {
            try { return ownConnected != null && ownConnected(); }
            catch { return false; }
        }

        private string SafeOwnType()
        {
            try { return ownType == null ? "" : (ownType() ?? ""); }
            catch { return ""; }
        }

        private static DeviceInventoryItem ToInventory(PersistentDeviceRecord record, DeviceInventoryRole role, bool present)
        {
            return new DeviceInventoryItem
            {
                Role = role,
                DeviceKey = record.DeviceKey ?? "",
                DisplayName = string.IsNullOrWhiteSpace(record.FriendlyName) ? "Known gaming device" : record.FriendlyName,
                Provider = record.Provider ?? "",
                IdentityBasis = record.IdentityBasis ?? "",
                Present = present,
                Persistent = true,
                XInputSlot = -1,
                Vid = record.Vid ?? "",
                Pid = record.Pid ?? "",
                SerialNumber = record.SerialNumber ?? "",
                DeviceInstancePath = record.DeviceInstancePath ?? "",
                XusbDeviceInstancePath = record.XusbDeviceInstancePath ?? "",
                BaseContainerDeviceInstancePath = record.BaseContainerDeviceInstancePath ?? "",
                ContainerId = record.ContainerId ?? "",
                VirtualBus = record.VirtualBus,
                FirstSeenUtc = record.FirstSeenUtc,
                LastSeenUtc = record.LastSeenUtc
            };
        }

        private static int CompareInventoryItems(DeviceInventoryItem a, DeviceInventoryItem b)
        {
            int role = RoleOrder(a == null ? DeviceInventoryRole.KnownOffline : a.Role).CompareTo(RoleOrder(b == null ? DeviceInventoryRole.KnownOffline : b.Role));
            if (role != 0) return role;
            return string.Compare(a == null ? "" : a.DisplayName, b == null ? "" : b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int RoleOrder(DeviceInventoryRole role)
        {
            if (role == DeviceInventoryRole.InputStitchVirtual) return 0;
            if (role == DeviceInventoryRole.PersistentGamingDevice) return 1;
            if (role == DeviceInventoryRole.DiscoveredVirtualBusDevice) return 2;
            if (role == DeviceInventoryRole.UnresolvedXInputSource) return 3;
            return 4;
        }
    }
}
