namespace Wevito.VNext.Core.SelfImprovement.Apply;

public abstract record RollbackResult
{
    private RollbackResult()
    {
    }

    public sealed record Refused(string Reason) : RollbackResult;

    public sealed record Succeeded(string DraftRelativePath, string PostHashSha256) : RollbackResult;
}
