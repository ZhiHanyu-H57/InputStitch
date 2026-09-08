using System;
using System.Windows.Forms;

namespace InputStitch.Tools.InputLab
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int initialView = 0;
            for (int i = 0; args != null && i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "--view", StringComparison.OrdinalIgnoreCase)) continue;
                string value = args[i + 1];
                if (string.Equals(value, "devices", StringComparison.OrdinalIgnoreCase)) initialView = 1;
                else if (string.Equals(value, "log", StringComparison.OrdinalIgnoreCase)) initialView = 2;
                break;
            }

            Application.Run(new InputLabForm(initialView));
        }
    }
}
