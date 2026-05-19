namespace Wevito.VNext.Core.SelfImprovement;

public sealed record PrerequisiteEntry(string Name, bool Passed, string Detail);

public sealed record ApplyRunnerPrerequisiteCheckResult(
    string OperationId,
    IReadOnlyList<PrerequisiteEntry> Entries,
    bool AllPassed,
    DateTimeOffset CheckedAtUtc);
