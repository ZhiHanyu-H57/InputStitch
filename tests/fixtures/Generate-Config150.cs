// Compile against the original v1.5.0 EXE, never against current production types.
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using InputStitch;
internal static class GenerateConfig150
{
    public static void Main(string[] args)
    {
        MacroConfig c = new MacroConfig { Language = "en-US", UpdateMode = "Manual",
            GamepadDeviceType = "None", UseScanCodeInput = false, ActivateTargetWindowOnUiRun = true,
            UiRunStartDelayMs = 470, KeepWindowTopMost = true, PauseMacroInRiskyUi = false,
            AutoSwitchProfiles = true, MinimizeToTray = false, HasSeenWelcome = true,
            LastShownReleaseSummaryVersion = "1.5.0", TargetProcessName = "ExampleGame",
            TargetWindowTitle = "Example game window", TargetWindowClass = "ExampleClass" };
        c.PanicTrigger = new TriggerSpec { Ctrl = true, Alt = true, Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F11 };
        c.MappingLayers.Clear();
        c.MappingLayers.Add(new MappingLayerDefinition { Id = "base", Name = "My base", IsBase = true });
        c.MappingLayers.Add(new MappingLayerDefinition { Id = "custom-old", Name = "Old custom layer", IsBase = false,
            SwitchTrigger = new TriggerSpec { Ctrl = true, Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 } });
        MacroDefinition sequence = new MacroDefinition { Name = "1.5.0 sequence / 顺序", Description = "Legacy fixture",
            Trigger = new TriggerSpec { Ctrl = true, Alt = true, Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 },
            RunMode = TriggerRunMode.Toggle, RepeatCount = 7, SuppressTrigger = false, MappingLayerId = "custom-old" };
        sequence.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A,
            ScanCode = 30, Action = MacroAction.Down, HoldMs = 25, DelayMs = 81,
            RandomDelay = true, RandomDelayMinMs = 40, RandomDelayMaxMs = 120 });
        sequence.Steps.Add(new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.A, Action = MacroAction.Up, HoldMs = 0, DelayMs = 13 });
        c.Macros.Add(sequence);
        MacroDefinition held = new MacroDefinition { Name = "1.5.0 mouse hold", RunMode = TriggerRunMode.Hold,
            Trigger = new TriggerSpec { Kind = InputKind.MouseX1 }, Infinite = true };
        held.Steps.Add(new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.LeftStick,
            GamepadX = -32, GamepadY = 77, GamepadValue = 83, HoldMs = 19, DelayMs = 31 });
        c.Macros.Add(held);
        MacroDefinition disabled = new MacroDefinition { Name = "1.5.0 disabled", Enabled = false,
            Trigger = new TriggerSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.RightShoulder, GamepadUserIndex = 2 } };
        disabled.Steps.Add(new MacroStep { Kind = InputKind.MouseRight, Action = MacroAction.Press, HoldMs = 45, DelayMs = 123 });
        c.Macros.Add(disabled);
        c.IdleGamepad.IdleSeconds = 83; c.IdleGamepad.HoldMilliseconds = 211;
        c.IdleGamepad.TargetScopeInitialized = true; c.IdleGamepad.TargetProcessName = "ExampleIdle";
        c.IdleGamepad.TargetWindowTitle = "Idle example"; c.IdleGamepad.TargetWindowClass = "IdleClass";
        c.IdleGamepad.Pulse.GamepadControl = GamepadControl.RightStick;
        c.IdleGamepad.Pulse.GamepadX = 25; c.IdleGamepad.Pulse.GamepadY = -75;
        sequence.Activator.Mode = "LongPress"; sequence.Activator.LongPressMs = 420;
        sequence.Conditions.ForegroundProcessName = "ExampleGame";
        c.Crosshair.Enabled = true; c.Crosshair.Visible = false;
        c.Crosshair.Style = CrosshairStyle.RingDot; c.Crosshair.Size = 38;
        c.Crosshair.Color = "#34A2F0"; c.Crosshair.OutlineColor = "#D10C4B";
        c.Crosshair.Opacity = 66; c.Crosshair.OffsetX = -17; c.Crosshair.OffsetY = 23;
        c.Crosshair.Outline = false; c.Crosshair.OutlineWidth = 3;
        c.Sdl3InputEnabled = false;
        using (FileStream output = File.Create(args[0])) new XmlSerializer(typeof(MacroConfig)).Serialize(output, c);
    }
}
