namespace Wevito.VNext.Core.SelfImprovement;

public sealed record UserApplyApproval(
    bool UserConfirmedInThisMessage,
    string ConfirmationText,
    DateTimeOffset ConfirmedAtUtc,
    string ApprovedScopeId,
    string ApprovedOperationId,
    string ApprovedScopeHash);
