using System;
using System.Diagnostics;
using System.Threading;

namespace InputStitch
{
    // Dedicated cadence loop for the input pipeline (architecture-review finding F1).
    //
    // XInput polling and router mirroring used to run on a Windows.Forms.Timer, which means they shared the
    // UI thread: menus, modal dialogs and large list refreshes could delay or coalesce input ticks. This
    // scheduler owns a background thread with deadline-based pacing. The tick body therefore must not touch
    // UI controls directly; callers keep marshalling UI-affine work themselves, exactly like the existing
    // low-level keyboard/mouse hook lanes.
    internal sealed class InputPipelineScheduler : IDisposable
    {
        private readonly int intervalMs;
        private readonly Action tick;
        private readonly Action<Exception> onError;
        private readonly ManualResetEventSlim stop = new ManualResetEventSlim(false);
        private Thread thread;
        private int disposed;

        public InputPipelineScheduler(int intervalMs, Action tick, Action<Exception> onError)
        {
            if (intervalMs < 1) throw new ArgumentOutOfRangeException("intervalMs");
            if (tick == null) throw new ArgumentNullException("tick");
            this.intervalMs = intervalMs;
            this.tick = tick;
            this.onError = onError;
        }

        public bool Running
        {
            get { return thread != null && thread.IsAlive; }
        }

        public void Start()
        {
            if (disposed != 0) throw new ObjectDisposedException("InputPipelineScheduler");
            if (thread != null) return;
            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.Name = "InputStitch-InputPipeline";
            thread.Start();
        }

        private void Loop()
        {
            long intervalTicks = intervalMs * Stopwatch.Frequency / 1000L;
            long next = Stopwatch.GetTimestamp();
            while (!stop.IsSet)
            {
                try { tick(); }
                catch (Exception ex)
                {
                    if (onError != null)
                    {
                        try { onError(ex); } catch { }
                    }
                }

                next += intervalTicks;
                long now = Stopwatch.GetTimestamp();
                long remainingTicks = next - now;
                if (remainingTicks <= 0)
                {
                    // A slow tick or a long stall must not build an unbounded catch-up burst.
                    next = now;
                    continue;
                }
                long remainingMs = remainingTicks * 1000L / Stopwatch.Frequency;
                if (remainingMs > 1) stop.Wait((int)Math.Min(remainingMs - 1, 8));
                else Thread.SpinWait(200);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            stop.Set();
            Thread current = thread;
            if (current != null && current.IsAlive)
            {
                try { current.Join(500); } catch { }
            }
            if (current == null || !current.IsAlive) stop.Dispose();
        }
    }
}
