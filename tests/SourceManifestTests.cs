using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

internal static class SourceManifestTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static string RepoRoot()
    {
        // The suite runs from artifacts/tests/<suite>.exe, so the repository root is two levels up.
        DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        if (dir.Parent != null) dir = dir.Parent;
        if (dir.Parent != null) dir = dir.Parent;
        return dir.FullName;
    }

    public static int Main()
    {
        string root = RepoRoot();
        string manifestPath = Path.Combine(root, "scripts", "production-sources.txt");
        Check(File.Exists(manifestPath), "production source manifest exists");

        List<string> entries = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadAllLines(manifestPath))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            Check(line.IndexOf('\\') < 0 && line.IndexOf('/') < 0, "manifest entry has no directory: " + line);
            Check(line.EndsWith(".cs", StringComparison.OrdinalIgnoreCase), "manifest entry is a .cs file: " + line);
            Check(seen.Add(line), "manifest entries are unique: " + line);
            Check(File.Exists(Path.Combine(root, line)), "manifest entry exists on disk: " + line);
            entries.Add(line);
        }
        Check(entries.Count > 0, "manifest is not empty");

        // Every root-level production source must be listed, and the manifest must not list anything else.
        List<string> rootSources = new List<string>();
        foreach (string file in Directory.GetFiles(root, "*.cs"))
            rootSources.Add(Path.GetFileName(file));
        Check(rootSources.Count == entries.Count,
            "root .cs count matches the manifest (" + rootSources.Count.ToString() + " root files)");
        foreach (string file in rootSources)
            Check(seen.Contains(file), "root source file is listed in the manifest: " + file);

        // The csproj keeps an explicit list for Visual Studio users; it must match the manifest exactly.
        string projectPath = Path.Combine(root, "InputStitch.csproj");
        Check(File.Exists(projectPath), "InputStitch.csproj exists");
        HashSet<string> projectSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(File.ReadAllText(projectPath), "<Compile Include=\"([^\"]+)\"", RegexOptions.IgnoreCase))
            projectSources.Add(match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar));
        Check(projectSources.Count == entries.Count, "csproj compile list matches the manifest count");
        foreach (string entry in entries)
            Check(projectSources.Contains(entry), "csproj lists manifest entry: " + entry);
        foreach (string resource in new[] { "mouse-color.jpg", "mouse-alpha.png" })
        {
            Check(File.Exists(Path.Combine(root, "assets", resource)), "mouse artwork exists: " + resource);
            Check(File.ReadAllText(projectPath).Contains("InputStitch.Assets." + resource), "csproj embeds mouse artwork: " + resource);
            using (Stream stream = typeof(SourceManifestTests).Assembly.GetManifestResourceStream("InputStitch.Assets." + resource))
                Check(stream != null, "test build embeds mouse artwork: " + resource);
        }
        foreach (string script in new[] { "build.ps1", "tests/Run-Tests.ps1", "tests/Run-SecondaryLayout.ps1", "tests/Run-ProductAcceptance.ps1", "tests/Run-CrosshairAcceptance.ps1", "tools/InputLab/build-acceptance.ps1" })
            Check(File.ReadAllText(Path.Combine(root, script.Replace('/', Path.DirectorySeparatorChar))).Contains("Get-UiResourceArguments.ps1"), "UI resource build parity: " + script);

        // Every build/test entry point must consume the shared manifest instead of a private copy.
        Check(ScriptUsesManifest(root, "build.ps1"), "build.ps1 consumes the shared source manifest");
        Check(ScriptUsesManifest(root, Path.Combine("tests", "Run-Tests.ps1")), "Run-Tests.ps1 consumes the shared source manifest");
        Check(ScriptUsesManifest(root, Path.Combine("tools", "InputLab", "build-acceptance.ps1")), "Input Lab acceptance build consumes the shared source manifest");
        Check(File.ReadAllText(Path.Combine(root, "AGENTS.md")).IndexOf("production-sources.txt", StringComparison.Ordinal) >= 0,
            "AGENTS.md documents the source manifest rule");
        Check(File.ReadAllText(Path.Combine(root, "CONTRIBUTING.md")).IndexOf("production-sources.txt", StringComparison.Ordinal) >= 0,
            "CONTRIBUTING.md documents the source manifest rule");

        Console.WriteLine("PASS source manifest: " + checks.ToString() + " checks; single source list plus drift guards, no build or device side effects.");
        return 0;
    }

    private static bool ScriptUsesManifest(string root, string relativePath)
    {
        string path = Path.Combine(root, relativePath);
        if (!File.Exists(path)) return false;
        return File.ReadAllText(path).IndexOf("Get-ProductionSources.ps1", StringComparison.Ordinal) >= 0;
    }
}
