using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace InputStitch
{
    internal interface IGamingDeviceDiscovery
    {
        bool IsAvailable { get; }
        string ProviderName { get; }
        IList<GamingDeviceDescriptor> EnumerateGamingDevices();
    }

    internal sealed class HidHideGamingDeviceDiscovery : IGamingDeviceDiscovery
    {
        private readonly IDeviceHidingBackend backend;

        internal HidHideGamingDeviceDiscovery(IDeviceHidingBackend hidingBackend)
        {
            backend = hidingBackend;
        }

        public bool IsAvailable
        {
            get
            {
                try { return backend != null && backend.IsAvailable; }
                catch { return false; }
            }
        }

        public string ProviderName { get { return "HidHide"; } }

        public IList<GamingDeviceDescriptor> EnumerateGamingDevices()
        {
            if (backend == null) return new List<GamingDeviceDescriptor>();
            return backend.EnumerateGamingDevices();
        }
    }

    // Native fallback for the controller family InputStitch currently consumes through XInput.
    // It intentionally enumerates only present XnaComposite/XUSB devices rather than pretending to
    // be a universal HID provider. Broader HID/SDL providers belong behind the same discovery seam.
    internal sealed class WindowsXusbGamingDeviceDiscovery : IGamingDeviceDiscovery
    {
        private static readonly Guid XnaCompositeClassGuid = new Guid("D61CA365-5AF4-4486-998B-9DB4734C6CA3");
        private static readonly DEVPROPKEY DeviceContainerId = new DEVPROPKEY(
            new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint SPDRP_DEVICEDESC = 0x00000000;
        private const uint SPDRP_HARDWAREID = 0x00000001;
        private const uint SPDRP_MFG = 0x0000000B;
        private const uint SPDRP_FRIENDLYNAME = 0x0000000C;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const uint CR_SUCCESS = 0x00000000;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        public bool IsAvailable { get { return Environment.OSVersion.Platform == PlatformID.Win32NT; } }
        public string ProviderName { get { return "Windows XUSB/PnP"; } }

        public IList<GamingDeviceDescriptor> EnumerateGamingDevices()
        {
            List<GamingDeviceDescriptor> result = new List<GamingDeviceDescriptor>();
            if (!IsAvailable) return result;

            Guid classGuid = XnaCompositeClassGuid;
            IntPtr set = SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, DIGCF_PRESENT);
            if (set == InvalidHandleValue) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate Windows XUSB devices.");
            try
            {
                uint index = 0;
                while (true)
                {
                    SP_DEVINFO_DATA data = new SP_DEVINFO_DATA();
                    data.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                    if (!SetupDiEnumDeviceInfo(set, index, ref data))
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error == 259) break; // ERROR_NO_MORE_ITEMS
                        throw new Win32Exception(error, "Could not enumerate a Windows XUSB device.");
                    }
                    index++;

                    string instanceId = ReadInstanceId(set, ref data);
                    if (string.IsNullOrWhiteSpace(instanceId)) continue;
                    string friendly = ReadRegistryString(set, ref data, SPDRP_FRIENDLYNAME);
                    string description = ReadRegistryString(set, ref data, SPDRP_DEVICEDESC);
                    string manufacturer = ReadRegistryString(set, ref data, SPDRP_MFG);
                    string hardwareIds = ReadRegistryMultiString(set, ref data, SPDRP_HARDWAREID);
                    string container = ReadContainerId(data.DevInst);
                    bool virtualBus = HasAncestorContaining(data.DevInst, "VIGEMBUS");

                    GamingDeviceDescriptor descriptor = new GamingDeviceDescriptor();
                    descriptor.Present = true;
                    descriptor.GamingDevice = true;
                    descriptor.Vendor = manufacturer;
                    descriptor.Product = string.IsNullOrWhiteSpace(friendly) ? description : friendly;
                    descriptor.Description = description;
                    descriptor.DeviceInstancePath = instanceId;
                    descriptor.XusbDeviceInstancePath = instanceId;
                    descriptor.ContainerId = container;
                    descriptor.VirtualBus = virtualBus;
                    descriptor.NativeXusbMetadata = true;
                    if (string.IsNullOrWhiteSpace(descriptor.Product) && !string.IsNullOrWhiteSpace(hardwareIds)) descriptor.Product = hardwareIds;
                    result.Add(descriptor);
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
            return result;
        }

        private static string ReadInstanceId(IntPtr set, ref SP_DEVINFO_DATA data)
        {
            uint required = 0;
            SetupDiGetDeviceInstanceId(set, ref data, null, 0, out required);
            if (required == 0) return "";
            StringBuilder buffer = new StringBuilder((int)required + 1);
            if (!SetupDiGetDeviceInstanceId(set, ref data, buffer, (uint)buffer.Capacity, out required))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read an XUSB device instance ID.");
            return buffer.ToString();
        }

        private static string ReadRegistryString(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
        {
            byte[] raw = ReadRegistryProperty(set, ref data, property);
            if (raw == null || raw.Length < 2) return "";
            return Encoding.Unicode.GetString(raw).TrimEnd('\0').Trim();
        }

        private static string ReadRegistryMultiString(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
        {
            byte[] raw = ReadRegistryProperty(set, ref data, property);
            if (raw == null || raw.Length < 2) return "";
            string text = Encoding.Unicode.GetString(raw).TrimEnd('\0');
            string[] values = text.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            return values.Length == 0 ? "" : values[0].Trim();
        }

        private static byte[] ReadRegistryProperty(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
        {
            uint type;
            uint required = 0;
            SetupDiGetDeviceRegistryProperty(set, ref data, property, out type, null, 0, out required);
            int error = Marshal.GetLastWin32Error();
            if (required == 0)
            {
                if (error == 0 || error == 13) return null; // absent/invalid data is optional metadata
                return null;
            }
            byte[] buffer = new byte[required];
            if (!SetupDiGetDeviceRegistryProperty(set, ref data, property, out type, buffer, (uint)buffer.Length, out required))
            {
                error = Marshal.GetLastWin32Error();
                if (error == ERROR_INSUFFICIENT_BUFFER && required > buffer.Length)
                {
                    buffer = new byte[required];
                    if (SetupDiGetDeviceRegistryProperty(set, ref data, property, out type, buffer, (uint)buffer.Length, out required)) return buffer;
                }
                return null;
            }
            return buffer;
        }

        private static string ReadContainerId(uint devInst)
        {
            uint propertyType;
            uint size = 16;
            byte[] buffer = new byte[size];
            DEVPROPKEY key = DeviceContainerId;
            uint result = CM_Get_DevNode_Property(devInst, ref key, out propertyType, buffer, ref size, 0);
            if (result != CR_SUCCESS || size < 16) return "";
            try { return new Guid(buffer).ToString("D").ToUpperInvariant(); }
            catch { return ""; }
        }

        private static bool HasAncestorContaining(uint devInst, string marker)
        {
            uint current = devInst;
            for (int depth = 0; depth < 12; depth++)
            {
                uint parent;
                if (CM_Get_Parent(out parent, current, 0) != CR_SUCCESS) return false;
                current = parent;
                string id = ReadConfigManagerDeviceId(current);
                if (!string.IsNullOrWhiteSpace(id) && id.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static string ReadConfigManagerDeviceId(uint devInst)
        {
            uint length;
            if (CM_Get_Device_ID_Size(out length, devInst, 0) != CR_SUCCESS || length == 0) return "";
            StringBuilder buffer = new StringBuilder((int)length + 2);
            return CM_Get_Device_ID(devInst, buffer, buffer.Capacity, 0) == CR_SUCCESS ? buffer.ToString() : "";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
            public DEVPROPKEY(Guid formatId, uint propertyId) { fmtid = formatId; pid = propertyId; }
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId, uint DeviceInstanceIdSize, out uint RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property, out uint PropertyRegDataType, byte[] PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_DevNode_PropertyW")]
        private static extern uint CM_Get_DevNode_Property(uint dnDevInst, ref DEVPROPKEY PropertyKey,
            out uint PropertyType, byte[] PropertyBuffer, ref uint PropertyBufferSize, uint ulFlags);

        [DllImport("CfgMgr32.dll")]
        private static extern uint CM_Get_Parent(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("CfgMgr32.dll")]
        private static extern uint CM_Get_Device_ID_Size(out uint pulLen, uint dnDevInst, uint ulFlags);

        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW")]
        private static extern uint CM_Get_Device_ID(uint dnDevInst, StringBuilder Buffer, int BufferLen, uint ulFlags);
    }

    internal sealed class CompositeGamingDeviceDiscovery : IGamingDeviceDiscovery
    {
        private readonly List<IGamingDeviceDiscovery> providers = new List<IGamingDeviceDiscovery>();

        internal CompositeGamingDeviceDiscovery(params IGamingDeviceDiscovery[] sources)
        {
            if (sources == null) return;
            foreach (IGamingDeviceDiscovery source in sources) if (source != null) providers.Add(source);
        }

        public bool IsAvailable
        {
            get
            {
                foreach (IGamingDeviceDiscovery provider in providers)
                    try { if (provider.IsAvailable) return true; } catch { }
                return false;
            }
        }

        public string ProviderName
        {
            get
            {
                List<string> names = new List<string>();
                foreach (IGamingDeviceDiscovery provider in providers)
                {
                    bool available = false;
                    try { available = provider.IsAvailable; } catch { }
                    if (available && !string.IsNullOrWhiteSpace(provider.ProviderName)) names.Add(provider.ProviderName);
                }
                return names.Count == 0 ? "none" : string.Join(" + ", names.ToArray());
            }
        }

        public IList<GamingDeviceDescriptor> EnumerateGamingDevices()
        {
            List<GamingDeviceDescriptor> merged = new List<GamingDeviceDescriptor>();
            List<Exception> failures = new List<Exception>();
            bool anySucceeded = false;
            foreach (IGamingDeviceDiscovery provider in providers)
            {
                bool available = false;
                try { available = provider.IsAvailable; } catch { }
                if (!available) continue;
                try
                {
                    IList<GamingDeviceDescriptor> items = provider.EnumerateGamingDevices();
                    anySucceeded = true;
                    if (items == null) continue;
                    foreach (GamingDeviceDescriptor item in items) MergeDescriptor(merged, item);
                }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (!anySucceeded && failures.Count != 0) throw failures[0];
            return merged;
        }

        internal static void MergeDescriptor(List<GamingDeviceDescriptor> target, GamingDeviceDescriptor incoming)
        {
            if (target == null || incoming == null) return;
            GamingDeviceDescriptor existing = null;
            foreach (GamingDeviceDescriptor candidate in target)
            {
                if (candidate == null) continue;
                if (SameNonEmpty(candidate.XusbDeviceInstancePath, incoming.XusbDeviceInstancePath) ||
                    SameNonEmpty(candidate.DeviceInstancePath, incoming.DeviceInstancePath) ||
                    SameNonEmpty(candidate.ContainerId, incoming.ContainerId))
                {
                    existing = candidate;
                    break;
                }
            }
            if (existing == null)
            {
                target.Add(Clone(incoming));
                return;
            }

            existing.Present = existing.Present || incoming.Present;
            existing.GamingDevice = existing.GamingDevice || incoming.GamingDevice;
            Prefer(ref existing.Vendor, incoming.Vendor);
            Prefer(ref existing.Product, incoming.Product);
            Prefer(ref existing.SerialNumber, incoming.SerialNumber);
            Prefer(ref existing.Description, incoming.Description);
            // HidHide's HID-facing path and XUSB correlation are more informative than an XUSB-only
            // native row, so do not overwrite a distinct HID path once present.
            if (string.IsNullOrWhiteSpace(existing.DeviceInstancePath))
                Prefer(ref existing.DeviceInstancePath, incoming.DeviceInstancePath);
            else if (string.Equals(existing.DeviceInstancePath, existing.XusbDeviceInstancePath, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(incoming.DeviceInstancePath) &&
                !string.Equals(incoming.DeviceInstancePath, incoming.XusbDeviceInstancePath, StringComparison.OrdinalIgnoreCase))
                existing.DeviceInstancePath = incoming.DeviceInstancePath;
            Prefer(ref existing.XusbDeviceInstancePath, incoming.XusbDeviceInstancePath);
            Prefer(ref existing.BaseContainerDeviceInstancePath, incoming.BaseContainerDeviceInstancePath);
            Prefer(ref existing.ContainerId, incoming.ContainerId);
            existing.VirtualBus = existing.VirtualBus || incoming.VirtualBus;
            existing.NativeXusbMetadata = existing.NativeXusbMetadata || incoming.NativeXusbMetadata;
        }

        private static GamingDeviceDescriptor Clone(GamingDeviceDescriptor source)
        {
            return new GamingDeviceDescriptor
            {
                Present = source.Present,
                GamingDevice = source.GamingDevice,
                Vendor = source.Vendor ?? "",
                Product = source.Product ?? "",
                SerialNumber = source.SerialNumber ?? "",
                Description = source.Description ?? "",
                DeviceInstancePath = source.DeviceInstancePath ?? "",
                XusbDeviceInstancePath = source.XusbDeviceInstancePath ?? "",
                BaseContainerDeviceInstancePath = source.BaseContainerDeviceInstancePath ?? "",
                ContainerId = source.ContainerId ?? "",
                VirtualBus = source.VirtualBus,
                NativeXusbMetadata = source.NativeXusbMetadata
            };
        }

        private static bool SameNonEmpty(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) &&
                string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void Prefer(ref string target, string value)
        {
            if (string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(value)) target = value;
        }
    }
}
