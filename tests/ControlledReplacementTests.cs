using System;
using System.Collections.Generic;
using System.IO;
using InputStitch;

internal static class ControlledReplacementTests
{
    private sealed class FakeBackend : IDeviceHidingBackend
    {
        public bool Available = true;
        public bool Cloak;
        public readonly HashSet<string> Hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Apps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Calls = new List<string>();
        public Action<string> OnHide;
        public bool FailHide;

        public bool IsAvailable { get { return Available; } }
        public string BackendName { get { return "fake"; } }
        public IList<GamingDeviceDescriptor> EnumerateGamingDevices() { return new List<GamingDeviceDescriptor>(); }
        public ISet<string> GetHiddenDevices() { return new HashSet<string>(Hidden, StringComparer.OrdinalIgnoreCase); }
        public ISet<string> GetAllowedApplications() { return new HashSet<string>(Apps, StringComparer.OrdinalIgnoreCase); }
        public bool GetCloakActive() { Calls.Add("cloak-state"); return Cloak; }
        public void RegisterApplication(string path) { Calls.Add("app+" + path); Apps.Add(path); }
        public void UnregisterApplication(string path) { Calls.Add("app-" + path); Apps.Remove(path); }
        public void HideDevice(string path)
        {
            Calls.Add("hide:" + path);
            if (FailHide) throw new InvalidOperationException("hide failed");
            Hidden.Add(path);
            if (OnHide != null) OnHide(path);
        }
        public void UnhideDevice(string path)
        {
            Calls.Add("unhide:" + path);
            Hidden.Remove(path);
        }
        public void SetCloakActive(bool active) { Calls.Add(active ? "cloak-on" : "cloak-off"); Cloak = active; }
    }

    private sealed class FakeJournal : IControlledReplacementJournal
    {
        public ControlledReplacementRecoveryRecord Record;
        public int SaveCount;
        public int ClearCount;
        public ControlledReplacementRecoveryRecord Load() { return Record; }
        public void Save(ControlledReplacementRecoveryRecord record)
        {
            SaveCount++;
            Record = new ControlledReplacementRecoveryRecord
            {
                ApplicationPath = record.ApplicationPath,
                InitialCloakActive = record.InitialCloakActive,
                AddedApplication = record.AddedApplication,
                AddedHiddenDevices = new List<string>(record.AddedHiddenDevices ?? new List<string>())
            };
        }
        public void Clear() { ClearCount++; Record = null; }
    }

    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static ControlledReplacementCoordinator Coordinator(FakeBackend backend, Func<bool> router, Func<int> slot, Func<bool> health)
    {
        return new ControlledReplacementCoordinator(backend, router, slot, health, @"C:\Apps\InputStitch.exe");
    }

