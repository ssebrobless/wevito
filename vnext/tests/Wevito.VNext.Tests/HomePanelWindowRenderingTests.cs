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
}
