namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed record SupervisedScoringDryRunResult(
    string OperationId,
    string ResultKind,
    string Reason,
    string ModelIdentity,
    DateTimeOffset RanAtUtc)
{
    public SupervisedScoringDryRunResult(KillSwitchService killSwitchService)
        : this("", "Refused", "", "", DateTimeOffset.MinValue)
    {
        _ = killSwitchService;
    }
}
