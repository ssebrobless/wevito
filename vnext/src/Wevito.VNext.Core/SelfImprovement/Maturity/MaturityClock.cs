namespace Wevito.VNext.Core.SelfImprovement.Maturity;

public sealed record MaturityClock(
    int ProgressIncrements,
    int DryRunCompletedCount,
    int ApplyCompletedCount,
    int RollbackVerifiedCount,
    int EvalCompletedPassedCount,
    IReadOnlyList<MaturityClockResetReason> ResetReasons,
    bool IsBlocked,
    string StatusMessage);
