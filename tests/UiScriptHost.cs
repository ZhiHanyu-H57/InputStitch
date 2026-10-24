using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

internal static class UiScriptHost
{
    [STAThread] private static int Main(string[] args)
    { return IsolatedUiDesktop.Run(() => Execute(args)); }
    private static int Execute(string[] args)
    {
        try
        {
            if (args.Length == 0) throw new ArgumentException("A test script path is required.");
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.ApartmentState = System.Threading.ApartmentState.STA;
                runspace.ThreadOptions = PSThreadOptions.UseCurrentThread; runspace.Open();
                using (PowerShell shell = PowerShell.Create())
                {
                    shell.Runspace = runspace;
                    shell.AddScript("Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force"); shell.Invoke(); shell.Commands.Clear();
                    string modules = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32/WindowsPowerShell/v1.0/Modules");
                    shell.AddCommand("Import-Module").AddArgument(Path.Combine(modules, "Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1"));
                    shell.Invoke(); shell.Commands.Clear();
                    if (shell.HadErrors) throw new InvalidOperationException("Cannot load the Windows PowerShell utility module.");
                    shell.AddCommand(Path.GetFullPath(args[0]));
                    for (int i = 1; i < args.Length; i++)
                    {
                        string key = args[i].TrimStart('-');
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)) shell.AddParameter(key, args[++i]);
                        else shell.AddParameter(key);
                    }
                    foreach (PSObject value in shell.Invoke()) Console.WriteLine(value);
                    if (shell.HadErrors) { foreach (ErrorRecord error in shell.Streams.Error) Console.Error.WriteLine(error); return 1; }
                }
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.GetType().Name + ": " + error.Message); return 1; }
    }
}
