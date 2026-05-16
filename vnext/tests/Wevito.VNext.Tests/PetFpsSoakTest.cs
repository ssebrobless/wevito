using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetFpsSoakTest
{
    [Fact]
    public void LoopAt100PercentMaintains30FpsFloor()
    {
        var service = new PetFpsMonitorService();
        var start = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        for (var i = 0; i < 10 * 60 * 4; i++)
        {
            var fps = 60 - (i % 6);
            var observation = service.Observe(new PetFpsSample(fps, start.AddMilliseconds(250 * i)));
            Assert.False(observation.ViolationEmitted);
        }

        Assert.Equal(1.0, service.GetThrottleMultiplier(start.AddMinutes(10)));
    }
}
