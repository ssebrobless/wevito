using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ChatContextBudgetSnapshot(
    Guid SessionId,
    int SystemBudget,
    int TurnsBudget,
    int RetrievalBudget,
    int ToolBufferBudget,
    int ReplyBudget,
    int TurnsTokensUsed,
    int RemainingTurnsBudget,
    bool IsPressureThresholdReached);

public sealed class ChatContextBudgetService
{
    public const string ContextBudgetPressurePacketKind = "context_budget_pressure";
    public const int SystemBudget = 4_000;
    public const int TurnsBudget = 70_000;
    public const int RetrievalBudget = 30_000;
    public const int ToolBufferBudget = 16_000;
    public const int ReplyBudget = 8_000;
    public const double PressureThreshold = 0.80d;

    private readonly ChatHistoryStore _store;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public ChatContextBudgetService(ChatHistoryStore? store = null, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _store = store ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public int RemainingTurnsBudget(Guid sessionId)
    {
        return Math.Max(0, TurnsBudget - _store.GetTurnTokenTotal(sessionId));
    }

    public ChatContextBudgetSnapshot Snapshot(Guid sessionId, DateTimeOffset? nowUtc = null)
    {
        var used = _store.GetTurnTokenTotal(sessionId);
        var snapshot = new ChatContextBudgetSnapshot(
            sessionId,
            SystemBudget,
            TurnsBudget,
            RetrievalBudget,
            ToolBufferBudget,
            ReplyBudget,
            used,
            Math.Max(0, TurnsBudget - used),
            used >= TurnsBudget * PressureThreshold);

        if (snapshot.IsPressureThresholdReached)
        {
            RecordPressure(snapshot, nowUtc ?? DateTimeOffset.UtcNow);
        }

        return snapshot;
    }

    private void RecordPressure(ChatContextBudgetSnapshot snapshot, DateTimeOffset timestamp)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            ContextBudgetPressurePacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _store.DatabasePath,
            Summary: $"Session {snapshot.SessionId} reached chat-context pressure at {snapshot.TurnsTokensUsed}/{snapshot.TurnsBudget} turn tokens.",
            Status: "Pressure"));
    }
}
