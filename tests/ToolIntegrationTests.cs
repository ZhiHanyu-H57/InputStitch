using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using InputStitch;

internal static class ToolIntegrationTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    private static int checks, windows;
    private static string root;
    private sealed class NoOutput : IOutputBackend
    {
        internal int Writes;
        public void SendDown(InputSpec input) { Writes++; }
        public void SendUp(InputSpec input) { Writes++; }
        public void NeutralizeGamepad() { }
    }
    private sealed class Connection : INetworkHelperConnection
    {
        internal int Requests, Disposals;
        public bool IsAlive { get { return Volatile.Read(ref Disposals) == 0; } }
        public event EventHandler Disconnected { add { } remove { } }
        public string Request(string command)
        { Interlocked.Increment(ref Requests); return command.StartsWith("BLOCK", StringComparison.Ordinal) ? "BLOCKED" : "RESTORED"; }
        public void Dispose() { Interlocked.Exchange(ref Disposals, 1); }
    }
    private sealed class DelayedFactory : INetworkHelperFactory, IDisposable
    {
        internal readonly ManualResetEvent Started = new ManualResetEvent(false), Release = new ManualResetEvent(false);
        internal readonly Connection Connection = new Connection();
        internal int Starts;
        public INetworkHelperConnection Start()
        { Interlocked.Increment(ref Starts); Started.Set(); if (!Release.WaitOne(5000)) throw new TimeoutException("Fake elevation did not finish."); return Connection; }
        public void Dispose() { Release.Set(); Started.Dispose(); Release.Dispose(); }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Hidden).GetValue(value); }
    private static void Set(object value, string name, object data) { value.GetType().GetField(name, Hidden).SetValue(value, data); }
    private static object Call(object value, string name, params object[] args)
    {
        MethodInfo method = value.GetType().GetMethods(Hidden).Single(x => x.Name == name && x.GetParameters().Length == args.Length);
        try { return method.Invoke(value, args); } catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Pump(int milliseconds)
    { Stopwatch timer = Stopwatch.StartNew(); while (timer.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(1); } }
    private static void Until(Func<bool> ready, string message)
    { Stopwatch timer = Stopwatch.StartNew(); while (!ready() && timer.ElapsedMilliseconds < 3000) Pump(3); Check(ready(), message); }
    private static TriggerSpec Hotkey(Keys key)
    { return new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)key, Ctrl = true, Alt = true }; }
    private static InputEventInfo Event(TriggerSpec key)
    { return new InputEventInfo { Input = new InputSpec { Kind = key.Kind, VirtualKey = key.VirtualKey, Extended = key.Extended }, Ctrl = key.Ctrl, Alt = key.Alt, Shift = key.Shift, Win = key.Win }; }
    private static MacroConfig Config()
    {
        MacroConfig c = new MacroConfig { GamepadDeviceType = VirtualGamepadTypes.None, PanicTrigger = Hotkey(Keys.F12) };
        c.Macros.Clear(); c.MappingLayers.Clear();
        c.Crosshair = new CrosshairOptions { Enabled = true, Visible = false, Hotkey = Hotkey(Keys.F5) };
        c.Timer = new TimerOptions { Enabled = true, Visible = false, StartPauseHotkey = Hotkey(Keys.F6), ResetHotkey = Hotkey(Keys.F7), VisibilityHotkey = Hotkey(Keys.F8) };
        c.NetworkControl = new NetworkControlOptions { Enabled = true, Mode = NetworkBlockMode.EntireComputer, Hotkey = Hotkey(Keys.F9), BlockSound = false, RestoreSound = false };
        return c;
    }
    private static string Xml<T>(T data)
    { using (StringWriter writer = new StringWriter()) { new XmlSerializer(typeof(T)).Serialize(writer, data); return writer.ToString(); } }
    [STAThread] private static int Main(string[] args)
    {
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool-integration-results"); Directory.CreateDirectory(root);
        Application.EnableVisualStyles(); EmbeddedDependencyLoader.Register();
        try
        {
            bool core = !args.Contains("--ui") || args.Contains("--core");
            if (core) { Persistence(); Conflicts(); Safety(); NetworkExitRaces(); ShortcutValidation(); IndependentNetworkControls(); PartEditing(); }
            if (args.Contains("--ui")) { DialogLayouts(); ModalWorkflow(); }
            string summary = "PASS tool integration: " + checks + " checks; " + windows + " dialog views; core=" + core + "; ui=" + args.Contains("--ui");
            File.WriteAllText(Path.Combine(root, core && args.Contains("--ui") ? "all-summary.txt" : core ? "core-summary.txt" : "ui-summary.txt"), summary);
            Console.WriteLine(summary); return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(root, "failure.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }
    private static void Persistence()
    {
        MacroConfig defaults = new MacroConfig();
        string cornerXml = Xml(new CrosshairOptions { Style = CrosshairStyle.CornerCutSquare });
        Check(cornerXml.Contains("<Style>Ring</Style>") && cornerXml.Contains("<StyleVariant>CornerCutSquare</StyleVariant>"), "new crosshair writes a value older versions can read");
        using (StringReader reader = new StringReader("<CrosshairOptions><StyleVariant>CornerCutSquare</StyleVariant><Style>Ring</Style></CrosshairOptions>"))
            Check(((CrosshairOptions)new XmlSerializer(typeof(CrosshairOptions)).Deserialize(reader)).Style == CrosshairStyle.CornerCutSquare, "variant restores independently of XML element order");
        Check(!defaults.Crosshair.Enabled && !defaults.Timer.Enabled && !defaults.NetworkControl.Enabled, "all optional tools default off");
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            string file = Path.Combine(root, "config-" + language + ".xml");
            File.WriteAllText(file, "<MacroConfig><Language>" + language + "</Language></MacroConfig>");
            MacroConfig legacy = ConfigPackageSerializer.DeserializeConfigFromFile(file);
            Check(legacy.Timer != null && !legacy.Timer.Enabled && legacy.NetworkControl != null && !legacy.NetworkControl.Enabled, "old XML lacking tools remains inert " + language);
            Check(!legacy.Crosshair.Enabled && legacy.Timer.Visible && legacy.Timer.X == 24 && legacy.Timer.Y == 24, "old XML receives practical display defaults " + language);
            MacroConfig source = Config(); source.Language = language;
            source.Macros.Add(new MacroDefinition { Name = "Compatibility / 兼容", Enabled = false, Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.MouseX1, HoldMs = 47, DelayMs = 108 } } });
            source.Timer.FontFamily = "Microsoft YaHei UI"; source.Timer.FontSize = 37.5F; source.Timer.Bold = source.Timer.Italic = true;
            source.Timer.Outline = false; source.Timer.OutlineWidth = 3.5F; source.Timer.LetterSpacing = 2.5F;
            source.Timer.Color = "#F1A2C3"; source.Timer.OutlineColor = "#0A1B2C"; source.Timer.Opacity = 73;
            source.Timer.MonitorDevice = "UNPLUGGED-DISPLAY"; source.Timer.X = -17; source.Timer.Y = 93;
            source.Timer.Backend = TimerBackend.Rtss; source.Timer.TimeFormat = TimerTimeFormat.HoursMinutesSecondsMilliseconds;
            source.Timer.TextTemplate = "本次用时 / Run time: {time}\n第二行 / Line two";
            source.NetworkControl.Mode = NetworkBlockMode.Executable; source.NetworkControl.ExecutablePath = "C:\\测试\\Example app.exe";
            source.Crosshair.Style = CrosshairStyle.CornerCutSquare; source.Crosshair.Gap = 17;
            new ConfigStore().Save(file, source);
            MacroConfig restored = ConfigPackageSerializer.DeserializeConfigFromFile(file);
            Check(restored.Macros.Count == 1 && restored.Macros[0].Steps[0].HoldMs == 47 && restored.Macros[0].Steps[0].DelayMs == 108, "ordinary macros survive new tool settings " + language);
            Check(Xml(source.Timer) == Xml(restored.Timer), "all timer settings and bilingual template round-trip " + language);
            Check(Xml(source.NetworkControl) == Xml(restored.NetworkControl), "network settings and Unicode path round-trip " + language);
            Check(restored.Crosshair.Style == CrosshairStyle.CornerCutSquare && restored.Crosshair.Gap == 17, "corner style and other tools coexist " + language);
            MacroConfig clone = ConfigPackageSerializer.CloneConfig(source);
            clone.Timer.StartPauseHotkey.VirtualKey = (int)Keys.F1; clone.NetworkControl.Hotkey.VirtualKey = (int)Keys.F2;
            Check(source.Timer.StartPauseHotkey.VirtualKey == (int)Keys.F6 && source.NetworkControl.Hotkey.VirtualKey == (int)Keys.F9, "tool hotkeys are deep-cloned " + language);
            source.Timer.StartPauseHotkey = source.Timer.ResetHotkey = source.Timer.VisibilityHotkey = null; source.NetworkControl.Hotkey = null;
            new ConfigStore().Save(file, source); restored = ConfigPackageSerializer.DeserializeConfigFromFile(file);
            Check(restored.Timer.StartPauseHotkey == null && restored.Timer.ResetHotkey == null && restored.Timer.VisibilityHotkey == null && restored.NetworkControl.Hotkey == null, "cleared hotkeys stay cleared " + language);
        }
    }
    private static void IndependentNetworkControls()
    {
        MacroConfig config = Config(); config.NetworkControl.Hotkey = null;
        config.NetworkControl.BlockHotkey = Hotkey(Keys.F9); config.NetworkControl.RestoreHotkey = Hotkey(Keys.F10);
        config.NetworkControl.BlockSound = config.NetworkControl.RestoreSound = true;
        using (DelayedFactory factory = new DelayedFactory())
        using (MainForm main = new MainForm(config, Path.Combine(root, "separate-network"), new NoOutput()))
        {
            IntPtr handle = main.Handle; NetworkController network = new NetworkController(factory); Set(main, "networkController", network);
            List<NetworkSoundKind> cues = new List<NetworkSoundKind>(); Set(main, "networkStateSounds", new NetworkStateSounds(cues.Add));
            Check((bool)Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.RestoreHotkey)), "restore key accepted while connected");
            Until(() => Field<int>(main, "networkOperationPending") == 0, "connected restore settles");
            Check(factory.Starts == 0, "restore while connected prompted elevation");
            Check((bool)Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.BlockHotkey)), "separate block admitted");
            Check(factory.Started.WaitOne(2000), "block worker did not enter elevation");
            Set(main, "uiSafetyModalDepth", 1);
            Check((bool)Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.RestoreHotkey)), "restore admitted through modal protection during pending elevation");
            factory.Release.Set(); Until(() => Field<int>(main, "networkOperationPending") == 0, "pending restore settles after elevation");
            Check(!network.IsBlocked, "restore requested during elevation was lost");
            Set(main, "uiSafetyModalDepth", 0); cues.Clear();
            Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.BlockHotkey));
            Until(() => Field<int>(main, "networkOperationPending") == 0, "separate block settles"); Check(network.IsBlocked, "separate block failed");
            Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.BlockHotkey));
            Until(() => Field<int>(main, "networkOperationPending") == 0, "repeated block settles");
            Check(network.IsBlocked && cues.Count == 1 && cues[0] == NetworkSoundKind.Blocked, "repeated block toggled or replayed sound");
            Set(main, "pauseHotkeys", true); Set(main, "uiSafetyPauseRequested", true); Set(main, "uiSafetyModalDepth", 1);
            Check((bool)Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.RestoreHotkey)), "separate restore survives all edit guards");
            Until(() => Field<int>(main, "networkOperationPending") == 0, "separate restore settles");
            Check(!network.IsBlocked && cues.Count == 2 && cues[1] == NetworkSoundKind.Restored, "restore did not confirm sound/state");
            Set(main, "pauseHotkeys", false); Set(main, "uiSafetyPauseRequested", false); Set(main, "uiSafetyModalDepth", 0);
            config.NetworkControl.RestoreHotkey = config.NetworkControl.BlockHotkey.Clone();
            for (int i = 0; i < 2; i++) { Call(main, "HandleScreenToolShortcut", Event(config.NetworkControl.BlockHotkey)); Until(() => Field<int>(main, "networkOperationPending") == 0, "same-key toggle settles"); Check(network.IsBlocked == (i == 0), "same-key option no longer toggles"); }
            config.NetworkControl.RestoreHotkey = config.Timer.ResetHotkey.Clone();
            Check(((string)Call(main, "NetworkShortcutConflict", config.NetworkControl)).Length > 0, "restore key conflicts not checked");
            TimerOptions candidate = config.Timer.Clone(); candidate.StartPauseHotkey = config.NetworkControl.BlockHotkey.Clone();
            Check(((string)Call(main, "TimerShortcutConflict", candidate)).Length > 0, "timer ignored separate block key reservation");
        }
    }
    private static void PartEditing()
    {
        TimerOptions legacy = new TimerOptions { TextTemplate = "原样 / Legacy {time}", FontSize = 35, Opacity = 62 };
        using (TimerSettingsDialog dialog = new TimerSettingsDialog(legacy, null, null))
        {
            Field<NumericUpDown>(dialog, "x").Value = -12;
            TimerOptions positionOnly = (TimerOptions)Call(dialog, "CaptureOptions");
            Check(positionOnly.Parts == null && positionOnly.TextTemplate == legacy.TextTemplate && positionOnly.FontSize == 35 && positionOnly.Opacity == 62, "position-only edit migrated legacy appearance");
            Call(dialog, "ApplyPreset", TimerTimeFormat.MinutesSecondsMilliseconds);
            TimerOptions preset = (TimerOptions)Call(dialog, "CaptureOptions");
            Check(preset.Parts.All(p => p.Kind != TimerPartKind.Hours) && TimerText.Format(TimeSpan.FromMilliseconds(3661123), preset) == "61:01.123", "minutes-only preset loses elapsed hour");
            Call(dialog, "AddPart"); Field<TextBox>(dialog, "partText").Text = " 赛段 / Split ";
            Field<NumericUpDown>(dialog, "size").Value = 21.5M; Field<NumericUpDown>(dialog, "partOpacity").Value = 74;
            Field<CheckBox>(dialog, "italic").Checked = true;
            Call(dialog, "MovePart", -1);
            TimerOptions edited = (TimerOptions)Call(dialog, "CaptureOptions");
            TimerPart text = edited.Parts[edited.Parts.Count - 2];
            Check(text.Text == " 赛段 / Split " && text.Style.FontSize == 21.5F && text.Style.Italic && text.Style.Opacity == 74 && edited.Opacity == 62, "custom part style/reorder failed");
            Check(edited.Parts.First().Style.FontSize == 35, "editing text style changed numeric parts");
            Check(legacy.Parts == null && legacy.TextTemplate == "原样 / Legacy {time}", "draft mutated original timer");
            Field<ComboBox>(dialog, "partKind").SelectedIndex = (int)TimerPartKind.Milliseconds;
            Field<NumericUpDown>(dialog, "digits").Value = 1;
            TimerOptions converted = (TimerOptions)Call(dialog, "CaptureOptions");
            Check(converted.Parts[converted.Parts.Count - 2].Kind == TimerPartKind.Milliseconds && converted.Parts[converted.Parts.Count - 2].Digits == 1, "part kind/precision edit failed");
            Call(dialog, "RemovePart"); Check(((TimerOptions)Call(dialog, "CaptureOptions")).Parts.Count == preset.Parts.Count, "part deletion failed");
            MacroConfig config = Config(); config.Timer = edited; config.NetworkControl.Hotkey = null;
            config.NetworkControl.BlockHotkey = Hotkey(Keys.F9); config.NetworkControl.RestoreHotkey = Hotkey(Keys.F10); config.NetworkControl.BlockSound = false; config.NetworkControl.RestoreSound = true;
            string path = Path.Combine(root, "parts-and-network.xml"); new ConfigStore().Save(path, config);
            MacroConfig reopened = ConfigPackageSerializer.DeserializeConfigFromFile(path);
            Check(Xml(config.Timer) == Xml(reopened.Timer) && Xml(config.NetworkControl) == Xml(reopened.NetworkControl), "combined part/new shortcut settings failed real file roundtrip");
            MacroConfig clone = ConfigPackageSerializer.CloneConfig(reopened); clone.Timer.Parts[0].Style.FontSize = 110; clone.NetworkControl.RestoreHotkey.VirtualKey = 40;
            Check(reopened.Timer.Parts[0].Style.FontSize == 35 && reopened.NetworkControl.RestoreHotkey.VirtualKey == (int)Keys.F10, "config clone aliases nested styles/shortcuts");
        }
    }
    private static void ShortcutValidation()
    {
        Check(!ToolFields.ValidShortcut(new TriggerSpec { Kind = InputKind.MouseX1 }), "tool shortcuts reject mouse before normalization");
        Check(!ToolFields.ValidShortcut(new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = 17 }), "tool shortcut requires terminal key");
        Check(ToolFields.ValidShortcut(new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = 75, Ctrl = true }), "ordinary keyboard shortcut accepted");
        using (TimerSettingsDialog dialog = new TimerSettingsDialog(new TimerOptions(), null, null))
        {
            Field<TimerOptions>(dialog, "draft").StartPauseHotkey = new TriggerSpec { Kind = InputKind.MouseLeft };
            Call(dialog, "Changed"); Check(!((Button)dialog.AcceptButton).Enabled, "invalid timer shortcut cannot silently disappear on save");
            using (Font selected = new Font("Segoe UI", 24, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point)) Call(dialog, "ApplySelectedFont", selected);
            Check(Math.Abs((double)Field<NumericUpDown>(dialog, "size").Value - 32) < .01, "font dialog point size converts to physical pixels");
            Check(Field<CheckBox>(dialog, "bold").Checked && Field<CheckBox>(dialog, "italic").Checked, "font dialog preserves selected bold and italic");
            for (Control parent = Field<Button>(dialog, "start").Parent; parent != null; parent = parent.Parent)
                Check(!(parent is TabPage), "start control is visible outside tabs");
        }
        using (NetworkSettingsDialog dialog = new NetworkSettingsDialog(new NetworkControlOptions(), null))
        {
            Field<NetworkControlOptions>(dialog, "draft").BlockHotkey = new TriggerSpec { Kind = InputKind.MouseLeft };
            Call(dialog, "Changed"); Check(!((Button)dialog.AcceptButton).Enabled, "invalid network shortcut cannot silently disappear on save");
        }
    }
    private static void Conflicts()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        {
            Localizer.SetLanguage(language); MacroConfig c = Config(); c.Language = language;
            using (MainForm main = new MainForm(c, Path.Combine(root, "conflicts-" + language), new NoOutput()))
            {
                Check((string)Call(main, "TimerShortcutConflict", c.Timer) == "", "distinct timer shortcuts accepted " + language);
                Check((string)Call(main, "NetworkShortcutConflict", c.NetworkControl) == "", "distinct network shortcut accepted " + language);
                Check((string)Call(main, "CrosshairShortcutConflict", c.Crosshair) == "", "distinct crosshair shortcut accepted " + language);
                foreach (TriggerSpec occupied in new[] { c.Crosshair.Hotkey, c.NetworkControl.Hotkey })
                { TimerOptions candidate = c.Timer.Clone(); candidate.StartPauseHotkey = occupied.Clone(); Check(((string)Call(main, "TimerShortcutConflict", candidate)).Length > 0, "timer rejects occupied tool shortcut " + language); }
                foreach (TriggerSpec occupied in new[] { c.Crosshair.Hotkey, c.Timer.StartPauseHotkey, c.Timer.ResetHotkey, c.Timer.VisibilityHotkey })
                { NetworkControlOptions candidate = c.NetworkControl.Clone(); candidate.Hotkey = occupied.Clone(); Check(((string)Call(main, "NetworkShortcutConflict", candidate)).Length > 0, "network rejects occupied tool shortcut " + language); }
                foreach (TriggerSpec occupied in new[] { c.NetworkControl.Hotkey, c.Timer.StartPauseHotkey, c.Timer.ResetHotkey, c.Timer.VisibilityHotkey })
                { CrosshairOptions candidate = c.Crosshair.Clone(); candidate.Hotkey = occupied.Clone(); Check(((string)Call(main, "CrosshairShortcutConflict", candidate)).Length > 0, "crosshair rejects occupied tool shortcut " + language); }
                TimerOptions duplicate = c.Timer.Clone(); duplicate.ResetHotkey = duplicate.StartPauseHotkey.Clone();
                Check(((string)Call(main, "TimerShortcutConflict", duplicate)).Length > 0, "timer rejects duplicate controls " + language);
                duplicate.Enabled = false; Check((string)Call(main, "TimerShortcutConflict", duplicate) == "", "disabled timer reserves no shortcut " + language);
                foreach (string kind in new[] { "timer", "network", "crosshair" })
                {
                    Check(Conflict(main, c, kind, c.PanicTrigger).Length > 0, kind + " rejects emergency-stop chord " + language);
                    c.Macros.Add(new MacroDefinition { Name = "测试 / Test", Enabled = true, Trigger = Hotkey(Keys.F4) });
                    Check(Conflict(main, c, kind, Hotkey(Keys.F4)).Length > 0, kind + " rejects active macro chord " + language); c.Macros.Clear();
                    c.MappingLayers.Add(new MappingLayerDefinition { Name = "分组 / Layer", SwitchTrigger = Hotkey(Keys.F3) });
                    Check(Conflict(main, c, kind, Hotkey(Keys.F3)).Length > 0, kind + " rejects layer chord " + language); c.MappingLayers.Clear();
                }
                NetworkControlOptions missing = c.NetworkControl.Clone(); missing.Hotkey = null;
                Check(((string)Call(main, "NetworkShortcutConflict", missing)).Length > 0, "enabled network requires hotkey " + language);
                InputEventInfo extraModifier = Event(c.Timer.StartPauseHotkey); extraModifier.Shift = true;
                Check(!(bool)typeof(MainForm).GetMethod("MatchesToolShortcut", StaticHidden).Invoke(null, new object[] { c.Timer.StartPauseHotkey, extraModifier }), "extra modifiers do not trigger a tool " + language);
            }
        }
    }
    private static string Conflict(MainForm main, MacroConfig c, string kind, TriggerSpec key)
    {
        if (kind == "timer") { TimerOptions o = c.Timer.Clone(); o.StartPauseHotkey = key.Clone(); return (string)Call(main, "TimerShortcutConflict", o); }
        if (kind == "network") { NetworkControlOptions o = c.NetworkControl.Clone(); o.Hotkey = key.Clone(); return (string)Call(main, "NetworkShortcutConflict", o); }
        CrosshairOptions crosshair = c.Crosshair.Clone(); crosshair.Hotkey = key.Clone(); return (string)Call(main, "CrosshairShortcutConflict", crosshair);
    }
    private static void Safety()
    {
        MacroConfig c = Config(); NoOutput output = new NoOutput();
        using (MainForm main = new MainForm(c, Path.Combine(root, "safety"), output))
        {
            IntPtr handle = main.Handle; Call(main, "ApplyScreenTools");
            TimerController timer = Field<TimerController>(main, "timerController");
            Check(!timer.IsRunning && timer.Elapsed == TimeSpan.Zero, "timer launches paused at zero");
            foreach (string guard in new[] { "uiSafetyModalDepth", "pauseHotkeys", "uiSafetyPauseRequested" })
            {
                object active = guard == "uiSafetyModalDepth" ? (object)1 : true;
                object inactive = guard == "uiSafetyModalDepth" ? (object)0 : false;
                Set(main, guard, active);
                Check(!(bool)Call(main, "HandleScreenToolShortcut", Event(c.Timer.StartPauseHotkey)), "timer respects pre-existing editor protection " + guard);
                Check(!(bool)Call(main, "HandleScreenToolShortcut", Event(c.NetworkControl.Hotkey)), "network respects pre-existing editor protection " + guard);
                Check(!(bool)Call(main, "HandleCrosshairShortcut", Event(c.Crosshair.Hotkey)), "crosshair respects pre-existing editor protection " + guard);
                Set(main, guard, inactive);
                Check((bool)Call(main, "HandleScreenToolShortcut", Event(c.Timer.StartPauseHotkey)), "timer command admitted before guard transition " + guard);
                Set(main, guard, active); Pump(25);
                Check(!timer.IsRunning, "queued timer command rechecks editor protection " + guard); Set(main, guard, inactive);
            }
            Check(Field<int>(main, "networkOperationPending") == 0 && Field<NetworkController>(main, "networkController") == null, "protected events never start network helper");
            Set(main, "manualTriggerSuspend", true);
            Check((bool)Call(main, "HandleScreenToolShortcut", Event(c.Timer.StartPauseHotkey)), "manual macro pause permits timer shortcut");
            Pump(25); Check(timer.IsRunning, "timer still runs while macro triggers are manually paused"); timer.Pause();
            Check((bool)Call(main, "HandleCrosshairShortcut", Event(c.Crosshair.Hotkey)), "manual macro pause permits crosshair shortcut");
            // Admission is the behavior under test; suppress screen output before the queued toggle.
            c.Crosshair.Enabled = false; Pump(25); c.Crosshair.Enabled = true;
            using (DelayedFactory factory = new DelayedFactory())
            {
                factory.Release.Set(); NetworkController network = new NetworkController(factory); Set(main, "networkController", network);
                Check((bool)Call(main, "HandleScreenToolShortcut", Event(c.NetworkControl.Hotkey)), "manual macro pause permits network shortcut");
                Until(() => Field<int>(main, "networkOperationPending") == 0, "fake network command settles during manual macro pause");
                Check(factory.Connection.Requests == 1 && network.IsBlocked, "network command runs through fake helper during manual macro pause");
                network.Dispose(); Set(main, "networkController", null);
            }
            Set(main, "manualTriggerSuspend", false);
            c.Crosshair.Hotkey = c.PanicTrigger.Clone(); c.Timer.StartPauseHotkey = c.PanicTrigger.Clone(); c.NetworkControl.Hotkey = c.PanicTrigger.Clone();
            foreach (string state in new[] { "normal", "paused", "modal", "recording", "capture" })
            {
                if (state == "paused") Set(main, "pauseHotkeys", true);
                if (state == "modal") Set(main, "uiSafetyModalDepth", 1);
                if (state == "recording") Set(main, "recordingActive", true);
                if (state == "capture") { FieldInfo capture = typeof(MainForm).GetField("captureMode", Hidden); capture.SetValue(main, Enum.Parse(capture.FieldType, "Trigger")); }
                Check((bool)Call(main, "HandleTerminalInputCore", Event(c.PanicTrigger), false), "emergency stop consumes input before tools in " + state);
                Pump(20); Check(!timer.IsRunning && Field<int>(main, "networkOperationPending") == 0, "emergency stop did not invoke tool in " + state);
                Set(main, "pauseHotkeys", false); Set(main, "uiSafetyModalDepth", 0); Set(main, "recordingActive", false);
                FieldInfo mode = typeof(MainForm).GetField("captureMode", Hidden); mode.SetValue(main, Enum.Parse(mode.FieldType, "None"));
            }
            Check(output.Writes == 0, "isolated integration never sends real key or mouse output");
        }
    }
    private static void NetworkExitRaces()
    {
        // Inject a fake helper factory. Neither test invokes UAC, WFP, or the real network stack.
        using (DelayedFactory factory = new DelayedFactory())
        using (MainForm main = new MainForm(Config(), Path.Combine(root, "network-exit-elevation"), new NoOutput()))
        {
            IntPtr handle = main.Handle; Set(main, "networkController", new NetworkController(factory));
            Call(main, "QueueNetworkOperation", false);
            Check(factory.Started.WaitOne(2000), "network worker entered simulated elevation");
            Stopwatch stop = Stopwatch.StartNew(); Call(main, "DisposeScreenTools"); stop.Stop();
            Check(stop.ElapsedMilliseconds < 500, "root shutdown does not wait behind elevation");
            factory.Release.Set(); Until(() => Field<int>(main, "networkOperationPending") == 0, "network worker settles after root shutdown");
            Check(factory.Connection.Disposals == 1 && factory.Connection.Requests == 0, "late elevated helper disposed before issuing block");
            Check(Field<NetworkController>(main, "networkController") == null, "shutdown does not resurrect network controller");
        }
        using (DelayedFactory factory = new DelayedFactory())
        using (MainForm main = new MainForm(Config(), Path.Combine(root, "network-exit-queued"), new NoOutput()))
        {
            IntPtr handle = main.Handle; Set(main, "networkController", new NetworkController(factory));
            object gate = Field<object>(main, "screenToolsGate");
            lock (gate) { Call(main, "QueueNetworkOperation", false); Call(main, "DisposeScreenTools"); }
            Until(() => Field<int>(main, "networkOperationPending") == 0, "worker queued before exit settles");
            Check(factory.Starts == 0 && Field<NetworkController>(main, "networkController") == null, "queued worker cannot start helper after root exit");
        }
    }
    private static void Existing(string method, params object[] args)
    { typeof(SecondaryLayoutTests).GetMethod(method, StaticHidden).Invoke(null, args); }
    private static IEnumerable<Control> All(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (Control child2 in All(child)) yield return child2; } }
    private static void DialogLayouts()
    {
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
        foreach (bool timer in new[] { true, false })
        foreach (bool save in new[] { true, false })
        {
            Localizer.SetLanguage(language); MacroConfig source = Config(); source.Timer.MonitorDevice = "UNPLUGGED-DISPLAY";
            source.Timer.TextTemplate = "原始 / Original {time}";
            string timerBefore = Xml(source.Timer), networkBefore = Xml(source.NetworkControl);
            string name = (timer ? "timer" : "network") + "-" + language + "-" + (int)(scale * 100) + (save ? "-save" : "-cancel");
            typeof(SecondaryLayoutTests).GetField("context", StaticHidden).SetValue(null, name);
            using (Form dialog = timer ? (Form)new TimerSettingsDialog(source.Timer, null, null) : new NetworkSettingsDialog(source.NetworkControl, null))
            {
                if (timer)
                {
                    Call(dialog, "ApplyPreset", TimerTimeFormat.MinutesSecondsMilliseconds);
                    Call(dialog, "AddPart");
                    Field<TextBox>(dialog, "partText").Text = "本次 / Run\r\n测试 / Test";
                    Field<NumericUpDown>(dialog, "x").Value = -37; Field<NumericUpDown>(dialog, "y").Value = 99;
                    Field<NumericUpDown>(dialog, "size").Value = 39.5M;
                    Check(Field<NumericUpDown>(dialog, "x").Value == -37, "precise negative position survives coarse slider clamping " + name);
                }
                else
                {
                    ComboBox mode = Field<ComboBox>(dialog, "mode"); mode.SelectedIndex = 0;
                    Field<TextBox>(dialog, "path").Text = Path.GetFullPath(Application.ExecutablePath);
                    Check(Field<TextBox>(dialog, "path").Enabled, "application scope exposes path " + name);
                    mode.SelectedIndex = 1; Check(!Field<TextBox>(dialog, "path").Enabled, "whole computer scope disables path " + name);
                }
                dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-20000, -20000); dialog.ShowInTaskbar = false;
                Size requested = dialog.ClientSize;
                if (scale != 1F)
                {
                    dialog.Scale(new SizeF(scale, scale)); Control[] controls = new[] { (Control)dialog }.Concat(All(dialog)).ToArray();
                    Font[] fonts = controls.Select(c => new Font(c.Font.FontFamily, c.Font.Size * scale, c.Font.Style)).ToArray();
                    for (int i = 0; i < controls.Length; i++) controls[i].Font = fonts[i];
                    dialog.Disposed += delegate { foreach (Font font in fonts) font.Dispose(); };
                    requested = new Size((int)(requested.Width * scale), (int)(requested.Height * scale)); dialog.ClientSize = requested;
                }
                dialog.Show(); Pump(20); windows++;
                Size chrome = new Size(dialog.Width - dialog.ClientSize.Width, dialog.Height - dialog.ClientSize.Height);
                Existing("SyntheticSize", dialog, new Size(requested.Width + chrome.Width, requested.Height + chrome.Height));
                if (scale == 1F)
                {
                    if (timer)
                    {
                        ScrollableControl stylePage = (ScrollableControl)Field<TableLayoutPanel>(dialog, "detail").Parent;
                        Check(!stylePage.VerticalScroll.Visible, "default selected part exposes every style control without scrolling " + name);
                        ListView list = Field<ListView>(dialog, "partList");
                        Check(list.Items[list.Items.Count - 1].Bounds.Bottom <= list.ClientSize.Height, "default content list shows the full preset and custom text " + name);
                    }
                    else Check(!All(dialog).OfType<ScrollableControl>().Any(c => c.Visible && c.AutoScroll && c.VerticalScroll.Visible), "default network settings fit without page scrolling " + name);
                }
                AuditAllTabs(dialog);
                Size original = dialog.Size;
                Existing("SyntheticSize", dialog, new Size((int)(original.Width * .90), (int)(original.Height * .90))); Pump(10); AuditAllTabs(dialog);
                Existing("SyntheticSize", dialog, original); Pump(10); AuditAllTabs(dialog);
                if (save && scale == 1F)
                {
                    foreach (TabControl tabs in All(dialog).OfType<TabControl>()) tabs.SelectedIndex = 0;
                    Pump(5);
                    using (Bitmap image = new Bitmap(dialog.Width, dialog.Height))
                    { dialog.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(root, name + ".png")); }
                }
                if (save) ((Button)dialog.AcceptButton).PerformClick(); else { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
                Check(Xml(source.Timer) == timerBefore && Xml(source.NetworkControl) == networkBefore, "draft editing leaves saved source untouched " + name);
                if (timer)
                {
                    TimerOptions result = ((TimerSettingsDialog)dialog).Result;
                    Check(save ? result != null : result == null, "timer save/cancel result " + name);
                    if (save)
                    {
                        Check(result.MonitorDevice == "UNPLUGGED-DISPLAY", "saving preserves disconnected monitor choice " + name);
                        TimerPart custom = result.Parts.Last();
                        Check(custom.Text == "本次 / Run\n测试 / Test" && result.X == -37 && result.Y == 99 && custom.Style.FontSize == 39.5F, "timer captures bilingual text and precise appearance/position " + name);
                    }
                }
                else
                {
                    NetworkControlOptions result = ((NetworkSettingsDialog)dialog).Result;
                    Check(save ? result != null && result.Mode == NetworkBlockMode.EntireComputer : result == null, "network save/cancel captures chosen scope " + name);
                }
            }
        }
        string[] failures = ((IEnumerable<string>)typeof(SecondaryLayoutTests).GetField("failures", StaticHidden).GetValue(null)).ToArray();
        File.WriteAllLines(Path.Combine(root, "layout-issues.txt"), failures);
        Check(failures.Length == 0, string.Join(Environment.NewLine, failures.Take(15)));
        checks += (int)typeof(SecondaryLayoutTests).GetField("checks", StaticHidden).GetValue(null);
    }
    private static void AuditAllTabs(Form form)
    {
        Existing("Audit", form);
        foreach (TabControl tabs in All(form).OfType<TabControl>())
            foreach (TabPage page in tabs.TabPages) { tabs.SelectedTab = page; Pump(5); Existing("Audit", form); }
        foreach (ComboBox choice in All(form).OfType<ComboBox>().Where(c => c.Visible)) Existing("ExerciseChoice", choice);
    }
    private static void ModalWorkflow()
    {
        foreach (bool timer in new[] { true, false }) foreach (bool save in new[] { true, false })
        {
            MacroConfig config = Config(); config.Timer.Enabled = config.Crosshair.Enabled = config.NetworkControl.Enabled = false; config.NetworkControl.Mode = NetworkBlockMode.Executable;
            using (MainForm main = new MainForm(config, Path.Combine(root, "modal-" + timer + "-" + save), new NoOutput()))
            using (System.Windows.Forms.Timer close = new System.Windows.Forms.Timer())
            {
                IntPtr handle = main.Handle; bool found = false; Exception callbackFailure = null;
                close.Interval = 20; close.Tick += delegate
                {
                    Form dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(f => timer ? f is TimerSettingsDialog : f is NetworkSettingsDialog);
                    if (dialog == null) return; close.Stop(); found = true;
                    try
                    {
                        Check(Field<int>(main, "uiSafetyModalDepth") > 0, "actual tool dialog enters macro edit protection");
                        if (timer) { Call(dialog, "AddPart"); Field<TextBox>(dialog, "partText").Text = "保存 / Saved"; }
                        else Field<ComboBox>(dialog, "mode").SelectedIndex = 1;
                        if (save) ((Button)dialog.AcceptButton).PerformClick(); else { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
                    }
                    catch (Exception ex) { callbackFailure = ex; dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
                };
                close.Start(); Call(main, timer ? "ShowTimerDialog" : "ShowNetworkDialog", null, EventArgs.Empty);
                if (callbackFailure != null) throw callbackFailure;
                Check(found && Field<int>(main, "uiSafetyModalDepth") == 0, "actual tool dialog exits edit protection after save/cancel");
                Check(timer ? (config.Timer.Parts != null && config.Timer.Parts.Last().Text == "保存 / Saved") == save : (config.NetworkControl.Mode == NetworkBlockMode.EntireComputer) == save, "actual tool dialog applies settings only after save");
            }
        }
    }
}
