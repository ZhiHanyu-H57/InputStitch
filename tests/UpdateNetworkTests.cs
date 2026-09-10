using System;
using System.Collections.Generic;
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

            IList<Uri> manifestUris = UpdateSourcePolicy.ManifestUris("test-cache");
            Check(manifestUris.Count == 2 && manifestUris[0].Host == "download.zhihanyu.com" && manifestUris[1].Host == "github.com",
                "stable update checks prefer the website manifest and retain GitHub as fallback");
            Check(manifestUris[0].Query.Contains("test-cache") && manifestUris[1].Query.Contains("test-cache"),
                "both manifest sources receive the same cache-busting token");

            UpdateCheckResult sourcePlan = new UpdateCheckResult
            {
                Manifest = new UpdateManifest { Version = "1.4.2" },
                Asset = new UpdateAsset
                {
                    Architecture = "x64",
                    FileName = "InputStitch-1.4.2-Windows-x64.exe",
                    Url = "https://download.zhihanyu.com/releases/v1.4.2/InputStitch-1.4.2-Windows-x64.exe",
                    Sha256 = new string('a', 64)
                },
                IsAvailable = true
            };
            IList<Uri> assetUris = UpdateSourcePolicy.AssetUris(sourcePlan);
            Check(assetUris.Count == 2 && assetUris[0].Host == "download.zhihanyu.com" && assetUris[1].Host == "github.com",
                "update downloads prefer the website asset and derive a GitHub release fallback");
            Check(assetUris[1].AbsolutePath == "/ZhiHanyu-H57/InputStitch/releases/download/v1.4.2/InputStitch-1.4.2-Windows-x64.exe",
                "GitHub fallback is pinned to the checked release version rather than mutable latest");
            Check(UpdateSourcePolicy.IsOfficialAssetUri(assetUris[0], "1.4.2", sourcePlan.Asset.FileName, "x64"),
                "versioned website asset is accepted as an official update source");
            Check(UpdateSourcePolicy.IsOfficialAssetUri(assetUris[1], "1.4.2", sourcePlan.Asset.FileName, "x64"),
                "versioned GitHub fallback asset is accepted as an official update source");
            Check(!UpdateSourcePolicy.IsOfficialAssetUri(new Uri("https://example.com/releases/v1.4.2/InputStitch-1.4.2-Windows-x64.exe"), "1.4.2", sourcePlan.Asset.FileName, "x64"),
                "third-party asset hosts remain rejected");
            ExpectFailure(delegate
            {
                UpdateSourcePolicy.ValidateArchitectureFileName("InputStitch-1.4.3-Windows-x64.exe", "x64", "1.4.2");
            }, "asset file name from a different release version");

            int sourceAttempts = 0;
            string fallbackResult = UpdateManager.ExecuteWithSourceFallbackAsync<string>(manifestUris, delegate(Uri uri)
            {
                sourceAttempts++;
                if (uri.Host == "download.zhihanyu.com") throw new InvalidDataException("primary manifest unavailable or invalid");
                return Task.FromResult("github-ok");
            }).GetAwaiter().GetResult();
            Check(sourceAttempts == 2 && fallbackResult == "github-ok",
                "a failed website source falls through to GitHub even when the failure is not retryable network noise");

            sourceAttempts = 0;
            ExpectFailure(delegate
            {
                UpdateManager.ExecuteWithSourceFallbackAsync<string>(manifestUris, delegate(Uri uri)
                {
                    sourceAttempts++;
                    throw new WebException(uri.Host + " unavailable", WebExceptionStatus.ConnectFailure);
                }).GetAwaiter().GetResult();
            }, "both update sources fail");
            Check(sourceAttempts == 2, "dual-source failure is surfaced only after both website and GitHub have been tried");

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
