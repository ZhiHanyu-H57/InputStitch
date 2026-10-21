using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using InputStitch;

// Isolated, repeatable microbenchmarks. No hooks, configuration, output or forced GC.
// Compare the same runner and machine before/after; CPU is a fraction of ONE core.
internal static class PerformanceProbe
{
    private sealed class Provider : IGamepadInputProvider
    {
        private readonly int[] indices;
        public long Reads;
        public long Polls;
        public Provider(params int[] indices) { this.indices = indices; }
        public string ProviderName { get { return "probe"; } }
        public long TopologyVersion { get { return 1; } }
        public Action<InputEventInfo> InputDown { get; set; }
        public Action<InputEventInfo> InputUp { get; set; }
        public void Poll() { Polls++; }
        public int[] ConnectedDeviceIndices() { Reads++; return (int[])indices.Clone(); }
        public bool TryGetState(int index, out XInputPadState state) { state = new XInputPadState(); return false; }
        public bool IsTriggerSatisfied(TriggerSpec trigger) { return false; }
        public void Dispose() { }
    }

    private static void Measure(string name, Action action, Func<long> operations, Func<long> reads)
    {
        using (Process process = Process.GetCurrentProcess())
        {
            TimeSpan cpu = process.TotalProcessorTime;
            int collections = GC.CollectionCount(0);
            Stopwatch clock = Stopwatch.StartNew();
            action();
            clock.Stop();
            process.Refresh();
            double cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
            Console.WriteLine(string.Join(",", new[] { name, clock.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture),
                operations().ToString(), cpuMs.ToString("F2", CultureInfo.InvariantCulture),
                (cpuMs / clock.Elapsed.TotalMilliseconds * 100).ToString("F3", CultureInfo.InvariantCulture),
                (GC.CollectionCount(0) - collections).ToString(), reads().ToString(), process.PrivateMemorySize64.ToString() }));
        }
    }

    public static int Main(string[] args)
    {
        int seconds = args.Length == 0 ? 6 : int.Parse(args[0], CultureInfo.InvariantCulture);
        Console.WriteLine("scenario,elapsed_ms,operations,cpu_ms,one_core_percent,gen0_collections,topology_snapshots,private_bytes");
        for (int repeat = 1; repeat <= 3; repeat++)
        {
            foreach (int interval in new[] { 10, 100 })
            {
                long ticks = 0;
                Measure("scheduler-" + interval + "ms-" + repeat, delegate
                {
                    using (InputPipelineScheduler scheduler = new InputPipelineScheduler(interval, delegate { Interlocked.Increment(ref ticks); }, null))
                    { scheduler.Start(); Thread.Sleep(seconds * 1000); }
                }, delegate { return Interlocked.Read(ref ticks); }, delegate { return 0; });
            }
            Provider first = new Provider(0, 1), second = new Provider(4, 5);
            using (CompositeInputProvider composite = new CompositeInputProvider(first, second))
            {
                composite.Poll(); // initialize/JIT outside measurement
                first.Reads = second.Reads = 0;
                Measure("steady-topology-" + repeat, delegate { for (int i = 0; i < 1000000; i++) composite.Poll(); },
                    delegate { return 1000000; }, delegate { return first.Reads + second.Reads; });
            }
        }
        return 0;
    }
}
