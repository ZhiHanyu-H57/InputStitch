using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Forms;

// Loads either real package. Measures diagnostic-state publication only, not
// macro throughput. Isolated editor, no hooks/timers/device/output or forced GC.
internal static class RuntimeProgressProbe
{
    private static void Set(object value, string name, object data)
    { value.GetType().GetField(name).SetValue(value, data); }
    private static object Call(object value, string name, params object[] args)
    { return value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, args); }
    [STAThread]
    public static int Main(string[] args)
    {
        string package = Path.GetFullPath(args[0]), directory = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(directory); Application.EnableVisualStyles();
        Assembly assembly = Assembly.LoadFrom(package);
        assembly.GetType("InputStitch.EmbeddedDependencyLoader").GetMethod("Register").Invoke(null, null);
        Type paths = assembly.GetType("InputStitch.AppPaths");
        foreach (string field in new[] { "Root", "Config", "DeviceRegistry", "MacroPackages", "Profiles", "Backups", "Logs" })
            paths.GetField(field).SetValue(null, field == "Root" ? directory : Path.Combine(directory, field));
        object config = Activator.CreateInstance(assembly.GetType("InputStitch.MacroConfig"));
        Set(config, "GamepadDeviceType", "None"); Set(config, "UpdateMode", "Disabled");
        object macro = Activator.CreateInstance(assembly.GetType("InputStitch.MacroDefinition"));
        Set(macro, "Enabled", false);
        ((IList)config.GetType().GetField("Macros").GetValue(config)).Add(macro);
        Type formType = assembly.GetType("InputStitch.MainForm"), runType = assembly.GetType("InputStitch.MacroRunRuntime");
        Type inputType = assembly.GetType("InputStitch.InputSpec");
        Type heldType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string), inputType);
        using (Form form = (Form)Activator.CreateInstance(formType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { config, directory }, null))
        using (System.Threading.ManualResetEventSlim stop = new System.Threading.ManualResetEventSlim(false))
        using (Process process = Process.GetCurrentProcess())
        {
            object run = Activator.CreateInstance(runType); Set(run, "OwnerMacro", macro); Set(run, "Stop", stop);
            object coordinator = formType.GetField("runCoordinator", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            Call(coordinator, "TryRegister", run);
            IDictionary held = (IDictionary)Activator.CreateInstance(heldType), empty = (IDictionary)Activator.CreateInstance(heldType);
            for (int i = 0; i < 8; i++)
            {
                object input = Activator.CreateInstance(inputType); Set(input, "VirtualKey", (int)Keys.A + i);
                held.Add("key:" + i, input);
            }
            MethodInfo sync = formType.GetMethod("SyncRunHeldInputs", BindingFlags.Instance | BindingFlags.NonPublic);
            DynamicMethod method = new DynamicMethod("Publish", typeof(void), new[] { typeof(object), typeof(object), typeof(object) }, typeof(RuntimeProgressProbe), true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, formType);
            il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Castclass, runType);
            il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Castclass, heldType);
            il.Emit(OpCodes.Call, sync); il.Emit(OpCodes.Ret);
            Action<object, object, object> publish = (Action<object, object, object>)method.CreateDelegate(typeof(Action<object, object, object>));
            for (int i = 0; i < 1000; i++) { publish(form, run, held); publish(form, run, empty); }
            StringBuilder csv = new StringBuilder("repeat,updates,elapsed_ms,cpu_ms,gen0_collections\r\n");
            for (int repeat = 1; repeat <= 3; repeat++)
            {
                TimeSpan cpu = process.TotalProcessorTime; int gen0 = GC.CollectionCount(0); Stopwatch clock = Stopwatch.StartNew();
                for (int i = 0; i < 100000; i++) { publish(form, run, held); publish(form, run, empty); }
                clock.Stop(); process.Refresh();
                csv.AppendLine(repeat + ",200000," + clock.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + "," +
                    (process.TotalProcessorTime - cpu).TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + "," + (GC.CollectionCount(0) - gen0));
            }
            Call(coordinator, "Finish", run, "completed");
            File.WriteAllText(Path.Combine(directory, "samples.csv"), csv.ToString()); Console.Write(csv);
        }
        return 0;
    }
}
