using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace InputStitch
{
    // All staging and replacement stay on the configuration volume. No delete-before-move.
    internal class ConfigStore
    {
        private const string BackupStampFormat = "yyyyMMddTHHmmssfffffff";
        private static readonly object BackupNameSync = new object();
        private static readonly Dictionary<string, long> LastAllocatedBackupTicksByDirectory =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        internal virtual DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        internal virtual void Write(string path, MacroConfig config)
        {
            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                new XmlSerializer(typeof(MacroConfig)).Serialize(stream, config);
                stream.Flush(true);
            }
        }

        internal virtual void Validate(string path)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            using (XmlReader reader = XmlReader.Create(path, settings))
            {
                MacroConfig value = new XmlSerializer(typeof(MacroConfig)).Deserialize(reader) as MacroConfig;
                if (value == null || value.Macros == null || value.Macros.Any(m => m == null || m.Steps == null || m.Steps.Any(s => s == null)))
                    throw new InvalidDataException("Invalid macro configuration.");
            }
        }

        internal virtual void Replace(string staged, string target, string backup)
        {
            File.Replace(staged, target, backup);
        }

        internal void Save(string path, MacroConfig config)
        {
            path = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string staged = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Write(staged, config);
                Validate(staged);
                if (File.Exists(path))
                {
                    // Do not rotate backups on focus changes that saved identical data.
                    if (File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(staged))) return;
                    bool validOld = true;
                    try { Validate(path); } catch { validOld = false; }
                    string backups = Path.Combine(directory, "backups", "config");
                    Directory.CreateDirectory(backups);
                    string backup = CreateBackupPath(backups, validOld);
                    Replace(staged, path, backup);
                    // Replacement keeps the exact old file. Unreadable originals are retained separately.
                    try { TrimBackups(backups); }
                    catch (Exception ex) { AppLog.Write("Config backup cleanup failed; saved configuration is intact", ex); }
                }
                else File.Move(staged, path);
            }
            finally
            {
                try { if (File.Exists(staged)) File.Delete(staged); } catch { }
            }
        }

        private string CreateBackupPath(string directory, bool validOld)
        {
            string prefix = validOld ? "config-" : "unreadable-";
            DateTime stamp = GetUtcNow().ToUniversalTime();

            if (validOld)
            {
                // File-name ordering is the backup retention ordering. DateTime.UtcNow can return the
                // same value for several rapid saves on Windows, despite the seven fractional digits in
                // the formatted name. Guarantee a strictly increasing stamp so GUID ordering can never
                // decide which valid backup is considered newest. Scan existing files as well so the
                // guarantee survives process restart or a short wall-clock rollback.
                lock (BackupNameSync)
                {
                    string directoryKey = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    long lastAllocatedBackupTicks;
                    if (!LastAllocatedBackupTicksByDirectory.TryGetValue(directoryKey, out lastAllocatedBackupTicks))
                        lastAllocatedBackupTicks = 0;
                    long newestExistingTicks = 0;
                    foreach (string existing in Directory.GetFiles(directory, "config-*.xml"))
                    {
                        string name = Path.GetFileName(existing);
                        if (name == null || name.Length < prefix.Length + BackupStampFormat.Length) continue;
                        string text = name.Substring(prefix.Length, BackupStampFormat.Length);
                        DateTime parsed;
                        if (DateTime.TryParseExact(text, BackupStampFormat, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
                            newestExistingTicks = Math.Max(newestExistingTicks, parsed.Ticks);
                    }

                    long ticks = Math.Max(stamp.Ticks, Math.Max(lastAllocatedBackupTicks + 1, newestExistingTicks + 1));
                    if (ticks > DateTime.MaxValue.Ticks) ticks = DateTime.MaxValue.Ticks;
                    stamp = new DateTime(ticks, DateTimeKind.Utc);
                    LastAllocatedBackupTicksByDirectory[directoryKey] = stamp.Ticks;
                }
            }

            return Path.Combine(directory, prefix + stamp.ToString(BackupStampFormat, CultureInfo.InvariantCulture) +
                "-" + Guid.NewGuid().ToString("N") + ".xml");
        }

        private void TrimBackups(string directory)
        {
            int valid = 0;
            foreach (string path in Directory.GetFiles(directory, "config-*.xml").OrderByDescending(p => p, StringComparer.Ordinal))
            {
                try { Validate(path); } catch { continue; }
                if (++valid > 5) File.Delete(path);
            }
        }
    }
}
