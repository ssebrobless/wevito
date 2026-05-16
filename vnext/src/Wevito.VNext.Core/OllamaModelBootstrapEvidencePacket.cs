namespace Wevito.VNext.Core;

public sealed record OllamaModelBootstrapEvidencePacket(
    string PacketKind,
    DateTimeOffset CreatedAtUtc,
    string Endpoint,
    string Model,
    string RuntimeStatus,
    string Instruction,
    string InstallInstructions,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidUseLocalModel,
    bool DidMutate,
    string DecisionId);

public sealed record OllamaModelBootstrapStatus(
    string PacketKind,
    bool DidRunProbe,
    bool RuntimeAvailable,
    bool ModelPresent,
    bool WasSkipped,
    string ArtifactPath,
    string Summary);
