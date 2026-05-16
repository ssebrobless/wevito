using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record RollingSummarizerResult(
    bool Succeeded,
    Guid SessionId,
    int SourceTurnCount,
    int SourceTokenCount,
    string Summary,
    string ArtifactFolder,
    string Message);

public sealed class RollingSummarizerService
{
    public const string SummarizationRunPacketKind = "summarization_run";
    public const string MemoryKind = "session_summary";
    public const int SourceWindowTokens = 20_000;
    public const int TargetSummaryTokens = 3_000;

    private readonly ChatHistoryStore _historyStore;
    private readonly ChatContextBudgetService _budgetService;
    private readonly PetMemoryStore _memoryStore;
    private readonly IModelAdapter _modelAdapter;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public RollingSummarizerService(
        ChatHistoryStore? historyStore = null,
        ChatContextBudgetService? budgetService = null,
        PetMemoryStore? memoryStore = null,
        IModelAdapter? modelAdapter = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _historyStore = historyStore ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _budgetService = budgetService ?? new ChatContextBudgetService(_historyStore, auditLedgerService, killSwitchService);
        _memoryStore = memoryStore ?? new PetMemoryStore();
        _modelAdapter = modelAdapter ?? new LocalModelAdapter(killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public async Task<RollingSummarizerResult> RunIfNeededAsync(Guid sessionId, string artifactRoot, DateTimeOffset? nowUtc = null, CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true)
        {
            return Block(sessionId, "kill_switch=true");
        }

        var budget = _budgetService.Snapshot(sessionId, timestamp);
        if (!budget.IsPressureThresholdReached)
        {
            return new RollingSummarizerResult(false, sessionId, 0, budget.TurnsTokensUsed, "", "", "Budget pressure threshold not reached.");
        }

        var turns = _historyStore.GetOldestTurnsWithinTokenBudget(sessionId, SourceWindowTokens);
        if (turns.Count == 0)
        {
            return new RollingSummarizerResult(false, sessionId, 0, 0, "", "", "No chat turns available for summarization.");
        }

        var source = string.Join(Environment.NewLine, turns.Select(turn => $"{turn.CreatedAtUtc:O} {turn.Role}: {turn.Content}"));
        var request = new ModelRequest(
            sessionId,
            "Wevito",
            "ContextSummarizer",
            "summarization",
            $"Compress this chat window to about {TargetSummaryTokens} tokens while preserving decisions, tasks, open questions, and file references.",
            "Rolling chat-context summarization.",
            TrustedContext: [source],
            UntrustedContext: [],
            ApprovedForModelCall: true,
            ArtifactRoot: artifactRoot,
            RequestedAtUtc: timestamp);
        var response = await _modelAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false);
        var summary = string.IsNullOrWhiteSpace(response.Summary)
            ? BuildExtractiveSummary(turns)
            : response.Summary.Trim();

        _memoryStore.AddExample(sessionId, MemoryKind, summary, "chat-context", timestamp, "chat-context", sessionId.ToString("N"));

        var folder = Path.Combine(Path.GetFullPath(artifactRoot), $"{timestamp:yyyyMMdd-HHmmss}-session-summary");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "summary.json"), JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            sessionId,
            sourceTurnCount = turns.Count,
            sourceTokenCount = turns.Sum(turn => turn.TokensUsed),
            targetSummaryTokens = TargetSummaryTokens,
            summary
        }, JsonDefaults.Options));

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            SummarizationRunPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: response.DidCallProvider,
            DidMutate: true,
            ArtifactPath: folder,
            Summary: $"Summarized {turns.Count} chat turn(s) for session {sessionId}; originals remain in ChatHistoryStore.",
            Status: "Completed"));

        return new RollingSummarizerResult(true, sessionId, turns.Count, turns.Sum(turn => turn.TokensUsed), summary, folder, "Summary stored in local memory.");
    }

    private static string BuildExtractiveSummary(IReadOnlyList<ChatTurn> turns)
    {
        return string.Join(Environment.NewLine, turns.Take(20).Select(turn => $"- {turn.Role}: {Trim(turn.Content, 240)}"));
    }

    private static string Trim(string value, int limit)
    {
        return value.Length <= limit ? value : value[..limit] + "...";
    }

    private static RollingSummarizerResult Block(Guid sessionId, string message)
    {
        return new RollingSummarizerResult(false, sessionId, 0, 0, "", "", message);
    }
}
