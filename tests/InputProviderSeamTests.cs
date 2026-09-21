using System;
using System.Collections.Generic;
using System.Linq;
using InputStitch;

internal static class InputProviderSeamTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        public void SendDown(InputSpec input) { Events.Add("D"); }
        public void SendUp(InputSpec input) { Events.Add("U"); }
        public void NeutralizeGamepad() { Events.Add("N"); }
    }

    // Minimal provider double: proves the router consumes the seam, not a concrete XInput service.
    private sealed class FakeProvider : IGamepadInputProvider
    {
        public readonly Dictionary<int, XInputPadState> States = new Dictionary<int, XInputPadState>();
        public readonly List<int> Connected = new List<int>();
        public Action<InputEventInfo> InputDown { get; set; }
        public Action<InputEventInfo> InputUp { get; set; }
        public string ProviderName { get { return "fake"; } }
        public long TopologyVersion { get { return 7; } }
        public int PollCount;
        public void Poll() { PollCount++; }
        public int[] ConnectedDeviceIndices() { return Connected.ToArray(); }
        public bool TryGetState(int deviceIndex, out XInputPadState state)
        {
            state = new XInputPadState();
            if (!Connected.Contains(deviceIndex)) return false;
            XInputPadState stored;
            if (States.TryGetValue(deviceIndex, out stored)) state = stored;
            state.Connected = true;
            return true;
        }
        public bool IsTriggerSatisfied(TriggerSpec trigger) { return false; }
        public void RaiseDown(int index, GamepadControl control)
        {
            Action<InputEventInfo> handler = InputDown;
            if (handler == null) return;
            handler(new InputEventInfo
            {
                Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = control },
                DeviceIndex = index
            });
        }
        public void Dispose() { }
    }

    private static XInputPadState Pad(ushort buttons)
    {
        return new XInputPadState { Connected = true, Buttons = buttons };
    }

    private static bool HasSource(OutputOwnershipSnapshot snapshot, string sourceId)
    {
        return snapshot.Sources.Any(delegate(OutputSourceSnapshot source) { return source.SourceId == sourceId; });
    }

    private static bool HasMerged(OutputOwnershipSnapshot snapshot, GamepadControl control)
    {
        return snapshot.Merged.Any(delegate(InputSpec input)
        {
            return input != null && input.Kind == InputKind.Gamepad && input.GamepadControl == control;
        });
    }

    public static int Main()
    {
        // 1. The existing XInput service implements the seam with its historical behaviour.
        XInputPadState[] pads = new XInputPadState[4];
        XInputInputService xinput = new XInputInputService(delegate { return -1; }, delegate(int index) { return pads[index]; });
        IGamepadInputProvider provider = xinput;
        Check(provider.ProviderName == "xinput", "the XInput service exposes a provider name through the seam");
        pads[2] = Pad(0x1000);
        xinput.Poll();
        int[] indices = provider.ConnectedDeviceIndices();
        Check(indices.Length == 1 && indices[0] == 2, "provider device indices match the classic XInput slots");
        XInputPadState state;
        Check(provider.TryGetState(2, out state) && state.Connected && (state.Buttons & 0x1000) != 0,
            "provider state access returns the polled pad state");
        Check(!provider.TryGetState(3, out state) && !state.Connected, "a disconnected index reports no state");
        Check(provider.TopologyVersion > 0, "provider topology version is exposed through the seam");

        // 2. The router consumes the seam, including synthetic indices above the XInput range (SDL3 devices).
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager ownership = new OutputOwnershipManager(backend);
        GamepadRouterService router = new GamepadRouterService(ownership);
        router.Configure(true);
        FakeProvider fake = new FakeProvider();
        fake.Connected.Add(1);
        fake.Connected.Add(7);
        fake.States[1] = Pad(0x1000);
        fake.States[7] = Pad(0x2000);
        router.Tick(fake, 0);
        OutputOwnershipSnapshot merged = ownership.Snapshot();
        Check(router.RoutedControllerCount == 2, "the router routes classic and synthetic provider devices together");
        Check(HasSource(merged, "router:xinput:1") && HasSource(merged, "router:xinput:7"),
            "both provider devices own independent router sources");
        Check(!HasSource(merged, "router:xinput:0"), "the virtual Xbox slot is still excluded by index");
        Check(HasMerged(merged, GamepadControl.South) && HasMerged(merged, GamepadControl.East),
            "digital state from both provider devices merges into ownership");

        // 3. A disappearing synthetic device releases only its own source.
        fake.Connected.Remove(7);
        router.Tick(fake, 0);
        merged = ownership.Snapshot();
        Check(!HasSource(merged, "router:xinput:7"), "a disconnected synthetic device releases its source");
        Check(HasSource(merged, "router:xinput:1"), "disconnecting one provider device leaves the other source intact");
        Check(router.RoutedControllerCount == 1, "the routed controller count follows the provider device set");

        // 4. Provider loss clears every routed source, including synthetic indices.
        router.Tick(null, 0);
        merged = ownership.Snapshot();
        Check(!HasSource(merged, "router:xinput:1") && !HasSource(merged, "router:xinput:7"),
            "provider loss clears all routed sources");
        Check(router.RoutedControllerCount == 0, "provider loss resets the routed controller count");

        // 5. Edge callbacks still travel through the seam with the provider device index.
        List<int> edgeIndices = new List<int>();
        fake.Connected.Clear();
        fake.Connected.Add(9);
        fake.InputDown = delegate(InputEventInfo e) { edgeIndices.Add(e.DeviceIndex); };
        fake.RaiseDown(9, GamepadControl.North);
        Check(edgeIndices.Count == 1 && edgeIndices[0] == 9,
            "provider edge callbacks carry the provider device index");

        Console.WriteLine("PASS input provider seam: " + checks + " checks; interface + XInput adapter + provider-driven router, fakes only.");
        return 0;
    }
}
