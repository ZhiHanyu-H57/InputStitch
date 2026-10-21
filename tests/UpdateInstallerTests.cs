using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using InputStitch;

internal static class UpdateInstallerTests
{
    private static int checks;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    private sealed class Rig : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "InputStitch-update-test-" + Guid.NewGuid().ToString("N"));
        internal string Source, Target, Config, OldHash, NewHash;
        internal int Launches;
        internal Rig()
        {
            Directory.CreateDirectory(Root);
            Source = Path.Combine(Root, "download.exe"); Target = Path.Combine(Root, "app.exe"); Config = Path.Combine(Root, "config.xml");
            File.WriteAllText(Source, "new executable test bytes"); File.WriteAllText(Target, "old executable test bytes");
            File.WriteAllText(Config, "<user-config>keep me</user-config>");
            OldHash = UpdateManager.ComputeSha256(Target); NewHash = UpdateManager.ComputeSha256(Source);
        }
        internal UpdateInstallReceipt Run(UpdateFiles files, bool failLaunch)
        {
            return UpdateInstaller.Install(Source, Target, OldHash, NewHash, Config, Path.Combine(Root,"backups"),
                delegate(string path) { Launches++; Check(UpdateManager.ComputeSha256(path) == NewHash, "launch only verified target"); if (failLaunch) throw new IOException("injected launch failure"); }, files);
        }
        internal void OriginalIntact() { Check(UpdateManager.ComputeSha256(Target) == OldHash, "original executable must survive"); }
        public void Dispose()
        {
            string prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + "InputStitch-update-test-";
            if (!Path.GetFullPath(Root).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe test cleanup path");
            Directory.Delete(Root, true);
        }
    }
    private sealed class Faults : UpdateFiles
    {
        internal string FailCopySuffix;
        internal bool CorruptStage, FailReplace, FailRecovery, FailMove;
        private int replaces;
        public override void Copy(string source, string destination)
        {
            if (FailCopySuffix != null && destination.EndsWith(FailCopySuffix, StringComparison.Ordinal)) throw new IOException("injected copy failure");
            base.Copy(source, destination);
            if (CorruptStage && destination.EndsWith(".tmp", StringComparison.Ordinal)) File.AppendAllText(destination, "corrupt");
        }
        public override void Replace(string source, string target, string backup)
        {
            replaces++;
            if (FailReplace || (FailRecovery && replaces > 1)) throw new IOException("injected replacement failure");
            base.Replace(source, target, backup);
        }
        public override void Move(string source, string target)
        { if (FailMove) throw new IOException("injected rename failure"); base.Move(source, target); }
    }
    private static string Fails(Action action)
    {
        try { action(); }
        catch (IOException ex) { checks++; return ex.Message; }
        catch (InvalidDataException ex) { checks++; return ex.Message; }
        throw new Exception("Expected safe failure");
    }
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            EmbeddedDependencyLoader.Register();
            string childRoot = Environment.GetEnvironmentVariable("INPUTSTITCH_UPDATE_TEST_ROOT");
            if (string.IsNullOrEmpty(childRoot) && File.Exists(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "update-fixture.marker")))
                childRoot = Path.GetDirectoryName(Application.ExecutablePath);
            if (!string.IsNullOrEmpty(childRoot))
            {
                string prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + "InputStitch-update-flow-";
                if (!Path.GetFullPath(childRoot).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe update child root");
                foreach (string name in new[] { "Root", "Config", "DeviceRegistry", "MacroPackages", "Profiles", "Backups", "Logs" })
                    typeof(AppPaths).GetField(name).SetValue(null, name == "Root" ? childRoot : Path.Combine(childRoot,
                        name == "Config" ? "config.xml" : name == "DeviceRegistry" ? "devices.xml" : name.ToLowerInvariant()));
                typeof(UpdateManager).GetField("UpdatesDirectory", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(childRoot,"updates"));
                typeof(UpdateManager).GetField("PendingPath", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(childRoot,"updates","pending-update.xml"));
                if (args.Length == 1 && args[0] == "--old-process") { Thread.Sleep(700); return 0; }
                if (args.Length > 0 && args[0] == "--apply-update")
                { Check(UpdateManager.TryApplyPendingUpdate(args), "child handled update request"); return 0; }
                File.WriteAllText(Path.Combine(childRoot,"started.txt"), Application.ExecutablePath); return 0;
            }
            using (Rig r = new Rig())
            {
                UpdateInstallReceipt receipt = r.Run(new UpdateFiles(), false);
                Check(r.Launches == 1 && UpdateManager.ComputeSha256(r.Target) == r.NewHash, "successful install");
                Check(UpdateManager.ComputeSha256(receipt.ExecutableBackup) == r.OldHash, "verified executable backup");
                Check(File.ReadAllText(receipt.ConfigurationBackup) == File.ReadAllText(r.Config), "main configuration snapshot");
                Check(Directory.GetFiles(r.Root,"*.tmp").Length == 0, "staging cleanup");
            }
            foreach (Faults fault in new Faults[] { new Faults { FailCopySuffix=".tmp" }, new Faults { FailCopySuffix=".previous.exe" },
                new Faults { FailCopySuffix="config.xml" }, new Faults { CorruptStage=true }, new Faults { FailReplace=true } })
            using (Rig r = new Rig())
            {
                Fails(delegate { r.Run(fault, false); }); r.OriginalIntact();
                Check(r.Launches == 0, "failed preparation must not launch");
                Check(Directory.GetFiles(r.Root,"*.tmp").Length == 0, "failed staging cleanup");
            }
            using (Rig r = new Rig())
            {
                Fails(delegate { r.Run(new UpdateFiles(), true); }); r.OriginalIntact();
                Check(Directory.GetFiles(r.Root,"*.failed.exe").Length == 1, "retain rejected build after launch failure");
                Check(Directory.GetFiles(r.Root,"*.previous.exe").Length == 1, "rollback preserves old backup");
            }
            using (Rig r = new Rig())
            {
                string error = Fails(delegate { r.Run(new Faults { FailRecovery=true }, true); });
                Check(error.Contains("automatic recovery also failed"), "recovery failure clearly distinguished");
                string[] backup = Directory.GetFiles(r.Root,"*.previous.exe");
                Check(backup.Length == 1 && UpdateManager.ComputeSha256(backup[0]) == r.OldHash, "recovery failure retains intact manual backup");
            }
            using (Rig r = new Rig())
            using (FileStream held = new FileStream(r.Target,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                Fails(delegate { r.Run(new UpdateFiles(), false); }); r.OriginalIntact(); Check(r.Launches == 0,"locked target never launches");
            }
            using (Rig r = new Rig())
            {
                File.AppendAllText(r.Source,"tampered");
                Fails(delegate { r.Run(new UpdateFiles(), false); }); r.OriginalIntact();
            }
            using (Rig r = new Rig())
            {
                File.AppendAllText(r.Target,"user changed"); string changed = UpdateManager.ComputeSha256(r.Target);
                Fails(delegate { r.Run(new UpdateFiles(), false); });
                Check(UpdateManager.ComputeSha256(r.Target) == changed, "changed target is not overwritten");
            }
            using (Rig r = new Rig())
            {
                File.Delete(r.Config); UpdateInstallReceipt receipt = r.Run(new UpdateFiles(), false);
                Check(receipt.ConfigurationBackup == null,"missing configuration permitted without inventing data");
            }
            TestStableName();
            TestProcessUpdate();
            Console.WriteLine("PASS updater: " + checks + " checks; temporary files, injected faults, shortcuts and real isolated update/restart child processes; no user data changed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void TestProcessUpdate()
    {
        string root = Path.Combine(Path.GetTempPath(), "InputStitch-update-flow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root,"updates"));
        try
        {
            File.WriteAllText(Path.Combine(root,"update-fixture.marker"), "isolated updater test");
            string oldPath = Path.Combine(root,"InputStitch-1.4.3-Windows-x64.exe");
            string source = Path.Combine(root,"updates","download.exe");
            File.Copy(Application.ExecutablePath, oldPath); File.Copy(Application.ExecutablePath, source);
            string fixture = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","..","tests","fixtures","config-1.4.3.xml"));
            string config = Path.Combine(root,"config.xml"); File.Copy(fixture, config);
            string configHash = UpdateManager.ComputeSha256(config);
            ProcessStartInfo oldStart = new ProcessStartInfo(oldPath,"--old-process") { UseShellExecute = false, CreateNoWindow = true };
            oldStart.EnvironmentVariables["INPUTSTITCH_UPDATE_TEST_ROOT"] = root;
            using (Process old = Process.Start(oldStart))
            {
                PendingUpdate pending = new PendingUpdate { Token = Guid.NewGuid().ToString("N"), SourcePath = source, TargetPath = oldPath,
                    Sha256 = UpdateManager.ComputeSha256(source), OriginalSha256 = UpdateManager.ComputeSha256(oldPath),
                    CreatedUtc = DateTime.UtcNow.ToString("o"), OriginalProcessStartedUtc = old.StartTime.ToUniversalTime().ToString("o") };
                using (FileStream stream = File.Create(Path.Combine(root,"updates","pending-update.xml"))) new XmlSerializer(typeof(PendingUpdate)).Serialize(stream,pending);
                ProcessStartInfo updateStart = new ProcessStartInfo(source,"--apply-update " + pending.Token + " " + old.Id) { UseShellExecute = false, CreateNoWindow = true };
                updateStart.EnvironmentVariables["INPUTSTITCH_UPDATE_TEST_ROOT"] = root;
                using (Process updater = Process.Start(updateStart))
                    Check(updater.WaitForExit(15000) && updater.ExitCode == 0, "real update helper exits successfully after waiting for old process");
                Check(old.WaitForExit(3000), "old test process exited normally");
            }
            string started = Path.Combine(root,"started.txt");
            Stopwatch wait = Stopwatch.StartNew(); while (!File.Exists(started) && wait.ElapsedMilliseconds < 5000) Thread.Sleep(10);
            Check(File.Exists(started) && File.ReadAllText(started) == Path.Combine(root,"InputStitch.exe"), "real restarted process uses fixed executable name");
            Check(!File.Exists(oldPath) && File.Exists(Path.Combine(root,"InputStitch.exe")), "real helper removes obsolete active executable path");
            Check(UpdateManager.ComputeSha256(config) == configHash, "real helper leaves the original 1.4.3 config byte-for-byte intact");
            Check(!File.Exists(Path.Combine(root,"updates","pending-update.xml")), "successful real update consumes pending request");
            string[] backups = Directory.GetFiles(Path.Combine(root,"backups"),"config.xml",SearchOption.AllDirectories);
            Check(backups.Length == 1 && UpdateManager.ComputeSha256(backups[0]) == configHash, "real update backs up original 1.4.3 config");
        }
        finally
        {
            // Let the short-lived installed test process finish without killing it.
            Thread.Sleep(100);
            Directory.Delete(root,true);
        }
    }
    private static void TestStableName()
    {
        using (Rig r = new Rig())
        {
            string installed = Path.Combine(r.Root, "InputStitch.exe");
            UpdateInstallReceipt receipt = UpdateInstaller.Install(r.Source, r.Target, r.OldHash, r.NewHash,
                r.Config, Path.Combine(r.Root, "backups"), delegate(string path)
                { Check(path == installed && UpdateManager.ComputeSha256(path) == r.NewHash, "fixed-name verified launch"); r.Launches++; }, new UpdateFiles(), installed);
            Check(receipt.InstalledPath == installed && !File.Exists(r.Target) && r.Launches == 1, "old versioned program path replaced by fixed name");
            Check(UpdateManager.ComputeSha256(receipt.ExecutableBackup) == r.OldHash, "rename retains original executable backup");
            Check(File.ReadAllText(receipt.ConfigurationBackup) == File.ReadAllText(r.Config), "rename retains exact config backup");
        }
        foreach (bool failRename in new[] { false, true })
        using (Rig r = new Rig())
        {
            string installed = Path.Combine(r.Root, "InputStitch.exe");
            Fails(delegate { UpdateInstaller.Install(r.Source, r.Target, r.OldHash, r.NewHash, r.Config,
                Path.Combine(r.Root,"backups"), delegate(string path) { throw new IOException("injected launch failure after rename"); },
                new Faults { FailMove = failRename }, installed); });
            r.OriginalIntact(); Check(!File.Exists(installed), "rename/launch failure restores old path without an active rejected program");
        }
        using (Rig r = new Rig())
        {
            string installed = Path.Combine(r.Root, "InputStitch.exe"); File.WriteAllText(installed, "existing user file");
            Fails(delegate { UpdateInstaller.Install(r.Source, r.Target, r.OldHash, r.NewHash, r.Config,
                Path.Combine(r.Root,"backups"), delegate(string path) { r.Launches++; }, new UpdateFiles(), installed); });
            r.OriginalIntact(); Check(File.ReadAllText(installed) == "existing user file" && r.Launches == 0, "fixed-name collision never overwrites an existing file");
        }
        using (Rig r = new Rig())
        {
            string installed = Path.Combine(r.Root, "InputStitch.exe");
            string originalLink = Path.Combine(r.Root, "app.lnk"), customLink = Path.Combine(r.Root, "My macros.lnk"), otherLink = Path.Combine(r.Root, "other.lnk");
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            try
            {
                foreach (string path in new[] { originalLink, customLink, otherLink })
                {
                    object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                    try
                    {
                        Type type = link.GetType();
                        type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { path == otherLink ? r.Source : r.Target });
                        type.InvokeMember("Arguments", BindingFlags.SetProperty, null, link, new object[] { "--example" });
                        type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { r.Root });
                        type.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { r.Target + ",0" });
                        type.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
                    }
                    finally { Marshal.FinalReleaseComObject(link); }
                }
                Check(UpdateShortcuts.Retarget(new[] { originalLink, customLink, otherLink }, r.Target, installed) == 2, "only matching installation shortcuts are changed");
                Check(!File.Exists(originalLink) && File.Exists(Path.Combine(r.Root, "InputStitch.lnk")) && File.Exists(customLink), "default shortcut name updated; custom name retained");
                foreach (string path in new[] { Path.Combine(r.Root, "InputStitch.lnk"), customLink, otherLink })
                {
                    object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                    try
                    {
                        Type type = link.GetType();
                        Check((string)type.InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null) == (path == otherLink ? r.Source : installed), "shortcut target is correct");
                        Check((string)type.InvokeMember("Arguments", BindingFlags.GetProperty, null, link, null) == "--example", "shortcut arguments retained");
                        Check((string)type.InvokeMember("WorkingDirectory", BindingFlags.GetProperty, null, link, null) == r.Root, "shortcut working folder retained");
                        if (path != otherLink) Check(((string)type.InvokeMember("IconLocation", BindingFlags.GetProperty, null, link, null)).StartsWith(installed + ",", StringComparison.OrdinalIgnoreCase), "shortcut icon follows installed program");
                    }
                    finally { Marshal.FinalReleaseComObject(link); }
                }
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }
    }
}
