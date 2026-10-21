using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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
        public virtual void Move(string source, string target) { File.Move(source, target); }
    }

    internal sealed class UpdateInstallReceipt
    {
        public string ExecutableBackup;
        public string ConfigurationBackup;
        public string InstalledPath;
    }

    internal static class UpdateInstaller
    {
        internal static UpdateInstallReceipt Install(string source, string target, string oldHash,
            string newHash, string configPath, string backupDirectory, Action<string> launch, UpdateFiles files,
            string installedPath = null)
        {
            if (launch == null || files == null) throw new ArgumentNullException();
            source = Path.GetFullPath(source); target = Path.GetFullPath(target);
            installedPath = Path.GetFullPath(installedPath ?? target);
            bool rename = !string.Equals(target, installedPath, StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(Path.GetDirectoryName(target), Path.GetDirectoryName(installedPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The installed program must stay in its original folder.");
            if (rename && File.Exists(installedPath))
                throw new IOException("InputStitch.exe already exists in this folder. No files were replaced. / 此文件夹已有 InputStitch.exe，未替换任何文件。");
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update source and target must differ.");
            Verify(source, newHash); Verify(target, oldHash);
            string id = Guid.NewGuid().ToString("N");
            // Staging and replacement always stay on the target volume.
            string stage = target + ".update-" + id + ".tmp";
            string previous = target + ".update-" + id + ".previous.exe";
            string restore = target + ".update-" + id + ".restore.tmp";
            string rejected = target + ".update-" + id + ".failed.exe";
            bool replacementAttempted = false;
            bool renamed = false;
            UpdateInstallReceipt receipt = new UpdateInstallReceipt { ExecutableBackup = previous, InstalledPath = installedPath };
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
                if (rename) { files.Move(target, installedPath); renamed = true; Verify(installedPath, newHash); }
                launch(installedPath);
                return receipt;
            }
            catch (Exception installError)
            {
                if (replacementAttempted)
                {
                    try
                    {
                        Verify(previous, oldHash);
                        if (renamed)
                        {
                            Verify(installedPath, newHash);
                            files.Move(installedPath, rejected);
                        }
                        if (!File.Exists(target) || !string.Equals(UpdateManager.ComputeSha256(target), oldHash, StringComparison.OrdinalIgnoreCase))
                        {
                            files.Copy(previous, restore); Verify(restore, oldHash);
                            if (File.Exists(target)) files.Replace(restore, target, rejected);
                            else files.Move(restore, target);
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

    // Update only links that already point to this installation. No shortcuts
    // are created, and user-assigned names, arguments and working folders remain.
    internal static class UpdateShortcuts
    {
        internal static void RetargetKnownFolders(string oldPath, string newPath)
        {
            foreach (Environment.SpecialFolder folder in new[] { Environment.SpecialFolder.DesktopDirectory,
                Environment.SpecialFolder.StartMenu, Environment.SpecialFolder.CommonDesktopDirectory,
                Environment.SpecialFolder.CommonStartMenu })
            {
                string root = Environment.GetFolderPath(folder);
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try { Retarget(Directory.GetFiles(root, "*.lnk", SearchOption.AllDirectories), oldPath, newPath); }
                catch (Exception ex) { AppLog.Write("Update shortcut folder could not be read", ex); }
            }
        }

        internal static int Retarget(string[] paths, string oldPath, string newPath)
        {
            int changed = 0;
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return 0;
            object shell = Activator.CreateInstance(shellType);
            try
            {
                foreach (string path in paths)
                {
                    object link = null;
                    try
                    {
                        link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                        Type type = link.GetType();
                        string target = (string)type.InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null);
                        if (string.IsNullOrEmpty(target) || !string.Equals(Path.GetFullPath(target), Path.GetFullPath(oldPath), StringComparison.OrdinalIgnoreCase)) continue;
                        type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { newPath });
                        string icon = (string)type.InvokeMember("IconLocation", BindingFlags.GetProperty, null, link, null);
                        if (icon.StartsWith(oldPath + ",", StringComparison.OrdinalIgnoreCase))
                            type.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { newPath + icon.Substring(oldPath.Length) });
                        type.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
                        changed++;
                        if (string.Equals(Path.GetFileNameWithoutExtension(path), Path.GetFileNameWithoutExtension(oldPath), StringComparison.OrdinalIgnoreCase))
                        {
                            string renamed = Path.Combine(Path.GetDirectoryName(path), "InputStitch.lnk");
                            if (!string.Equals(path, renamed, StringComparison.OrdinalIgnoreCase) && !File.Exists(renamed)) File.Move(path, renamed);
                        }
                    }
                    catch (Exception ex) { AppLog.Write("Update shortcut could not be retargeted: " + path, ex); }
                    finally { if (link != null && Marshal.IsComObject(link)) Marshal.FinalReleaseComObject(link); }
                }
            }
            finally { if (Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell); }
            return changed;
        }
    }
}
