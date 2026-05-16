namespace Wevito.VNext.Core;

public sealed class ChatSessionService
{
    public const string ChatSessionStartedPacketKind = "chat_session_started";
    public const string ChatSearchPerformedPacketKind = "chat_search_performed";

    private readonly ChatHistoryStore _store;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private Guid? _currentSessionId;

    public ChatSessionService(ChatHistoryStore? store = null, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _store = store ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public Guid StartNewSession(string? title = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var now = DateTimeOffset.UtcNow;
        _currentSessionId = _store.CreateSession(title, now);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            ChatSessionStartedPacketKind,
            null,
            now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _store.DatabasePath,
            Summary: $"Started chat session {_currentSessionId}.",
            Status: "Completed"));
        return _currentSessionId.Value;
    }

    public Guid GetCurrentSessionId()
    {
        return _currentSessionId ?? StartNewSession();
    }

    public IReadOnlyList<ChatTurn> GetTurns(Guid sessionId, int limit = 50)
    {
        return _store.GetTurns(sessionId, limit);
    }

    public IReadOnlyList<ChatSessionSummary> ListSessions(int limit = 20)
    {
        return _store.ListSessions(limit);
    }

    public IReadOnlyList<ChatTurn> SearchTurns(string query, int limit = 50)
    {
        var results = _store.SearchTurns(query, limit);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            ChatSearchPerformedPacketKind,
            null,
            DateTimeOffset.UtcNow,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _store.DatabasePath,
            Summary: $"Searched chat history; hits={results.Count}.",
            Status: "Completed"));
        return results;
    }
}
