using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using InputStitch;

internal static class RouterSourcePolicyTests
{
    private static int checks;

    private sealed class FakeDiscovery : IGamingDeviceDiscovery
    {
        internal readonly List<GamingDeviceDescriptor> Devices = new List<GamingDeviceDescriptor>();
        public bool IsAvailable { get { return true; } }
        public string ProviderName { get { return "fake"; } }
        public IList<GamingDeviceDescriptor> EnumerateGamingDevices() { return new List<GamingDeviceDescriptor>(Devices); }
    }

    private sealed class FakeBackend : IOutputBackend
    {
        public void SendDown(InputSpec input) { }
        public void SendUp(InputSpec input) { }
        public void NeutralizeGamepad() { }
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static XInputPadState Pad(bool connected, ushort buttons)
    {
        return new XInputPadState { Connected = connected, Buttons = buttons };
    }

    private static GamingDeviceDescriptor Device(string name)
    {
        return new GamingDeviceDescriptor
        {
            Present = true,
            GamingDevice = true,
            Vendor = "Vendor",
            Product = name,
            DeviceInstancePath = @"HID\VID_1234&PID_5678\A",
            XusbDeviceInstancePath = @"USB\VID_1234&PID_5678\A",
            ContainerId = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
            NativeXusbMetadata = true
        };
    }

    [STAThread]
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-RouterPolicyTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            ConfigPolicyDefaults();
            RuntimeSelection(root);
            DialogRefreshPreservesEdits();
            Console.WriteLine("PASS router source policy: " + checks + " checks; fake XInput/device metadata/output only.");
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

    private static void DialogRefreshPreservesEdits()
    {
        const string key = "dev:v1:dialog-pad";
        DeviceInventorySnapshot snapshot = new DeviceInventorySnapshot();
        snapshot.Items.Add(new DeviceInventoryItem
        {
            Role = DeviceInventoryRole.PersistentGamingDevice,
            DeviceKey = key,
            DisplayName = "Dialog Pad",
            Present = true,
            Persistent = true,
            XInputSlot = 1
        });
        snapshot.Items.Add(new DeviceInventoryItem
        {
            Role = DeviceInventoryRole.UnresolvedXInputSource,
            DisplayName = "XInput source 2",
            Present = true,
            XInputSlot = 2
        });
        snapshot.Items.Add(new DeviceInventoryItem
        {
            Role = DeviceInventoryRole.DiscoveredVirtualBusDevice,
            DeviceKey = "dev:v1:virtual-other",
            DisplayName = "Other virtual pad",
            Present = true,
            Persistent = true,
            VirtualBus = true
        });

        RouterSourcePolicyConfig policy = new RouterSourcePolicyConfig
        {
            Mode = RouterSourceModes.SelectedDevices,
            SelectedDeviceKeys = new List<string> { key }
        };
        using (RouterSourceDialog dialog = new RouterSourceDialog(policy, delegate { return snapshot; }))
        {
            MethodInfo refresh = typeof(RouterSourceDialog).GetMethod("RefreshInventory", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo devicesField = typeof(RouterSourceDialog).GetField("devices", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(refresh != null && devicesField != null, "Router source dialog exposes expected internal controls to the smoke test");
            refresh.Invoke(dialog, null);
            CheckedListBox list = (CheckedListBox)devicesField.GetValue(dialog);
            Check(list.Items.Count == 1 && list.GetItemChecked(0),
                "Router source dialog lists the stable physical device but excludes unresolved and virtual-bus rows");
            list.SetItemChecked(0, false);
            refresh.Invoke(dialog, null);
            Check(list.Items.Count == 1 && !list.GetItemChecked(0),
                "manual uncheck survives Refresh devices instead of being restored from the original policy");
        }
    }

    private static void ConfigPolicyDefaults()
    {
        MacroConfig config = new MacroConfig();
        Check(config.GamepadRouterSourcePolicy != null &&
            config.GamepadRouterSourcePolicy.Mode == RouterSourceModes.AllVisible,
            "new/old-compatible config defaults Router to all-visible sources");

        RouterSourcePolicyConfig messy = new RouterSourcePolicyConfig
        {
            Mode = "unknown-mode",
            SelectedDeviceKeys = new List<string> { " dev:a ", "DEV:A", "", "dev:b" }
        };
        RouterSourcePolicyConfig.Normalize(messy);
        Check(messy.Mode == RouterSourceModes.AllVisible, "unknown source mode normalizes to backward-compatible all-visible");
        Check(messy.SelectedDeviceKeys.Count == 2 && messy.SelectedDeviceKeys[0] == "dev:a" && messy.SelectedDeviceKeys[1] == "dev:b",
            "selected DeviceKeys are trimmed and de-duplicated case-insensitively");

        config.GamepadRouterSourcePolicy = new RouterSourcePolicyConfig
        {
            Mode = RouterSourceModes.SelectedDevices,
            SelectedDeviceKeys = new List<string> { "dev:v1:one", "dev:v1:two" }
        };
        MacroConfig cloned = ConfigPackageSerializer.CloneConfig(config);
        Check(cloned.GamepadRouterSourcePolicy != null && cloned.GamepadRouterSourcePolicy.Mode == RouterSourceModes.SelectedDevices &&
            cloned.GamepadRouterSourcePolicy.SelectedDeviceKeys.Count == 2,
            "Router source policy survives the real XML config clone/round-trip path");

        cloned.GamepadRouterSourcePolicy = null;
        ConfigPackageSerializer.NormalizeConfig(cloned);
        Check(cloned.GamepadRouterSourcePolicy != null && cloned.GamepadRouterSourcePolicy.Mode == RouterSourceModes.AllVisible,
            "missing legacy Router source policy migrates to all-visible without changing old behavior");
    }

    private static void RuntimeSelection(string root)
    {
        XInputPadState[] pads = new XInputPadState[4];
        int own = 0;
        pads[1] = Pad(true, 0x1000);
        XInputInputService input = new XInputInputService(delegate { return own; }, delegate(int index) { return pads[index]; });
        input.Poll();

        FakeDiscovery discovery = new FakeDiscovery();
        discovery.Devices.Add(Device("Pad One"));
        DeviceIdentityService identities = new DeviceIdentityService(
            Path.Combine(root, "devices.xml"), discovery,
            delegate { return input.ConnectedUserIndices(); },
            delegate { return own; },
            delegate { return true; },
            delegate { return VirtualGamepadTypes.Xbox360; },
            delegate { return DateTime.UtcNow; },
            delegate { return input.TopologyVersion; });
        DeviceInventorySnapshot snapshot = identities.Refresh();
        DeviceInventoryItem known = snapshot.Items.FirstOrDefault(delegate(DeviceInventoryItem item)
        {
            return item != null && item.Role == DeviceInventoryRole.PersistentGamingDevice;
        });
        Check(known != null && known.XInputSlot == 1 && !string.IsNullOrWhiteSpace(known.DeviceKey),
            "single-device topology produces a proven transient DeviceKey↔XInput binding");

        RouterSourcePolicyConfig selected = new RouterSourcePolicyConfig
        {
            Mode = RouterSourceModes.SelectedDevices,
            SelectedDeviceKeys = new List<string> { known.DeviceKey }
        };
        RouterSourcePolicyEvaluator evaluator = new RouterSourcePolicyEvaluator(identities, selected);
        RouterSourceDecision allow = evaluator.Evaluate(1, input.TopologyVersion);
        Check(allow.Route && allow.DeviceKey == known.DeviceKey && allow.Reason == "selected-device",
            "selected DeviceKey allows the proven runtime source");

        RouterSourcePolicyConfig wrong = new RouterSourcePolicyConfig
        {
            Mode = RouterSourceModes.SelectedDevices,
            SelectedDeviceKeys = new List<string> { "dev:v1:not-this-device" }
        };
        evaluator.Update(wrong);
        RouterSourceDecision blockKnown = evaluator.Evaluate(1, input.TopologyVersion);
        Check(!blockKnown.Route && blockKnown.Reason == "device-not-selected",
            "known but unselected DeviceKey is blocked");
        evaluator.Update(selected);

        OutputOwnershipManager ownership = new OutputOwnershipManager(new FakeBackend());
        GamepadRouterService router = new GamepadRouterService(ownership, evaluator);
        router.Configure(true);
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 1 && router.PolicyBlockedCount == 0,
            "Router selected-device mode routes the selected proven source");
        Check(ownership.Snapshot().Sources.Any(delegate(OutputSourceSnapshot source) { return source.SourceId == "router:xinput:1"; }),
            "Router keeps existing source ownership ID while policy is DeviceKey-based");

        evaluator.Update(wrong);
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 0 && router.PolicyBlockedCount == 1 && ownership.Snapshot().Merged.Count == 0,
            "changing policy to exclude a currently routed DeviceKey releases that source immediately on the next Router tick");
        evaluator.Update(selected);
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 1,
            "re-selecting the same still-proven DeviceKey restores routing without changing source identity");

