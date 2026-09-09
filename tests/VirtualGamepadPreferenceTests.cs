using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using InputStitch;

internal static class VirtualGamepadPreferenceTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition)
        {
            Console.WriteLine("FAIL virtual gamepad preference: " + message);
            throw new Exception("Check failed: " + message);
        }
    }

    private static object Field(object instance, string name)
    {
        return instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }

    private static object Call(object instance, string name, params object[] args)
    {
        return instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, args);
    }

    private static object StaticCall(Type type, string name, params object[] args)
    {
        return type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }

    [STAThread]
    public static int Main()
    {
        Application.EnableVisualStyles();
        EmbeddedDependencyLoader.Register();
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-VirtualGamepadPreference-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Check(new MacroConfig().GamepadDeviceType == VirtualGamepadTypes.Xbox360,
                "existing default remains Xbox for backward-compatible fresh-install behavior");
            Check(VirtualGamepadTypes.Normalize(VirtualGamepadTypes.None) == VirtualGamepadTypes.None,
                "None survives virtual-gamepad type normalization");
            Check(VirtualGamepadTypes.IsDisabled("none") && !VirtualGamepadTypes.IsDisabled(VirtualGamepadTypes.Xbox360),
                "disabled type detection is case-insensitive and does not affect Xbox");

            GamepadOutput.Disconnect();
            GamepadOutput.Configure(VirtualGamepadTypes.None);
            Check(GamepadOutput.PreferredType == VirtualGamepadTypes.None && !GamepadOutput.IsConnected,
                "configuring None does not create a virtual controller");
            bool disabledThrown = false;
            try { GamepadOutput.EnsureConnected(); }
            catch (GamepadOutputException ex) { disabledThrown = ex.FailureKind == GamepadFailureKind.OutputDisabled; }
            Check(disabledThrown && !GamepadOutput.IsConnected,
                "explicit connection is refused while None is selected without touching ViGEm");

            MacroConfig disabledConfig = new MacroConfig();
            disabledConfig.GamepadDeviceType = VirtualGamepadTypes.None;
            disabledConfig.GamepadRouterEnabled = false;
            disabledConfig.IdleGamepad.Enabled = true;
            StaticCall(typeof(MainForm), "NormalizeConfig", disabledConfig);
            Check(disabledConfig.GamepadDeviceType == VirtualGamepadTypes.None,
                "configuration normalization preserves explicit no-virtual-controller preference");
            Check(!disabledConfig.IdleGamepad.Enabled,
                "Idle gamepad automation is disabled when virtual-controller creation is disabled");

            MacroConfig routerConfig = new MacroConfig();
            routerConfig.GamepadDeviceType = VirtualGamepadTypes.None;
            routerConfig.GamepadRouterEnabled = true;
            StaticCall(typeof(MainForm), "NormalizeConfig", routerConfig);
            Check(routerConfig.GamepadDeviceType == VirtualGamepadTypes.Xbox360,
                "controller merging still requires and normalizes to a virtual Xbox controller");

            Localizer.SetLanguage(Localizer.Chinese);
            using (SettingsDialog dialog = new SettingsDialog(disabledConfig))
            {
                ComboBox typeBox = (ComboBox)Field(dialog, "gamepadTypeBox");
                Check(typeBox.Items.Count == 3 && typeBox.SelectedIndex == 2 && dialog.SelectedGamepadType == VirtualGamepadTypes.None,
                    "Settings exposes Do not create as the third virtual-controller choice");
                Button connect = null;
                foreach (Control c in Descendants(dialog))
                {
                    Button b = c as Button;
                    if (b != null && b.Text.IndexOf("检测驱动并连接", StringComparison.Ordinal) >= 0) { connect = b; break; }
                }
                Check(connect != null && !connect.Enabled,
                    "driver/connect button is disabled while no virtual controller is selected");

                CheckBox router = (CheckBox)Field(dialog, "gamepadRouterBox");
                router.Checked = true;
                Check(dialog.SelectedGamepadType == VirtualGamepadTypes.Xbox360 && !typeBox.Enabled,
                    "enabling controller merging temporarily forces Xbox output");
                router.Checked = false;
                Check(dialog.SelectedGamepadType == VirtualGamepadTypes.None && typeBox.Enabled,
                    "turning controller merging back off restores prior no-controller choice");
            }

            MacroDefinition gamepadMacro = new MacroDefinition
            {
                Name = "gamepad output",
                Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.South } }
            };
            MacroDefinition keyboardMacro = new MacroDefinition
            {
                Name = "keyboard output",
                Steps = new List<MacroStep> { new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.K } }
            };
            MacroConfig hostConfig = new MacroConfig();
            hostConfig.GamepadDeviceType = VirtualGamepadTypes.None;
            hostConfig.Macros = new List<MacroDefinition> { gamepadMacro, keyboardMacro };
            using (MainForm form = new MainForm(hostConfig, root))
            {
                Check(!(bool)Call(form, "EnsureGamepadReady", gamepadMacro, false),
                    "runtime refuses a gamepad-output macro while virtual output is disabled");
                Check((bool)Call(form, "EnsureGamepadReady", keyboardMacro, false),
                    "keyboard-only macro remains runnable while virtual output is disabled");
                Check((bool)Call(form, "EnsureGamepadReady", null, true) && !GamepadOutput.IsConnected,
                    "startup skips virtual-controller creation even when saved gamepad-output macros exist");
            }
        }
        finally
        {
            try { GamepadOutput.Disconnect(); } catch { }
            try { Directory.Delete(root, true); } catch { }
        }

        Console.WriteLine("PASS virtual gamepad preference: " + checks + " checks; no ViGEm device created.");
        return 0;
    }
}
