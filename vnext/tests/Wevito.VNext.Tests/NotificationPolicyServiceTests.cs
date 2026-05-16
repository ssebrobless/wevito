using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class NotificationPolicyServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void DefersWhenUserTyping()
    {
        var service = new NotificationPolicyService();

        var decision = service.Submit(Request(), Context(userTyping: true));

        Assert.False(decision.DeliverNow);
        Assert.True(decision.Deferred);
        Assert.False(decision.StealsFocus);
        Assert.Single(service.QueuedNotifications);
    }

    [Fact]
    public void DefersDuringFullscreen()
    {
        var service = new NotificationPolicyService();

        var decision = service.Submit(Request(), Context(fullscreen: true));

        Assert.False(decision.DeliverNow);
        Assert.True(decision.Deferred);
        Assert.False(decision.StealsFocus);
    }

    [Fact]
    public void DeliversOnNextIdleMoment()
    {
        var service = new NotificationPolicyService();
        service.Submit(Request(), Context(userTyping: true));

        var decisions = service.DeliverReady(Context());

        Assert.Single(decisions);
        Assert.True(decisions[0].DeliverNow);
        Assert.Empty(service.QueuedNotifications);
    }

    [Fact]
    public void EmergencyBypassesQueue()
    {
        var service = new NotificationPolicyService();

        var decision = service.Submit(Request(isEmergency: true), Context(userTyping: true, fullscreen: true));

        Assert.True(decision.DeliverNow);
        Assert.False(decision.Deferred);
        Assert.Empty(service.QueuedNotifications);
    }

    [Fact]
    public void EmergencyDoesNotStealFocus()
    {
        var service = new NotificationPolicyService();

        var decision = service.Submit(Request(isEmergency: true), Context(userTyping: true));

        Assert.True(decision.DeliverNow);
        Assert.False(decision.StealsFocus);
    }

    private static NotificationRequest Request(bool isEmergency = false)
    {
        return new NotificationRequest("notice-1", "helper", isEmergency, "Helper finished.");
    }

    private static NotificationContext Context(
        bool userTyping = false,
        bool fullscreen = false,
        bool coexistence = false,
        bool dnd = false)
    {
        return new NotificationContext(userTyping, fullscreen, coexistence, dnd, Now);
    }
}
