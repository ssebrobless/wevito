using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class MultiMonitorServiceTests
{
    [Fact]
    public void PetStaysOnPreferredMonitor()
    {
        var service = new MultiMonitorService();
        var settings = new Dictionary<string, string>
        {
            [MultiMonitorService.PreferredMonitorSetting] = "monitor-2"
        };
        var monitors = new[]
        {
            new MonitorDescriptor("monitor-1", IsPrimary: true),
            new MonitorDescriptor("monitor-2", IsPrimary: false)
        };

        var resolution = service.ResolvePreferredMonitor(settings, monitors);

        Assert.Equal("monitor-2", resolution.MonitorId);
        Assert.False(resolution.FellBackToPrimary);
    }

    [Fact]
    public void FallsBackToPrimaryWhenMonitorDisconnected()
    {
        var service = new MultiMonitorService();
        var settings = new Dictionary<string, string>
        {
            [MultiMonitorService.PreferredMonitorSetting] = "missing-monitor"
        };
        var monitors = new[]
        {
            new MonitorDescriptor("monitor-1", IsPrimary: true),
            new MonitorDescriptor("monitor-2", IsPrimary: false)
        };

        var resolution = service.ResolvePreferredMonitor(settings, monitors);

        Assert.Equal("monitor-1", resolution.MonitorId);
        Assert.True(resolution.FellBackToPrimary);
    }
}
