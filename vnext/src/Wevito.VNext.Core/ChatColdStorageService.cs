using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ChatColdStorageResult(
    bool Succeeded,
    int SessionsArchived,
    string ColdStorePath,
    string Message);

public sealed class ChatColdStorageService
{
    public const string ChatSessionArchivedPacketKind = "chat_session_archived";

    private readonly ChatHistoryStore _activeStore;
    private readonly ChatHistoryStore _coldStore;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public ChatColdStorageService(ChatHistoryStore? activeStore = null, ChatHistoryStore? coldStore = null, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _activeStore = activeStore ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _coldStore = coldStore ?? new ChatHistoryStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wevito", "chat-history-cold.sqlite"), killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public ChatColdStorageResult ArchiveInactiveSessions(DateTimeOffset nowUtc, TimeSpan? inactiveAge = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new ChatColdStorageResult(false, 0, _coldStore.DatabasePath, "kill_switch=true");
        }

        var cutoff = nowUtc - (inactiveAge ?? TimeSpan.FromDays(183));
        var inactive = _activeStore.ListSessionsInactiveBefore(cutoff);
        var archived = 0;
        foreach (var session in inactive)
        {
            _coldStore.CopySessionFrom(_activeStore, session.SessionId);
            _activeStore.SoftDeleteSession(session.SessionId, nowUtc);
            archived++;
            _auditLedgerService?.Record(new EvidencePacket(Guid.NewGuid(), ChatSessionArchivedPacketKind, null, nowUtc, false, false, false, true, _coldStore.DatabasePath, $"Archived inactive chat session {session.SessionId} to cold storage; active session was soft-deleted to preserve append-only turns.", "Completed"));
        }

        return new ChatColdStorageResult(true, archived, _coldStore.DatabasePath, $"Archived {archived} inactive chat session(s).");
    }

    public IReadOnlyList<ChatTurn> SearchWithColdFallback(string query, int limit = 50)
    {
        var active = _activeStore.SearchTurns(query, limit);
        return active.Count > 0 ? active : _coldStore.SearchTurns(query, limit);
    }
}
