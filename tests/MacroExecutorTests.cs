using System;
using System.Collections.Generic;
using System.Threading;
using InputStitch;

// Console-only executor checks. No Form, message loop, native input or configuration.
internal static class MacroExecutorTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }

    private sealed class Edge
    {
        internal bool Down;
        internal InputSpec Input;
    }
    private sealed class Observer : IOutputBackend
    {
        internal readonly List<Edge> Edges = new List<Edge>();
        internal Action<bool, InputSpec> OnWrite;
        public void SendDown(InputSpec input) { Write(true,input); }
        public void SendUp(InputSpec input) { Write(false,input); }
        private void Write(bool down, InputSpec input)
        {
            Edges.Add(new Edge { Down=down, Input=input.Clone() });
            if (OnWrite!=null) OnWrite(down,input);
        }
        public void NeutralizeGamepad() { }
        internal int Downs { get { return Edges.FindAll(x=>x.Down).Count; } }
        internal int Ups { get { return Edges.Count-Downs; } }
    }
    private sealed class Environment : IMacroExecutionEnvironment
    {
        internal readonly List<string> Calls = new List<string>();
        internal readonly List<int> Waits = new List<int>();
        internal Action<MacroExecutionContext,int> OnWait;
        internal Action<MacroExecutionContext,MacroStep> OnDanger;
        internal Action<MacroExecutionContext,int> OnSafety;
        internal Action<MacroExecutionContext,int,int> OnAdvance;
        internal bool CancelRelease;
        internal int SafetyCalls, AdvanceCalls;
        private static bool Stopped(MacroExecutionContext context) { return context.Run.Stop.IsSet; }
        public bool WaitForTriggerRelease(MacroExecutionContext context, TriggerSpec trigger)
        { Calls.Add("release"); if(CancelRelease) context.Run.Stop.Set(); return Stopped(context); }
        public bool WaitForUiSafetyClear(MacroExecutionContext context)
        { Calls.Add("safety"); SafetyCalls++; if(OnSafety!=null) OnSafety(context,SafetyCalls); return Stopped(context); }
        public bool WaitOrStopWithUiSafety(MacroExecutionContext context, int ms)
        { Calls.Add("wait:"+ms); Waits.Add(ms); if(OnWait!=null) OnWait(context,ms); return Stopped(context); }
        public bool WaitForDangerousShortcutClear(MacroExecutionContext context, MacroStep step)
        { Calls.Add("physical"); if(OnDanger!=null) OnDanger(context,step); return Stopped(context); }
        public bool WaitForSingleStepAdvance(MacroExecutionContext context, int completed, int total)
        { Calls.Add("advance:"+completed+"/"+total); AdvanceCalls++; if(OnAdvance!=null) OnAdvance(context,completed,total); return Stopped(context); }
        public void ReportStatus(MacroExecutionContext context, MacroExecutionPhase phase, int value)
        { Calls.Add("status:"+phase+":"+value); }
    }
    private sealed class Rig : IDisposable
    {
        internal readonly Observer Backend;
        internal readonly Environment Host;
        internal readonly OutputOwnershipManager Ownership;
        internal readonly MacroRunCoordinator Coordinator;
        internal readonly MacroRunRuntime Run;
        internal readonly MacroExecutionContext Context;
        internal readonly MacroExecutor Executor;
        private bool ended;
        internal Rig(MacroDefinition macro, bool singleStep=false)
        {
            Backend=new Observer(); Host=new Environment(); Ownership=new OutputOwnershipManager(Backend);
            Coordinator=new MacroRunCoordinator(new object(),null);
            Run=MakeRun(macro,singleStep); Check(Coordinator.TryRegister(Run),"console fixture registers a run");
            Context=new MacroExecutionContext(Run); Executor=new MacroExecutor(Coordinator,Ownership,new RuntimeTrace(),Host);
        }
        internal void Execute(int delay=0, TriggerSpec release=null) { Executor.Execute(Context,delay,release); }
        internal void End()
        {
            if(ended) return; ended=true;
            try { Ownership.ClearSource(Run.SourceId); }
            finally
            {
                Context.Held.Clear(); Coordinator.UpdateHeldInputs(Run,Context.Held);
                Coordinator.Finish(Run,null); Run.Stop.Dispose(); if(Run.StepGate!=null) Run.StepGate.Dispose();
            }
        }
        public void Dispose() { End(); }
    }
    private static MacroRunRuntime MakeRun(MacroDefinition macro, bool single)
    {
        return new MacroRunRuntime { OwnerMacro=macro, Snapshot=macro.Clone(), Stop=new ManualResetEventSlim(false),
            SingleStep=single, StepGate=single?new AutoResetEvent(false):null };
    }
    private static MacroDefinition Macro(params MacroStep[] steps)
    {
        MacroDefinition macro=new MacroDefinition {Name="console executor",Enabled=true,Infinite=false,RepeatCount=1};
        macro.Steps.AddRange(steps); return macro;
    }
    private static MacroStep Key(MacroAction action, int key=65, int hold=0, int delay=0)
    { return new MacroStep {Kind=InputKind.Keyboard,VirtualKey=key,Action=action,HoldMs=hold,DelayMs=delay}; }
    private static MacroStep Pad(MacroAction action, int x, int y=0)
    { return new MacroStep {Kind=InputKind.Gamepad,GamepadControl=GamepadControl.LeftStick,Action=action,GamepadX=x,GamepadY=y,HoldMs=0,DelayMs=0}; }

    private static void FiniteAndOrdering()
    {
        MacroDefinition macro=Macro(Key(MacroAction.Press,65,7,11)); macro.RepeatCount=3;
        using(Rig rig=new Rig(macro))
        {
            rig.Execute(23,new TriggerSpec());
            Check(rig.Backend.Downs==3 && rig.Backend.Ups==3,"finite repeats preserve press pairs");
            Check(string.Join(",",rig.Host.Waits.ToArray())=="23,7,11,7,11,7,11","release/start delay/hold/post-delay ordering");
            Check(rig.Host.Calls[0]=="status:WaitingForRelease:0" && rig.Host.Calls[1]=="release" && rig.Host.Calls[2]=="status:StartDelay:23","startup status and physical release precede execution");
            Check(rig.Host.SafetyCalls==4,"initial and every step retain their safety check");
            MacroRunProgressSnapshot progress=rig.Coordinator.ReadProgress()[0];
            Check(progress.Iteration==3 && progress.StepIndex==1 && progress.StepCount==1,"coordinator receives executed progress");
            Check(rig.Context.OutputObserved && rig.Context.Held.Count==0,"successful execution reports output without residual per-run holding");
            rig.End(); Check(rig.Coordinator.Count==0 && rig.Ownership.Snapshot().Sources.Count==0,"host completion clears registration and source");
        }
        macro.RepeatCount=0;
        using(Rig rig=new Rig(macro)) { rig.Execute(); Check(rig.Backend.Downs==1,"nonpositive repeat count remains one execution"); }
    }

    private static void CancelAtEveryBoundary()
    {
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press))))
        { rig.Host.CancelRelease=true; rig.Execute(20,new TriggerSpec()); Check(rig.Backend.Edges.Count==0 && rig.Host.Waits.Count==0,"release cancellation prevents start delay and output"); }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press))))
        { rig.Host.OnWait=(c,ms)=>c.Run.Stop.Set(); rig.Execute(20); Check(rig.Backend.Edges.Count==0 && rig.Host.SafetyCalls==0,"start delay cancellation prevents loop entry"); }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press))))
        { rig.Host.OnSafety=(c,n)=>c.Run.Stop.Set(); rig.Execute(); Check(rig.Backend.Edges.Count==0,"initial editing protection may cancel before any output"); }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Down),Key(MacroAction.Press,66))))
        {
            rig.Host.OnSafety=(c,n)=>{if(n==3)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==1 && rig.Backend.Ups==0,"per-step protection stops later keys with earlier held state intact");
            rig.End(); Check(rig.Backend.Ups==1 && rig.Ownership.ActiveSourceCount==0,"host finally releases a protection-cancelled hold");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press,65,19),Key(MacroAction.Press,66))))
        {
            rig.Host.OnWait=(c,ms)=>{if(ms==19)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==1 && rig.Backend.Ups==0,"stop during a Press hold prevents normal release and later steps");
            rig.End(); Check(rig.Backend.Ups==1,"host finally supplies the cancelled Press release");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press,65,0,31),Key(MacroAction.Press,66))))
        {
            rig.Host.OnWait=(c,ms)=>{if(ms==31)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==1 && rig.Backend.Ups==1,"post-delay cancellation retains completed pair and skips next step");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Down),Key(MacroAction.Press,66))))
        {
            rig.Host.OnDanger=(c,step)=>{if(step.VirtualKey==66)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==1,"dangerous physical shortcut gate remains before output");
            rig.End(); Check(rig.Backend.Ups==1,"physical gate cancellation is releasable by host cleanup");
        }
    }

    private static void SingleStepAndInfinite()
    {
        MacroDefinition macro=Macro(Key(MacroAction.Press,65,5,90),Key(MacroAction.Press,66,6,91)); macro.Infinite=true; macro.RepeatCount=4;
        using(Rig rig=new Rig(macro,true))
        {
            rig.Run.SingleStep=false;
            Check(rig.Context.SingleStep,"execution captures single-step mode before the loop starts");
            rig.Host.OnAdvance=(c,done,total)=>Check(done==1 && total==2,"single-step gate receives completed and total counts");
            rig.Execute();
            Check(!rig.Context.Infinite && rig.Context.RepeatCount==1 && rig.Backend.Downs==2,"single-step overrides repeat/infinite settings");
            Check(rig.Host.AdvanceCalls==1 && string.Join(",",rig.Host.Waits.ToArray())=="5,6","single-step waits only holds, not ordinary post-delays");
        }
        using(Rig rig=new Rig(macro,true))
        { rig.Host.OnAdvance=(c,a,b)=>c.Run.Stop.Set(); rig.Execute(); Check(rig.Backend.Downs==1 && rig.Backend.Ups==1,"single-step cancellation prevents the next step"); }
        macro=Macro(new MacroStep {Kind=InputKind.WheelUp,Action=MacroAction.Press,HoldMs=999,DelayMs=0}); macro.Infinite=true;
        using(Rig rig=new Rig(macro))
        {
            int pace=0; rig.Host.OnWait=(c,ms)=>{if(ms==1 && ++pace==3)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==3 && rig.Backend.Ups==0,"infinite wheel remains edge-only and stoppable");
            Check(string.Join(",",rig.Host.Waits.ToArray())=="0,1,0,1,0,1","unused wheel hold cannot remove infinite-loop pacing");
        }
        macro=Macro(Pad(MacroAction.Down,20)); macro.Infinite=true;
        using(Rig rig=new Rig(macro))
        {
            rig.Host.OnWait=(c,ms)=>{if(ms==15)c.Run.Stop.Set();}; rig.Execute();
            Check(rig.Backend.Downs==1 && string.Join(",",rig.Host.Waits.ToArray())=="0,1,0,15","unchanged gamepad state preserves idle-loop pacing");
            rig.End(); Check(rig.Backend.Ups==1,"infinite held state is released by host cleanup");
        }
    }

    private static void HeldStateAndIsolation()
    {
        using(Rig rig=new Rig(Macro(Key(MacroAction.Down),Key(MacroAction.Press,65,17),Key(MacroAction.Up))))
        {
            rig.Execute();
            Check(rig.Backend.Downs==1 && rig.Backend.Ups==1,"same-source Down/Press/Up does not bounce an already-held key");
            Check(string.Join(",",rig.Host.Waits.ToArray())=="0,17,0,0","already-held Press still consumes its hold time");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press,65,17))))
        {
            rig.Ownership.SetDown("other",new InputSpec {VirtualKey=65});
            rig.Execute(); rig.End();
            Check(rig.Backend.Downs==1 && rig.Backend.Ups==0 && rig.Ownership.ActiveSourceCount==1,"run completion cannot release another source's key");
            rig.Ownership.ClearSource("other"); Check(rig.Backend.Ups==1,"last source releases shared key once");
        }
        using(Rig rig=new Rig(Macro(Pad(MacroAction.Down,20,30),Pad(MacroAction.Down,20,30),Pad(MacroAction.Down,40,30),Pad(MacroAction.Up,40,30))))
        {
            rig.Execute();
            Check(rig.Backend.Downs==2 && rig.Backend.Ups==1,"duplicate gamepad state is skipped but changed state is emitted");
            Check(rig.Backend.Edges[0].Input.GamepadX==20 && rig.Backend.Edges[1].Input.GamepadX==40,"axis state changes retain exact prepared values");
        }
        // Reentrant execution exercises independent contexts against shared coordinator/ownership.
        Observer backend=new Observer(); OutputOwnershipManager ownership=new OutputOwnershipManager(backend);
        MacroRunCoordinator coordinator=new MacroRunCoordinator(new object(),null); Environment host=new Environment();
        MacroRunRuntime a=MakeRun(Macro(Key(MacroAction.Press,65,10)),false), b=MakeRun(Macro(Key(MacroAction.Press,65,20)),false);
        Check(coordinator.TryRegister(a) && coordinator.TryRegister(b),"two console runs receive independent identities");
        MacroExecutionContext ca=new MacroExecutionContext(a), cb=new MacroExecutionContext(b);
        MacroExecutor executor=new MacroExecutor(coordinator,ownership,new RuntimeTrace(),host);
        host.OnWait=(c,ms)=>
        {
            if(ReferenceEquals(c,ca) && ms==10)
            {
                executor.Execute(cb,0,null);
                Check(backend.Downs==1 && backend.Ups==0,"nested run does not pulse or release the first owner's key");
            }
            if(ReferenceEquals(c,cb) && ms==20)
            {
                string key=MacroExecutionPlan.KeyForInput(new InputSpec {VirtualKey=65});
                Check(!ReferenceEquals(ca.Held[key],cb.Held[key]),"different runs do not share prepared input instances");
            }
        };
        try
        {
            executor.Execute(ca,0,null);
            Check(backend.Downs==1 && backend.Ups==1,"reentrant independent contexts preserve merged press state");
        }
        finally
        {
            ownership.ClearSource(a.SourceId); ownership.ClearSource(b.SourceId);
            coordinator.Finish(a,null);coordinator.Finish(b,null);a.Stop.Dispose();b.Stop.Dispose();
        }
    }

    private static bool Same(InputSpec input, MacroStep original)
    {
        return input.Kind==original.Kind && input.VirtualKey==original.VirtualKey && input.ScanCode==original.ScanCode && input.Extended==original.Extended &&
            input.GamepadControl==original.GamepadControl && input.GamepadX==original.GamepadX && input.GamepadY==original.GamepadY && input.GamepadValue==original.GamepadValue;
    }
    private static void CachedInputCopies()
    {
        foreach(InputKind kind in Enum.GetValues(typeof(InputKind)))
        {
            MacroStep original=new MacroStep {Kind=kind,VirtualKey=65,ScanCode=30,Extended=true,GamepadControl=GamepadControl.RightStick,
                GamepadX=-31,GamepadY=78,GamepadValue=64,Action=MacroAction.Press,HoldMs=13,DelayMs=0};
            MacroStep expected=original.Clone(); MacroDefinition macro=Macro(original);macro.RepeatCount=2;
            using(Rig rig=new Rig(macro))
            {
                int observedHolds=0;
                rig.Backend.OnWrite=(down,input)=>{input.VirtualKey=90;input.GamepadX=99;};
                rig.Host.OnDanger=(c,step)=>Check(step.Kind==expected.Kind && step.VirtualKey==65 && step.GamepadX==-31,"prepared definition stays detached across repeated output "+kind);
                rig.Host.OnWait=(c,ms)=>
                {
                    if(ms!=13) return;
                    observedHolds++;
                    string key=MacroExecutionPlan.KeyForInput(MacroExecutionPlan.ConvertInput(expected));
                    Check(Same(c.Held[key],expected),"all cached input fields survive backend changes "+kind);
                    MacroRunProgressSnapshot progress=rig.Coordinator.ReadProgress()[0];
                    progress.HeldInputs[0].VirtualKey=91;progress.HeldInputs[0].GamepadY=99;
                    OutputOwnershipSnapshot output=rig.Ownership.Snapshot();output.Merged[0].VirtualKey=92;
                    original.VirtualKey=93;rig.Run.Snapshot.Steps[0].VirtualKey=94;
                    Check(Same(c.Held[key],expected),"config and diagnostic snapshots cannot mutate cached held input "+kind);
                };
                rig.Execute();
                Check(rig.Backend.Downs==2,"repeated prepared input executes after external copy edits "+kind);
                Check(observedHolds==(kind==InputKind.WheelUp || kind==InputKind.WheelDown?0:2),"hold waits follow each input kind "+kind);
            }
        }
    }

    private static void ExceptionsAndRandomWaits()
    {
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press,65,17))))
        {
            rig.Host.OnWait=(c,ms)=>{throw new InvalidOperationException("host wait failure");};bool failed=false;
            try{rig.Execute();}catch(InvalidOperationException){failed=true;}
            Check(failed && rig.Context.Held.Count==1 && rig.Context.OutputObserved,"host error propagates with releasable execution state");
            rig.End();Check(rig.Backend.Downs==rig.Backend.Ups && rig.Coordinator.Count==0 && rig.Ownership.ActiveSourceCount==0,"caller finally releases and finishes an errored execution");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press))))
        {
            rig.Run.Snapshot.Steps.Add(null);bool failed=false;
            try{rig.Execute();}catch(ArgumentNullException){failed=true;}
            Check(failed && rig.Backend.Edges.Count==0,"invalid plan fails before output");
            rig.End();Check(rig.Coordinator.Count==0,"caller can finish preparation failures");
        }
        using(Rig rig=new Rig(Macro(Key(MacroAction.Press))))
        {
            rig.Backend.OnWrite=(down,input)=>{if(down)throw new InvalidOperationException("output failure");};bool failed=false;
            try{rig.Execute();}catch(OutputOwnershipException){failed=true;}
            Check(failed,"ownership failure propagates for host global recovery");
            rig.Backend.OnWrite=null;rig.End();Check(rig.Ownership.ActiveSourceCount==0 && rig.Coordinator.Count==0,"ownership failure remains cleanable");
        }
        Random random=new Random(42);
        Check(MacroExecutor.ResolveStepDelay(null,random)==0,"null delay step keeps its existing zero result");
        Check(MacroExecutor.ResolveStepDelay(new MacroStep {DelayMs=-1},random)==0,"negative fixed delay clamps to zero");
        MacroStep step=new MacroStep {RandomDelay=true,RandomDelayMinMs=9,RandomDelayMaxMs=2};
        bool sawLow=false,sawHigh=false;
        for(int i=0;i<200;i++)
        {
            int delay=MacroExecutor.ResolveStepDelay(step,random);
            Check(delay>=2 && delay<=9,"random delay keeps inclusive reordered range");sawLow|=delay==2;sawHigh|=delay==9;
        }
        Check(sawLow && sawHigh,"random delay reaches both range endpoints");
        step.RandomDelayMinMs=-5;step.RandomDelayMaxMs=-1;
        Check(MacroExecutor.ResolveStepDelay(step,random)==0,"negative random range clamps to zero");
    }

    public static int Main()
    {
        try
        {
            FiniteAndOrdering();CancelAtEveryBoundary();SingleStepAndInfinite();HeldStateAndIsolation();CachedInputCopies();ExceptionsAndRandomWaits();
            Console.WriteLine("PASS macro executor: "+checks+" checks; console-only finite/infinite/single-step, safety/stop, ownership/copy isolation and failure cleanup; no Form, hooks, real input or configuration.");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
