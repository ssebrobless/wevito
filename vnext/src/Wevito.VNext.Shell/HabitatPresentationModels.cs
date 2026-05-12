using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed record HabitatDisplayItem(
    string Id,
    string Label,
    string CategoryFolder,
    string AssetId,
    string Purpose,
    string PreferenceHint = "",
    string ActionId = "",
    bool IsUrgent = false,
    bool IsSmallIconSafe = true,
    string VisualMappingId = "");

internal sealed record HabitatLoadout(
    IReadOnlyList<HabitatDisplayItem> RecommendedItems,
    IReadOnlyDictionary<string, HabitatDisplayItem> ActionItems,
    IReadOnlyDictionary<string, IReadOnlyList<HabitatDisplayItem>> ActionOptions,
    IReadOnlyList<StagePropSpec> DynamicStageProps);

internal sealed record StagePropSpec(
    string CategoryFolder,
    string AssetId,
    double Left,
    double Top,
    double Width,
    double Height,
    double Opacity = 1.0,
    DepthBand DepthBand = DepthBand.GroundContact,
    OcclusionMode OcclusionMode = OcclusionMode.None,
    ContactShadowMode ContactShadowMode = ContactShadowMode.Soft,
    string SlotId = "");

internal static class HabitatDepthOrder
{
    public static int GetZIndex(DepthBand depthBand)
    {
        return depthBand switch
        {
            DepthBand.Backdrop => 0,
            DepthBand.FarProp => 10,
            DepthBand.GroundContact => 20,
            DepthBand.PetShadow => 30,
            DepthBand.PetBody => 40,
            DepthBand.HeldOrCarriedProp => 50,
            DepthBand.NearOccluder => 60,
            DepthBand.UiOverlay => 70,
            _ => 20
        };
    }

    public static int GetShadowZIndex(ContactShadowMode contactShadowMode)
    {
        return contactShadowMode == ContactShadowMode.None
            ? GetZIndex(DepthBand.GroundContact)
            : GetZIndex(DepthBand.PetShadow);
    }
}
