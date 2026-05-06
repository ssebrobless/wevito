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
    bool IsUrgent = false);

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
    ContactShadowMode ContactShadowMode = ContactShadowMode.Soft);
