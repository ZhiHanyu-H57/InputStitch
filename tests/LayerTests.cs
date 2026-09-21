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

            // ---------------------------------------------------------------------------------
            // Momentary / hold-to-layer: holding a layer's switch shortcut activates that layer and
            // releasing it returns to the previous (persistent) layer. Base stays eligible, layer
            // cleanup is source-local, and a missed release edge is recovered from physical state.
            // ---------------------------------------------------------------------------------
            MappingLayerDefinition momentaryBase = new MappingLayerDefinition
            {
                Id = MappingLayerIds.Base,
                Name = "基础层",
                IsBase = true,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 }
            };
            MappingLayerDefinition auraLayer = new MappingLayerDefinition
            {
                Id = "layer-aura",
                Name = "Aura",
                IsBase = false,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F5 }
            };
            MappingLayerDefinition focusLayer = new MappingLayerDefinition
            {
                Id = "layer-focus",
                Name = "Focus",
                IsBase = false,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F6 }
            };
            MappingLayerDefinition stanceLayer = new MappingLayerDefinition
            {
                Id = "layer-stance",
                Name = "Stance",
                IsBase = false,
                MomentarySwitch = false,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F7 }
            };
            MappingLayerDefinition fallbackLayer = new MappingLayerDefinition
            {
                Id = "layer-fallback",
                Name = "Fallback",
                IsBase = false,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.RightThumb }
            };
            MappingLayerDefinition modifierLayer = new MappingLayerDefinition
            {
                Id = "layer-modifier",
                Name = "Modifier",
                IsBase = false,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F9, Ctrl = true }
            };
            MacroDefinition auraHeld = Held("momentary aura", auraLayer.Id, Keys.F3, GamepadControl.LeftShoulder);
            MacroDefinition baseHeld = Held("momentary base", MappingLayerIds.Base, Keys.F1, GamepadControl.South);
            MacroDefinition fallbackHeld = Held("momentary fallback", fallbackLayer.Id, Keys.F4, GamepadControl.RightShoulder);
            MacroDefinition auraWorker = new MacroDefinition
            {
                Name = "momentary worker",
                Enabled = true,
                MappingLayerId = auraLayer.Id,
                RunMode = TriggerRunMode.Toggle,
                Infinite = false,
                Trigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F2 },
                Steps = new List<MacroStep>
                {
                    new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.J, Action = MacroAction.Down, DelayMs = 5000 },
                    new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.J, Action = MacroAction.Up, DelayMs = 0 }
                }
            };

            MacroConfig momentaryConfig = new MacroConfig();
            momentaryConfig.MappingLayers.Clear();
            momentaryConfig.MappingLayers.Add(momentaryBase);
            momentaryConfig.MappingLayers.Add(auraLayer);
            momentaryConfig.MappingLayers.Add(focusLayer);
            momentaryConfig.MappingLayers.Add(stanceLayer);
            momentaryConfig.MappingLayers.Add(fallbackLayer);
            momentaryConfig.MappingLayers.Add(modifierLayer);
            momentaryConfig.Macros = new List<MacroDefinition> { auraHeld, baseHeld, fallbackHeld, auraWorker };

            string momentaryXmlPath = Path.Combine(root, "momentary-roundtrip.xml");
            using (FileStream stream = File.Create(momentaryXmlPath))
                new XmlSerializer(typeof(MacroConfig)).Serialize(stream, momentaryConfig);
            MacroConfig momentaryRoundTrip;
            using (FileStream stream = File.OpenRead(momentaryXmlPath))
                momentaryRoundTrip = (MacroConfig)new XmlSerializer(typeof(MacroConfig)).Deserialize(stream);
            MappingLayerDefinition roundTripAura = null;
            MappingLayerDefinition roundTripStance = null;
            foreach (MappingLayerDefinition layer in momentaryRoundTrip.MappingLayers)
            {
                if (layer == null) continue;
                if (layer.Id == auraLayer.Id) roundTripAura = layer;
                if (layer.Id == stanceLayer.Id) roundTripStance = layer;
            }
            Check(roundTripAura != null && roundTripAura.MomentarySwitch && roundTripAura.SwitchTrigger != null,
                "momentary layer keeps hold mode and switch shortcut through an XML save/reload round trip");
            Check(roundTripStance != null && !roundTripStance.MomentarySwitch,
                "click-to-switch layers stay in toggle mode through XML");

            FakeBackend momentaryBackend = new FakeBackend();
            using (MainForm momentaryForm = new MainForm(momentaryConfig, root, momentaryBackend))
            {
                InputEventInfo f5Down = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F5 } };
                InputEventInfo f5Up = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F5 } };
                InputEventInfo f6Up = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F6 } };
                InputEventInfo f8Up = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8 } };
                System.Collections.ICollection holds = (System.Collections.ICollection)Field(momentaryForm, "momentaryLayerHolds");

                Check(object.ReferenceEquals(Call(momentaryForm, "SelectLayerSwitchTarget", f5Down), auraLayer),
                    "momentary shortcut resolves through normal layer-switch press matching");
                Check(((string)Call(momentaryForm, "LayerDisplayName", auraLayer)).IndexOf("按住", StringComparison.Ordinal) >= 0,
                    "layer display marks hold-to-activate layers");

                Call(momentaryForm, "BeginMomentaryLayerHold", auraLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == auraLayer.Id && holds.Count == 1,
                    "holding the momentary shortcut activates the layer");
                Check((bool)Call(momentaryForm, "IsMacroLayerEligible", auraHeld) && (bool)Call(momentaryForm, "IsMacroLayerEligible", baseHeld),
                    "momentary activation keeps Base eligible while its own macros become eligible");
                Check(((string)Call(momentaryForm, "BuildRuntimeObservationText")).IndexOf("按住", StringComparison.Ordinal) >= 0,
                    "runtime observation reports the momentary hold state");

                MethodInfo startMomentary = typeof(MainForm).GetMethod("StartMacro", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new Type[] { typeof(MacroDefinition), typeof(int), typeof(TriggerSpec), typeof(bool), typeof(bool) }, null);
                startMomentary.Invoke(momentaryForm, new object[] { auraWorker, 0, null, false, false });
                OutputOwnershipManager momentaryOwnership = (OutputOwnershipManager)Field(momentaryForm, "outputOwnership");
                Check(WaitUntil(delegate
                {
                    foreach (InputSpec input in momentaryOwnership.Snapshot().Merged)
                        if (input.Kind == InputKind.Keyboard && input.VirtualKey == (int)Keys.J) return true;
                    return false;
                }, 1500), "worker-backed macro of the held layer owns output while the shortcut is down");
                Call(momentaryForm, "StartParallelHeldMapping", auraHeld);
                Check(momentaryBackend.Events.Contains("D:G:LeftShoulder"), "held mapping of the momentary layer produces output while held");

                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f5Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == "" && holds.Count == 0,
                    "releasing the momentary shortcut returns to the persistent Base-only selection");
                Check(momentaryBackend.Events.Contains("U:G:LeftShoulder"), "releasing the momentary shortcut releases its held mapping output");
                bool workerStillMerged = false;
                foreach (InputSpec input in momentaryOwnership.Snapshot().Merged)
                    if (input.Kind == InputKind.Keyboard && input.VirtualKey == (int)Keys.J) workerStillMerged = true;
                Check(!workerStillMerged, "releasing the momentary shortcut removes its worker-backed output");
                Check(WaitUntil(delegate { return !(bool)Call(momentaryForm, "IsMacroActuallyRunning", auraWorker); }, 1500),
                    "releasing the momentary shortcut stops the layer worker runtime");

                Call(momentaryForm, "SetPersistentMappingLayer", stanceLayer.Id);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id,
                    "persistent layer selection still uses click-to-switch semantics");
                Call(momentaryForm, "BeginMomentaryLayerHold", auraLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == auraLayer.Id,
                    "a momentary hold overrides the persistent layer");
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f5Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id,
                    "releasing a momentary hold returns to the persistent layer instead of Base");

                Call(momentaryForm, "BeginMomentaryLayerHold", auraLayer);
                Call(momentaryForm, "BeginMomentaryLayerHold", focusLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == focusLayer.Id && holds.Count == 2,
                    "a second held momentary layer becomes the effective layer");
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f5Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == focusLayer.Id && holds.Count == 1,
                    "releasing one held layer keeps the other still-held layer active");
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f6Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id && holds.Count == 0,
                    "releasing the last momentary hold restores the persistent layer");

                Call(momentaryForm, "BeginMomentaryLayerHold", momentaryBase);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == "",
                    "a momentary Base-layer shortcut temporarily returns to Base-only");
                Check(((string)Call(momentaryForm, "BuildDiagnosticsText")).IndexOf("MomentaryHold=True", StringComparison.Ordinal) >= 0,
                    "diagnostics report the momentary hold state");
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f8Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id,
                    "releasing the momentary Base shortcut restores the previous layer");

                Call(momentaryForm, "BeginMomentaryLayerHold", auraLayer);
                Call(momentaryForm, "RemoveMappingLayerCore", auraLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id && holds.Count == 0,
                    "deleting a layer that is currently held safely returns to the persistent layer");

                // Missed-release fallback: with no terminal release edge at all, the physical-state
                // probe must still return the layer instead of leaving it stuck.
                Call(momentaryForm, "BeginMomentaryLayerHold", fallbackLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == fallbackLayer.Id,
                    "fallback precondition: gamepad momentary layer is held");
                Check(WaitUntil(delegate
                {
                    Call(momentaryForm, "ReconcileHeldTriggerRelease");
                    return (string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id;
                }, 2000), "missed momentary release edge is recovered by the physical-state fallback");
                Check(holds.Count == 0, "fallback clears the momentary hold entry");

                // A chorded momentary shortcut must also return when its modifier is released first,
                // and an unrelated key release must never end the hold.
                Call(momentaryForm, "BeginMomentaryLayerHold", modifierLayer);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == modifierLayer.Id,
                    "chorded momentary shortcut activates its layer");
                InputEventInfo f12Up = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F12 } };
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", f12Up);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == modifierLayer.Id && holds.Count == 1,
                    "releasing an unrelated key keeps a momentary hold active");
                InputEventInfo ctrlUp = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.ControlKey } };
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", ctrlUp);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == stanceLayer.Id && holds.Count == 0,
                    "releasing the chord modifier first still returns to the persistent layer");

                // Changing the persistent selection while a layer is held must not disturb the hold,
                // and the release must land on the newly selected persistent layer.
                Call(momentaryForm, "BeginMomentaryLayerHold", modifierLayer);
                Call(momentaryForm, "SetPersistentMappingLayer", focusLayer.Id);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == modifierLayer.Id,
                    "changing the persistent layer keeps an active momentary hold in place");
                Call(momentaryForm, "EndMomentaryLayerHoldsForRelease", ctrlUp);
                Check((string)Field(momentaryForm, "activeMappingLayerId") == focusLayer.Id,
                    "releasing the hold lands on the newly selected persistent layer");

                // Converting a held layer back to click-to-switch drops the hold safely.
                Call(momentaryForm, "BeginMomentaryLayerHold", modifierLayer);
                modifierLayer.MomentarySwitch = false;
                Call(momentaryForm, "ApplyEffectiveMappingLayer");
                Check((string)Field(momentaryForm, "activeMappingLayerId") == focusLayer.Id && holds.Count == 0,
                    "converting a held layer back to click-to-switch drops the hold");
                Check(!(bool)Call(momentaryForm, "BeginMomentaryLayerHold", stanceLayer),
                    "click-to-switch layers refuse momentary holds");
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
