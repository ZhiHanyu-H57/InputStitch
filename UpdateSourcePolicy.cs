using System;
using System.Collections.Generic;
using System.IO;

namespace InputStitch
{
    internal static class UpdateSourcePolicy
    {
        internal static IList<Uri> ManifestUris(string cacheToken)
        {
            string suffix = string.IsNullOrEmpty(cacheToken) ? "" : "?cache=" + Uri.EscapeDataString(cacheToken);
            return new List<Uri>
            {
                new Uri(AppInfo.UpdateManifestUrl + suffix),
                new Uri(AppInfo.UpdateManifestFallbackUrl + suffix)
            };
        }

        internal static IList<Uri> AssetUris(UpdateCheckResult update)
        {
            if (update == null || update.Asset == null || update.Manifest == null)
                throw new ArgumentNullException("update");

            Uri primary = new Uri(update.Asset.Url);
            List<Uri> result = new List<Uri> { primary };
            Uri github = GithubAssetUri(update.Manifest.Version, update.Asset.FileName);
            if (!SameEndpoint(primary, github)) result.Add(github);
            return result;
        }

        internal static Uri GithubAssetUri(string version, string fileName)
        {
            ValidateVersionAndFileName(version, fileName);
            return new Uri(AppInfo.UpdateGithubReleaseBaseUrl + "/v" + version + "/" + fileName);
        }

        internal static bool IsOfficialAssetUri(Uri uri, string version, string fileName, string architecture)
        {
            if (uri == null || uri.Scheme != Uri.UriSchemeHttps) return false;
            ValidateVersionAndFileName(version, fileName);
            ValidateArchitectureFileName(fileName, architecture, version);

            string expectedR2 = "/releases/v" + version + "/" + fileName;
            if (string.Equals(uri.Host, AppInfo.UpdateDownloadHost, StringComparison.OrdinalIgnoreCase))
                return string.Equals(uri.AbsolutePath, expectedR2, StringComparison.Ordinal);

            if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
            string versionedGithub = "/ZhiHanyu-H57/InputStitch/releases/download/v" + version + "/" + fileName;
            string latestGithub = "/ZhiHanyu-H57/InputStitch/releases/latest/download/" + fileName;
            return string.Equals(uri.AbsolutePath, versionedGithub, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(uri.AbsolutePath, latestGithub, StringComparison.OrdinalIgnoreCase);
        }

        internal static void ValidateArchitectureFileName(string fileName, string architecture, string version)
        {
            if (!string.Equals(architecture, "x64", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(architecture, "x86", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update architecture is invalid.");
            string expected = "InputStitch-" + version + "-Windows-" + architecture.ToLowerInvariant() + ".exe";
            if (!string.Equals(fileName, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update manifest contains an invalid asset file name.");
        }

        private static void ValidateVersionAndFileName(string version, string fileName)
        {
            Version parsed;
            if (string.IsNullOrWhiteSpace(version) || !Version.TryParse(version, out parsed))
                throw new InvalidDataException("The update manifest contains an invalid version.");
            if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
                fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0)
                throw new InvalidDataException("The update manifest contains an invalid asset file name.");
        }

        private static bool SameEndpoint(Uri left, Uri right)
        {
            return left != null && right != null &&
                string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) &&
                left.Port == right.Port &&
                string.Equals(left.AbsolutePath, right.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
