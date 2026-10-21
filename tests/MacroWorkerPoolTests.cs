using System;
using System.Diagnostics;
using System.Threading;
using InputStitch;

internal static class MacroWorkerPoolTests
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    public static int Main()
    {
        int executions = 0, errors = 0;
        using (MacroWorkerPool pool = new MacroWorkerPool(delegate { Interlocked.Increment(ref errors); }))
        using (ManualResetEventSlim release = new ManualResetEventSlim(false))
        using (ManualResetEventSlim started = new ManualResetEventSlim(false))
        {
            MacroWorkItem held = new MacroWorkItem(delegate { started.Set(); release.Wait(); });
            pool.Start(held);
            Check(started.Wait(2000), "a long-running macro starts immediately");
            Check(!held.Wait(0), "completion belongs to the run, not an alive reusable thread");
            for (int i = 0; i < 1000; i++)
            {
                MacroWorkItem finite = new MacroWorkItem(delegate { Interlocked.Increment(ref executions); });
                pool.Start(finite);
                Check(finite.Wait(2000), "finite runs complete beside an unrelated long-running macro");
            }
            Check(executions == 1000, "every finite run executes exactly once");
            Check(pool.CreatedThreadCount <= 6, "rapid restarts reuse bounded dedicated workers (" + pool.CreatedThreadCount + ")");
            bool duplicate = false;
            try { pool.Start(held); } catch (InvalidOperationException) { duplicate = true; }
            Check(duplicate, "a run cannot be queued twice");
            release.Set();
            Check(held.Wait(2000), "a blocked run completes independently");
            MacroWorkItem faulty = new MacroWorkItem(delegate { throw new InvalidOperationException("probe"); });
            pool.Start(faulty);
            Check(faulty.Wait(2000) && Volatile.Read(ref errors) == 1, "errors are reported and completion is signalled");
            MacroWorkItem next = new MacroWorkItem(delegate { Interlocked.Increment(ref executions); });
            pool.Start(next);
            Check(next.Wait(2000) && executions == 1001, "a fault does not strand later runs");
            Stopwatch stop = Stopwatch.StartNew();
            pool.Dispose();
            Check(stop.ElapsedMilliseconds < 1200, "idle workers shut down without waiting for another macro");
            bool refused = false;
            try { pool.Start(new MacroWorkItem(delegate { })); } catch (ObjectDisposedException) { refused = true; }
            Check(refused, "a disposed pool refuses new runs");
        }
        Console.WriteLine("PASS macro worker reuse: " + checks + " checks; rapid restarts, concurrent long/finite runs, completion, faults, clean shutdown; no output or forced GC.");
        return 0;
    }
}
