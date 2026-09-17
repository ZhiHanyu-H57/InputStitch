using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using InputStitch;

internal static class AnalogTransformTests
{
    private static int checks;

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

    private static void CheckNear(int actual, int expected, int tolerance, string message)
    {
        Check(Math.Abs(actual - expected) <= tolerance,
            message + " (expected=" + expected.ToString() + ", actual=" + actual.ToString() + ")");
    }

    private static InputSpec Pad(GamepadControl control, int x, int y, int value)
    {
        return new InputSpec
        {
            Kind = InputKind.Gamepad,
            GamepadControl = control,
            GamepadX = x,
            GamepadY = y,
            GamepadValue = value
        };
    }

    [STAThread]
    public static int Main()
    {
        try
        {
            IdentityAndNormalization();
            StickTransforms();
            TriggerTransforms();
            PipelineAndHalfAxis();
            RouterIntegration();
            ConfigRoundTrip();
            DialogSmoke();
            Console.WriteLine("PASS analog transform: " + checks + " checks; pure math/fake XInput/output only.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void IdentityAndNormalization()
    {
        AnalogTransformProfile profile = new AnalogTransformProfile();
        Check(profile.IsIdentity, "default transform profile is exact identity");
        InputSpec corner = Pad(GamepadControl.LeftStick, 100, 100, 100);
        InputSpec unchanged = AnalogTransformEngine.Apply(corner, profile);
        Check(unchanged.GamepadX == 100 && unchanged.GamepadY == 100,
            "identity bypass preserves pre-merge corner contribution exactly instead of normalizing it early");
        InputSpec trigger = AnalogTransformEngine.Apply(Pad(GamepadControl.LeftTrigger, 0, 0, 47), profile);
        Check(trigger.GamepadValue == 47, "identity trigger bypass preserves exact percentage");

        AnalogStickTransformConfig invalidStick = new AnalogStickTransformConfig
        {
            InnerDeadzonePercent = 140,
            OuterDeadzonePercent = 80,
            CurveExponentPercent = 900,
            ScalePercent = -1,
            MaxOutputPercent = 130
        };
        AnalogStickTransformConfig.Normalize(invalidStick);
        Check(invalidStick.InnerDeadzonePercent == 99 && invalidStick.OuterDeadzonePercent == 0,
            "stick deadzone normalization keeps inner+outer below 100");
        Check(invalidStick.CurveExponentPercent == 400 && invalidStick.ScalePercent == 0 && invalidStick.MaxOutputPercent == 100,
            "stick curve/scale/clamp normalization bounds config");

        AnalogTriggerTransformConfig invalidTrigger = new AnalogTriggerTransformConfig
        {
            InnerDeadzonePercent = -5,
            OuterDeadzonePercent = 105,
            CurveExponentPercent = 1,
            ScalePercent = 999,
            MaxOutputPercent = -3
        };
        AnalogTriggerTransformConfig.Normalize(invalidTrigger);
        Check(invalidTrigger.InnerDeadzonePercent == 0 && invalidTrigger.OuterDeadzonePercent == 99,
            "trigger deadzone normalization bounds config");
        Check(invalidTrigger.CurveExponentPercent == 25 && invalidTrigger.ScalePercent == 300 && invalidTrigger.MaxOutputPercent == 0,
            "trigger curve/scale/clamp normalization bounds config");
    }

    private static void StickTransforms()
    {
        AnalogStickTransformConfig inner = new AnalogStickTransformConfig { InnerDeadzonePercent = 20 };
        int x = 10, y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, inner);
        Check(x == 0 && y == 0, "radial inner deadzone suppresses small stick input");
        x = 60; y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, inner);
        CheckNear(x, 50, 1, "inner deadzone remaps remaining range continuously");

        AnalogStickTransformConfig outer = new AnalogStickTransformConfig { OuterDeadzonePercent = 10 };
        x = 90; y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, outer);
        Check(x == 100, "outer deadzone reaches full output at configured edge");

        AnalogStickTransformConfig curve = new AnalogStickTransformConfig { CurveExponentPercent = 200 };
        x = 50; y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, curve);
        CheckNear(x, 25, 1, "200 percent response curve squares normalized magnitude");
        curve.CurveExponentPercent = 50;
        x = 25; y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, curve);
        CheckNear(x, 50, 1, "50 percent response curve gives square-root-like response");

        AnalogStickTransformConfig scaled = new AnalogStickTransformConfig { ScalePercent = 150, MaxOutputPercent = 80 };
        x = 60; y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, scaled);
        Check(x == 80, "scale then max-output clamp uses documented order");

        AnalogStickTransformConfig inverted = new AnalogStickTransformConfig { InvertX = true, InvertY = true };
        x = 30; y = 40;
        AnalogTransformEngine.TransformStick(ref x, ref y, inverted);
        CheckNear(x, -30, 1, "stick X inversion preserves magnitude");
        CheckNear(y, -40, 1, "stick Y inversion preserves magnitude");

        AnalogStickTransformConfig radial = new AnalogStickTransformConfig { InnerDeadzonePercent = 20 };
        x = 42; y = 42;
        AnalogTransformEngine.TransformStick(ref x, ref y, radial);
        CheckNear(Math.Abs(x), Math.Abs(y), 1, "radial transform preserves diagonal direction instead of applying square axial deadzone");
        Check(x > 0 && y > 0, "radial transform preserves quadrant");
    }

    private static void TriggerTransforms()
    {
        AnalogTriggerTransformConfig inner = new AnalogTriggerTransformConfig { InnerDeadzonePercent = 20 };
        Check(AnalogTransformEngine.TransformTrigger(10, inner) == 0, "trigger inner deadzone suppresses small travel");
        CheckNear(AnalogTransformEngine.TransformTrigger(60, inner), 50, 1, "trigger inner deadzone remaps active travel");

        AnalogTriggerTransformConfig outer = new AnalogTriggerTransformConfig { OuterDeadzonePercent = 10 };
        Check(AnalogTransformEngine.TransformTrigger(90, outer) == 100, "trigger outer deadzone reaches full output early");

        AnalogTriggerTransformConfig curve = new AnalogTriggerTransformConfig { CurveExponentPercent = 200 };
        CheckNear(AnalogTransformEngine.TransformTrigger(50, curve), 25, 1, "trigger response curve is applied");

        AnalogTriggerTransformConfig scaled = new AnalogTriggerTransformConfig { ScalePercent = 160, MaxOutputPercent = 70 };
        Check(AnalogTransformEngine.TransformTrigger(50, scaled) == 70, "trigger scale is followed by final clamp");
    }

    private static void PipelineAndHalfAxis()
    {
        AnalogStickTransformConfig ordered = new AnalogStickTransformConfig
        {
            InnerDeadzonePercent = 20,
            OuterDeadzonePercent = 10,
            CurveExponentPercent = 200,
            ScalePercent = 150,
            MaxOutputPercent = 80
        };
        int x = 50, y = 0;
        AnalogTransformEngine.TransformStick(ref x, ref y, ordered);
        // (0.50-0.20)/(0.90-0.20)=0.42857; squared=0.18367; *1.5=0.27551.
        CheckNear(x, 28, 1, "combined transform order is deadzone -> curve -> scale -> clamp");

        AnalogStickTransformConfig axis = new AnalogStickTransformConfig();
        Check(AnalogTransformEngine.TransformSignedAxis(70, axis, 1) == 70,
            "positive half-axis keeps positive signed input");
        Check(AnalogTransformEngine.TransformSignedAxis(-70, axis, 1) == 0,
            "positive half-axis rejects negative signed input");
        Check(AnalogTransformEngine.TransformSignedAxis(-70, axis, -1) == 70,
            "negative half-axis converts negative signed input to positive magnitude");
        Check(AnalogTransformEngine.TransformSignedAxis(70, axis, -1) == 0,
            "negative half-axis rejects positive signed input");

        Check(AnalogTransformEngine.ClassifyMagnitude(19, 20, 70) == AnalogZoneMatch.Below,
            "zone classifier reports below threshold");
        Check(AnalogTransformEngine.ClassifyMagnitude(-45, 20, 70) == AnalogZoneMatch.Inside,
            "zone classifier uses magnitude and accepts negative signed input");
        Check(AnalogTransformEngine.ClassifyMagnitude(90, 20, 70) == AnalogZoneMatch.Above,
            "zone classifier reports above band");
        Check(AnalogTransformEngine.ClassifyMagnitude(45, 70, 20) == AnalogZoneMatch.Inside,
            "zone classifier normalizes reversed bounds deterministically");

        AnalogTransformProfile profile = new AnalogTransformProfile();
        profile.LeftStick.InnerDeadzonePercent = 30;
        List<InputSpec> transformed = AnalogTransformEngine.Apply(new InputSpec[]
        {
            Pad(GamepadControl.LeftStick, 10, 0, 100),
            Pad(GamepadControl.South, 0, 0, 100),
            Pad(GamepadControl.RightTrigger, 0, 0, 0)
        }, profile);
        Check(transformed.Count == 1 && transformed[0].GamepadControl == GamepadControl.South,
            "pipeline drops neutralized analog state but preserves digital state unchanged");

        AnalogStickTransformConfig callerOwned = new AnalogStickTransformConfig
        {
            InnerDeadzonePercent = 150,
            OuterDeadzonePercent = 80,
            CurveExponentPercent = 700
        };
        int pureX = 50, pureY = 0;
        AnalogTransformEngine.TransformStick(ref pureX, ref pureY, callerOwned);
        Check(callerOwned.InnerDeadzonePercent == 150 && callerOwned.OuterDeadzonePercent == 80 && callerOwned.CurveExponentPercent == 700,
            "transform engine normalizes a clone and does not mutate caller-owned config");
    }

    private static void RouterIntegration()
    {
        XInputPadState[] pads = new XInputPadState[4];
        pads[1] = new XInputPadState
        {
            Connected = true,
            ThumbLX = 32767,
            ThumbLY = 0,
            LeftTrigger = 255,
            Buttons = 0x1000
        };
        int own = 0;
        XInputInputService input = new XInputInputService(delegate { return own; }, delegate(int index) { return pads[index]; });
        input.Poll();

        RouterSourcePolicyConfig config = new RouterSourcePolicyConfig();
        config.AnalogTransform.LeftStick.ScalePercent = 50;
        config.AnalogTransform.LeftTrigger.MaxOutputPercent = 40;
        RouterSourcePolicyEvaluator evaluator = new RouterSourcePolicyEvaluator(null, config);
        OutputOwnershipManager ownership = new OutputOwnershipManager(new FakeBackend());
        GamepadRouterService router = new GamepadRouterService(ownership, evaluator);
        router.Configure(true);
        router.Tick(input, own);
        OutputOwnershipSnapshot transformed = ownership.Snapshot();
        InputSpec ls = Find(transformed.Merged, GamepadControl.LeftStick);
        InputSpec lt = Find(transformed.Merged, GamepadControl.LeftTrigger);
        InputSpec a = Find(transformed.Merged, GamepadControl.South);
        Check(ls != null && ls.GamepadX == 50 && ls.GamepadY == 0,
            "Router applies stick transform before ownership merge");
        Check(lt != null && lt.GamepadValue == 40, "Router applies trigger transform before ownership merge");
        Check(a != null, "Router analog transform does not alter digital buttons");
        Check(evaluator.Summary.IndexOf("analog=", StringComparison.Ordinal) >= 0 &&
            evaluator.Summary.IndexOf("LS=", StringComparison.Ordinal) >= 0,
            "Router diagnostics expose custom analog-transform summary");

        evaluator.Update(new RouterSourcePolicyConfig());
        router.Tick(input, own);
        OutputOwnershipSnapshot identity = ownership.Snapshot();
        ls = Find(identity.Merged, GamepadControl.LeftStick);
        lt = Find(identity.Merged, GamepadControl.LeftTrigger);
        Check(ls != null && ls.GamepadX == 100, "live policy update restores identity stick output");
        Check(lt != null && lt.GamepadValue == 100, "live policy update restores identity trigger output");
        Check(evaluator.Summary == "all-visible",
            "identity transform preserves legacy Router diagnostic summary");

        router.Dispose();
        input.Dispose();
    }

    private static void ConfigRoundTrip()
    {
        MacroConfig config = new MacroConfig();
        config.GamepadRouterSourcePolicy.AnalogTransform.LeftStick.InnerDeadzonePercent = 17;
        config.GamepadRouterSourcePolicy.AnalogTransform.RightStick.InvertY = true;
        config.GamepadRouterSourcePolicy.AnalogTransform.RightTrigger.CurveExponentPercent = 175;
        MacroConfig cloned = ConfigPackageSerializer.CloneConfig(config);
        Check(cloned.GamepadRouterSourcePolicy != null && cloned.GamepadRouterSourcePolicy.AnalogTransform != null,
            "analog transform profile survives real config XML clone");
        Check(cloned.GamepadRouterSourcePolicy.AnalogTransform.LeftStick.InnerDeadzonePercent == 17 &&
            cloned.GamepadRouterSourcePolicy.AnalogTransform.RightStick.InvertY &&
            cloned.GamepadRouterSourcePolicy.AnalogTransform.RightTrigger.CurveExponentPercent == 175,
            "analog transform values survive config XML round trip");

        cloned.GamepadRouterSourcePolicy.AnalogTransform = null;
        ConfigPackageSerializer.NormalizeConfig(cloned);
        Check(cloned.GamepadRouterSourcePolicy.AnalogTransform != null && cloned.GamepadRouterSourcePolicy.AnalogTransform.IsIdentity,
            "missing legacy analog transform normalizes to exact identity");
    }

    private static void DialogSmoke()
    {
        Localizer.SetLanguage(Localizer.Chinese);
        using (AnalogTransformDialog dialog = new AnalogTransformDialog(new AnalogTransformProfile()))
        {
            Check(dialog.SelectedProfile.IsIdentity, "analog dialog opens with identity profile unchanged");
            TabControl tabs = FindControl<TabControl>(dialog);
            Check(tabs != null && tabs.TabCount == 4, "analog dialog exposes four independent analog channels");
            MethodInfo capture = typeof(AnalogTransformDialog).GetMethod("CaptureProfile", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(capture != null, "analog dialog exposes deterministic private capture boundary to smoke test");
            capture.Invoke(dialog, null);
            Check(dialog.SelectedProfile.IsIdentity, "capturing untouched analog dialog remains exact identity");
        }

        RouterSourcePolicyConfig policy = new RouterSourcePolicyConfig();
        policy.AnalogTransform.LeftStick.InnerDeadzonePercent = 15;
        using (RouterSourceDialog dialog = new RouterSourceDialog(policy, delegate { return new DeviceInventorySnapshot(); }))
        {
            FieldInfo buttonField = typeof(RouterSourceDialog).GetField("analogTransform", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo summaryField = typeof(RouterSourceDialog).GetField("analogSummary", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(buttonField != null && buttonField.GetValue(dialog) is Button,
                "Router source dialog exposes analog-transform editor entry point");
            Label summary = summaryField == null ? null : summaryField.GetValue(dialog) as Label;
            Check(summary != null && summary.Text.IndexOf("已自定义", StringComparison.Ordinal) >= 0,
                "Router source dialog visibly reports custom analog transform state");
        }
    }

    private static T FindControl<T>(Control root) where T : Control
    {
        if (root == null) return null;
        T own = root as T;
        if (own != null) return own;
        foreach (Control child in root.Controls)
        {
            T found = FindControl<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static InputSpec Find(List<InputSpec> values, GamepadControl control)
    {
        if (values == null) return null;
        return values.FirstOrDefault(delegate(InputSpec input)
        {
            return input != null && input.Kind == InputKind.Gamepad && input.GamepadControl == control;
        });
    }
}
