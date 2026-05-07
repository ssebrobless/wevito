using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class HabitatDepthBandTests
{
    [Fact]
    public void DepthBandZOrder_FollowsHabitatPlacementContract()
    {
        var expectedOrder = new[]
        {
            DepthBand.Backdrop,
            DepthBand.FarProp,
            DepthBand.GroundContact,
            DepthBand.PetShadow,
            DepthBand.PetBody,
            DepthBand.HeldOrCarriedProp,
            DepthBand.NearOccluder,
            DepthBand.UiOverlay
        };

        var actual = expectedOrder.Select(HabitatDepthOrder.GetZIndex).ToArray();

        Assert.Equal(actual.Order().ToArray(), actual);
        Assert.Equal(actual.Length, actual.Distinct().Count());
    }

    [Fact]
    public void ContactShadows_RenderBelowPetBodies()
    {
        var shadowZIndex = HabitatDepthOrder.GetShadowZIndex(ContactShadowMode.Soft);

        Assert.True(shadowZIndex > HabitatDepthOrder.GetZIndex(DepthBand.GroundContact));
        Assert.True(shadowZIndex < HabitatDepthOrder.GetZIndex(DepthBand.PetBody));
    }
}
