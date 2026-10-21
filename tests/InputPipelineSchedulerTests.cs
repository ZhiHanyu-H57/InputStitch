using System;
using System.Diagnostics;
using System.Threading;
using InputStitch;

internal static class InputPipelineSchedulerTests
{
    private static int checks;
    private static int ticks;
    private static int faultyTicks;
    private static int reportedErrors;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
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

    public static int Main()
    {
        // Cadence: the scheduler must tick repeatedly, stay far away from a busy loop, and stop on dispose.
        InputPipelineScheduler scheduler = new InputPipelineScheduler(5, delegate { Interlocked.Increment(ref ticks); }, null);
        try
        {
            Check(!scheduler.Running, "a freshly created scheduler is not running yet");
            scheduler.Start();
            Check(scheduler.Running, "Start() launches the pipeline thread");
            Check(WaitUntil(delegate { return Volatile.Read(ref ticks) >= 5; }, 1000), "the scheduler ticks repeatedly");
            int observed = Volatile.Read(ref ticks);
            Check(observed >= 5 && observed <= 400, "cadence stays between a slow ticker and a busy loop (" + observed.ToString() + " ticks)");
            scheduler.Dispose();
            Check(!scheduler.Running, "Dispose() stops the pipeline thread");
            int afterStop = Volatile.Read(ref ticks);
            Thread.Sleep(80);
            Check(Volatile.Read(ref ticks) <= afterStop + 1, "no ticks continue after Dispose()");
        }
        finally
        {
            scheduler.Dispose();
        }

        // Error containment: a throwing tick body must not kill the loop.
        InputPipelineScheduler faulty = new InputPipelineScheduler(5, delegate
        {
            Interlocked.Increment(ref faultyTicks);
            throw new InvalidOperationException("probe");
        }, delegate { Interlocked.Increment(ref reportedErrors); });
        try
        {
            faulty.Start();
            Check(WaitUntil(delegate { return Volatile.Read(ref reportedErrors) >= 3; }, 1000),
                "a throwing tick body is reported through the error handler");
            Check(Volatile.Read(ref faultyTicks) >= 3, "the loop keeps ticking after a tick body throws");
        }
        finally
        {
            faulty.Dispose();
        }

        // Dispose is idempotent and a disposed scheduler refuses to restart.
        faulty.Dispose();
        bool refused = false;
        try { faulty.Start(); }
        catch (ObjectDisposedException) { refused = true; }
        Check(refused, "a disposed scheduler refuses to start again");

        // A long interval previously returned to tick() after only an 8 ms wait. Check an actual
        // deadline, not a broad count taken as soon as the fifth tick happens.
        long first = 0, second = 0;
        int paced = 0;
        using (InputPipelineScheduler slow = new InputPipelineScheduler(100, delegate
        {
            int number = Interlocked.Increment(ref paced);
            if (number == 1) Interlocked.Exchange(ref first, Stopwatch.GetTimestamp());
            if (number == 2) Interlocked.Exchange(ref second, Stopwatch.GetTimestamp());
        }, null))
        {
            slow.Start();
            Check(WaitUntil(delegate { return Interlocked.Read(ref second) != 0; }, 2000), "a long-interval scheduler reaches its second tick");
            double gap = (Interlocked.Read(ref second) - Interlocked.Read(ref first)) * 1000.0 / Stopwatch.Frequency;
            Check(gap >= 95, "a 100 ms deadline is not shortened to one 8 ms wait (" + gap.ToString("F1") + " ms)");
        }
        using (InputPipelineScheduler waiting = new InputPipelineScheduler(10000, delegate { }, null))
        {
            waiting.Start();
            Thread.Sleep(30);
            Stopwatch shutdown = Stopwatch.StartNew();
            waiting.Dispose();
            Check(!waiting.Running && shutdown.ElapsedMilliseconds < 500, "a long wait remains immediately cancellable");
        }
        int fallbackTicks = 0;
        using (InputPipelineScheduler fallback = new InputPipelineScheduler(100, delegate { Interlocked.Increment(ref fallbackTicks); }, null, false))
        {
            fallback.Start();
            Thread.Sleep(450);
            Check(Volatile.Read(ref fallbackTicks) >= 2 && Volatile.Read(ref fallbackTicks) <= 5, "older-Windows fallback respects deadlines without spinning");
        }

        Console.WriteLine("PASS input pipeline scheduler: " + checks + " checks; dedicated cadence thread, error containment, clean shutdown, no hooks or devices.");
        return 0;
    }
}
