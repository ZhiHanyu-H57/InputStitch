using System;
using System.Collections.Generic;
using System.IO;
using InputStitch;

internal static class Sdl3DeviceIdentityTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private sealed class EmptyDiscovery : IGamingDeviceDiscovery
    {
        public bool IsAvailable { get { return true; } }
        public string ProviderName { get { return "test-discovery"; } }
        public IList<GamingDeviceDescriptor> EnumerateGamingDevices() { return new List<GamingDeviceDescriptor>(); }
    }

    private static Sdl3DeviceDescriptor Device(string name, ushort vendorId, ushort productId, string path)
    {
        return new Sdl3DeviceDescriptor { DeviceId = 1, Name = name, VendorId = vendorId, ProductId = productId, Path = path };
    }

    public static int Main()
    {
        const string wiredPath = @"\\?\HID#VID_054C&PID_0CE6#9&2a1b3c4&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
        Sdl3DeviceDescriptor wired = Device("DualSense Wireless Controller", 0x054C, 0x0CE6, wiredPath);
        GamingDeviceDescriptor mapped = Sdl3DeviceIdentity.ToGamingDevice(wired);
        Check(mapped != null && mapped.ProviderName == "sdl3" && mapped.Present && mapped.GamingDevice,
            "SDL3 descriptors map onto the identity model with the provider label");
        Check(mapped.DeviceInstancePath == wiredPath, "the SDL3 device path becomes the pnp evidence");
        Check(DeviceIdentityKeys.StrongestBasis(mapped) == "pnp", "a real device path outranks the provider alias");
        Check(!string.IsNullOrWhiteSpace(mapped.ProviderDeviceKey), "VID/PID plus name produce a provider alias");
        Check(!mapped.VirtualBus && !mapped.NativeXusbMetadata && mapped.XusbDeviceInstancePath.Length == 0,
            "SDL3 devices never claim XUSB metadata");

        GamingDeviceDescriptor withoutPath = Sdl3DeviceIdentity.ToGamingDevice(Device("8BitDo Pro 2", 0x2DC8, 0x6101, ""));
        Check(withoutPath != null && DeviceIdentityKeys.StrongestBasis(withoutPath) == "provider",
            "a device without a PnP path falls back to the provider basis");
        Check(Sdl3DeviceIdentity.ToGamingDevice(Device("Unknown", 0, 0, "")) == null,
            "a device reporting neither path nor VID/PID is skipped instead of inventing an identity");
        Check(Sdl3DeviceIdentity.ToGamingDevice(null) == null, "a missing descriptor maps to nothing");

        string root = Path.Combine(Path.GetTempPath(), "InputStitch-Sdl3Identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            PersistentDeviceRegistry registry = new PersistentDeviceRegistry(Path.Combine(root, "devices.xml"));
            DateTime now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
            PersistentDeviceRecord first = registry.Merge(mapped, "sdl3", now);
            Check(first != null && first.DeviceKey.StartsWith("dev:v1:", StringComparison.Ordinal),
                "a wired SDL3 device gets a persistent key");
            Check(first.Vid == "054C" && first.Pid == "0CE6", "VID/PID are derived from the reported path");
            PersistentDeviceRecord again = registry.Merge(mapped, "sdl3", now.AddMinutes(1));
            Check(again.DeviceKey == first.DeviceKey, "the same device keeps its key across refreshes");

            const string movedPath = @"\\?\HID#VID_054C&PID_0CE6#7&ffffeeee&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
            PersistentDeviceRecord moved = registry.Merge(
                Sdl3DeviceIdentity.ToGamingDevice(Device("DualSense Wireless Controller", 0x054C, 0x0CE6, movedPath)),
                "sdl3", now.AddMinutes(2));
            Check(moved.DeviceKey == first.DeviceKey,
                "a device that moved to another port merges back through the provider alias");

            PersistentDeviceRecord other = registry.Merge(withoutPath, "sdl3", now.AddMinutes(3));
            Check(other.DeviceKey != first.DeviceKey, "different VID/PID produce different keys");
            Check(registry.SnapshotRecords().Count == 2, "the registry keeps exactly the known devices");

            // Service integration: provider devices show up in the inventory without claiming an XInput slot.
            DeviceIdentityService service = new DeviceIdentityService(
                Path.Combine(root, "devices-service.xml"), new EmptyDiscovery(),
                delegate { return new int[0]; },
                delegate { return -1; },
                delegate { return false; },
                delegate { return VirtualGamepadTypes.Xbox360; },
                delegate { return now; },
                delegate { return 1L; },
                delegate { return new List<GamingDeviceDescriptor> { Sdl3DeviceIdentity.ToGamingDevice(wired) }; });
            DeviceInventorySnapshot snapshot = service.Refresh();
            DeviceInventoryItem item = null;
            foreach (DeviceInventoryItem candidate in snapshot.Items)
                if (candidate.Provider == "sdl3") item = candidate;
            Check(item != null, "provider devices appear in the device inventory");
            Check(item != null && item.XInputSlot == -1 && item.Role == DeviceInventoryRole.PersistentGamingDevice,
                "SDL3 devices are persistent records without an XInput slot claim");
            Check(item != null && item.IdentityBasis == "pnp", "the inventory reports the identity basis");
            Check(item != null && item.Vid == "054C" && item.Pid == "0CE6", "the inventory reports VID/PID");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }

        Console.WriteLine("PASS sdl3 device identity: " + checks + " checks; provider evidence mapping, registry merge and inventory integration without real devices.");
        return 0;
    }
}
