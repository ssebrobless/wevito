using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FocusDisciplineServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void WindowShowsWithoutFocus()
    {
        var service = new FocusDisciplineService();

        var decision = service.Decide(new WindowShowRequest("Home", UserInitiated: false, IsFirstLaunchWizard: false), Now);

        Assert.False(decision.ShowActivated);
        Assert.True(decision.PreventedFocusSteal);
    }

    [Fact]
    public void UserClickOpensWithFocus()
    {
        var service = new FocusDisciplineService();

        var decision = service.Decide(new WindowShowRequest("Tools", UserInitiated: true, IsFirstLaunchWizard: false), Now);

        Assert.True(decision.ShowActivated);
        Assert.False(decision.PreventedFocusSteal);
    }

    [Fact]
    public void FirstLaunchWizardTakesFocus()
    {
        var service = new FocusDisciplineService();

        var decision = service.Decide(new WindowShowRequest("FirstLaunchWizard", UserInitiated: false, IsFirstLaunchWizard: true), Now);

        Assert.True(decision.ShowActivated);
        Assert.False(decision.PreventedFocusSteal);
    }
}
