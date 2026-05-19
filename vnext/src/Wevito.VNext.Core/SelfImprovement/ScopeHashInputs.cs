namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ScopeHashInputs(
    string ScopeId,
    string OperationId,
    string ProposalSha256,
    string DryRunSha256,
    string EvalSha256,
    string ExperimentManifestVersion,
    IReadOnlyList<string> PacketKindsTouched);
