namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ExperimentDescriptor(
    ExperimentKind Kind,
    string DisplayName,
    string Description,
    bool EnabledByDefault = false);
