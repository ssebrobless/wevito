using System.Windows;

namespace Wevito.VNext.Shell;

public partial class App : Application
{
    private ShellCoordinator? _coordinator;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        // When future settings/credential windows are split out, call SetWindowDisplayAffinity
        // on those surfaces too so they are excluded from Wevito capture artifacts.

        _coordinator = new ShellCoordinator(this);
        await _coordinator.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_coordinator is not null)
        {
            await _coordinator.DisposeAsync();
        }

        base.OnExit(e);
    }
}
