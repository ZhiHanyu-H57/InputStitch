using System;
using System.IO;

namespace InputStitch
{
    // File operations are injectable so faults can be exercised without replacing
    // a running program, launching processes or touching real user configuration.
    internal class UpdateFiles
    {
        public virtual void Copy(string source, string destination)
        {
            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { input.CopyTo(output); output.Flush(true); }
        }
        public virtual void Replace(string source, string target, string backup) { File.Replace(source, target, backup); }
    }

    internal sealed class UpdateInstallReceipt
    {
        public string ExecutableBackup;
        public string ConfigurationBackup;
    }

    internal static class UpdateInstaller
    {
        internal static UpdateInstallReceipt Install(string source, string target, string oldHash,
            string newHash, string configPath, string backupDirectory, Action<string> launch, UpdateFiles files)
        {
            if (launch == null || files == null) throw new ArgumentNullException();
            source = Path.GetFullPath(source); target = Path.GetFullPath(target);
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update source and target must differ.");
            Verify(source, newHash); Verify(target, oldHash);
            string id = Guid.NewGuid().ToString("N");
            // Staging and replacement always stay on the target volume.
            string stage = target + ".update-" + id + ".tmp";
            string previous = target + ".update-" + id + ".previous.exe";
            string restore = target + ".update-" + id + ".restore.tmp";
            string rejected = target + ".update-" + id + ".failed.exe";
            bool replacementAttempted = false;
            UpdateInstallReceipt receipt = new UpdateInstallReceipt { ExecutableBackup = previous };
            try
            {
                files.Copy(source, stage); Verify(stage, newHash);
                files.Copy(target, previous); Verify(previous, oldHash);
                if (File.Exists(configPath))
                {
                    string directory = Path.Combine(backupDirectory, "update-" + id);
                    Directory.CreateDirectory(directory);
                    receipt.ConfigurationBackup = Path.Combine(directory, "config.xml");
                    files.Copy(configPath, receipt.ConfigurationBackup);
                    Verify(receipt.ConfigurationBackup, UpdateManager.ComputeSha256(configPath));
                }
                Verify(target, oldHash);
                replacementAttempted = true;
                // No overwrite-copy fallback: unsupported filesystems/permissions
                // must fail safely, rather than truncate the current executable.
                files.Replace(stage, target, null);
                Verify(target, newHash);
                launch(target);
                return receipt;
            }
            catch (Exception installError)
            {
                if (replacementAttempted)
                {
                    try
                    {
                        Verify(previous, oldHash);
                        if (!File.Exists(target) || !string.Equals(UpdateManager.ComputeSha256(target), oldHash, StringComparison.OrdinalIgnoreCase))
                        {
                            files.Copy(previous, restore); Verify(restore, oldHash);
                            if (File.Exists(target)) files.Replace(restore, target, rejected);
                            else File.Move(restore, target);
                            Verify(target, oldHash);
                        }
                    }
                    catch (Exception recoveryError)
                    {
                        throw new IOException("Update failed; automatic recovery also failed. Do not discard the backup.\r\n" +
                            "更新失败且自动恢复未成功，请保留备份。\r\nBackup / 备份: " + previous +
                            "\r\nConfiguration / 配置: " + (receipt.ConfigurationBackup ?? "(none)") +
                            "\r\nRecovery / 恢复: " + recoveryError.Message, installError);
                    }
                }
                throw new IOException("Update failed; the previous executable is intact or has been restored.\r\n" +
                    "更新失败；原程序未变更或已恢复。\r\nBackup / 备份: " + previous +
                    "\r\nConfiguration / 配置: " + (receipt.ConfigurationBackup ?? "(none)") +
                    "\r\n" + installError.Message, installError);
            }
            finally
            {
                // Keep verified backups and any rejected executable for recovery.
                // Only the uniquely named, non-executable staging files are removed.
                TryDelete(stage); TryDelete(restore);
            }
        }

        private static void Verify(string path, string expected)
        {
            if (string.IsNullOrEmpty(expected) || !string.Equals(UpdateManager.ComputeSha256(path), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update file verification failed: " + path);
        }
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }
}
