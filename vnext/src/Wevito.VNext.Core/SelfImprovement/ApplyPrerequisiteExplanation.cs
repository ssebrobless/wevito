namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ApplyPrerequisiteExplanationEntry(
    string Name,
    bool Passed,
    string Detail,
    string PlainLanguage)
{
    public ApplyPrerequisiteExplanationEntry(KillSwitchService killSwitchService)
        : this("", false, "", "")
    {
        _ = killSwitchService;
    }
}

public sealed record ApplyPrerequisiteExplanation(
    string OperationId,
    IReadOnlyList<ApplyPrerequisiteExplanationEntry> Entries,
    bool AllPassed,
    DateTimeOffset GeneratedAtUtc,
    string Reason)
{
    public ApplyPrerequisiteExplanation(KillSwitchService killSwitchService)
        : this("", Array.Empty<ApplyPrerequisiteExplanationEntry>(), false, DateTimeOffset.MinValue, "")
    {
        _ = killSwitchService;
    }
}
