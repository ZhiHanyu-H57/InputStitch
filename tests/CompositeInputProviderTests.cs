using System;
using System.Collections.Generic;
using InputStitch;

internal static class CompositeInputProviderTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private sealed class FakeProvider : IGamepadInputProvider
    {
        private readonly List<int> indices = new List<int>();
        public readonly Dictionary<int, XInputPadState> States = new Dictionary<int, XInputPadState>();
        public readonly List<string> Calls = new List<string>();
        public Action<InputEventInfo> InputDown { get; set; }
        public Action<InputEventInfo> InputUp { get; set; }
        public string Name;
        public long Version;
        public bool Disposed;

        public FakeProvider(string name) { Name = name; }

        public string ProviderName { get { return Name; } }
        public long TopologyVersion { get { return Version; } }
        public void Poll() { Calls.Add("poll"); }
        public int[] ConnectedDeviceIndices() { return indices.ToArray(); }
        public bool TryGetState(int deviceIndex, out XInputPadState state)
        {
            if (States.TryGetValue(deviceIndex, out state) && state.Connected) return true;
            state = new XInputPadState();
            return false;
        }
        public bool IsTriggerSatisfied(TriggerSpec trigger)
        {
            if (trigger == null || trigger.Kind != InputKind.Gamepad) return false;
            foreach (XInputPadState state in States.Values)
                if (state.Connected && state.Buttons != 0) return true;
            return false;
        }
        public void Dispose() { Disposed = true; }

        public void Add(int index, ushort buttons)
        {
            indices.Add(index);
            States[index] = new XInputPadState { Connected = true, Buttons = buttons };
        }
        public void Remove(int index)
        {
            indices.Remove(index);
            States.Remove(index);
        }
        public void RaiseDown(int index, GamepadControl control) { Raise(InputDown, index, control); }
        public void RaiseUp(int index, GamepadControl control) { Raise(InputUp, index, control); }
        private static void Raise(Action<InputEventInfo> handler, int index, GamepadControl control)
        {
            if (handler == null) return;
            handler(new InputEventInfo
            {
                Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = control },
                DeviceIndex = index
            });
        }
    }

    private static XInputPadState Pad(ushort buttons)
    {
        return new XInputPadState { Connected = true, Buttons = buttons };
    }

    public static int Main()
    {
        bool rejected = false;
        try { new CompositeInputProvider(new IGamepadInputProvider[0]); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "a composite requires at least one provider");

        FakeProvider xinput = new FakeProvider("xinput");
        FakeProvider sdl3 = new FakeProvider("sdl3");
        CompositeInputProvider composite = new CompositeInputProvider(xinput, sdl3);
        xinput.Add(1, 0x1000);
        sdl3.Add(4, 0x2000);
        sdl3.Add(7, 0x4000);
        composite.Poll();
        Check(xinput.Calls.Count == 1 && sdl3.Calls.Count == 1, "every child provider is polled");
        int[] indices = composite.ConnectedDeviceIndices();
        Check(indices.Length == 3 && indices[0] == 1 && indices[1] == 4 && indices[2] == 7,
            "device indices from all providers are merged into one sorted space");
        XInputPadState state;
        Check(composite.TryGetState(7, out state) && state.Connected && (state.Buttons & 0x4000) != 0,
            "state reads are routed to the provider that owns the index");
        Check(!composite.TryGetState(3, out state) && !state.Connected, "unknown indices report no state");
        Check(composite.TopologyVersion == xinput.TopologyVersion + sdl3.TopologyVersion,
            "the composite topology version combines the child versions");
        xinput.Version = 5;
        sdl3.Version = 2;
        composite.Poll();
        Check(composite.TopologyVersion == 7, "a child topology change moves the composite version");

        List<string> edges = new List<string>();
        composite.InputDown = delegate(InputEventInfo e) { edges.Add("D:" + e.DeviceIndex.ToString() + ":" + e.Input.GamepadControl.ToString()); };
        composite.InputUp = delegate(InputEventInfo e) { edges.Add("U:" + e.DeviceIndex.ToString() + ":" + e.Input.GamepadControl.ToString()); };
        xinput.RaiseDown(1, GamepadControl.South);
        sdl3.RaiseDown(7, GamepadControl.North);
        sdl3.RaiseUp(7, GamepadControl.North);
        Check(edges.Count == 3 && edges[0] == "D:1:South" && edges[1] == "D:7:North" && edges[2] == "U:7:North",
            "edge callbacks from every provider are forwarded with their original device index");

        Check(composite.IsTriggerSatisfied(new TriggerSpec { Kind = InputKind.Gamepad, GamepadUserIndex = -1, GamepadControl = GamepadControl.South }),
            "trigger queries aggregate across providers");

        // Duplicate indices: the first provider in constructor order owns the index.
        FakeProvider duplicate = new FakeProvider("duplicate");
        duplicate.Add(4, 0x8000);
        CompositeInputProvider overlapping = new CompositeInputProvider(sdl3, duplicate);
        overlapping.Poll();
        Check(overlapping.TryGetState(4, out state) && (state.Buttons & 0x2000) != 0 && (state.Buttons & 0x8000) == 0,
            "the first provider owns a duplicated device index");
        overlapping.Dispose();

        // Pinned triggers never leak across providers: an XInput pad must not satisfy an index that belongs to
        // the synthetic range (and the XInput provider itself enforces the same rule).
        XInputPadState[] pads = new XInputPadState[4];
        pads[0] = Pad(0x1000);
        XInputInputService realXinput = new XInputInputService(delegate { return -1; }, delegate(int index) { return pads[index]; });
        realXinput.Poll();
        Check(realXinput.IsTriggerSatisfied(new TriggerSpec { Kind = InputKind.Gamepad, GamepadUserIndex = -1, GamepadControl = GamepadControl.South }),
            "an unpinned trigger matches a visible XInput pad");
        Check(!realXinput.IsTriggerSatisfied(new TriggerSpec { Kind = InputKind.Gamepad, GamepadUserIndex = 7, GamepadControl = GamepadControl.South }),
            "a trigger pinned to a synthetic index is not satisfied by an XInput pad");

        CompositeInputProvider withReal = new CompositeInputProvider(realXinput, sdl3);
        withReal.Poll();
        Check(withReal.ConnectedDeviceIndices().Length == 3, "the composite exposes XInput and SDL3 devices together");
        withReal.Dispose();
        Check(sdl3.Disposed, "disposing the composite disposes its children");
        sdl3.RaiseDown(4, GamepadControl.South);
        Check(edges.Count == 3, "composite disposal detaches child edge handlers");

        Console.WriteLine("PASS composite input provider: " + checks + " checks; shared index space, routed state, forwarded edges, duplicate and pinned-index rules.");
        return 0;
    }
}
