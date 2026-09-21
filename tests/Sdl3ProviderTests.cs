using System;
using System.Collections.Generic;
using System.IO;
using InputStitch;

internal static class Sdl3ProviderTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private sealed class FakeSdl3Api : ISdl3Api
    {
        public bool InitializeResult = true;
        public int InitializeCount;
        public int ShutdownCount;
        public int UpdateCount;
        public readonly Dictionary<int, Sdl3DeviceDescriptor> Devices = new Dictionary<int, Sdl3DeviceDescriptor>();
        public readonly Dictionary<int, Sdl3DeviceSnapshot> Snapshots = new Dictionary<int, Sdl3DeviceSnapshot>();
        public readonly List<int> Opened = new List<int>();
        public readonly List<int> Closed = new List<int>();

        public bool Initialize() { InitializeCount++; return InitializeResult; }
        public void Shutdown() { ShutdownCount++; }
        public void Update() { UpdateCount++; }
        public int[] ListDeviceIds() { return new List<int>(Devices.Keys).ToArray(); }
        public Sdl3DeviceDescriptor Describe(int deviceId)
        {
            Sdl3DeviceDescriptor descriptor;
            return Devices.TryGetValue(deviceId, out descriptor) ? descriptor : null;
        }
        public bool Open(int deviceId) { Opened.Add(deviceId); return true; }
        public void Close(int deviceId) { Closed.Add(deviceId); }
        public Sdl3DeviceSnapshot GetSnapshot(int deviceId)
        {
            Sdl3DeviceSnapshot snapshot;
            return Snapshots.TryGetValue(deviceId, out snapshot) ? snapshot : new Sdl3DeviceSnapshot();
        }
    }

    private static Sdl3DeviceDescriptor Device(string name, ushort vendorId, ushort productId)
    {
        return new Sdl3DeviceDescriptor { Name = name, VendorId = vendorId, ProductId = productId };
    }

    private static Sdl3DeviceSnapshot Snapshot(bool[] buttons, short[] axes)
    {
        return new Sdl3DeviceSnapshot { Buttons = buttons, Axes = axes };
    }

    private static bool[] Buttons(params int[] pressed)
    {
        bool[] buttons = new bool[Sdl3GamepadButtons.Count];
        if (pressed != null)
            foreach (int index in pressed)
                if (index >= 0 && index < buttons.Length) buttons[index] = true;
        return buttons;
    }

    private static short[] Axes(short lx, short ly, short rx, short ry, short lt, short rt)
    {
        short[] axes = new short[Sdl3GamepadAxes.Count];
        axes[Sdl3GamepadAxes.LeftX] = lx;
        axes[Sdl3GamepadAxes.LeftY] = ly;
        axes[Sdl3GamepadAxes.RightX] = rx;
        axes[Sdl3GamepadAxes.RightY] = ry;
        axes[Sdl3GamepadAxes.LeftTrigger] = lt;
        axes[Sdl3GamepadAxes.RightTrigger] = rt;
        return axes;
    }

    public static int Main()
    {
        // 1. An unavailable SDL3 runtime leaves the provider inert instead of throwing.
        FakeSdl3Api missing = new FakeSdl3Api();
        missing.InitializeResult = false;
        Sdl3InputProvider unavailable = new Sdl3InputProvider(missing);
        unavailable.Poll();
        Check(!unavailable.Available && unavailable.ConnectedDeviceIndices().Length == 0,
            "an unavailable SDL3 runtime leaves the provider inert");
        Check(missing.InitializeCount == 1, "initialization is attempted exactly once");
        unavailable.Dispose();
        Check(missing.ShutdownCount == 0, "an unavailable runtime is not shut down");

        // 2. Enumeration, synthetic index assignment and state mapping.
        FakeSdl3Api api = new FakeSdl3Api();
        api.Devices[11] = Device("DualSense", 0x054C, 0x0CE6);
        api.Devices[12] = Device("8BitDo Pro 2", 0x2DC8, 0x6101);
        api.Snapshots[11] = Snapshot(Buttons(Sdl3GamepadButtons.South, Sdl3GamepadButtons.DPadRight),
            Axes(16000, -8000, -16000, 8000, 16384, 32767));
        api.Snapshots[12] = Snapshot(Buttons(Sdl3GamepadButtons.North), Axes(0, 0, 0, 0, 0, 0));
        Sdl3InputProvider provider = new Sdl3InputProvider(api);
        List<string> edges = new List<string>();
        provider.InputDown = delegate(InputEventInfo e) { edges.Add("D:" + e.DeviceIndex.ToString() + ":" + e.Input.GamepadControl.ToString()); };
        provider.InputUp = delegate(InputEventInfo e) { edges.Add("U:" + e.DeviceIndex.ToString() + ":" + e.Input.GamepadControl.ToString()); };
        provider.Poll();
        Check(edges.Count == 0, "the first observation of a device is a baseline without synthetic edges");
        int[] indices = provider.ConnectedDeviceIndices();
        Check(provider.Available && indices.Length == 2 && indices[0] == Sdl3InputProvider.FirstSyntheticIndex && indices[1] == Sdl3InputProvider.FirstSyntheticIndex + 1,
            "SDL3 devices are published at synthetic indices starting above the XInput slots");
        Check(api.Opened.Count == 2, "discovered devices are opened once");
        XInputPadState first;
        Check(provider.TryGetState(indices[0], out first) && first.Connected, "a mapped SDL3 device reports a connected pad state");
        Check((first.Buttons & 0x1000) != 0 && (first.Buttons & 0x0008) != 0,
            "face and D-pad buttons map onto the shared XInput button mask");
        Check(first.ThumbLX == 16000 && first.ThumbRX == -16000, "stick X axes pass through unchanged");
        Check(first.ThumbLY == 8000 && first.ThumbRY == -8000,
            "stick Y axes are inverted because SDL reports positive-down and the app positive-up");
        Check(first.LeftTrigger == Sdl3InputProvider.ScaleTrigger(16384) && first.RightTrigger == 255,
            "SDL triggers scale from 0..32767 onto the app's 0..255 trigger bytes");
        Check(provider.TopologyVersion > 0, "device discovery advances the provider topology version");
        Check(provider.IsTriggerSatisfied(new TriggerSpec { Kind = InputKind.Gamepad, GamepadUserIndex = -1, GamepadControl = GamepadControl.South }),
            "provider trigger queries see SDL3 button state");
        Check(!provider.IsTriggerSatisfied(new TriggerSpec { Kind = InputKind.Gamepad, GamepadUserIndex = -1, GamepadControl = GamepadControl.LeftShoulder }),
            "provider trigger queries do not report unpressed controls");

        // 3. Index stability and button edges.
        edges.Clear();
        provider.Poll();
        Check(edges.Count == 0, "an unchanged SDL3 frame emits no edges");
        int[] again = provider.ConnectedDeviceIndices();
        Check(again.Length == 2 && again[0] == indices[0] && again[1] == indices[1], "device indices stay stable across polls");
        api.Snapshots[11] = Snapshot(Buttons(Sdl3GamepadButtons.South, Sdl3GamepadButtons.DPadRight, Sdl3GamepadButtons.Guide),
            Axes(16000, -8000, -16000, 8000, 16384, 32767));
        provider.Poll();
        Check(edges.Count == 1 && edges[0] == "D:" + indices[0].ToString() + ":Guide",
            "a newly pressed button raises one down edge with the provider device index");
        api.Snapshots[11] = Snapshot(Buttons(Sdl3GamepadButtons.DPadRight), Axes(16000, -8000, -16000, 8000, 16384, 32767));
        provider.Poll();
        Check(edges.Count == 3 && edges[1] == "U:" + indices[0].ToString() + ":Guide" && edges[2] == "U:" + indices[0].ToString() + ":South",
            "released buttons raise up edges");

        // 4. Trigger hysteresis follows the XInput lane (80% press, 70% release).
        api.Snapshots[11] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
        provider.Poll();
        edges.Clear();
        api.Snapshots[11] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, (short)(32767 * 85 / 100)));
        provider.Poll();
        Check(edges.Count == 1 && edges[0] == "D:" + indices[0].ToString() + ":RightTrigger",
            "a trigger above the activation threshold raises one down edge");
        api.Snapshots[11] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, (short)(32767 * 75 / 100)));
        provider.Poll();
        Check(edges.Count == 1, "a trigger between the thresholds stays engaged");
        api.Snapshots[11] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, (short)(32767 * 60 / 100)));
        provider.Poll();
        Check(edges.Count == 2 && edges[1] == "U:" + indices[0].ToString() + ":RightTrigger",
            "a trigger below the release threshold raises one up edge");

        // 5. Hot-plug: a removed device releases held buttons, frees its index and reuses it deterministically.
        edges.Clear();
        api.Snapshots[11] = Snapshot(Buttons(Sdl3GamepadButtons.South), Axes(0, 0, 0, 0, 0, 0));
        provider.Poll();
        edges.Clear();
        long topologyBefore = provider.TopologyVersion;
        api.Devices.Remove(11);
        provider.Poll();
        Check(edges.Count == 1 && edges[0] == "U:" + indices[0].ToString() + ":South",
            "a disconnected device releases its held buttons");
        Check(api.Closed.Contains(11), "a disconnected device is closed");
        Check(provider.TopologyVersion > topologyBefore, "hot-plug advances the topology version");
        api.Devices[13] = Device("Switch Pro", 0x057E, 0x2009);
        api.Snapshots[13] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
        provider.Poll();
        Check(provider.ConnectedDeviceIndices().Length == 2, "a new device joins after a disconnect");
        Check(provider.ConnectedDeviceIndices()[0] == Sdl3InputProvider.FirstSyntheticIndex,
            "the freed synthetic index is reused deterministically");

        // 6. Microsoft-family pads are skipped by default so the XInput lane owns them.
        api.Devices[14] = Device("Xbox Wireless Controller", Sdl3InputProvider.MicrosoftVendorId, 0x0B13);
        api.Snapshots[14] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
        provider.Poll();
        Check(provider.ConnectedDeviceIndices().Length == 2, "Microsoft-family pads stay on the XInput lane by default");
        Sdl3InputProvider unsuppressed = new Sdl3InputProvider(api, false);
        unsuppressed.Poll();
        Check(unsuppressed.ConnectedDeviceIndices().Length == 3,
            "disabling duplicate suppression lets SDL3 expose Microsoft-family pads too");

        provider.Dispose();
        Check(api.ShutdownCount == 1, "disposing the provider shuts the SDL3 runtime down once");
        unsuppressed.Dispose();
        Check(api.ShutdownCount == 2, "each provider instance shuts down its own runtime handle once");

        // 7. Optional real-runtime smoke: only runs when INPUTSTITCH_SDL3_DLL points at a local SDL3.dll, so
        //    normal (CI) runs stay independent of the native library while a developer machine can prove the
        //    P/Invoke binding against the real runtime.
        string nativePath = Environment.GetEnvironmentVariable(Sdl3NativeApi.DllPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(nativePath))
        {
            // Developer convenience: use the restored third-party copy when present (scripts/Restore-Sdl3.ps1).
            DirectoryInfo root = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            if (root.Parent != null) root = root.Parent;
            if (root.Parent != null) root = root.Parent;
            string arch = Environment.Is64BitProcess ? "x64" : "x86";
            string restored = System.IO.Path.Combine(root.FullName, "third-party", "SDL3", arch, "SDL3.dll");
            if (System.IO.File.Exists(restored)) nativePath = restored;
        }
        if (!string.IsNullOrWhiteSpace(nativePath) && System.IO.File.Exists(nativePath))
        {
            using (Sdl3NativeApi native = new Sdl3NativeApi(nativePath))
            {
                bool started = native.Initialize();
                Check(started, "the real SDL3 runtime initializes through the native binding: " + native.StatusSummary);
                if (started)
                {
                    int[] live = native.ListDeviceIds();
                    Check(live != null, "the real runtime enumerates devices without throwing");
                    Sdl3InputProvider liveProvider = new Sdl3InputProvider(native);
                    liveProvider.Poll();
                    Check(liveProvider.Available, "the provider reports the real runtime as available");
                    Console.WriteLine("REAL-SDL3 smoke: device count=" + live.Length.ToString() +
                        "; provider status=" + liveProvider.StatusSummary);
                    liveProvider.Dispose();
                }
            }
        }
        else
        {
            Console.WriteLine("REAL-SDL3 smoke skipped (set " + Sdl3NativeApi.DllPathEnvironmentVariable +
                " to a local SDL3.dll to run it).");
        }

        Console.WriteLine("PASS sdl3 provider: " + checks + " checks; mapping, edges, hysteresis, hot-plug and duplicate suppression with a fake SDL3 API.");
        return 0;
    }
}
