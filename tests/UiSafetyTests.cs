using System;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class UiSafetyTests
{
    private static int checks;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    [STAThread]
    public static int Main()
    {
        try
        {
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
            Console.WriteLine("PASS UI safety/diagnostics: " + checks + " checks; no real hooks, windows shown, input or user configuration.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
