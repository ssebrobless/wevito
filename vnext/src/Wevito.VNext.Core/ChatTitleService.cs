namespace Wevito.VNext.Core;

public sealed class ChatTitleService
{
    public const string ChatSessionTitleSetPacketKind = "chat_session_title_set";

    private readonly ChatHistoryStore _store;
    private readonly IModelAdapter _modelAdapter;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public ChatTitleService(
        ChatHistoryStore? store = null,
        IModelAdapter? modelAdapter = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _store = store ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _modelAdapter = modelAdapter ?? new LocalModelAdapter(killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public async Task<string?> TryTitleAfterFirstTurnAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return null;
        }

        var turns = _store.GetTurns(sessionId, limit: 10);
        if (turns.Count(turn => turn.Role is "user" or "assistant") != 2 ||
            turns.Count(turn => turn.Role == "assistant") != 1)
        {
            return null;
        }

        var prompt = string.Join(Environment.NewLine, turns.Select(turn => $"{turn.Role}: {turn.Content}"));
        var response = await _modelAdapter.SuggestAsync(new ModelRequest(
            sessionId,
            "Wevito",
            "ChatAgent",
            "chatTitle",
            "Create a 5-7 word title for this chat.",
            prompt,
            TrustedContext: [prompt],
            UntrustedContext: [],
            ApprovedForModelCall: true,
            ArtifactRoot: Path.Combine(Path.GetTempPath(), "wevito-chat-title", sessionId.ToString("N")),
            RequestedAtUtc: DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);

        var title = SanitizeTitle(response.Summary);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        _store.SetSessionTitle(sessionId, title, now);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            ChatSessionTitleSetPacketKind,
            null,
            now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: response.DidCallProvider,
            DidMutate: false,
            ArtifactPath: _store.DatabasePath,
            Summary: $"Set chat title: {title}",
            Status: "Completed"));
        return title;
    }

    private static string SanitizeTitle(string value)
    {
        var compact = string.Join(" ", (value ?? "").Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries));
        compact = compact.Trim('"', '\'', '.', ':', '-');
        var words = compact.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(7).ToArray();
        return words.Length == 0 ? "" : string.Join(" ", words);
    }
}
