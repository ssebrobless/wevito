using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal static class DevControlSnapshotBuilder
{
    private static readonly string[] DefaultLifeStages = ["baby", "teen", "adult", "senior"];
    private static readonly string[] DefaultGenders = ["female", "male"];
    private static readonly string[] DefaultColors = ["red", "orange", "yellow", "green", "blue", "indigo", "violet"];

    public static DevControlSnapshot Build(
        IReadOnlyList<PetActor> pets,
        GameContent content,
        DateTimeOffset now)
    {
        var slots = Enumerable.Range(0, 3)
            .Select(index => index < pets.Count ? FromPet(index, pets[index]) : DevControlPetSlotSnapshot.Empty(index))
            .ToList();

        return new DevControlSnapshot(slots, BuildOptions(content), now);
    }

    public static bool TryResolveSlot(
        IReadOnlyList<PetActor> pets,
        int slotIndex,
        Guid? expectedPetId,
        out PetActor? pet,
        out string message)
    {
        pet = null;
        if (slotIndex < 0 || slotIndex > 2)
        {
            message = "Slot must be 1, 2, or 3.";
            return false;
        }

        if (slotIndex >= pets.Count)
        {
            message = "Selected slot is empty.";
            return false;
        }

        pet = pets[slotIndex];
        if (expectedPetId is not null && pet.Id != expectedPetId.Value)
        {
            message = "Selected pet changed; refresh the controller and try again.";
            pet = null;
            return false;
        }

        message = "";
        return true;
    }

    private static DevControlPetSlotSnapshot FromPet(int slotIndex, PetActor pet)
    {
        var gender = pet.Gender.ToString().ToLowerInvariant();
        var lifeStage = pet.AgeStage.ToString().ToLowerInvariant();
        var behavior = pet.BehaviorState.ToString();
        var animation = pet.CurrentAnimationState.ToString();
        var actionVisualFamily = pet.CurrentActionVisualIntent?.Family.ToString();
        var display = $"{pet.Name}\n{pet.SpeciesId} | {gender} | {pet.ColorVariant} | {lifeStage}";

        return new DevControlPetSlotSnapshot(
            slotIndex,
            pet.Id,
            display,
            pet.Name,
            pet.SpeciesId,
            gender,
            pet.ColorVariant,
            lifeStage,
            false,
            pet.IsDead,
            behavior,
            animation,
            actionVisualFamily,
            string.IsNullOrWhiteSpace(pet.LastActionId) ? null : pet.LastActionId);
    }

    private static DevControlOptions BuildOptions(GameContent content)
    {
        var speciesIds = content.Species
            .Select(species => species.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lifeStages = content.Species
            .SelectMany(species => species.SupportedAgeStages ?? Enum.GetValues<PetAgeStage>())
            .Select(stage => stage.ToString().ToLowerInvariant())
            .Concat(DefaultLifeStages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(StageSort)
            .ThenBy(stage => stage, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var genders = content.Species
            .SelectMany(species => species.SupportedGenders ?? Enum.GetValues<PetGender>())
            .Select(gender => gender.ToString().ToLowerInvariant())
            .Concat(DefaultGenders)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GenderSort)
            .ThenBy(gender => gender, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var colors = content.Species
            .SelectMany(species => species.SupportedColors ?? [])
            .Concat(DefaultColors)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ColorSort)
            .ThenBy(color => color, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var actions = content.Actions
            .Where(action => action.IsPrimaryAction)
            .Select(action => new DevControlActionOption(action.Id, action.DisplayName))
            .ToList();

        return new DevControlOptions(speciesIds, lifeStages, genders, colors, actions);
    }

    private static int StageSort(string stage)
    {
        return stage.ToLowerInvariant() switch
        {
            "baby" => 0,
            "teen" => 1,
            "adult" => 2,
            "senior" => 3,
            _ => 99
        };
    }

    private static int GenderSort(string gender)
    {
        return gender.ToLowerInvariant() switch
        {
            "female" => 0,
            "male" => 1,
            _ => 99
        };
    }

    private static int ColorSort(string color)
    {
        return color.ToLowerInvariant() switch
        {
            "red" => 0,
            "orange" => 1,
            "yellow" => 2,
            "green" => 3,
            "blue" => 4,
            "indigo" => 5,
            "violet" => 6,
            _ => 99
        };
    }
}
