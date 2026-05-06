namespace Wevito.VNext.Contracts;

public sealed record SpeciesDefinition(
    string Id,
    string DisplayName,
    string AccentColor,
    double BaseSpeed,
    string DefaultEnvironmentId = "",
    IReadOnlyList<PetAgeStage>? SupportedAgeStages = null,
    IReadOnlyList<PetGender>? SupportedGenders = null,
    IReadOnlyList<string>? SupportedColors = null,
    string PrimaryFoodGroupId = "",
    string SecondaryFoodGroupId = "",
    string InnateConditionId = "",
    PetPersonalityProfile? PersonalitySeed = null);

public sealed record ActionDefinition(
    string Id,
    string DisplayName,
    string EffectSummary,
    string IconId = "",
    PetAnimationState AnimationState = PetAnimationState.Idle,
    string FeedbackText = "",
    bool IsPrimaryAction = true,
    string? OptionalAnimationFamily = null,
    string? PropOverlay = null);

public sealed record EnvironmentDefinition(
    string Id,
    string DisplayName,
    string PrimaryColor,
    string SecondaryColor,
    string AssetId = "",
    bool IsNightEnvironment = false);

public sealed record ToolDefinition(
    string Id,
    string DisplayName,
    int Capacity);

public sealed record NeedDefinition(
    string Id,
    string DisplayName,
    string IconId,
    string AccentColor);

public sealed record StatusDefinition(
    string Id,
    string DisplayName,
    string IconId,
    string Description);

public sealed record ConditionDefinition(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    string TreatmentHint = "");

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string CategoryId,
    string IconId,
    IReadOnlyList<string>? SpeciesIds = null,
    IReadOnlyList<string>? EnvironmentIds = null);

public sealed record ItemVisualMapping(
    string Id,
    string DisplayName,
    string Category,
    string VisualAssetId,
    IReadOnlyList<string>? SpeciesIds = null,
    bool SmallIconSafe = true,
    bool HabitatObjectSafe = false,
    string Notes = "");

public enum DepthBand
{
    Backdrop,
    FarProp,
    GroundContact,
    PetShadow,
    PetBody,
    HeldOrCarriedProp,
    NearOccluder,
    UiOverlay
}

public enum OcclusionMode
{
    None,
    BodyOnly,
    FullPet
}

public enum ContactShadowMode
{
    None,
    Soft,
    Hard
}

public enum AgeScalePolicy
{
    Constant,
    PerStage
}

public sealed record HabitatRect(
    double Left,
    double Top,
    double Width,
    double Height);

public sealed record HabitatObjectSlot(
    string SlotId,
    string Role,
    string AssetId,
    HabitatRect DefaultRect,
    DepthBand DepthBand,
    OcclusionMode OcclusionMode,
    ContactShadowMode ContactShadowMode,
    AgeScalePolicy AgeScalePolicy,
    int PriorityTier = 0,
    string Notes = "");

public sealed record HabitatLoadoutDefinition(
    string SpeciesId,
    string EnvironmentId,
    IReadOnlyList<HabitatObjectSlot> Slots);

public sealed record GameContent(
    IReadOnlyList<SpeciesDefinition> Species,
    IReadOnlyList<ActionDefinition> Actions,
    IReadOnlyList<EnvironmentDefinition> Environments,
    IReadOnlyList<ToolDefinition> Tools,
    IReadOnlyList<NeedDefinition> Needs,
    IReadOnlyList<StatusDefinition> Statuses,
    IReadOnlyList<ItemDefinition> Items,
    IReadOnlyList<ConditionDefinition> Conditions,
    IReadOnlyList<ItemVisualMapping>? ItemVisualMappings = null,
    IReadOnlyList<HabitatLoadoutDefinition>? HabitatLoadouts = null);
