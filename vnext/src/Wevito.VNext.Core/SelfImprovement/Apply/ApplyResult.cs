namespace Wevito.VNext.Core.SelfImprovement.Apply;

public abstract record ApplyResult
{
    private ApplyResult()
    {
    }

    public sealed record Refused(string Reason) : ApplyResult;

    public sealed record RolledBack(string Reason, string BackupRelativePath) : ApplyResult;

    public sealed record Succeeded(string ApprovedRelativePath, string PostHashSha256) : ApplyResult;
}
