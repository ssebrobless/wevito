namespace Wevito.VNext.Core;

public enum BetaGateDecisionLabel
{
    EnableLimitedAutonomy,
    KeepPreviewOnly,
    PauseForSafetyWork
}

public sealed record BetaGateCheckResult(
    string CheckId,
    bool Passed,
    bool IsSafetyCheck,
    string Detail);

public sealed record BetaGateDecision(
    string SchemaVersion,
    BetaGateDecisionLabel Decision,
    IReadOnlyList<BetaGateCheckResult> Checks,
    DateTimeOffset CreatedAtUtc,
    string Summary);
