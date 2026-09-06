using System;
using System.IO;
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
        internal bool CorruptStage, FailReplace, FailRecovery;
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
    }
    private static string Fails(Action action)
    {
        try { action(); }
        catch (IOException ex) { checks++; return ex.Message; }
        catch (InvalidDataException ex) { checks++; return ex.Message; }
        throw new Exception("Expected safe failure");
    }
    public static int Main()
    {
        try
        {
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
            Console.WriteLine("PASS updater: " + checks + " checks; actual temporary files, injected faults; no process launched or user data touched.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
