using System;
using System.Collections.Generic;
using System.IO;
using InputStitch;

internal static class SlotAcquisitionTests
{
    private sealed class FakeBackend : IDeviceHidingBackend
    {
        public bool Available = true;
        public readonly List<GamingDeviceDescriptor> Devices = new List<GamingDeviceDescriptor>();
        public Func<IList<GamingDeviceDescriptor>> Enumerate;
        public bool IsAvailable { get { return Available; } }
        public string BackendName { get { return "fake"; } }
        public IList<GamingDeviceDescriptor> EnumerateGamingDevices()
        {
            return Enumerate == null ? new List<GamingDeviceDescriptor>(Devices) : Enumerate();
        }
        public ISet<string> GetHiddenDevices() { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
        public ISet<string> GetAllowedApplications() { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
        public bool GetCloakActive() { return false; }
        public void RegisterApplication(string executablePath) { }
        public void UnregisterApplication(string executablePath) { }
        public void HideDevice(string deviceInstancePath) { }
        public void UnhideDevice(string deviceInstancePath) { }
        public void SetCloakActive(bool active) { }
    }

    private sealed class FakePnp : IPnpDeviceControl
    {
        public bool Available = true;
        public bool Elevated = true;
        public readonly Dictionary<string, bool> Enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Calls = new List<string>();
        public string FailDisablePath = "";
        public string FailEnablePath = "";
        public Action<string, bool> Changed;
        public bool IsAvailable { get { return Available; } }
        public bool IsElevated { get { return Elevated; } }
        public bool IsEnabled(string path)
        {
            Calls.Add("status:" + path);
            bool enabled;
            if (!Enabled.TryGetValue(path, out enabled)) throw new InvalidOperationException("unknown PnP path " + path);
            return enabled;
        }
        public void Disable(string path)
        {
            Calls.Add("disable:" + path);
            if (string.Equals(path, FailDisablePath, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("disable failed " + path);
            Enabled[path] = false;
            if (Changed != null) Changed(path, false);
        }
        public void Enable(string path)
        {
            Calls.Add("enable:" + path);
            if (string.Equals(path, FailEnablePath, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("enable failed " + path);
            Enabled[path] = true;
            if (Changed != null) Changed(path, true);
        }
    }

    private sealed class FakeJournal : ISlotAcquisitionJournal
    {
        public SlotAcquisitionRecoveryRecord Record;
        public int Saves;
        public int Clears;
        public readonly List<string> Events = new List<string>();
        public SlotAcquisitionRecoveryRecord Load() { return Record; }
        public void Save(SlotAcquisitionRecoveryRecord record)
        {
            Saves++;
            Events.Add("journal-save");
            Record = new SlotAcquisitionRecoveryRecord
            {
                TemporarilyDisabledPnpDevices = new List<string>(record.TemporarilyDisabledPnpDevices ?? new List<string>())
            };
        }
        public void Clear() { Clears++; Events.Add("journal-clear"); Record = null; }
    }

    private sealed class Rig
    {
        public readonly FakeBackend Backend = new FakeBackend();
        public readonly FakePnp Pnp = new FakePnp();
        public readonly FakeJournal Journal = new FakeJournal();
        public readonly List<string> Events = new List<string>();
        public bool VirtualConnected = true;
        public int VirtualSlot = 2;
        public int SlotOnConnect = 0;
        public int InitialExternalCount = 2;
        public int CurrentExternalCount = 2;
        public bool DropVirtualAfterExternalRestore;

        public Rig()
        {
            Pnp.Enabled["XUSB-A"] = true;
            Pnp.Enabled["XUSB-B"] = true;
            Backend.Devices.Add(Device("HID-A", "XUSB-A", "Pad A"));
            Backend.Devices.Add(Device("HID-B", "XUSB-B", "Pad B"));
            Pnp.Changed = delegate(string path, bool enabled)
            {
                Events.Add((enabled ? "enable:" : "disable:") + path);
                foreach (GamingDeviceDescriptor device in Backend.Devices)
                    if (device != null && string.Equals(device.XusbDeviceInstancePath, path, StringComparison.OrdinalIgnoreCase))
                        device.Present = enabled;
                RecomputeExternal();
                if (enabled && DropVirtualAfterExternalRestore) VirtualSlot = 1;
            };
        }

        public XInputSlotAcquisitionCoordinator Coordinator()
        {
            return new XInputSlotAcquisitionCoordinator(
                Backend,
                Pnp,
                delegate { Events.Add("virtual-disconnect"); VirtualConnected = false; VirtualSlot = -1; },
                delegate { Events.Add("virtual-connect"); VirtualConnected = true; VirtualSlot = SlotOnConnect; },
                delegate { return VirtualConnected ? VirtualSlot : -1; },
                delegate
                {
                    if (!VirtualConnected) return ExternalSlots(CurrentExternalCount, -1);
                    return ExternalSlots(CurrentExternalCount, VirtualSlot);
                },
                Journal,
                delegate(int ms) { });
        }

        public void RecomputeExternal()
        {
            int count = 0;
            foreach (bool enabled in Pnp.Enabled.Values) if (enabled) count++;
            CurrentExternalCount = Math.Min(InitialExternalCount, count);
        }
    }

    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
        {
            Console.WriteLine("FAIL slot acquisition: " + message);
            throw new Exception("Check failed: " + message);
        }
    }

    private static GamingDeviceDescriptor Device(string hid, string xusb, string name)
    {
        return new GamingDeviceDescriptor
        {
            Present = true,
            GamingDevice = true,
            Product = name,
            DeviceInstancePath = hid,
            XusbDeviceInstancePath = xusb,
            BaseContainerDeviceInstancePath = "BASE-" + name
        };
    }

    private static int[] ExternalSlots(int count, int ownSlot)
    {
        List<int> slots = new List<int>();
        for (int i = 0; i < 4 && slots.Count < count; i++)
            if (i != ownSlot) slots.Add(i);
        return slots.ToArray();
    }

    private static List<GamingDeviceDescriptor> Select(params GamingDeviceDescriptor[] devices)
    {
        return new List<GamingDeviceDescriptor>(devices);
    }

    public static int Main()
    {
        // File journal is independent from HidHide recovery and survives a round trip.
        string journalPath = Path.Combine(Path.GetTempPath(), "InputStitch-SlotJournal-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            FileSlotAcquisitionJournal journal = new FileSlotAcquisitionJournal(journalPath);
            journal.Save(new SlotAcquisitionRecoveryRecord { TemporarilyDisabledPnpDevices = new List<string> { "A", "B" } });
            SlotAcquisitionRecoveryRecord loaded = journal.Load();
            Check(loaded != null && loaded.TemporarilyDisabledPnpDevices.Count == 2 && loaded.TemporarilyDisabledPnpDevices[1] == "B",
                "slot-acquisition recovery journal round trips independently");
            journal.Clear();
            Check(journal.Load() == null && !File.Exists(journalPath), "slot-acquisition journal clears after recovery");
        }
        finally { try { if (File.Exists(journalPath)) File.Delete(journalPath); } catch { } }

        Rig already = new Rig();
        already.VirtualSlot = 0;
        SlotAcquisitionResult r = already.Coordinator().Acquire(Select(already.Backend.Devices[0]), 0);
        Check(r.State == SlotAcquisitionState.AlreadyTarget && already.VirtualSlot == 0 &&
              already.Events.Count == 2 && already.Events[0] == "virtual-disconnect" && already.Events[1] == "virtual-connect",
            "already-slot-zero path performs only own-virtual identity disconnect/reconnect");
        Check(already.Pnp.Calls.Count == 0 && already.Journal.Saves == 0 &&
              r.ExternalDeviceInstancePaths.Count == 1 && r.ExternalDeviceInstancePaths[0] == "HID-A",
            "already-slot-zero identity verification does not cycle PnP devices and returns external HidHide identity");

        Rig noAdmin = new Rig();
        noAdmin.Pnp.Elevated = false;
        r = noAdmin.Coordinator().Acquire(Select(noAdmin.Backend.Devices[0]), 0);
        Check(r.State == SlotAcquisitionState.ElevationRequired && noAdmin.Events.Count == 0 && noAdmin.Pnp.Calls.Count == 0,
            "non-elevated slot acquisition refuses before mutation");

        Rig noSources = new Rig();
        noSources.InitialExternalCount = noSources.CurrentExternalCount = 0;
        r = noSources.Coordinator().Acquire(Select(noSources.Backend.Devices[0]), 0);
        Check(r.State == SlotAcquisitionState.NoExternalSources && noSources.Events.Count == 0,
            "no external XInput source refuses before disconnecting virtual controller");

        Rig ownSelected = new Rig();
        GamingDeviceDescriptor ownDescriptor = Device("HID-OWN", "XUSB-OWN", "InputStitch virtual");
        ownSelected.Backend.Devices.Add(ownDescriptor);
        ownSelected.Backend.Enumerate = delegate
        {
            // The selected own virtual device disappears once InputStitch disconnects it.
            return new List<GamingDeviceDescriptor> { ownSelected.Backend.Devices[0], ownSelected.Backend.Devices[1] };
        };
        r = ownSelected.Coordinator().Acquire(Select(ownDescriptor), 0);
        Check(r.State == SlotAcquisitionState.NoExternalSources && ownSelected.VirtualConnected && ownSelected.Pnp.Calls.Count == 0,
            "selected device that disappears with own virtual controller is excluded before PnP disable");

        Rig noXusb = new Rig();
        GamingDeviceDescriptor hidOnly = Device("HID-C", "", "HID-only pad");
        noXusb.Backend.Devices.Add(hidOnly);
        r = noXusb.Coordinator().Acquire(Select(hidOnly), 0);
        Check(r.State == SlotAcquisitionState.NoExternalSources && noXusb.VirtualConnected && noXusb.Pnp.Calls.Count == 0,
            "HID-only device is excluded from XInput takeover and virtual output is restored");

        Rig selectAll = new Rig();
        selectAll.VirtualSlot = 0;
        GamingDeviceDescriptor selectAllOwn = Device("HID-OWN", "XUSB-OWN", "InputStitch virtual");
        selectAll.Backend.Devices.Add(selectAllOwn);
        selectAll.Backend.Enumerate = delegate
        {
            List<GamingDeviceDescriptor> result = new List<GamingDeviceDescriptor>
            {
                selectAll.Backend.Devices[0],
                selectAll.Backend.Devices[1]
            };
            if (selectAll.VirtualConnected) result.Add(selectAllOwn);
            return result;
        };
        r = selectAll.Coordinator().Acquire(Select(selectAll.Backend.Devices[0], selectAll.Backend.Devices[1], selectAllOwn), 0);
        Check(r.State == SlotAcquisitionState.AlreadyTarget && r.ExternalDeviceInstancePaths.Contains("HID-A") &&
              r.ExternalDeviceInstancePaths.Contains("HID-B") && !r.ExternalDeviceInstancePaths.Contains("HID-OWN"),
            "Select All automatically excludes InputStitch's own virtual HidHide identity");
        Check(selectAll.Pnp.Calls.Count == 0, "self-identity filtering at existing slot zero never cycles physical PnP devices");

        Rig success = new Rig();
        r = success.Coordinator().Acquire(Select(success.Backend.Devices[0], success.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Acquired && success.VirtualSlot == 0,
            "slot acquisition reconnects InputStitch into slot zero");
        Check(success.Pnp.Enabled["XUSB-A"] && success.Pnp.Enabled["XUSB-B"] && success.CurrentExternalCount == 2,
            "all cycled external controllers are re-enabled after slot zero acquisition");
        Check(success.Journal.Saves == 1 && success.Journal.Clears == 1 && success.Journal.Record == null,
            "PnP recovery journal exists during mutation and clears only after originals return");
        Check(success.Events.IndexOf("virtual-disconnect") < success.Events.IndexOf("disable:XUSB-A") &&
              success.Events.IndexOf("disable:XUSB-B") < success.Events.IndexOf("virtual-connect") &&
              success.Events.IndexOf("virtual-connect") < success.Events.IndexOf("enable:XUSB-B"),
            "transaction order is disconnect-own -> disable originals -> connect-own-zero -> restore originals");

        Rig subset = new Rig();
        r = subset.Coordinator().Acquire(Select(subset.Backend.Devices[0]), 0);
        Check(r.State == SlotAcquisitionState.Acquired && subset.VirtualSlot == 0,
            "selecting one controller still acquires slot zero when another external controller exists");
        Check(subset.Pnp.Calls.Contains("disable:XUSB-A") && subset.Pnp.Calls.Contains("disable:XUSB-B"),
            "slot acquisition temporarily cycles every external XUSB controller, not only the selected hide target");
        Check(r.ExternalDeviceInstancePaths.Count == 1 && r.ExternalDeviceInstancePaths[0] == "HID-A",
            "final HidHide identity set contains only the explicitly selected external controller");

        Rig stuckZero = new Rig();
        stuckZero.SlotOnConnect = 2;
        r = stuckZero.Coordinator().Acquire(Select(stuckZero.Backend.Devices[0], stuckZero.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Failed && stuckZero.Pnp.Enabled["XUSB-A"] && stuckZero.Pnp.Enabled["XUSB-B"],
            "reserved/stuck slot zero fails closed and restores original devices");
        Check(stuckZero.Journal.Record == null && stuckZero.VirtualConnected,
            "slot-zero acquisition failure completes rollback and clears journal only after restore");

        Rig disableMidway = new Rig();
        disableMidway.Pnp.FailDisablePath = "XUSB-B";
        r = disableMidway.Coordinator().Acquire(Select(disableMidway.Backend.Devices[0], disableMidway.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Failed && disableMidway.Pnp.Enabled["XUSB-A"] && disableMidway.Pnp.Enabled["XUSB-B"],
            "second-device disable failure re-enables the first controller and leaves the second enabled");

        Rig virtualFalls = new Rig();
        virtualFalls.DropVirtualAfterExternalRestore = true;
        r = virtualFalls.Coordinator().Acquire(Select(virtualFalls.Backend.Devices[0], virtualFalls.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Failed && virtualFalls.Pnp.Enabled["XUSB-A"] && virtualFalls.Pnp.Enabled["XUSB-B"],
            "virtual pad leaving slot zero when originals return invalidates acquisition");

        Rig enableFailure = new Rig();
        enableFailure.Pnp.FailEnablePath = "XUSB-A";
        r = enableFailure.Coordinator().Acquire(Select(enableFailure.Backend.Devices[0], enableFailure.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Failed && enableFailure.Journal.Record != null,
            "failed PnP restore retains recovery journal rather than claiming cleanup");

        Rig identityLoss = new Rig();
        int identityEnumerations = 0;
        identityLoss.Backend.Enumerate = delegate
        {
            identityEnumerations++;
            if (identityEnumerations == 1)
                return new List<GamingDeviceDescriptor> { identityLoss.Backend.Devices[0], identityLoss.Backend.Devices[1] };
            return new List<GamingDeviceDescriptor> { identityLoss.Backend.Devices[0] };
        };
        r = identityLoss.Coordinator().Acquire(Select(identityLoss.Backend.Devices[0], identityLoss.Backend.Devices[1]), 0);
        Check(r.State == SlotAcquisitionState.Failed && identityLoss.Pnp.Enabled["XUSB-A"] && identityLoss.Pnp.Enabled["XUSB-B"],
            "matching external-slot count is insufficient when one cycled XUSB identity fails to return");

        FakePnp recoveryPnp = new FakePnp();
        recoveryPnp.Enabled["XUSB-A"] = false;
        recoveryPnp.Enabled["XUSB-B"] = false;
        FakeJournal recoveryJournal = new FakeJournal();
        recoveryJournal.Record = new SlotAcquisitionRecoveryRecord { TemporarilyDisabledPnpDevices = new List<string> { "XUSB-A", "XUSB-B" } };
        string recoveryError = SlotAcquisitionRecovery.RestorePending(recoveryPnp, recoveryJournal);
        Check(recoveryError == "" && recoveryPnp.Enabled["XUSB-A"] && recoveryPnp.Enabled["XUSB-B"] && recoveryJournal.Record == null,
            "next-launch recovery re-enables every temporarily disabled PnP controller and clears journal");

        FakePnp recoveryNoAdmin = new FakePnp { Elevated = false };
        FakeJournal retainedJournal = new FakeJournal();
        retainedJournal.Record = new SlotAcquisitionRecoveryRecord { TemporarilyDisabledPnpDevices = new List<string> { "XUSB-A" } };
        recoveryError = SlotAcquisitionRecovery.RestorePending(recoveryNoAdmin, retainedJournal);
        Check(recoveryError.IndexOf("Administrator", StringComparison.OrdinalIgnoreCase) >= 0 && retainedJournal.Record != null,
            "recovery without elevation keeps journal and gives restart/admin guidance");

        // Multiple HID entries can represent one XUSB source; the coordinator must disable it once.
        Rig duplicateXusb = new Rig();
        GamingDeviceDescriptor duplicate = Device("HID-A-SECOND", "XUSB-A", "Pad A second collection");
        duplicateXusb.Backend.Devices.Add(duplicate);
        duplicateXusb.InitialExternalCount = duplicateXusb.CurrentExternalCount = 1;
        duplicateXusb.Pnp.Enabled["XUSB-B"] = false;
        duplicateXusb.Backend.Devices[1].Present = false;
        r = duplicateXusb.Coordinator().Acquire(Select(duplicateXusb.Backend.Devices[0], duplicate), 0);
        Check(r.State == SlotAcquisitionState.Acquired && duplicateXusb.Pnp.Calls.FindAll(delegate(string x) { return x == "disable:XUSB-A"; }).Count == 1,
            "multiple HID collections sharing one XUSB device are deduplicated before cycling");

        Rig tooMany = new Rig();
        GamingDeviceDescriptor padC = Device("HID-C", "XUSB-C", "Pad C");
        GamingDeviceDescriptor padD = Device("HID-D", "XUSB-D", "Pad D");
        tooMany.Backend.Devices.Add(padC);
        tooMany.Backend.Devices.Add(padD);
        tooMany.Pnp.Enabled["XUSB-C"] = true;
        tooMany.Pnp.Enabled["XUSB-D"] = true;
        tooMany.InitialExternalCount = tooMany.CurrentExternalCount = 3;
        r = tooMany.Coordinator().Acquire(Select(tooMany.Backend.Devices[0]), 0);
        Check(r.State == SlotAcquisitionState.UnsupportedSource &&
              tooMany.Pnp.Calls.FindAll(delegate(string x) { return x.StartsWith("disable:", StringComparison.Ordinal); }).Count == 0,
            "more than three external XUSB controllers is rejected before any PnP disable because XInput cannot route all four beside slot zero");

        Console.WriteLine("PASS slot acquisition: " + checks + " checks; fake PnP/XInput only, no real device was disabled.");
        return 0;
    }
}
