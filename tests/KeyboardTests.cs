using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using InputStitch;

internal static class KeyboardTests
{
    private static int checks;
    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }
    private static InputSpec K(Keys key) { return new InputSpec { VirtualKey = (int)key }; }
    private static InputEventInfo E(Keys key, bool ctrl, bool shift, bool alt = false, bool win = false)
    { return new InputEventInfo { Input = K(key), Ctrl = ctrl, Shift = shift, Alt = alt, Win = win }; }
    private static FieldInfo Field(Type type, string name) { return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); }
    private static object Call(object target, string name, params object[] args)
    { return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args); }
    private static object StaticCall(Type type, string name, params object[] args)
    { return type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args); }
    private static uint KeyboardFlags(InputSpec spec, bool up)
    {
        object input = StaticCall(typeof(InputSender), "BuildKeyboardInput", spec, up);
        object union = input.GetType().GetField("U").GetValue(input);
        object keyboard = union.GetType().GetField("ki").GetValue(union);
        return (uint)keyboard.GetType().GetField("dwFlags").GetValue(keyboard);
    }
    private static void RenderKeyboard(string language, bool compact)
    {
        Localizer.SetLanguage(language);
        using (VirtualKeyboardDialog dialog = new VirtualKeyboardDialog(true, new[] { K(Keys.LControlKey), K(Keys.K) }))
        {
            if (compact) dialog.Size = new Size(800, 490);
            typeof(Control).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, new object[] { 2, true });
            dialog.CreateControl(); dialog.PerformLayout();
            using (Bitmap preview = new Bitmap(dialog.Width, dialog.Height))
            {
                dialog.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
                preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keyboard-" + language + (compact ? "-compact" : "") + ".png"));
            }
        }
    }
    private static TriggerSpec Trigger(params InputSpec[] keys)
    {
        TriggerSpec t; string error;
        Check(VirtualKeyboardDialog.TryCreateTrigger(keys, out t, out error), "create " + keys[keys.Length - 1].VirtualKey);
        return t;
    }
    [STAThread]
    public static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            TriggerSpec shift = Trigger(K(Keys.LShiftKey));
            Check(!shift.Shift && ModifierSafetyPolicy.TriggerMatchesExactly(shift, E(Keys.LShiftKey, false, true)), "standalone left Shift matches its hook modifier snapshot");
            Check(!ModifierSafetyPolicy.TriggerMatchesExactly(shift, E(Keys.RShiftKey, false, true)), "left Shift keeps side distinction");
            foreach (Keys modifierKey in new[] { Keys.LControlKey, Keys.RControlKey, Keys.LShiftKey, Keys.RShiftKey,
                Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin })
            {
                int mask = ModifierSafetyPolicy.ModifierMaskForKey((int)modifierKey);
                TriggerSpec standalone = Trigger(K(modifierKey));
                Check(!standalone.Ctrl && !standalone.Shift && !standalone.Alt && !standalone.Win && standalone.VirtualKey == (int)modifierKey,
                    "standalone modifier stored as terminal key " + modifierKey);
                Check(ModifierSafetyPolicy.TriggerMatchesExactly(standalone, E(modifierKey,
                    (mask & ModifierSafetyPolicy.Ctrl) != 0, (mask & ModifierSafetyPolicy.Shift) != 0,
                    (mask & ModifierSafetyPolicy.Alt) != 0, (mask & ModifierSafetyPolicy.Win) != 0)),
                    "standalone modifier matches physical hook snapshot " + modifierKey);
            }
            foreach (Keys shiftKey in new[] { Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey })
            {
                MacroDefinition map = new MacroDefinition { RunMode = TriggerRunMode.Hold, SuppressTrigger = true,
                    Trigger = new TriggerSpec { VirtualKey = (int)shiftKey } };
                map.Steps.Add(new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadY = 80 });
                Check(ModifierSafetyPolicy.PreserveNativeShiftForGamepad(map), "held gamepad mapping preserves " + shiftKey);
                Check(!ModifierSafetyPolicy.ShouldSuppressTrigger(map) && map.SuppressTrigger, "effective passthrough does not rewrite saved preference " + shiftKey);
                Keys physical = shiftKey == Keys.ShiftKey ? Keys.LShiftKey : shiftKey;
                Check(ModifierSafetyPolicy.TriggerMatchesExactly(map.Trigger, E(physical, false, true)), "native Shift still triggers mapping " + shiftKey);
                Check(ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(map.Steps[0], ModifierSafetyPolicy.Shift) == 0, "Shift does not block gamepad output " + shiftKey);
                map.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.Enter });
                Check(ModifierSafetyPolicy.ShouldSuppressTrigger(map), "mixed keyboard macro retains suppression " + shiftKey);
                map.Steps.RemoveAt(1); map.RunMode = TriggerRunMode.Toggle;
                Check(ModifierSafetyPolicy.ShouldSuppressTrigger(map), "toggle macro retains suppression " + shiftKey);
                map.RunMode = TriggerRunMode.Hold; map.Trigger.Ctrl = true;
                Check(ModifierSafetyPolicy.ShouldSuppressTrigger(map), "modifier chord retains suppression " + shiftKey);
                map.Trigger.Ctrl = false; map.Trigger.VirtualKey = (int)Keys.C;
                Check(ModifierSafetyPolicy.ShouldSuppressTrigger(map), "C mapping retains suppression preference");
                map.Trigger.Kind = InputKind.MouseX2;
                Check(ModifierSafetyPolicy.ShouldSuppressTrigger(map), "mouse mapping retains suppression preference");
                map.SuppressTrigger = false;
                Check(!ModifierSafetyPolicy.ShouldSuppressTrigger(map), "explicit passthrough remains respected");
            }
            TriggerSpec esc = Trigger(K(Keys.Escape));
            Check(ModifierSafetyPolicy.TriggerMatchesExactly(esc, E(Keys.Escape, false, false)), "Esc is a usable virtual trigger");
            TriggerSpec chord = Trigger(K(Keys.K), K(Keys.LControlKey), K(Keys.LShiftKey));
            Check(chord.VirtualKey == (int)Keys.K && chord.Ctrl && chord.Shift, "ordinary key terminal independent of selection order");
            Check(ModifierSafetyPolicy.TriggerMatchesExactly(chord, E(Keys.K, true, true)), "Ctrl Shift K matches exactly");
            Check(!ModifierSafetyPolicy.TriggerMatchesExactly(chord, E(Keys.K, true, false)), "missing Shift does not match");
            TriggerSpec rejected; string error;
            Check(!VirtualKeyboardDialog.TryCreateTrigger(new[] { K(Keys.A), K(Keys.B) }, out rejected, out error), "multiple ordinary trigger keys rejected");
            Check(!VirtualKeyboardDialog.TryCreateTrigger(new[] { K(Keys.LControlKey), K(Keys.RControlKey), K(Keys.K) }, out rejected, out error), "duplicate modifier sides rejected even with ordinary terminal");
            Check(!VirtualKeyboardDialog.TryCreateTrigger(new InputSpec[0], out rejected, out error), "empty trigger rejected");
            TriggerSpec numEnter = Trigger(new InputSpec { VirtualKey = (int)Keys.Enter, Extended = true });
            Check(!ModifierSafetyPolicy.TriggerMatchesExactly(numEnter, E(Keys.Enter, false, false)), "numpad Enter differs from main Enter");
            InputEventInfo extendedEnter = E(Keys.Enter, false, false); extendedEnter.Input.Extended = true;
            Check(ModifierSafetyPolicy.TriggerMatchesExactly(numEnter, extendedEnter), "numpad Enter matches extended event");
            TriggerSpec oldEnter = new TriggerSpec { VirtualKey = (int)Keys.Enter };
            Check(ModifierSafetyPolicy.TriggerMatchesExactly(oldEnter, extendedEnter), "legacy Enter remains compatible with both Enter keys");
            TriggerSpec mainEnter = Trigger(K(Keys.Enter));
            Check(!(bool)StaticCall(typeof(MainForm), "TriggersEqual", mainEnter, numEnter), "distinct Enter triggers have no false conflict");
            Check((bool)StaticCall(typeof(MainForm), "TriggersEqual", oldEnter, numEnter), "legacy wildcard Enter still conflicts with numpad Enter");
            Check(InputNames.FormatInput(extendedEnter.Input) == "Num Enter", "step list displays numpad Enter distinctly");
            Check(!StaticCall(typeof(MainForm), "InputKey", K(Keys.Enter)).Equals(StaticCall(typeof(MainForm), "InputKey", extendedEnter.Input)), "main and numpad Enter have independent held state");
            InputSender.UseScanCodeInput = false;
            Check((KeyboardFlags(extendedEnter.Input, false) & 1) != 0 && (KeyboardFlags(K(Keys.Enter), false) & 1) == 0, "virtual key mode preserves numpad extended flag");
            Check((KeyboardFlags(extendedEnter.Input, true) & 3) == 3, "numpad Enter release preserves extended flag");
            InputSender.UseScanCodeInput = true;
            Check((KeyboardFlags(extendedEnter.Input, false) & 9) == 9, "scan code mode preserves numpad extended flag");
            TriggerSpec reopened = Trigger(VirtualKeyboardDialog.InputsFromTrigger(chord).ToArray());
            Check(reopened.Ctrl && reopened.Shift && reopened.VirtualKey == chord.VirtualKey, "existing trigger selection roundtrip");
            TriggerSpec f8 = new TriggerSpec { VirtualKey = (int)Keys.F8 };
            Check(ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(f8) && ModifierSafetyPolicy.TriggerRequiredModifiersMatch(f8, E(Keys.F8, false, true)), "legacy F8 while sprinting still allowed");
            Check(!ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(new TriggerSpec { VirtualKey = (int)Keys.E }), "bare printable triggers remain strict");
            Check(ModifierSafetyPolicy.TriggerMatchesExactly(new TriggerSpec { Kind = InputKind.MouseX1, Ctrl = true, VirtualKey = 0 }, new InputEventInfo { Input = new InputSpec { Kind = InputKind.MouseX1, VirtualKey = 0 }, Ctrl = true }), "legacy Ctrl mouse chord");

            InputSpec[] inputs = { K(Keys.A), K(Keys.LShiftKey), K(Keys.B), K(Keys.LControlKey) };
            MacroStep template = new MacroStep { HoldMs = 73, DelayMs = 91, RandomDelay = true, RandomDelayMinMs = 88, RandomDelayMaxMs = 99 };
            List<MacroStep> steps = VirtualKeyboardDialog.BuildChordSteps(inputs, template);
            Keys[] expectedKeys = { Keys.LShiftKey, Keys.LControlKey, Keys.A, Keys.B, Keys.A, Keys.LControlKey, Keys.LShiftKey };
            MacroAction[] expectedActions = { MacroAction.Down, MacroAction.Down, MacroAction.Down, MacroAction.Press, MacroAction.Up, MacroAction.Up, MacroAction.Up };
            Check(steps.Count == 7, "four-key Press expands to seven balanced steps");
            for (int i = 0; i < steps.Count; i++)
            {
                Check(steps[i].VirtualKey == (int)expectedKeys[i] && steps[i].Action == expectedActions[i], "chord hold/release order " + i);
                Check(steps[i].HoldMs == 73 && (i == steps.Count - 1 ? steps[i].DelayMs == 91 && steps[i].RandomDelay && steps[i].RandomDelayMinMs == 88 && steps[i].RandomDelayMaxMs == 99 : steps[i].DelayMs == 0 && !steps[i].RandomDelay), "timing applies once after complete chord " + i);
            }
            template.Action = MacroAction.Down;
            steps = VirtualKeyboardDialog.BuildChordSteps(inputs, template);
            Check(steps.Count == 4 && steps[0].VirtualKey == (int)Keys.LShiftKey && steps[3].VirtualKey == (int)Keys.B && steps.TrueForAll(delegate(MacroStep x) { return x.Action == MacroAction.Down; }), "Down holds modifiers before ordinary keys");
            template.Action = MacroAction.Up;
            steps = VirtualKeyboardDialog.BuildChordSteps(inputs, template);
            Check(steps.Count == 4 && steps[0].VirtualKey == (int)Keys.B && steps[3].VirtualKey == (int)Keys.LShiftKey && steps.TrueForAll(delegate(MacroStep x) { return x.Action == MacroAction.Up; }), "Up releases in reverse order");

            using (VirtualKeyboardDialog dialog = new VirtualKeyboardDialog(false, null))
            {
                List<Button> buttons = (List<Button>)Field(typeof(VirtualKeyboardDialog), "keyButtons").GetValue(dialog);
                Check(buttons.Count == 104, "full standard 104-key layout");
                HashSet<string> identities = new HashSet<string>();
                foreach (Button b in buttons)
                {
                    InputSpec key = (InputSpec)b.Tag;
                    Check(identities.Add(key.VirtualKey + ":" + key.Extended), "unique keyboard key " + b.Text.Replace('\n', ' '));
                }
                Button a = buttons.Find(delegate(Button b) { return ((InputSpec)b.Tag).VirtualKey == (int)Keys.A; });
                Button bkey = buttons.Find(delegate(Button b) { return ((InputSpec)b.Tag).VirtualKey == (int)Keys.B; });
                MethodInfo click = typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
                click.Invoke(a, new object[] { EventArgs.Empty });
                click.Invoke(bkey, new object[] { EventArgs.Empty });
                List<InputSpec> selected = (List<InputSpec>)Field(typeof(VirtualKeyboardDialog), "selected").GetValue(dialog);
                Check(selected.Count == 1 && selected[0].VirtualKey == (int)Keys.B, "single mode replaces previous selection");
                ((RadioButton)Field(typeof(VirtualKeyboardDialog), "multipleButton").GetValue(dialog)).Checked = true;
                click.Invoke(a, new object[] { EventArgs.Empty });
                Check(selected.Count == 2, "multiple mode preserves previous selection");
                click.Invoke(a, new object[] { EventArgs.Empty });
                Check(selected.Count == 1, "click selected key deselects it");
                click.Invoke(a, new object[] { EventArgs.Empty });
                ((RadioButton)Field(typeof(VirtualKeyboardDialog), "singleButton").GetValue(dialog)).Checked = true;
                Check(selected.Count == 1, "switching to single truncates chord");
                dialog.CreateControl(); dialog.PerformLayout();
                foreach (Button key in buttons)
                    Check(key.Width > 0 && key.Height > 0 && key.Right <= key.Parent.ClientSize.Width && key.Bottom <= key.Parent.ClientSize.Height, "keyboard bounds " + key.Text.Replace('\n', ' '));
            }
            RenderKeyboard("zh-CN", false);
            RenderKeyboard("en-US", false);
            RenderKeyboard("en-US", true);

            // Do not construct MainForm: its constructor installs hooks and starts device services.
            // Invoke only capture logic on an uninitialized instance, with no window handle.
            // BeginInvoke cannot run: no macro, saved configuration, or physical input is emitted.
            MainForm main = (MainForm)FormatterServices.GetUninitializedObject(typeof(MainForm));
            MacroConfig config = new MacroConfig();
            MacroDefinition macro = new MacroDefinition(); config.Macros.Add(macro);
            Field(typeof(MainForm), "config").SetValue(main, config);
            using (ListBox list = new ListBox())
            {
                list.Items.Add("test"); list.SelectedIndex = 0;
                Field(typeof(MainForm), "macroList").SetValue(main, list);
                FieldInfo mode = Field(typeof(MainForm), "captureMode");
                mode.SetValue(main, Enum.Parse(mode.FieldType, "Trigger"));
                Call(main, "HandleTerminalInput", E(Keys.LControlKey, true, false));
                Check(macro.Trigger.VirtualKey == (int)Keys.F8, "physical Ctrl defers capture awaiting main key");
                Call(main, "HandleTerminalInput", E(Keys.K, true, false));
                Check(macro.Trigger.Ctrl && macro.Trigger.VirtualKey == (int)Keys.K, "physical Ctrl then K captures traditional chord");
                foreach (Keys modifierKey in new[] { Keys.LControlKey, Keys.RControlKey, Keys.LShiftKey, Keys.RShiftKey,
                    Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin })
                {
                    int mask = ModifierSafetyPolicy.ModifierMaskForKey((int)modifierKey);
                    Call(main, "HandleTerminalInput", E(modifierKey,
                        (mask & ModifierSafetyPolicy.Ctrl) != 0, (mask & ModifierSafetyPolicy.Shift) != 0,
                        (mask & ModifierSafetyPolicy.Alt) != 0, (mask & ModifierSafetyPolicy.Win) != 0));
                    Call(main, "HandleTerminalInputReleased", E(modifierKey, false, false, false, false));
                    Check(macro.Trigger.VirtualKey == (int)modifierKey && !macro.Trigger.Ctrl && !macro.Trigger.Shift &&
                        !macro.Trigger.Alt && !macro.Trigger.Win, "physical standalone modifier captured on release " + modifierKey);
                }
                Field(typeof(MainForm), "macroList").SetValue(main, null);
            }
            Console.WriteLine("SUCCESS: " + checks + " keyboard checks; no hooks, physical input, macro workers, or config writes.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
