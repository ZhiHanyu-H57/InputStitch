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
    private static bool densityUsesNativeDpi;
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, IntPtr state);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    private static void SyntheticViewport(Form form, Size requested)
    {
        // Form.Size itself clamps to MaxWindowTrackSize even for hidden forms.
        // Native sizing lets bitmap tests cover the stated DPI viewport without
        // changing desktop resolution or weakening any ancestor-clipping check.
        form.MaximumSize = requested;
        if (!SetWindowPos(form.Handle, IntPtr.Zero, -20000, -20000, requested.Width, requested.Height, 0x14))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
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
        internal long FiniteDowns, LastFiniteDownAt;
        private long lastContinuousDown;
        private static string Key(InputSpec value) { return value.Kind + ":" + value.VirtualKey + ":" + value.GamepadControl; }
        public void SendDown(InputSpec value)
        {
            lock (sync)
            {
                Downs++; pressed.Add(Key(value));
                if (value.Kind == InputKind.Keyboard) KeyboardDowns++;
                if (value.Kind == InputKind.Keyboard && value.VirtualKey == (int)Keys.F7)
                {
                    Interlocked.Exchange(ref LastFiniteDownAt, Stopwatch.GetTimestamp());
                    Interlocked.Increment(ref FiniteDowns);
                }
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
        foreach (Control child in All(form)) child.CreateControl();
        form.PerformLayout();
        Pump(20);
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            form.PerformLayout(); Pump(20);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(root, name + ".png"));
        }
    }
    private static IEnumerable<Control> All(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (Control next in All(child)) yield return next; }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        bool nativeJourney = args.Length > 0 && args[0] == "--journey-native";
        bool nativeFields = args.Length > 0 && args[0] == "--fields-native";
        bool nativeDensity = args.Length > 0 && args[0] == "--density-native";
        densityUsesNativeDpi = nativeDensity;
        if (nativeDensity || nativeJourney || nativeFields) SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles();
        EmbeddedDependencyLoader.Register();
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "product-acceptance");
        Directory.CreateDirectory(root);
        try
        {
            if (args.Length >= 2 && args[0] == "--soak-minutes") Soak(int.Parse(args[1], CultureInfo.InvariantCulture));
            else if (nativeDensity)
            {
                using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero))
                { Console.WriteLine("NATIVE-DPI " + screen.DpiX); Density(screen.DpiX / 96F); }
                DefaultViewport();
            }
            else if (args.Length > 0 && args[0] == "--density") Density();
            else if (args.Length > 0 && args[0] == "--layout") ModesAndLayout();
            else if (args.Length > 0 && (args[0] == "--fields" || nativeFields))
            {
                using(Graphics screen=Graphics.FromHwnd(IntPtr.Zero)) Console.WriteLine("FIELDS DPI="+screen.DpiX);
                BoundedFields();
            }
            else if (args.Length > 0 && (args[0] == "--journey" || nativeJourney))
            {
                using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) Console.WriteLine("JOURNEY DPI=" + screen.DpiX);
                BeginnerWorkflow(); QuickShortcutCapture(); StepInputMethods();
            }
            else if (args.Length > 0 && args[0] == "--dialogs") { RemainingDialogs(); NewUserJourney(); BeginnerWorkflow(); QuickShortcutCapture(); }
            else { Workflow(); ModesAndLayout(); Density(); FriendlyInteractions(); RemainingDialogs(); NewUserJourney(); BeginnerWorkflow(); QuickShortcutCapture(); StepInputMethods(); BoundedFields(); }
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
            Check(Field<Button>(form, "triggerButton").Text == (macro.Trigger == null
                ? "选择触发键…" : InputNames.FormatTrigger(macro.Trigger)), "single shortcut selector shows the saved key or choose prompt");
            NumericUpDown repeats = Field<NumericUpDown>(form, "repeatBox");
            CheckBox infinite = Field<CheckBox>(form, "infiniteBox");
            repeats.Value = 17;
            infinite.Checked = true;
            Check(!repeats.Enabled && repeats.Value == 17 && macro.RepeatCount == 17 && macro.Infinite,
                "infinite repeat disables the adjacent count without discarding it");
            infinite.Checked = false;
            Check(repeats.Enabled && repeats.Value == 17 && macro.RepeatCount == 17 && !macro.Infinite,
                "finite repeat restores the saved count");
            repeats.Value = 1;
            macro.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8, Ctrl = true, Alt = true, Shift = true, Win = true };
            Call(form, "LoadSelectedMacroToUi");
            Check(Field<Button>(form, "triggerButton").Text == InputNames.FormatTrigger(macro.Trigger), "selector displays a complete modifier chord");
            Call(form, "CaptureTriggerButton_Click", null, EventArgs.Empty);
            Check(Field<Button>(form, "triggerButton").Text == "请按触发键…", "physical capture replaces selector text with its prompt");
            form.CancelCapture();
            Check(Field<Button>(form, "triggerButton").Text == InputNames.FormatTrigger(macro.Trigger), "cancel restores the actual saved shortcut");
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
                // Synthetic viewports are rendered offscreen. Keep the real
                // first-show work-area fitting in DefaultViewport's native test.
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-20000, -20000);
                form.Scale(new SizeF(scale, scale));
                Size requested = new Size((int)(size.Width * scale), (int)(size.Height * scale));
                SyntheticViewport(form, requested);
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
                Check(form.Size == requested, "synthetic DPI viewport uses its requested size: " + requested + "; actual=" + form.Size);
                Check(All(form).OfType<ComboBox>().Count(x => x == modes) == 1 && modes.Items.Count == 7,
                    "editor presents one seven-mode trigger choice");
                CompactTriggerLayout(form);
                Call(form, "ToggleOptionalSettings", null, EventArgs.Empty);
                Render(form, "main-expanded-" + language + "-" + size.Width + "-" + (int)(scale * 100));
                Check(Field<TableLayoutPanel>(form, "morePanel").Visible, "optional grouping and target settings expand");
                Call(form, "ToggleOptionalSettings", null, EventArgs.Empty);
                Check(!Field<TableLayoutPanel>(form, "morePanel").Visible, "optional settings collapse");
            }
        }
    }

    // Larger synthetic windows exceed the test desktop's track limit; native 144 DPI is checked in its own process.
    private static void Density() { foreach (float scale in new[] { 1F, 1.25F }) Density(scale); }
    private static void Density(float scale)
    {
        ResponsiveTransitions(scale);
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (Size size in new[] { new Size(1000, 700), new Size(1160, 780), new Size(1280, 900) })
        {
            MacroConfig config = new MacroConfig { Language = language, GamepadDeviceType = VirtualGamepadTypes.None };
            MacroDefinition macro = Timed("常用操作 · Example", Keys.F8, false);
            for (int i = 1; i < 16; i++) macro.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, HoldMs = 45, DelayMs = 120 });
            config.Macros.Add(macro);
            using (MainForm form = new MainForm(config, Path.Combine(root, "density-" + Guid.NewGuid().ToString("N")), new Observer()))
            {
                form.StartPosition = FormStartPosition.Manual;
                if (!densityUsesNativeDpi) form.Scale(new SizeF(scale, scale));
                Size requested = new Size((int)(size.Width * scale), (int)(size.Height * scale));
                if (densityUsesNativeDpi) form.Size = requested; else SyntheticViewport(form, requested);
                Render(form, "density-" + language + "-" + size.Width + "-" + (int)(scale * 100));
                CompactTriggerLayout(form);
                DataGridView grid = Field<DataGridView>(form, "grid");
                Rectangle bounds = form.RectangleToClient(grid.RectangleToScreen(grid.ClientRectangle));
                Check(bounds.Top >= 0 && bounds.Bottom <= form.ClientSize.Height && bounds.Right <= form.ClientSize.Width,
                    "steps remain inside the initial viewport without dragging or scrolling: " + size + "; grid=" + bounds);
                Check(All(form).OfType<Panel>().Where(x => x.AutoScroll && x.Contains(grid)).All(x => !x.VerticalScroll.Visible && !x.HorizontalScroll.Visible),
                    "ordinary editor has no outer scrolling at " + size);
                Check(!All(grid).OfType<HScrollBar>().Any(x => x.Visible), "all step columns fit without horizontal scrolling at " + size);
                int rows = grid.DisplayedRowCount(false);
                Console.WriteLine("DENSITY " + language + " " + size.Width + "x" + size.Height + " scale=" + scale + " window=" + form.Size + " rows=" + rows + " grid=" + grid.ClientSize);
                for (Control parent = grid.Parent; parent != form; parent = parent.Parent)
                    Check(parent.RectangleToClient(grid.RectangleToScreen(grid.ClientRectangle)).Bottom <= parent.ClientSize.Height,
                        "steps are not clipped by " + parent.GetType().Name + " at " + size);
                Check(rows >= (size.Height >= 780 ? 7 : 5), "useful step rows are visible initially: " + rows + " at " + size);
                Check(grid.Rows.Cast<DataGridViewRow>().All(x => x.Height >= grid.Font.Height + grid.DefaultCellStyle.Padding.Vertical),
                    "every step row fits its text and padding without crowding");
                IEnumerable<Button> actions = Field<Button>(form, "addStepButton").Parent.Controls.OfType<Button>()
                    .Concat(new[] { Field<Button>(form, "runButton"), Field<Button>(form, "stepRunButton"), Field<Button>(form, "recordButton") });
                foreach (Button button in actions)
                {
                    Rectangle action = form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                    Check(action.Bottom <= form.ClientSize.Height && action.Right <= form.ClientSize.Width,
                        "normal actions remain visible without expanding or scrolling: " + button.Text);
                    for (Control parent = button.Parent; parent != form; parent = parent.Parent)
                        Check(parent.ClientRectangle.Contains(parent.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))),
                            "action is not clipped by its container: " + button.Text);
                }
            }
        }
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (QuickTemplate template in new[] { QuickTemplate.Repeat, QuickTemplate.Sequence })
        {
            Localizer.SetLanguage(language);
            using (QuickCreateDialog quick = new QuickCreateDialog(null, template))
            {
                List<MacroStep> steps = Field<List<MacroStep>>(quick, "steps");
                for (int i = 0; i < 16; i++) steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, HoldMs = 45, DelayMs = 120 });
                Call(quick, "RefreshActions");
                if (!densityUsesNativeDpi) quick.Scale(new SizeF(scale, scale));
                else quick.ClientSize = new Size((int)(900 * scale), (int)(650 * scale));
                Render(quick, "density-actions-" + language + "-" + template + "-" + (int)(scale * 100));
                ListBox actions = Field<ListBox>(quick, "stepList");
                int rows = actions.ClientSize.Height / actions.ItemHeight;
                Console.WriteLine("ACTIONS " + language + " " + template + " rows=" + rows + " itemHeight=" + actions.ItemHeight + " fontHeight=" + actions.Font.Height);
                Check(rows >= 6 && actions.ItemHeight >= actions.Font.Height + 6, "quick action list has readable separated rows and useful initial capacity");
                Check(actions.GetItemRectangle(0).Height >= actions.Font.Height + 6, "native list row height matches the font instead of squeezing text");
                Check(actions.Items[0].ToString().Contains("45 ms") && actions.Items[0].ToString().Contains("120 ms"), "timing is visible directly in the action list");
                Rectangle visible = quick.RectangleToClient(actions.RectangleToScreen(actions.ClientRectangle));
                Check(visible.Bottom <= quick.ClientSize.Height, "quick actions stay within the initial viewport");
                Check(!All(quick).OfType<Panel>().Any(x => x.AutoScroll && x.VerticalScroll.Visible), "quick creation needs no outer scrolling at default size");
            }
        }
    }

    private static void ResponsiveTransitions(float scale)
    {
        MacroConfig config = new MacroConfig { Language = "zh-CN", GamepadDeviceType = VirtualGamepadTypes.None };
        MacroDefinition macro = Timed("调整窗口 · Resize", Keys.F8, false);
        config.Macros.Add(macro);
        using (MainForm form = new MainForm(config, Path.Combine(root, "resize-" + Guid.NewGuid().ToString("N")), new Observer()))
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            if (!densityUsesNativeDpi) form.Scale(new SizeF(scale, scale));
            form.Show(); Pump(25);
            foreach (string language in new[] { "zh-CN", "en-US", "zh-CN" })
            foreach (Size size in new[] { new Size(1000, 700), new Size(1280, 900), new Size(1000, 700) })
            {
                Localizer.SetLanguage(language);
                Call(form, "ApplyLanguageToMainUi", false);
                macro.Trigger = size.Width == 1280 ? null : new TriggerSpec { Kind = InputKind.Keyboard,
                    VirtualKey = (int)Keys.F8, Ctrl = true, Alt = true, Shift = true, Win = true };
                Call(form, "LoadSelectedMacroToUi");
                Size requested = new Size((int)(size.Width * scale), (int)(size.Height * scale));
                if (densityUsesNativeDpi) form.Size = requested; else SyntheticViewport(form, requested);
                Pump(25);
                CompactTriggerLayout(form);
                Check(Field<Button>(form, "triggerButton").Text == (macro.Trigger == null
                    ? (language == "en-US" ? "Choose shortcut…" : "选择触发键…") : InputNames.FormatTrigger(macro.Trigger)),
                    "live resize and language switch retain the current key or empty prompt");
            }
            form.Close();
        }
    }

    private static void CompactTriggerLayout(MainForm form)
    {
        ComboBox modes = Field<ComboBox>(form, "triggerModeBox");
        Button shortcut = Field<Button>(form, "triggerButton");
        NumericUpDown repeats = Field<NumericUpDown>(form, "repeatBox");
        CheckBox infinite = Field<CheckBox>(form, "infiniteBox");
        float scale = form.Font.GetHeight() / form.Font.GetHeight(96F);
        int longest = TriggerEditor.Names.Max(x => TextRenderer.MeasureText(x, modes.Font).Width);
        Check(modes.Width >= longest + 20, "all translated trigger choices fit the closed selector");
        Check(modes.Width <= longest + 85 * scale, "trigger mode uses a measured compact width instead of stretching");
        Check(shortcut.Width >= TextRenderer.MeasureText(shortcut.Text, shortcut.Font).Width + 8,
            "the selected keyboard shortcut is readable in its single control");
        Check(shortcut.Width <= 330 * scale, "shortcut selection does not stretch across the settings card");
        Check(repeats.Parent == infinite.Parent && !((FlowLayoutPanel)repeats.Parent).WrapContents,
            "count, unit and infinite option form one indivisible row");
        Check(infinite.Left > repeats.Right && infinite.Left - repeats.Right < 85 * scale,
            "infinite option is immediately beside the count and unit");
        Check(Math.Abs(repeats.Top + repeats.Height / 2 - infinite.Top - infinite.Height / 2) <= 2,
            "count and infinite option share a visual center");
        Check(repeats.Width >= TextRenderer.MeasureText(repeats.Maximum.ToString(), repeats.Font).Width + 16,
            "count field fits its supported maximum and spinner");
        foreach (Control control in new Control[] { shortcut, modes, repeats, infinite,
            Field<Button>(form, "activatorConditionButton"), Field<Button>(form, "moreButton"),
            Field<Label>(form, "runtimeLabel") })
        {
            for (Control parent = control.Parent; parent != form; parent = parent.Parent)
                Check(parent.ClientRectangle.Contains(parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))),
                    "compact group remains visible inside " + parent.GetType().Name + ": " + control.Text +
                    "; control=" + control.Bounds + "; parent=" + parent.ClientRectangle + "; visible=" + parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle)));
        }
        Control triggerFlow = modes.Parent.Parent;
        Control[] groups = triggerFlow.Controls.Cast<Control>().Where(x => x.Visible).ToArray();
        for (int i = 0; i < groups.Length; i++)
            for (int j = i + 1; j < groups.Length; j++)
                Check(!groups[i].Bounds.IntersectsWith(groups[j].Bounds), "wrapped trigger groups never overlap");
        Label readiness = Field<Label>(form, "runtimeLabel");
        Check(readiness.Height >= TextRenderer.MeasureText(readiness.Text, readiness.Font,
            new Size(readiness.Width - readiness.Padding.Horizontal, 1000), TextFormatFlags.WordBreak).Height,
            "readiness and error explanations remain directly visible");
    }

    private static void DefaultViewport()
    {
        MacroConfig config = new MacroConfig { Language = "zh-CN", GamepadDeviceType = VirtualGamepadTypes.None };
        config.Macros.Add(Timed("启动布局", Keys.F8, false));
        using (MainForm form = new MainForm(config, Path.Combine(root, "initial-viewport-" + Guid.NewGuid().ToString("N")), new Observer()))
        {
            form.Show(); Pump(50);
            Check(Screen.FromControl(form).WorkingArea.Contains(form.Bounds), "default window fits the actual work area without manual resizing");
            DataGridView grid = Field<DataGridView>(form, "grid");
            Check(form.ClientRectangle.Contains(form.RectangleToClient(grid.RectangleToScreen(grid.ClientRectangle))), "default visible step area is inside the window");
            Render(form, "density-native-default-startup");
            Console.WriteLine("INITIAL-VIEWPORT window=" + form.Size + " rows=" + grid.DisplayedRowCount(false));
            form.Close();
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
                Check(!Field<DataGridView>(form, "grid").Visible, "empty-step guidance and the table never draw over each other");
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

    private static void FooterVisible(Form form)
    {
        Control shell = All(form).Single(x => x.Name == "dialogShell");
        Control footer = ((TableLayoutPanel)shell).GetControlFromPosition(0, 2);
        foreach (Button button in All(footer).OfType<Button>().Where(x => x.Visible))
        {
            Rectangle bounds = form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
            Check(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= form.ClientSize.Width && bounds.Bottom <= form.ClientSize.Height,
                form.GetType().Name + " footer reachable: " + button.Text);
        }
    }

    private static void RemainingDialogs()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            Localizer.SetLanguage(language);
            foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
            {
                MacroConfig originalSettings = new MacroConfig { Language = language };
                using (SettingsDialog settings = new SettingsDialog(originalSettings))
                {
                    if (scale == 1F)
                    {
                        TabControl fullTabs = All(settings).OfType<TabControl>().Single();
                        for (int tab = 0; tab < fullTabs.TabPages.Count; tab++) { fullTabs.SelectedIndex = tab; Render(settings, "settings-full-" + language + "-" + tab); }
                    }
                    settings.Scale(new SizeF(scale, scale));
                    settings.ClientSize = new Size((int)(604 * scale), (int)(441 * scale));
                    TabControl tabs = All(settings).OfType<TabControl>().Single();
                    for (int i = 0; i < tabs.TabPages.Count; i++)
                    {
                        tabs.SelectedIndex = i;
                        Render(settings, "settings-unified-" + language + "-" + i + "-" + (int)(scale * 100));
                        FooterVisible(settings);
                    }
                    Field<CheckBox>(settings, "topMostBox").Checked = true;
                    Check(settings.KeepWindowTopMost && !originalSettings.KeepWindowTopMost, "settings stage changes until Save; cancel keeps the original configuration");
                    Field<CheckBox>(settings, "sdl3InputBox").Checked = false;
                    All(settings).OfType<Button>().Single(x => x.Text == Localizer.T("恢复默认")).PerformClick();
                    Check(!settings.KeepWindowTopMost && settings.Sdl3InputEnabled == new MacroConfig().Sdl3InputEnabled, "Restore defaults includes optional controller input");
                    settings.DialogResult = DialogResult.Cancel;
                }
                foreach (int mode in new[] { 0, 4, 5, 6 })
                using (ActivatorConditionDialog dialog = new ActivatorConditionDialog(new MacroDefinition { Activator = new ActivatorConfig { Mode = mode == 0 ? ActivatorModes.Legacy : mode == 4 ? ActivatorModes.ShortPress : mode == 5 ? ActivatorModes.LongPress : ActivatorModes.DoublePress } }, null, "Base"))
                {
                    if (scale == 1F) Render(dialog, "conditions-full-" + language + "-" + mode);
                    dialog.Scale(new SizeF(scale, scale));
                    dialog.ClientSize = new Size((int)(700 * scale), (int)(590 * scale));
                    Render(dialog, "conditions-unified-" + language + "-" + mode + "-" + (int)(scale * 100));
                    Check(Field<NumericUpDown>(dialog, "shortPress").Visible == (mode == 4) && Field<NumericUpDown>(dialog, "longPress").Visible == (mode == 5) && Field<NumericUpDown>(dialog, "doubleWindow").Visible == (mode == 6),
                        "only timing used by the selected trigger is presented");
                    Check(!Field<ComboBox>(dialog, "analogControl").Visible, "unused analog limits are collapsed");
                    Field<CheckBox>(dialog, "analogEnabled").Checked = true;
                    Check(Field<ComboBox>(dialog, "analogControl").Visible, "enabling an analog limit reveals its controls");
                    Field<TextBox>(dialog, "process").Text = "game.exe";
                    Call(dialog, "CaptureValues");
                    Check(dialog.SelectedConditions.ForegroundProcessName == "game.exe" && dialog.SelectedConditions.Analog.Enabled, "condition edits survive presentation changes");
                    FooterVisible(dialog);
                }
                using (VirtualKeyboardDialog keyboard = new VirtualKeyboardDialog(true, null))
                {
                    if (scale == 1F) Render(keyboard, "keyboard-full-" + language);
                    keyboard.Scale(new SizeF(scale, scale));
                    keyboard.ClientSize = new Size((int)(1000 * scale), (int)(620 * scale));
                    Render(keyboard, "keyboard-unified-" + language + "-" + (int)(scale * 100));
                    Button use = (Button)keyboard.AcceptButton;
                    Check(!use.Enabled, "empty key selection cannot be confirmed");
                    Field<RadioButton>(keyboard, "multipleButton").Checked = true;
                    PressKey(keyboard, Keys.A, false); PressKey(keyboard, Keys.B, false);
                    Check(!use.Enabled && Field<Label>(keyboard, "selectionLabel").ForeColor != EditorTheme.Accent,
                        "invalid trigger combinations are explained before confirmation");
                    PressKey(keyboard, Keys.B, false); PressKey(keyboard, Keys.LControlKey, false);
                    Check(use.Enabled, "a modifier plus one main key enables confirmation");
                    FooterVisible(keyboard);
                    Call(keyboard, "ConfirmSelection", null, EventArgs.Empty);
                    Check(keyboard.SelectedTrigger != null && keyboard.SelectedTrigger.Ctrl && keyboard.SelectedTrigger.VirtualKey == (int)Keys.A,
                        "confirmed key selection preserves trigger semantics");
                }
            }
            using (BatchDelayDialog delay = new BatchDelayDialog())
            {
                Render(delay, "batch-wait-" + language);
                Field<RadioButton>(delay, "randomMode").Checked = true;
                Field<NumericUpDown>(delay, "minBox").Value = 90;
                Check(!((Button)delay.AcceptButton).Enabled, "invalid batch wait is explained without a popup");
                Field<NumericUpDown>(delay, "maxBox").Value = 110;
                Check(((Button)delay.AcceptButton).Enabled, "correcting batch wait restores confirmation");
                FooterVisible(delay);
                ((Button)delay.AcceptButton).PerformClick();
                Check(delay.UseRandom && delay.MinMs == 90 && delay.MaxMs == 110, "batch wait confirmation commits the chosen range");
            }
            using (MacroExportSelectionForm export = new MacroExportSelectionForm(new List<MacroDefinition> { new MacroDefinition { Name = "Export example" } }, 0))
            {
                Render(export, "export-unified-" + language); FooterVisible(export);
                CheckedListBox list = Field<CheckedListBox>(export, "list");
                list.SetItemChecked(0, false); Check(!((Button)export.AcceptButton).Enabled, "empty export selection cannot be confirmed");
                list.SetItemChecked(0, true); ((Button)export.AcceptButton).PerformClick();
                Check(export.SelectedIndices.SequenceEqual(new[] { 0 }), "export still returns the checked macro indices");
            }
            using (RouterSourceDialog router = new RouterSourceDialog(new RouterSourcePolicyConfig(), null)) { Render(router, "router-unified-" + language); FooterVisible(router); }
            using (AnalogTransformDialog analog = new AnalogTransformDialog(new AnalogTransformProfile())) { Render(analog, "analog-unified-" + language); FooterVisible(analog); }
            using (DeviceManagerDialog devices = new DeviceManagerDialog(null)) { Render(devices, "devices-unified-" + language); FooterVisible(devices); }
            using (ControlledReplacementDialog takeover = new ControlledReplacementDialog(null, delegate { return -1; }, delegate { return false; }, delegate { return VirtualGamepadTypes.None; }, delegate { return false; }, delegate { return false; }, null, null))
            { Render(takeover, "takeover-unified-" + language); FooterVisible(takeover); }
            using (AboutForm about = new AboutForm()) { Render(about, "about-unified-" + language); FooterVisible(about); }
            using (DiagnosticsForm diagnostics = new DiagnosticsForm(delegate { return "Isolated diagnostic example"; })) { Render(diagnostics, "diagnostics-unified-" + language); FooterVisible(diagnostics); }
        }
    }

    private static void PressKey(VirtualKeyboardDialog keyboard, Keys key, bool extended)
    {
        Field<List<Button>>(keyboard, "keyButtons").Single(x => ((InputSpec)x.Tag).VirtualKey == (int)key && ((InputSpec)x.Tag).Extended == extended).PerformClick();
    }

    private static void NewUserJourney()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            MacroConfig config = new MacroConfig { Language = language, GamepadDeviceType = VirtualGamepadTypes.None };
            string directory = Path.Combine(root, "new-user-" + Guid.NewGuid().ToString("N"));
            Observer observer = new Observer();
            using (MainForm form = new MainForm(config, directory, observer))
            using (System.Windows.Forms.Timer driver = new System.Windows.Forms.Timer())
            {
                Render(form, "new-user-start-" + language);
                int stage = 0;
                driver.Interval = 40;
                driver.Tick += delegate
                {
                    Form top = Application.OpenForms.Cast<Form>().LastOrDefault(x => x != form);
                    NewMacroDialog chooser = top as NewMacroDialog;
                    QuickCreateDialog quick = top as QuickCreateDialog;
                    StepEditDialog editor = top as StepEditDialog;
                    VirtualKeyboardDialog keyboard = top as VirtualKeyboardDialog;
                    if (stage == 0 && chooser != null)
                    {
                        stage++;
                        chooser.BeginInvoke((MethodInvoker)delegate { All(chooser).OfType<Button>().Single(x => x.Name == "template1").PerformClick(); });
                    }
                    else if (stage == 1 && quick != null)
                    {
                        Field<TextBox>(quick, "nameBox").Text = "First macro";
                        stage++;
                        quick.BeginInvoke((MethodInvoker)delegate { Field<Button>(quick, "triggerButton").PerformClick(); });
                    }
                    else if (stage == 2 && keyboard != null)
                    { PressKey(keyboard, Keys.F8, false); stage++; ((Button)keyboard.AcceptButton).PerformClick(); }
                    else if (stage == 3 && quick != null)
                    {
                        stage++;
                        quick.BeginInvoke((MethodInvoker)delegate { All(quick).OfType<Button>().Single(x => x.Name == "addActionButton").PerformClick(); });
                    }
                    else if (stage == 4 && editor != null)
                    {
                        stage++;
                        editor.BeginInvoke((MethodInvoker)delegate { Field<Button>(editor, "captureButton").PerformClick(); });
                    }
                    else if (stage == 5 && keyboard != null)
                    { PressKey(keyboard, Keys.F6, false); stage++; ((Button)keyboard.AcceptButton).PerformClick(); }
                    else if (stage == 6 && editor != null)
                    {
                        Field<NumericUpDown>(editor, "holdBox").Value = 11;
                        Field<NumericUpDown>(editor, "delayBox").Value = 15;
                        stage++; ((Button)editor.AcceptButton).PerformClick();
                    }
                    else if (stage == 7 && quick != null)
                    {
                        Check(((Button)quick.AcceptButton).Enabled, "first-time setup becomes ready after choosing shortcut and action");
                        stage++; driver.Stop(); ((Button)quick.AcceptButton).PerformClick();
                    }
                };
                driver.Start();
                Field<Button>(form, "newMacroButton").PerformClick();
                Check(stage == 8, "the actual nested creation, key selection and step windows complete");
                Check(config.Macros.Count == 1 && config.Macros[0].Trigger.VirtualKey == (int)Keys.F8 && config.Macros[0].Steps[0].VirtualKey == (int)Keys.F6,
                    "new user reaches a configured macro using only visible controls");
                Check(Field<Label>(form, "workflowGuide").Text.Length > 40, "created macro explains saving and where to run");
                MacroConfig saved = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml"));
                Check(saved.Macros[0].Steps[0].HoldMs == 11 && saved.Macros[0].Steps[0].DelayMs == 15, "complete modal journey is saved without an extra save step");
                Field<Button>(form, "runButton").PerformClick();
                Wait(delegate { return observer.Downs > 0; }, "Run button executes the newly created macro", 2000);
                Wait(delegate { return !(bool)Call(form, "HasWorkerBackedRuns"); }, "first run completes", 3000);
                Check(observer.Pressed == 0 && observer.KeyboardDowns == observer.KeyboardUps, "first run leaves no pressed key");
                Field<CheckBox>(form, "enabledBox").Checked = false;
                Check(Field<Label>(form, "workflowGuide").Text.Contains(Localizer.IsEnglish ? "enable" : "启用"), "guidance follows shortcut enable/disable edits");
                Field<CheckBox>(form, "enabledBox").Checked = true;
                using (System.Windows.Forms.Timer nameDriver = new System.Windows.Forms.Timer())
                {
                    nameDriver.Interval = 40;
                    nameDriver.Tick += delegate
                    {
                        Form prompt = Application.OpenForms.Cast<Form>().SingleOrDefault(x => x.Text == "Test group");
                        if (prompt == null) return;
                        nameDriver.Stop();
                        Check(Field<int>(form, "uiSafetyModalDepth") > 0, "group name editing retains modal output protection");
                        All(prompt).OfType<TextBox>().Single().Text = "My group";
                        Render(prompt, "group-name-" + language); FooterVisible(prompt);
                        ((Button)prompt.AcceptButton).PerformClick();
                    };
                    nameDriver.Start();
                    Check((string)Call(form, "PromptLayerName", "Test group", "") == "My group" && Field<int>(form, "uiSafetyModalDepth") == 0,
                        "group name editing returns its value and restores modal protection");
                }
                Render(form, "new-user-complete-" + language);
            }
        }
    }

    private static void BoundedFields()
    {
        foreach(string language in new[]{"zh-CN","en-US"})
        {
            MacroConfig config=new MacroConfig { Language=language, GamepadDeviceType=VirtualGamepadTypes.None };
            MacroDefinition macro=Timed("Compact fields",Keys.F8,false); config.Macros.Add(macro);
            using(MainForm form=new MainForm(config,Path.Combine(root,"fields-"+Guid.NewGuid().ToString("N")),new Observer()))
            {
                form.Show(); Pump(30);
                float scale=form.Font.GetHeight()/form.Font.GetHeight(96F);
                CheckBox suppress=Field<CheckBox>(form,"suppressBox");
                TableLayoutPanel library=(TableLayoutPanel)Field<ListBox>(form,"macroList").Parent;
                Button stop=All(form).OfType<Button>().Single(x=>x.Text==Localizer.T("全部停止"));
                foreach(Button action in library.GetControlFromPosition(0,5).Controls.OfType<Button>().Concat(new[]{stop}))
                {
                    bool compact=action is EditorButton && ((EditorButton)action).CompactText;
                    Size text=TextRenderer.MeasureText(action.Text,action.Font,Size.Empty,TextFormatFlags.NoPrefix|(compact?TextFormatFlags.NoPadding:0));
                    Check(action.Height>=text.Height+4 && action.Width>=text.Width+(compact?2:10),"main library/footer action text fits at actual DPI: "+action.Text);
                }

                Button settings=Field<Button>(form,"toolsButton");
                Check(settings.Width>=42*scale-1 && settings.Height>=42*scale-1 && settings.TabStop,"settings button has a larger keyboard-reachable target");
                Check(settings.BackColor==EditorTheme.Selection && settings.FlatAppearance.BorderColor==EditorTheme.Accent,"settings entry has a visible accent");
                settings.PerformClick(); Pump(25);
                ContextMenuStrip menu=Field<ContextMenuStrip>(form,"toolsMenu");
                Check(menu.Visible && menu.Font.SizeInPoints>=form.Font.SizeInPoints-.1,"settings button opens a readable menu");
                foreach(ToolStripItem item in menu.Items.Cast<ToolStripItem>().Where(x=>!(x is ToolStripSeparator)))
                {
                    Check(item.Height>=item.Font.Height+6,"settings menu rows have breathing room: "+item.Text);
                    Check(TextRenderer.MeasureText(item.Text,item.Font).Width+32<=menu.Width,"settings menu text and arrows fit: "+item.Text);
                }
                using(Bitmap bitmap=new Bitmap(menu.Width,menu.Height))
                { menu.DrawToBitmap(bitmap,menu.ClientRectangle); bitmap.Save(Path.Combine(root,"settings-menu-"+language+".png")); }
                ToolStripMenuItem languages=Field<ToolStripMenuItem>(form,"languageMenuItem");
                languages.ShowDropDown(); Pump(20);
                Check(languages.DropDown.Visible && languages.DropDown.Font.SizeInPoints>=form.Font.SizeInPoints-.1,"nested language choices retain readable menu text");
                foreach(ToolStripItem item in languages.DropDownItems)
                    Check(item.Height>=item.Font.Height+6,"nested menu row spacing");
                languages.HideDropDown(); menu.Close();

                foreach(int width in new[]{1000,1160,1280})
                {
                    form.Size=new Size((int)(width*scale),(int)(780*scale)); Pump(30);
                    foreach(bool expanded in new[]{false,true})
                    {
                        if(Field<bool>(form,"moreExpanded")!=expanded) Field<Button>(form,"moreButton").PerformClick();
                        Pump(30);
                        CompactTriggerLayout(form);
                        Control[] common=suppress.Parent.Controls.Cast<Control>().Where(x=>x.Visible).ToArray();
                        for(int a=0;a<common.Length;a++) for(int b=a+1;b<common.Length;b++)
                            Check(!common[a].Bounds.IntersectsWith(common[b].Bounds),"common repeat/block/disclosure controls do not overlap");
                        Check(suppress.Visible,"trigger blocking stays visible regardless of disclosure state");
                        Control frame=Field<Control>(form,"optionsFrame");
                        Check(!frame.Contains(suppress) && frame.Visible==expanded,"common blocking option is outside the advanced frame");
                        if(expanded)
                        {
                            Check(frame.Contains(Field<ComboBox>(form,"activeLayerBox")) && frame.Contains(Field<ComboBox>(form,"mappingLayerBox")) && frame.BackColor!=Color.White,"both layer fields belong to the tinted advanced frame");
                            foreach(string name in new[]{"activeLayerBox","mappingLayerBox"})
                            {
                                ComboBox choice=Field<ComboBox>(form,name);
                                Check(choice.Width<=320*scale+1 && choice.Width<frame.Width*.8,"layer field has a bounded content width: "+name);
                                Check(TextRenderer.MeasureText(choice.Text,choice.Font).Width+SystemInformation.VerticalScrollBarWidth+6<=choice.Width,"default layer name fits");
                            }
                            if(width==1160) Render(form,"compact-advanced-"+language);
                        }
                    }
                }
                bool before=macro.SuppressTrigger; suppress.Checked=!before;
                Check(macro.SuppressTrigger==!before,"visible checkbox still saves the original suppression setting");
                suppress.Checked=before;
                form.Close();
            }
            foreach(bool gamepad in new[]{false,true})
            using(StepEditDialog editor=new StepEditDialog(null,new MacroStep { Kind=gamepad?InputKind.Gamepad:InputKind.Keyboard, VirtualKey=(int)Keys.F6, GamepadControl=GamepadControl.LeftStick }))
            {
                editor.Show(); Pump(30);
                float scale=editor.Font.GetHeight()/editor.Font.GetHeight(96F);
                foreach(ComboBox choice in All(editor).OfType<ComboBox>().Where(x=>x.Visible))
                {
                    Check(choice.Width<=320*scale+1,"step choices do not stretch across the dialog");
                    Check(TextRenderer.MeasureText(choice.Text,choice.Font).Width+SystemInformation.VerticalScrollBarWidth+6<=choice.Width,"step choice remains readable: "+choice.Text);
                }
                if(gamepad) Check(Field<Control>(editor,"gamepadPreview").Width>=300*scale,"controller diagram is substantially wider than the previous 224px content");
                else
                {
                    Button key=Field<Button>(editor,"captureButton");
                    Label label=Field<Label>(editor,"inputLabel");
                    Label selected=Field<Label>(editor,"inputText");
                    Button physical=Field<Button>(editor,"recordButton");
                    Check(selected.Text=="F6" && key.Visible && physical.Visible,"current step key and both input methods are immediately visible");
                    Check(key.Parent==selected.Parent && physical.Parent==key.Parent,"diagram and physical capture share one clearly grouped row");
                    Check(editor.RectangleToClient(selected.RectangleToScreen(selected.ClientRectangle)).Left-editor.RectangleToClient(label.RectangleToScreen(label.ClientRectangle)).Right<=12*scale,"step input group is beside its label");
                    EditorKeyPreview preview=Field<EditorKeyPreview>(editor,"keyPreview");
                    preview.Text="A";
                    Check(preview.KeyBounds.Width==preview.KeyBounds.Height,"single letter has a square preview");
                    int single=preview.KeyBounds.Width;
                    preview.Text="Ctrl + Shift + F8";
                    Check(preview.KeyBounds.Width>single && preview.ClientRectangle.Contains(preview.KeyBounds),"long key chord gets bounded extra width");
                    preview.Text=selected.Text;
                }
                Render(editor,"compact-step-"+language+"-"+(gamepad?"gamepad":"keyboard"));
                foreach(Button footer in new[]{(Button)editor.AcceptButton,(Button)editor.CancelButton})
                    Check(editor.ClientRectangle.Contains(editor.RectangleToClient(footer.RectangleToScreen(footer.ClientRectangle))),"step footer remains visible");
                editor.Close();
            }
        }
    }

    private static void InDialog<T>(Action open, Action<T> interact) where T : Form
    {
        Exception failure = null;
        bool handled = false;
        Stopwatch clock = Stopwatch.StartNew();
        using (System.Windows.Forms.Timer driver = new System.Windows.Forms.Timer())
        {
            driver.Interval = 30;
            driver.Tick += delegate
            {
                T dialog = Application.OpenForms.OfType<T>().LastOrDefault();
                if (dialog == null && clock.ElapsedMilliseconds < 8000) return;
                driver.Stop();
                try { Check(dialog != null, "expected " + typeof(T).Name + " opens"); handled = true; interact(dialog); }
                catch (Exception ex) { failure = ex; if (dialog != null) { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); } }
            };
            driver.Start(); open();
        }
        if (failure != null) throw failure;
        Check(handled, "visible dialog completed: " + typeof(T).Name);
    }

    private static void ChooseKeys(VirtualKeyboardDialog dialog, params Keys[] keys)
    {
        All(dialog).OfType<Button>().Single(b => b.Text == (Localizer.IsEnglish ? "Clear selection" : "清空选择")).PerformClick();
        Field<RadioButton>(dialog, "multipleButton").Checked = keys.Length > 1;
        Field<RadioButton>(dialog, "singleButton").Checked = keys.Length <= 1;
        foreach (Keys key in keys) PressKey(dialog, key, key == Keys.LWin);
        Check(((Button)dialog.AcceptButton).Enabled, "chosen keys can be confirmed");
        ((Button)dialog.AcceptButton).PerformClick();
    }

    private static void BeginnerWorkflow()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            string directory = Path.Combine(root, "beginner-" + Guid.NewGuid().ToString("N"));
            MacroConfig config = new MacroConfig { Language = language, GamepadDeviceType = VirtualGamepadTypes.None };
            Observer observer = new Observer();
            using (MainForm form = new MainForm(config, directory, observer))
            {
                form.Show(); Pump(25);
                InDialog<NewMacroDialog>(() => Field<Button>(form, "newMacroButton").PerformClick(), chooser =>
                    All(chooser).OfType<Button>().Single(b => b.Name == "template3").PerformClick());
                Check(config.Macros.Count == 1, "blank creation adds only one macro");
                MacroDefinition macro = config.Macros[0];
                Check(!Field<Button>(form, "runButton").Enabled, "empty macro cannot run and explains the next step");
                Check(Field<Label>(form, "runtimeLabel").Text.Contains(Localizer.IsEnglish ? "step" : "步骤"), "empty state directs the user to steps");
                TextBox name = Field<TextBox>(form, "nameBox"), note = Field<TextBox>(form, "descriptionBox");
                name.Focus(); name.Text = "  " + (Localizer.IsEnglish ? "My first macro" : "我的第一个宏") + "  ";
                note.Focus(); note.Text = "Bilingual workflow";
                InDialog<VirtualKeyboardDialog>(() => Field<Button>(form, "triggerButton").PerformClick(), keys => ChooseKeys(keys, Keys.LControlKey, Keys.F9));
                Field<ComboBox>(form, "triggerModeBox").SelectedIndex = 2;
                Field<NumericUpDown>(form, "repeatBox").Value = 2;
                InDialog<StepEditDialog>(() => Field<Button>(form, "addStepButton").PerformClick(), editor =>
                {
                    InDialog<VirtualKeyboardDialog>(() => Field<Button>(editor, "captureButton").PerformClick(), keys => ChooseKeys(keys, Keys.F6));
                    Field<NumericUpDown>(editor, "holdBox").Value = 11;
                    Field<NumericUpDown>(editor, "delayBox").Value = 17;
                    ((Button)editor.AcceptButton).PerformClick();
                });
                Check(macro.Trigger.Ctrl && macro.Trigger.VirtualKey == (int)Keys.F9 && macro.Steps.Count == 1, "blank setup preserves trigger chord and action");
                Check(Field<Label>(form,"stepsHintLabel").Text==(Localizer.IsEnglish ? "1 step" : "1 个步骤"), "step count uses the correct singular wording");
                Check(Field<Button>(form,"runButton").Enabled, "adding the first step immediately enables Run without an unrelated focus change");
                DataGridView grid = Field<DataGridView>(form, "grid"); grid.Rows[0].Selected = true;
                InDialog<StepEditDialog>(() => Field<Button>(form, "editStepButton").PerformClick(), editor =>
                {
                    Field<NumericUpDown>(editor, "delayBox").Value = 88;
                    ((Button)editor.CancelButton).PerformClick();
                });
                Check(macro.Steps[0].DelayMs == 17, "cancelled step edit leaves the saved step intact");
                InDialog<StepEditDialog>(() => Field<Button>(form, "editStepButton").PerformClick(), editor =>
                {
                    Field<CheckBox>(editor, "randomDelayBox").Checked = true;
                    Field<NumericUpDown>(editor, "randomMinBox").Value = 300;
                    Field<NumericUpDown>(editor, "randomMaxBox").Value = 100;
                    Check(!((Button)editor.AcceptButton).Enabled && Field<Label>(editor, "stepPreviewText").Text.Contains(Localizer.IsEnglish ? "minimum" : "最小值"), "invalid range is explained inline without an extra error dialog");
                    Field<NumericUpDown>(editor, "randomMaxBox").Value = 500;
                    Check(((Button)editor.AcceptButton).Enabled, "correcting the range immediately enables saving");
                    Field<CheckBox>(editor, "randomDelayBox").Checked = false;
                    Field<NumericUpDown>(editor, "delayBox").Value = 19;
                    ((Button)editor.AcceptButton).PerformClick();
                });
                MacroConfig saved = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml"));
                Check(saved.Macros[0].Name == macro.Name && saved.Macros[0].Description == note.Text &&
                    saved.Macros[0].Trigger.Ctrl && saved.Macros[0].Activator.Mode == ActivatorModes.Press &&
                    saved.Macros[0].RepeatCount == 2 && saved.Macros[0].Steps[0].DelayMs == 19, "name, note, trigger, repeat and edited step persist without a Save action");
                Label readiness = Field<Label>(form, "runtimeLabel");
                Check(readiness.Visible && readiness.Text.Contains(Localizer.IsEnglish ? "auto-save" : "自动保存"), "save and run guidance remains visible after adding steps");
                grid.ClearSelection(); grid.Rows[0].Selected=true;
                All(grid.Parent.Parent).OfType<Button>().Single(b => b.Text==Localizer.T("删除")).PerformClick();
                Check(macro.Steps.Count==0 && !Field<Button>(form,"runButton").Enabled, "deleting the last step immediately disables Run");
                Field<Button>(form,"undoStepsButton").PerformClick();
                Check(macro.Steps.Count==1 && Field<Button>(form,"runButton").Enabled, "undo restores both the last step and Run readiness");
                Render(form, "beginner-ready-" + language);
                // PerformClick does not activate its window as a real mouse click would.
                form.Activate(); Pump(25);
                Field<Button>(form, "runButton").PerformClick();
                try { Wait(() => observer.KeyboardDowns >= 2 && !(bool)Call(form, "HasWorkerBackedRuns"), "new macro runs the configured count", 3000); }
                catch (Exception ex) { throw new Exception("Beginner Run: downs="+observer.Downs+"; ups="+observer.Ups+"; pause="+Field<bool>(form,"uiSafetyPauseRequested")+"/"+Field<string>(form,"uiSafetyPauseReason")+"; modal="+Field<int>(form,"uiSafetyModalDepth")+"; status="+Field<Label>(form,"statusLabel").Text+"; enabled="+Field<Button>(form,"runButton").Enabled,ex); }
                Check(observer.KeyboardDowns == observer.KeyboardUps && observer.Pressed == 0, "new macro releases every output");
                Field<CheckBox>(form, "enabledBox").Checked = false;
                Check(readiness.Text.Contains(Localizer.IsEnglish ? "Shortcut off" : "触发键已关闭"), "disabled shortcut explicitly keeps manual execution available");
                long before = observer.Downs;
                Field<Button>(form, "runButton").PerformClick();
                Wait(() => observer.Downs > before && !(bool)Call(form, "HasWorkerBackedRuns"), "manual Run still works with shortcut disabled", 3000);
                form.Close();
            }
            MacroConfig reopened = ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml"));
            using (MainForm form = new MainForm(reopened, directory, observer))
            {
                form.Show(); Pump(25);
                Check(!Field<CheckBox>(form, "enabledBox").Checked && Field<ComboBox>(form, "triggerModeBox").SelectedIndex == 2 &&
                    reopened.Macros[0].Steps[0].DelayMs == 19, "reopening restores all settings and edited steps");
                long before = observer.Downs; Field<Button>(form, "runButton").PerformClick();
                Wait(() => observer.Downs > before && !(bool)Call(form, "HasWorkerBackedRuns"), "reopened macro runs", 3000);
                Check(observer.Pressed == 0, "reopened run leaves no held output");
                Field<CheckBox>(form,"enabledBox").Checked = true;
                form.Hide(); Pump(20); Call(form,"UpdateUiSafetyPauseState",false);
                before = observer.Downs;
                Call(form,"HandleTerminalInput",CaptureEvent(Keys.F9,true));
                Call(form,"HandleTerminalInputReleased",CaptureEvent(Keys.F9,true));
                Wait(() => observer.Downs > before && !(bool)Call(form,"HasWorkerBackedRuns"), "saved shortcut dispatch runs the reopened macro", 3000);
                Check(observer.Pressed == 0, "shortcut run releases every output");
                form.Close();
            }
        }
    }

    private static InputEventInfo CaptureEvent(Keys key, bool ctrl = false, bool shift = false, bool alt = false, bool win = false)
    { return new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key }, Ctrl = ctrl, Shift = shift, Alt = alt, Win = win }; }

    private static void QuickShortcutCapture()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            MacroConfig config = new MacroConfig { Language = language, GamepadDeviceType = VirtualGamepadTypes.None,
                PanicTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.Pause, Ctrl = true } };
            config.Macros.Add(Timed("Unchanged existing macro", Keys.F7, false));
            Observer observer = new Observer();
            using (MainForm form = new MainForm(config, Path.Combine(root,"capture-"+Guid.NewGuid().ToString("N")), observer))
            using (QuickCreateDialog quick = new QuickCreateDialog(form, QuickTemplate.Sequence))
            {
                form.Show(); quick.Show(form); Pump(25);
                Check(!Field<Button>(quick,"editActionButton").Enabled && !Field<Button>(quick,"removeActionButton").Enabled, "empty step list has no clickable no-op edit/delete actions");
                Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                Call(form,"HandleTerminalInput",CaptureEvent(Keys.LControlKey,true));
                Check(Field<TriggerSpec>(quick,"trigger") == null, "modifier waits for the main key");
                Call(form,"HandleTerminalInput",CaptureEvent(Keys.F9,true,true,true,true)); Pump(25);
                TriggerSpec captured = Field<TriggerSpec>(quick,"trigger");
                Check(captured.VirtualKey == (int)Keys.F9 && captured.Ctrl && captured.Shift && captured.Alt && captured.Win, "physical quick capture keeps the full modifier chord");
                foreach (Keys key in new[] { Keys.LControlKey, Keys.RShiftKey, Keys.RMenu, Keys.LWin })
                {
                    Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                    Call(form,"HandleTerminalInput",CaptureEvent(key));
                    Call(form,"HandleTerminalInputReleased",CaptureEvent(key)); Pump(20);
                    captured = Field<TriggerSpec>(quick,"trigger");
                    Check(captured.VirtualKey == (int)key && !captured.Ctrl && !captured.Shift && !captured.Alt && !captured.Win, "standalone modifier captures on release: " + key);
                }
                Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                Call(form,"HandleTerminalInput",new InputEventInfo { Input = new InputSpec { Kind=InputKind.MouseX1, VirtualKey=0 }, Ctrl=true }); Pump(20);
                captured=Field<TriggerSpec>(quick,"trigger");
                Check(captured.Kind==InputKind.MouseX1 && captured.Ctrl, "mouse chord keeps modifiers");
                Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                Call(form,"HandleTerminalInput",new InputEventInfo { Input = new InputSpec { Kind=InputKind.Keyboard, VirtualKey=(int)Keys.Enter, Extended=true } }); Pump(20);
                captured=Field<TriggerSpec>(quick,"trigger");
                Check(captured.Extended && captured.MatchExtended, "capture distinguishes numpad Enter");
                Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                Call(form,"HandleTerminalInput",CaptureEvent(Keys.Escape));
                Check(Field<TriggerSpec>(quick,"trigger")==captured && Field<Button>(quick,"triggerButton").Enabled, "Escape keeps the previous trigger and restores selection");
                Call(quick,"CaptureTrigger",null,EventArgs.Empty);
                Call(form,"HandleTerminalInput",CaptureEvent(Keys.Pause,true)); Pump(40);
                Check(Field<TriggerSpec>(quick,"trigger")==captured && Field<Button>(quick,"triggerButton").Enabled, "Emergency Stop wins and restores quick capture controls");
                Check(config.Macros[0].Trigger.VirtualKey==(int)Keys.F7 && observer.Downs==0, "quick capture never changes or runs the existing macro");
                Field<List<MacroStep>>(quick,"steps").AddRange(new[] { new MacroStep(), new MacroStep(), new MacroStep() });
                Call(quick,"RefreshActions"); Field<ListBox>(quick,"stepList").SelectedIndex=0;
                Check(Field<Button>(quick,"editActionButton").Enabled && Field<Button>(quick,"removeActionButton").Enabled, "selected steps enable edit/delete");
                InDialog<StepEditDialog>(() => Field<Button>(quick,"editActionButton").PerformClick(), editor =>
                { Field<NumericUpDown>(editor,"delayBox").Value=37; ((Button)editor.AcceptButton).PerformClick(); });
                Check(Field<ListBox>(quick,"stepList").SelectedIndex==0 && Field<List<MacroStep>>(quick,"steps")[0].DelayMs==37, "editing keeps the selected row instead of jumping to the last step");
                Render(quick,"beginner-quick-"+language);
                quick.Close(); form.Close();
            }
            using (QuickCreateDialog held = new QuickCreateDialog(null,QuickTemplate.HeldMapping))
            {
                held.GetType().GetField("trigger",Private).SetValue(held,new TriggerSpec { Kind=InputKind.WheelUp });
                Call(held,"UpdateTrigger");
                Check(!((Button)held.AcceptButton).Enabled && Field<Label>(held,"readyLabel").Text.Contains(Localizer.IsEnglish ? "wheel" : "滚轮"), "unsupported hold trigger is explained before Create");
            }
            using (StepEditDialog wheel = new StepEditDialog(null,new MacroStep { Kind=InputKind.WheelDown }))
            {
                Field<ComboBox>(wheel,"actionBox").SelectedIndex=1;
                Check(!((Button)wheel.AcceptButton).Enabled, "unsupported wheel hold is blocked inline");
                Field<ComboBox>(wheel,"actionBox").SelectedIndex=0;
                Check(((Button)wheel.AcceptButton).Enabled, "tap immediately restores valid wheel action");
            }
        }
    }

    private static void StepInputMethods()
    {
        foreach(string language in new[]{"zh-CN","en-US"})
        {
            MacroConfig config=new MacroConfig { Language=language,GamepadDeviceType=VirtualGamepadTypes.None,
                PanicTrigger=new TriggerSpec { Kind=InputKind.Keyboard,VirtualKey=(int)Keys.Pause,Ctrl=true } };
            Observer observer=new Observer();
            using(MainForm owner=new MainForm(config,Path.Combine(root,"step-input-"+Guid.NewGuid().ToString("N")),observer))
            using(StepEditDialog editor=new StepEditDialog(owner,new MacroStep { Kind=InputKind.Keyboard,VirtualKey=(int)Keys.A }))
            {
                owner.Show(); editor.Show(owner); Pump(25);
                Button physical=Field<Button>(editor,"recordButton"),diagram=Field<Button>(editor,"captureButton");
                Label selected=Field<Label>(editor,"inputText");
                Check(physical.Visible && physical.Enabled && diagram.Visible && diagram.Enabled,"both step input methods are visible and enabled");
                physical.PerformClick();
                Check(!physical.Enabled && physical.Text.Contains("Esc"),"physical capture explains listening and cancellation inline");
                Call(owner,"HandleTerminalInput",CaptureEvent(Keys.F7)); Pump(25);
                Check(selected.Text=="F7" && physical.Enabled,"physical keyboard input updates the selected step key");
                physical.PerformClick();
                Call(owner,"HandleTerminalInput",new InputEventInfo { Input=new InputSpec { Kind=InputKind.MouseX1 } }); Pump(25);
                string mouse=selected.Text;
                Check(Field<InputSpec>(editor,"selectedInput").Kind==InputKind.MouseX1 && physical.Enabled,"physical mouse capture uses the same visible method");
                physical.PerformClick(); Call(owner,"HandleTerminalInput",CaptureEvent(Keys.Escape)); Pump(20);
                Check(selected.Text==mouse && physical.Enabled,"Escape restores physical capture and preserves the previous input");
                physical.PerformClick(); Call(owner,"HandleTerminalInput",CaptureEvent(Keys.Pause,true)); Pump(30);
                Check(selected.Text==mouse && physical.Enabled,"Emergency Stop restores physical capture without changing the step");
                physical.PerformClick();
                InDialog<VirtualKeyboardDialog>(()=>diagram.PerformClick(),keys=>ChooseKeys(keys,Keys.A));
                Check(selected.Text=="A" && physical.Enabled,"diagram selection cancels physical capture and visibly updates the same value");
                Check(Field<EditorKeyPreview>(editor,"keyPreview").KeyBounds.Width==Field<EditorKeyPreview>(editor,"keyPreview").KeyBounds.Height,"actual A selection uses a square preview");
                Render(editor,"step-input-single-"+language);
                InDialog<VirtualKeyboardDialog>(()=>diagram.PerformClick(),keys=>ChooseKeys(keys,Keys.LControlKey,Keys.F8));
                Check(Field<List<InputSpec>>(editor,"selectedInputs").Count==2 && selected.Text.Contains("F8"),"diagram keeps multi-key selection semantics");
                Render(editor,"step-input-methods-"+language);
                Check(observer.Downs==0,"choosing step inputs emits no output");
                editor.Close(); owner.Close();
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
        finite.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 };
        finite.Activator = new ActivatorConfig { Mode = ActivatorModes.LongPress, LongPressMs = 50 };
        MacroDefinition held = QuickMacros.Create(QuickTemplate.HeldMapping, "held-stick", new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.W },
            new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.LeftStick, GamepadY = 80 } }, 1);
        config.Macros.Add(continuous); config.Macros.Add(finite); config.Macros.Add(held);
        Observer observer = new Observer();
        Stopwatch clock = Stopwatch.StartNew();
        Process process = Process.GetCurrentProcess();
        long cycles = 0, baselineBytes = 0, maxBytes = 0, maxPumpMs = 0;
        long longPressCycles = 0, blockedUiSamples = 0, maxLongPressMs = 0;
        int baselineHandles = 0, maxHandles = 0, baselineThreads = 0, maxThreads = 0;
        int baselineGdi = 0, baselineUser = 0;
        List<int> warmHandles = new List<int>(), tailHandles = new List<int>();
        List<string> samples = new List<string> { "elapsed_seconds,cycles,private_bytes,handles,threads,output_downs,output_ups,cpu_milliseconds" };
        using (MainForm form = new MainForm(config, directory, observer))
        using (InputPipelineScheduler scheduler = new InputPipelineScheduler(10, delegate { Call(form, "RequestActivatorTick"); }, null))
        {
            IntPtr handle = form.Handle;
            form.ActivatorPhysicalProbe = delegate { return true; };
            form.ActivatorOwnWindowProbe = delegate { return false; };
            scheduler.Start();
            Call(form, "StartParallelHeldMapping", held);
            Call(form, "StartMacro", continuous, 0, null, false, false);
            Wait(delegate { return observer.Downs > 0; }, "continuous run starts", 2000);
            long nextSample = 0;
            while (clock.Elapsed.TotalMinutes < minutes)
            {
                if (cycles % 2 == 0)
                {
                    InputEventInfo edge = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F24 } };
                    long previous = Interlocked.Read(ref observer.FiniteDowns), started = Stopwatch.GetTimestamp();
                    Call(form, "HandleTerminalInput", edge);
                    if (cycles > 0 && cycles % 20 == 0)
                    {
                        Thread.Sleep(120);
                        Check(Interlocked.Read(ref observer.FiniteDowns) > previous, "long-press output occurs before blocked UI resumes during soak");
                        blockedUiSamples++;
                    }
                    Wait(delegate { return Interlocked.Read(ref observer.FiniteDowns) > previous; }, "long-press starts during soak", 2000);
                    long latency = (Interlocked.Read(ref observer.LastFiniteDownAt) - started) * 1000 / Stopwatch.Frequency;
                    Check(latency >= 50 && latency < 300, "sustained long-press respects threshold without UI delay");
                    maxLongPressMs = Math.Max(maxLongPressMs, latency);
                    Call(form, "HandleTerminalInputReleased", edge);
                    longPressCycles++;
                }
                else Call(form, "StartMacro", finite, 0, null, false, false);
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
                    { baselineBytes = process.PrivateMemorySize64; baselineHandles = process.HandleCount; baselineThreads = process.Threads.Count;
                        baselineGdi = GetGuiResources(process.Handle, 0); baselineUser = GetGuiResources(process.Handle, 1); }
                    if (clock.Elapsed.TotalSeconds >= 30 && clock.Elapsed.TotalSeconds <= 90) warmHandles.Add(process.HandleCount);
                    if (clock.Elapsed.TotalSeconds >= minutes * 60 - 60) tailHandles.Add(process.HandleCount);
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
            process.Refresh();
            int[] warm = warmHandles.OrderBy(x => x).ToArray(), tail = tailHandles.OrderBy(x => x).ToArray();
            maxBytes = Math.Max(maxBytes, process.PrivateMemorySize64);
            maxHandles = Math.Max(maxHandles, process.HandleCount);
            maxThreads = Math.Max(maxThreads, process.Threads.Count);
            int warmMedian = warm.Length == 0 ? baselineHandles : warm[warm.Length / 2];
            int tailMedian = tail.Length == 0 ? process.HandleCount : tail[tail.Length / 2];
            int finalGdi = GetGuiResources(process.Handle, 0), finalUser = GetGuiResources(process.Handle, 1);
            // CLR Thread objects retain native bookkeeping handles until a natural GC, even after
            // Join. Compare equal steady-state windows, retaining the +32 growth limit, and bound
            // peaks separately. A single end sample depends on where the GC cycle happened to stop.
            string report = "{\"status\":\"MEASURED\",\"version\":\"" + AppInfo.Version + "\",\"durationSeconds\":" + clock.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) +
                ",\"cycles\":" + cycles + ",\"outputDowns\":" + observer.Downs + ",\"outputUps\":" + observer.Ups +
                ",\"longPressCycles\":" + longPressCycles + ",\"longPressThresholdMs\":50,\"blockedUiSamples\":" + blockedUiSamples + ",\"blockedUiDurationMs\":120,\"maxLongPressMs\":" + maxLongPressMs +
                ",\"keyboardDowns\":" + observer.KeyboardDowns + ",\"keyboardUps\":" + observer.KeyboardUps + ",\"maxContinuousGapMs\":" + observer.MaxContinuousGapMs +
                ",\"cpuMilliseconds\":" + process.TotalProcessorTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) +
                ",\"baselinePrivateBytes\":" + baselineBytes + ",\"finalPrivateBytes\":" + process.PrivateMemorySize64 + ",\"peakPrivateBytes\":" + maxBytes +
                ",\"baselineHandles\":" + baselineHandles + ",\"finalHandles\":" + process.HandleCount + ",\"peakHandles\":" + maxHandles +
                ",\"warmedMedianHandles\":" + warmMedian + ",\"tailMedianHandles\":" + tailMedian + ",\"handlePeakBudget\":" + (warmMedian + 256) +
                ",\"baselineGdi\":" + baselineGdi + ",\"finalGdi\":" + finalGdi + ",\"baselineUser\":" + baselineUser + ",\"finalUser\":" + finalUser +
                ",\"baselineThreads\":" + baselineThreads + ",\"finalThreads\":" + process.Threads.Count + ",\"peakThreads\":" + maxThreads +
                ",\"workerThreadsCreated\":" + Field<MacroWorkerPool>(form,"macroWorkers").CreatedThreadCount +
                ",\"maxUiDispatchMs\":" + maxPumpMs + ",\"remainingOutputSources\":0,\"remainingPressedOutputs\":0,\"forcedGc\":false,\"backend\":\"isolated observer\"}";
            File.WriteAllText(Path.Combine(directory, "report.json"), report, Encoding.UTF8);
            Check(baselineBytes > 0 && process.PrivateMemorySize64 <= baselineBytes + 128L * 1024 * 1024, "private memory growth stays below 128 MiB after warmup");
            Check(tailMedian <= warmMedian + 32, "steady-state median handle growth stays below 32 without forced GC");
            Check(process.HandleCount <= baselineHandles + 32, "final handle growth stays below 32 without forced GC");
            Check(maxHandles <= warmMedian + 256, "peak handles stay within the explicit CLR-thread bookkeeping budget");
            Check(finalGdi <= baselineGdi + 8 && finalUser <= baselineUser + 8, "GDI and USER objects remain bounded after sustained execution");
            Check(process.Threads.Count <= baselineThreads + 12, "final thread growth stays below 12");
            Check(Field<MacroWorkerPool>(form,"macroWorkers").CreatedThreadCount <= 6, "sustained restarts reuse bounded workers without thread churn");
            Check(maxPumpMs < 2000, "UI message dispatch stays below 2 seconds");
            File.WriteAllText(Path.Combine(directory, "report.json"), report.Replace("\"MEASURED\"", "\"PASS\""), Encoding.UTF8);
            Console.WriteLine("SOAK REPORT " + Path.Combine(directory, "report.json"));
        }
    }
}
