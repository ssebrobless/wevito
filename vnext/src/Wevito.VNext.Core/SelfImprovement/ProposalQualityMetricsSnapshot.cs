namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ProposalQualityMetricsSnapshot(
    string OperationId,
    IReadOnlyDictionary<string, int> PacketCountsByKind,
    bool LatestScopeHashFormatValid,
    int? JudgeRulesEvaluated,
    int? JudgeRulesPassed,
    double? SnapshotAgeDays,
    string LatestReplayResultKind,
    IReadOnlyList<string> LatestEvalGatesPresent,
    IReadOnlyList<string> LatestEvalGatesMissing,
    string LatestAwaitingApprovalStatus,
    int ApplyRefusedNotImplementedCount,
    DateTimeOffset GeneratedAtUtc,
    string Reason)
{
    public ProposalQualityMetricsSnapshot(KillSwitchService killSwitchService)
        : this("", new Dictionary<string, int>(StringComparer.Ordinal), false, null, null, null,
            "none", Array.Empty<string>(), Array.Empty<string>(), "", 0, DateTimeOffset.MinValue, "")
    {
        _ = killSwitchService;
    }
}