    public static int Main()
    {
        // Parser coverage for the official HidHide CLI output shapes used by the backend.
        string list = "--dev-hide \"HID\\\\VID_1234&PID_5678\\\\A\"\r\n--dev-hide \"HID\\\\VID_9999\\\\B\"\r\n";
        ISet<string> hiddenParsed = HidHideCliBackend.ParseQuotedCommandList(list, "--dev-hide");
        Check(hiddenParsed.Contains(@"HID\VID_1234&PID_5678\A") && hiddenParsed.Contains(@"HID\VID_9999\B"), "HidHide dev-list command output parses device paths");

        string json = "[{\"friendlyName\":\"Pad\",\"devices\":[{" +
            "\"present\":true,\"gamingDevice\":true,\"symbolicLink\":\"x\",\"vendor\":\"Vendor\",\"product\":\"Pad One\",\"serialNumber\":\"\",\"usage\":\"Gamepad\",\"description\":\"Controller\"," +
            "\"deviceInstancePath\":\"HID\\\\VID_1234&PID_5678\\\\A\",\"xusbDeviceInstancePath\":\"USB\\\\VID_1234&PID_5678\\\\X\",\"baseContainerDeviceInstancePath\":\"USB\\\\VID_1234&PID_5678\\\\BASE\",\"baseContainerClassGuid\":\"{}\",\"baseContainerDeviceCount\":1}]}]";
        IList<GamingDeviceDescriptor> devices = HidHideCliBackend.ParseGamingDevices(json);
        Check(devices.Count == 1 && devices[0].Present && devices[0].GamingDevice, "HidHide dev-gaming JSON parses flags");
        Check(devices[0].Product == "Pad One" && devices[0].DeviceInstancePath == @"HID\VID_1234&PID_5678\A", "HidHide dev-gaming JSON parses controller identity");
        Check(devices[0].XusbDeviceInstancePath == @"USB\VID_1234&PID_5678\X", "HidHide dev-gaming JSON preserves XUSB identity");

        string journalPath = Path.Combine(Path.GetTempPath(), "InputStitch-ReplacementJournal-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            FileControlledReplacementJournal fileJournal = new FileControlledReplacementJournal(journalPath);
            ControlledReplacementRecoveryRecord fileRecord = new ControlledReplacementRecoveryRecord
            {
                ApplicationPath = @"C:\Apps\InputStitch.exe",
                InitialCloakActive = false,
                AddedApplication = true,
                AddedHiddenDevices = new List<string> { "A", "B" }
            };
            fileJournal.Save(fileRecord);
            ControlledReplacementRecoveryRecord loaded = fileJournal.Load();
            Check(loaded != null && loaded.AddedApplication && loaded.AddedHiddenDevices.Count == 2 && loaded.AddedHiddenDevices[1] == "B",
                "file recovery journal survives serialize/validate/replace round trip");
            fileJournal.Clear();
            Check(fileJournal.Load() == null && !File.Exists(journalPath), "file recovery journal clears after safe restore");
        }
        finally { try { if (File.Exists(journalPath)) File.Delete(journalPath); } catch { } }

        FakeBackend crashBackend = new FakeBackend();
        FakeJournal crashJournal = new FakeJournal();
        bool journalExistedBeforeHide = false;
        crashBackend.OnHide = delegate { journalExistedBeforeHide = crashJournal.Record != null; };
        ControlledReplacementCoordinator crashedCoordinator = new ControlledReplacementCoordinator(crashBackend,
            delegate { return true; }, delegate { return 0; }, delegate { return true; }, @"C:\Apps\InputStitch.exe", crashJournal);
        Check(crashedCoordinator.Begin(new string[] { "A" }, 0).State == ControlledReplacementState.Active, "journal-protected replacement can activate");
        Check(journalExistedBeforeHide && crashJournal.SaveCount == 1, "recovery journal is persisted before first device hide mutation");
        Check(crashBackend.Hidden.Contains("A") && crashBackend.Cloak && crashBackend.Apps.Contains(@"C:\Apps\InputStitch.exe"),
            "simulated crash leaves the same HidHide state that requires recovery");
        string recoveryError = ControlledReplacementRecovery.RestorePending(crashBackend, crashJournal);
        Check(recoveryError == "", "next-launch recovery completes without error");
        Check(!crashBackend.Hidden.Contains("A") && !crashBackend.Cloak && !crashBackend.Apps.Contains(@"C:\Apps\InputStitch.exe"),
            "next-launch recovery restores visibility, cloak state and added app whitelist");
        Check(crashJournal.Record == null && crashJournal.ClearCount == 1, "successful recovery clears the crash journal");

        FakeBackend unavailable = new FakeBackend { Available = false };
        using (ControlledReplacementCoordinator c = Coordinator(unavailable, delegate { return true; }, delegate { return 0; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.BackendUnavailable && unavailable.Calls.Count == 0, "missing hiding backend performs no mutation");
        }

        FakeBackend routerOff = new FakeBackend();
        using (ControlledReplacementCoordinator c = Coordinator(routerOff, delegate { return false; }, delegate { return 0; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.RouterNotReady && routerOff.Calls.Count == 0, "router precondition blocks hiding before backend mutation");
        }

        FakeBackend wrongSlot = new FakeBackend();
        using (ControlledReplacementCoordinator c = Coordinator(wrongSlot, delegate { return true; }, delegate { return 2; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.WrongVirtualSlot, "wrong virtual slot blocks controlled replacement");
            Check(wrongSlot.Calls.Count == 0 && wrongSlot.Hidden.Count == 0, "wrong slot never hides the physical controller");
        }

        FakeBackend normal = new FakeBackend();
        normal.Hidden.Add("PREEXISTING");
        using (ControlledReplacementCoordinator c = Coordinator(normal, delegate { return true; }, delegate { return 0; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A", "A", "PREEXISTING" }, 0);
            Check(r.State == ControlledReplacementState.Active && c.Active, "eligible replacement reaches Active");
            Check(normal.Apps.Contains(@"C:\Apps\InputStitch.exe"), "InputStitch is allowed through the device cloak before active state");
            Check(normal.Hidden.Contains("A") && normal.Hidden.Contains("PREEXISTING"), "requested controller is hidden without disturbing preexisting hidden device");
            Check(normal.Cloak, "device cloak is enabled when it was initially off");
            Check(normal.Calls.FindAll(delegate(string x) { return x == "hide:A"; }).Count == 1, "duplicate selected device is hidden only once");

            ControlledReplacementResult stopped = c.Stop();
            Check(stopped.State == ControlledReplacementState.Idle && !c.Active, "Stop returns coordinator to Idle");
            Check(!normal.Hidden.Contains("A") && normal.Hidden.Contains("PREEXISTING"), "Stop unhides only InputStitch-added device entries");
            Check(!normal.Cloak, "Stop restores initially-off cloak state");
            Check(!normal.Apps.Contains(@"C:\Apps\InputStitch.exe"), "Stop removes only InputStitch-added application whitelist entry");
        }

        FakeBackend preexisting = new FakeBackend { Cloak = true };
        preexisting.Hidden.Add("A");
        preexisting.Apps.Add(@"C:\Apps\InputStitch.exe");
        using (ControlledReplacementCoordinator c = Coordinator(preexisting, delegate { return true; }, delegate { return 0; }, delegate { return true; }))
        {
            Check(c.Begin(new string[] { "A" }, 0).State == ControlledReplacementState.Active, "preconfigured HidHide state can be adopted");
            c.Stop();
            Check(preexisting.Cloak && preexisting.Hidden.Contains("A") && preexisting.Apps.Contains(@"C:\Apps\InputStitch.exe"),
                "Stop preserves HidHide state that existed before InputStitch began replacement");
        }

        int dynamicSlot = 0;
        FakeBackend slotLoss = new FakeBackend();
        slotLoss.OnHide = delegate { dynamicSlot = 1; };
        using (ControlledReplacementCoordinator c = Coordinator(slotLoss, delegate { return true; }, delegate { return dynamicSlot; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.Failed, "post-hide target-slot loss fails activation");
            Check(!slotLoss.Hidden.Contains("A") && !slotLoss.Cloak && !slotLoss.Apps.Contains(@"C:\Apps\InputStitch.exe"),
                "slot-loss failure rolls back hide, cloak and application whitelist changes");
        }

        FakeBackend healthLoss = new FakeBackend();
        using (ControlledReplacementCoordinator c = Coordinator(healthLoss, delegate { return true; }, delegate { return 0; }, delegate { return false; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.Failed, "losing source visibility through the cloak fails activation");
            Check(!healthLoss.Hidden.Contains("A") && !healthLoss.Cloak, "source-health failure restores device visibility");
        }

        FakeBackend hideFailure = new FakeBackend { FailHide = true };
        using (ControlledReplacementCoordinator c = Coordinator(hideFailure, delegate { return true; }, delegate { return 0; }, delegate { return true; }))
        {
            ControlledReplacementResult r = c.Begin(new string[] { "A" }, 0);
            Check(r.State == ControlledReplacementState.Failed && !hideFailure.Cloak, "backend hide exception fails closed before cloak remains enabled");
            Check(!hideFailure.Apps.Contains(@"C:\Apps\InputStitch.exe"), "hide exception rolls back newly added application whitelist entry");
        }

        FakeBackend pendingBackend = new FakeBackend();
        FakeJournal pendingJournal = new FakeJournal
        {
            Record = new ControlledReplacementRecoveryRecord
            {
                ApplicationPath = @"C:\Apps\InputStitch.exe",
                AddedApplication = true,
                AddedHiddenDevices = new List<string> { "OLD" }
            }
        };
        using (ControlledReplacementCoordinator c = new ControlledReplacementCoordinator(pendingBackend,
            delegate { return true; }, delegate { return 0; }, delegate { return true; }, @"C:\Apps\InputStitch.exe", pendingJournal))
        {
            ControlledReplacementResult blocked = c.Begin(new string[] { "A" }, 0);
            Check(blocked.State == ControlledReplacementState.Failed && pendingBackend.Calls.Count == 0,
                "pending HidHide recovery journal blocks a new takeover before any backend mutation");
            pendingJournal.Record = null;
        }

        // Cross-stage pipeline: HidHide Begin is unreachable until slot acquisition, target-slot
        // verification and Router/source preparation all succeed.
        GamingDeviceDescriptor selectedDevice = new GamingDeviceDescriptor { Present = true, GamingDevice = true, DeviceInstancePath = "HID-A", XusbDeviceInstancePath = "XUSB-A" };
        int pipelineSlot = 2;
        int prepareCalls = 0;
        int hideBeginCalls = 0;
        ControlledTakeoverPipeline pipeline = new ControlledTakeoverPipeline(
            delegate(IEnumerable<GamingDeviceDescriptor> selected, int target)
            {
                return new SlotAcquisitionResult { State = SlotAcquisitionState.Failed, TargetSlot = target, Message = "slot failed" };
            },
            delegate { return pipelineSlot; },
            delegate { prepareCalls++; return true; },
            delegate(IEnumerable<string> paths, int target) { hideBeginCalls++; return new ControlledReplacementResult { State = ControlledReplacementState.Active }; });
        ControlledReplacementResult pipelineResult = pipeline.Begin(new GamingDeviceDescriptor[] { selectedDevice }, 0);
        Check(pipelineResult.State == ControlledReplacementState.SlotAcquisitionFailed && prepareCalls == 0 && hideBeginCalls == 0,
            "slot acquisition failure makes HidHide Begin unreachable");

        pipelineSlot = 1;
        prepareCalls = 0;
        hideBeginCalls = 0;
        pipeline = new ControlledTakeoverPipeline(
            delegate(IEnumerable<GamingDeviceDescriptor> selected, int target)
            {
                return new SlotAcquisitionResult { State = SlotAcquisitionState.Acquired, TargetSlot = target,
                    ExternalDeviceInstancePaths = new List<string> { "HID-A" } };
            },
            delegate { return pipelineSlot; },
            delegate { prepareCalls++; return true; },
            delegate(IEnumerable<string> paths, int target) { hideBeginCalls++; return new ControlledReplacementResult { State = ControlledReplacementState.Active }; });
        pipelineResult = pipeline.Begin(new GamingDeviceDescriptor[] { selectedDevice }, 0);
        Check(pipelineResult.State == ControlledReplacementState.SlotAcquisitionFailed && prepareCalls == 0 && hideBeginCalls == 0,
            "claimed slot acquisition is independently rejected if virtual slot is not actually zero");

        pipelineSlot = 0;
        prepareCalls = 0;
        hideBeginCalls = 0;
        pipeline = new ControlledTakeoverPipeline(
            delegate(IEnumerable<GamingDeviceDescriptor> selected, int target)
            {
                return new SlotAcquisitionResult { State = SlotAcquisitionState.Acquired, TargetSlot = target,
                    ExternalDeviceInstancePaths = new List<string> { "HID-A" } };
            },
            delegate { return pipelineSlot; },
            delegate { prepareCalls++; return false; },
            delegate(IEnumerable<string> paths, int target) { hideBeginCalls++; return new ControlledReplacementResult { State = ControlledReplacementState.Active }; });
        pipelineResult = pipeline.Begin(new GamingDeviceDescriptor[] { selectedDevice }, 0);
        Check(pipelineResult.State == ControlledReplacementState.SlotAcquisitionFailed && prepareCalls == 1 && hideBeginCalls == 0,
            "Router/source verification failure after slot zero still prevents HidHide Begin");

        pipelineSlot = 0;
        prepareCalls = 0;
        hideBeginCalls = 0;
        List<string> hiddenRequest = new List<string>();
        pipeline = new ControlledTakeoverPipeline(
            delegate(IEnumerable<GamingDeviceDescriptor> selected, int target)
            {
                return new SlotAcquisitionResult { State = SlotAcquisitionState.Acquired, TargetSlot = target,
                    ExternalDeviceInstancePaths = new List<string> { "HID-A", "HID-B" } };
            },
            delegate { return pipelineSlot; },
            delegate { prepareCalls++; return true; },
            delegate(IEnumerable<string> paths, int target)
            {
                hideBeginCalls++;
                hiddenRequest.AddRange(paths);
                return new ControlledReplacementResult { State = ControlledReplacementState.Active };
            });
        pipelineResult = pipeline.Begin(new GamingDeviceDescriptor[] { selectedDevice }, 0);
        Check(pipelineResult.State == ControlledReplacementState.Active && prepareCalls == 1 && hideBeginCalls == 1,
            "HidHide Begin is called exactly once only after every slot/router/source gate passes");
        Check(hiddenRequest.Count == 2 && hiddenRequest.Contains("HID-A") && hiddenRequest.Contains("HID-B"),
            "pipeline forwards only slot-acquisition-resolved external HidHide identities");

        Console.WriteLine("PASS controlled replacement: " + checks + " checks; fake backend/parser only, HidHide was not invoked.");
        return 0;
    }
}
