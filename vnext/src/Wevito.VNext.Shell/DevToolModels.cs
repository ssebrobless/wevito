using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal enum DevToolCommandKind
{
    SelectPet,
    AddPet,
    RemovePet,
    RemoveAllPets,
    SpawnColorSet,
    ApplyAppearance,
    ApplyEnvironment,
    ApplyPreset,
    ApplyVitals,
    ApplyAnimation,
    ClearAnimation,
    SetCondition,
    ClearCondition
}

internal sealed record DevToolCommand(
    DevToolCommandKind Kind,
    Guid? PetId = null,
    string SpeciesId = "",
    string EnvironmentId = "",
    PetAgeStage? AgeStage = null,
    PetGender? Gender = null,
    string ColorVariant = "",
    string PresetId = "",
    double? Hunger = null,
    double? Thirst = null,
    double? Energy = null,
    double? Cleanliness = null,
    double? Affection = null,
    double? Comfort = null,
    double? Health = null,
    double? Fitness = null,
    double? BiologicalAgeMinutes = null,
    PetAnimationState? AnimationState = null,
    double? OverrideDurationSeconds = null,
    string ConditionId = "",
    int? ConditionSeverity = null);

internal sealed record DevPetOption(
    Guid Id,
    string Label)
{
    public override string ToString() => Label;
}

internal sealed record DevEnvironmentOption(
    string Id,
    string Label)
{
    public override string ToString() => Label;
}
