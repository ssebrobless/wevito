namespace Wevito.VNext.Shell;

public sealed record StarterEggOption(
    string ColorVariant,
    string Label,
    string HexColor,
    string SpeciesId,
    bool IsEnabled = true,
    string DisabledReason = "");

public static class StarterEggCatalog
{
    public static IReadOnlyList<StarterEggOption> Eggs { get; } =
    [
        new("red", "Red egg", "#D94A42", "fox"),
        new("orange", "Orange egg", "#E98635", "squirrel"),
        new("yellow", "Yellow egg", "#E8C84E", "goose"),
        new("green", "Green egg", "#62B45D", "deer", false, "Green runtime sprites are not installed yet."),
        new("blue", "Blue egg", "#4C8FE8", "frog"),
        new("indigo", "Indigo egg", "#4B5BC8", "crow"),
        new("violet", "Violet egg", "#8C58D8", "raccoon")
    ];

    public static StarterEggOption? Resolve(string colorVariant)
    {
        return Eggs.FirstOrDefault(egg => string.Equals(egg.ColorVariant, colorVariant, StringComparison.OrdinalIgnoreCase));
    }
}
