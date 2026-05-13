namespace Wevito.VNext.Core;

public sealed record ModelInferenceEvidencePacket(
    string SchemaVersion,
    Guid PetId,
    string PetName,
    string HelperRole,
    string ToolFamily,
    string Provider,
    string Model,
    string RuntimeId,
    string PromptSha256,
    string ResponseSha256,
    long LatencyMs,
    bool DidUseLocalModel,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidFallbackToDeterministic,
    string BlockReason,
    DateTimeOffset GeneratedAtUtc);

public sealed record LocalRuntimeProbeResult(
    bool IsAvailable,
    bool WasDormant,
    string RuntimeId,
    string Endpoint,
    string Model,
    string Reason,
    DateTimeOffset ProbedAtUtc);
