using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using InputStitch;

internal static class XInputInputTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private sealed class Edge
    {
        public bool Down;
        public int Slot;
        public GamepadControl Control;
        public int Value;
    }

    private static XInputPadState Pad(bool connected, ushort buttons, byte lt, byte rt)
    {
        return new XInputPadState { Connected = connected, Buttons = buttons, LeftTrigger = lt, RightTrigger = rt };
    }

    private static object StaticCall(Type type, string name, params object[] args)
    {
        return type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }

    public static int Main()
    {
        XInputPadState[] pads = new XInputPadState[4];
        int own = -1;
        List<Edge> edges = new List<Edge>();
        XInputInputService service = new XInputInputService(delegate { return own; }, delegate(int index) { return pads[index]; });
        service.InputDown = delegate(InputEventInfo e) { edges.Add(new Edge { Down = true, Slot = e.DeviceIndex, Control = e.Input.GamepadControl, Value = e.Input.GamepadValue }); };
        service.InputUp = delegate(InputEventInfo e) { edges.Add(new Edge { Down = false, Slot = e.DeviceIndex, Control = e.Input.GamepadControl, Value = e.Input.GamepadValue }); };

        // First sight of a connected controller establishes a baseline instead of firing a macro.
        pads[0] = Pad(true, 0x1000, 0, 0); // A already held at startup.
        service.Poll();
        Check(edges.Count == 0, "startup held button does not create a Down edge");
        pads[0] = Pad(true, 0, 0, 0);
        service.Poll();
        Check(edges.Count == 1 && !edges[0].Down && edges[0].Slot == 0 && edges[0].Control == GamepadControl.South,
            "startup-held baseline must release before later activation");
        edges.Clear();
        pads[0] = Pad(true, 0x1000, 0, 0);
        service.Poll();
        Check(edges.Count == 1 && edges[0].Down && edges[0].Slot == 0 && edges[0].Control == GamepadControl.South,
            "A press becomes controller Down");

        TriggerSpec anyA = new TriggerSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.South, GamepadUserIndex = -1 };
        TriggerSpec slot0A = anyA.Clone(); slot0A.GamepadUserIndex = 0;
        TriggerSpec slot1A = anyA.Clone(); slot1A.GamepadUserIndex = 1;
        Check(service.IsTriggerSatisfied(anyA), "any-controller A sees slot 0");
        Check(service.IsTriggerSatisfied(slot0A), "pinned A sees originating slot");
        Check(!service.IsTriggerSatisfied(slot1A), "pinned A does not see another slot");

        // A second controller is independent.
        pads[1] = Pad(true, 0, 0, 0);
        service.Poll();
        edges.Clear();
        pads[1] = Pad(true, 0x2000, 0, 0); // B
        service.Poll();
        Check(edges.Count == 1 && edges[0].Down && edges[0].Slot == 1 && edges[0].Control == GamepadControl.East,
            "second controller produces an independent B edge");

        // Trigger hysteresis: >=80% activates, the 70-80% band retains state, <=70% releases.
        pads[2] = Pad(true, 0, 0, 0);
        service.Poll();
        edges.Clear();
        pads[2] = Pad(true, 0, 205, 0); // ~80.4%
        service.Poll();
        Check(edges.Count == 1 && edges[0].Down && edges[0].Control == GamepadControl.LeftTrigger,
            "LT crosses activation threshold");
        pads[2] = Pad(true, 0, 190, 0); // ~74.5%
        service.Poll();
        Check(edges.Count == 1, "LT hysteresis band does not chatter");
        pads[2] = Pad(true, 0, 178, 0); // ~69.8%
        service.Poll();
        Check(edges.Count == 2 && !edges[1].Down && edges[1].Control == GamepadControl.LeftTrigger,
            "LT crosses release threshold");

        // The InputStitch-owned ViGEm Xbox slot is excluded dynamically.
        pads[3] = Pad(true, 0, 0, 0);
        service.Poll();
        own = 3;
        edges.Clear();
        pads[3] = Pad(true, 0x1000, 0, 0);
        service.Poll();
        Check(edges.Count == 0, "own virtual Xbox slot cannot feed back into triggers");
        Check(!service.ConnectedUserIndices().Contains(3), "own virtual slot is absent from visible source list");
        Check(service.ExcludedUserIndex == 3, "own virtual slot is reported as excluded");

        // Disconnecting a pressed physical controller synthesizes Up so Hold mappings cannot stick.
        own = -1;
        pads[3] = Pad(true, 0, 0, 0);
        service.Poll(); // new baseline after exclusion ends
        pads[3] = Pad(true, 0x4000, 0, 0); // X
        edges.Clear();
        service.Poll();
        Check(edges.Count == 1 && edges[0].Down && edges[0].Control == GamepadControl.West, "X down before disconnect");
        pads[3] = new XInputPadState();
        service.Poll();
        Check(edges.Count == 2 && !edges[1].Down && edges[1].Control == GamepadControl.West, "disconnect synthesizes X up");

        // Trigger matching understands any-controller configuration and runtime device pinning.
        InputEventInfo slot0Event = new InputEventInfo { DeviceIndex = 0, Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.South, GamepadValue = 100 } };
        InputEventInfo slot1Event = new InputEventInfo { DeviceIndex = 1, Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.South, GamepadValue = 100 } };
        Check(ModifierSafetyPolicy.TriggerTerminalMatches(anyA, slot0Event), "any-controller trigger matches slot 0");
        Check(ModifierSafetyPolicy.TriggerTerminalMatches(anyA, slot1Event), "any-controller trigger matches slot 1");
        Check(ModifierSafetyPolicy.TriggerTerminalMatches(slot0A, slot0Event), "pinned trigger matches source slot");
        Check(!ModifierSafetyPolicy.TriggerTerminalMatches(slot0A, slot1Event), "pinned trigger rejects other controller release");
        TriggerSpec clone = slot0A.Clone();
        Check(clone.Kind == InputKind.Gamepad && clone.GamepadControl == GamepadControl.South && clone.GamepadUserIndex == 0,
            "gamepad trigger clone preserves source metadata");
        Check(InputNames.FormatTrigger(anyA).Contains("XInput") && InputNames.FormatTrigger(anyA).Contains(InputNames.FormatGamepadControl(GamepadControl.South)),
            "controller trigger has user-readable display text");

        MacroDefinition controllerMacro = new MacroDefinition { Trigger = anyA, SuppressTrigger = true };
        Check(!ModifierSafetyPolicy.ShouldSuppressTrigger(controllerMacro), "observed controller trigger never claims physical suppression before Router");
        Check(MacroRuntimeClassifier.IsHoldTriggerSupported(anyA), "controller buttons are valid Hold triggers");
        MacroDefinition quick = QuickMacros.Create(QuickTemplate.Sequence, "controller to key", anyA,
            new[] { new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.K, Action = MacroAction.Press } }, 1);
        Check(quick.Trigger.Kind == InputKind.Gamepad && quick.Steps[0].Kind == InputKind.Keyboard,
            "quick create supports controller to keyboard hybrid mapping");

        // Private conflict logic: any-controller A overlaps a slot-specific A, but not B.
        TriggerSpec anyB = new TriggerSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.East, GamepadUserIndex = -1 };
        Check((bool)StaticCall(typeof(MainForm), "TriggersEqual", anyA, slot0A), "any-controller and pinned same control conflict");
        Check(!(bool)StaticCall(typeof(MainForm), "TriggersEqual", anyA, anyB), "different controller controls do not conflict");

        service.Dispose();
        Console.WriteLine("PASS XInput input: " + checks + " checks; injected states only, no real controller or virtual device used.");
        return 0;
    }
}
