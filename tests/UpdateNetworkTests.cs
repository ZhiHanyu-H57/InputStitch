using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using InputStitch;

internal static class UpdateNetworkTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static void ExpectFailure(Action action, string message)
    {
        try { action(); }
        catch { checks++; return; }
        throw new Exception("Expected failure: " + message);
    }

    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-update-network-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Check(UpdateManager.NetworkTimeoutMs == 8000, "update requests have an explicit 8-second no-response timeout");
            Check(UpdateManager.NetworkMaxAttempts == 3, "network policy is initial attempt plus two retries");
            Check(UpdateManager.IsTransientNetworkException(new WebException("timeout", WebExceptionStatus.Timeout)), "timeout is transient");
            Check(UpdateManager.IsTransientNetworkException(new WebException("dns", WebExceptionStatus.NameResolutionFailure)), "DNS failure is transient");
            Check(UpdateManager.IsTransientNetworkException(new WebException("receive", WebExceptionStatus.ReceiveFailure)), "mid-transfer receive failure is transient");
            Check(!UpdateManager.IsTransientNetworkException(new WebException("trust", WebExceptionStatus.TrustFailure)), "TLS trust failure is not blindly retried");
            Check(!UpdateManager.IsTransientNetworkException(new InvalidDataException("bad manifest")), "validation errors are not network retries");

            int attempts = 0;
            string result = UpdateManager.ExecuteWithRetryAsync<string>(delegate
            {
                attempts++;
                if (attempts < 3) throw new WebException("offline", WebExceptionStatus.ConnectFailure);
                return Task.FromResult("ok");
            }).GetAwaiter().GetResult();
            Check(result == "ok" && attempts == 3, "transient failures retry twice and can recover on the third attempt");

            attempts = 0;
            ExpectFailure(delegate
            {
                UpdateManager.ExecuteWithRetryAsync<string>(delegate
                {
                    attempts++;
                    throw new WebException("certificate", WebExceptionStatus.TrustFailure);
                }).GetAwaiter().GetResult();
            }, "non-transient failure");
            Check(attempts == 1, "non-transient failures do not retry");

            string destination = Path.Combine(root, "retry.exe");
            attempts = 0;
            UpdateManager.DownloadFileWithRetryAsync(new Uri("https://github.com/ZhiHanyu-H57/InputStitch/releases/download/test/file.exe"), destination,
                delegate(Uri uri, string path)
                {
                    attempts++;
                    Check(!File.Exists(path), "each retry starts after deleting the previous partial file");
                    File.WriteAllText(path, attempts < 3 ? "partial-" + attempts.ToString() : "complete");
                    if (attempts < 3) throw new WebException("connection dropped", WebExceptionStatus.ReceiveFailure);
                    return Task.FromResult(true);
                }).GetAwaiter().GetResult();
            Check(attempts == 3 && File.ReadAllText(destination) == "complete", "successful retry never appends to an earlier partial download");

            string failedDestination = Path.Combine(root, "failed.exe");
            attempts = 0;
            ExpectFailure(delegate
            {
                UpdateManager.DownloadFileWithRetryAsync(new Uri("https://github.com/ZhiHanyu-H57/InputStitch/releases/download/test/file.exe"), failedDestination,
                    delegate(Uri uri, string path)
                    {
                        attempts++;
                        File.WriteAllText(path, "partial-final");
                        throw new WebException("still offline", WebExceptionStatus.Timeout);
                    }).GetAwaiter().GetResult();
            }, "all download attempts fail");
            Check(attempts == 3, "permanent transient outage stops after the bounded retry count");
            Check(!File.Exists(failedDestination), "final failed attempt leaves no partial executable behind");

            attempts = 0;
            string manifest = UpdateManager.DownloadStringWithRetryAsync(new Uri("https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download/InputStitch-update.xml"),
                delegate(Uri uri)
                {
                    attempts++;
                    if (attempts == 1) throw new WebException("DNS temporarily unavailable", WebExceptionStatus.NameResolutionFailure);
                    return Task.FromResult("<manifest/>");
                }).GetAwaiter().GetResult();
            Check(attempts == 2 && manifest == "<manifest/>", "manifest check also uses bounded transient retry");

            Console.WriteLine("PASS update network: " + checks + " checks; injected failures only, no network request was made.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
