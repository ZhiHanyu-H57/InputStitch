using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

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
        private readonly bool precise;
        private readonly ManualResetEventSlim stop = new ManualResetEventSlim(false, 0);
        private Thread thread;
        private int disposed;

        public InputPipelineScheduler(int intervalMs, Action tick, Action<Exception> onError)
            : this(intervalMs, tick, onError, true) { }

        internal InputPipelineScheduler(int intervalMs, Action tick, Action<Exception> onError, bool precise)
        {
            if (intervalMs < 1) throw new ArgumentOutOfRangeException("intervalMs");
            if (tick == null) throw new ArgumentNullException("tick");
            this.intervalMs = intervalMs;
            this.tick = tick;
            this.onError = onError;
            this.precise = precise;
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
            using (DeadlineWait timer = precise ? DeadlineWait.Create() : null)
                Loop(timer);
        }

        private void Loop(DeadlineWait timer)
        {
            WaitHandle[] waits = timer == null ? null : new WaitHandle[] { stop.WaitHandle, timer };
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
                if (next <= now)
                {
                    // A slow tick or a long stall must not build an unbounded catch-up burst.
                    next = now + intervalTicks;
                }
                // Wait for the deadline, not just one fragment of it. The old 8 ms cap returned
                // straight to tick(), so long intervals polled much too often. Round UP and use
                // an interruptible kernel wait: no busy tail or system-wide timer-resolution change.
                while (!stop.IsSet)
                {
                    long remaining = next - Stopwatch.GetTimestamp();
                    if (remaining <= 0) break;
                    if (timer != null && timer.Arm(remaining))
                    {
                        if (WaitHandle.WaitAny(waits) == 0) return;
                    }
                    else
                    {
                        long milliseconds = (remaining * 1000L + Stopwatch.Frequency - 1) / Stopwatch.Frequency;
                        if (stop.Wait((int)Math.Min(milliseconds, int.MaxValue))) return;
                    }
                }
            }
        }

        // Windows 10 1803+ supplies a precise, per-object wait without timeBeginPeriod or spinning.
        // Older Windows falls back to the interruptible framework wait. The worker owns the handle.
        private sealed class DeadlineWait : WaitHandle
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern SafeWaitHandle CreateWaitableTimerExW(IntPtr attributes, string name, uint flags, uint access);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long due, int period, IntPtr callback, IntPtr state, bool resume);

            internal static DeadlineWait Create()
            {
                SafeWaitHandle handle;
                try { handle = CreateWaitableTimerExW(IntPtr.Zero, null, 2, 0x100002); }
                catch (EntryPointNotFoundException) { return null; }
                if (handle.IsInvalid) { handle.Dispose(); return null; }
                return new DeadlineWait { SafeWaitHandle = handle };
            }

            internal bool Arm(long remaining)
            {
                long due = -Math.Max(1, (long)Math.Ceiling(remaining * 10000000.0 / Stopwatch.Frequency));
                return SetWaitableTimer(SafeWaitHandle, ref due, 0, IntPtr.Zero, IntPtr.Zero, false);
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
