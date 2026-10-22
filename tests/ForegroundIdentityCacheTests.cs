using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using InputStitch;

internal static class ForegroundIdentityCacheTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static object Call(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Test hook was not found: " + name);
        return method.Invoke(target, null);
    }

    private static int Field(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new Exception("Test field was not found: " + name);
        return (int)field.GetValue(target);
    }

    private sealed class SilentBackend : IOutputBackend
    {
        public void SendDown(InputSpec input) { }
        public void SendUp(InputSpec input) { }
        public void NeutralizeGamepad() { }
    }

    [STAThread]
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-ForegroundCache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (MainForm form = new MainForm(new MacroConfig(), root, new SilentBackend()))
            {
                int before = Field(form, "foregroundIdentityCacheMisses");
                object first = Call(form, "DescribeForegroundIdentityCached");
                object second = Call(form, "DescribeForegroundIdentityCached");
                int afterHits = Field(form, "foregroundIdentityCacheMisses");
                Check(object.ReferenceEquals(first, second),
                    "two immediate foreground describes reuse one cached identity");
                Check(afterHits - before == 1, "two immediate foreground describes cause exactly one cache miss");

                Thread.Sleep(320);
                Call(form, "DescribeForegroundIdentityCached");
                int afterExpiry = Field(form, "foregroundIdentityCacheMisses");
                Check(afterExpiry - afterHits == 1, "the cached identity refreshes after the TTL expires");

                object refreshedFirst = Call(form, "DescribeForegroundIdentityCached");
                object refreshedSecond = Call(form, "DescribeForegroundIdentityCached");
                Check(object.ReferenceEquals(refreshedFirst, refreshedSecond),
                    "the cache stays consistent after a refresh");

                // Concurrent callers (hook threads and the UI thread can both evaluate conditions) must not
                // corrupt the cache or throw.
                Exception failure = null;
                Thread[] workers = new Thread[4];
                for (int i = 0; i < workers.Length; i++)
                {
                    workers[i] = new Thread(delegate()
                    {
                        try
                        {
                            for (int j = 0; j < 50; j++) Call(form, "DescribeForegroundIdentityCached");
                        }
                        catch (Exception ex) { failure = ex; }
                    });
                    workers[i].Start();
                }
                for (int i = 0; i < workers.Length; i++) workers[i].Join();
                Check(failure == null, "concurrent foreground describes do not throw");

                // A real window handle, without stealing foreground focus or emitting input.
                // The UI waits without pumping; background condition lookup must not send
                // synchronous caption messages back to this blocked thread.
                IntPtr ownHandle = form.Handle;
                MethodInfo read = typeof(MainForm).GetMethod("ReadForegroundIdentityCached", BindingFlags.Instance | BindingFlags.NonPublic);
                TargetWindowIdentity own = read.Invoke(form, new object[] { ownHandle }) as TargetWindowIdentity;
                Check(own != null && own.Title == form.Text, "UI lane can still inspect its own window conditions");
                using (ManualResetEventSlim completed = new ManualResetEventSlim(false))
                {
                    object backgroundIdentity = own;
                    Thread background = new Thread(delegate()
                    {
                        try { backgroundIdentity = read.Invoke(form, new object[] { ownHandle }); }
                        catch (Exception ex) { failure = ex; }
                        finally { completed.Set(); }
                    });
                    background.IsBackground = true;
                    background.Start();
                    bool returnedWithoutUi = completed.Wait(300);
                    Stopwatch cleanup = Stopwatch.StartNew();
                    while (!completed.IsSet && cleanup.ElapsedMilliseconds < 1000) { Application.DoEvents(); Thread.Sleep(2); }
                    background.Join(1000);
                    Check(returnedWithoutUi && failure == null && backgroundIdentity == null,
                        "background own-window conditions fail closed while UI is blocked, including with a warm own-window cache");
                }
            }
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
        Console.WriteLine("PASS foreground identity cache: " + checks + " checks; bounded TTL cache, concurrent-safe, no hooks or devices.");
        return 0;
    }
}
