using System.Diagnostics;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ProcessPriorityManagerServiceTests
{
    [Fact]
    public void AllWevitoProcessesSetToBelowNormalOnStart()
    {
        var applied = new List<ProcessPriorityClass>();
        var target = Target(ProcessPriorityClass.Normal, applied.Add);
        var service = new ProcessPriorityManagerService();

        var result = service.ApplyBelowNormal(target);

        Assert.True(result.Applied, result.Reason);
        Assert.Equal(ProcessPriorityClass.BelowNormal, result.EffectivePriority);
        Assert.Equal([ProcessPriorityClass.BelowNormal], applied);
    }

    [Theory]
    [InlineData(ProcessPriorityClass.AboveNormal)]
    [InlineData(ProcessPriorityClass.High)]
    [InlineData(ProcessPriorityClass.RealTime)]
    public void RefusesAboveNormalAndHigher(ProcessPriorityClass requested)
    {
        var result = ProcessPriorityManagerService.RefuseUnsafePriority(Target(ProcessPriorityClass.Normal, _ => { }), requested);

        Assert.False(result.Applied);
        Assert.Contains("above Normal", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoostDecaysAfter5Seconds()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var applied = new List<ProcessPriorityClass>();
        var target = Target(ProcessPriorityClass.BelowNormal, applied.Add);
        var service = new ProcessPriorityManagerService(clock: () => now);

        service.BoostForForeground(target, TimeSpan.FromSeconds(5));
        now = now.AddSeconds(6);
        var decayed = service.DecayExpiredBoosts([target]);

        Assert.Equal([ProcessPriorityClass.Normal, ProcessPriorityClass.BelowNormal], applied);
        Assert.Single(decayed);
        Assert.Equal(ProcessPriorityClass.BelowNormal, decayed[0].EffectivePriority);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        var applied = new List<ProcessPriorityClass>();
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new ProcessPriorityManagerService(killSwitchService: killSwitch);

        var result = service.ApplyBelowNormal(Target(ProcessPriorityClass.Normal, applied.Add));

        Assert.False(result.Applied);
        Assert.Empty(applied);
        Assert.Contains("kill_switch", result.Reason);
    }

    private static ProcessPriorityTarget Target(ProcessPriorityClass current, Action<ProcessPriorityClass> setter)
    {
        return new ProcessPriorityTarget(100, "wevito-test", current, setter);
    }
}
