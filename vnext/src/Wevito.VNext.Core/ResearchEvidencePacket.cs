namespace Wevito.VNext.Core;

public enum ResearchSourceKind
{
    LocalMemory,
    LocalDocument,
    ToolReport,
    UserProvided,
    WebSourcePlaceholder
}

public sealed record ResearchSourceRecord(
    string Id,
    ResearchSourceKind Kind,
    string Title,
    string PathOrUri,
    bool IsNetworkSource = false,
    bool WasFetched = false);

public sealed record ResearchClaimRecord(
    string Claim,
    IReadOnlyList<string> SourceIds,
    double Confidence,
    string Uncertainty);

public sealed record ResearchEvidencePacket(
    string Question,
    IReadOnlyList<string> LocalMemoryUsed,
    IReadOnlyList<ResearchSourceRecord> SourcesInspected,
    IReadOnlyList<ResearchClaimRecord> ClaimsExtracted,
    string Synthesis,
    string NextRecommendedAction,
    bool DidUseHostedAi,
    bool DidUseNetwork,
    DateTimeOffset CreatedAtUtc);
