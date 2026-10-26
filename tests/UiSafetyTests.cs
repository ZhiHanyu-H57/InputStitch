using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class UiSafetyTests
{
    private static int checks;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    [STAThread]
    public static int Main()
    { return IsolatedUiDesktop.Run(MainCore); }
    private static int MainCore()
    {
        try
        {
            EmbeddedDependencyLoader.Register();
            ScreenshotPassthrough();
            MouseRecovery();
            ShiftNavigationTriggers();
            Check(!UiSafetyPolicy.ShouldPauseControl(false,true,false,false,false), "idle hover over a button/list must not reject hotkeys");
            Check(!UiSafetyPolicy.ShouldPauseControl(false,true,true,false,false), "hover over an editor is not editing");
            Check(!UiSafetyPolicy.ShouldPauseControl(true,false,false,false,false), "idle button/list focus can transfer to the input sink");
            Check(UiSafetyPolicy.ShouldPauseControl(true,false,true,false,false), "focused editable field blocks a new macro");
            Check(UiSafetyPolicy.ShouldPauseControl(true,false,false,true,false), "running keyboard output cannot activate a focused button");
            Check(!UiSafetyPolicy.ShouldPauseControl(false,true,false,true,false), "keyboard/gamepad run ignores harmless hover");
            Check(UiSafetyPolicy.ShouldPauseControl(false,true,false,true,true), "mouse output remains paused over destructive controls");
            Check(!UiSafetyPolicy.ShouldPauseControl(false,false,false,true,true), "mouse output resumes outside guarded controls");
            using (TextBox text = new TextBox())
            using (Button button = new Button())
            using (ListBox list = new ListBox())
            using (ComboBox combo = new ComboBox())
            using (NumericUpDown number = new NumericUpDown())
            using (DataGridView grid = new DataGridView())
            {
                Check(UiSafetyPolicy.IsEditor(text), "name/note input is an editor");
                text.ReadOnly = true; Check(!UiSafetyPolicy.IsEditor(text), "read-only trigger field is not editing");
                Check(!UiSafetyPolicy.IsEditor(button) && !UiSafetyPolicy.IsEditor(list), "buttons and macro selection are not text editing");
                Check(UiSafetyPolicy.IsEditor(combo) && UiSafetyPolicy.IsEditor(number), "editable settings remain protected");
                Check(!UiSafetyPolicy.IsEditor(grid), "step selection without cell editing is safe before dispatch");
            }
            MacroDefinition macro = new MacroDefinition();
            macro.Steps.Add(new MacroStep { Kind=InputKind.Gamepad });
            Check(!UiSafetyPolicy.HasMouseOutput(macro), "gamepad only has no mouse hazard");
            macro.Steps.Add(new MacroStep { Kind=InputKind.Keyboard });
            Check(!UiSafetyPolicy.HasMouseOutput(macro), "keyboard has no pointer hazard");
            macro.Steps.Add(new MacroStep { Kind=InputKind.MouseLeft });
            Check(UiSafetyPolicy.HasMouseOutput(macro), "mixed mouse macro retains pointer protection");
            macro.Steps[2].Kind=InputKind.WheelDown;
            Check(UiSafetyPolicy.HasMouseOutput(macro), "wheel cannot change pointed-at numeric values");
            Localizer.SetLanguage("zh-CN"); Check(UiSafetyPolicy.ProtectionHint("编辑名称").Contains("点击空白"), "Chinese hint explains recovery");
            Localizer.SetLanguage("en-US"); Check(UiSafetyPolicy.ProtectionHint("编辑名称").Contains("Click an empty area"), "English hint explains recovery");
            RuntimeTrace trace = new RuntimeTrace();
            Thread a = new Thread(delegate() { for(int i=0;i<5000;i++) trace.Add("test", "a"); });
            Thread b = new Thread(delegate() { for(int i=0;i<5000;i++) trace.Add("test", "b"); });
            a.Start(); b.Start(); Check(a.Join(5000) && b.Join(5000), "concurrent trace completes");
            string snapshot=trace.Snapshot();
            Check(snapshot.Split(new[] {'\n'},StringSplitOptions.RemoveEmptyEntries).Length == RuntimeTrace.Capacity+1, "trace remains bounded after 10000 events");
            Check(snapshot.Contains("10000 "), "trace preserves last sequence");
            trace.Add("test", new string('x',200)+"\nnot-an-event");
            Check(!trace.Snapshot().Contains("not-an-event"), "detail length and newline sanitization");
            ContextMenuLifetime();
            Console.WriteLine("PASS UI safety/diagnostics: " + checks + " checks; private desktop, isolated windows and observer output; no global hooks, physical input or user configuration.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message); return 1; }
    }

    private static void ScreenshotPassthrough()
    {
        System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        using (InputSinkControl sink = new InputSinkControl())
        {
            foreach (Keys modifiers in new[] { Keys.None, Keys.Control, Keys.Shift, Keys.Alt, Keys.Control | Keys.Alt })
            {
                KeyEventArgs screen = new KeyEventArgs(Keys.PrintScreen | modifiers);
                typeof(InputSinkControl).GetMethod("OnKeyDown", hidden).Invoke(sink, new object[] { screen });
                Check(!screen.Handled && !screen.SuppressKeyPress && UiSafetyPolicy.PassThroughUiKey(screen.KeyData), "PrintScreen reaches Windows from the focused input sink " + modifiers);
            }
            foreach (Keys key in new[] { Keys.Enter, Keys.Space, Keys.A, Keys.F8 })
            {
                KeyEventArgs ordinary = new KeyEventArgs(key);
                typeof(InputSinkControl).GetMethod("OnKeyDown", hidden).Invoke(sink, new object[] { ordinary });
                Check(ordinary.Handled && ordinary.SuppressKeyPress, "ordinary macro keys remain absorbed by the inert sink " + key);
            }
        }
    }

    private static void ContextMenuLifetime()
    {
        using (Control dispatcher = new Control())
        {
            IntPtr handle = dispatcher.Handle;
            using (ContextMenuStrip menu = new ContextMenuStrip())
            {
                MainForm.DeferContextMenuDispose(dispatcher, menu);
                Check(!menu.IsDisposed, "context menu disposal is deferred past the current ToolStrip event");
                Application.DoEvents();
                Check(menu.IsDisposed, "deferred context menu disposal completes on the next UI message turn");
            }
        }

        ContextMenuStrip orphan = new ContextMenuStrip();
        using (Control disposedDispatcher = new Control())
        {
            IntPtr handle = disposedDispatcher.Handle;
            disposedDispatcher.Dispose();
            MainForm.DeferContextMenuDispose(disposedDispatcher, orphan);
            Check(!orphan.IsDisposed, "disposed dispatcher never forces synchronous context menu disposal");
        }
        orphan.Dispose();
    }

    private sealed class Observer : IOutputBackend
    {
        internal int Downs, Ups;
        public void SendDown(InputSpec input) { Interlocked.Increment(ref Downs); }
        public void SendUp(InputSpec input) { Interlocked.Increment(ref Ups); }
        public void NeutralizeGamepad() { }
    }
    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Call(MainForm form, string name, params object[] args)
    { return typeof(MainForm).GetMethod(name, Hidden).Invoke(form, args); }
    private static T Field<T>(MainForm form, string name)
    { return (T)typeof(MainForm).GetField(name, Hidden).GetValue(form); }
    private static void Pump() { for (int i=0;i<12;i++) { Application.DoEvents(); Thread.Sleep(2); } }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseData { internal int X,Y; internal uint Data,Flags,Time; internal IntPtr Extra; }
    private static void MouseRecovery()
    {
        string directory=Path.Combine(Path.GetTempPath(), "InputStitch-mouse-recovery-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            MacroConfig config=new MacroConfig { GamepadDeviceType=VirtualGamepadTypes.None, PauseMacroInRiskyUi=false };
            MacroDefinition macro=new MacroDefinition { Trigger=new TriggerSpec { Kind=InputKind.MouseLeft, VirtualKey=0 }, SuppressTrigger=true };
            macro.Steps.Add(new MacroStep { VirtualKey=(int)Keys.F23, HoldMs=1, DelayMs=1 });
            config.Macros.Add(macro);
            Observer observer=new Observer();
            using (MainForm form=new MainForm(config,directory,observer))
            {
                IntPtr handle=form.Handle;
                InputEventInfo own=new InputEventInfo { Input=new InputSpec { Kind=InputKind.MouseLeft, VirtualKey=0 }, PointerOverInputStitch=true };
                Check(!(bool)Call(form,"HandleTerminalInput",own),"own-window primary clicks pass even with UI protection disabled");
                Pump(); Check(observer.Downs==0,"own-window click cannot start the left-button macro");
                InputEventInfo outside=new InputEventInfo { Input=new InputSpec { Kind=InputKind.MouseLeft, VirtualKey=0 } };
                Check((bool)Call(form,"HandleTerminalInput",outside),"intentional external mouse trigger retains suppression");
                InputEventInfo panic=new InputEventInfo { Input=new InputSpec { Kind=InputKind.Keyboard, VirtualKey=(int)Keys.F12 }, Ctrl=true, Shift=true };
                Check((bool)Call(form,"HandleTerminalInput",panic),"emergency chord retains priority");
                Check(Field<bool>(form,"manualTriggerSuspend"),"emergency suspension is immediate before UI dispatch");
                Check(!(bool)Call(form,"HandleTerminalInput",outside),"next click passes before emergency cleanup is pumped");
                Pump(); Check(observer.Downs==0,"queued macro start is cancelled by emergency suspension");
                Check(macro.Enabled && macro.SuppressTrigger,"emergency suspension preserves stored trigger preferences");
                ToolStripMenuItem pause=Field<ToolStripMenuItem>(form,"suspendTriggersMenuItem");
                Check(pause.Checked,"gear menu exposes the emergency suspension");
                pause.Checked=false;
                Check(!Field<bool>(form,"manualTriggerSuspend"),"existing menu explicitly resumes triggers");
                Call(form,"EmergencyStop","ui-stop-all");
                Check(!Field<bool>(form,"manualTriggerSuspend"),"ordinary Stop all does not suspend future triggers");
                Check((bool)Call(form,"HandleTerminalInput",outside),"external primary trigger remains usable after explicit resume");
                Pump(); Check(observer.Downs>0 && observer.Downs==observer.Ups,"resumed macro emits balanced observer output");
                Call(form,"EmergencyStop","tray-emergency");
                Check(Field<bool>(form,"manualTriggerSuspend") && pause.Checked,"tray emergency stop also latches and updates the menu");
            }
            using (Form target=new Form { StartPosition=FormStartPosition.Manual, Location=new Point(100,100), Size=new Size(360,230), Text="Private mouse recovery target" })
            {
                target.Show(); Application.DoEvents();
                Point client=target.PointToScreen(new Point(30,30));
                Check(NativeWindowFocus.IsCurrentProcessWindowAtPoint(client.X,client.Y),"native point lookup recognizes own client window");
                Check(NativeWindowFocus.IsCurrentProcessWindowAtPoint(target.Right-22,target.Top+12),"native point lookup includes caption/close-button area");
                NativeWindow overlay=new NativeWindow();
                try
                {
                    overlay.CreateHandle(new CreateParams { Caption="Private click-through overlay", X=-20000,Y=-20000,Width=20,Height=20,Style=unchecked((int)0x80000000),ExStyle=0x080800A0 });
                    Check(!NativeWindowFocus.IsInteractiveInputStitchWindow(overlay.Handle),"native click-through overlays are not mistaken for editor controls");
                }
                finally { overlay.DestroyHandle(); }
                HookManager hook=(HookManager)FormatterServices.GetUninitializedObject(typeof(HookManager));
                typeof(HookManager).GetField("physicalKeysDown",Hidden).SetValue(hook,new HashSet<int>());
                typeof(HookManager).GetField("suppressedMouseUntilUp",Hidden).SetValue(hook,new HashSet<InputKind>());
                bool marked=false;
                hook.OnTerminalInput=delegate(InputEventInfo input) { marked=input.PointerOverInputStitch; return false; };
                IntPtr data=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MouseData)));
                try
                {
                    Marshal.StructureToPtr(new MouseData { X=client.X,Y=client.Y },data,false);
                    typeof(HookManager).GetMethod("MouseCallback",Hidden).Invoke(hook,new object[] { 0,new IntPtr(0x201),data });
                    Check(marked,"real mouse decoder marks the native target without a global hook");
                }
                finally { Marshal.FreeHGlobal(data); hook.Dispose(); }
                target.Close();
            }
        }
        finally { Directory.Delete(directory,true); }
    }

    private static void ShiftNavigationTriggers()
    {
        string directory=Path.Combine(Path.GetTempPath(),"InputStitch-shift-trigger-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            MacroDefinition bare=new MacroDefinition { Trigger=new TriggerSpec { VirtualKey=(int)Keys.PageDown }, SuppressTrigger=true };
            bare.Steps.Add(new MacroStep { VirtualKey=(int)Keys.F23, HoldMs=1, DelayMs=1 });
            InputEventInfo shifted=new InputEventInfo { Input=new InputSpec { VirtualKey=(int)Keys.PageDown, Extended=true }, Shift=true };
            MethodInfo select=typeof(MainForm).GetMethod("SelectTriggerMacro",BindingFlags.Static|BindingFlags.NonPublic);
            MacroDefinition chord=bare.Clone(); chord.Trigger.Shift=true;
            List<MacroDefinition> choices=new List<MacroDefinition> { bare,chord };
            Check(ReferenceEquals(select.Invoke(null,new object[] { choices,shifted }),chord),"explicit Shift+PageDown wins even below the bare key");
            chord.Enabled=false;
            Check(ReferenceEquals(select.Invoke(null,new object[] { choices,shifted }),bare),"disabled specific chord allows the bare Shift-compatible trigger");
            MacroConfig config=new MacroConfig { GamepadDeviceType=VirtualGamepadTypes.None,PauseMacroInRiskyUi=false };
            config.Macros.Add(bare); Observer observer=new Observer();
            using (MainForm form=new MainForm(config,directory,observer))
            {
                IntPtr handle=form.Handle;
                Check((bool)Call(form,"HandleTerminalInput",shifted),"real macro admission accepts Shift+PageDown for a PageDown trigger");
                Call(form,"HandleTerminalInputReleased",shifted); Pump();
                Check(observer.Downs>0 && observer.Downs==observer.Ups,"Shift+PageDown reaches the worker and releases output");
                InputEventInfo ctrl=new InputEventInfo { Input=new InputSpec { VirtualKey=(int)Keys.PageDown },Ctrl=true };
                Check(!(bool)Call(form,"HandleTerminalInput",ctrl),"bare PageDown does not consume an unconfigured Ctrl shortcut");
            }
        }
        finally { Directory.Delete(directory,true); }
    }
}
