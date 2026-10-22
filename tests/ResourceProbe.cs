using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

// Minimal managed host for a REAL packaged assembly; PowerShell UI callbacks would distort CPU.
internal static class ResourceProbe
{
    private sealed class Sample
    {
        internal int Phase, Handles, Threads, Devices;
        internal double Seconds, Cpu;
        internal long Bytes;
    }
    private static FieldInfo Field(object value, string name) { return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
    private static void Set(object value, string name, object data) { Field(value, name).SetValue(value, data); }
    private static string Number(double value) { return value.ToString("F3", CultureInfo.InvariantCulture); }
    private static string Quote(string value) { return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }

    [STAThread]
    public static int Main(string[] args)
    {
        string package = Path.GetFullPath(args[0]), directory = Path.GetFullPath(args[1]);
        int seconds = int.Parse(args[2], CultureInfo.InvariantCulture);
        bool afterAdvancedTrigger = args.Length > 3 && bool.Parse(args[3]);
        Directory.CreateDirectory(directory);
        Application.EnableVisualStyles();
        Assembly assembly = Assembly.LoadFrom(package);
        assembly.GetType("InputStitch.EmbeddedDependencyLoader").GetMethod("Register").Invoke(null, null);
        string version = (string)assembly.GetType("InputStitch.ReleaseInfo").GetField("Version").GetRawConstantValue();
        Type paths = assembly.GetType("InputStitch.AppPaths");
        foreach (string field in new[] { "Root", "Config", "DeviceRegistry", "MacroPackages", "Profiles", "Backups", "Logs" })
        {
            string name = field == "Config" ? "config.xml" : field == "DeviceRegistry" ? "devices.xml" : field == "MacroPackages" ? "macro-packages" : field.ToLowerInvariant();
            paths.GetField(field).SetValue(null, field == "Root" ? directory : Path.Combine(directory, name));
        }
        object config = Activator.CreateInstance(assembly.GetType("InputStitch.MacroConfig"));
        Set(config, "GamepadDeviceType", "None"); Set(config, "UpdateMode", "Disabled");
        Set(config, "HasSeenWelcome", true); Set(config, "LastShownReleaseSummaryVersion", version);
        object macro = Activator.CreateInstance(assembly.GetType("InputStitch.MacroDefinition"));
        Set(macro, "Name", "Disabled resource probe"); Set(macro, "Enabled", false);
        ((IList)Field(config, "Macros").GetValue(config)).Add(macro);
        using (FileStream stream = File.Create(Path.Combine(directory, "config.xml"))) new XmlSerializer(config.GetType()).Serialize(stream, config);
        List<Sample> samples = new List<Sample>();
        int idleTickRequestsQueued = 0;
        using (Form form = (Form)Activator.CreateInstance(assembly.GetType("InputStitch.MainForm")))
        using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
        using (Process process = Process.GetCurrentProcess())
        {
            form.StartPosition = FormStartPosition.Manual; form.Location = new System.Drawing.Point(-20000, -20000); form.ShowInTaskbar = false;
            if (afterAdvancedTrigger)
            {
                object engine = Field(form, "activatorEngine").GetValue(form);
                object activator = Field(macro, "Activator").GetValue(macro);
                Set(activator, "Mode", "DoublePress");
                object input = Activator.CreateInstance(assembly.GetType("InputStitch.InputSpec"));
                Set(input, "VirtualKey", (int)Keys.F24);
                object edge = Activator.CreateInstance(assembly.GetType("InputStitch.InputEventInfo"));
                Set(edge, "Input", input);
                object trigger = Field(macro, "Trigger").GetValue(macro);
                Set(trigger, "VirtualKey", (int)Keys.F24);
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                engine.GetType().GetMethod("OnDown", flags).Invoke(engine, new object[] { macro, edge, trigger, 0L, true });
                engine.GetType().GetMethod("OnUp", flags).Invoke(engine, new object[] { edge, 1L, null });
                IntPtr handle = form.Handle;
                MethodInfo request = form.GetType().GetMethod("RequestActivatorTick", flags);
                for (int i = 0; i < 1000; i++)
                {
                    request.Invoke(form, null);
                    if ((int)Field(form, "activatorTickQueued").GetValue(form) != 0) idleTickRequestsQueued++;
                    Application.DoEvents();
                }
            }
            int phase = -1;
            double cpuStart = 0;
            Stopwatch clock = Stopwatch.StartNew();
            timer.Interval = 1000;
            Exception failure = null;
            timer.Tick += delegate
            {
                try
                {
                    process.Refresh();
                    if (phase == -1)
                    {
                        if (clock.Elapsed.TotalSeconds < 5) return;
                        phase = 0; clock.Restart(); cpuStart = process.TotalProcessorTime.TotalMilliseconds;
                    }
                    object input = Field(form, "gamepadInput").GetValue(form);
                    int count = ((int[])input.GetType().GetMethod("ConnectedDeviceIndices").Invoke(input, null)).Length;
                    samples.Add(new Sample { Phase = phase, Seconds = clock.Elapsed.TotalSeconds, Cpu = process.TotalProcessorTime.TotalMilliseconds - cpuStart,
                        Bytes = process.PrivateMemorySize64, Handles = process.HandleCount, Threads = process.Threads.Count, Devices = count });
                    if (clock.Elapsed.TotalSeconds < seconds) return;
                    if (phase == 1) { timer.Stop(); form.Close(); return; }
                    object live = Field(form, "config").GetValue(form), crosshair = Field(live, "Crosshair").GetValue(live);
                    Set(crosshair, "Enabled", true); Set(crosshair, "Visible", true);
                    form.GetType().GetMethod("ApplyCrosshair", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);
                    phase = 1; clock.Restart(); process.Refresh(); cpuStart = process.TotalProcessorTime.TotalMilliseconds;
                }
                catch (Exception ex) { failure = ex; timer.Stop(); form.Close(); }
            };
            timer.Start(); Application.Run(form);
            if (failure != null) throw failure;
        }
        StringBuilder csv = new StringBuilder("phase,seconds,cpu_ms,private_bytes,handles,threads,devices\r\n");
        foreach (Sample row in samples) csv.AppendLine(row.Phase + "," + Number(row.Seconds) + "," + Number(row.Cpu) + "," + row.Bytes + "," + row.Handles + "," + row.Threads + "," + row.Devices);
        File.WriteAllText(Path.Combine(directory, "samples.csv"), csv.ToString());
        List<string> summaries = new List<string>();
        foreach (IGrouping<int, Sample> group in samples.GroupBy(x => x.Phase))
        {
            Sample first = group.First(), last = group.Last();
            double duration = last.Seconds - first.Seconds, cpu = last.Cpu - first.Cpu;
            summaries.Add("{\"phase\":" + Quote(group.Key == 0 ? "idle-crosshair-off" : "idle-crosshair-on") + ",\"measuredSeconds\":" + Number(duration) + ",\"cpuMilliseconds\":" + Number(cpu) +
                ",\"oneCorePercent\":" + Number(cpu / (duration * 10)) + ",\"finalPrivateBytes\":" + last.Bytes + ",\"peakPrivateBytes\":" + group.Max(x => x.Bytes) +
                ",\"firstHandles\":" + first.Handles + ",\"finalHandles\":" + last.Handles + ",\"peakHandles\":" + group.Max(x => x.Handles) + ",\"firstThreads\":" + first.Threads + ",\"finalThreads\":" + last.Threads + ",\"devices\":" + last.Devices + "}");
        }
        string report = "{\"version\":" + Quote(version) + ",\"package\":" + Quote(package) + ",\"afterAdvancedTrigger\":" + afterAdvancedTrigger.ToString().ToLowerInvariant() +
            ",\"idleTickRequestsTested\":" + (afterAdvancedTrigger ? 1000 : 0) + ",\"idleTickRequestsQueued\":" + idleTickRequestsQueued + ",\"forcedGc\":false,\"logicalProcessors\":" + Environment.ProcessorCount +
            ",\"note\":\"Isolated minimal managed host; production startup, SDL3 and hooks; disabled macros; CPU is one core.\",\"phases\":[" + string.Join(",", summaries) + "]}";
        File.WriteAllText(Path.Combine(directory, "report.json"), report); Console.WriteLine(report);
        return 0;
    }
}
