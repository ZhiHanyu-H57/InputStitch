using System;
using System.IO;
using System.Xml.Serialization;

namespace InputStitch
{
    internal sealed class AtomicXmlFileStore<T> where T : class
    {
        private readonly string path;

        internal AtomicXmlFileStore(string fullPath)
        {
            path = fullPath;
        }

        internal T Load()
        {
            if (!File.Exists(path)) return null;
            using (FileStream stream = File.OpenRead(path))
                return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
        }

        internal void Save(T value)
        {
            if (value == null) throw new ArgumentNullException("value");
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            string staged = path + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(staged, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new XmlSerializer(typeof(T)).Serialize(stream, value);
                    stream.Flush(true);
                }
                using (FileStream stream = File.OpenRead(staged))
                    new XmlSerializer(typeof(T)).Deserialize(stream);
                if (File.Exists(path)) File.Replace(staged, path, null);
                else File.Move(staged, path);
            }
            finally
            {
                try { if (File.Exists(staged)) File.Delete(staged); } catch { }
            }
        }

        internal void Clear()
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
