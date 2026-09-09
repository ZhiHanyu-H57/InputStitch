using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using InputStitch;

internal static class LayerTests
{
    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        public void SendDown(InputSpec input) { Events.Add("D:" + Describe(input)); }
        public void SendUp(InputSpec input) { Events.Add("U:" + Describe(input)); }
        public void NeutralizeGamepad() { Events.Add("N"); }
        private static string Describe(InputSpec input)
        {
            if (input == null) return "null";
            if (input.Kind == InputKind.Gamepad) return "G:" + input.GamepadControl.ToString();
            return "I:" + input.Kind.ToString() + ":" + input.VirtualKey.ToString();
        }
    }

    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static object Call(object obj, string name, params object[] args)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    }

    private static object Field(object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }

    private static object StaticCall(string name, params object[] args)
    {
        return typeof(MainForm).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }

    private static bool WaitUntil(Func<bool> condition, int milliseconds)
    {
        int elapsed = 0;
        while (elapsed < milliseconds)
        {
            if (condition()) return true;
            Thread.Sleep(10);
            elapsed += 10;
        }
        return condition();
    }

    private static MacroDefinition Held(string name, string layer, Keys trigger, GamepadControl output)
    {
        MacroDefinition macro = new MacroDefinition();
        macro.Name = name;
        macro.Enabled = true;
        macro.MappingLayerId = layer;
        macro.RunMode = TriggerRunMode.Hold;
        macro.Infinite = true;
        macro.Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)trigger };
        macro.Steps = new List<MacroStep>
        {
            new MacroStep { Kind = InputKind.Gamepad, GamepadControl = output, Action = MacroAction.Down, DelayMs = 0 }
        };
        return macro;
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-LayerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            MacroConfig deletionCompatibility = new MacroConfig();
            deletionCompatibility.MappingLayers.RemoveAll(delegate(MappingLayerDefinition layer)
            {
                return layer != null && string.Equals(layer.Id, MappingLayerIds.Layer1, StringComparison.OrdinalIgnoreCase);
            });
            StaticCall("NormalizeMappingLayers", deletionCompatibility);
            bool layer1Recreated = false;
            foreach (MappingLayerDefinition layer in deletionCompatibility.MappingLayers)
                if (layer != null && string.Equals(layer.Id, MappingLayerIds.Layer1, StringComparison.OrdinalIgnoreCase)) layer1Recreated = true;
            Check(!layer1Recreated, "deleted legacy Layer 1 is not forced back by normalization");
            Check(deletionCompatibility.MappingLayers.Count == 1 && deletionCompatibility.MappingLayers[0].IsBase,
                "Base remains mandatory when all user layers are removed");

            string layerRoundTripPath = Path.Combine(root, "layers-roundtrip.xml");
            using (FileStream stream = File.Create(layerRoundTripPath))
                new XmlSerializer(typeof(MacroConfig)).Serialize(stream, deletionCompatibility);
            MacroConfig layerRoundTrip;
            using (FileStream stream = File.OpenRead(layerRoundTripPath))
                layerRoundTrip = (MacroConfig)new XmlSerializer(typeof(MacroConfig)).Deserialize(stream);
            StaticCall("NormalizeMappingLayers", layerRoundTrip);
            bool roundTripLayer1 = false;
            foreach (MappingLayerDefinition layer in layerRoundTrip.MappingLayers)
                if (layer != null && string.Equals(layer.Id, MappingLayerIds.Layer1, StringComparison.OrdinalIgnoreCase)) roundTripLayer1 = true;
            Check(!roundTripLayer1 && layerRoundTrip.MappingLayers.Count == 1 && layerRoundTrip.MappingLayers[0].IsBase,
                "deleting legacy Layer 1 survives a real XML save/reload round trip");

            MacroDefinition layerFirst = Held("layer first", MappingLayerIds.Layer1, Keys.F8, GamepadControl.East);
            MacroDefinition baseSecond = Held("base second", MappingLayerIds.Base, Keys.F8, GamepadControl.South);
            MacroDefinition ordinaryLayerTagged = new MacroDefinition
            {
                Name = "ordinary Layer 1",
                Enabled = true,
                MappingLayerId = MappingLayerIds.Layer1,
                RunMode = TriggerRunMode.Toggle,
                Infinite = false,
                Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 },
                Steps = new List<MacroStep>
                {
                    new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.K, Action = MacroAction.Down, DelayMs = 5000 },
                    new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.K, Action = MacroAction.Up, DelayMs = 0 }
                }
            };
            MappingLayerDefinition combatLayer = new MappingLayerDefinition
            {
                Id = "layer-combat",
                Name = "Combat",
                IsBase = false,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 }
            };
            MappingLayerDefinition drivingLayer = new MappingLayerDefinition
            {
                Id = "layer-driving",
                Name = "Driving",
                IsBase = false,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F11 }
            };
            MacroDefinition combatMacro = Held("combat", combatLayer.Id, Keys.F7, GamepadControl.West);
            MacroDefinition drivingMacro = Held("driving", drivingLayer.Id, Keys.F6, GamepadControl.North);
            MacroConfig config = new MacroConfig();
            config.MappingLayers.Add(combatLayer);
            config.MappingLayers.Add(drivingLayer);
            config.MappingLayers[0].SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F12 };
            config.Macros = new List<MacroDefinition> { layerFirst, baseSecond, ordinaryLayerTagged, combatMacro, drivingMacro };
            FakeBackend backend = new FakeBackend();
            using (MainForm form = new MainForm(config, root, backend))
            {
                InputEventInfo f8 = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 } };
                InputEventInfo f9 = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9 } };
                InputEventInfo f10 = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 } };
                InputEventInfo f11 = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F11 } };
                InputEventInfo f12 = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F12 } };

                Check(config.MappingLayers.Count == 4, "Base plus three user-defined layers can coexist");
                Check(object.ReferenceEquals(Call(form, "SelectLayerSwitchTarget", f10), combatLayer), "custom layer F10 shortcut resolves to Combat");
                Check(object.ReferenceEquals(Call(form, "SelectLayerSwitchTarget", f11), drivingLayer), "custom layer F11 shortcut resolves to Driving");
                Check(object.ReferenceEquals(Call(form, "SelectLayerSwitchTarget", f12), config.MappingLayers[0]), "Base can have its own return-to-Base shortcut");
                Call(form, "SwitchActiveMappingLayer", combatLayer.Id);
                Check((string)Field(form, "activeMappingLayerId") == combatLayer.Id && (bool)Call(form, "IsMacroLayerEligible", combatMacro),
                    "arbitrary named layer can become active and make its macros eligible");
                Check(!(bool)Call(form, "IsMacroLayerEligible", layerFirst), "activating a custom layer does not also enable Layer 1");
                Call(form, "SwitchActiveMappingLayer", drivingLayer.Id);
                Check((string)Field(form, "activeMappingLayerId") == drivingLayer.Id && !(bool)Call(form, "IsMacroLayerEligible", combatMacro),
                    "switching between arbitrary layers removes prior-layer eligibility");
                Call(form, "RemoveMappingLayerCore", drivingLayer);
                Check((string)Field(form, "activeMappingLayerId") == "" && !config.MappingLayers.Contains(drivingLayer),
                    "deleting the active arbitrary layer safely returns runtime eligibility to Base");
                Check(drivingMacro.MappingLayerId == MappingLayerIds.Base && (bool)Call(form, "IsMacroLayerEligible", drivingMacro),
                    "deleting a layer moves its macros back to Base instead of leaving dangling layer ids");
                combatLayer.Name = "Combat Renamed";
                Check(((string)Call(form, "LayerDisplayName", combatLayer)).IndexOf("Combat Renamed", StringComparison.Ordinal) >= 0,
                    "custom layer display uses editable user name instead of hard-coded Layer 1 text");
                Call(form, "SwitchActiveMappingLayer", combatLayer.Id);
                string observation = (string)Call(form, "BuildRuntimeObservationText");
                Check(observation.IndexOf("Combat Renamed", StringComparison.Ordinal) >= 0 &&
                    (observation.IndexOf("Controller takeover", StringComparison.OrdinalIgnoreCase) >= 0 || observation.IndexOf("手柄接管", StringComparison.Ordinal) >= 0),
                    "runtime observation reports user-facing active layer name and controller-takeover platform state");
                string diagnostics = (string)Call(form, "BuildDiagnosticsText");
                Check(diagnostics.IndexOf("MappingLayerCount:", StringComparison.Ordinal) >= 0 && diagnostics.IndexOf("Layer[layer-combat]", StringComparison.Ordinal) >= 0 && diagnostics.IndexOf("ControlledReplacement:", StringComparison.Ordinal) >= 0,
                    "diagnostics enumerate flexible layers and controlled-replacement state");
                Call(form, "SwitchActiveMappingLayer", "");

                MacroDefinition chosen = (MacroDefinition)Call(form, "SelectEligibleTriggerMacro", f8);
                Check(object.ReferenceEquals(chosen, baseSecond), "Base-only mode excludes Layer 1 even when Layer 1 is higher in list priority");
                Check((bool)Call(form, "IsMacroLayerEligible", baseSecond), "Base held mapping is always eligible");
                Check(!(bool)Call(form, "IsMacroLayerEligible", layerFirst), "inactive Layer 1 held mapping is not eligible");
                Check(!(bool)Call(form, "IsMacroLayerEligible", ordinaryLayerTagged), "inactive Layer 1 ordinary macro is not eligible");
                Check(Call(form, "SelectEligibleTriggerMacro", f9) == null,
                    "ordinary/timed macro in inactive Layer 1 cannot trigger");

                Call(form, "SwitchActiveMappingLayer", MappingLayerIds.Layer1);
                Check((string)Field(form, "activeMappingLayerId") == MappingLayerIds.Layer1, "Layer 1 becomes active at runtime");
                Check((bool)Call(form, "IsMacroLayerEligible", layerFirst), "Layer 1 held mapping becomes eligible");
                Check((bool)Call(form, "IsMacroLayerEligible", ordinaryLayerTagged), "Layer 1 ordinary macro becomes eligible");
                Check(object.ReferenceEquals((MacroDefinition)Call(form, "SelectEligibleTriggerMacro", f9), ordinaryLayerTagged),
                    "ordinary/timed macro can trigger when its Layer is active");
                chosen = (MacroDefinition)Call(form, "SelectEligibleTriggerMacro", f8);
                Check(object.ReferenceEquals(chosen, layerFirst), "normal list priority applies among Base + active layer");

                // Start a worker-backed ordinary Layer 1 macro plus one Base and one Layer 1 held mapping.
                // Switching to Base-only must immediately remove every Layer 1 output type while Base survives.
                MethodInfo start = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                Check(start != null, "ordinary macro start entrypoint is available");
                start.Invoke(form, new object[] { ordinaryLayerTagged, 0, null, false, false });
                OutputOwnershipManager ownership = (OutputOwnershipManager)Field(form, "outputOwnership");
                Check(WaitUntil(delegate
                {
                    foreach (InputSpec input in ownership.Snapshot().Merged)
                        if (input.Kind == InputKind.Keyboard && input.VirtualKey == (int)Keys.K) return true;
                    return false;
                }, 1500), "ordinary Layer 1 worker owns keyboard output while layer is active");

                Call(form, "StartParallelHeldMapping", baseSecond);
                Call(form, "StartParallelHeldMapping", layerFirst);
                Check(backend.Events.Contains("D:G:South") && backend.Events.Contains("D:G:East"), "Base and Layer 1 held mappings can own output concurrently");
                int beforeSwitch = backend.Events.Count;
                Call(form, "SwitchActiveMappingLayer", "");
                List<string> switchEvents = backend.Events.GetRange(beforeSwitch, backend.Events.Count - beforeSwitch);
                Check(switchEvents.Contains("U:G:East"), "leaving Layer 1 releases its held-mapping output");
                Check(!switchEvents.Contains("U:G:South"), "Base held output is not released by layer switch");
                bool keyboardStillMerged = false;
                foreach (InputSpec input in ownership.Snapshot().Merged)
                    if (input.Kind == InputKind.Keyboard && input.VirtualKey == (int)Keys.K) keyboardStillMerged = true;
                Check(!keyboardStillMerged, "leaving Layer 1 immediately removes worker-backed ordinary output");
                Check(WaitUntil(delegate { return !(bool)Call(form, "IsMacroActuallyRunning", ordinaryLayerTagged); }, 1500),
                    "leaving Layer 1 stops its ordinary worker runtime");
                Check((string)Field(form, "activeMappingLayerId") == "", "active layer is runtime-only Base state after switch");
                Check(!(bool)Call(form, "IsMacroLayerEligible", layerFirst), "Layer 1 held mapping becomes ineligible again");
                Check(!(bool)Call(form, "IsMacroLayerEligible", ordinaryLayerTagged), "Layer 1 ordinary macro becomes ineligible again");
                Check((bool)Call(form, "IsMacroLayerEligible", baseSecond), "Base remains eligible after switch");

                // Explicit blocked-until-release set participates in selection. This isolates the
                // eligibility rule without querying real keyboard state.
                HashSet<MacroDefinition> blocked = (HashSet<MacroDefinition>)Field(form, "layerBlockedUntilRelease");
                blocked.Add(baseSecond);
                Check(Call(form, "SelectEligibleTriggerMacro", f8) == null, "pre-held trigger block prevents activation after layer switch");
                Call(form, "HandleTerminalInputReleased", f8);
                Check(!blocked.Contains(baseSecond), "physical release removes layer-switch block");
                Check(object.ReferenceEquals((MacroDefinition)Call(form, "SelectEligibleTriggerMacro", f8), baseSecond),
                    "mapping can activate after release and fresh press");
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
        Console.WriteLine("PASS Layer v1: " + checks + " checks; isolated UI/fake backend, no hooks or devices.");
        return 0;
    }
}
