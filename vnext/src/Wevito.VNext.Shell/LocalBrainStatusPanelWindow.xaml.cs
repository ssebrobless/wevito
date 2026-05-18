using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class LocalBrainStatusPanelWindow : Window
{
    private readonly LocalBrainStatusPanelService _service;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly Func<RuntimeSupervisorStatus> _runtimeStatusProvider;

    public LocalBrainStatusPanelWindow(
        LocalBrainStatusPanelService service,
        Func<IReadOnlyDictionary<string, string>> settingsProvider,
        Func<RuntimeSupervisorStatus> runtimeStatusProvider)
    {
        _service = service;
        _settingsProvider = settingsProvider;
        _runtimeStatusProvider = runtimeStatusProvider;
        InitializeComponent();
        Render(_service.Show(_settingsProvider(), _runtimeStatusProvider(), DateTimeOffset.UtcNow));
    }

    public void RefreshFromCurrentState()
    {
        Render(_service.Show(_settingsProvider(), _runtimeStatusProvider(), DateTimeOffset.UtcNow));
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var snapshot = await _service.RefreshAsync(_settingsProvider(), _runtimeStatusProvider(), DateTimeOffset.UtcNow);
            Render(snapshot);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void Render(LocalBrainStatusPanelSnapshot snapshot)
    {
        StatusText.Text = $"state: {snapshot.StateLabel}";
        SummaryText.Text = snapshot.Summary;
        RefreshMessageText.Text = snapshot.RefreshMessage;
        RefreshMessageText.Foreground = snapshot.RefreshRateLimited
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD37D"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB6C6CE"));
        RecommendedText.Text = $"Endpoint: {snapshot.RecommendedEndpoint}\nModel: {snapshot.RecommendedModel}";
        KeepAliveText.Text = snapshot.KeepAliveGuidance;
        FallbackInstallerText.Text = snapshot.FallbackInstallerNote;
        ProbeText.Text = BuildProbeText(snapshot);
        RenderCommands(snapshot.SetupCommands);
    }

    private void RenderCommands(IReadOnlyList<LocalBrainSetupCommand> commands)
    {
        CommandList.Items.Clear();
        foreach (var command in commands)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = command.Description,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB6C6CE")),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBox
            {
                Text = command.Command,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00111820")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAF7FB")),
                Margin = new Thickness(0, 2, 0, 0)
            });
            var button = new Button
            {
                Content = $"Copy command: {command.Label}",
                Tag = command.Id,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAF7FB")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#253847")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7DA4B8")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 4, 0, 0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += CopyCommandButton_OnClick;
            panel.Children.Add(button);
            CommandList.Items.Add(panel);
        }
    }

    private void CopyCommandButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string commandId })
        {
            return;
        }

        var result = _service.CopyCommand(commandId, DateTimeOffset.UtcNow);
        if (result.Success)
        {
            Clipboard.SetText(result.Command);
        }

        RefreshMessageText.Text = result.Message;
    }

    private static string BuildProbeText(LocalBrainStatusPanelSnapshot snapshot)
    {
        var status = snapshot.Status;
        var probe = snapshot.LastProbe;
        var lines = new List<string>
        {
            $"Last heartbeat: {status.LastProbeAtUtc:O}",
            $"Endpoint: {status.Endpoint}",
            $"Model: {status.Model}",
            $"Reason: {status.Reason}"
        };

        if (probe is not null)
        {
            lines.Add($"Last refresh: {(probe.IsAvailable ? "responded" : probe.WasDormant ? "dormant" : "timeout/unavailable")}");
            lines.Add($"Probe endpoint: {probe.Endpoint}");
            lines.Add($"Probe model: {probe.Model}");
        }
        else
        {
            lines.Add("Last refresh: not run from this panel yet.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
