namespace Wevito.VNext.Core.SelfImprovement;

[method: System.Text.Json.Serialization.JsonConstructor]
public sealed record ApplyRunnerStatusReport(
    string ReportId,
    bool ApplyRunnerImplemented,
    IReadOnlyList<string> OutstandingPrerequisites,
    string SourceConstant,
    DateTimeOffset GeneratedAtUtc)
{
    public ApplyRunnerStatusReport(KillSwitchService killSwitchService)
        : this("", false, Array.Empty<string>(), "", DateTimeOffset.MinValue)
    {
        _ = killSwitchService;
    }
}
