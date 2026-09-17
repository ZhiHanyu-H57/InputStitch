using System;
using System.Collections.Generic;
using System.IO;
using InputStitch;

internal static class DeviceIdentityTests
{
    private static int checks;

    private sealed class FakeDiscovery : IGamingDeviceDiscovery
    {
        internal bool Available = true;
        internal readonly List<GamingDeviceDescriptor> Devices = new List<GamingDeviceDescriptor>();
        public bool IsAvailable { get { return Available; } }
        public string ProviderName { get { return "fake metadata"; } }
        public IList<GamingDeviceDescriptor> EnumerateGamingDevices() { return new List<GamingDeviceDescriptor>(Devices); }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
    }

    private static GamingDeviceDescriptor Pad(string name, string hid, string xusb, string container, string serial)
    {
        return new GamingDeviceDescriptor
        {
            Present = true,
            GamingDevice = true,
            Vendor = "Vendor",
            Product = name,
            Description = "Game controller",
            SerialNumber = serial ?? "",
            DeviceInstancePath = hid ?? "",
            XusbDeviceInstancePath = xusb ?? "",
            BaseContainerDeviceInstancePath = container ?? ""
        };
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-DeviceIdentityTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            KeyPolicy();
            ParserPreservesSerial();
            PersistenceAndInventory(root);
            Console.WriteLine("PASS device identity: " + checks + " checks; temporary XML/fake device metadata only, no real devices touched.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static void KeyPolicy()
    {
        GamingDeviceDescriptor a = Pad("Pad A", @"HID\VID_1234&PID_5678\OLD", @"USB\VID_1234&PID_5678\XUSB-OLD", @"USB\VID_1234&PID_5678\CONTAINER-A", "SER-A");
        GamingDeviceDescriptor moved = Pad("Pad A", @"HID\VID_1234&PID_5678\NEW", @"USB\VID_1234&PID_5678\XUSB-NEW", @"USB\VID_1234&PID_5678\CONTAINER-A", "SER-A");
        GamingDeviceDescriptor b = Pad("Pad B", @"HID\VID_1234&PID_5678\B", @"USB\VID_1234&PID_5678\XUSB-B", @"USB\VID_1234&PID_5678\CONTAINER-B", "SER-B");
        a.ContainerId = "11111111-1111-1111-1111-111111111111";
        moved.ContainerId = a.ContainerId;
        b.ContainerId = "22222222-2222-2222-2222-222222222222";

        string keyA = DeviceIdentityKeys.CreatePersistentKey(a);
        Check(keyA.StartsWith("dev:v1:", StringComparison.Ordinal), "persistent DeviceKey is versioned and opaque");
        Check(keyA == DeviceIdentityKeys.CreatePersistentKey(moved), "container identity survives HID/XUSB re-enumeration path changes");
        Check(keyA != DeviceIdentityKeys.CreatePersistentKey(b), "different containers receive different DeviceKeys");
        Check(DeviceIdentityKeys.StrongestBasis(a) == "container-id", "PnP Container ID is strongest identity basis");

        GamingDeviceDescriptor serialOnly = Pad("Serial pad", "", "", "", "SERIAL-01");
        serialOnly.DeviceInstancePath = @"HID\VID_AAAA&PID_BBBB\TEMP";
        Check(DeviceIdentityKeys.StrongestBasis(serialOnly) == "serial", "serial plus VID/PID outranks paths when no container is available");

        GamingDeviceDescriptor xusbOnly = Pad("XUSB pad", "", @"USB\VID_1111&PID_2222\X", "", "");
        Check(DeviceIdentityKeys.StrongestBasis(xusbOnly) == "xusb", "XUSB path is accepted when stronger evidence is unavailable");
        GamingDeviceDescriptor pnpOnly = Pad("PnP pad", @"HID\VID_3333&PID_4444\P", "", "", "");
        Check(DeviceIdentityKeys.StrongestBasis(pnpOnly) == "pnp", "PnP path is final durable fallback");

        string vid;
        string pid;
        Check(DeviceIdentityKeys.TryGetVidPid(a, out vid, out pid) && vid == "1234" && pid == "5678", "VID/PID extraction is normalized");
        Check(DeviceIdentityKeys.VirtualKey(VirtualGamepadTypes.Xbox360) == DeviceIdentityKeys.VirtualXbox360, "own Xbox virtual output has fixed product DeviceKey");
        Check(DeviceIdentityKeys.VirtualKey(VirtualGamepadTypes.DualShock4) == DeviceIdentityKeys.VirtualDualShock4, "own DS4 virtual output has fixed product DeviceKey");
        Check(DeviceIdentityKeys.VirtualXbox360 != DeviceIdentityKeys.VirtualDualShock4, "own virtual output types have distinct identities");

        List<GamingDeviceDescriptor> merged = new List<GamingDeviceDescriptor>();
        GamingDeviceDescriptor native = Pad("Xbox Controller", @"USB\VID_045E&PID_028E\XUSB", @"USB\VID_045E&PID_028E\XUSB", "", "");
        native.ContainerId = "33333333-3333-3333-3333-333333333333";
        native.VirtualBus = true;
        CompositeGamingDeviceDiscovery.MergeDescriptor(merged, native);
        GamingDeviceDescriptor hidhide = Pad("Xbox Controller", @"HID\VID_045E&PID_028E\HID", @"USB\VID_045E&PID_028E\XUSB", @"USB\VID_045E&PID_028E\BASE", "SER-MERGED");
        CompositeGamingDeviceDiscovery.MergeDescriptor(merged, hidhide);
        Check(merged.Count == 1, "native XUSB and HidHide metadata for the same XUSB identity merge into one record");
        Check(merged[0].SerialNumber == "SER-MERGED" && merged[0].ContainerId == native.ContainerId && merged[0].VirtualBus,
            "composite discovery preserves complementary serial/container/virtual-bus metadata");
        Check(merged[0].DeviceInstancePath == hidhide.DeviceInstancePath, "HID-facing path enriches an XUSB-only native row");
    }

    private static void ParserPreservesSerial()
    {
        string json = "[{\"friendlyName\":\"Pad\",\"devices\":[{" +
            "\"present\":true,\"gamingDevice\":true,\"symbolicLink\":\"x\",\"vendor\":\"Vendor\",\"product\":\"Pad One\",\"serialNumber\":\"SER-123\",\"usage\":\"Gamepad\",\"description\":\"Controller\"," +
            "\"deviceInstancePath\":\"HID\\\\VID_1234&PID_5678\\\\A\",\"xusbDeviceInstancePath\":\"USB\\\\VID_1234&PID_5678\\\\X\",\"baseContainerDeviceInstancePath\":\"USB\\\\VID_1234&PID_5678\\\\BASE\",\"baseContainerClassGuid\":\"{}\",\"baseContainerDeviceCount\":1}]}]";
        IList<GamingDeviceDescriptor> parsed = HidHideCliBackend.ParseGamingDevices(json);
        Check(parsed.Count == 1 && parsed[0].SerialNumber == "SER-123", "HidHide discovery preserves serialNumber for identity evidence");
    }

    private static void PersistenceAndInventory(string root)
    {
        string registryPath = Path.Combine(root, "devices.xml");
        FakeDiscovery discovery = new FakeDiscovery();
        GamingDeviceDescriptor first = Pad("Pad One", @"HID\VID_1234&PID_5678\A", @"USB\VID_1234&PID_5678\X1", "", "");
        discovery.Devices.Add(first);
        DateTime t1 = new DateTime(2026, 9, 17, 9, 0, 0, DateTimeKind.Utc);
        DeviceIdentityService service = new DeviceIdentityService(
            registryPath, discovery,
            delegate { return new int[] { 1 }; },
            delegate { return 0; },
            delegate { return true; },
            delegate { return VirtualGamepadTypes.Xbox360; },
            delegate { return t1; });

        DeviceInventorySnapshot one = service.Refresh();
        DeviceInventoryItem own = FindRole(one, DeviceInventoryRole.InputStitchVirtual);
        DeviceInventoryItem persistent = FindRole(one, DeviceInventoryRole.PersistentGamingDevice);
        DeviceInventoryItem unresolved = FindRole(one, DeviceInventoryRole.UnresolvedXInputSource);
        Check(own != null && own.DeviceKey == DeviceIdentityKeys.VirtualXbox360 && own.XInputSlot == 0, "inventory identifies own virtual Xbox separately from external sources");
        Check(persistent != null && persistent.Persistent && persistent.DeviceKey.StartsWith("dev:v1:", StringComparison.Ordinal), "discovered physical metadata receives persistent DeviceKey");
        string firstKey = persistent.DeviceKey;
        Check(unresolved != null && unresolved.XInputSlot == 1 && !unresolved.Persistent && unresolved.DeviceKey == "", "XInput slot remains transient unresolved metadata, never a DeviceKey");
        Check(File.Exists(registryPath), "device registry persists separately to devices.xml");

        // Enrich the same record with stronger container evidence while retaining the original XUSB alias.
        discovery.Devices.Clear();
        GamingDeviceDescriptor enrichedDescriptor = Pad("Pad One", @"HID\VID_1234&PID_5678\B", @"USB\VID_1234&PID_5678\X1", @"USB\VID_1234&PID_5678\CONTAINER", "SER-1");
        enrichedDescriptor.ContainerId = "44444444-4444-4444-4444-444444444444";
        discovery.Devices.Add(enrichedDescriptor);
        DeviceInventorySnapshot enriched = service.Refresh();
        PersistentDeviceRecord enrichedRecord = FindPersistentRecord(registryPath, firstKey);
        Check(enrichedRecord != null && ContainsAliasPrefix(enrichedRecord, "container-id|"), "metadata enrichment adds stronger aliases to the existing record");
        Check(enrichedRecord != null && enrichedRecord.IdentityBasis == "container-id", "identity diagnostics upgrade to the strongest known evidence without changing DeviceKey");
        Check(FindRole(enriched, DeviceInventoryRole.PersistentGamingDevice).DeviceKey == firstKey, "stronger metadata enrichment does not churn an existing DeviceKey");

        // Simulate restart + re-enumeration: XUSB/HID changed, container remains. The alias learned above must recover the old key.
        FakeDiscovery afterRestart = new FakeDiscovery();
        GamingDeviceDescriptor reenumerated = Pad("Pad One", @"HID\VID_1234&PID_5678\C", @"USB\VID_1234&PID_5678\X2", @"USB\VID_1234&PID_5678\CONTAINER", "SER-1");
        reenumerated.ContainerId = enrichedDescriptor.ContainerId;
        afterRestart.Devices.Add(reenumerated);
        DateTime t2 = t1.AddHours(1);
        DeviceIdentityService restarted = new DeviceIdentityService(
            registryPath, afterRestart,
            delegate { return new int[] { 3 }; },
            delegate { return 2; },
            delegate { return true; },
            delegate { return VirtualGamepadTypes.Xbox360; },
            delegate { return t2; });
        DeviceInventorySnapshot two = restarted.Refresh();
        DeviceInventoryItem after = FindRole(two, DeviceInventoryRole.PersistentGamingDevice);
        DeviceInventoryItem ownMoved = FindRole(two, DeviceInventoryRole.InputStitchVirtual);
        DeviceInventoryItem transientMoved = FindRole(two, DeviceInventoryRole.UnresolvedXInputSource);
        Check(after != null && after.DeviceKey == firstKey, "DeviceKey survives process restart and device path re-enumeration");
        Check(ownMoved != null && ownMoved.DeviceKey == DeviceIdentityKeys.VirtualXbox360 && ownMoved.XInputSlot == 2, "own fixed DeviceKey survives XInput slot movement");
        Check(transientMoved != null && transientMoved.XInputSlot == 3 && transientMoved.DeviceKey == "", "external XInput slot movement remains transient and cannot change physical DeviceKey");

        // Unavailable stable metadata must fail honestly: expose XInput runtime source but never mint a key from slot N.
        FakeDiscovery unavailable = new FakeDiscovery();
        unavailable.Available = false;
        DeviceIdentityService noMetadata = new DeviceIdentityService(
            Path.Combine(root, "unavailable.xml"), unavailable,
            delegate { return new int[] { 2 }; },
            delegate { return -1; },
            delegate { return false; },
            delegate { return ""; },
            delegate { return t2; });
        DeviceInventorySnapshot unresolvedOnly = noMetadata.Refresh();
        DeviceInventoryItem onlySlot = FindRole(unresolvedOnly, DeviceInventoryRole.UnresolvedXInputSource);
        Check(!unresolvedOnly.StableMetadataAvailable && onlySlot != null && onlySlot.DeviceKey == "" && !onlySlot.Persistent,
            "without stable metadata, runtime XInput stays explicitly unresolved rather than guessed");

        FakeDiscovery virtualDiscovery = new FakeDiscovery();
        GamingDeviceDescriptor virtualProviderRow = Pad("Virtual Xbox", @"USB\\VID_045E&PID_028E\\VIRTUAL", @"USB\\VID_045E&PID_028E\\VIRTUAL", "", "");
        virtualProviderRow.ContainerId = "55555555-5555-5555-5555-555555555555";
        virtualProviderRow.VirtualBus = true;
        virtualDiscovery.Devices.Add(virtualProviderRow);
        DeviceIdentityService virtualService = new DeviceIdentityService(
            Path.Combine(root, "virtual-provider.xml"), virtualDiscovery,
            delegate { return new int[0]; }, delegate { return -1; }, delegate { return false; }, delegate { return ""; }, delegate { return t2; });
        Check(FindRole(virtualService.Refresh(), DeviceInventoryRole.DiscoveredVirtualBusDevice) != null,
            "provider-discovered virtual-bus devices are labeled separately instead of being presented as physical/external devices");

        // Once a known device disappears, registry history remains visible as offline.
        FakeDiscovery empty = new FakeDiscovery();
        DeviceIdentityService offlineService = new DeviceIdentityService(
            registryPath, empty,
            delegate { return new int[0]; },
            delegate { return -1; },
            delegate { return false; },
            delegate { return ""; },
            delegate { return t2.AddHours(1); });
        DeviceInventorySnapshot offline = offlineService.Refresh();
        DeviceInventoryItem knownOffline = FindRole(offline, DeviceInventoryRole.KnownOffline);
        Check(knownOffline != null && knownOffline.DeviceKey == firstKey && !knownOffline.Present, "known devices remain in persistent inventory while offline");
    }

    private static DeviceInventoryItem FindRole(DeviceInventorySnapshot snapshot, DeviceInventoryRole role)
    {
        if (snapshot == null || snapshot.Items == null) return null;
        foreach (DeviceInventoryItem item in snapshot.Items) if (item != null && item.Role == role) return item;
        return null;
    }

    private static PersistentDeviceRecord FindPersistentRecord(string path, string key)
    {
        DeviceRegistryDocument doc = new AtomicXmlFileStore<DeviceRegistryDocument>(path).Load();
        if (doc == null || doc.Devices == null) return null;
        foreach (PersistentDeviceRecord record in doc.Devices)
            if (record != null && string.Equals(record.DeviceKey, key, StringComparison.OrdinalIgnoreCase)) return record;
        return null;
    }

    private static bool ContainsAliasPrefix(PersistentDeviceRecord record, string prefix)
    {
        if (record == null || record.IdentityAliases == null) return false;
        foreach (string alias in record.IdentityAliases)
            if (!string.IsNullOrWhiteSpace(alias) && alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
