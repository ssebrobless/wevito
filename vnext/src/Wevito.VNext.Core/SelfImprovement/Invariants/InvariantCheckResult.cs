namespace Wevito.VNext.Core.SelfImprovement.Invariants;

public sealed record InvariantCheckResult(
    InvariantCheck Check,
    bool Triggered,
    string EvidenceSummary);
