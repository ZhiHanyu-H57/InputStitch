using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace InputStitch
{
    // Completion belongs to the RUN, not the reusable thread. Monitor waiting creates no
    // per-run kernel event and works for configuration changes without joining an idle worker.
    internal sealed class MacroWorkItem
    {
        private readonly object sync = new object();
        internal readonly Action Body;
        private bool finished;
        private bool queued;
        internal MacroWorkItem(Action body) { Body = body; }
        internal bool Queue() { lock (sync) { if (queued || finished) return false; queued = true; return true; } }
        internal void Complete() { lock (sync) { finished = true; Monitor.PulseAll(sync); } }
        internal bool Wait(int milliseconds)
        {
            Stopwatch clock = Stopwatch.StartNew();
            lock (sync)
            {
                while (!finished)
                {
                    int remaining = Math.Max(0, milliseconds - (int)clock.ElapsedMilliseconds);
                    if (remaining == 0) return false;
                    Monitor.Wait(sync, remaining);
                }
                return true;
            }
        }
    }

    // One dedicated worker per concurrently running macro, with at most four sleeping spares.
    // Running jobs never wait behind unrelated long-running macros or the shared CLR thread pool.
    // Only the execution thread is reused: cancellation, snapshots and output ownership stay per run.
    internal sealed class MacroWorkerPool : IDisposable
    {
        private readonly object sync = new object();
        private readonly Stack<Worker> idle = new Stack<Worker>();
        private readonly HashSet<Worker> workers = new HashSet<Worker>();
        private readonly Action<Exception> onError;
        private bool disposed;
        private int created;
        internal int CreatedThreadCount { get { lock (sync) return created; } }

        internal MacroWorkerPool(Action<Exception> onError) { this.onError = onError; }

        internal void Start(MacroWorkItem item)
        {
            lock (sync)
            {
                if (disposed) throw new ObjectDisposedException("MacroWorkerPool");
                if (!item.Queue()) throw new InvalidOperationException("A macro work item can only be queued once.");
                if (idle.Count != 0)
                {
                    Worker worker = idle.Pop();
                    worker.Pending = item;
                    worker.Signal.Set();
                }
                else
                {
                    Worker worker = new Worker(this, item, ++created);
                    workers.Add(worker);
                    try { worker.Thread.Start(); }
                    catch { workers.Remove(worker); worker.Signal.Dispose(); throw; }
                }
            }
        }

        private sealed class Worker
        {
            internal readonly Thread Thread;
            internal readonly AutoResetEvent Signal = new AutoResetEvent(false);
            internal MacroWorkItem Pending;
            private readonly MacroWorkerPool owner;
            internal Worker(MacroWorkerPool owner, MacroWorkItem item, int number)
            {
                this.owner = owner; Pending = item;
                Thread = new Thread(Loop) { IsBackground = true, Name = "InputStitch Macro Worker " + number };
            }
            private void Loop()
            {
                try
                {
                    while (true)
                    {
                        MacroWorkItem item;
                        lock (owner.sync) { item = Pending; Pending = null; }
                        if (item != null)
                        {
                            try { item.Body(); }
                            catch (Exception ex) { if (owner.onError != null) owner.onError(ex); }
                            finally { item.Complete(); }
                            item = null;
                        }
                        lock (owner.sync)
                        {
                            if (owner.disposed || owner.idle.Count >= 4) return;
                            owner.idle.Push(this);
                        }
                        Signal.WaitOne();
                    }
                }
                finally
                {
                    lock (owner.sync) owner.workers.Remove(this);
                    Signal.Dispose();
                }
            }
        }

        public void Dispose()
        {
            List<Thread> threads = new List<Thread>();
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                foreach (Worker worker in workers) { threads.Add(worker.Thread); worker.Signal.Set(); }
                idle.Clear();
            }
            Stopwatch clock = Stopwatch.StartNew();
            foreach (Thread thread in threads)
            {
                int remaining = Math.Max(0, 1200 - (int)clock.ElapsedMilliseconds);
                if (thread != Thread.CurrentThread && remaining > 0) thread.Join(remaining);
            }
        }
    }
}
