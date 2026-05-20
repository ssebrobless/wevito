namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed record RollbackRequest(
    string OperationId,
    string ScopeId,
    string ScopeHash,
    string ApprovedRelativePath,
    string ApprovalToken);
