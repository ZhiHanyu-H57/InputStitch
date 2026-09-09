using System;
using System.Collections.Generic;
using System.Linq;
using InputStitch;

internal static class GamepadRouterTests
{
    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        public void SendDown(InputSpec input) { Events.Add("D:" + Describe(input)); }
        public void SendUp(InputSpec input) { Events.Add("U:" + Describe(input)); }
        public void NeutralizeGamepad() { Events.Add("N"); }
        private static string Describe(InputSpec input)
        {
            if (input == null) return "null";
            return input.GamepadControl + ":" + input.GamepadX + ":" + input.GamepadY + ":" + input.GamepadValue;
        }
    }

    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static XInputPadState Pad(bool connected, ushort buttons, byte lt, byte rt, short lx, short ly, short rx, short ry)
    {
        return new XInputPadState
        {
            Connected = connected,
            Buttons = buttons,
            LeftTrigger = lt,
            RightTrigger = rt,
            ThumbLX = lx,
            ThumbLY = ly,
            ThumbRX = rx,
            ThumbRY = ry
        };
    }

    private static InputSpec Find(OutputOwnershipSnapshot snapshot, GamepadControl control)
    {
        return snapshot.Merged.FirstOrDefault(delegate(InputSpec x)
        {
            return x != null && x.Kind == InputKind.Gamepad && x.GamepadControl == control;
        });
    }

    public static int Main()
    {
        XInputPadState[] pads = new XInputPadState[4];
        int own = 0;
        XInputInputService input = new XInputInputService(delegate { return own; }, delegate(int index) { return pads[index]; });
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager ownership = new OutputOwnershipManager(backend);
        GamepadRouterService router = new GamepadRouterService(ownership);
        router.Configure(true);

        // Slot 0 represents InputStitch's own virtual Xbox and must never become a router source.
        pads[0] = Pad(true, 0x1000, 255, 255, 32767, 32767, 32767, 32767);
        pads[1] = Pad(true, 0x1000, 128, 0, 16384, 0, 0, 0); // A + 50% LT + half-right LS
        pads[2] = Pad(true, 0x2000, 0, 192, 0, 16384, 0, -16384); // B + 75% RT + half-up LS + half-down RS
        input.Poll();
        router.Tick(input, own);

        OutputOwnershipSnapshot merged = ownership.Snapshot();
        Check(router.OwnsPreferredSlotZero && router.OwnSlot == 0, "router reports virtual Xbox in preferred slot 0");
        Check(router.RoutedControllerCount == 2, "two external XInput controllers are routed");
        Check(merged.Sources.Any(delegate(OutputSourceSnapshot s) { return s.SourceId == "router:xinput:1"; }), "slot 1 owns an independent router source");
        Check(merged.Sources.Any(delegate(OutputSourceSnapshot s) { return s.SourceId == "router:xinput:2"; }), "slot 2 owns an independent router source");
        Check(!merged.Sources.Any(delegate(OutputSourceSnapshot s) { return s.SourceId == "router:xinput:0"; }), "own virtual slot is never fed back into router");
        Check(Find(merged, GamepadControl.South) != null && Find(merged, GamepadControl.East) != null, "digital buttons from two controllers merge");
        InputSpec lt = Find(merged, GamepadControl.LeftTrigger);
        InputSpec rt = Find(merged, GamepadControl.RightTrigger);
        Check(lt != null && lt.GamepadValue == GamepadRouterService.ScaleTrigger(128), "left trigger mirrors raw XInput strength");
        Check(rt != null && rt.GamepadValue == GamepadRouterService.ScaleTrigger(192), "right trigger mirrors raw XInput strength");
        InputSpec ls = Find(merged, GamepadControl.LeftStick);
        Check(ls != null && ls.GamepadX == 50 && ls.GamepadY == 50, "left sticks from separate controllers vector-merge through ownership");
        InputSpec rs = Find(merged, GamepadControl.RightStick);
        Check(rs != null && rs.GamepadX == 0 && rs.GamepadY == -50, "right stick state is mirrored");

        // Identical reports must be a no-op: no output churn and no ownership version churn.
        int eventsBeforeSame = backend.Events.Count;
        long versionBeforeSame = ownership.Snapshot().Version;
        input.Poll();
        router.Tick(input, own);
        Check(backend.Events.Count == eventsBeforeSame, "unchanged router frame sends no native output");
        Check(ownership.Snapshot().Version == versionBeforeSame, "unchanged router frame does not churn ownership version");

        // Neutralizing one source removes only its contribution; the other physical controller survives.
        pads[1] = Pad(true, 0, 0, 0, 0, 0, 0, 0);
        input.Poll();
        router.Tick(input, own);
        merged = ownership.Snapshot();
        Check(Find(merged, GamepadControl.South) == null, "slot 1 A releases independently");
        Check(Find(merged, GamepadControl.East) != null, "slot 2 B survives slot 1 neutralization");
        Check(Find(merged, GamepadControl.LeftTrigger) == null, "slot 1 LT releases independently");
        ls = Find(merged, GamepadControl.LeftStick);
        Check(ls != null && ls.GamepadX == 0 && ls.GamepadY == 50, "remaining slot 2 left stick survives");

        // Disconnecting the remaining source clears its entire router contribution.
        pads[2] = new XInputPadState();
        input.Poll();
        router.Tick(input, own);
        Check(ownership.Snapshot().Merged.Count == 0, "disconnect clears complete controller source");
        Check(router.RoutedControllerCount == 1, "connected-but-neutral slot still counts as routed source");

        // Non-zero own slot is allowed, but explicitly reports that slot-0-only games may ignore it.
        own = 2;
        pads[0] = Pad(true, 0x4000, 0, 0, 0, 0, 0, 0); // physical X on slot 0
        pads[1] = new XInputPadState();
        pads[2] = Pad(true, 0x1000, 255, 255, 32767, 32767, 32767, 32767); // own virtual state, excluded
        input.Poll();
        router.Tick(input, own);
        merged = ownership.Snapshot();
        Check(!router.OwnsPreferredSlotZero && router.OwnSlot == 2, "router exposes non-zero virtual slot");
        Check(router.LastStatus == "routing-nonzero-slot", "non-zero routing status is explicit");
        Check(Find(merged, GamepadControl.West) != null, "physical slot 0 can still be mirrored into non-zero virtual target");
        Check(Find(merged, GamepadControl.South) == null, "new own slot cannot feed back into output");

        // Losing the virtual Xbox target clears routed output so stale physical state cannot stick.
        router.Tick(input, -1);
        Check(ownership.Snapshot().Merged.Count == 0, "missing virtual target clears router ownership");
        Check(router.LastStatus == "waiting-for-virtual-xbox", "missing target enters explicit waiting state");

        // Conversion boundaries preserve signed axes and full trigger range.
        Check(GamepadRouterService.ScaleAxis(short.MinValue) == -100, "negative axis extreme maps to -100");
        Check(GamepadRouterService.ScaleAxis(short.MaxValue) == 100, "positive axis extreme maps to +100");
        Check(GamepadRouterService.ScaleTrigger(0) == 0 && GamepadRouterService.ScaleTrigger(255) == 100, "trigger endpoints map to 0..100");

        router.Configure(false);
        Check(!router.Enabled && ownership.Snapshot().Merged.Count == 0, "disabling router releases all router outputs");
        router.Dispose();
        input.Dispose();
        Console.WriteLine("PASS gamepad router: " + checks + " checks; injected XInput/fake backend only, no real controller or virtual device.");
        return 0;
    }
}
