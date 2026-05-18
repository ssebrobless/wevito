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
        Assert.Equal(2.0, HomePanelWindow.ResolveCalmLineupScale(spriteHeight: 40, baseScale: 2.0));
    }

    [Fact]
    public void ResolveCalmLineupPlacement_SpreadsThreePetsSideBySideOnOneBaseline()
    {
        var stage = new RectInt(35, 460, 612, 330);

        var first = HomePanelWindow.ResolveCalmLineupPlacement(index: 0, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);
        var second = HomePanelWindow.ResolveCalmLineupPlacement(index: 1, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);
        var third = HomePanelWindow.ResolveCalmLineupPlacement(index: 2, livingPetCount: 3, stage, spriteWidth: 80, spriteHeight: 96);

        Assert.True(first.X < second.X);
        Assert.True(second.X < third.X);
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(second.Y, third.Y);
        Assert.InRange(first.X, 0, stage.Width);
        Assert.InRange(third.X, 0, stage.Width);
        Assert.InRange(first.Y, 0, stage.Height);
    }

    [Fact]
    public void ShouldUseCalmLineup_ImmediatelyPlacesLivingPetsRegardlessOfMotionState()
    {
        var settled = new PetActor(Guid.NewGuid(), "Fox 1", "fox", BehaviorState: PetBehaviorState.Home, ActiveStatuses: []);
        var returning = settled with { BehaviorState = PetBehaviorState.Recalling };
        var roaming = settled with { BehaviorState = PetBehaviorState.Roaming };
        var dead = settled with { IsDead = true, IsGhost = true };

        Assert.True(HomePanelWindow.ShouldUseCalmLineupPlacement(calmLineup: true, settled));
        Assert.True(HomePanelWindow.ShouldUseCalmLineupPlacement(calmLineup: true, returning));
        Assert.True(HomePanelWindow.ShouldUseCalmLineupPlacement(calmLineup: true, roaming));
        Assert.False(HomePanelWindow.ShouldUseCalmLineupPlacement(calmLineup: true, dead));
        Assert.False(HomePanelWindow.ShouldUseCalmLineupPlacement(calmLineup: false, settled));
    }

    [Fact]
    public void ComputeStarterEggPromptLayout_KeepsSixEggGridInsideStage()
    {
        var stage = new RectInt(25, 440, 360, 220);

        var layout = HomePanelWindow.ComputeStarterEggPromptLayout(stage, eggCount: 6);

        Assert.True(layout.Width <= stage.Width - 16);
        Assert.True(layout.MaxHeight <= stage.Height - 16);
        Assert.True(layout.Top >= 8);
        Assert.True(layout.Top + layout.MaxHeight <= stage.Height - 8);
        Assert.True(layout.Left >= 0);
        Assert.True(layout.Left + layout.Width <= stage.Width);
        Assert.InRange(layout.Columns, 2, 3);
    }

    [Fact]
    public void ComputeStarterEggPromptLayout_UsesCompactColumnsForSixEggs()
    {
        var stage = new RectInt(25, 440, 612, 300);

        var layout = HomePanelWindow.ComputeStarterEggPromptLayout(stage, eggCount: 6);

        Assert.Equal(3, layout.Columns);
        Assert.True(layout.Width <= 460);
        Assert.True(layout.Top + layout.MaxHeight <= stage.Height - 8);
    }
}
