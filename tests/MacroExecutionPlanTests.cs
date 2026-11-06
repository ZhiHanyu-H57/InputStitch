using System;
using System.Collections.Generic;
using InputStitch;

internal static class MacroExecutionPlanTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }

    private sealed class Observer : IOutputBackend
    {
        internal InputSpec LastDown;
        internal int Downs, Ups;
        public void SendDown(InputSpec input) { LastDown = input; Downs++; }
        public void SendUp(InputSpec input) { Ups++; }
        public void NeutralizeGamepad() { }
    }

    private static bool Natural(params MacroStep[] steps)
    { return new MacroExecutionPlan(steps).HasNaturalDelay; }

    private static void MappingAndIdentity()
    {
        foreach (InputKind kind in Enum.GetValues(typeof(InputKind)))
        foreach (MacroAction action in Enum.GetValues(typeof(MacroAction)))
        {
            MacroStep original = new MacroStep { Kind=kind, Action=action, VirtualKey=65, ScanCode=30, Extended=true,
                GamepadControl=GamepadControl.RightStick, GamepadX=-31, GamepadY=78, GamepadValue=64,
                HoldMs=13, DelayMs=19, RandomDelay=true, RandomDelayMinMs=4, RandomDelayMaxMs=9 };
            MacroExecutionPlan plan = new MacroExecutionPlan(new[] { original });
            PreparedMacroStep step = plan[0];
            Check(plan.Count == 1 && !ReferenceEquals(step.Definition,original), "plan owns a detached step " + kind + "/" + action);
            Check(step.Input.Kind==kind && step.Input.VirtualKey==65 && step.Input.ScanCode==30 && step.Input.Extended,
                "captured key/mouse fields survive preparation " + kind);
            Check(step.Input.GamepadControl==GamepadControl.RightStick && step.Input.GamepadX==-31 && step.Input.GamepadY==78 && step.Input.GamepadValue==64,
                "gamepad control, direction and value survive preparation " + kind);
            Check(step.Definition.Action==action && step.Definition.HoldMs==13 && step.Definition.DelayMs==19 && step.Definition.RandomDelay &&
                step.Definition.RandomDelayMinMs==4 && step.Definition.RandomDelayMaxMs==9,"action and timing metadata are unchanged");
            Check(step.Holdable == (kind!=InputKind.WheelUp && kind!=InputKind.WheelDown), "wheel edges keep non-held behavior " + kind);
            string expectedKey = kind==InputKind.Gamepad ? "8:" + ((int)GamepadControl.RightStick).ToString() : ((int)kind).ToString() + ":65:30:True";
            Check(step.Key==expectedKey,"input ownership identity remains compatible " + kind);
            Check(ReferenceEquals(plan[0],step) && ReferenceEquals(plan[0].Input,step.Input) && ReferenceEquals(plan[0].Key,step.Key),
                "repeated accesses reuse the private prepared values");
            original.Kind=InputKind.MouseLeft; original.VirtualKey=90; original.GamepadX=100; original.HoldMs=999;
            Check(step.Input.Kind==kind && step.Input.VirtualKey==65 && step.Input.GamepadX==-31 && step.Definition.HoldMs==13,
                "later editor changes do not modify the run's prepared input or timing");
        }
        MacroStep key = new MacroStep { Kind=InputKind.Keyboard, VirtualKey=65, ScanCode=30, Extended=false };
        string keyIdentity = new MacroExecutionPlan(new[] { key })[0].Key;
        key.Extended=true;
        Check(keyIdentity!=new MacroExecutionPlan(new[] { key })[0].Key,"extended identity stays distinct");
        key.Extended=false; key.ScanCode=31;
        Check(keyIdentity!=new MacroExecutionPlan(new[] { key })[0].Key,"scan-code identity stays distinct");
        MacroStep axis = new MacroStep { Kind=InputKind.Gamepad, GamepadControl=GamepadControl.LeftStick, GamepadX=-100 };
        string axisIdentity = new MacroExecutionPlan(new[] { axis })[0].Key;
        axis.GamepadX=100;
        Check(axisIdentity==new MacroExecutionPlan(new[] { axis })[0].Key,"different axis values address the same ownership control");
        Check(MacroExecutionPlan.ConvertInput(null)==null,"shared converter preserves the existing null-step result");
    }

    private static void DelayClassification()
    {
        Check(!Natural(),"empty plan has no natural delay");
        Check(!Natural(new MacroStep { Action=MacroAction.Down, DelayMs=0, HoldMs=300 }),"Down does not wait its unused HoldMs");
        Check(!Natural(new MacroStep { Action=MacroAction.Up, DelayMs=-1, HoldMs=300 }),"Up/negative fixed delay keeps the no-delay classification");
        Check(Natural(new MacroStep { Action=MacroAction.Press, DelayMs=0, HoldMs=1 }),"Press hold counts as natural delay");
        Check(!Natural(new MacroStep { Action=MacroAction.Press, DelayMs=0, HoldMs=0 }),"zero hold and fixed delay has no natural delay");
        Check(Natural(new MacroStep { Action=MacroAction.Down, DelayMs=1 }),"positive fixed post delay counts");
        Check(Natural(new MacroStep { Action=MacroAction.Down, RandomDelay=true, RandomDelayMinMs=9, RandomDelayMaxMs=2 }),"reversed random range retains positive natural delay");
        Check(!Natural(new MacroStep { Action=MacroAction.Down, DelayMs=99, RandomDelay=true, RandomDelayMinMs=-2, RandomDelayMaxMs=-1 }),"random timing ignores unused fixed delay");
        Check(Natural(new MacroStep { Action=MacroAction.Down, DelayMs=0 },new MacroStep { Action=MacroAction.Down, DelayMs=2 }),"any delayed step marks the plan");
        foreach (InputKind wheel in new[] { InputKind.WheelUp, InputKind.WheelDown })
        foreach (MacroAction action in Enum.GetValues(typeof(MacroAction)))
        {
            Check(!Natural(new MacroStep { Kind=wheel, Action=action, HoldMs=999, DelayMs=0 }),"unused wheel hold does not bypass no-delay throttling " + wheel + "/" + action);
            Check(Natural(new MacroStep { Kind=wheel, Action=action, HoldMs=999, DelayMs=1 }),"explicit wheel delay still counts " + wheel + "/" + action);
            Check(Natural(new MacroStep { Kind=wheel, Action=action, HoldMs=999, DelayMs=0, RandomDelay=true, RandomDelayMinMs=5, RandomDelayMaxMs=0 }),"wheel random delay still counts " + wheel + "/" + action);
            Check(!Natural(new MacroStep { Kind=wheel, Action=action, HoldMs=999, DelayMs=99, RandomDelay=true, RandomDelayMinMs=0, RandomDelayMaxMs=0 }),"zero wheel random delay ignores unused fixed/hold values " + wheel + "/" + action);
        }
    }

    private static void RunOwnership()
    {
        MacroStep original = new MacroStep { Kind=InputKind.Gamepad, Action=MacroAction.Down, GamepadControl=GamepadControl.LeftStick, GamepadX=-45, GamepadY=67 };
        List<MacroStep> definitions = new List<MacroStep> { original };
        MacroExecutionPlan first = new MacroExecutionPlan(definitions), second = new MacroExecutionPlan(definitions);
        Check(!ReferenceEquals(first[0].Definition,second[0].Definition) && !ReferenceEquals(first[0].Input,second[0].Input),"plans are per-run rather than shared mutable caches");
        definitions.Clear();
        Check(first.Count==1 && first[0].Input.GamepadX==-45,"source-list editing does not change the plan");
        Observer observer = new Observer(); OutputOwnershipManager ownership = new OutputOwnershipManager(observer);
        ownership.SetDown("plan-run",first[0].Input);
        Check(observer.Downs==1 && !ReferenceEquals(observer.LastDown,first[0].Input),"ownership sends its own copy of the cached input");
        observer.LastDown.GamepadX=99;
        Check(first[0].Input.GamepadX==-45,"backend changes cannot mutate the plan cache");
        ownership.SetUp("plan-run",first[0].Input);
        Check(observer.Ups==1 && ownership.ActiveSourceCount==0,"cached input retains matching release identity");
        Check(new MacroExecutionPlan(new MacroStep[0]).Count==0,"empty plans preserve their step count");
        bool nullList=false, nullStep=false;
        try { new MacroExecutionPlan(null); } catch(ArgumentNullException) { nullList=true; }
        try { new MacroExecutionPlan(new MacroStep[] { null }); } catch(ArgumentNullException) { nullStep=true; }
        Check(nullList && nullStep,"invalid preparation fails before publishing a partial plan");
    }

    public static int Main()
    {
        MappingAndIdentity(); DelayClassification(); RunOwnership();
        Console.WriteLine("PASS execution plan: "+checks+" checks; detached per-run preparation, compatible input identity/timing and observer-only ownership; no UI, hooks, output or configuration.");
        return 0;
    }
}
