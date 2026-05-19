using Wevito.VNext.Core.SelfImprovement.Maturity;

namespace Wevito.VNext.Core.SelfImprovement.Invariants;

public sealed record InvariantCheck(
    string Id,
    string Description,
    MaturityClockResetReason Reason);
