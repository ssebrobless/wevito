using System.Windows.Forms;
using System.Diagnostics;

namespace Wevito.VNext.Broker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Shell-side policy also applies priority; broker startup should not fail if Windows refuses this.
        }

        var pipeName = "wevito-vnext";
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--pipe-name", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                pipeName = args[i + 1];
                i++;
            }
        }

        ApplicationConfiguration.Initialize();
        using var context = new BrokerApplicationContext(pipeName);
        Application.Run(context);
    }
}
