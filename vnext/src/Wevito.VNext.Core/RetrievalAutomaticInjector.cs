using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record RetrievalAutomaticInjection(
    Guid SessionId,
    IReadOnlyList<string> ContextLines,
    IReadOnlyList<double> Scores,
    DateTimeOffset RetrievedAtUtc);

public sealed class RetrievalAutomaticInjector
{
    public const string RetrievalTriggeredPacketKind = "retrieval_triggered";
    public const int DefaultTopK = 3;

    private readonly PetMemoryStore _memoryStore;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public RetrievalAutomaticInjector(PetMemoryStore? memoryStore = null, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _memoryStore = memoryStore ?? new PetMemoryStore();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public RetrievalAutomaticInjection RetrieveForUserTurn(Guid sessionId, string userMessage, int topK = DefaultTopK, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true || string.IsNullOrWhiteSpace(userMessage))
        {
            return new RetrievalAutomaticInjection(sessionId, [], [], timestamp);
        }

        var results = _memoryStore.Search(sessionId, userMessage, RollingSummarizerService.MemoryKind, Math.Max(1, topK));
        var lines = results
            .Select(result => $"[memory:{result.Example.Kind} score={result.Score:0.000}] {result.Example.Content}")
            .ToList();
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            RetrievalTriggeredPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"Retrieved {lines.Count} memory item(s) for chat session {sessionId}.",
            Status: "Completed"));
        return new RetrievalAutomaticInjection(sessionId, lines, results.Select(result => result.Score).ToList(), timestamp);
    }
}
