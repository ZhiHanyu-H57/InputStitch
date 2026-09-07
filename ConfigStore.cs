using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace InputStitch
{
    // All staging and replacement stay on the configuration volume. No delete-before-move.
    internal class ConfigStore
    {
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
                    string backup = Path.Combine(backups, (validOld ? "config-" : "unreadable-") +
                        DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff") + "-" + Guid.NewGuid().ToString("N") + ".xml");
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
