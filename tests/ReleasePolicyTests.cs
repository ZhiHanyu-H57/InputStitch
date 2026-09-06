using System;
using System.Reflection;
using InputStitch;
internal static class ReleasePolicyTests
{
    public static int Main()
    {
        try
        {
            if (ReleaseInfo.IsPrerelease != ReleaseInfo.Version.Contains("-beta.")) throw new Exception("Channel/version mismatch");
            if (ReleaseInfo.AutomaticChecksAllowed == ReleaseInfo.IsPrerelease) throw new Exception("Beta automatic checks enabled");
            if (AppInfo.UpdateManifestUrl != "https://github.com/ZhiHanyu-H57/InputStitch/releases/latest/download/InputStitch-update.xml") throw new Exception("Stable endpoint changed");
            string actual = ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(typeof(AppInfo).Assembly, typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;
            if (actual != ReleaseInfo.Version) throw new Exception("Display/assembly version mismatch");
            if (ReleaseInfo.IsPrerelease)
            {
                bool rejected = false;
                try { UpdateManager.CheckAsync().GetAwaiter().GetResult(); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new Exception("Beta attempted an automatic update check");
            }
            Console.WriteLine("PASS release channel, metadata, stable endpoint and beta no-network guard.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
