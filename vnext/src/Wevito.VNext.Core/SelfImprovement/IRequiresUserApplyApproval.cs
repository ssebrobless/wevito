namespace Wevito.VNext.Core.SelfImprovement;

public interface IRequiresUserApplyApproval
{
    ApprovalResult ValidateUserApplyApproval(
        UserApplyApproval? approval,
        string expectedScopeId,
        string expectedOperationId,
        string expectedScopeHash,
        DateTimeOffset nowUtc);
}
