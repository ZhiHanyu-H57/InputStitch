using System;
using System.IO;
using System.Reflection;
using InputStitch;

internal static class TickCadenceTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Test hook was not found: " + name);
        return method.Invoke(target, args);
    }

    private static object Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Test field was not found: " + name);
        return field.GetValue(target);
    }

    private sealed class SilentBackend : IOutputBackend
    {
        public void SendDown(InputSpec input) { }
        public void SendUp(InputSpec input) { }
        public void NeutralizeGamepad() { }
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-TickCadence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (MainForm form = new MainForm(new MacroConfig(), root, new SilentBackend()))
            {
                object stats = Field(form, "gamepadTickStats");
                Check(stats != null, "gamepad tick statistics exist");

                long before = (long)Field(stats, "Ticks");
                Call(form, "TickGamepadInput");
                Call(form, "TickGamepadInput");
                long after = (long)Field(stats, "Ticks");
                Check(after - before == 2, "every input tick is counted, even when no controller backend is attached");
                Check((long)Field(stats, "MaxGapTicks") > 0, "the gap between consecutive ticks is measured");

                string description = (string)Call(stats, "Describe", "gamepad");
                Check(description.IndexOf("ticks=", StringComparison.Ordinal) >= 0 &&
                    description.IndexOf("stalls=", StringComparison.Ordinal) >= 0 &&
                    description.IndexOf("maxGapUs=", StringComparison.Ordinal) >= 0 &&
                    description.IndexOf("maxDurationUs=", StringComparison.Ordinal) >= 0 &&
                    description.IndexOf("avgDurationUs=", StringComparison.Ordinal) >= 0,
                    "tick statistics describe count, stalls, gap and duration");

                string diagnostics = (string)Call(form, "BuildDiagnosticsText");
                Check(diagnostics.IndexOf("GamepadInputTick:", StringComparison.Ordinal) >= 0,
                    "diagnostics expose the input tick statistics");
                Check(diagnostics.IndexOf("ForegroundTick:", StringComparison.Ordinal) >= 0,
                    "diagnostics expose the foreground tick statistics");

                string observation = (string)Call(form, "BuildRuntimeObservationText");
                Check(observation.IndexOf("节拍", StringComparison.Ordinal) >= 0 ||
                    observation.IndexOf("cadence", StringComparison.OrdinalIgnoreCase) >= 0,
                    "runtime observation exposes the input cadence summary");
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
        Console.WriteLine("PASS tick cadence: " + checks + " checks; input/foreground cadence instrumentation, no hooks or devices.");
        return 0;
    }
}
