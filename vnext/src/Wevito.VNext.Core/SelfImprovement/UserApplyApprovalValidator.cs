namespace Wevito.VNext.Core.SelfImprovement;

public sealed class UserApplyApprovalValidator : IRequiresUserApplyApproval
{
    private static readonly TimeSpan MaximumApprovalAge = TimeSpan.FromSeconds(60);

    public ApprovalResult ValidateUserApplyApproval(
        UserApplyApproval? approval,
        string expectedScopeId,
        string expectedOperationId,
        DateTimeOffset nowUtc)
    {
        if (approval is null)
        {
            return new ApprovalResult.Refused("approval_missing");
        }

        if (!approval.UserConfirmedInThisMessage)
        {
            return new ApprovalResult.Refused("not_confirmed_in_this_message");
        }

        if (string.IsNullOrWhiteSpace(approval.ConfirmationText))
        {
            return new ApprovalResult.Refused("empty_confirmation_text");
        }

        if (nowUtc - approval.ConfirmedAtUtc > MaximumApprovalAge)
        {
            return new ApprovalResult.Refused("stale_confirmation");
        }

        if (!string.Equals(approval.ApprovedScopeId, expectedScopeId, StringComparison.Ordinal))
        {
            return new ApprovalResult.Refused("scope_id_mismatch");
        }

        if (!string.Equals(approval.ApprovedOperationId, expectedOperationId, StringComparison.Ordinal))
        {
            return new ApprovalResult.Refused("operation_id_mismatch");
        }

        return new ApprovalResult.Accepted();
    }
}
