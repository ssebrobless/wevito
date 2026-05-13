using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class HomePanelWindowRenderingTests
{
    [Fact]
    public void ShouldRenderPetInHomePanel_UsesSingleOwnerOutsidePassiveMode()
    {
        Assert.True(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Focused));
        Assert.True(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Pinned));
    }

    [Fact]
    public void ShouldRenderPetInHomePanel_DisablesHomeCopyInPassiveMode()
    {
        Assert.False(HomePanelWindow.ShouldRenderPetInHomePanel(CompanionMode.Passive));
    }
}
