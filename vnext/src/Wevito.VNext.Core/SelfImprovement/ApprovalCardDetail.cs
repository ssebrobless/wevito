namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ApprovalCardInputFile(string Path, string Sha256, string Role)
{
    public ApprovalCardInputFile(KillSwitchService killSwitchService)
        : this("", "", "")
    {
        ArgumentNullException.ThrowIfNull(killSwitchService);
    }
}

public sealed record ApprovalCardDetail(
    string OperationId,
    string ScopeId,
    string ScopeHash,
    IReadOnlyList<ApprovalCardInputFile> InputFiles,
    IReadOnlyList<string> ExpectedPacketChain,
    string SafetyCopy,
    string ArtifactJsonPath,
    bool Blocked,
    string BlockedReason)
{
    public ApprovalCardDetail(KillSwitchService killSwitchService)
        : this("", "", "", [], [], SafetyCopyText, "", true, "kill_switch=true")
    {
        ArgumentNullException.ThrowIfNull(killSwitchService);
    }

    public static ApprovalCardDetail BlockedDetail(string reason)
    {
        return new ApprovalCardDetail("", "", "", [], [], SafetyCopyText, "", true, reason);
    }

    public const string SafetyCopyText = "Proposal-only. Apply still requires explicit approval.";
}
