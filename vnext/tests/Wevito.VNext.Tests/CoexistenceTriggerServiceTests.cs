using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CoexistenceTriggerServiceTests
{
    [Fact]
    public void AppListMatchTriggersQuietMode()
    {
        var service = new CoexistenceTriggerService();
        var result = service.Evaluate(
            new Dictionary<string, string>(),
            Context("zoom.exe", fullscreen: false),
            new CoexistenceResourceSnapshot(),
            Now);

        Assert.True(result.IsQuieting);
        Assert.Contains("app_list", result.ActiveTriggers);
    }

    [Fact]
    public void FullscreenAppTriggers()
    {
        var service = new CoexistenceTriggerService();
        var result = service.Evaluate(
            new Dictionary<string, string>(),
            Context("game.exe", fullscreen: true),
            new CoexistenceResourceSnapshot(),
            Now);

        Assert.True(result.IsQuieting);
        Assert.Contains("fullscreen", result.ActiveTriggers);
    }

    [Fact]
    public void CpuPressureTriggers()
    {
        var service = new CoexistenceTriggerService();
        var result = service.Evaluate(
            new Dictionary<string, string>(),
            Context("notepad.exe", fullscreen: false),
            new CoexistenceResourceSnapshot(NonWevitoCpuPercent: 81),
            Now);

        Assert.True(result.IsQuieting);
        Assert.Contains("cpu_pressure", result.ActiveTriggers);
    }

    [Fact]
    public void TieredResumeOnClear()
    {
        var service = new CoexistenceTriggerService();
        service.Evaluate(new Dictionary<string, string>(), Context("zoom.exe", fullscreen: false), new CoexistenceResourceSnapshot(), Now);

        var justCleared = service.Evaluate(new Dictionary<string, string>(), Context("notepad.exe", fullscreen: false), new CoexistenceResourceSnapshot(), Now.AddSeconds(1));
        var maintenanceReady = service.Evaluate(new Dictionary<string, string>(), Context("notepad.exe", fullscreen: false), new CoexistenceResourceSnapshot(), Now.AddMinutes(2));
        var experimentationReady = service.Evaluate(new Dictionary<string, string>(), Context("notepad.exe", fullscreen: false), new CoexistenceResourceSnapshot(), Now.AddMinutes(6));

        Assert.False(justCleared.MaintenanceCanResume);
        Assert.True(maintenanceReady.MaintenanceCanResume);
        Assert.False(maintenanceReady.ExperimentationCanResume);
        Assert.True(experimentationReady.ExperimentationCanResume);
    }

    [Fact]
    public void KillSwitchSuppressesTriggerEvidenceWrites()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-coexistence-tests", Guid.NewGuid().ToString("N"));
        var jsonl = Path.Combine(root, "coexistence-events.jsonl");
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new CoexistenceTriggerService(killSwitchService: killSwitch, jsonlPath: jsonl);

        service.Evaluate(new Dictionary<string, string>(), Context("zoom.exe", fullscreen: false), new CoexistenceResourceSnapshot(), Now);

        Assert.False(File.Exists(jsonl));
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    private static DesktopContext Context(string processName, bool fullscreen)
    {
        return new DesktopContext(
            new ForegroundWindowInfo(1, 2, processName, "Foreground", "Window", IsShellSurface: false, IsFullscreenApp: fullscreen),
            new RectInt(0, 0, 1920, 1040),
            new RectInt(0, 0, 1920, 1080),
            new PointInt(0, 0),
            Now);
    }
}

