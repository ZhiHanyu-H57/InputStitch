using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using InputStitch;

internal static class ProductivityTests
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
    }
    private sealed class FaultStore : ConfigStore
    {
        internal int Fail;
        internal override void Write(string path, MacroConfig config)
        {
            if (Fail == 1) { File.WriteAllText(path, "partial"); throw new IOException("Simulated disk full"); }
            base.Write(path, config);
            if (Fail == 2) File.WriteAllText(path, "<broken");
        }
        internal override void Replace(string staged, string target, string backup)
        {
            if (Fail == 3) throw new IOException("Simulated replacement denied");
            base.Replace(staged, target, backup);
        }
    }
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        if ((args.Length > 0 && args[0] == "--ui") || AppDomain.CurrentDomain.FriendlyName.Contains("Preview"))
        {
            Localizer.SetLanguage("en-US");
            Application.Run(new QuickCreateDialog(null, QuickTemplate.HeldMapping));
            return;
        }
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-productivity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { TestSave(root); TestHistory(); TestTemplates(); TestMainUi(root); TestDialogs(); }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("PASS productivity: " + checks + " checks; isolated files, no hooks / real input / devices.");
    }
    private static void TestSave(string directory)
    {
        string path = Path.Combine(directory, "config.xml");
        FaultStore store = new FaultStore();
        MacroConfig config = new MacroConfig();
        config.Macros.Add(new MacroDefinition { Name = "original", Steps = new List<MacroStep> { new MacroStep() } });
        store.Save(path, config);
        store.Validate(path);
        string legacyPath = Path.Combine(directory, "legacy-config.xml");
        string legacyXml = File.ReadAllText(path).Replace("</MacroConfig>",
            "  <RestorePreviousWindowOnUiRun>false</RestorePreviousWindowOnUiRun>\r\n</MacroConfig>");
        File.WriteAllText(legacyPath, legacyXml);
        store.Validate(legacyPath);
        MacroConfig legacyConfig = (MacroConfig)StaticCall(typeof(MainForm), "DeserializeConfigFromFile", legacyPath);
        Check(legacyConfig != null && legacyConfig.Macros.Count == 1 && legacyConfig.Macros[0].Name == "original",
            "Legacy removed RestorePreviousWindowOnUiRun element remains deserializable");
        byte[] original = File.ReadAllBytes(path);
        string backups = Path.Combine(directory, "backups", "config");
        store.Save(path, config);
        Check(!Directory.Exists(backups), "Identical saves do not rotate history");
        config.Macros[0].Name = "new";
        for (int failure = 1; failure <= 3; failure++)
        {
            store.Fail = failure;
            bool failed = false;
            try { store.Save(path, config); } catch { failed = true; }
            Check(failed, "Fault reaches caller " + failure);
            Check(original.SequenceEqual(File.ReadAllBytes(path)), "Old config survives " + failure);
            Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Staging cleanup " + failure);
        }
        store.Fail = 0;
        using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            bool failed = false;
            try { store.Save(path, config); } catch (IOException) { failed = true; }
            Check(failed, "Locked target reports failure");
            Check(original.SequenceEqual(File.ReadAllBytes(path)), "Locked target intact");
        }
        store.Save(path, config);
        Check(Directory.GetFiles(backups, "config-*.xml").Length == 1, "Old valid backup created");
        Check(original.SequenceEqual(File.ReadAllBytes(Directory.GetFiles(backups, "config-*.xml")[0])), "Backup byte exact");
        for (int i = 0; i < 9; i++) { config.Macros[0].Name = "revision-" + i; store.Save(path, config); }
        string[] files = Directory.GetFiles(backups, "config-*.xml");
        Check(files.Length == 5, "Only latest five valid backups");
        foreach (string file in files) store.Validate(file);
        foreach (int i in Enumerable.Range(3, 5)) Check(files.Any(p => File.ReadAllText(p).Contains("revision-" + i)), "Expected retained revision " + i);
        store.Save(path, config);
        Check(files.OrderBy(p => p).SequenceEqual(Directory.GetFiles(backups, "config-*.xml").OrderBy(p => p)), "No-op preserves backup names");
        File.WriteAllText(path, "unreadable original");
        store.Save(path, config);
        store.Validate(path);
        Check(File.ReadAllText(Directory.GetFiles(backups, "unreadable-*.xml")[0]) == "unreadable original", "Unreadable original preserved separately");
    }
    private static void TestHistory()
    {
        StepHistory history = new StepHistory();
        MacroDefinition macro = new MacroDefinition { Name = "unchanged", Steps = new List<MacroStep> { new MacroStep() } };
        history.Observe(macro);
        Check(!history.CanUndo && !history.CanRedo, "Initial history empty");
        macro.Steps.Add(new MacroStep { VirtualKey = (int)Keys.B }); history.Observe(macro);
        Check(history.UndoCount == 1, "Add is one edit");
        MacroDefinition workerSnapshot = macro.Clone();
        Check(history.Restore(macro, false) && macro.Steps.Count == 1, "Undo add");
        Check(workerSnapshot.Steps.Count == 2, "Worker snapshot unchanged");
        Check(history.Restore(macro, true) && macro.Steps.Count == 2, "Redo add");
        foreach (FieldInfo field in typeof(MacroStep).GetFields())
        {
            object previous = field.GetValue(macro.Steps[0]);
            object next = field.FieldType == typeof(bool) ? (object)!(bool)previous :
                field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, ((int)previous + 1) % Enum.GetValues(field.FieldType).Length) : (object)((int)previous + 1);
            field.SetValue(macro.Steps[0], next);
            history.Observe(macro);
            Check(history.Restore(macro, false) && Object.Equals(previous, field.GetValue(macro.Steps[0])), "Undo field " + field.Name);
            Check(history.Restore(macro, true) && Object.Equals(next, field.GetValue(macro.Steps[0])), "Redo field " + field.Name);
        }
        history.Restore(macro, false);
        macro.Steps[0].DelayMs = 812; history.Observe(macro);
        Check(!history.CanRedo, "New edit truncates redo");
        int before = history.UndoCount;
        history.Observe(macro); Check(history.UndoCount == before, "Refresh no-op");
        for (int i = 0; i < 70; i++) { macro.Steps[0].HoldMs = i + 100; history.Observe(macro); }
        Check(history.UndoCount == 50, "Fifty-operation cap");
        for (int i = 0; i < 50; i++) Check(history.Restore(macro, false), "Undo retained operation " + i);
        Check(!history.Restore(macro, false), "Cannot undo discarded operation");
        Check(macro.Name == "unchanged", "History does not rename macro");
        history.Observe(new MacroDefinition()); history.Observe(macro);
        Check(!history.CanUndo && !history.CanRedo, "Switch clears both histories");
        macro.Steps.RemoveAt(0); history.Observe(macro); Check(history.Restore(macro, false) && macro.Steps.Count == 2, "Undo delete");
        macro.Steps.Reverse(); history.Observe(macro); Check(history.Restore(macro, false), "Undo move");
        foreach (MacroStep s in macro.Steps) { s.RandomDelay = true; s.RandomDelayMaxMs = 821; }
        history.Observe(macro); Check(history.Restore(macro, false), "Undo batch in one operation");
    }
    private static void TestTemplates()
    {
        TriggerSpec trigger = new TriggerSpec { VirtualKey = (int)Keys.LShiftKey };
        MacroStep step = new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.LeftStick, GamepadX = 0, GamepadY = 80 };
        List<MacroStep> steps = new List<MacroStep> { step };
        MacroDefinition held = QuickMacros.Create(QuickTemplate.HeldMapping, "held", trigger, steps, 1);
        Check(held.Enabled && held.RunMode == TriggerRunMode.Hold && held.Infinite && !held.SuppressTrigger, "Held passthrough defaults enabled");
        Check(held.Steps[0].Action == MacroAction.Down && held.Steps[0].DelayMs == 0 && !held.Steps[0].RandomDelay, "Held native representation");
        Check(held.Steps[0].GamepadY == 80, "Mapping vector preserved");
        foreach (Keys modifierKey in new[] { Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
            Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey, Keys.Menu, Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin })
        {
            TriggerSpec standalone = new TriggerSpec { VirtualKey = (int)modifierKey };
            MacroDefinition modifierHeld = QuickMacros.Create(QuickTemplate.HeldMapping, "modifier", standalone, steps, 1);
            Check(modifierHeld.Trigger.VirtualKey == (int)modifierKey && !modifierHeld.Trigger.Ctrl && !modifierHeld.Trigger.Shift &&
                !modifierHeld.Trigger.Alt && !modifierHeld.Trigger.Win && modifierHeld.RunMode == TriggerRunMode.Hold,
                "Held mapping accepts standalone modifier " + modifierKey);
        }
        step.GamepadY = 5; trigger.VirtualKey = (int)Keys.A;
        Check(held.Steps[0].GamepadY == 80 && held.Trigger.VirtualKey == (int)Keys.LShiftKey, "Template deep copies");
        foreach (TriggerSpec chordTrigger in new[] {
            new TriggerSpec { Ctrl = true, VirtualKey = (int)Keys.F8 },
            new TriggerSpec { Shift = true, VirtualKey = (int)Keys.A },
            new TriggerSpec { Alt = true, Kind = InputKind.MouseX1, VirtualKey = 0 },
            new TriggerSpec { Win = true, VirtualKey = (int)Keys.F8 } })
        {
            MacroDefinition chordHeld = QuickMacros.Create(QuickTemplate.HeldMapping, "chord", chordTrigger, steps, 1);
            Check(chordHeld.RunMode == TriggerRunMode.Hold && chordHeld.Infinite,
                "Modifier chord hold trigger accepted");
        }
        bool wheelFailed = false;
        try { QuickMacros.Create(QuickTemplate.HeldMapping, "bad", new TriggerSpec { Kind = InputKind.WheelDown }, steps, 1); }
        catch (ArgumentException) { wheelFailed = true; }
        Check(wheelFailed, "Wheel hold trigger rejected");
        MacroDefinition repeat = QuickMacros.Create(QuickTemplate.Repeat, "repeat", trigger, steps, 17);
        Check(repeat.RunMode == TriggerRunMode.Toggle && !repeat.Infinite && repeat.RepeatCount == 17, "Repeat defaults");
        steps.Add(new MacroStep { VirtualKey = (int)Keys.Escape });
        MacroDefinition sequence = QuickMacros.Create(QuickTemplate.Sequence, "sequence", trigger, steps, 17);
        Check(sequence.RepeatCount == 1 && !sequence.Infinite && sequence.Steps.Count == 2, "Sequence defaults and order");
    }
    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach (Control child in control.Controls) { yield return child; foreach (Control nested in Descendants(child)) yield return nested; }
    }
    private static object Field(object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }
    private static object Call(object obj, string name, params object[] args)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    }
    private static object StaticCall(Type type, string name, params object[] args)
    {
        return type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }
    private static void TestMainUi(string root)
    {
        string directory = Path.Combine(root, "ui");
        MacroConfig config = new MacroConfig();
        MacroDefinition macro = new MacroDefinition { Steps = new List<MacroStep> { new MacroStep(), new MacroStep { VirtualKey = (int)Keys.B } } };
        config.Macros.Add(macro);
        config.Macros.Add(new MacroDefinition());
        using (MainForm form = new MainForm(config, directory))
        {
            ToolStripMenuItem githubMenuItem = (ToolStripMenuItem)Field(form, "githubMenuItem");
            Check(githubMenuItem != null && githubMenuItem.Text == "打开项目 GitHub",
                "Gear menu exposes project GitHub item in Chinese");
            Check(AppInfo.ProjectUrl == "https://github.com/ZhiHanyu-H57/InputStitch",
                "Project GitHub item targets canonical repository URL");
            Call(form, "ChangeLanguage", Localizer.English);
            Check(githubMenuItem.Text == "Open Project on GitHub",
                "Project GitHub item follows live English localization");
            Call(form, "ChangeLanguage", Localizer.Chinese);

            DataGridView grid = (DataGridView)Field(form, "grid");
            grid.ClearSelection(); grid.Rows[0].Selected = true;
            Call(form, "CopySelectedSteps"); Check(macro.Steps.Count == 3, "Main copy");
            Call(form, "RestoreSteps", false); Check(macro.Steps.Count == 2, "Main undo copy");
            Call(form, "RestoreSteps", true); Check(macro.Steps.Count == 3, "Main redo copy");
            grid.ClearSelection(); grid.Rows[0].Selected = true;
            Call(form, "DeleteSelectedStep"); Check(macro.Steps.Count == 2, "Main delete");
            Call(form, "RestoreSteps", false); Check(macro.Steps.Count == 3, "Main undo delete");
            grid.ClearSelection(); grid.Rows[2].Selected = true;
            Call(form, "MoveSelectedStep", -1); Check(macro.Steps[1].VirtualKey == (int)Keys.B, "Main move");
            Call(form, "RestoreSteps", false); Check(macro.Steps[2].VirtualKey == (int)Keys.B, "Main undo move");
            MacroConfig saved;
            using (var reader = File.OpenRead(Path.Combine(directory, "config.xml")))
                saved = (MacroConfig)new System.Xml.Serialization.XmlSerializer(typeof(MacroConfig)).Deserialize(reader);
            Check(saved.Macros[0].Steps[2].VirtualKey == (int)Keys.B, "Undo goes through safe save");
            ListBox list = (ListBox)Field(form, "macroList");
            list.SelectedIndex = 1; list.SelectedIndex = 0;
            Check(!((Button)Field(form, "undoStepsButton")).Enabled, "Switch clears main undo button");

            MacroDefinition second = config.Macros[1];
            macro.Name = "priority-top";
            second.Name = "priority-bottom";
            macro.Enabled = second.Enabled = true;
            macro.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 };
            second.Trigger = macro.Trigger.Clone();
            InputEventInfo sameTrigger = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 } };
            Check(Object.ReferenceEquals(macro, (MacroDefinition)StaticCall(typeof(MainForm), "SelectTriggerMacro", config.Macros, sameTrigger)), "Duplicate trigger uses top macro priority");
            Check(!(bool)Call(form, "IsMacroTriggerConflicted", macro) && !(bool)Call(form, "IsMacroTriggerConflicted", second), "Duplicate macro triggers are valid priority overlap");
            list.SelectedIndex = 1;
            Call(form, "MoveSelectedMacro", -1);
            Check(Object.ReferenceEquals(second, config.Macros[0]), "Move up changes macro order");
            Check(Object.ReferenceEquals(second, (MacroDefinition)StaticCall(typeof(MainForm), "SelectTriggerMacro", config.Macros, sameTrigger)), "Move up changes duplicate-trigger priority");
            config.PanicTrigger = second.Trigger.Clone();
            Check((bool)Call(form, "IsMacroTriggerConflicted", second), "Emergency Stop collision remains a hard conflict");
            config.PanicTrigger = new TriggerSpec { Ctrl = true, Shift = true, VirtualKey = (int)Keys.F12 };

            MacroDefinition bareE = new MacroDefinition { Name = "bare-E", Enabled = true,
                Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.E } };
            MacroDefinition shiftE = new MacroDefinition { Name = "shift-E", Enabled = true,
                Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.E, Shift = true } };
            InputEventInfo shiftedE = new InputEventInfo
            {
                Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.E },
                Shift = true
            };
            Check(Object.ReferenceEquals(shiftE, (MacroDefinition)StaticCall(typeof(MainForm), "SelectTriggerMacro",
                new List<MacroDefinition> { bareE, shiftE }, shiftedE)),
                "Explicit Shift+E trigger wins over bare E Shift fallback");
            Check(Object.ReferenceEquals(bareE, (MacroDefinition)StaticCall(typeof(MainForm), "SelectTriggerMacro",
                new List<MacroDefinition> { bareE }, shiftedE)),
                "Bare ordinary key remains triggerable while gameplay Shift is held when no explicit chord exists");
            InputEventInfo ctrlE = new InputEventInfo
            {
                Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.E },
                Ctrl = true
            };
            Check(StaticCall(typeof(MainForm), "SelectTriggerMacro", new List<MacroDefinition> { bareE }, ctrlE) == null,
                "Bare ordinary key does not fallback through Ctrl");

            Check((bool)StaticCall(typeof(MainForm), "IdleKeyboardMouseScopeMatches", false, "", "chrome", false),
                "Idle keyboard/mouse remains global without configured target");
            Check((bool)StaticCall(typeof(MainForm), "IdleKeyboardMouseScopeMatches", true, "GTA5_Enhanced", "gta5_enhanced", false),
                "Idle keyboard/mouse counts inside configured target process");
            Check(!(bool)StaticCall(typeof(MainForm), "IdleKeyboardMouseScopeMatches", true, "GTA5_Enhanced", "chrome", false),
                "Typing in another process does not reset target idle timer");
            Check((bool)StaticCall(typeof(MainForm), "IdleKeyboardMouseScopeMatches", true, "", "", true),
                "Exact configured target window counts even without process name");
            Check(!(bool)StaticCall(typeof(MainForm), "IdleKeyboardMouseScopeMatches", true, "", "chrome", false),
                "Configured title/class target does not treat unrelated windows as activity");
            Check(!(bool)StaticCall(typeof(MainForm), "ShouldPauseIdleForMissingTarget", false, false),
                "Idle without configured target does not pause just because no target window resolves");
            Check((bool)StaticCall(typeof(MainForm), "ShouldPauseIdleForMissingTarget", true, false),
                "Configured idle target pauses while target is absent");
            Check(!(bool)StaticCall(typeof(MainForm), "ShouldPauseIdleForMissingTarget", true, true),
                "Configured idle target resumes when target is available");

            MacroConfig releaseSummaryConfig = new MacroConfig();
            Check(!(bool)StaticCall(typeof(MainForm), "ShouldShowReleaseSummary", releaseSummaryConfig),
                "Fresh install shows Welcome instead of a second release-summary popup");
            releaseSummaryConfig.HasSeenWelcome = true;
            Check((bool)StaticCall(typeof(MainForm), "ShouldShowReleaseSummary", releaseSummaryConfig),
                "Upgraded old config with no seen-version marker shows release summary once");
            releaseSummaryConfig.LastShownReleaseSummaryVersion = AppInfo.Version;
            Check(!(bool)StaticCall(typeof(MainForm), "ShouldShowReleaseSummary", releaseSummaryConfig),
                "Current version release summary is not shown twice");
            releaseSummaryConfig.LastShownReleaseSummaryVersion = "1.1.0";
            Check((bool)StaticCall(typeof(MainForm), "ShouldShowReleaseSummary", releaseSummaryConfig),
                "Older seen-version marker shows the new release summary");
            Localizer.SetLanguage("zh-CN");
            string zhSummary = (string)StaticCall(typeof(MainForm), "BuildReleaseSummaryText");
            Check(zhSummary.Contains(AppInfo.Version) && zhSummary.Contains(ReleaseInfo.ReleaseSummaryZh),
                "Chinese release summary includes current version and configured summary");
            Localizer.SetLanguage("en-US");
            string enSummary = (string)StaticCall(typeof(MainForm), "BuildReleaseSummaryText");
            Check(enSummary.Contains(AppInfo.Version) && enSummary.Contains(ReleaseInfo.ReleaseSummaryEn),
                "English release summary includes current version and configured summary");
            Localizer.SetLanguage("zh-CN");

            config.TargetProcessName = "MainTargetShouldNotControlIdle";
            config.TargetWindowTitle = "Main target";
            config.IdleGamepad.TargetProcessName = "";
            config.IdleGamepad.TargetWindowTitle = "";
            config.IdleGamepad.TargetWindowClass = "";
            config.IdleGamepad.TargetScopeInitialized = true;
            Check(!(bool)Call(form, "HasConfiguredIdleTarget"), "Main target no longer configures idle target");
            config.IdleGamepad.TargetProcessName = "GTA5_Enhanced";
            config.IdleGamepad.TargetWindowTitle = "Grand Theft Auto V";
            config.IdleGamepad.TargetWindowClass = "sgaWindow";
            Check((bool)Call(form, "HasConfiguredIdleTarget"), "Idle target has independent identity");

            MacroConfig legacyIdleTarget = new MacroConfig();
            legacyIdleTarget.TargetProcessName = "GTA5_Enhanced";
            legacyIdleTarget.TargetWindowTitle = "Grand Theft Auto V";
            legacyIdleTarget.TargetWindowClass = "sgaWindow";
            legacyIdleTarget.IdleGamepad.Enabled = true;
            legacyIdleTarget.IdleGamepad.TargetScopeInitialized = false;
            StaticCall(typeof(MainForm), "NormalizeConfig", legacyIdleTarget);
            Check(legacyIdleTarget.IdleGamepad.TargetScopeInitialized && legacyIdleTarget.IdleGamepad.TargetProcessName == "GTA5_Enhanced" &&
                legacyIdleTarget.IdleGamepad.TargetWindowTitle == "Grand Theft Auto V" && legacyIdleTarget.IdleGamepad.TargetWindowClass == "sgaWindow",
                "Legacy beta main target migrates once into idle target");
            legacyIdleTarget.IdleGamepad.TargetProcessName = legacyIdleTarget.IdleGamepad.TargetWindowTitle = legacyIdleTarget.IdleGamepad.TargetWindowClass = "";
            StaticCall(typeof(MainForm), "NormalizeConfig", legacyIdleTarget);
            Check(legacyIdleTarget.IdleGamepad.TargetProcessName == "", "Cleared initialized idle target does not re-copy main target");

            TargetWindowIdentity pickedIdleTarget = new TargetWindowIdentity { ProcessName = "GTA5_Enhanced", Title = "Grand Theft Auto V", ClassName = "sgaWindow" };
            using (IdleGamepadSettingsPanel idlePanel = new IdleGamepadSettingsPanel(new IdleGamepadOptions(), delegate { return pickedIdleTarget; }))
            {
                ((Button)Field(idlePanel, "lockTargetButton")).PerformClick();
                IdleGamepadOptions picked = idlePanel.ReadOptions();
                Check(picked.TargetScopeInitialized && picked.TargetProcessName == "GTA5_Enhanced" && picked.TargetWindowTitle == "Grand Theft Auto V" && picked.TargetWindowClass == "sgaWindow",
                    "Automation panel stores its own picked idle target");
                ((Button)Field(idlePanel, "clearTargetButton")).PerformClick();
                IdleGamepadOptions cleared = idlePanel.ReadOptions();
                Check(cleared.TargetScopeInitialized && cleared.TargetProcessName == "" && cleared.TargetWindowTitle == "" && cleared.TargetWindowClass == "",
                    "Automation panel clears only its idle target identity");
            }

            second.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.LShiftKey };
            second.RunMode = TriggerRunMode.Hold;
            second.Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, Action = MacroAction.Down, GamepadControl = GamepadControl.LeftStick, GamepadY = 80 } };
            list.SelectedIndex = 0;
            Call(form, "LoadSelectedMacroToUi");
            Check(!((CheckBox)Field(form, "suppressBox")).Enabled && ((CheckBox)Field(form, "suppressBox")).Checked, "Shift gamepad hold shows forced pass-through as checked");

            long settleTicks = 60L * Stopwatch.Frequency / 1000L;
            long t0 = Math.Max(1L, Stopwatch.GetTimestamp());
            MacroRunRuntime releaseRunA = new MacroRunRuntime { RunId = 101 };
            MacroRunRuntime releaseRunB = new MacroRunRuntime { RunId = 102 };
            Check(!(bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunA, true, t0), "Concurrent Hold release fallback starts per-run settle window");
            Check(!(bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunA, true, t0 + Math.Max(0L, settleTicks - 1)), "Concurrent Hold release fallback waits for stable release");
            Check(releaseRunA.ReleaseProbeSince != 0 && releaseRunB.ReleaseProbeSince == 0, "Release settle state is isolated per Hold run");
            Check(!(bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunB, true, t0 + 10), "Second Hold run starts an independent settle window");
            Check(!(bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunA, false, t0 + settleTicks), "Held trigger becoming pressed again cancels only its own fallback");
            Check(releaseRunA.ReleaseProbeSince == 0 && releaseRunB.ReleaseProbeSince != 0, "Cancelling one release probe leaves another Hold run's probe intact");
            long t1 = t0 + settleTicks + 100;
            Check(!(bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunA, true, t1), "Concurrent Hold release fallback restarts after cancellation");
            Check((bool)StaticCall(typeof(MainForm), "UpdateRunReleaseProbe", releaseRunA, true, t1 + settleTicks), "Stable lost-KeyUp fallback requests stop for the matching run");
            string path = Path.Combine(directory, "config.xml");
            byte[] before = File.ReadAllBytes(path);
            using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                config.Macros[0].Description = "unsaved change";
                Check(!(bool)Call(form, "SaveConfig"), "UI save returns failure");
                Check(((string)Field(form, "saveFailure")).Length > 0, "Persistent failure state");
                Check(before.SequenceEqual(File.ReadAllBytes(path)), "UI failure preserves disk");
            }
            Check((bool)Call(form, "SaveConfig") && (string)Field(form, "saveFailure") == "", "Retry clears warning");
            foreach (string language in new[] { "zh-CN", "en-US" })
            {
                Localizer.SetLanguage(language);
                Call(form, "ApplyLanguageToMainUi", false);
                typeof(Control).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { 2, true });
                form.CreateControl();
                form.Size = new Size(1000, 700);
                form.PerformLayout();
                using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    string renders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "renders");
                    Directory.CreateDirectory(renders);
                    bitmap.Save(Path.Combine(renders, "main-" + language + ".png"));
                }
            }
        }
    }
    private static void TestDialogs()
    {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "renders");
        Directory.CreateDirectory(directory);
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (QuickTemplate template in Enum.GetValues(typeof(QuickTemplate)))
        foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
        {
            Localizer.SetLanguage(language);
            using (QuickCreateDialog dialog = new QuickCreateDialog(null, template))
            {
                typeof(Control).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, new object[] { 2, true });
                dialog.CreateControl();
                dialog.Scale(new SizeF(scale, scale));
                dialog.ClientSize = new Size((int)(720 * scale), (int)(500 * scale));
                dialog.PerformLayout();
                Check(dialog.AcceptButton != null && dialog.CancelButton != null, "Persistent dialog actions");
                foreach (Button button in Descendants(dialog).OfType<Button>().Where(b => b.Visible))
                {
                    Check(button.Height >= TextRenderer.MeasureText(button.Text, button.Font).Height, "Button height: " + button.Text);
                }
                using (Bitmap bitmap = new Bitmap(dialog.Width, dialog.Height))
                {
                    dialog.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    bitmap.Save(Path.Combine(directory, "quick-" + language + "-" + template + "-" + scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png"));
                }
            }
        }
    }
}
