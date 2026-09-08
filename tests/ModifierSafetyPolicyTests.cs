using System;
using System.Windows.Forms;
using InputStitch;

internal static class ModifierSafetyPolicyTests
{
    private static int failures;

    private static MacroStep Key(MacroAction action, Keys key)
    {
        MacroStep step = new MacroStep();
        step.Action = action;
        step.Kind = InputKind.Keyboard;
        step.VirtualKey = (int)key;
        return step;
    }

    private static void Expect(string name, int expected, int actual)
    {
        if (expected == actual) Console.WriteLine("PASS " + name);
        else
        {
            failures++;
            Console.WriteLine("FAIL " + name + ": expected=" + expected + ", actual=" + actual);
        }
    }

    private static void Expect(string name, bool expected, bool actual)
    {
        Expect(name, expected ? 1 : 0, actual ? 1 : 0);
    }

    private static TriggerSpec Trigger(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        TriggerSpec trigger = new TriggerSpec();
        trigger.Kind = InputKind.Keyboard;
        trigger.VirtualKey = (int)key;
        trigger.Ctrl = ctrl;
        trigger.Shift = shift;
        trigger.Alt = alt;
        trigger.Win = win;
        return trigger;
    }

    private static InputEventInfo Event(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        InputEventInfo inputEvent = new InputEventInfo();
        inputEvent.Input = new InputSpec();
        inputEvent.Input.Kind = InputKind.Keyboard;
        inputEvent.Input.VirtualKey = (int)key;
        inputEvent.Ctrl = ctrl;
        inputEvent.Shift = shift;
        inputEvent.Alt = alt;
        inputEvent.Win = win;
        return inputEvent;
    }

    public static int Main()
    {
        int ctrl = ModifierSafetyPolicy.Ctrl;
        int shift = ModifierSafetyPolicy.Shift;
        int alt = ModifierSafetyPolicy.Alt;
        int win = ModifierSafetyPolicy.Win;

        Expect("Shift+E is allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.E), shift));
        Expect("Shift+Enter is allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Enter), shift));
        Expect("Shift+mouse is allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(new MacroStep { Action = MacroAction.Press, Kind = InputKind.MouseX1 }, shift));
        Expect("Alt+Enter is blocked", alt, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Enter), alt));
        Expect("Alt+E is allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.E), alt));
        Expect("Alt+F4 is blocked", alt, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Down, Keys.F4), alt));
        Expect("Alt+Tab is blocked", alt, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Tab), alt));
        Expect("Alt+Space is blocked", alt, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Space), alt));
        Expect("Ctrl+Esc is blocked", ctrl, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Escape), ctrl));
        Expect("Ctrl+E is allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.E), ctrl));
        Expect("Ctrl+Alt+Delete is blocked", ctrl | alt, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Delete), ctrl | alt));
        Expect("Win+E is blocked", win, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.E), win));
        Expect("Up is always allowed", 0, ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Up, Keys.Enter), alt | win));

        int macroHeldAlt = alt;
        int physicalNone = 0;
        int intentionalDanger = ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Enter), macroHeldAlt);
        Expect("Macro-authored Alt+Enter is not physically blocked", 0, intentionalDanger & physicalNone);
        int physicalCtrl = ctrl;
        int mixedDanger = ModifierSafetyPolicy.GetDangerousPhysicalModifierMask(Key(MacroAction.Press, Keys.Delete), macroHeldAlt | physicalCtrl);
        Expect("Physical Ctrl joining macro Alt+Delete is blocked", ctrl, mixedDanger & physicalCtrl);

        TriggerSpec bareF8 = Trigger(Keys.F8, false, false, false, false);
        InputEventInfo shiftF8 = Event(Keys.F8, false, true, false, false);
        Expect("Bare F8 is not exact under Shift", false, ModifierSafetyPolicy.TriggerMatchesExactly(bareF8, shiftF8));
        Expect("Bare F8 accepts required-subset fallback", true, ModifierSafetyPolicy.TriggerRequiredModifiersMatch(bareF8, shiftF8));
        Expect("Bare F8 supports extra modifiers", true, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareF8));

        TriggerSpec bareE = Trigger(Keys.E, false, false, false, false);
        InputEventInfo shiftE = Event(Keys.E, false, true, false, false);
        Expect("Bare E does not broadly accept arbitrary modifiers", false, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareE));
        Expect("Bare E is not exact under Shift", false, ModifierSafetyPolicy.TriggerMatchesExactly(bareE, shiftE));
        Expect("Bare E accepts Shift-only gameplay fallback", true, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareE, shiftE));
        InputEventInfo ctrlE = Event(Keys.E, true, false, false, false);
        Expect("Bare E remains strict under Ctrl", false, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareE, ctrlE));
        InputEventInfo shiftCtrlE = Event(Keys.E, true, true, false, false);
        Expect("Bare E Shift fallback does not also admit Ctrl", false, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareE, shiftCtrlE));
        TriggerSpec bareTab = Trigger(Keys.Tab, false, false, false, false);
        InputEventInfo shiftTab = Event(Keys.Tab, false, true, false, false);
        Expect("Bare Tab does not gain Shift fallback", false, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(bareTab, shiftTab));

        TriggerSpec shiftEHold = Trigger(Keys.E, false, true, false, false);
        Expect("Shift+E Hold terminal release ends chord", true,
            ModifierSafetyPolicy.HoldTriggerReleasedByEvent(shiftEHold, Event(Keys.E, false, true, false, false)));
        Expect("Shift+E Hold modifier release ends chord", true,
            ModifierSafetyPolicy.HoldTriggerReleasedByEvent(shiftEHold, Event(Keys.LShiftKey, false, false, false, false)));
        Expect("Shift+E remains held if the other Shift side is still down", false,
            ModifierSafetyPolicy.HoldTriggerReleasedByEvent(shiftEHold, Event(Keys.LShiftKey, false, true, false, false)));
        Expect("Unrelated Ctrl release does not end Shift+E Hold", false,
            ModifierSafetyPolicy.HoldTriggerReleasedByEvent(shiftEHold, Event(Keys.LControlKey, false, true, false, false)));

        TriggerSpec alt9 = Trigger(Keys.D9, false, false, true, false);
        InputEventInfo shiftAlt9Event = Event(Keys.D9, false, true, true, false);
        Expect("Alt+9 accepts extra sprint Shift", true, ModifierSafetyPolicy.TriggerRequiredModifiersMatch(alt9, shiftAlt9Event));
        Expect("Alt+9 supports extra modifiers", true, ModifierSafetyPolicy.SupportsExtraPhysicalModifiers(alt9));

        TriggerSpec shiftAlt9Trigger = Trigger(Keys.D9, false, true, true, false);
        Expect("Exact Shift+Alt+9 remains exact", true, ModifierSafetyPolicy.TriggerMatchesExactly(shiftAlt9Trigger, shiftAlt9Event));
        Expect("More specific trigger scores higher", true, ModifierSafetyPolicy.TriggerSpecificity(shiftAlt9Trigger) > ModifierSafetyPolicy.TriggerSpecificity(alt9));

        TriggerSpec panic = Trigger(Keys.F12, true, true, false, false);
        InputEventInfo panicWithAlt = Event(Keys.F12, true, true, true, false);
        Expect("Emergency stop accepts extra Alt", true, ModifierSafetyPolicy.TriggerRequiredModifiersMatch(panic, panicWithAlt));

        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : failures + " TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }
}