        // A second controller changes topology. Before background identity refresh, the old same-slot
        // binding must immediately become invalid so a different device can never inherit permission.
        pads[2] = Pad(true, 0x2000);
        input.Poll();
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 0 && router.PolicyBlockedCount == 2 && router.PolicyUnresolvedCount == 2,
            "topology change fails closed before identity refresh and clears stale routed ownership");
        Check(ownership.Snapshot().Merged.Count == 0,
            "stale selected-device output is released when topology generation changes");

        identities.Refresh();
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 0 && router.PolicyUnresolvedCount == 2,
            "multi-controller topology remains unresolved after refresh rather than guessing slot order");

        RouterSourcePolicyConfig all = new RouterSourcePolicyConfig { Mode = RouterSourceModes.AllVisible };
        evaluator.Update(all);
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 2 && router.PolicyBlockedCount == 0,
            "all-visible policy preserves legacy routing even when stable per-device correlation is unavailable");
        Check(evaluator.IsAllVisible && evaluator.Summary == "all-visible", "all-visible evaluator exposes explicit diagnostic state");

        // Return to a one-controller layout. Selected mode must remain closed until a fresh identity
        // snapshot is taken, then recover the same DeviceKey even though the runtime topology changed.
        evaluator.Update(selected);
        pads[2] = new XInputPadState();
        input.Poll();
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 0 && router.PolicyUnresolvedCount == 1,
            "selected mode fails closed after topology contracts back to one source until identity refresh");
        identities.Refresh();
        router.Tick(input, own);
        Check(router.RoutedControllerCount == 1 && router.PolicyBlockedCount == 0,
            "selected DeviceKey recovers after fresh one-to-one correlation on the new topology generation");

        router.Dispose();
        input.Dispose();
    }
}
