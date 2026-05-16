using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetStateContextInjector
{
    public const string AiMentionsPetStateSetting = "ai_mentions_pet_state";
    public static readonly TimeSpan DefaultFreshnessWindow = TimeSpan.FromMinutes(5);

    private readonly TimeSpan _freshnessWindow;

    public PetStateContextInjector(TimeSpan? freshnessWindow = null)
    {
        _freshnessWindow = freshnessWindow ?? DefaultFreshnessWindow;
    }

    public IReadOnlyList<string> BuildContextLines(
        IReadOnlyList<PetActor> pets,
        IReadOnlyDictionary<string, string>? settings,
        DateTimeOffset snapshotCapturedAtUtc,
        DateTimeOffset nowUtc)
    {
        if (!IsEnabled(settings) || nowUtc - snapshotCapturedAtUtc > _freshnessWindow)
        {
            return [];
        }

        var lines = new List<string>();
        foreach (var pet in pets.Where(pet => !pet.IsDead))
        {
            var alert = BuildAlert(pet);
            if (!string.IsNullOrWhiteSpace(alert))
            {
                lines.Add(alert);
            }
        }

        return lines;
    }

    private static bool IsEnabled(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is null || !settings.TryGetValue(AiMentionsPetStateSetting, out var raw))
        {
            return true;
        }

        return !bool.TryParse(raw, out var enabled) || enabled;
    }

    private static string BuildAlert(PetActor pet)
    {
        if (pet.Health < 30)
        {
            return $"{pet.Name}'s health is at {pet.Health:0}% - you can mention this if the user asks about pets.";
        }

        if (pet.Thirst < 12)
        {
            return $"{pet.Name}'s thirst meter is critically low at {pet.Thirst:0}% - you can mention water if the user asks about pets.";
        }

        if (pet.Hunger < 12)
        {
            return $"{pet.Name}'s hunger meter is critically low at {pet.Hunger:0}% - you can mention feeding if the user asks about pets.";
        }

        if (pet.Energy < 10)
        {
            return $"{pet.Name}'s energy is at {pet.Energy:0}% - you can mention rest if the user asks about pets.";
        }

        return "";
    }
}
