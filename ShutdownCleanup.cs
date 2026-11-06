using System;

namespace InputStitch
{
    // A failed resource must not skip later stop/release/restore operations.
    // Order remains explicit at the caller; no UI, background queue or service registry.
    internal sealed class ShutdownCleanup
    {
        private readonly Action<string, Exception> report;
        internal int FailureCount { get; private set; }

        internal ShutdownCleanup(Action<string, Exception> report) { this.report = report; }

        internal void Run(string name, Action cleanup)
        {
            try { cleanup(); }
            catch (Exception ex)
            {
                FailureCount++;
                try { if (report != null) report(name, ex); } catch { }
            }
        }
    }
}
