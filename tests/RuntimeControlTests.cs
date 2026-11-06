using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class RuntimeControlTests
{
    private static int checks;
    private static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    private sealed class Observer : IOutputBackend
    {
        internal int Downs, Ups, Neutralizations;
        internal Action DuringDown;
        public void SendDown(InputSpec input) { if(DuringDown!=null)DuringDown(); Interlocked.Increment(ref Downs); }
        public void SendUp(InputSpec input) { Interlocked.Increment(ref Ups); }
        public void NeutralizeGamepad() { Interlocked.Increment(ref Neutralizations); }
    }
    private sealed class Store : ConfigStore
    {
        internal int Writes, Validations, Replaces;
        internal override void Write(string path, byte[] payload) { Writes++; base.Write(path,payload); }
        internal override void Validate(string path) { Validations++; base.Validate(path); }
        internal override void Replace(string staged,string target,string backup) { Replaces++;base.Replace(staged,target,backup); }
    }
    private sealed class Provider : IGamepadInputProvider
    {
        internal int Reads,Enumerations; internal bool ThrowOnPoll; internal XInputPadState State=new XInputPadState{Connected=true,Buttons=0x1000,ThumbLX=12000};
        public string ProviderName{get{return "audit";}} public long TopologyVersion{get{return 1;}}
        public Action<InputEventInfo> InputDown{get;set;} public Action<InputEventInfo> InputUp{get;set;}
        public void Poll(){if(ThrowOnPoll)throw new InvalidOperationException("test provider fault");} public int[] ConnectedDeviceIndices(){Enumerations++;return new[]{1};}
        public bool TryGetState(int index,out XInputPadState state){Reads++;state=State;return true;}
        public bool IsTriggerSatisfied(TriggerSpec trigger){return false;} public void Dispose(){}
    }
    private static readonly Func<long> Allocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
    private static object Call(MainForm form,string name,params object[] args)
    { return typeof(MainForm).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).Single(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(form,args); }
    private static int Sources(MainForm form) { return ((OutputOwnershipManager)typeof(MainForm).GetField("outputOwnership",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form)).ActiveSourceCount; }
    private static void OnInputThread(Action body)
    {
        Exception error=null; Thread t=new Thread(delegate(){try{body();}catch(Exception ex){error=ex;}}); t.Start();
        if(!t.Join(1500))throw new Exception("input callback blocked");if(error!=null)throw error;
    }
    private static void Pump() { for(int i=0;i<20;i++){Application.DoEvents();Thread.Sleep(5);} }
    private static void Until(Func<bool> ready)
    { Stopwatch c=Stopwatch.StartNew();while(!ready()&&c.ElapsedMilliseconds<1500)Thread.Sleep(2);if(!ready())throw new Exception("output setup failed"); }
    private static InputEventInfo Edge(Keys key) { return new InputEventInfo{Input=new InputSpec{Kind=InputKind.Keyboard,VirtualKey=(int)key}}; }
    private static MacroConfig Config(MacroDefinition macro)
    { MacroConfig c=new MacroConfig{GamepadDeviceType=VirtualGamepadTypes.None,PauseMacroInRiskyUi=false,PanicTrigger=new TriggerSpec{Kind=InputKind.Keyboard,VirtualKey=(int)Keys.F12}};c.Macros.Add(macro);return c; }
    private static MacroDefinition Macro(bool parallel)
    {
        MacroDefinition m=new MacroDefinition{Name="audit",Enabled=true,Infinite=true,RunMode=parallel?TriggerRunMode.Hold:TriggerRunMode.Toggle,Trigger=new TriggerSpec{Kind=InputKind.Keyboard,VirtualKey=(int)Keys.F24}};
        m.Steps.Add(parallel?new MacroStep{Kind=InputKind.Gamepad,GamepadControl=GamepadControl.South,Action=MacroAction.Down,DelayMs=0}:
            new MacroStep{Kind=InputKind.Keyboard,VirtualKey=(int)Keys.F23,Action=MacroAction.Press,HoldMs=10,DelayMs=10});return m;
    }
    private static void StopScenario(string root,string kind)
    {
        bool release=kind.StartsWith("release",StringComparison.Ordinal);
        bool parallel=kind=="panic" || (release && kind!="release-worker");MacroDefinition m=Macro(parallel);
        if(kind=="release-worker")m.RunMode=TriggerRunMode.Hold;
        if(kind=="release-mouse"){m.Trigger.Kind=InputKind.MouseX2;m.Trigger.VirtualKey=0;}
        if(kind=="release-gamepad"){m.Trigger.Kind=InputKind.Gamepad;m.Trigger.GamepadControl=GamepadControl.West;m.Trigger.GamepadUserIndex=1;}MacroConfig c=Config(m);Observer output=new Observer();
        using(MainForm form=new MainForm(c,Path.Combine(root,kind),output))
        {
            IntPtr handle=form.Handle;
            if(parallel)Call(form,"StartParallelHeldMapping",m);else Call(form,"StartMacro",m,0,null,kind=="release-worker");
            Until(()=>output.Downs>0);int before=output.Downs;
            Stopwatch clock=Stopwatch.StartNew();
            OnInputThread(delegate{if(release)Call(form,"HandleTerminalInputReleased",new InputEventInfo{Input=new InputSpec{Kind=m.Trigger.Kind,VirtualKey=m.Trigger.VirtualKey,GamepadControl=m.Trigger.GamepadControl},DeviceIndex=m.Trigger.GamepadUserIndex});else Call(form,"HandleTerminalInput",Edge(kind=="panic"?Keys.F12:Keys.F24));});
            Until(()=>Sources(form)==0);
            if(!parallel)Until(()=>!(bool)Call(form,"HasWorkerBackedRuns"));
            Check(clock.ElapsedMilliseconds<500,kind+" releases before the UI resumes");
            long response=clock.ElapsedMilliseconds;
            int stopped=output.Downs;Thread.Sleep(220);
            Check(output.Downs==stopped && Sources(form)==0,kind+" cannot retain or reassert output during a UI block");
            if(kind=="panic")Check(output.Neutralizations>0,"panic immediately neutralizes held gamepad state");
            Console.WriteLine("OBSERVED "+kind+" response="+response+"ms; UI remained blocked for220ms; downs="+output.Downs+" ups="+output.Ups+" additionalDowns="+(output.Downs-before)+" sources="+Sources(form)+" neutralizations="+output.Neutralizations);
            Pump();if(!parallel)Until(()=>!(bool)Call(form,"HasWorkerBackedRuns"));
            Console.WriteLine("OBSERVED "+kind+" after UI pump: downs="+output.Downs+" ups="+output.Ups+" sources="+Sources(form)+" neutralizations="+output.Neutralizations);
            Call(form,"EmergencyStop","audit-cleanup");
        }
    }
    private static void StaleStart(string root,string kind)
    {
        MacroDefinition m=Macro(false);if(kind=="released")m.RunMode=TriggerRunMode.Hold;m.Infinite=false;m.RepeatCount=1;MacroConfig c=Config(m);Observer output=new Observer();
        using(MainForm form=new MainForm(c,Path.Combine(root,kind),output))
        {
            IntPtr handle=form.Handle;
            OnInputThread(()=>Call(form,"HandleTerminalInput",Edge(Keys.F24)));
            if(kind=="disabled" || kind=="removed" || kind=="edited")Call(form,"StopMacroForDefinitionChange",m,"definition-change");
            if(kind=="disabled" || kind=="disabled-only")m.Enabled=false;
            if(kind=="removed" || kind=="removed-only")c.Macros.Remove(m);
            if(kind=="condition")m.Conditions.Analog=new AnalogZoneCondition{Enabled=true};
            if(kind=="edited")m.Steps[0].VirtualKey=(int)Keys.F21;
            if(kind=="stop-all")Call(form,"StopAllMacroRuns","test-stop");
            if(kind=="stop-selected")Call(form,"StopMacro",m,"test-stop");
            if(kind=="other-stop")Call(form,"StopMacro",Macro(false),"test-stop");
            if(kind=="profile"){Call(form,"StopAllMacroRuns","profile");c.Macros.Clear();c.Macros.Add(m.Clone());}
            if(kind=="released")OnInputThread(()=>Call(form,"HandleTerminalInputReleased",Edge(Keys.F24)));
            Pump();Thread.Sleep(100);Pump();
            Check(output.Downs==((kind=="valid" || kind=="other-stop")?1:0),kind+" only admits a current ordinary trigger");
            MacroRunCoordinator coordinator=(MacroRunCoordinator)typeof(MainForm).GetField("runCoordinator",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            Check(coordinator.PendingTriggerMacrosLocked.Count==0,"pending tickets retire after dispatch/cancellation");
            Console.WriteLine("OBSERVED queued start after "+kind+": downs="+output.Downs+" ups="+output.Ups+" enabled="+m.Enabled+" inConfig="+c.Macros.Contains(m));
            Call(form,"EmergencyStop","audit-cleanup");
        }
    }
    private static void SaveProbe(string root)
    {
        Store store=new Store();MacroConfig c=new MacroConfig{GamepadDeviceType=VirtualGamepadTypes.None};
        for(int i=0;i<10;i++){MacroDefinition m=Macro(false);m.Name="macro"+i;for(int j=1;j<250;j++)m.Steps.Add(m.Steps[0].Clone());c.Macros.Add(m);}
        string path=Path.Combine(root,"same.xml");store.Save(path,c);store.Writes=store.Validations=store.Replaces=0;
        Stopwatch clock=Stopwatch.StartNew();for(int i=0;i<10;i++)store.Save(path,c);clock.Stop();
        Check(store.Writes==0 && store.Validations==0 && store.Replaces==0,"validated identical saves perform no disk write or deserialization");
        Console.WriteLine("OBSERVED10 identical saves: bytes="+new FileInfo(path).Length+" writes="+store.Writes+" validations="+store.Validations+" replaces="+store.Replaces+" elapsedMs="+clock.ElapsedMilliseconds);
    }
    private static void PanicRouterAndLateAdmission(string root)
    {
        MacroDefinition m=Macro(true);MacroConfig config=Config(m);Observer output=new Observer();
        using(MainForm form=new MainForm(config,Path.Combine(root,"panic-router"),output))
        {
            IntPtr handle=form.Handle;
            OutputOwnershipManager ownership=(OutputOwnershipManager)typeof(MainForm).GetField("outputOwnership",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            MacroRunCoordinator coordinator=(MacroRunCoordinator)typeof(MainForm).GetField("runCoordinator",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            MacroTriggerTicket invalid=coordinator.QueueTrigger(m);coordinator.Stop(m,"test-invalidate");
            Call(form,"StartPreparedMacroWithTicket",m,m.Clone(),MacroRuntimeClassifier.Classify(m),0,null,false,false,null,false,invalid);
            Call(form,"StartParallelHeldMappingWithTicket",m,null,invalid);
            Check(coordinator.Count==0 && output.Downs==0,"both worker and held-source commits reject a late invalidated ticket");
            ownership.SetDown("other",new InputSpec{Kind=InputKind.Keyboard,VirtualKey=(int)Keys.F22});
            Call(form,"StartParallelHeldMapping",m);
            OnInputThread(()=>Call(form,"HandleTerminalInputReleased",Edge(Keys.F24)));
            Check(ownership.ActiveSourceCount==1,"terminal release preserves another source");ownership.ClearSource("other");
            Provider input=new Provider();GamepadRouterService router=new GamepadRouterService(ownership);router.Configure(true);router.Tick(input,0);
            typeof(MainForm).GetField("gamepadRouter",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,router);
            OnInputThread(()=>Call(form,"HandleTerminalInput",Edge(Keys.F12)));
            int stopped=output.Downs;for(int i=0;i<10;i++)router.Tick(input,0);
            Check(!router.Enabled && ownership.ActiveSourceCount==0 && output.Downs==stopped,"panic disables routing before UI recovery so unchanged frames cannot reassert output");
            Pump();router.Dispose();
        }
    }
    private static void SelectionProbe(string root)
    {
        MacroConfig c=Config(Macro(false));c.Macros.Clear();
        for(int i=0;i<100;i++){MacroDefinition m=Macro(false);m.Enabled=false;m.Conditions.Analog=new AnalogZoneCondition{Enabled=true};c.Macros.Add(m);}
        using(MainForm form=new MainForm(c,Path.Combine(root,"selection"),new Observer()))
        {
            IntPtr handle=form.Handle;Provider input=new Provider();typeof(MainForm).GetField("gamepadInput",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,input);
            OnInputThread(()=>Call(form,"HandleTerminalInput",Edge(Keys.A)));
            Check(input.Reads==0 && input.Enumerations==0,"unrelated key does not read disabled macro conditions");
            Console.WriteLine("UNRELATED A: reads="+input.Reads+" enumerations="+input.Enumerations);
            c.Macros[0].Enabled=true;
            OnInputThread(()=>Call(form,"HandleTerminalInput",Edge(Keys.F24)));
            Check(input.Reads==1 && input.Enumerations==1,"matching key evaluates enabled candidates only");
            Console.WriteLine("MATCHED F24,1 enabled/99 disabled macros: reads="+input.Reads+" enumerations="+input.Enumerations);
        }
    }
    private static void MissedRelease(string root,bool parallel,bool failPoll)
    {
        MacroDefinition m=Macro(parallel);m.RunMode=TriggerRunMode.Hold;Observer output=new Observer();int physical=1,errors=0;
        using(MainForm form=new MainForm(Config(m),Path.Combine(root,"fallback-"+parallel+failPoll),output))
        {
            IntPtr handle=form.Handle;form.ActivatorPhysicalProbe=delegate{return Volatile.Read(ref physical)!=0;};
            Provider provider=new Provider{ThrowOnPoll=failPoll};typeof(MainForm).GetField("gamepadInput",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,provider);
            if(parallel)Call(form,"StartParallelHeldMapping",m);else Call(form,"StartMacro",m,0,null,true);
            Until(()=>output.Downs>0);
            using(InputPipelineScheduler scheduler=new InputPipelineScheduler(10,delegate{Call(form,"TickGamepadInput");},delegate{Interlocked.Increment(ref errors);}))
            {
                typeof(MainForm).GetField("inputPipelineScheduler",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,scheduler);
                scheduler.Start();Thread.Sleep(70);Check((bool)Call(form,"HasAnyActiveRuntime"),"held input remains active while physically held");
                Stopwatch clock=Stopwatch.StartNew();Volatile.Write(ref physical,0);Until(()=>Sources(form)==0);
                if(!parallel)Until(()=>!(bool)Call(form,"HasWorkerBackedRuns"));
                Check(clock.ElapsedMilliseconds<500,"missed-up physical fallback completes without UI pumping");
                if(failPoll)Check(errors>0,"physical fallback remains live when provider polling fails");
                Console.WriteLine("FALLBACK parallel="+parallel+" pollFault="+failPoll+" responseMs="+clock.ElapsedMilliseconds+" sources="+Sources(form));
            }
            Pump();Call(form,"EmergencyStop","test-cleanup");
        }
    }
    private static void HeldStartPanicRace(string root)
    {
        MacroDefinition macro=Macro(true);Observer output=new Observer();
        using(MainForm form=new MainForm(Config(macro),Path.Combine(root,"held-panic-race"),output))
        using(ManualResetEventSlim entered=new ManualResetEventSlim(false))
        using(ManualResetEventSlim release=new ManualResetEventSlim(false))
        {
            IntPtr handle=form.Handle;Exception startError=null,stopError=null;
            output.DuringDown=delegate{entered.Set();if(!release.Wait(1500))throw new Exception("test output barrier timed out");};
            Thread starter=new Thread(delegate(){try{Call(form,"StartParallelHeldMapping",macro);}catch(Exception ex){startError=ex;}});
            Thread stopper=new Thread(delegate(){try{Call(form,"HandleTerminalInput",Edge(Keys.F12));}catch(Exception ex){stopError=ex;}});
            starter.Start();Check(entered.Wait(1500),"held admission reaches its actual output boundary");stopper.Start();
            try{Until(()=>(bool)typeof(MainForm).GetField("manualTriggerSuspend",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form));}
            finally{release.Set();}
            Check(starter.Join(1500) && stopper.Join(1500),"racing held start and panic finish without UI pumping");
            if(startError!=null)throw startError;if(stopError!=null)throw stopError;
            Check(Sources(form)==0 && (int)Call(form,"ActiveParallelHeldMappingCount")==0,"in-flight held publication cannot become an orphan after panic");
            output.DuringDown=null;int stopped=output.Downs;Call(form,"StartParallelHeldMapping",macro);
            Check(output.Downs==stopped,"panic latch also rejects a late direct held admission");Pump();
        }
    }
    [STAThread] public static int Main(){return IsolatedUiDesktop.Run(Run);}
    private static int Run()
    {
        EmbeddedDependencyLoader.Register();Application.EnableVisualStyles();Control.CheckForIllegalCrossThreadCalls=true;string root=Path.Combine(Path.GetTempPath(),"InputStitch-runtime-control-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{foreach(string kind in new[]{"release-keyboard","release-mouse","release-gamepad","release-worker","panic","toggle"})StopScenario(root,kind);foreach(string kind in new[]{"valid","other-stop","disabled","disabled-only","removed","removed-only","edited","stop-all","stop-selected","profile","released","condition"})StaleStart(root,kind);PanicRouterAndLateAdmission(root);HeldStartPanicRace(root);foreach(bool parallel in new[]{false,true})foreach(bool fail in new[]{false,true})MissedRelease(root,parallel,fail);SaveProbe(root);SelectionProbe(root);Console.WriteLine("PASS runtime control: "+checks+" checks; blocked-UI release/panic/toggle, stale admission, unchanged save and input candidate filtering; private observer only.");return 0;}
        catch(Exception ex){Console.WriteLine(ex);return 1;}
        finally{Directory.Delete(root,true);}
    }
}
