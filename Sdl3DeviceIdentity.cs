using System;
using System.Globalization;

namespace InputStitch
{
    // Maps SDL3 gamepad descriptors onto the persistent device-identity model (development plan H2.3).
    //
    // SDL 3.4 reports a gamepad path (usually the HID path), VID/PID and a name, but no PnP container id and no
    // serial. Only real reported data is used: a device path becomes the pnp evidence the identity layer can
    // derive, while VID/PID plus name produce a provider-scoped alias that is explicitly ranked below every PnP
    // basis (so a later, stronger discovery can take over the same record). A device that reports neither a
    // path nor VID/PID is skipped instead of inventing an identity, and SDL3 devices never claim an XInput
    // slot - slot correlation stays reserved for the XUSB/HidHide metadata lanes.
    internal static class Sdl3DeviceIdentity
    {
        internal const string ProviderName = "sdl3";

        internal static GamingDeviceDescriptor ToGamingDevice(Sdl3DeviceDescriptor device)
        {
            if (device == null) return null;
            string path = (device.Path ?? "").Trim();
            string name = (device.Name ?? "").Trim();
            bool hasVidPid = device.VendorId != 0 || device.ProductId != 0;
            if (path.Length == 0 && !hasVidPid) return null;

            GamingDeviceDescriptor descriptor = new GamingDeviceDescriptor();
            descriptor.Present = true;
            descriptor.GamingDevice = true;
            descriptor.ProviderName = ProviderName;
            descriptor.Product = name;
            descriptor.Description = BuildDescription(device, name);
            descriptor.DeviceInstancePath = path;
            if (hasVidPid)
            {
                // Derived only from reported values; it lets a device that moves to another USB port (and thus
                // gets a different HID path) still merge into the same record.
                descriptor.ProviderDeviceKey = "vid:" + FormatHex(device.VendorId) +
                    "|pid:" + FormatHex(device.ProductId) +
                    "|name:" + name.ToUpperInvariant();
            }
            return descriptor;
        }

        private static string BuildDescription(Sdl3DeviceDescriptor device, string name)
        {
            string ids = "VID_" + FormatHex(device.VendorId) + "&PID_" + FormatHex(device.ProductId);
            return name.Length == 0 ? ids : name + " (" + ids + ")";
        }

        private static string FormatHex(ushort value)
        {
            return value.ToString("X4", CultureInfo.InvariantCulture);
        }
    }
}
