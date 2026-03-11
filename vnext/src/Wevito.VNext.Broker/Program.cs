using System.Windows.Forms;

namespace Wevito.VNext.Broker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
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
