using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using InputStitch;

internal static class Sdl3ProviderTests
{
    private static int checks;
    private static int gamepadUpdates;
    private static int joystickUpdates;
    private static int nativeCount;
    private static int nativeFrees;
    private static int[] nativeIds = new[] { 7, 9 };
    private static uint nativeButtons;
    private static short[] nativeAxes = new short[6];
    private static int nativeButtonReads, nativeAxisReads, nativeCloses, nativeQuits;
    private static bool failNativeClose, failNativeAxis;
    private static IntPtr NativeList(out int count)
    {
        count = Math.Max(0, nativeCount);
        if (nativeCount < 0) return IntPtr.Zero;
        IntPtr pointer = Marshal.AllocHGlobal((count + 1) * 4);
        for (int i = 0; i < count; i++) Marshal.WriteInt32(pointer, i * 4, nativeIds[i]);
        Marshal.WriteInt32(pointer, count * 4, 0);
        return pointer;
    }
    private static void NativeFree(IntPtr pointer) { nativeFrees++; Marshal.FreeHGlobal(pointer); }
    private static void NativeEnumeration()
    {
        Sdl3NativeApi api = new Sdl3NativeApi();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Sdl3NativeApi).GetField("available", flags).SetValue(api, true);
        FieldInfo list = typeof(Sdl3NativeApi).GetField("sdlGetGamepads", flags), free = typeof(Sdl3NativeApi).GetField("sdlFree", flags);
        list.SetValue(api, Delegate.CreateDelegate(list.FieldType, typeof(Sdl3ProviderTests).GetMethod("NativeList", BindingFlags.Static | BindingFlags.NonPublic)));
        free.SetValue(api, Delegate.CreateDelegate(free.FieldType, typeof(Sdl3ProviderTests).GetMethod("NativeFree", BindingFlags.Static | BindingFlags.NonPublic)));
        for (int i=0;i<5000;i++) api.ListDeviceIds();
        Check(nativeFrees == 5000, "every allocated empty native device list is freed without waiting for GC");
        nativeCount = 1;
        int[] indices = api.ListDeviceIds();
        Check(indices.Length == 1 && indices[0] == 7 && nativeFrees == 5001, "nonempty lists are copied before the native allocation is freed");
        nativeCount = -1;
        Check(api.ListDeviceIds().Length == 0 && nativeFrees == 5001, "a null native result is not freed");
        nativeCount = 1;
        int[] retained = api.ListDeviceIds();
        Check(object.ReferenceEquals(retained, api.ListDeviceIds()), "unchanged native membership reuses its read-only snapshot");
        nativeIds[0] = 13;
        int[] replaced = api.ListDeviceIds();
        Check(replaced[0] == 13 && retained[0] == 7 && !object.ReferenceEquals(retained, replaced), "same-count membership change replaces the snapshot without mutating a retained list");
        nativeCount = 2;
        Check(api.ListDeviceIds().Length == 2 && replaced.Length == 1, "member count change preserves the previous snapshot");
        nativeCount = 0;
        Check(object.ReferenceEquals(api.ListDeviceIds(), api.ListDeviceIds()), "empty native membership reuses an empty snapshot while freeing every native list");
        Check(nativeFrees == 5007, "snapshot reuse never skips SDL_free or native enumeration");
        nativeIds[0] = 7;
    }
    private static void BindNative(Sdl3NativeApi api, string field, string method)
    {
        FieldInfo target = typeof(Sdl3NativeApi).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        target.SetValue(api, Delegate.CreateDelegate(target.FieldType, typeof(Sdl3ProviderTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)));
    }
    private static bool NativeButton(IntPtr handle, int index) { nativeButtonReads++; return (nativeButtons & (1U << index)) != 0; }
    private static short NativeAxis(IntPtr handle, int index)
    { nativeAxisReads++; if (failNativeAxis) throw new IOException("native axis failure"); return nativeAxes[index]; }
    private static void NativeClose(IntPtr handle)
    { nativeCloses++; if (failNativeClose && nativeCloses == 1) throw new IOException("native close failure"); }
    private static void NativeQuit(uint flags) { nativeQuits++; }
    private static void NativeFramesAndCleanup()
    {
        BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        Sdl3NativeApi api = new Sdl3NativeApi();
        typeof(Sdl3NativeApi).GetField("available", hidden).SetValue(api, true);
        Dictionary<int, IntPtr> handles = (Dictionary<int, IntPtr>)typeof(Sdl3NativeApi).GetField("handles", hidden).GetValue(api);
        handles.Add(7, new IntPtr(1)); handles.Add(9, new IntPtr(2));
        BindNative(api, "sdlGetGamepadButton", "NativeButton"); BindNative(api, "sdlGetGamepadAxis", "NativeAxis");
        BindNative(api, "sdlCloseGamepad", "NativeClose"); BindNative(api, "sdlQuitSubSystem", "NativeQuit");
        nativeButtons = 0x7fff;
        nativeAxes = new[] { short.MinValue, short.MaxValue, (short)1500, short.MinValue, short.MaxValue, (short)16384 };
        Sdl3DeviceSnapshot first;
        Check(api.TryGetSnapshot(7, out first) && first.Buttons == 0x7fff, "native frame reads all mapped SDL buttons into an independent value");
        Check(nativeButtonReads == 15 && nativeAxisReads == 6, "each connected device still reads every mapped button and all six axes");
        XInputPadState mapped = Sdl3InputProvider.MapSnapshot(first);
        Check(mapped.Buttons == 0xf7ff && mapped.ThumbLX == short.MinValue && mapped.ThumbLY == -short.MaxValue && mapped.ThumbRX == 1500 && mapped.ThumbRY == short.MaxValue && mapped.LeftTrigger == 255 && mapped.RightTrigger == 128,
            "value-frame mapping preserves all digital controls, Y saturation and trigger scaling");
        ushort[] expected = { 0x1000, 0x2000, 0x4000, 0x8000, 0x20, 0x400, 0x10, 0x40, 0x80, 0x100, 0x200, 1, 2, 4, 8 };
        for (int i = 0; i < expected.Length; i++)
        {
            nativeButtons = 1U << i; Sdl3DeviceSnapshot single;
            Check(api.TryGetSnapshot(7, out single) && Sdl3InputProvider.MapSnapshot(single).Buttons == expected[i], "individual SDL button mapping remains unchanged: " + i);
        }
        nativeButtons = 0; nativeAxes[0] = 42; Sdl3DeviceSnapshot later;
        Check(api.TryGetSnapshot(7, out later) && later.Buttons == 0 && later.LeftX == 42 && first.Buttons == 0x7fff && first.LeftX == short.MinValue,
            "later reads cannot mutate retained value frames");
        Check(!api.TryGetSnapshot(999, out later) && later.Equals(new Sdl3DeviceSnapshot()), "missing native handle returns false and a zero frame");
        failNativeAxis = true;
        try { api.TryGetSnapshot(7, out later); Check(false, "native getter exception is observable"); }
        catch (IOException) { Check(later.Equals(new Sdl3DeviceSnapshot()), "failed native read never publishes a partially sampled frame"); }
        failNativeAxis = false; failNativeClose = true;
        api.Shutdown(); api.Shutdown();
        Check(nativeCloses == 2 && nativeQuits == 1 && handles.Count == 0 && !api.Available, "one close failure cannot skip another handle or subsystem shutdown; cleanup is idempotent");
        Check(!api.TryGetSnapshot(7, out later) && later.Equals(new Sdl3DeviceSnapshot()), "shutdown leaves no readable stale native frame");
        api.Dispose();
        Check(nativeCloses == 2 && nativeQuits == 1, "dispose after shutdown repeats no native side effects");
        failNativeClose = false;
    }
    private static void GamepadUpdate() { gamepadUpdates++; }
    private static void JoystickUpdate() { joystickUpdates++; }

    private static void NativeUpdate()
    {
        Sdl3NativeApi api = new Sdl3NativeApi();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Sdl3NativeApi).GetField("available", flags).SetValue(api, true);
        FieldInfo gamepad = typeof(Sdl3NativeApi).GetField("sdlUpdateGamepads", flags);
        FieldInfo joystick = typeof(Sdl3NativeApi).GetField("sdlUpdateJoysticks", flags);
        gamepad.SetValue(api, Delegate.CreateDelegate(gamepad.FieldType, typeof(Sdl3ProviderTests).GetMethod("GamepadUpdate", BindingFlags.Static | BindingFlags.NonPublic)));
        joystick.SetValue(api, Delegate.CreateDelegate(joystick.FieldType, typeof(Sdl3ProviderTests).GetMethod("JoystickUpdate", BindingFlags.Static | BindingFlags.NonPublic)));
        api.Update();
        Check(gamepadUpdates == 1 && joystickUpdates == 0, "native updates pump gamepads once instead of updating joysticks twice");
        gamepad.SetValue(api, null);
        api.Update();
        Check(gamepadUpdates == 1 && joystickUpdates == 1, "the native joystick update remains available as a fallback");
    }

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
        public int DescribeCount;
        public int SnapshotCount;
        public int[] DeviceIds;
        public bool FailEnumeration;
        public readonly Dictionary<int, Sdl3DeviceDescriptor> Devices = new Dictionary<int, Sdl3DeviceDescriptor>();
        public readonly Dictionary<int, Sdl3DeviceSnapshot> Snapshots = new Dictionary<int, Sdl3DeviceSnapshot>();
        public readonly List<int> Opened = new List<int>();
        public readonly List<int> Closed = new List<int>();

        public bool Initialize() { InitializeCount++; return InitializeResult; }
        public void Shutdown() { ShutdownCount++; }
        public void Update() { UpdateCount++; }
        public int[] ListDeviceIds() { if (FailEnumeration) throw new IOException("enumeration test"); return DeviceIds ?? new List<int>(Devices.Keys).ToArray(); }
        public Sdl3DeviceDescriptor Describe(int deviceId)
        {
            DescribeCount++;
            Sdl3DeviceDescriptor descriptor;
            return Devices.TryGetValue(deviceId, out descriptor) ? descriptor : null;
        }
        public bool Open(int deviceId) { Opened.Add(deviceId); return true; }
        public void Close(int deviceId) { Closed.Add(deviceId); }
        public bool TryGetSnapshot(int deviceId, out Sdl3DeviceSnapshot snapshot)
        {
            SnapshotCount++;
            Snapshots.TryGetValue(deviceId, out snapshot);
            return true;
        }
    }

    private static Sdl3DeviceDescriptor Device(string name, ushort vendorId, ushort productId)
    {
        return new Sdl3DeviceDescriptor { Name = name, VendorId = vendorId, ProductId = productId };
    }

    private static Sdl3DeviceSnapshot Snapshot(bool[] buttons, short[] axes)
    {
        uint mask = 0;
        if (buttons != null) for (int i = 0; i < buttons.Length && i < 32; i++) if (buttons[i]) mask |= 1U << i;
        return new Sdl3DeviceSnapshot { Buttons = mask,
            LeftX = axes == null || axes.Length < 1 ? (short)0 : axes[0],
            LeftY = axes == null || axes.Length < 2 ? (short)0 : axes[1],
            RightX = axes == null || axes.Length < 3 ? (short)0 : axes[2],
            RightY = axes == null || axes.Length < 4 ? (short)0 : axes[3],
            LeftTrigger = axes == null || axes.Length < 5 ? (short)0 : axes[4],
            RightTrigger = axes == null || axes.Length < 6 ? (short)0 : axes[5] };
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
        NativeUpdate();
        NativeEnumeration();
        NativeFramesAndCleanup();
        PollingLifetime();
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
        Check(api.DescribeCount == 2, "unchanged connected devices do not requery native metadata every frame");
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

        // 8. Embedded-runtime extraction helper: release builds carry the pinned SDL3.dll as a resource and
        //    extract it with a hash check into the per-user runtime cache.
        string extractionRoot = Path.Combine(Path.GetTempPath(), "InputStitch-Sdl3Extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractionRoot);
        try
        {
            byte[] payload = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            string payloadHash;
            using (SHA256 sha = SHA256.Create())
            {
                StringBuilder text = new StringBuilder();
                foreach (byte value in sha.ComputeHash(payload)) text.Append(value.ToString("x2"));
                payloadHash = text.ToString();
            }
            string target = Path.Combine(extractionRoot, "SDL3.dll");
            Check(Sdl3NativeApi.TryWriteVerified(new MemoryStream(payload), target, payloadHash),
                "the embedded-runtime helper writes a hash-verified payload");
            Check(File.Exists(target) && new FileInfo(target).Length == payload.Length,
                "the extracted runtime matches the payload");
            Check(Sdl3NativeApi.TryWriteVerified(new MemoryStream(payload), target, payloadHash),
                "an already extracted runtime is reused without rewriting");
            string rejected = Path.Combine(extractionRoot, "rejected.dll");
            Check(!Sdl3NativeApi.TryWriteVerified(new MemoryStream(new byte[] { 9, 9, 9 }), rejected, payloadHash),
                "a payload that fails the pinned hash is rejected");
            Check(!File.Exists(rejected), "a rejected payload leaves no file behind");
            Check(!Sdl3NativeApi.TryWriteVerified(null, Path.Combine(extractionRoot, "none.dll"), payloadHash),
                "a missing source stream is rejected");
        }
        finally
        {
            try { Directory.Delete(extractionRoot, true); } catch { }
        }

        Console.WriteLine("PASS sdl3 provider: " + checks + " checks; mapping, edges, hysteresis, hot-plug and duplicate suppression with a fake SDL3 API.");
        return 0;
    }

    private static void PollingLifetime()
    {
        FakeSdl3Api api = new FakeSdl3Api();
        api.Devices[21] = Device("DualSense", 0x054C, 1);
        api.Devices[22] = Device("Xbox", Sdl3InputProvider.MicrosoftVendorId, 1);
        api.DeviceIds = new[] { 21, 21, 22, 22 };
        api.Snapshots[21] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
        Sdl3InputProvider provider = new Sdl3InputProvider(api);
        List<InputEventInfo> downs = new List<InputEventInfo>(), ups = new List<InputEventInfo>();
        provider.InputDown = downs.Add;
        provider.InputUp = ups.Add;
        provider.Poll();
        int index = provider.ConnectedDeviceIndices()[0];
        Check(api.Opened.Count == 1 && api.DescribeCount == 2, "duplicate ids open and describe each present device once");
        int reads = api.SnapshotCount, updates = api.UpdateCount;
        for (int i = 0; i < 100; i++) provider.Poll();
        Check(api.UpdateCount - updates == 100 && api.SnapshotCount - reads == 100, "unchanged membership still updates and reads live state every tick");
        Check(api.DescribeCount == 2, "a suppressed Xbox instance does not repeatedly query unchanged metadata");
        api.Snapshots[21] = Snapshot(Buttons(Sdl3GamepadButtons.South), Axes(0, 0, 0, 0, 32767, 32767));
        provider.Poll();
        Check(downs.Count == 3, "button and both triggers have independent down edges");
        api.Snapshots[21] = Snapshot(Buttons(Sdl3GamepadButtons.South), Axes(0, 0, 0, 0, (short)(32767 * 75 / 100), (short)(32767 * 75 / 100)));
        provider.Poll();
        Check(ups.Count == 0, "both held triggers retain hysteresis before disconnect");
        api.DeviceIds = new[] { 22 };
        provider.Poll();
        Check(ups.Count == 3 && ups.Exists(delegate(InputEventInfo e) { return e.Input.GamepadControl == GamepadControl.LeftTrigger; }) && ups.Exists(delegate(InputEventInfo e) { return e.Input.GamepadControl == GamepadControl.RightTrigger; }), "disconnect releases both latched triggers as well as held buttons");
        Check(ups.TrueForAll(delegate(InputEventInfo e) { return e.DeviceIndex == index; }), "disconnect release edges retain the removed device index");
        provider.Poll();
        Check(ups.Count == 3, "disconnected controls are released exactly once");

        api.Snapshots[21] = Snapshot(Buttons(Sdl3GamepadButtons.South), Axes(0, 0, 0, 0, 32767, 32767));
        api.DeviceIds = new[] { 21, 22 };
        provider.Poll();
        Check(downs.Count == 3 && provider.ConnectedDeviceIndices()[0] == index, "reconnected held inputs establish a baseline without activation");
        api.DeviceIds = new[] { 21 };
        provider.Poll();
        api.DeviceIds = new[] { 21, 22 };
        provider.Poll();
        Check(api.DescribeCount == 4, "suppressed metadata cache is discarded when that instance leaves");

        api.FailEnumeration = true;
        provider.Poll();
        Check(ups.Count == 6 && provider.ConnectedDeviceIndices().Length == 0, "enumeration failure releases the last known button and trigger state");
        Check(provider.StatusSummary.StartsWith("enumerate-failed:", StringComparison.Ordinal), "enumeration failure remains visible in diagnostics");
        api.FailEnumeration = false;
        provider.Poll();
        Check(provider.StatusSummary == "ready; devices=1" && downs.Count == 3, "successful recovery restores ready status and reconnect baseline");
        // A callback must run after the state lock is released; use another thread to observe it.
        bool outsideLock = false;
        provider.InputUp = delegate(InputEventInfo e)
        {
            ups.Add(e);
            System.Threading.Thread observer = new System.Threading.Thread(delegate()
            { provider.ConnectedDeviceIndices(); outsideLock = true; });
            observer.Start();
            Check(observer.Join(1000), "release callbacks do not hold the provider state lock");
        };
        long topology = provider.TopologyVersion;
        provider.Dispose();
        Check(outsideLock && ups.Count == 9 && provider.CurrentGamingDevices().Count == 0, "dispose releases held controls and clears published device metadata");
        Check(provider.TopologyVersion > topology && !provider.Available, "dispose invalidates published topology and availability");
        provider.Dispose();
        updates = api.UpdateCount;
        provider.Poll();
        Check(ups.Count == 9 && api.ShutdownCount == 1 && api.UpdateCount == updates, "repeated dispose and subsequent polling stay inert");
        Sdl3InputProvider neverStarted = new Sdl3InputProvider(api);
        neverStarted.Dispose(); neverStarted.Poll();
        Check(api.InitializeCount == 1, "disposing before the first poll cannot initialize a runtime later");

        FakeSdl3Api reentrantApi = new FakeSdl3Api();
        reentrantApi.Devices[31] = Device("DualSense", 0x054C, 2);
        reentrantApi.Snapshots[31] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
        using (Sdl3InputProvider reentrant = new Sdl3InputProvider(reentrantApi))
        {
            reentrant.Poll();
            InputEventInfo retained = null;
            int releases = 0;
            reentrant.InputUp = delegate { releases++; };
            reentrant.InputDown = delegate(InputEventInfo e)
            {
                retained = e;
                reentrantApi.Snapshots[31] = Snapshot(Buttons(), Axes(0, 0, 0, 0, 0, 0));
                reentrant.Poll();
            };
            reentrantApi.Snapshots[31] = Snapshot(Buttons(Sdl3GamepadButtons.South), Axes(0, 0, 0, 0, 0, 0));
            reentrant.Poll();
            Check(releases == 1 && retained != null && retained.Input.GamepadControl == GamepadControl.South, "reentrant callbacks retain independent edge objects and frame lists");
        }
    }
}
