namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed record ApplyRequest(
    string OperationId,
    string ScopeId,
    string ScopeHash,
    string DraftRelativePath,
    string ApprovalToken);
