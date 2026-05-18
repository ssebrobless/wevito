namespace Wevito.VNext.Core.SelfImprovement.Maturity;

public enum MaturityClockResetReason
{
    InvariantViolation,
    SilentMutationDetected,
    SilentNetworkDetected,
    SilentHostedAiDetected,
    FailedRollback,
    UserRejectedProposal
}
