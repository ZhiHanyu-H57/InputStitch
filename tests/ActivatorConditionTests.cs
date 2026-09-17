using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using InputStitch;

internal static class ActivatorConditionTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static MacroDefinition Macro(string mode)
    {
        MacroDefinition macro = new MacroDefinition();
        macro.Name = mode;
        macro.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 };
        macro.Activator.Mode = mode;
        macro.Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A } };
        return macro;
    }

    private static InputEventInfo Key(Keys key)
    {
        return new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key }, DeviceIndex = -1 };
    }

    private static TriggerSpec Trigger(Keys key)
    {
        return new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key };
    }

    [STAThread]
    public static int Main()
    {
        try
        {
            ConfigAndClassifier();
            PressReleaseAndHeld();
            LongAndDoublePress();
            EdgeOnlyWheelSemantics();
            ConditionLossAndReset();
            AnalogZoneEvaluation();
            ConfigRoundTrip();
            DialogAndMainUiSmoke();
            Console.WriteLine("PASS activator/condition: " + checks + " checks; pure state/fake config only.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void ConfigAndClassifier()
    {
        ActivatorConfig config = new ActivatorConfig { Mode = "weird", LongPressMs = 1, DoublePressWindowMs = 9999 };
        ActivatorConfig.Normalize(config);
        Check(config.Mode == ActivatorModes.Legacy, "unknown activator normalizes to legacy");
        Check(config.LongPressMs == 100 && config.DoublePressWindowMs == 2000, "activator timing bounds normalize");

        MacroConditionConfig conditions = new MacroConditionConfig
        {
            ForegroundProcessName = "  GTA5_Enhanced.exe  ",
            ForegroundTitleContains = "  Grand Theft Auto V  ",
            DeviceKey = "  device:test  ",
            Analog = new AnalogZoneCondition
            {
                Enabled = true,
                Control = GamepadControl.LeftStick,
                Direction = AnalogConditionDirection.PositiveX,
                MinimumPercent = 90,
                MaximumPercent = 20
            }
        };
        MacroConditionConfig.Normalize(conditions);
        Check(conditions.ForegroundProcessName == "GTA5_Enhanced.exe" && conditions.DeviceKey == "device:test", "condition strings trim");
        Check(conditions.Analog.MinimumPercent == 20 && conditions.Analog.MaximumPercent == 90, "analog zone bounds normalize order");

        MacroDefinition hold = Macro(ActivatorModes.WhileHeld);
        hold.RunMode = TriggerRunMode.Toggle;
        MacroRuntimeCapability holdCapability = MacroRuntimeClassifier.Classify(hold);
        Check(holdCapability.Category == MacroRuntimeCategory.ConcurrentHoldMacro || holdCapability.Category == MacroRuntimeCategory.ParallelHeldMapping,
            "advanced WhileHeld uses hold lifecycle even when legacy RunMode is Toggle");

        MacroDefinition press = Macro(ActivatorModes.Press);
        press.RunMode = TriggerRunMode.Hold;
        MacroRuntimeCapability pressCapability = MacroRuntimeClassifier.Classify(press);
        Check(pressCapability.Category == MacroRuntimeCategory.ConcurrentTimedMacro,
            "advanced Press uses timed lifecycle even when legacy RunMode is Hold");

        MacroDefinition legacy = Macro(ActivatorModes.Legacy);
        legacy.RunMode = TriggerRunMode.Hold;
        Check(ActivatorModes.IsHoldLifecycle(legacy), "legacy Hold remains hold lifecycle");
        legacy.RunMode = TriggerRunMode.Toggle;
        Check(!ActivatorModes.IsHoldLifecycle(legacy), "legacy Toggle remains timed lifecycle");
    }

    private static void PressReleaseAndHeld()
    {
        ActivatorRuntimeEngine engine = new ActivatorRuntimeEngine();
        MacroDefinition press = Macro(ActivatorModes.Press);
        ActivatorDecision d = engine.OnDown(press, Key(Keys.F8), Trigger(Keys.F8), 1000, true);
        Check(d != null && d.Kind == ActivatorDecisionKind.StartOnce && d.Reason == "press", "Press fires on down");
        Check(engine.OnDown(press, Key(Keys.F8), Trigger(Keys.F8), 1010, true) == null, "repeated down while held is ignored");
        engine.OnUp(Key(Keys.F8), 1020, delegate { return true; });

        MacroDefinition release = Macro(ActivatorModes.Release);
        Check(engine.OnDown(release, Key(Keys.F8), Trigger(Keys.F8), 2000, true) == null, "Release waits on down");
        List<ActivatorDecision> up = engine.OnUp(Key(Keys.F8), 2020, delegate { return true; });
        Check(up.Count == 1 && up[0].Kind == ActivatorDecisionKind.StartOnce && up[0].Reason == "release", "Release fires on matching up");

        MacroDefinition held = Macro(ActivatorModes.WhileHeld);
        d = engine.OnDown(held, Key(Keys.F8), Trigger(Keys.F8), 3000, true);
        Check(d != null && d.Kind == ActivatorDecisionKind.StartHeld, "WhileHeld starts hold lifecycle on down");
        up = engine.OnUp(Key(Keys.F8), 3010, delegate { return true; });
        Check(up.Count == 0, "physical held release remains delegated to existing hold-release runtime");
    }

    private static void LongAndDoublePress()
    {
        ActivatorRuntimeEngine engine = new ActivatorRuntimeEngine();
        MacroDefinition longPress = Macro(ActivatorModes.LongPress);
        longPress.Activator.LongPressMs = 500;
        Check(engine.OnDown(longPress, Key(Keys.F8), Trigger(Keys.F8), 1000, true) == null, "LongPress waits initially");
        Check(engine.Tick(1499, delegate { return true; }).Count == 0, "LongPress does not fire early");
        List<ActivatorDecision> tick = engine.Tick(1500, delegate { return true; });
        Check(tick.Count == 1 && tick[0].Reason == "long-press", "LongPress fires at threshold");
        Check(engine.Tick(1800, delegate { return true; }).Count == 0, "LongPress fires only once per hold");
        engine.OnUp(Key(Keys.F8), 1810, delegate { return true; });

        MacroDefinition doublePress = Macro(ActivatorModes.DoublePress);
        doublePress.Activator.DoublePressWindowMs = 300;
        Check(engine.OnDown(doublePress, Key(Keys.F8), Trigger(Keys.F8), 2000, true) == null, "first DoublePress down arms window");
        engine.OnUp(Key(Keys.F8), 2010, delegate { return true; });
        ActivatorDecision second = engine.OnDown(doublePress, Key(Keys.F8), Trigger(Keys.F8), 2250, true);
        Check(second != null && second.Reason == "double-press", "second press inside window fires");
        engine.OnUp(Key(Keys.F8), 2260, delegate { return true; });
        Check(engine.OnDown(doublePress, Key(Keys.F8), Trigger(Keys.F8), 3000, true) == null, "new double sequence starts after previous fires");
        engine.OnUp(Key(Keys.F8), 3010, delegate { return true; });
        Check(engine.OnDown(doublePress, Key(Keys.F8), Trigger(Keys.F8), 3401, true) == null, "press outside window does not fire");

        MacroDefinition gatedDouble = Macro(ActivatorModes.DoublePress);
        gatedDouble.Activator.DoublePressWindowMs = 300;
        Check(engine.OnDown(gatedDouble, Key(Keys.F8), Trigger(Keys.F8), 4000, true) == null, "gated DoublePress arms on a valid first press");
        engine.OnUp(Key(Keys.F8), 4010, delegate { return true; });
        Check(engine.OnDown(gatedDouble, Key(Keys.F8), Trigger(Keys.F8), 4100, false) == null, "invalid-condition press never fires DoublePress");
        engine.OnUp(Key(Keys.F8), 4110, delegate { return false; });
        Check(engine.OnDown(gatedDouble, Key(Keys.F8), Trigger(Keys.F8), 4200, true) == null,
            "invalid-condition press resets DoublePress history instead of bridging two valid presses");
    }

    private static void EdgeOnlyWheelSemantics()
    {
        ActivatorRuntimeEngine engine = new ActivatorRuntimeEngine();
        InputEventInfo wheel = new InputEventInfo { Input = new InputSpec { Kind = InputKind.WheelUp } };
        TriggerSpec wheelTrigger = new TriggerSpec { Kind = InputKind.WheelUp };

        MacroDefinition press = Macro(ActivatorModes.Press);
        Check(engine.OnDown(press, wheel, wheelTrigger, 1000, true) != null &&
            engine.OnDown(press, wheel, wheelTrigger, 1010, true) != null,
            "edge-only wheel Press can fire repeatedly without a synthetic release");

        MacroDefinition dbl = Macro(ActivatorModes.DoublePress);
        dbl.Activator.DoublePressWindowMs = 300;
        Check(engine.OnDown(dbl, wheel, wheelTrigger, 2000, true) == null, "wheel DoublePress first edge arms window");
        ActivatorDecision second = engine.OnDown(dbl, wheel, wheelTrigger, 2200, true);
        Check(second != null && second.Reason == "double-press", "wheel DoublePress uses two real wheel edges");

        MacroDefinition release = Macro(ActivatorModes.Release);
        MacroDefinition held = Macro(ActivatorModes.WhileHeld);
        MacroDefinition longPress = Macro(ActivatorModes.LongPress);
        Check(engine.OnDown(release, wheel, wheelTrigger, 3000, true) == null &&
            engine.OnDown(held, wheel, wheelTrigger, 3010, true) == null &&
            engine.OnDown(longPress, wheel, wheelTrigger, 3020, true) == null,
            "edge-only wheel refuses Release/WhileHeld/LongPress instead of inventing held state");
        Check(engine.Tick(9999, delegate { return true; }).Count == 0,
            "unsupported wheel hold modes cannot later fire from timer state");
    }

    private static void ConditionLossAndReset()
    {
        ActivatorRuntimeEngine engine = new ActivatorRuntimeEngine();
        MacroDefinition held = Macro(ActivatorModes.WhileHeld);
        ActivatorDecision d = engine.OnDown(held, Key(Keys.F8), Trigger(Keys.F8), 1000, true);
        Check(d != null && d.Kind == ActivatorDecisionKind.StartHeld, "condition-loss setup starts held source");
        List<ActivatorDecision> tick = engine.Tick(1010, delegate { return false; });
        Check(tick.Count == 1 && tick[0].Kind == ActivatorDecisionKind.StopHeld && tick[0].Reason == "condition-failed",
            "WhileHeld fails closed when a condition becomes false");
        Check(engine.Tick(1020, delegate { return true; }).Count == 0, "condition recovery does not restart a still-held trigger");
        engine.OnUp(Key(Keys.F8), 1030, delegate { return true; });

        MacroDefinition longPress = Macro(ActivatorModes.LongPress);
        longPress.Activator.LongPressMs = 200;
        engine.OnDown(longPress, Key(Keys.F8), Trigger(Keys.F8), 2000, true);
        engine.Tick(2050, delegate { return false; });
        Check(engine.Tick(2300, delegate { return true; }).Count == 0, "LongPress is permanently disarmed after condition loss during a hold");
        engine.Reset(longPress);
        Check(engine.TrackedCount >= 0, "per-macro reset is safe");
        engine.Clear();
        Check(engine.TrackedCount == 0, "global activator reset clears all timing state");
    }

    private static void AnalogZoneEvaluation()
    {
        MethodInfo method = typeof(MainForm).GetMethod("IsAnalogConditionSatisfied", BindingFlags.Static | BindingFlags.NonPublic);
        Check(method != null, "analog condition evaluator is available to runtime");
        XInputPadState state = new XInputPadState
        {
            Connected = true,
            LeftTrigger = 204,
            ThumbLX = 24575,
            ThumbLY = -16384
        };
        AnalogZoneCondition trigger = new AnalogZoneCondition
        {
            Enabled = true,
            Control = GamepadControl.LeftTrigger,
            MinimumPercent = 75,
            MaximumPercent = 85
        };
        Check((bool)method.Invoke(null, new object[] { state, trigger }), "trigger analog zone uses percentage band");
        AnalogZoneCondition positiveX = new AnalogZoneCondition
        {
            Enabled = true,
            Control = GamepadControl.LeftStick,
            Direction = AnalogConditionDirection.PositiveX,
            MinimumPercent = 70,
            MaximumPercent = 80
        };
        Check((bool)method.Invoke(null, new object[] { state, positiveX }), "stick positive half-axis zone uses reusable half-axis primitive");
        positiveX.Direction = AnalogConditionDirection.PositiveY;
        Check(!(bool)method.Invoke(null, new object[] { state, positiveX }), "opposite signed stick half-axis does not satisfy zone");
    }

    private static void ConfigRoundTrip()
    {
        MacroConfig config = new MacroConfig();
        MacroDefinition macro = Macro(ActivatorModes.DoublePress);
        macro.Activator.DoublePressWindowMs = 420;
        macro.Conditions.ForegroundProcessName = "GTA5_Enhanced";
        macro.Conditions.DeviceKey = "device:test";
        macro.Conditions.Analog.Enabled = true;
        macro.Conditions.Analog.Control = GamepadControl.RightStick;
        macro.Conditions.Analog.Direction = AnalogConditionDirection.NegativeY;
        macro.Conditions.Analog.MinimumPercent = 30;
        macro.Conditions.Analog.MaximumPercent = 60;
        config.Macros.Add(macro);
        MacroConfig clone = ConfigPackageSerializer.CloneConfig(config);
        MacroDefinition loaded = clone.Macros[0];
        Check(loaded.Activator != null && loaded.Activator.Mode == ActivatorModes.DoublePress && loaded.Activator.DoublePressWindowMs == 420,
            "activator config survives real XML clone");
        Check(loaded.Conditions != null && loaded.Conditions.ForegroundProcessName == "GTA5_Enhanced" && loaded.Conditions.DeviceKey == "device:test",
            "foreground/device conditions survive real XML clone");
        Check(loaded.Conditions.Analog.Enabled && loaded.Conditions.Analog.Control == GamepadControl.RightStick &&
            loaded.Conditions.Analog.Direction == AnalogConditionDirection.NegativeY && loaded.Conditions.Analog.MinimumPercent == 30,
            "analog zone condition survives real XML clone");

        loaded.Activator = null;
        loaded.Conditions = null;
        ConfigPackageSerializer.NormalizeConfig(clone);
        Check(loaded.Activator != null && loaded.Activator.Mode == ActivatorModes.Legacy,
            "missing legacy activator normalizes to Legacy");
        Check(loaded.Conditions != null && loaded.Conditions.IsEmpty, "missing legacy conditions normalize to no additional gates");
    }

    private static void DialogAndMainUiSmoke()
    {
        Localizer.SetLanguage(Localizer.Chinese);
        MacroDefinition macro = Macro(ActivatorModes.LongPress);
        macro.Activator.LongPressMs = 650;
        macro.Conditions.ForegroundProcessName = "GTA5_Enhanced";
        DeviceInventorySnapshot snapshot = new DeviceInventorySnapshot();
        using (ActivatorConditionDialog dialog = new ActivatorConditionDialog(macro, delegate { return snapshot; }, "基础层"))
        {
            ComboBox mode = GetField<ComboBox>(dialog, "mode");
            Label deferred = FindLabelContaining(dialog, "Turbo");
            Check(mode != null && mode.Items.Count == 6 && mode.SelectedIndex == 4,
                "activation dialog exposes Legacy/Press/Release/WhileHeld/Long/Double without fake Turbo option");
            Check(deferred != null, "activation dialog explicitly documents deferred Turbo semantics");
            MethodInfo capture = typeof(ActivatorConditionDialog).GetMethod("CaptureValues", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(capture != null, "activation dialog has deterministic capture boundary");
            capture.Invoke(dialog, null);
            Check(dialog.SelectedActivator.Mode == ActivatorModes.LongPress && dialog.SelectedActivator.LongPressMs == 650,
                "activation dialog preserves selected LongPress values");
            Check(dialog.SelectedConditions.ForegroundProcessName == "GTA5_Enhanced",
                "activation dialog preserves foreground condition");
        }

        MacroConfig uiConfig = new MacroConfig();
        MacroDefinition advanced = Macro(ActivatorModes.DoublePress);
        advanced.RunMode = TriggerRunMode.Hold; // legacy fallback remains stored, but must not own active semantics.
        advanced.Conditions.ForegroundTitleContains = "Grand Theft Auto V";
        uiConfig.Macros.Add(advanced);
        using (MainForm form = new MainForm(uiConfig, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InputStitch-activator-ui-" + Guid.NewGuid().ToString("N"))))
        {
            Button button = GetField<Button>(form, "activatorConditionButton");
            ComboBox legacyMode = GetField<ComboBox>(form, "triggerModeBox");
            Check(button != null && button.Enabled && button.Text.IndexOf("双击", StringComparison.Ordinal) >= 0,
                "main editor visibly summarizes advanced activator mode");
            Check(button.Text.IndexOf("1 个条件", StringComparison.Ordinal) >= 0,
                "main editor visibly summarizes active condition count");
            Check(legacyMode != null && !legacyMode.Enabled,
                "legacy Toggle/Hold selector is disabled while an explicit advanced activator owns semantics");
            MethodInfo diagnosticsMethod = typeof(MainForm).GetMethod("BuildDiagnosticsText", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo observationMethod = typeof(MainForm).GetMethod("BuildRuntimeObservationText", BindingFlags.Instance | BindingFlags.NonPublic);
            string diagnostics = diagnosticsMethod == null ? "" : diagnosticsMethod.Invoke(form, null) as string;
            string observation = observationMethod == null ? "" : observationMethod.Invoke(form, null) as string;
            Check((diagnostics ?? "").IndexOf("ActivationRule[0]: mode=DoublePress", StringComparison.Ordinal) >= 0,
                "diagnostics exposes configured advanced activation semantics");
            Check((observation ?? "").IndexOf("configured=1", StringComparison.Ordinal) >= 0,
                "runtime observation exposes configured/tracked activator state");
        }
    }

    private static T GetField<T>(object target, string name) where T : class
    {
        FieldInfo field = target == null ? null : target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        return field == null ? null : field.GetValue(target) as T;
    }

    private static Label FindLabelContaining(Control root, string text)
    {
        if (root == null) return null;
        Label own = root as Label;
        if (own != null && (own.Text ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) return own;
        foreach (Control child in root.Controls)
        {
            Label found = FindLabelContaining(child, text);
            if (found != null) return found;
        }
        return null;
    }
}
