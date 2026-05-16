using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CursorReactivityServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void RateLimitedToOncePer10Seconds()
    {
        var service = new CursorReactivityService();
        var first = service.Evaluate(new CursorReactivityRequest("pet-1", 100, 100, 140, 100, Now));
        var second = service.Evaluate(new CursorReactivityRequest("pet-1", 100, 100, 140, 100, Now.AddSeconds(9)));
        var third = service.Evaluate(new CursorReactivityRequest("pet-1", 100, 100, 140, 100, Now.AddSeconds(10)));

        Assert.True(first.Triggered);
        Assert.False(second.Triggered);
        Assert.Equal("cursor_reactivity_rate_limited", second.Reason);
        Assert.True(third.Triggered);
    }

    [Fact]
    public void OnlyTriggersWithinProximity()
    {
        var service = new CursorReactivityService();

        var decision = service.Evaluate(new CursorReactivityRequest("pet-1", 100, 100, 400, 100, Now));

        Assert.False(decision.Triggered);
        Assert.Equal("cursor_outside_proximity", decision.Reason);
    }
}
