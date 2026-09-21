using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using InputStitch;

internal static class LayerContextTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Test hook was not found: " + name);
        return method.Invoke(target, args);
    }

    private static string Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Test field was not found: " + name);
        return (string)field.GetValue(target);
    }

    private static object StaticCall(string name, params object[] args)
    {
        MethodInfo method = typeof(MainForm).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Static test hook was not found: " + name);
        return method.Invoke(null, args);
    }

    private sealed class SilentBackend : IOutputBackend
    {
        public void SendDown(InputSpec input) { }
        public void SendUp(InputSpec input) { }
        public void NeutralizeGamepad() { }
    }

    private static MacroConditionConfig ProcessContext(string processName)
    {
        return new MacroConditionConfig { ForegroundProcessName = processName };
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-LayerContext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            MappingLayerDefinition contextA = new MappingLayerDefinition
            {
                Id = "layer-context-a",
                Name = "Context A",
                IsBase = false,
                ContextCondition = ProcessContext("gameA")
            };
            MappingLayerDefinition contextB = new MappingLayerDefinition
            {
                Id = "layer-context-b",
                Name = "Context B",
                IsBase = false,
                ContextCondition = ProcessContext("gameB")
            };
            MappingLayerDefinition plain = new MappingLayerDefinition
            {
                Id = "layer-plain",
                Name = "Plain",
                IsBase = false,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)System.Windows.Forms.Keys.F7 }
            };
            MappingLayerDefinition momentary = new MappingLayerDefinition
            {
                Id = "layer-momentary",
                Name = "Momentary",
                IsBase = false,
                MomentarySwitch = true,
                SwitchTrigger = new TriggerSpec { Kind = InputKind.Keyboard, VirtualKey = (int)System.Windows.Forms.Keys.F6 }
            };

            MacroConfig config = new MacroConfig();
            config.MappingLayers.Clear();
            config.MappingLayers.Add(new MappingLayerDefinition { Id = MappingLayerIds.Base, Name = "基础层", IsBase = true });
            config.MappingLayers.Add(contextA);
            config.MappingLayers.Add(contextB);
            config.MappingLayers.Add(plain);
            config.MappingLayers.Add(momentary);

            // XML round trip: context conditions survive, empty conditions are normalized away.
            MappingLayerDefinition emptyContextLayer = new MappingLayerDefinition
            {
                Id = "layer-empty-context",
                Name = "Empty",
                IsBase = false,
                ContextCondition = new MacroConditionConfig { ForegroundProcessName = "   " }
            };
            config.MappingLayers.Add(emptyContextLayer);
            string roundTripPath = Path.Combine(root, "context-roundtrip.xml");
            using (FileStream stream = File.Create(roundTripPath))
                new XmlSerializer(typeof(MacroConfig)).Serialize(stream, config);
            MacroConfig reloaded;
            using (FileStream stream = File.OpenRead(roundTripPath))
                reloaded = (MacroConfig)new XmlSerializer(typeof(MacroConfig)).Deserialize(stream);
            StaticCall("NormalizeMappingLayers", reloaded);
            MappingLayerDefinition reloadedA = null;
            MappingLayerDefinition reloadedEmpty = null;
            foreach (MappingLayerDefinition layer in reloaded.MappingLayers)
            {
                if (layer == null) continue;
                if (layer.Id == contextA.Id) reloadedA = layer;
                if (layer.Id == emptyContextLayer.Id) reloadedEmpty = layer;
            }
            Check(reloadedA != null && reloadedA.ContextCondition != null &&
                reloadedA.ContextCondition.ForegroundProcessName == "gameA",
                "a layer context condition survives an XML save/reload round trip");
            Check(reloadedEmpty != null && reloadedEmpty.ContextCondition == null,
                "an all-blank context condition is normalized away");
            config.MappingLayers.Remove(emptyContextLayer);

            HashSet<string> activeContexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            FakeBackend backend = new FakeBackend();
            using (MainForm form = new MainForm(config, root, backend))
            {
                // Deterministic test seam: the evaluator answers from the process-name field of the condition.
                FieldInfo evaluatorField = typeof(MainForm).GetField("layerContextEvaluator", BindingFlags.Instance | BindingFlags.NonPublic);
                Check(evaluatorField != null, "layer context evaluator seam exists");
                evaluatorField.SetValue(form, new Func<MacroConditionConfig, bool>(delegate(MacroConditionConfig condition)
                {
                    return condition != null && activeContexts.Contains(condition.ForegroundProcessName ?? "");
                }));

                Check(Field(form, "activeMappingLayerId") == "", "layers without a matching context keep the runtime at Base-only");

                activeContexts.Add("gameA");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == contextA.Id,
                    "a matching context condition activates its layer automatically");
                Check((bool)Call(form, "IsActiveLayerContextDriven"), "the active layer is reported as context driven");

                Call(form, "SetPersistentMappingLayer", plain.Id);
                Check(Field(form, "activeMappingLayerId") == contextA.Id,
                    "a matching context layer overrides the persistent selection");

                activeContexts.Add("gameB");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == contextA.Id,
                    "the first matching context layer in list order wins");

                activeContexts.Remove("gameA");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == contextB.Id,
                    "the remaining matching context layer takes over");

                activeContexts.Remove("gameB");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == plain.Id,
                    "when every context stops matching the runtime returns to the persistent layer");

                // Momentary holds outrank context layers, and the context resumes after release.
                activeContexts.Add("gameA");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == contextA.Id, "context precondition: layer A is active");
                Call(form, "BeginMomentaryLayerHold", momentary);
                Check(Field(form, "activeMappingLayerId") == momentary.Id,
                    "a momentary hold overrides a context layer");
                InputEventInfo f6Up = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)System.Windows.Forms.Keys.F6 } };
                Call(form, "EndMomentaryLayerHoldsForRelease", f6Up);
                Check(Field(form, "activeMappingLayerId") == contextA.Id,
                    "releasing the momentary hold resumes the context layer");

                // Base can be a context target: matching it temporarily returns to Base-only.
                MappingLayerDefinition baseLayer = config.MappingLayers[0];
                baseLayer.ContextCondition = ProcessContext("baseOnly");
                activeContexts.Add("baseOnly");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == "",
                    "a Base context temporarily returns the runtime to Base-only");
                baseLayer.ContextCondition = null;
                activeContexts.Remove("baseOnly");
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == contextA.Id,
                    "clearing the Base context restores the remaining context layer");

                // Clearing the active layer's condition while it drives the runtime returns to persistent.
                contextA.ContextCondition = null;
                Call(form, "EvaluateContextLayers");
                Check(Field(form, "activeMappingLayerId") == plain.Id,
                    "clearing the matching condition returns to the persistent layer");

                string displayName = (string)Call(form, "LayerDisplayName", contextB);
                Check(displayName.IndexOf("上下文", StringComparison.Ordinal) >= 0 || displayName.IndexOf("context", StringComparison.OrdinalIgnoreCase) >= 0,
                    "layer display names mark context layers");

                activeContexts.Add("gameB");
                Call(form, "EvaluateContextLayers");
                string diagnostics = (string)Call(form, "BuildDiagnosticsText");
                Check(diagnostics.IndexOf("ContextLayer=" + contextB.Id, StringComparison.Ordinal) >= 0,
                    "diagnostics report the active context layer");
                Check(diagnostics.IndexOf("Context=process=gameB", StringComparison.Ordinal) >= 0,
                    "diagnostics describe each layer's context condition");
                string observation = (string)Call(form, "BuildRuntimeObservationText");
                Check(observation.IndexOf("上下文自动", StringComparison.Ordinal) >= 0 || observation.IndexOf("context)", StringComparison.OrdinalIgnoreCase) >= 0,
                    "runtime observation marks a context-driven layer");
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
        Console.WriteLine("PASS layer context: " + checks + " checks; deterministic context precedence, XML round trip, diagnostics, no hooks or devices.");
        return 0;
    }

    private sealed class FakeBackend : IOutputBackend
    {
        public readonly List<string> Events = new List<string>();
        public void SendDown(InputSpec input) { Events.Add("D"); }
        public void SendUp(InputSpec input) { Events.Add("U"); }
        public void NeutralizeGamepad() { Events.Add("N"); }
    }
}
