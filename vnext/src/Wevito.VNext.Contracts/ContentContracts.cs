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
    bool IsPrimaryAction = true);

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

public sealed record GameContent(
    IReadOnlyList<SpeciesDefinition> Species,
    IReadOnlyList<ActionDefinition> Actions,
    IReadOnlyList<EnvironmentDefinition> Environments,
    IReadOnlyList<ToolDefinition> Tools,
    IReadOnlyList<NeedDefinition> Needs,
    IReadOnlyList<StatusDefinition> Statuses,
    IReadOnlyList<ItemDefinition> Items,
    IReadOnlyList<ConditionDefinition> Conditions);
