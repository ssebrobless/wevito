using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record LocalReasoningEvidencePacket(
    string SchemaVersion,
    Guid TaskCardId,
    string AgentRole,
    string ToolFamily,
    string Question,
    IReadOnlyList<string> RetrievedChunkIds,
    string PromptSha256,
    string ResponseSha256,
    string ModelId,
    double CitationCoverageRatio,
    bool DidUseLocalModel,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidMutate,
    string ArtifactPath,
    DateTimeOffset CreatedAtUtc);

public sealed record LocalReasoningRequest(
    Guid TaskCardId,
    string Question,
    RetrievalResult Retrieved,
    string AgentRole,
    string ToolFamily,
    IReadOnlyList<string>? TrustedContext = null,
    IReadOnlyList<string>? UntrustedContext = null,
    string ArtifactRoot = "",
    DateTimeOffset RequestedAtUtc = default);

public sealed record LocalReasoningResult(
    bool Succeeded,
    string Synthesis,
    double CitationCoverageRatio,
    LocalReasoningEvidencePacket Packet,
    string PacketPath,
    string BlockReason = "");
