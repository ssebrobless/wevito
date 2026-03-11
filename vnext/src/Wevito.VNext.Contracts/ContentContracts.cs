namespace Wevito.VNext.Contracts;

public sealed record SpeciesDefinition(
    string Id,
    string DisplayName,
    string AccentColor,
    double BaseSpeed);

public sealed record ActionDefinition(
    string Id,
    string DisplayName,
    string EffectSummary);

public sealed record EnvironmentDefinition(
    string Id,
    string DisplayName,
    string PrimaryColor,
    string SecondaryColor);

public sealed record ToolDefinition(
    string Id,
    string DisplayName,
    int Capacity);

public sealed record GameContent(
    IReadOnlyList<SpeciesDefinition> Species,
    IReadOnlyList<ActionDefinition> Actions,
    IReadOnlyList<EnvironmentDefinition> Environments,
    IReadOnlyList<ToolDefinition> Tools);
