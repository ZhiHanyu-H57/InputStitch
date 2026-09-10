using System;
using System.IO;

namespace InputStitch
{
    internal static class ProfileCatalog
    {
        internal static string FindBoundProfileForProcess(string profilesDirectory, string processName)
        {
            try
            {
                if (!Directory.Exists(profilesDirectory)) return "";
                string[] files = Directory.GetFiles(profilesDirectory, "*.mpprofile");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    try
                    {
                        ProfilePackage profile = ConfigPackageSerializer.DeserializeProfilePackageOrLegacy(file);
                        if (profile != null && !string.IsNullOrWhiteSpace(profile.BoundProcessName) &&
                            string.Equals(profile.BoundProcessName, processName, StringComparison.OrdinalIgnoreCase))
                            return file;
                    }
                    catch (Exception ex) { AppLog.Write("Profile metadata read failed: " + file, ex); }
                }
            }
            catch (Exception ex) { AppLog.Write("Profile scan failed", ex); }
            return "";
        }
    }
}
