using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace InputStitch
{
    [Serializable]
    public sealed class SlotAcquisitionRecoveryRecord
    {
        public List<string> TemporarilyDisabledPnpDevices = new List<string>();
    }

    internal interface ISlotAcquisitionJournal
    {
        SlotAcquisitionRecoveryRecord Load();
        void Save(SlotAcquisitionRecoveryRecord record);
        void Clear();
    }

    internal sealed class FileSlotAcquisitionJournal : ISlotAcquisitionJournal
    {
        private readonly AtomicXmlFileStore<SlotAcquisitionRecoveryRecord> store;

        internal FileSlotAcquisitionJournal(string journalPath)
        {
            if (string.IsNullOrWhiteSpace(journalPath)) throw new ArgumentException("A slot-acquisition recovery journal path is required.");
            store = new AtomicXmlFileStore<SlotAcquisitionRecoveryRecord>(Path.GetFullPath(journalPath));
        }

        public SlotAcquisitionRecoveryRecord Load()
        {
            return store.Load();
        }

        public void Save(SlotAcquisitionRecoveryRecord record)
        {
            if (record == null) throw new ArgumentNullException("record");
            store.Save(record);
        }

        public void Clear()
        {
            store.Clear();
        }
    }

    internal static class SlotAcquisitionRecovery
    {
        internal static string RestorePending(IPnpDeviceControl pnp, ISlotAcquisitionJournal journal)
        {
            if (pnp == null || journal == null) return "";
            SlotAcquisitionRecoveryRecord record;
            try { record = journal.Load(); }
            catch (Exception ex) { return "Could not read slot-acquisition recovery journal: " + ex.Message; }
            if (record == null) return "";
            if (!pnp.IsAvailable) return "Windows PnP recovery is unavailable; restart Windows to restore any temporarily disabled controller.";
            if (!pnp.IsElevated) return "Administrator permission is required to restore a controller left temporarily disabled by an interrupted slot-acquisition attempt. Restart Windows or run InputStitch as administrator.";

            List<string> errors = new List<string>();
            if (record.TemporarilyDisabledPnpDevices != null)
            {
                for (int i = record.TemporarilyDisabledPnpDevices.Count - 1; i >= 0; i--)
                {
                    string path = record.TemporarilyDisabledPnpDevices[i];
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    try { pnp.Enable(path); }
                    catch (Exception ex) { errors.Add("enable " + path + ": " + ex.Message); }
                }
            }
            if (errors.Count != 0) return string.Join(" | ", errors.ToArray());
            try { journal.Clear(); }
            catch (Exception ex) { return "PnP devices were restored but the slot-acquisition recovery journal could not be cleared: " + ex.Message; }
            return "";
        }
    }

    internal interface IPnpDeviceControl
    {
        bool IsAvailable { get; }
        bool IsElevated { get; }
        bool IsEnabled(string deviceInstancePath);
        void Disable(string deviceInstancePath);
        void Enable(string deviceInstancePath);
    }

    internal sealed class WindowsPnpDeviceControl : IPnpDeviceControl
    {
        private const uint CR_SUCCESS = 0x00000000;
        private const uint CR_NO_SUCH_DEVNODE = 0x0000000D;
        private const uint CM_DISABLE_UI_NOT_OK = 0x00000004;
        private const uint CM_PROB_DISABLED = 0x00000016;

        public bool IsAvailable { get { return Environment.OSVersion.Platform == PlatformID.Win32NT; } }

        public bool IsElevated
        {
            get
            {
                try
                {
                    WindowsIdentity identity = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch { return false; }
            }
        }

        public bool IsEnabled(string deviceInstancePath)
        {
            uint devInst = Locate(deviceInstancePath);
            uint status;
            uint problem;
            uint result = CM_Get_DevNode_Status(out status, out problem, devInst, 0);
            if (result != CR_SUCCESS) throw ConfigManagerFailure("read status", deviceInstancePath, result);
            return problem != CM_PROB_DISABLED;
        }

        public void Disable(string deviceInstancePath)
        {
            if (!IsElevated) throw new UnauthorizedAccessException("Administrator permission is required to temporarily re-enumerate XInput devices.");
            uint devInst = Locate(deviceInstancePath);
            // Deliberately omit CM_DISABLE_PERSIST. If the process/machine crashes before recovery,
            // Windows restores the device after reboot rather than persisting a disabled controller.
            uint result = CM_Disable_DevNode(devInst, CM_DISABLE_UI_NOT_OK);
            if (result != CR_SUCCESS) throw ConfigManagerFailure("disable", deviceInstancePath, result);
        }

        public void Enable(string deviceInstancePath)
        {
            if (!IsElevated) throw new UnauthorizedAccessException("Administrator permission is required to restore a temporarily disabled XInput device.");
            uint devInst;
            uint locate = CM_Locate_DevNode(out devInst, deviceInstancePath, 0);
            if (locate == CR_NO_SUCH_DEVNODE) return;
            if (locate != CR_SUCCESS) throw ConfigManagerFailure("locate for enable", deviceInstancePath, locate);
            uint result = CM_Enable_DevNode(devInst, 0);
            if (result != CR_SUCCESS) throw ConfigManagerFailure("enable", deviceInstancePath, result);
        }

        private static uint Locate(string deviceInstancePath)
        {
            if (string.IsNullOrWhiteSpace(deviceInstancePath)) throw new ArgumentException("A PnP device instance path is required.");
            uint devInst;
            uint result = CM_Locate_DevNode(out devInst, deviceInstancePath, 0);
            if (result != CR_SUCCESS) throw ConfigManagerFailure("locate", deviceInstancePath, result);
            return devInst;
        }

        private static Exception ConfigManagerFailure(string action, string path, uint code)
        {
            return new InvalidOperationException("Windows PnP could not " + action + " device " + path + " (CONFIGRET=0x" + code.ToString("X8") + ").");
        }

        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Locate_DevNodeW")]
        private static extern uint CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, uint ulFlags);

        [DllImport("CfgMgr32.dll")]
        private static extern uint CM_Disable_DevNode(uint dnDevInst, uint ulFlags);

        [DllImport("CfgMgr32.dll")]
        private static extern uint CM_Enable_DevNode(uint dnDevInst, uint ulFlags);

        [DllImport("CfgMgr32.dll")]
        private static extern uint CM_Get_DevNode_Status(out uint pulStatus, out uint pulProblemNumber, uint dnDevInst, uint ulFlags);
    }

    internal enum SlotAcquisitionState
    {
        AlreadyTarget,
        Acquired,
        ElevationRequired,
        NoExternalSources,
        UnsupportedSource,
        Failed
    }

    internal sealed class SlotAcquisitionResult
    {
        public SlotAcquisitionState State;
        public string Message = "";
        public int TargetSlot = -1;
        public List<string> ExternalDeviceInstancePaths = new List<string>();
        public bool Success { get { return State == SlotAcquisitionState.AlreadyTarget || State == SlotAcquisitionState.Acquired; } }
    }

    internal sealed class XInputSlotAcquisitionCoordinator
    {
        private readonly IDeviceHidingBackend deviceDiscovery;
        private readonly IPnpDeviceControl pnp;
        private readonly Action disconnectVirtual;
        private readonly Action connectVirtual;
        private readonly Func<int> virtualSlot;
        private readonly Func<int[]> externalSlots;
        private readonly ISlotAcquisitionJournal journal;
        private readonly Action<int> delay;

        internal XInputSlotAcquisitionCoordinator(IDeviceHidingBackend discoveryBackend, IPnpDeviceControl pnpDeviceControl,
            Action disconnectVirtualController, Action connectVirtualController, Func<int> ownVirtualSlot,
            Func<int[]> visibleExternalSlots, ISlotAcquisitionJournal recoveryJournal)
            : this(discoveryBackend, pnpDeviceControl, disconnectVirtualController, connectVirtualController,
                ownVirtualSlot, visibleExternalSlots, recoveryJournal, Thread.Sleep) { }

        internal XInputSlotAcquisitionCoordinator(IDeviceHidingBackend discoveryBackend, IPnpDeviceControl pnpDeviceControl,
            Action disconnectVirtualController, Action connectVirtualController, Func<int> ownVirtualSlot,
            Func<int[]> visibleExternalSlots, ISlotAcquisitionJournal recoveryJournal, Action<int> sleep)
        {
            if (discoveryBackend == null) throw new ArgumentNullException("discoveryBackend");
            if (pnpDeviceControl == null) throw new ArgumentNullException("pnpDeviceControl");
            if (disconnectVirtualController == null || connectVirtualController == null || ownVirtualSlot == null || visibleExternalSlots == null)
                throw new ArgumentNullException("slot acquisition callbacks");
            deviceDiscovery = discoveryBackend;
            pnp = pnpDeviceControl;
            disconnectVirtual = disconnectVirtualController;
            connectVirtual = connectVirtualController;
            virtualSlot = ownVirtualSlot;
            externalSlots = visibleExternalSlots;
            journal = recoveryJournal;
            delay = sleep ?? Thread.Sleep;
        }

        public SlotAcquisitionResult Acquire(IEnumerable<GamingDeviceDescriptor> selectedDevices, int targetSlot)
        {
            if (!pnp.IsAvailable) return Result(SlotAcquisitionState.Failed, targetSlot, "Windows PnP device control is unavailable.");
            if (journal != null)
            {
                try
                {
                    if (journal.Load() != null)
                        return Result(SlotAcquisitionState.Failed, targetSlot, "A previous slot-acquisition recovery record is still pending. Restore it or restart Windows before starting a new takeover.");
                }
                catch (Exception ex)
                {
                    return Result(SlotAcquisitionState.Failed, targetSlot, "Could not verify slot-acquisition recovery state: " + ex.Message);
                }
            }

            List<string> requestedHidPaths = SelectedDevicePaths(selectedDevices);
            if (requestedHidPaths.Count == 0) return Result(SlotAcquisitionState.NoExternalSources, targetSlot, "No controller was selected for slot acquisition.");

            int initialVirtualSlot = virtualSlot();
            int initialExternalCount = SafeExternalSlots().Length;
            if (initialExternalCount == 0) return Result(SlotAcquisitionState.NoExternalSources, targetSlot, "No external XInput controller is currently visible.");
            if (initialVirtualSlot != targetSlot && !pnp.IsElevated)
                return Result(SlotAcquisitionState.ElevationRequired, targetSlot, "Administrator permission is required to safely reorder XInput devices.");

            List<string> disabled = new List<string>();
            bool virtualDisconnected = false;
            bool virtualReconnectedAtTarget = false;
            List<string> xusbPaths = new List<string>();
            try
            {
                // Always remove InputStitch's own virtual target from discovery once, even if it
                // already owns slot 0. This is the authoritative self-identity filter: a selected
                // HidHide entry that disappears with our virtual controller is not an external source
                // and must never be hidden from the target game later.
                disconnectVirtual();
                virtualDisconnected = true;
                WaitUntil(delegate { return virtualSlot() < 0; }, 1200);

                IList<GamingDeviceDescriptor> fresh = deviceDiscovery.EnumerateGamingDevices();
                List<string> selectedXusbPaths = ResolveSelectedExternalXusbPaths(requestedHidPaths, fresh);
                if (selectedXusbPaths.Count == 0)
                    throw new SlotAcquisitionException(SlotAcquisitionState.NoExternalSources, "No selected external XUSB controller remained after the InputStitch virtual controller was removed from discovery.");

                // Slot acquisition is a system-ordering operation, not a hiding operation. To make
                // InputStitch deterministically first, every present external XUSB controller must
                // temporarily leave XInput enumeration, even if only a subset is selected for final
                // HidHide suppression. Unselected controllers are restored immediately and are never
                // added to HidHide by this transaction.
                xusbPaths = ResolveAllExternalXusbPaths(fresh);
                if (xusbPaths.Count == 0)
                    throw new SlotAcquisitionException(SlotAcquisitionState.NoExternalSources, "No external XUSB controller is available for slot acquisition.");
                if (xusbPaths.Count > 3)
                    throw new SlotAcquisitionException(SlotAcquisitionState.UnsupportedSource, "More than three external XInput/XUSB controllers are present. With InputStitch occupying slot 0, the current XInput backend cannot keep all of them simultaneously routable.");

                // If we already owned slot 0, first try the least invasive path: reconnect our
                // virtual controller without cycling physical XUSB devnodes. This still gives us a
                // safe own-device identity filter. If Windows does not return us to slot 0, fall
                // through to the same full re-enumeration used for every other starting slot.
                if (initialVirtualSlot == targetSlot)
                {
                    connectVirtual();
                    virtualDisconnected = false;
                    if (WaitUntil(delegate { return virtualSlot() == targetSlot; }, 1600))
                    {
                        virtualReconnectedAtTarget = true;
                        IList<GamingDeviceDescriptor> existingSlotRestoredDevices = deviceDiscovery.EnumerateGamingDevices();
                        ResolveCurrentExternalHidPaths(xusbPaths, existingSlotRestoredDevices); // verify every external XUSB identity
                        List<string> externalHidPaths = ResolveCurrentExternalHidPaths(selectedXusbPaths, existingSlotRestoredDevices);
                        return Result(SlotAcquisitionState.AlreadyTarget, targetSlot,
                            "Virtual controller returned to the target XInput slot and external controller identity was verified.", externalHidPaths);
                    }
                    disconnectVirtual();
                    virtualDisconnected = true;
                    WaitUntil(delegate { return virtualSlot() < 0; }, 1200);
                }

                if (!pnp.IsElevated)
                    throw new SlotAcquisitionException(SlotAcquisitionState.ElevationRequired, "Administrator permission is required to safely reorder XInput devices.");

                // Never enable a device that was disabled before InputStitch began. The recovery
                // journal contains only devices proven enabled immediately before our mutation.
                foreach (string path in xusbPaths)
                {
                    bool enabled;
                    try { enabled = pnp.IsEnabled(path); }
                    catch (Exception ex) { throw new InvalidOperationException("Could not verify the selected XUSB device before slot acquisition: " + path + " — " + ex.Message); }
                    if (!enabled)
                        throw new InvalidOperationException("The selected XUSB device was already disabled before slot acquisition and will not be modified: " + path);
                }

                SavePnpRecovery(xusbPaths);
                foreach (string path in xusbPaths)
                {
                    pnp.Disable(path);
                    disabled.Add(path);
                }
                foreach (string path in disabled)
                {
                    if (!WaitUntil(delegate { return IsConfirmedDisabled(path); }, 1600))
                        throw new InvalidOperationException("Windows did not confirm that the selected XUSB device was temporarily disabled: " + path);
                }

                connectVirtual();
                virtualDisconnected = false;
                if (!WaitUntil(delegate { return virtualSlot() == targetSlot; }, 2200))
                    throw new InvalidOperationException("Windows did not assign the InputStitch virtual Xbox controller to XInput slot " + targetSlot.ToString() + ". The slot may be reserved by another process/device.");
                virtualReconnectedAtTarget = true;

                // Bring the originals back only after the virtual controller has demonstrably
                // acquired slot 0. They may then occupy later slots and remain readable by Router.
                EnableAll(disabled);
                disabled.Clear();

                bool restored = WaitUntil(delegate
                {
                    return virtualSlot() == targetSlot && SafeExternalSlots().Length >= initialExternalCount;
                }, 3500);
                if (!restored)
                    throw new InvalidOperationException("Original controller(s) did not all return while the InputStitch virtual controller remained in the target slot.");

                IList<GamingDeviceDescriptor> restoredDevices = deviceDiscovery.EnumerateGamingDevices();
                ResolveCurrentExternalHidPaths(xusbPaths, restoredDevices); // hard identity check for every cycled XUSB device
                List<string> resolvedHidPaths = ResolveCurrentExternalHidPaths(selectedXusbPaths, restoredDevices);
                ClearJournalAfterPnpRestore();
                return Result(SlotAcquisitionState.Acquired, targetSlot,
                    "InputStitch acquired XInput slot " + targetSlot.ToString() + " and all previously visible external XInput sources returned.", resolvedHidPaths);
            }
            catch (SlotAcquisitionException ex)
            {
                string rollback = Rollback(disabled, virtualDisconnected, virtualReconnectedAtTarget);
                return Result(ex.State, targetSlot, AppendRollback(ex.Message, rollback));
            }
            catch (Exception ex)
            {
                string rollback = Rollback(disabled, virtualDisconnected, virtualReconnectedAtTarget);
                return Result(SlotAcquisitionState.Failed, targetSlot, AppendRollback(ex.Message, rollback));
            }
        }

        private string Rollback(List<string> disabled, bool virtualDisconnected, bool virtualReconnectedAtTarget)
        {
            List<string> errors = new List<string>();
            try
            {
                if (virtualReconnectedAtTarget || !virtualDisconnected) disconnectVirtual();
            }
            catch (Exception ex) { errors.Add("disconnect virtual: " + ex.Message); }

            try { EnableAll(disabled); }
            catch (Exception ex) { errors.Add("restore PnP devices: " + ex.Message); }

            try
            {
                // Restore ordinary pre-takeover usability. Exact old slot numbers are not promised;
                // the hard rollback property is that originals are enabled/unhidden and the virtual
                // controller is reconnected if it was connected before acquisition.
                connectVirtual();
            }
            catch (Exception ex) { errors.Add("reconnect virtual: " + ex.Message); }

            if (errors.Count == 0)
            {
                try { ClearJournalAfterPnpRestore(); }
                catch (Exception ex) { errors.Add("clear PnP recovery journal: " + ex.Message); }
            }
            return errors.Count == 0 ? "" : string.Join(" | ", errors.ToArray());
        }

        private void EnableAll(List<string> paths)
        {
            List<Exception> failures = new List<Exception>();
            for (int i = paths.Count - 1; i >= 0; i--)
            {
                try { pnp.Enable(paths[i]); }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (failures.Count != 0) throw new InvalidOperationException(failures[0].Message);
        }

        private void SavePnpRecovery(List<string> paths)
        {
            if (journal == null || paths == null || paths.Count == 0) return;
            SlotAcquisitionRecoveryRecord record = new SlotAcquisitionRecoveryRecord();
            record.TemporarilyDisabledPnpDevices = new List<string>(paths);
            journal.Save(record);
        }

        private void ClearJournalAfterPnpRestore()
        {
            if (journal != null) journal.Clear();
        }

        private bool IsConfirmedDisabled(string path)
        {
            try { return !pnp.IsEnabled(path); }
            catch { return false; }
        }

        private int[] SafeExternalSlots()
        {
            try { return externalSlots() ?? new int[0]; }
            catch { return new int[0]; }
        }

        private bool WaitUntil(Func<bool> predicate, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed <= timeoutMs)
            {
                if (predicate()) return true;
                int slice = Math.Min(50, timeoutMs - elapsed);
                if (slice <= 0) break;
                delay(slice);
                elapsed += slice;
            }
            return predicate();
        }

        private static List<string> SelectedDevicePaths(IEnumerable<GamingDeviceDescriptor> selected)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selected == null) return result;
            foreach (GamingDeviceDescriptor device in selected)
            {
                string path = device == null ? "" : (device.DeviceInstancePath ?? "").Trim();
                if (path.Length != 0 && seen.Add(path)) result.Add(path);
            }
            return result;
        }

        private static List<string> ResolveSelectedExternalXusbPaths(List<string> requestedHidPaths, IList<GamingDeviceDescriptor> fresh)
        {
            Dictionary<string, GamingDeviceDescriptor> byHid = new Dictionary<string, GamingDeviceDescriptor>(StringComparer.OrdinalIgnoreCase);
            if (fresh != null)
            {
                foreach (GamingDeviceDescriptor device in fresh)
                {
                    if (device == null || !device.Present || !device.GamingDevice || string.IsNullOrWhiteSpace(device.DeviceInstancePath)) continue;
                    byHid[device.DeviceInstancePath] = device;
                }
            }

            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string requested in requestedHidPaths)
            {
                GamingDeviceDescriptor device;
                if (!byHid.TryGetValue(requested, out device))
                {
                    // Entries that disappear exactly with InputStitch's own virtual controller are
                    // intentionally ignored. This makes Select All safe without guessing by VID/PID.
                    continue;
                }
                string xusb = (device.XusbDeviceInstancePath ?? "").Trim();
                // The current Router backend is XInput-only. HID-only gaming devices are not routed
                // and therefore cannot be part of this controlled-XInput takeover transaction.
                if (xusb.Length == 0) continue;
                if (seen.Add(xusb)) result.Add(xusb);
            }
            return result;
        }

        private static List<string> ResolveAllExternalXusbPaths(IList<GamingDeviceDescriptor> devices)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (devices == null) return result;
            foreach (GamingDeviceDescriptor device in devices)
            {
                if (device == null || !device.Present || !device.GamingDevice) continue;
                string xusb = (device.XusbDeviceInstancePath ?? "").Trim();
                if (xusb.Length != 0 && seen.Add(xusb)) result.Add(xusb);
            }
            return result;
        }

        private static List<string> ResolveCurrentExternalHidPaths(List<string> xusbPaths, IList<GamingDeviceDescriptor> devices)
        {
            HashSet<string> wanted = new HashSet<string>(xusbPaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            HashSet<string> resolvedXusb = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> result = new List<string>();
            if (devices != null)
            {
                foreach (GamingDeviceDescriptor device in devices)
                {
                    if (device == null || !device.Present || !device.GamingDevice) continue;
                    string xusb = (device.XusbDeviceInstancePath ?? "").Trim();
                    string hid = (device.DeviceInstancePath ?? "").Trim();
                    if (xusb.Length == 0 || !wanted.Contains(xusb)) continue;
                    resolvedXusb.Add(xusb);
                    if (hid.Length == 0 || !seen.Add(hid)) continue;
                    result.Add(hid);
                }
            }
            if (wanted.Count == 0 || resolvedXusb.Count != wanted.Count)
                throw new InvalidOperationException("Not every external XUSB controller identity returned after re-enumeration.");
            if (result.Count == 0)
                throw new InvalidOperationException("The external controller returned to XInput but no current HidHide device identity could be resolved after re-enumeration.");
            return result;
        }

        private static string AppendRollback(string message, string rollback)
        {
            if (string.IsNullOrWhiteSpace(rollback)) return message ?? "";
            return (message ?? "") + " Rollback warning: " + rollback;
        }

        private static SlotAcquisitionResult Result(SlotAcquisitionState state, int targetSlot, string message)
        {
            return Result(state, targetSlot, message, null);
        }

        private static SlotAcquisitionResult Result(SlotAcquisitionState state, int targetSlot, string message, List<string> externalDevicePaths)
        {
            return new SlotAcquisitionResult
            {
                State = state,
                TargetSlot = targetSlot,
                Message = message ?? "",
                ExternalDeviceInstancePaths = externalDevicePaths == null ? new List<string>() : new List<string>(externalDevicePaths)
            };
        }

        private sealed class SlotAcquisitionException : Exception
        {
            public readonly SlotAcquisitionState State;
            public SlotAcquisitionException(SlotAcquisitionState state, string message) : base(message) { State = state; }
        }
    }
}
