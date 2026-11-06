using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InputStitch;

internal static class GamepadRouterTests
{
    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        internal bool FailNext;
        public void SendDown(InputSpec input) { if (FailNext) { FailNext = false; throw new InvalidOperationException("injected"); } Events.Add("D:" + Describe(input)); }
        public void SendUp(InputSpec input) { Events.Add("U:" + Describe(input)); }
        public void NeutralizeGamepad() { Events.Add("N"); }
        private static string Describe(InputSpec input)
        {
            if (input == null) return "null";
            return input.GamepadControl + ":" + input.GamepadX + ":" + input.GamepadY + ":" + input.GamepadValue;
        }
    }

    private static int checks;
    private sealed class ConstantProvider : IGamepadInputProvider
    {
        internal int Index = 1;
        internal XInputPadState State = new XInputPadState { Connected = true, Buttons = 0x1000, ThumbLX = 16384 };
        public string ProviderName { get { return "constant"; } }
        public long TopologyVersion { get; set; }
        public Action<InputEventInfo> InputDown { get; set; }
        public Action<InputEventInfo> InputUp { get; set; }
        public void Poll() { }
        public int[] ConnectedDeviceIndices() { return State.Connected ? new[] { Index } : new int[0]; }
        public bool TryGetState(int index, out XInputPadState state) { state = State; return index == Index && state.Connected; }
        public bool IsTriggerSatisfied(TriggerSpec trigger) { return false; }
        public void Dispose() { }
    }
    private sealed class MutablePolicy : IRouterSourcePolicyEvaluator
    {
        internal AnalogTransformProfile Transform = new AnalogTransformProfile();
        internal bool Allowed = true;
        internal string Text = "custom";
        public bool IsAllVisible { get { return true; } }
        public AnalogTransformProfile AnalogTransform { get { return Transform.Clone(); } }
        public string Summary { get { return Text; } }
        public RouterSourceDecision Evaluate(int slot, long topology) { return Allowed ? RouterSourceDecision.Allowed("", "test") : RouterSourceDecision.Blocked("", "test"); }
    }
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
        PreparedFrameLifecycle();
        TransformUpdateAfterFailure();
        CustomPolicyUpdates();
        ConcurrentRouting();
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

    private static void PreparedFrameLifecycle()
    {
        ConstantProvider input = new ConstantProvider();
        FakeBackend backend = new FakeBackend();
        OutputOwnershipManager ownership = new OutputOwnershipManager(backend);
        RouterSourcePolicyConfig config = new RouterSourcePolicyConfig();
        RouterSourcePolicyEvaluator policy = new RouterSourcePolicyEvaluator(null, config);
        using (GamepadRouterService router = new GamepadRouterService(ownership, policy))
        {
            router.Configure(true); router.Tick(input, 0);
            ownership.ClearSource("router:xinput:1");
            router.Tick(input, 0);
            Check(Find(ownership.Snapshot(), GamepadControl.South) != null, "unchanged prepared frame recovers an externally cleared source");
            ownership.ClearAll("external"); router.Tick(input, 0);
            Check(Find(ownership.Snapshot(), GamepadControl.LeftStick) != null, "unchanged prepared frame recovers after ClearAll");
            router.Stop("test"); Check(ownership.ActiveSourceCount == 0, "Stop clears prepared source");
            router.Tick(input, 0); Check(ownership.ActiveSourceCount == 1, "Stop retains enabled semantics and next frame restores source");
            router.Configure(false); router.Tick(input, 0); Check(ownership.ActiveSourceCount == 0, "disabled tick does not restore source");
            router.Configure(true); router.Tick(input, 0); Check(ownership.ActiveSourceCount == 1, "reenabling restores identical physical frame");
            router.Tick(input, 1); Check(ownership.ActiveSourceCount == 0, "same device becomes own slot and immediately releases");
            router.Tick(input, 0); Check(ownership.ActiveSourceCount == 1, "former own slot is routed again");
            input.State.Connected = false; input.TopologyVersion++; router.Tick(input, 0);
            Check(ownership.ActiveSourceCount == 0, "disconnect clears unchanged prepared source");
            input.State.Connected = true; input.Index = 7; input.TopologyVersion++; router.Tick(input, 0);
            Check(ownership.Snapshot().Sources.Any(s => s.SourceId == "router:xinput:7"), "reconnection at synthetic index uses correct source");
            config.AnalogTransform.LeftStick.InvertX = true; policy.Update(config); router.Tick(input, 0);
            Check(Find(ownership.Snapshot(), GamepadControl.LeftStick).GamepadX == -50, "transform update changes identical physical frame");
            Check(router.PolicyStatus.IndexOf("analog=", StringComparison.Ordinal) >= 0, "updated transform summary remains visible");
            config.Mode = RouterSourceModes.SelectedDevices; policy.Update(config); router.Tick(input, 0);
            Check(ownership.ActiveSourceCount == 0 && router.PolicyBlockedCount == 1, "policy denial clears cached frame");
            config.Mode = RouterSourceModes.AllVisible; policy.Update(config); router.Tick(input, 0);
            Check(ownership.ActiveSourceCount == 1, "same frame routes after policy allows again");
            input.State.Buttons = 0x2000; backend.FailNext = true; bool failed = false;
            try { router.Tick(input, 0); } catch (OutputOwnershipException) { failed = true; }
            Check(failed && ownership.ActiveSourceCount == 0, "backend failure preserves ownership fail-closed behavior");
            router.Tick(input, 0);
            Check(Find(ownership.Snapshot(), GamepadControl.East) != null, "identical failed frame recovers on next tick");
            router.Dispose(); Check(ownership.ActiveSourceCount == 0, "Dispose releases synthetic prepared source");
            router.Tick(input, 0); Check(ownership.ActiveSourceCount == 0, "disposed router cannot restore source");
        }
    }

    private static void CustomPolicyUpdates()
    {
        ConstantProvider input = new ConstantProvider(); MutablePolicy policy = new MutablePolicy();
        OutputOwnershipManager ownership = new OutputOwnershipManager(new FakeBackend());
        using (GamepadRouterService router = new GamepadRouterService(ownership, policy))
        {
            router.Configure(true); router.Tick(input, 0);
            policy.Transform.LeftStick.InvertX = true; policy.Text = "changed"; router.Tick(input, 0);
            Check(Find(ownership.Snapshot(), GamepadControl.LeftStick).GamepadX == -50, "non-versioned evaluator transform remains live each tick");
            Check(router.PolicyStatus.StartsWith("changed;", StringComparison.Ordinal), "dynamic custom summary is not configuration-cached");
            policy.Allowed = false; router.Tick(input, 0); Check(ownership.ActiveSourceCount == 0, "custom policy denial remains live");
            policy.Allowed = true; router.Tick(input, 0); Check(ownership.ActiveSourceCount == 1, "custom policy reallow restores same frame");
        }
    }

    private static void TransformUpdateAfterFailure()
    {
        XInputPadState[] pads = new XInputPadState[4];
        pads[1] = pads[2] = new XInputPadState { Connected = true, ThumbLX = 16384 };
        using (XInputInputService input = new XInputInputService(delegate { return 0; }, delegate(int i) { return pads[i]; }))
        {
            input.Poll(); FakeBackend backend = new FakeBackend(); OutputOwnershipManager ownership = new OutputOwnershipManager(backend);
            RouterSourcePolicyConfig config = new RouterSourcePolicyConfig(); RouterSourcePolicyEvaluator policy = new RouterSourcePolicyEvaluator(null, config);
            using (GamepadRouterService router = new GamepadRouterService(ownership, policy))
            {
                router.Configure(true); router.Tick(input, 0);
                config.AnalogTransform.LeftStick.InvertX = true; policy.Update(config); backend.FailNext = true;
                bool failed = false; try { router.Tick(input, 0); } catch (OutputOwnershipException) { failed = true; }
                Check(failed && ownership.ActiveSourceCount == 0, "transform transition failure clears all physical sources");
                router.Tick(input, 0);
                Check(Find(ownership.Snapshot(), GamepadControl.LeftStick).GamepadX == -100,
                    "devices not reached during failed tick still refresh the new transform on retry");
            }
        }
    }

    private static void ConcurrentRouting()
    {
        ConstantProvider input = new ConstantProvider(); OutputOwnershipManager ownership = new OutputOwnershipManager(new FakeBackend());
        RouterSourcePolicyEvaluator policy = new RouterSourcePolicyEvaluator(null, new RouterSourcePolicyConfig());
        using (GamepadRouterService router = new GamepadRouterService(ownership, policy))
        using (ManualResetEventSlim start = new ManualResetEventSlim(false))
        {
            router.Configure(true); Exception[] errors = new Exception[2];
            Thread tick = new Thread(delegate() { try { start.Wait(); for (int i = 0; i < 1000; i++) router.Tick(input, 0); } catch (Exception ex) { errors[0] = ex; } });
            Thread change = new Thread(delegate() { try { start.Wait(); for (int i = 0; i < 200; i++) { router.Stop("concurrent"); router.Configure(false); policy.Update(new RouterSourcePolicyConfig()); router.Configure(true); } } catch (Exception ex) { errors[1] = ex; } });
            tick.Start(); change.Start(); start.Set();
            Check(tick.Join(5000) && change.Join(5000), "concurrent tick/stop/configuration completes without deadlock");
            Check(errors[0] == null && errors[1] == null, "concurrent state cleanup does not invalidate collections");
            router.Configure(false); router.Tick(input, 0);
            Check(ownership.ActiveSourceCount == 0, "concurrent routing leaves no source after final disable");
        }
    }
}
