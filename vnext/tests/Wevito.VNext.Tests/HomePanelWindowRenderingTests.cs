using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class HomePanelWindowRenderingTests
{
    [Fact]
    public void ShouldRenderPetInHomePanel_UsesSingleOwnerOutsidePassiveMode()
    {
        var pet = new PetActor(Guid.NewGuid(), "Fox 1", "fox", ActiveStatuses: []);

        Assert.True(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Focused, pet));
        Assert.True(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Pinned, pet));
    }

    [Fact]
    public void ShouldRenderPetInHomePanel_DisablesHomeCopyInPassiveMode()
    {
        var pet = new PetActor(Guid.NewGuid(), "Fox 1", "fox", ActiveStatuses: []);

        Assert.False(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Passive, pet));
    }

    [Fact]
    public void ShouldRenderPetInHomePanel_ExcludesDeadPetsFromHabitat()
    {
        var ghost = new PetActor(Guid.NewGuid(), "Fox 1", "fox", IsDead: true, IsGhost: true, ActiveStatuses: []);

        Assert.False(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Focused, ghost));
        Assert.False(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Pinned, ghost));
    }

    [Fact]
    public void ResolveCalmLineupScale_NormalizesLargeSpritesWithoutUpscalingTinySprites()
    {
        Assert.Equal(2.0, HomePanelWindow.ResolveCalmLineupScale(spriteHeight: 72, baseScale: 3.0));
        Assert.Equal(2.7, HomePanelWindow.ResolveCalmLineupScale(spriteHeight: 40, baseScale: 2.0));
    }

    [Fact]
    public void ResolveCalmLineupPlacement_SpreadsThreePetsSideBySideOnOneBaseline()
    {
        var stage = new RectInt(0, 0, 612, 330);

        var first = HomePanelWindow.ResolveCalmLineupPlacement(index: 0, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);
        var second = HomePanelWindow.ResolveCalmLineupPlacement(index: 1, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);
        var third = HomePanelWindow.ResolveCalmLineupPlacement(index: 2, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);

        Assert.True(first.X < second.X);
        Assert.True(second.X < third.X);
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(second.Y, third.Y);
    }
}
