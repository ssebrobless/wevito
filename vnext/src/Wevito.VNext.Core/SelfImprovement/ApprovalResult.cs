namespace Wevito.VNext.Core.SelfImprovement;

public abstract record ApprovalResult
{
    private ApprovalResult()
    {
    }

    public sealed record Accepted() : ApprovalResult;

    public sealed record Refused(string Reason) : ApprovalResult;
}
