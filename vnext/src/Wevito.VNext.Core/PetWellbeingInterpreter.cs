using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetWellbeingInterpreter
{
    public PetWellbeingSnapshot BuildSnapshot(PetActor pet)
    {
        var normalizedStatuses = pet.ActiveStatuses ?? [];
        var pressures = BuildNeedPressures(pet);
        var dominantNeed = pressures
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .First();
        var conditionPressure = CalculateConditionPressure(pet.ActiveConditions);
        var maxPressure = Math.Max(dominantNeed.Value, conditionPressure);
        var urgency = ResolveUrgency(maxPressure);
        var drive = ResolveDrive(dominantNeed.Key, conditionPressure);
        var emotion = ResolveEmotion(pet, urgency, drive, conditionPressure);
        var descriptors = BuildPersonalityDescriptors(pet.Personality);
        var conditionIds = (pet.ActiveConditions ?? [])
            .Where(condition => condition.Severity > 0)
            .OrderByDescending(condition => condition.Severity)
            .ThenBy(condition => condition.Id, StringComparer.OrdinalIgnoreCase)
            .Select(condition => condition.Id)
            .ToList();

        return new PetWellbeingSnapshot(
            pet.Id,
            pet.Name,
            pet.SpeciesId,
            pet.AgeStage,
            pet.Gender,
            pet.ColorVariant,
            urgency,
            drive,
            emotion,
            BuildSummary(pet, urgency, dominantNeed.Key, conditionIds),
            pressures,
            descriptors,
            conditionIds,
            normalizedStatuses);
    }

    public IReadOnlyList<PetWellbeingSnapshot> BuildSnapshots(IReadOnlyList<PetActor> pets)
    {
        return pets.Select(BuildSnapshot).ToList();
    }

    private static IReadOnlyDictionary<string, double> BuildNeedPressures(PetActor pet)
    {
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["hunger"] = ToPressure(pet.Hunger),
            ["thirst"] = ToPressure(pet.Thirst),
            ["energy"] = ToPressure(pet.Energy),
            ["cleanliness"] = ToPressure(pet.Cleanliness),
            ["affection"] = ToPressure(pet.Affection),
            ["comfort"] = ToPressure(pet.Comfort),
            ["health"] = ToPressure(pet.Health),
            ["fitness"] = ToPressure(pet.Fitness)
        };
    }

    private static double ToPressure(double value)
    {
        return Math.Clamp(100 - value, 0, 100);
    }

    private static double CalculateConditionPressure(IReadOnlyList<PetConditionRecord>? conditions)
    {
        if (conditions is null || conditions.Count == 0)
        {
            return 0;
        }

        return Math.Clamp(conditions.Sum(condition => Math.Max(0, condition.Severity) * (condition.IsInnate ? 12 : 16)), 0, 100);
    }

    private static PetWellbeingUrgency ResolveUrgency(double pressure)
    {
        return pressure switch
        {
            >= 76 => PetWellbeingUrgency.Critical,
            >= 52 => PetWellbeingUrgency.NeedsCare,
            >= 28 => PetWellbeingUrgency.Watch,
            _ => PetWellbeingUrgency.Stable
        };
    }

    private static PetDriveFamily ResolveDrive(string needKey, double conditionPressure)
    {
        if (conditionPressure >= 52 || string.Equals(needKey, "health", StringComparison.OrdinalIgnoreCase))
        {
            return PetDriveFamily.SafetyAvoidance;
        }

        return needKey.ToLowerInvariant() switch
        {
            "energy" => PetDriveFamily.Rest,
            "affection" or "comfort" => PetDriveFamily.SocialConnection,
            "fitness" => PetDriveFamily.Exploration,
            _ => PetDriveFamily.SelfMaintenance
        };
    }

    private static PetEmotionChannel ResolveEmotion(
        PetActor pet,
        PetWellbeingUrgency urgency,
        PetDriveFamily drive,
        double conditionPressure)
    {
        if (urgency == PetWellbeingUrgency.Critical || conditionPressure >= 76)
        {
            return PetEmotionChannel.Threat;
        }

        if (urgency == PetWellbeingUrgency.Stable)
        {
            return (pet.Personality?.Playfulness ?? 0) >= 25 && pet.Fitness >= 58 && pet.Energy >= 46
                ? PetEmotionChannel.Curiosity
                : PetEmotionChannel.Relief;
        }

        if (drive == PetDriveFamily.Rest)
        {
            return PetEmotionChannel.Exhaustion;
        }

        if (drive == PetDriveFamily.SocialConnection)
        {
            return PetEmotionChannel.Attachment;
        }

        if (urgency == PetWellbeingUrgency.NeedsCare)
        {
            return PetEmotionChannel.Agitation;
        }

        if ((pet.Personality?.Playfulness ?? 0) >= 25 && pet.Fitness >= 58 && pet.Energy >= 46)
        {
            return PetEmotionChannel.Curiosity;
        }

        return PetEmotionChannel.Relief;
    }

    private static IReadOnlyList<string> BuildPersonalityDescriptors(PetPersonalityProfile? personality)
    {
        if (personality is null)
        {
            return ["settling"];
        }

        var descriptors = new List<string>();
        AddTraitDescriptor(descriptors, personality.Playfulness, "playful", "reserved");
        AddTraitDescriptor(descriptors, personality.Cheerfulness, "bright", "moody");
        AddTraitDescriptor(descriptors, personality.SocialNeed, "social", "solitary");
        AddTraitDescriptor(descriptors, personality.CuddleNeed, "clingy", "independent");
        AddTraitDescriptor(descriptors, personality.CleanlinessPreference, "neat", "messy");
        AddTraitDescriptor(descriptors, personality.ActivityLevel, "active", "calm");
        AddTraitDescriptor(descriptors, personality.Stubbornness, "headstrong", "pliable");
        AddTraitDescriptor(descriptors, personality.FoodLove, "food-motivated", "picky");
        return descriptors.Count == 0 ? ["settling"] : descriptors.Take(4).ToList();
    }

    private static void AddTraitDescriptor(List<string> descriptors, double value, string positive, string negative)
    {
        if (value >= 25)
        {
            descriptors.Add(positive);
        }
        else if (value <= -25)
        {
            descriptors.Add(negative);
        }
    }

    private static string BuildSummary(
        PetActor pet,
        PetWellbeingUrgency urgency,
        string dominantNeed,
        IReadOnlyList<string> conditionIds)
    {
        if (conditionIds.Count > 0 && urgency is PetWellbeingUrgency.Critical or PetWellbeingUrgency.NeedsCare)
        {
            return $"{pet.Name} needs care for {conditionIds[0]} and {dominantNeed}.";
        }

        return urgency switch
        {
            PetWellbeingUrgency.Critical => $"{pet.Name} needs immediate help with {dominantNeed}.",
            PetWellbeingUrgency.NeedsCare => $"{pet.Name} needs care for {dominantNeed}.",
            PetWellbeingUrgency.Watch => $"{pet.Name} should be watched for {dominantNeed}.",
            _ => $"{pet.Name} is stable."
        };
    }
}
