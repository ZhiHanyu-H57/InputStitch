using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InputStitch
{
    [Serializable]
    public sealed class GuestNetworkPermission
    {
        public int SchemaVersion = 1;
        public string Purpose = "InputStitch-Guest-WholeComputer-WFP-Acceptance";
        public string ComputerName = "";
        public string GuestUuid = "";
        public string Token = "";
        public DateTime ExpiresUtc;
        public int MaximumBlockSeconds = 90;
    }

    internal sealed class GuestMachineIdentity
    {
        internal string ComputerName, Manufacturer, Model, Bios, Uuid;
    }

    internal static class GuestNetworkGuard
    {
        internal const string ExpectedManufacturer = "InputStitch Acceptance";
        internal const string ExpectedModel = "InputStitch Isolated Network Guest";
        internal const string MarkerName = "ALLOW-GUEST-NETWORK-ACCEPTANCE.xml";
        internal static string BaseDirectory { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        internal static string MarkerPath { get { return Path.Combine(BaseDirectory, MarkerName); } }

        internal static GuestMachineIdentity ReadMachine()
        {
            // Firmware reads avoid three cold WMI queries in each helper. Under
            // software CPU emulation those queries exceeded the production pipe
            // startup deadline before the helper could enter its real code.
            const uint rsmb = 0x52534d42;
            uint size = GetSystemFirmwareTable(rsmb, 0, null, 0);
            if (size < 8 || size > 1024 * 1024) throw new InvalidOperationException("SMBIOS identity is unavailable.");
            byte[] data = new byte[size];
            if (GetSystemFirmwareTable(rsmb, 0, data, size) != size || BitConverter.ToUInt32(data, 4) > size - 8)
                throw new InvalidOperationException("Invalid SMBIOS identity table.");
            if (data[1] < 2 || (data[1] == 2 && data[2] < 6)) throw new InvalidOperationException("The guest needs SMBIOS 2.6 or newer.");
            GuestMachineIdentity identity = new GuestMachineIdentity { ComputerName = Environment.MachineName };
            int limit = 8 + (int)BitConverter.ToUInt32(data, 4);
            for (int offset = 8; offset + 4 <= limit; )
            {
                int type = data[offset], length = data[offset + 1];
                if (length < 4 || offset + length > limit) throw new InvalidOperationException("Truncated SMBIOS structure.");
                int strings = offset + length, end = strings;
                while (end + 1 < limit && (data[end] != 0 || data[end + 1] != 0)) end++;
                if (end + 1 >= limit) throw new InvalidOperationException("Unterminated SMBIOS strings.");
                if (type == 0 && length >= 6)
                    identity.Bios = FirmwareString(data, strings, end, data[offset + 4]) + " " + FirmwareString(data, strings, end, data[offset + 5]);
                if (type == 1 && length >= 24)
                {
                    identity.Manufacturer = FirmwareString(data, strings, end, data[offset + 4]);
                    identity.Model = FirmwareString(data, strings, end, data[offset + 5]);
                    byte[] uuid = new byte[16]; Array.Copy(data, offset + 8, uuid, 0, 16);
                    identity.Uuid = new Guid(uuid).ToString();
                }
                if (type == 127) break;
                offset = end + 2;
            }
            return identity;
        }
        private static string FirmwareString(byte[] data, int start, int end, int index)
        {
            if (index == 0) return "";
            for (int number = 1, offset = start; offset <= end; number++)
            {
                int next = offset; while (next < end && data[next] != 0) next++;
                if (number == index) return Encoding.ASCII.GetString(data, offset, next - offset).Trim();
                offset = next + 1;
            }
            throw new InvalidOperationException("Invalid SMBIOS string index.");
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetSystemFirmwareTable(uint provider, uint table, [Out] byte[] buffer, uint size);

        private static GuestMachineIdentity ReadMachineViaWmi()
        {
            GuestMachineIdentity identity = new GuestMachineIdentity { ComputerName = Environment.MachineName };
            using (ManagementObjectSearcher search = new ManagementObjectSearcher("SELECT Manufacturer,Model FROM Win32_ComputerSystem"))
                foreach (ManagementObject item in search.Get())
                    using (item) { identity.Manufacturer = Convert.ToString(item["Manufacturer"]); identity.Model = Convert.ToString(item["Model"]); break; }
            using (ManagementObjectSearcher search = new ManagementObjectSearcher("SELECT Manufacturer,SMBIOSBIOSVersion FROM Win32_BIOS"))
                foreach (ManagementObject item in search.Get())
                    using (item) { identity.Bios = Convert.ToString(item["Manufacturer"]) + " " + Convert.ToString(item["SMBIOSBIOSVersion"]); break; }
            using (ManagementObjectSearcher search = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                foreach (ManagementObject item in search.Get()) using (item) { identity.Uuid = Convert.ToString(item["UUID"]); break; }
            return identity;
        }

        internal static void ValidateMachine(GuestMachineIdentity identity)
        {
            if (identity == null) throw new InvalidOperationException("Machine identity is unavailable.");
            string combined = (identity.Manufacturer ?? "") + " " + (identity.Model ?? "") + " " + (identity.Bios ?? "");
            if (combined.IndexOf("HUAWEI", StringComparison.OrdinalIgnoreCase) >= 0 || combined.IndexOf("KLVG-XX", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new UnauthorizedAccessException("The physical HUAWEI host is explicitly prohibited.");
            if (!string.Equals(identity.Manufacturer, ExpectedManufacturer, StringComparison.Ordinal) || !string.Equals(identity.Model, ExpectedModel, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("This is not the dedicated InputStitch isolated guest.");
            string bios = identity.Bios ?? "";
            if (bios.IndexOf("QEMU", StringComparison.OrdinalIgnoreCase) < 0 && bios.IndexOf("SeaBIOS", StringComparison.OrdinalIgnoreCase) < 0 &&
                bios.IndexOf("EDK II", StringComparison.OrdinalIgnoreCase) < 0 && bios.IndexOf("EDKII", StringComparison.OrdinalIgnoreCase) < 0 && bios.IndexOf("OVMF", StringComparison.OrdinalIgnoreCase) < 0 &&
                bios.IndexOf("Bochs", StringComparison.OrdinalIgnoreCase) < 0)
                throw new UnauthorizedAccessException("The dedicated guest BIOS has no expected virtualization signature.");
            Guid uuid;
            if (!Guid.TryParse(identity.Uuid, out uuid) || uuid == Guid.Empty || uuid == new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"))
                throw new UnauthorizedAccessException("Assign a nonzero unique QEMU UUID before using this fixture.");
        }

        internal static GuestNetworkPermission Demand(string suppliedToken, bool helperEntry)
        {
            // Check physical machine identity before reading permission files or taking any network action.
            GuestMachineIdentity identity = ReadMachine();
            ValidateMachine(identity);
            string root = Path.GetPathRoot(BaseDirectory);
            if (BaseDirectory.StartsWith("\\\\", StringComparison.Ordinal) || string.IsNullOrEmpty(root) || new DriveInfo(root).DriveType != DriveType.Fixed)
                throw new UnauthorizedAccessException("Put the fixture and its control/log files on the guest's local fixed disk, never a network share.");
            if (!File.Exists(MarkerPath)) throw new UnauthorizedAccessException("Dedicated guest permission marker is missing.");
            // Parsing fixed fields avoids generating a serializer assembly during
            // every helper's deadline-sensitive cold startup.
            XmlDocument document = new XmlDocument(); document.XmlResolver = null;
            using (XmlReader reader = XmlReader.Create(MarkerPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                document.Load(reader);
            XmlElement marker = document.DocumentElement;
            if (marker == null || marker.Name != "GuestNetworkPermission") throw new UnauthorizedAccessException("Invalid guest permission root.");
            GuestNetworkPermission permission = new GuestNetworkPermission {
                SchemaVersion = XmlConvert.ToInt32(Field(marker, "SchemaVersion")), Purpose = Field(marker, "Purpose"),
                ComputerName = Field(marker, "ComputerName"), GuestUuid = Field(marker, "GuestUuid"), Token = Field(marker, "Token"),
                ExpiresUtc = XmlConvert.ToDateTime(Field(marker, "ExpiresUtc"), XmlDateTimeSerializationMode.RoundtripKind),
                MaximumBlockSeconds = XmlConvert.ToInt32(Field(marker, "MaximumBlockSeconds"))
            };
            Validate(permission, identity, suppliedToken, helperEntry, DateTime.UtcNow);
            return permission;
        }
        private static string Field(XmlElement marker, string name)
        { XmlElement child = marker[name]; if (child == null) throw new UnauthorizedAccessException("Missing guest permission field: " + name); return child.InnerText; }

        internal static void Validate(GuestNetworkPermission permission, GuestMachineIdentity identity, string suppliedToken, bool helperEntry, DateTime now)
        {
            ValidateMachine(identity);
            if (permission == null || permission.SchemaVersion != 1 || permission.Purpose != "InputStitch-Guest-WholeComputer-WFP-Acceptance")
                throw new UnauthorizedAccessException("Invalid dedicated guest permission marker.");
            if (!string.Equals(permission.ComputerName, identity.ComputerName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(permission.GuestUuid, identity.Uuid, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Guest permission belongs to a different machine.");
            if (!ValidToken(permission.Token) || (!helperEntry && !string.Equals(permission.Token, suppliedToken, StringComparison.Ordinal)))
                throw new UnauthorizedAccessException("An explicit matching guest test token is required.");
            DateTime expiry = permission.ExpiresUtc.ToUniversalTime();
            if (expiry <= now || expiry > now.AddHours(24)) throw new UnauthorizedAccessException("Guest permission is expired or exceeds 24 hours.");
            if (permission.MaximumBlockSeconds < 30 || permission.MaximumBlockSeconds > 120)
                throw new UnauthorizedAccessException("Maximum block duration must be between 30 and 120 seconds.");
        }
        internal static bool ValidToken(string token)
        {
            if (token == null || token.Length != 64) return false;
            foreach (char c in token) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }

        internal static int SelfTest()
        {
            int passed = 0;
            GuestMachineIdentity guest = new GuestMachineIdentity { ComputerName = "INPUTSTITCH-TEST", Manufacturer = ExpectedManufacturer, Model = ExpectedModel, Bios = "SeaBIOS QEMU", Uuid = "81b7a503-ef5f-427f-b4cc-0e639be85aaf" };
            GuestNetworkPermission valid = new GuestNetworkPermission { ComputerName = guest.ComputerName, GuestUuid = guest.Uuid, Token = new string('a', 64), ExpiresUtc = DateTime.UtcNow.AddHours(1) };
            Validate(valid, guest, valid.Token, false, DateTime.UtcNow); passed++;
            string[] faults = { "token", "expiry", "long-expiry", "purpose", "computer", "uuid", "duration" };
            foreach (string fault in faults)
            {
                GuestNetworkPermission p = new GuestNetworkPermission { ComputerName = valid.ComputerName, GuestUuid = valid.GuestUuid, Token = valid.Token, ExpiresUtc = valid.ExpiresUtc };
                if (fault == "expiry") p.ExpiresUtc = DateTime.UtcNow.AddSeconds(-1);
                if (fault == "long-expiry") p.ExpiresUtc = DateTime.UtcNow.AddHours(25);
                if (fault == "purpose") p.Purpose = "anything";
                if (fault == "computer") p.ComputerName = "HOST";
                if (fault == "uuid") p.GuestUuid = Guid.NewGuid().ToString();
                if (fault == "duration") p.MaximumBlockSeconds = 121;
                ExpectRejected(delegate { Validate(p, guest, fault == "token" ? new string('b', 64) : p.Token, false, DateTime.UtcNow); }); passed++;
            }
            foreach (GuestMachineIdentity rejected in new GuestMachineIdentity[] {
                new GuestMachineIdentity { Manufacturer = "HUAWEI", Model = "KLVG-XX", Bios = "HUAWEI", Uuid = guest.Uuid },
                new GuestMachineIdentity { Manufacturer = "QEMU", Model = "Standard PC", Bios = "SeaBIOS", Uuid = guest.Uuid },
                new GuestMachineIdentity { Manufacturer = ExpectedManufacturer, Model = ExpectedModel, Bios = "Physical BIOS", Uuid = guest.Uuid },
                new GuestMachineIdentity { Manufacturer = ExpectedManufacturer, Model = ExpectedModel, Bios = "SeaBIOS", Uuid = Guid.Empty.ToString() } })
            { ExpectRejected(delegate { ValidateMachine(rejected); }); passed++; }
            GuestMachineIdentity actual = ReadMachine();
            GuestMachineIdentity wmi = ReadMachineViaWmi();
            if (actual.Manufacturer != wmi.Manufacturer || actual.Model != wmi.Model ||
                !string.Equals(actual.Uuid, wmi.Uuid, StringComparison.OrdinalIgnoreCase) || actual.Bios != wmi.Bios)
                throw new Exception("Firmware identity differs from independent WMI identity.");
            passed++; Console.WriteLine("PASS fast firmware identity matches independent WMI manufacturer/model/BIOS/UUID");
            bool dedicatedGuest = actual.Manufacturer == ExpectedManufacturer && actual.Model == ExpectedModel;
            if (!dedicatedGuest) { ExpectRejected(delegate { ValidateMachine(actual); }); passed++; Console.WriteLine("PASS actual host identity rejected before marker/network access"); }
            else Console.WriteLine("PASS actual machine is the dedicated guest; no WFP calls in self-test");
            ExpectRejected(delegate { Validate(valid, guest, null, false, DateTime.UtcNow); }); passed++;
            Console.WriteLine("RESULT guard self-test " + passed + " passed; WFP mutations=0");
            return 0;
        }
        private static void ExpectRejected(Action action)
        {
            try { action(); } catch (UnauthorizedAccessException) { return; }
            throw new Exception("Guard accepted an invalid machine or permission.");
        }
    }
}
