using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class ProductAcceptanceTests
{
    private static int checks;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string root;
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, IntPtr state);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static T Field<T>(object owner, string name) { return (T)owner.GetType().GetField(name, Private).GetValue(owner); }
    private static object Call(object owner, string name, params object[] args)
    {
        MethodInfo method = owner.GetType().GetMethods(Private).SingleOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
        if (method == null) throw new MissingMethodException(owner.GetType().Name, name);
        try { return method.Invoke(owner, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }
    private static void Pump(int milliseconds)
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(2); }
    }
    private static void Wait(Func<bool> condition, string label, int timeout)
    {
        Stopwatch clock = Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < timeout) Pump(3);
        Check(condition(), label);
    }
    private sealed class Observer : IOutputBackend
    {
        private readonly object sync = new object();
        private readonly HashSet<string> pressed = new HashSet<string>();
        internal long Downs, Ups, KeyboardDowns, KeyboardUps, MaxContinuousGapMs;
        private long lastContinuousDown;
        private static string Key(InputSpec value) { return value.Kind + ":" + value.VirtualKey + ":" + value.GamepadControl; }
        public void SendDown(InputSpec value)
        {
            lock (sync)
            {
                Downs++; pressed.Add(Key(value));
                if (value.Kind == InputKind.Keyboard) KeyboardDowns++;
                if (value.Kind == InputKind.Keyboard && value.VirtualKey == (int)Keys.F6)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (lastContinuousDown != 0) MaxContinuousGapMs = Math.Max(MaxContinuousGapMs, (now - lastContinuousDown) * 1000 / Stopwatch.Frequency);
                    lastContinuousDown = now;
                }
            }
        }
        public void SendUp(InputSpec value) { lock (sync) { Ups++; pressed.Remove(Key(value)); if (value.Kind == InputKind.Keyboard) KeyboardUps++; } }
        public void NeutralizeGamepad() { lock (sync) pressed.RemoveWhere(x => x.StartsWith("Gamepad:", StringComparison.Ordinal)); }
        internal int Pressed { get { lock (sync) return pressed.Count; } }
    }
    private static MacroDefinition Timed(string name, Keys key, bool infinite)
    {
        return new MacroDefinition { Name = name, Infinite = infinite, SuppressTrigger = false,
            Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key },
            Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)key,
                Action = MacroAction.Press, HoldMs = 5, DelayMs = 7 } } };
    }
    private static void Render(Form form, string name)
    {
        typeof(Control).GetMethod("SetState", Private).Invoke(form, new object[] { 2, true });
        form.CreateControl();
        form.PerformLayout();
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(root, name + ".png")); }
    }
    private static IEnumerable<Control> All(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (Control next in All(child)) yield return next; }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "product-acceptance");
        Directory.CreateDirectory(root);
        try
        {
            if (args.Length >= 2 && args[0] == "--soak-minutes") Soak(int.Parse(args[1], CultureInfo.InvariantCulture));
            else if (args.Length > 0 && args[0] == "--layout") ModesAndLayout();
            else { Workflow(); ModesAndLayout(); FriendlyInteractions(); }
            Console.WriteLine("PASS product acceptance: " + checks + " checks; isolated editor, saved files and runtime observer; no user configuration or real input.");
            return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(root, "failure.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }

    private static void Workflow()
    {
        string directory = Path.Combine(root, "workflow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None, ActivateTargetWindowOnUiRun = false };
        Observer observer = new Observer();
        using (MainForm form = new MainForm(config, directory, observer))
        {
            IntPtr handle = form.Handle;
            Call(form, "AddMacro_Click", null, EventArgs.Empty);
            Check(config.Macros.Count == 1, "create a macro through the editor handler");
            MacroDefinition macro = config.Macros[0];
            Field<TextBox>(form, "nameBox").Text = "完整流程";
            Call(form, "CommitNameEdit");
            Field<TextBox>(form, "descriptionBox").Text = "编辑、保存、恢复与运行";
            Field<ComboBox>(form, "triggerModeBox").SelectedIndex = 4;
            Check(macro.Name == "完整流程" && macro.Activator.Mode == ActivatorModes.ShortPress, "name and trigger controls update saved model");
            using (StepEditDialog editor = new StepEditDialog(form, new MacroStep { Kind = InputKind.Keyboard,
                VirtualKey = (int)Keys.F6, Action = MacroAction.Press, HoldMs = 8, DelayMs = 12 }))
            {
                Field<NumericUpDown>(editor, "holdBox").Value = 11;
                Field<NumericUpDown>(editor, "delayBox").Value = 15;
                Render(editor, "step-editor-zh");
                Call(editor, "Ok_Click", null, EventArgs.Empty);
                Check(editor.ResultStep != null && editor.ResultStep.HoldMs == 11 && editor.ResultStep.DelayMs == 15,
                    "step editor commits changed duration and interval");
                macro.Steps.AddRange(editor.ResultSteps);
            }
            Call(form, "RefreshSteps");
            DataGridView grid = Field<DataGridView>(form, "grid");
            grid.Rows[0].Selected = true;
            Call(form, "CopySelectedSteps");
            Check(macro.Steps.Count == 2, "step copy action works");
            Call(form, "RestoreSteps", false);
            Check(macro.Steps.Count == 1, "undo restores the prior steps");
            Call(form, "RestoreSteps", true);
            Check(macro.Steps.Count == 2, "redo restores copied steps");
            Call(form, "SelectStepIndices", new List<int> { 1 });
            Call(form, "DeleteSelectedStep");
            Check(macro.Steps.Count == 1, "step delete action leaves the original intact");
            Call(form, "SaveConfig");
            using (ActivatorConditionDialog dialog = new ActivatorConditionDialog(macro, null, "基础层"))
            {
                Field<NumericUpDown>(dialog, "shortPress").Value = 240;
                Field<TextBox>(dialog, "process").Text = "example";
                Call(dialog, "CaptureValues");
                macro.Activator = dialog.SelectedActivator;
                macro.Conditions = dialog.SelectedConditions;
                Check(macro.Activator.Mode == ActivatorModes.ShortPress && macro.Activator.ShortPressMs == 240,
                    "timing editor preserves trigger selection while editing timing");
                Check(!All(dialog).Any(c => c.Text.Contains("Legacy") || c.Text.Contains("兼容旧") || c.Text.Contains("Output Ownership") || c.Text.Contains("Turbo")),
                    "editing copy has no historical mode or implementation notes");
                Render(dialog, "conditions-zh");
            }
            Call(form, "SaveConfig");
            MacroConfig saved = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml"));
            Check(saved.Macros[0].Name == macro.Name && saved.Macros[0].Steps[0].DelayMs == 15 &&
                saved.Macros[0].Activator.ShortPressMs == 240 && saved.Macros[0].Conditions.ForegroundProcessName == "example",
                "saved XML preserves editor changes");
            using (MainForm reopened = new MainForm(saved, directory, observer))
                Check(Field<ComboBox>(reopened, "triggerModeBox").SelectedIndex == 4 && Field<TextBox>(reopened, "nameBox").Text == macro.Name,
                    "reopened editor restores trigger and name");
            string package = Path.Combine(directory, "share.ismacros");
            ConfigPackageSerializer.SerializeMacroPackageToFile(new MacroPackage { Macros = new List<MacroDefinition> { macro.Clone() } }, package);
            MacroPackage imported = ConfigPackageSerializer.DeserializeMacroPackageOrLegacyConfig(package);
            Check(imported.Macros.Count == 1 && imported.Macros[0].Activator.Mode == macro.Activator.Mode &&
                imported.Macros[0].Steps[0].HoldMs == 11, "macro export/import round trip preserves actions and activation");
            string profile = Path.Combine(directory, "profile.isprofile");
            ConfigPackageSerializer.SerializeProfilePackageToFile(new ProfilePackage { Config = ConfigPackageSerializer.CloneConfig(config) }, profile);
            Check(ConfigPackageSerializer.DeserializeProfilePackageOrLegacy(profile).Config.Macros[0].Conditions.ForegroundProcessName == "example",
                "profile export/import preserves conditions");

            macro.Conditions = new MacroConditionConfig();
            Field<ComboBox>(form, "triggerModeBox").SelectedIndex = 0;
            Field<CheckBox>(form, "infiniteBox").Checked = true;
            Call(form, "RunButton_Click", null, EventArgs.Empty);
            Wait(delegate { return observer.Downs > 0; }, "run action reaches output observer", 2000);
            Call(form, "RunButton_Click", null, EventArgs.Empty);
            Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, "stop action ends the macro", 3000);
            Check(Field<OutputOwnershipManager>(form, "outputOwnership").Snapshot().Merged.Count == 0 && observer.Pressed == 0,
                "normal stop leaves no pressed output");
            Call(form, "RunButton_Click", null, EventArgs.Empty);
            Wait(delegate { return (bool)Call(form, "HasWorkerBackedRuns"); }, "restart works", 2000);
            Call(form, "EmergencyStop", "automated-workflow");
            Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, "emergency stop ends all runs", 3000);
            Check(observer.Pressed == 0, "emergency stop releases all output");
            Call(form, "CopyMacro_Click", null, EventArgs.Empty);
            Check(config.Macros.Count == 2 && !config.Macros[1].Enabled, "copy preserves original and starts disabled");
            bool confirmed = false;
            using (System.Windows.Forms.Timer confirm = new System.Windows.Forms.Timer())
            {
                confirm.Interval = 50;
                confirm.Tick += delegate
                {
                    EnumThreadWindows(GetCurrentThreadId(), delegate(IntPtr window, IntPtr state)
                    {
                        StringBuilder kind = new StringBuilder(64), title = new StringBuilder(128);
                        GetClassName(window, kind, kind.Capacity); GetWindowText(window, title, title.Capacity);
                        if (kind.ToString() == "#32770" && title.ToString() == AppInfo.ProductName)
                        { confirmed = true; confirm.Stop(); SendMessage(window, 0x0111, new IntPtr(6), IntPtr.Zero); }
                        return true;
                    }, IntPtr.Zero);
                };
                confirm.Start();
                Call(form, "DeleteMacro_Click", null, EventArgs.Empty);
            }
            Check(confirmed && config.Macros.Count == 1 && config.Macros[0].Name == "完整流程",
                "macro deletion confirms its own dialog and preserves the original");
        }
    }

    private static void ModesAndLayout()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (Size size in new[] { new Size(1000, 700), new Size(1280, 900) })
        foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
        {
            MacroConfig config = new MacroConfig { Language = language };
            MacroDefinition macro = Timed("示例宏 · Example", Keys.F8, false);
            config.Macros.Add(macro);
            using (MainForm form = new MainForm(config, Path.Combine(root, "layout-" + Guid.NewGuid().ToString("N")), new Observer()))
            {
                form.Scale(new SizeF(scale, scale));
                form.Size = new Size((int)(size.Width * scale), (int)(size.Height * scale));
                ComboBox modes = Field<ComboBox>(form, "triggerModeBox");
                foreach (int index in new[] { 0, 1, 2, 3, 4, 5, 6 })
                {
                    modes.SelectedIndex = index;
                    Check(TriggerEditor.Index(macro) == index && modes.Enabled, "one editable selector covers mode " + index);
                    Check(TriggerEditor.Index(ConfigPackageSerializer.CloneConfig(config).Macros[0]) == index,
                        "each trigger choice survives XML round trip " + index);
                }
                macro.Activator.Mode = ActivatorModes.Legacy;
                macro.RunMode = TriggerRunMode.Hold;
                Call(form, "LoadSelectedMacroToUi");
                Check(modes.SelectedIndex == 1 && macro.Activator.Mode == ActivatorModes.Legacy,
                    "old Hold configuration displays normally without rewriting its policy");
                using (ActivatorConditionDialog dialog = new ActivatorConditionDialog(macro, null, "Base"))
                { Call(dialog, "CaptureValues"); Check(dialog.SelectedActivator.Mode == ActivatorModes.Legacy, "opening timing dialog does not migrate old Hold"); }
                modes.SelectedIndex = 0;
                macro.Activator.Mode = ActivatorModes.DoublePress;
                macro.RunMode = TriggerRunMode.Hold;
                Call(form, "LoadSelectedMacroToUi");
                Check(modes.SelectedIndex == 6 && Field<Button>(form, "stepRunButton").Enabled,
                    "stored legacy fallback cannot disable single-step for a Double Press sequence");
                Localizer.SetLanguage(language == "zh-CN" ? "en-US" : "zh-CN");
                Call(form, "ApplyLanguageToMainUi", false);
                Check(modes.SelectedIndex == 6 && macro.Activator.Mode == ActivatorModes.DoublePress,
                    "language switching preserves the effective trigger policy");
                Localizer.SetLanguage(language);
                Call(form, "ApplyLanguageToMainUi", false);
                modes.SelectedIndex = 0;
                Check(modes.Text == TriggerEditor.Names[0], "selected trigger choice has readable text");
                Render(form, "main-" + language + "-" + size.Width + "-" + (int)(scale * 100));
                Check(All(form).OfType<ComboBox>().Count(x => x == modes) == 1 && modes.Items.Count == 7,
                    "editor presents one seven-mode trigger choice");
                Check(modes.Width > 180 && Field<Button>(form, "activatorConditionButton").Width >= 128, "trigger controls retain usable width");
                Call(form, "ToggleOptionalSettings", null, EventArgs.Empty);
                Render(form, "main-expanded-" + language + "-" + size.Width + "-" + (int)(scale * 100));
                Check(Field<TableLayoutPanel>(form, "optionalSettingsPanel").Visible, "optional grouping and target settings expand");
                Call(form, "ToggleOptionalSettings", null, EventArgs.Empty);
                Check(!Field<TableLayoutPanel>(form, "optionalSettingsPanel").Visible, "optional settings collapse");
            }
        }
    }

    private static void FriendlyInteractions()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            Localizer.SetLanguage(language);
            MacroConfig config = new MacroConfig { Language = language, GamepadDeviceType = VirtualGamepadTypes.None };
            config.Macros.Add(new MacroDefinition());
            using (MainForm form = new MainForm(config, Path.Combine(root, "friendly-" + Guid.NewGuid().ToString("N")), new Observer()))
            {
                Render(form, "empty-steps-" + language);
                Check(Field<Panel>(form, "emptyStepsPanel").Visible && !Field<Button>(form, "editStepButton").Enabled,
                    "empty macro offers a first action and disables editing a missing step");
                foreach (bool create in new[] { false, true })
                {
                    bool handled = false;
                    using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
                    {
                        timer.Interval = 50;
                        timer.Tick += delegate
                        {
                            NewMacroDialog chooser = Application.OpenForms.OfType<NewMacroDialog>().FirstOrDefault();
                            if (chooser == null) return;
                            timer.Stop();
                            Check(Field<bool>(form, "pauseHotkeys") && Field<int>(form, "uiSafetyModalDepth") > 0,
                                "new macro chooser pauses shortcuts while the modal is open");
                            handled = true;
                            if (create) All(chooser).OfType<Button>().Single(x => x.Name == "template3").PerformClick();
                            else { chooser.DialogResult = DialogResult.Cancel; chooser.Close(); }
                        };
                        timer.Start();
                        Field<Button>(form, "newMacroButton").PerformClick();
                    }
                    Check(handled && config.Macros.Count == (create ? 2 : 1), "chooser cancel/create uses the real main-window action");
                    Check(!Field<bool>(form, "pauseHotkeys") && Field<int>(form, "uiSafetyModalDepth") == 0,
                        "chooser restores shortcut state on cancel and create");
                }
                MacroDefinition saved = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(Field<string>(form, "appDir"), "config.xml")).Macros.Last();
                Check(saved.Steps.Count == 0 && saved.Name == config.Macros.Last().Name, "blank choice saves the ordinary compatible macro definition");
            }
            foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
            {
                using (NewMacroDialog chooser = new NewMacroDialog())
                {
                    chooser.Scale(new SizeF(scale, scale));
                    chooser.ClientSize = new Size((int)(800 * scale), (int)(570 * scale));
                    Render(chooser, "new-macro-" + language + "-" + (int)(scale * 100));
                    EditorTemplateCard[] cards = All(chooser).OfType<EditorTemplateCard>().ToArray();
                    Check(cards.Length == 4 && cards.All(x => x.TabStop && x.AccessibleDescription == x.Description && x.AccessibleName == x.Text),
                        "all starting points support keyboard focus and descriptive accessibility text");
                    foreach (EditorTemplateCard card in cards)
                    {
                        int bottom = card.Font.Height + 52;
                        Size measured = TextRenderer.MeasureText(card.Description, card.Font, new Size(card.Width - 36, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                        Check(measured.Height <= card.Height - bottom - 22, "template purpose fits inside its card at " + scale);
                    }
                }
                using (StepEditDialog editor = new StepEditDialog(null, new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F6, HoldMs = 11, DelayMs = 15 }))
                {
                    editor.Scale(new SizeF(scale, scale));
                    editor.ClientSize = new Size((int)(980 * scale), (int)(590 * scale));
                    Render(editor, "step-preview-" + language + "-" + (int)(scale * 100));
                    Label preview = Field<Label>(editor, "stepPreviewText");
                    Check(preview.Text.Contains("11") && preview.Text.Contains("15") && preview.Text.Contains("F6"), "step explanation reflects input and both timings");
                    Field<ComboBox>(editor, "actionBox").SelectedIndex = 1;
                    Check(!Field<NumericUpDown>(editor, "holdBox").Enabled && !preview.Text.Contains("11"), "held action explains release instead of irrelevant press duration");
                    Field<CheckBox>(editor, "randomDelayBox").Checked = true;
                    Field<NumericUpDown>(editor, "randomMinBox").Value = 32;
                    Field<NumericUpDown>(editor, "randomMaxBox").Value = 54;
                    Check(preview.Text.Contains("32–54"), "random waiting explanation updates immediately");
                    Button save = (Button)editor.AcceptButton;
                    Check(editor.RectangleToClient(save.RectangleToScreen(save.ClientRectangle)).Bottom <= editor.ClientSize.Height,
                        "step confirmation stays reachable at " + scale);
                }
            }
            foreach (QuickTemplate template in Enum.GetValues(typeof(QuickTemplate)))
            {
                using (QuickCreateDialog quick = new QuickCreateDialog(null, template))
                {
                    Render(quick, "quick-create-" + language + "-" + template);
                    Button create = (Button)quick.AcceptButton;
                    Check(!create.Enabled && Field<Label>(quick, "readyLabel").Text.Length > 10, "incomplete quick setup shows its next step");
                    quick.GetType().GetField("trigger", Private).SetValue(quick, new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 });
                    Call(quick, "UpdateTrigger");
                    if (template != QuickTemplate.HeldMapping)
                    {
                        Check(!create.Enabled, "shortcut alone does not finish a sequence");
                        Field<List<MacroStep>>(quick, "steps").Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, HoldMs = 18, DelayMs = 23 });
                        Call(quick, "RefreshActions");
                    }
                    Check(create.Enabled, "complete setup enables Create");
                    Call(quick, "CreateMacro", null, EventArgs.Empty);
                    Check(quick.Result != null && quick.Result.Trigger.VirtualKey == (int)Keys.F8 && quick.Result.Steps.Count == 1,
                        "each quick starting point creates an editable ordinary definition");
                }
            }
        }
    }

    private static void Soak(int minutes)
    {
        if (minutes < 1) throw new ArgumentOutOfRangeException("minutes");
        string directory = Path.Combine(root, "soak-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss"));
        Directory.CreateDirectory(directory);
        MacroConfig config = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition continuous = Timed("continuous", Keys.F6, true);
        MacroDefinition finite = Timed("repeated-start-stop", Keys.F7, false);
        MacroDefinition held = QuickMacros.Create(QuickTemplate.HeldMapping, "held-stick", new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.LeftStick, GamepadY = 80 } }, 1);
        config.Macros.Add(continuous); config.Macros.Add(finite); config.Macros.Add(held);
        Observer observer = new Observer();
        Stopwatch clock = Stopwatch.StartNew();
        Process process = Process.GetCurrentProcess();
        long cycles = 0, baselineBytes = 0, maxBytes = 0, maxPumpMs = 0;
        int baselineHandles = 0, maxHandles = 0, baselineThreads = 0, maxThreads = 0;
        List<string> samples = new List<string> { "elapsed_seconds,cycles,private_bytes,handles,threads,output_downs,output_ups,cpu_milliseconds" };
        using (MainForm form = new MainForm(config, directory, observer))
        {
            IntPtr handle = form.Handle;
            Call(form, "StartParallelHeldMapping", held);
            Call(form, "StartMacro", continuous, 0, null, false, false);
            Wait(delegate { return observer.Downs > 0; }, "continuous run starts", 2000);
            long nextSample = 0;
            while (clock.Elapsed.TotalMinutes < minutes)
            {
                Call(form, "StartMacro", finite, 0, null, false, false);
                Wait(delegate { return !(bool)Call(form, "IsMacroActuallyRunning", finite); }, "finite run completes during soak", 2000);
                cycles++;
                Check(Field<OutputOwnershipManager>(form, "outputOwnership").Snapshot().Merged.Any(x =>
                    x.Kind == InputKind.Gamepad && x.GamepadControl == GamepadControl.LeftStick && x.GamepadY == 80),
                    "finite run completion preserves sustained held output");
                if (cycles % 5 == 0)
                {
                    Call(form, "StopMacro", continuous, "soak-stop");
                    Call(form, "StopParallelHeldMapping", held, "soak-stop");
                    Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, "continuous stop completes", 2000);
                    Check(observer.Pressed == 0 && Field<OutputOwnershipManager>(form, "outputOwnership").Snapshot().Merged.Count == 0,
                        "stop/restart boundary has no residual output");
                    Call(form, "StartMacro", continuous, 0, null, false, false);
                    Call(form, "StartParallelHeldMapping", held);
                }
                if (cycles % 100 == 0)
                {
                    Field<TextBox>(form, "descriptionBox").Text = "soak cycle " + cycles;
                    Check((bool)Call(form, "SaveConfig"), "configuration remains writable during sustained execution");
                    Check(ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml")).Macros.Count == 3,
                        "saved configuration remains readable during sustained execution");
                }
                Stopwatch pump = Stopwatch.StartNew();
                bool dispatched = false;
                form.BeginInvoke((MethodInvoker)delegate { dispatched = true; });
                Wait(delegate { return dispatched; }, "UI message is dispatched", 2000);
                maxPumpMs = Math.Max(maxPumpMs, pump.ElapsedMilliseconds);
                Call(form, "UpdateRunButton");
                if (clock.ElapsedMilliseconds >= nextSample)
                {
                    process.Refresh();
                    if (baselineBytes == 0 && clock.Elapsed.TotalSeconds >= 30)
                    { GC.Collect(); GC.WaitForPendingFinalizers(); process.Refresh(); baselineBytes = process.PrivateMemorySize64; baselineHandles = process.HandleCount; baselineThreads = process.Threads.Count; }
                    maxBytes = Math.Max(maxBytes, process.PrivateMemorySize64);
                    maxHandles = Math.Max(maxHandles, process.HandleCount);
                    maxThreads = Math.Max(maxThreads, process.Threads.Count);
                    samples.Add(string.Join(",", new[] { clock.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture), cycles.ToString(),
                        process.PrivateMemorySize64.ToString(), process.HandleCount.ToString(), process.Threads.Count.ToString(), observer.Downs.ToString(), observer.Ups.ToString(),
                        process.TotalProcessorTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) }));
                    File.WriteAllLines(Path.Combine(directory, "samples.csv"), samples);
                    File.WriteAllText(Path.Combine(directory, "progress.json"), "{\"elapsedSeconds\":" + clock.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + ",\"cycles\":" + cycles + "}");
                    nextSample = clock.ElapsedMilliseconds + 10000;
                }
                Pump(20);
            }
            Call(form, "EmergencyStop", "soak-finish");
            Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, "final emergency stop completes", 3000);
            Check(observer.Pressed == 0 && Field<OutputOwnershipManager>(form, "outputOwnership").Snapshot().Sources.Count == 0, "soak final state has zero sources and held outputs");
            Check(observer.KeyboardDowns == observer.KeyboardUps, "every keyboard down has a matching release");
            Check(observer.Downs > cycles * 2 && observer.MaxContinuousGapMs < 2000, "continuous output remains active without a 2-second gap");
            GC.Collect(); GC.WaitForPendingFinalizers(); process.Refresh();
            Check(baselineBytes > 0 && process.PrivateMemorySize64 <= baselineBytes + 128L * 1024 * 1024, "private memory growth stays below 128 MiB after warmup");
            Check(process.HandleCount <= baselineHandles + 32, "final handle growth stays below 32");
            Check(process.Threads.Count <= baselineThreads + 12, "final thread growth stays below 12");
            Check(maxPumpMs < 2000, "UI message dispatch stays below 2 seconds");
            string report = "{\"status\":\"PASS\",\"version\":\"" + AppInfo.Version + "\",\"durationSeconds\":" + clock.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) +
                ",\"cycles\":" + cycles + ",\"outputDowns\":" + observer.Downs + ",\"outputUps\":" + observer.Ups +
                ",\"keyboardDowns\":" + observer.KeyboardDowns + ",\"keyboardUps\":" + observer.KeyboardUps + ",\"maxContinuousGapMs\":" + observer.MaxContinuousGapMs +
                ",\"cpuMilliseconds\":" + process.TotalProcessorTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) +
                ",\"baselinePrivateBytes\":" + baselineBytes + ",\"finalPrivateBytes\":" + process.PrivateMemorySize64 + ",\"peakPrivateBytes\":" + maxBytes +
                ",\"baselineHandles\":" + baselineHandles + ",\"finalHandles\":" + process.HandleCount + ",\"peakHandles\":" + maxHandles +
                ",\"baselineThreads\":" + baselineThreads + ",\"finalThreads\":" + process.Threads.Count + ",\"peakThreads\":" + maxThreads +
                ",\"maxUiDispatchMs\":" + maxPumpMs + ",\"remainingOutputSources\":0,\"remainingPressedOutputs\":0,\"backend\":\"isolated observer\"}";
            File.WriteAllText(Path.Combine(directory, "report.json"), report, Encoding.UTF8);
            Console.WriteLine("SOAK REPORT " + Path.Combine(directory, "report.json"));
        }
    }
}
