using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class RecordingEditingTests
{
    private static int checks;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Private).GetValue(value); }
    private static object Call(object value, string name, params object[] args) { return value.GetType().GetMethod(name, Private).Invoke(value, args); }
    private static void Pump() { for (int i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(1); } }
    private static void Click(Button button) { typeof(Button).GetMethod("OnClick", Private).Invoke(button, new object[] { EventArgs.Empty }); }
    private static InputSpec Mouse(InputKind kind) { return new InputSpec { Kind = kind, VirtualKey = 0 }; }
    private static InputEventInfo Event(InputKind kind) { return new InputEventInfo { Input = Mouse(kind) }; }
    private static readonly InputKind[] Mice = { InputKind.MouseLeft, InputKind.MouseRight, InputKind.MouseMiddle, InputKind.MouseX1, InputKind.MouseX2, InputKind.WheelUp, InputKind.WheelDown };
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, int message, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int process);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, IntPtr process);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    private static bool Activate(IntPtr window)
    {
        uint current = GetCurrentThreadId(), foreground = GetWindowThreadProcessId(NativeWindowFocus.ForegroundWindow(), IntPtr.Zero);
        bool attached = foreground != 0 && current != foreground && AttachThreadInput(current, foreground, true);
        try { return NativeWindowFocus.TryActivate(window); }
        finally { if (attached) AttachThreadInput(current, foreground, false); }
    }

    private static HookManager Hook(Action<InputEventInfo> down, Action<InputEventInfo> up)
    {
        // Exercise the real native decoder without installing a global hook or emitting input.
        HookManager hook = (HookManager)FormatterServices.GetUninitializedObject(typeof(HookManager));
        typeof(HookManager).GetField("physicalKeysDown", Private).SetValue(hook, new HashSet<int>());
        typeof(HookManager).GetField("suppressedMouseUntilUp", Private).SetValue(hook, new HashSet<InputKind>());
        hook.OnTerminalInput = delegate(InputEventInfo e) { down(e); return false; };
        hook.OnTerminalInputReleased = up;
        return hook;
    }

    private static void MouseEdge(HookManager hook, InputKind kind, bool down, bool injected)
    {
        int message = kind == InputKind.MouseLeft ? 0x201 : kind == InputKind.MouseRight ? 0x204 : kind == InputKind.MouseMiddle ? 0x207 : 0x20B;
        int data = kind == InputKind.MouseX1 ? 1 << 16 : kind == InputKind.MouseX2 ? 2 << 16 : 0;
        if (kind == InputKind.WheelUp || kind == InputKind.WheelDown) { message = 0x20A; data = (kind == InputKind.WheelUp ? 120 : -120) << 16; }
        else if (!down) message++;
        IntPtr buffer = Marshal.AllocHGlobal(32);
        try
        {
            for (int i = 0; i < 32; i += 4) Marshal.WriteInt32(buffer, i, 0);
            Marshal.WriteInt32(buffer, 8, data); Marshal.WriteInt32(buffer, 12, injected ? 1 : 0);
            Call(hook, "MouseCallback", 0, new IntPtr(message), buffer);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void TestMouseSelection()
    {
        TestArtwork();
        foreach (Size size in new[] { new Size(360, 300), new Size(540, 270), new Size(450, 330) })
        using (MouseInputDiagram diagram = new MouseInputDiagram { Size = size })
        {
            diagram.PerformLayout();
            RectangleF photo = (RectangleF)Call(diagram, "GetPhotoBounds");
            Check(Math.Abs(photo.Height / photo.Width - 1.5F) < .001F, "photo retains its proportions in viewport " + size);
            Button right = diagram.Controls.OfType<Button>().First(x => (InputKind)x.Tag == InputKind.MouseRight);
            Button middle = diagram.Controls.OfType<Button>().First(x => (InputKind)x.Tag == InputKind.MouseMiddle);
            PointF r = (PointF)Call(diagram, "GetCalloutTarget", InputKind.MouseRight), m = (PointF)Call(diagram, "GetCalloutTarget", InputKind.MouseMiddle);
            Check(r.Y < m.Y && right.Top + right.Height / 2F < middle.Top + middle.Height / 2F, "right and middle callouts retain the same vertical order without crossing " + size);
        }
        foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
        using (MouseInputDiagram diagram = new MouseInputDiagram { Size = new Size((int)(360 * scale), (int)(300 * scale)) })
        using (Font font = new Font("Segoe UI", EditorTheme.BodyFontSize * scale))
        {
            foreach (Button button in diagram.Controls) button.Font = font;
            diagram.SetSelection(InputKind.MouseX1); diagram.PerformLayout();
            using (Bitmap bitmap = new Bitmap(diagram.Width, diagram.Height))
            {
                diagram.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mouse-" + (int)(scale * 100) + ".png"));
            }
            foreach (Button button in diagram.Controls)
            {
                Size text = TextRenderer.MeasureText(button.Text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Check(text.Width <= button.ClientSize.Width - 2 && text.Height <= button.ClientSize.Height - 4, "mouse caption fits at scale " + scale + ": " + button.Text);
            }
            InputKind? hit = (InputKind?)Call(diagram, "Hit", new Point((int)(151 * scale), (int)(159 * scale)));
            Check(hit == InputKind.MouseX1, "photo side-button hit scales correctly " + scale);
        }
        foreach (InputKind kind in Mice)
        {
            TriggerSpec trigger; string error;
            Check(VirtualKeyboardDialog.TryCreateTrigger(new[] { Mouse(kind) }, out trigger, out error) && trigger.Kind == kind, "mouse trigger preserves " + kind);
            trigger.Ctrl = true;
            List<InputSpec> restored = VirtualKeyboardDialog.InputsFromTrigger(trigger);
            TriggerSpec reopened;
            Check(VirtualKeyboardDialog.TryCreateTrigger(restored, out reopened, out error) && reopened.Kind == kind && reopened.Ctrl, "Ctrl + mouse roundtrip " + kind);
            using (VirtualKeyboardDialog dialog = new VirtualKeyboardDialog(true, restored))
            {
                List<InputSpec> selected = Field<List<InputSpec>>(dialog, "selected");
                Check(selected.Count == 2 && selected[1].Kind == kind, "reopen keeps mouse selection " + kind);
                Call(dialog, "ConfirmSelection", null, EventArgs.Empty);
                Check(dialog.SelectedTrigger.Kind == kind && dialog.SelectedTrigger.Ctrl, "confirmation keeps mouse type " + kind);
            }
            using (StepEditDialog editor = new StepEditDialog(null, new MacroStep { Kind = kind, HoldMs = 87, DelayMs = 123 }))
            {
                Check(Field<ComboBox>(editor, "inputTypeBox").SelectedIndex == 2, "mouse device selected " + kind);
                MouseInputDiagram diagram = Field<MouseInputDiagram>(editor, "mousePreview");
                Button button = diagram.Controls.OfType<Button>().First(x => (InputKind)x.Tag == kind);
                Click(button);
                Check(Field<InputSpec>(editor, "selectedInput").Kind == kind, "diagram selects " + kind);
                Call(editor, "Ok_Click", null, EventArgs.Empty);
                Check(editor.ResultStep.Kind == kind && editor.ResultStep.HoldMs == 87 && editor.ResultStep.DelayMs == 123, "step saves mouse type/timing " + kind);
            }
            List<InputEventInfo> downs = new List<InputEventInfo>(), ups = new List<InputEventInfo>();
            using (HookManager hook = Hook(downs.Add, ups.Add))
            {
                MouseEdge(hook, kind, true, false);
                if (InputSender.IsHoldable(Mouse(kind))) MouseEdge(hook, kind, false, false);
                Check(downs.Count == 1 && downs[0].Input.Kind == kind, "native down decoder " + kind);
                Check(ups.Count == (InputSender.IsHoldable(Mouse(kind)) ? 1 : 0), "native release decoder " + kind);
                MouseEdge(hook, kind, true, true);
                Check(downs.Count == 1, "own injected output is not recaptured " + kind);
            }
        }
        List<MacroStep> chord = VirtualKeyboardDialog.BuildChordSteps(new[] { new InputSpec { VirtualKey = (int)Keys.LControlKey }, Mouse(InputKind.MouseX1) }, new MacroStep { HoldMs = 42, DelayMs = 63 });
        Check(chord.Count == 3 && chord[0].Action == MacroAction.Down && chord[1].Kind == InputKind.MouseX1 && chord[1].Action == MacroAction.Press && chord[2].Action == MacroAction.Up && chord[2].DelayMs == 63, "keyboard + mouse chord is balanced with one final wait");
        chord = VirtualKeyboardDialog.BuildChordSteps(new[] { Mouse(InputKind.WheelUp), Mouse(InputKind.MouseX2) }, new MacroStep());
        Check(chord.All(x => x.Kind != InputKind.WheelUp || x.Action == MacroAction.Press) && chord.Count(x => x.Kind == InputKind.MouseX2 && x.Action == MacroAction.Up) == 1, "wheel in a combination stays a pulse and held mouse releases");
        using (StepEditDialog editor = new StepEditDialog(null, new MacroStep()))
        {
            typeof(StepEditDialog).GetField("selectedInputs", Private).SetValue(editor, new List<InputSpec> { new InputSpec { VirtualKey = (int)Keys.LControlKey }, Mouse(InputKind.WheelUp) });
            Field<ComboBox>(editor, "actionBox").SelectedIndex = 1;
            Check(!((Button)editor.AcceptButton).Enabled, "wheel combination cannot be held");
            Field<ComboBox>(editor, "actionBox").SelectedIndex = 0;
            Check(((Button)editor.AcceptButton).Enabled, "wheel combination can be tapped");
        }
    }

    private static void TestArtwork()
    {
        using (Stream stream = typeof(MouseInputDiagram).Assembly.GetManifestResourceStream("InputStitch.Assets.mouse-color.jpg"))
        using (Bitmap color = new Bitmap(stream))
        using (Bitmap photo = (Bitmap)typeof(MouseInputDiagram).GetMethod("LoadPhoto", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null))
        {
            Check(photo.Width == 320 && photo.Height == 480 && photo.GetPixel(0, 0).A == 0, "photo dimensions and transparent edge preserved");
            foreach (Point p in new[] { new Point(90, 90), new Point(160, 130), new Point(180, 250), new Point(160, 380) })
            {
                Color expected = color.GetPixel(p.X, p.Y), actual = photo.GetPixel(p.X, p.Y);
                Check(Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1 && Math.Abs(expected.B - actual.B) <= 1, "photo color is copied pixel-for-pixel at native DPI: " + p);
            }
        }
    }

    private static void TestTiming(string directory)
    {
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition macro = new MacroDefinition(); config.Macros.Add(macro);
        macro.Steps.Add(new MacroStep { Kind = InputKind.MouseX1, HoldMs = 17, DelayMs = 31 });
        macro.Steps.Add(new MacroStep { Action = MacroAction.Down, HoldMs = 91, DelayMs = 33 });
        macro.Steps.Add(new MacroStep { Kind = InputKind.WheelDown, HoldMs = 94, DelayMs = 35 });
        using (MainForm form = new MainForm(config, directory))
        {
            Call(form, "RefreshSteps");
            Check(StepTimingEditor.Apply(macro.Steps, new[] { 0, 0, -1, 99 }, new StepTimingEdit { EditHold = true, HoldMs = 120, EditWait = false }) == 1, "selected hold only, deduplicated indices");
            Check(macro.Steps[0].HoldMs == 120 && macro.Steps[0].DelayMs == 31 && macro.Steps[1].HoldMs == 91, "hold-only keeps waits and other rows");
            Call(form, "RefreshSteps"); Call(form, "RestoreSteps", false);
            Check(macro.Steps[0].HoldMs == 17, "batch undo in editor");
            Call(form, "RestoreSteps", true);
            Check(macro.Steps[0].HoldMs == 120, "batch redo in editor");
            Check(StepTimingEditor.Apply(macro.Steps, null, new StepTimingEdit { AllSteps = true, EditHold = true, HoldMs = 78, EditWait = true, RandomWait = true, MinWaitMs = 80, MaxWaitMs = 120 }) == 3, "all steps hold + random wait");
            Check(macro.Steps[0].HoldMs == 78 && macro.Steps[1].HoldMs == 91 && macro.Steps[2].HoldMs == 94 && macro.Steps.All(x => x.RandomDelay && x.RandomDelayMinMs == 80 && x.RandomDelayMaxMs == 120), "non-holdable events keep hold values; all waits change");
            bool rejected = false;
            try { StepTimingEditor.Apply(macro.Steps, null, new StepTimingEdit { AllSteps = true, EditHold = true, HoldMs = 1, RandomWait = true, MinWaitMs = 9, MaxWaitMs = 8 }); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected && macro.Steps[0].HoldMs == 78, "invalid batch is rejected before any mutation");
            StepTimingEditor.Apply(macro.Steps, new[] { 0 }, new StepTimingEdit { WaitMs = 0 });
            Check(!macro.Steps[0].RandomDelay && macro.Steps[0].DelayMs == 0 && macro.Steps[1].RandomDelay, "zero fixed wait affects selected row only");
            Call(form, "RefreshSteps");
            Call(form, "SelectStepIndices", new List<int> { 0, 1 });
            bool seen = false;
            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 15 })
            {
                timer.Tick += delegate
                {
                    BatchTimingDialog dialog = Application.OpenForms.OfType<BatchTimingDialog>().FirstOrDefault();
                    if (dialog == null) return;
                    timer.Stop(); seen = true;
                    Field<CheckBox>(dialog, "holdChangeBox").Checked = true;
                    Field<NumericUpDown>(dialog, "holdBox").Value = 144;
                    Field<NumericUpDown>(dialog, "fixedBox").Value = 222;
                    Field<RadioButton>(dialog, "fixedMode").Checked = true;
                    Click((Button)dialog.AcceptButton);
                };
                timer.Start(); Call(form, "EditSelectedStep"); timer.Stop();
            }
            Check(seen && macro.Steps[0].HoldMs == 144 && macro.Steps[0].DelayMs == 222 && macro.Steps[1].DelayMs == 222 && macro.Steps[2].RandomDelay, "multi-selection Edit opens batch dialog and applies selected timing");
            Call(form, "RestoreSteps", false);
            Check(macro.Steps[0].HoldMs == 78 && macro.Steps[0].DelayMs == 0 && macro.Steps[1].RandomDelay, "complete dialog edit undoes in one operation");
            MouseInputDiagram mouseDiagram = new MouseInputDiagram { Size = new Size(360, 300) };
            InputKind picked = InputKind.Keyboard;
            mouseDiagram.InputSelected += kind => picked = kind;
            Call(mouseDiagram, "OnMouseClick", new MouseEventArgs(MouseButtons.Left, 1, 150, 85, 0));
            Check(picked == InputKind.MouseLeft, "mouse body itself is clickable");
            Call(mouseDiagram, "OnMouseClick", new MouseEventArgs(MouseButtons.Left, 1, 151, 159, 0));
            Check(picked == InputKind.MouseX1, "physical side button selects X1");
            mouseDiagram.Dispose();
            Call(form, "SaveConfig");
        }
        MacroConfig reopened = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml"));
        Check(reopened.Macros[0].Steps[0].Kind == InputKind.MouseX1 && reopened.Macros[0].Steps[0].HoldMs == 78 && reopened.Macros[0].Steps[1].RandomDelay, "batch mouse/timing save and reopen");
        using (BatchTimingDialog dialog = new BatchTimingDialog(0, 3, 0, 1, macro.Steps[0]))
        {
            Check(Field<RadioButton>(dialog, "allScope").Checked && !Field<RadioButton>(dialog, "selectedScope").Enabled, "no selection offers all steps");
            Field<CheckBox>(dialog, "waitChangeBox").Checked = false;
            Check(!((Button)dialog.AcceptButton).Enabled, "empty timing edit cannot apply");
            Field<CheckBox>(dialog, "holdChangeBox").Checked = true;
            Check(((Button)dialog.AcceptButton).Enabled, "hold-only edit can apply");
            Check(dialog.Edit == null, "cancel has no timing edit");
        }
    }

    private static void TestRecording(string root, bool native)
    {
        Process target = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--window-host" + (native ? "" : " --offscreen")) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
        IntPtr window = IntPtr.Zero;
        try
        {
            window = new IntPtr(long.Parse(target.StandardOutput.ReadLine()));
                MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None, AutoSwitchProfiles = true };
                MacroDefinition macro = new MacroDefinition(); config.Macros.Add(macro);
                using (MainForm form = new MainForm(config, Path.Combine(root, "recording")))
                using (HookManager hook = Hook(e =>
                {
                    if (!native && Field<bool>(form, "recordingActive")) Call(form, "RecordPhysicalInputForWindow", e, true, window);
                    else Call(form, "HandleTerminalInput", e);
                }, e =>
                {
                    if (!native && Field<bool>(form, "recordingActive")) Call(form, "RecordPhysicalInputForWindow", e, false, window);
                    else Call(form, "HandleTerminalInputReleased", e);
                }))
                {
                    if (!native) { form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false; }
                    form.Show(); Pump();
                    foreach (bool configured in new[] { false, true })
                    {
                    config.TargetProcessName = configured ? "nonexistent-recording-target" : "";
                    config.ActivateTargetWindowOnUiRun = configured;
                    bool staleCallbackCancelled = false;
                    form.BeginStepInputCapture(delegate { staleCallbackCancelled = true; });
                    Call(form, "StartMacroRecording");
                    Check(staleCallbackCancelled && Field<object>(form, "captureMode").ToString() == "None", "recording cancels old one-key capture");
                    AllowSetForegroundWindow(target.Id);
                    if (native) Check(Activate(window), "external recording app is foreground; configured=" + configured);
                    else Check(NativeWindowFocus.IsUsableExternalWindow(window), "recording foreground boundary uses a real external window");
                    foreach (InputKind kind in Mice)
                    {
                        MouseEdge(hook, kind, true, false); Thread.Sleep(2);
                        if (InputSender.IsHoldable(Mouse(kind))) MouseEdge(hook, kind, false, false);
                    }
                    InputEventInfo key = new InputEventInfo { Input = new InputSpec { VirtualKey = (int)Keys.A } };
                    if (native) { Call(form, "HandleTerminalInput", key); Thread.Sleep(2); Call(form, "HandleTerminalInputReleased", key); }
                    else { Call(form, "RecordPhysicalInputForWindow", key, true, window); Thread.Sleep(2); Call(form, "RecordPhysicalInputForWindow", key, false, window); }
                    Call(form, "StopMacroRecording", true, false);
                    Check(macro.Steps.Count == 8 && macro.Steps.Take(7).Select(x => x.Kind).SequenceEqual(Mice) && macro.Steps[7].VirtualKey == (int)Keys.A, "record keyboard and every mouse button; configured=" + configured);
                    Check(macro.Steps.All(x => x.Action == MacroAction.Press), "recording retains balanced taps");
                    if (native) Check(Activate(form.Handle), "editor is foreground for control-click exclusion");
                    else Check(NativeWindowFocus.IsCurrentProcessWindow(form.Handle), "editor boundary uses an actual own-process window"); Pump();
                    macro.Steps.Clear(); Call(form, "RefreshSteps");
                    Call(form, "StartMacroRecording");
                    if (native) { Call(form, "HandleTerminalInput", Event(InputKind.MouseLeft)); Call(form, "HandleTerminalInputReleased", Event(InputKind.MouseLeft)); }
                    else { Call(form, "RecordPhysicalInputForWindow", Event(InputKind.MouseLeft), true, form.Handle); Call(form, "RecordPhysicalInputForWindow", Event(InputKind.MouseLeft), false, form.Handle); }
                    Check(Field<List<RecordedInputEvent>>(form, "recordedEvents").Count == 0, "InputStitch control clicks do not enter recording");
                    Call(form, "StopMacroRecording", false, true);
                    foreach (InputKind kind in Mice)
                    {
                        InputSpec captured = null;
                        form.BeginStepInputCapture(input => captured = input);
                        MouseEdge(hook, kind, true, false); Pump();
                        Check(captured != null && captured.Kind == kind, "physical step capture " + kind);
                        FieldInfo mode = typeof(MainForm).GetField("captureMode", Private);
                        mode.SetValue(form, Enum.Parse(mode.FieldType, "Trigger"));
                        MouseEdge(hook, kind, true, false); Pump();
                        Check(macro.Trigger.Kind == kind, "physical trigger capture " + kind);
                    }
                    }
                    form.Close();
                }
        }
        finally { if (window != IntPtr.Zero) PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero); if (!target.WaitForExit(3000)) target.Kill(); target.Dispose(); }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--artwork-native")) SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles();
        if (args.Contains("--artwork-native"))
        {
            try { TestArtwork(); using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) Console.WriteLine("PASS native artwork: " + checks + " checks; DPI=" + screen.DpiX); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        if (args.Contains("--window-host"))
        {
            using (Form target = new Form { Text = "InputStitch isolated recording target", Size = new Size(400, 200) })
            { if (args.Contains("--offscreen")) { target.StartPosition = FormStartPosition.Manual; target.Location = new Point(-20000, -20000); target.ShowInTaskbar = false; } target.Shown += delegate { Console.WriteLine(target.Handle.ToInt64()); Console.Out.Flush(); }; Application.Run(target); }
            return 0;
        }
        EmbeddedDependencyLoader.Register();
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-record-edit-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try { TestMouseSelection(); TestTiming(Path.Combine(root, "timing")); TestRecording(root, args.Contains("--recording-native")); Console.WriteLine("PASS recording/editing: " + checks + " checks; native decoder, explicit foreground boundary, real external window, isolated settings; no global hooks or emitted input."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(root, true); }
    }
}
