using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ShellPresentationRulesTests
{
    [Fact]
    public void ShouldShowMainPanel_HidesOnlyInPassiveMode()
    {
        Assert.True(ShellPresentationRules.ShouldShowMainPanel(CompanionMode.Focused));
        Assert.True(ShellPresentationRules.ShouldShowMainPanel(CompanionMode.Pinned));
        Assert.False(ShellPresentationRules.ShouldShowMainPanel(CompanionMode.Passive));
    }

    [Fact]
    public void ResolveDesktopAssetOpacity_DimsWhenAnotherWindowOwnsForeground()
    {
        var context = BuildDesktopContext(hwnd: 22, isShellSurface: false);

        var opacity = ShellPresentationRules.ResolveDesktopAssetOpacity(CompanionMode.Pinned, context, [11]);

        Assert.Equal(ShellPresentationRules.BackgroundAssetOpacity, opacity);
    }

    [Fact]
    public void ResolveDesktopAssetOpacity_ReturnsActiveWhenWevitoOwnsForeground()
    {
        var context = BuildDesktopContext(hwnd: 11, isShellSurface: false);

        var opacity = ShellPresentationRules.ResolveDesktopAssetOpacity(CompanionMode.Focused, context, [11]);

        Assert.Equal(ShellPresentationRules.ActiveAssetOpacity, opacity);
    }

    [Fact]
    public void ResolvePetTopInBand_UsesPetScreenYAndClampsInsideBand()
    {
        Assert.Equal(54, RoamBandWindow.ResolvePetTopInBand(petScreenY: 990, windowTop: 900, height: 36, actualHeight: 118));
        Assert.Equal(0, RoamBandWindow.ResolvePetTopInBand(petScreenY: 880, windowTop: 900, height: 36, actualHeight: 118));
        Assert.Equal(74, RoamBandWindow.ResolvePetTopInBand(petScreenY: 1400, windowTop: 900, height: 36, actualHeight: 118));
    }

    [Fact]
    public void ResolveRoamMotionBounds_AlignsPetFeetToTaskbarTopInsteadOfScreenBottom()
    {
        var monitor = new RectInt(0, 0, 1920, 1080);
        var workArea = new RectInt(0, 0, 1920, 1040);
        var band = new RectInt(0, 962, 1920, 118);

        var motion = ShellPresentationRules.ResolveRoamMotionBounds(band, workArea);

        Assert.Equal(1050, motion.Bottom);
        Assert.Equal(88, motion.Height);
    }

    [Theory]
    [InlineData("actions", true, true)]
    [InlineData("action:feed", true, true)]
    [InlineData("settings", true, false)]
    [InlineData("actions", false, false)]
    public void IsActionsSurfaceOpen_IdentifiesInteractiveCareSurfaces(string toolId, bool isOpen, bool expected)
    {
        Assert.Equal(expected, ShellPresentationRules.IsActionsSurfaceOpen(new ToolSession(toolId, isOpen)));
    }

    [Fact]
    public void ShouldRenderPetInRoamBand_KeepsGhostsVisibleOutsideFocusHabitat()
    {
        var living = new PetActor(Guid.NewGuid(), "Fox 1", "fox", ActiveStatuses: []);
        var ghost = living with { IsDead = true, IsGhost = true };

        Assert.False(RoamBandWindow.ShouldRenderPetInRoamBand(CompanionMode.Focused, living));
        Assert.True(RoamBandWindow.ShouldRenderPetInRoamBand(CompanionMode.Focused, ghost));
        Assert.True(RoamBandWindow.ShouldRenderPetInRoamBand(CompanionMode.Passive, living));
    }

    private static DesktopContext BuildDesktopContext(long hwnd, bool isShellSurface)
    {
        return new DesktopContext(
            new ForegroundWindowInfo(999, hwnd, "example", "Example", "Example", isShellSurface, false),
            new RectInt(0, 0, 1920, 1040),
            new RectInt(0, 0, 1920, 1080),
            new PointInt(100, 100),
            DateTimeOffset.UtcNow);
    }
}
