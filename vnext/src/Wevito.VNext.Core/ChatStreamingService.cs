using System.Runtime.CompilerServices;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ChatStreamingService
{
    public const string ChatTurnCompletedPacketKind = "chat_turn_completed";
    public const string ChatTurnCancelledPacketKind = "chat_turn_cancelled";

    private readonly ChatHistoryStore _store;
    private readonly IModelAdapter _modelAdapter;
    private readonly ChatTitleService? _titleService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<ModelRequest, CancellationToken, IAsyncEnumerable<string>>? _tokenSource;

    public ChatStreamingService(
        ChatHistoryStore? store = null,
        IModelAdapter? modelAdapter = null,
        ChatTitleService? titleService = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<ModelRequest, CancellationToken, IAsyncEnumerable<string>>? tokenSource = null)
    {
        _store = store ?? new ChatHistoryStore(killSwitchService: killSwitchService);
        _modelAdapter = modelAdapter ?? new LocalModelAdapter(killSwitchService);
        _titleService = titleService;
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _tokenSource = tokenSource;
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamAssistantTurnAsync(
        Guid sessionId,
        string userText,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            yield return new ChatStreamEvent(ChatStreamEventKind.Error, Content: "Stop Everything is active.");
            yield break;
        }

        var now = DateTimeOffset.UtcNow;
        _store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "user", userText ?? "", null, null, now, "", EstimateTokens(userText)));

        var assistantText = new List<string>();
        var modelId = "";
        var didUseLocalModel = false;
        var cancelled = false;

        IAsyncEnumerable<string> stream;
        try
        {
            var request = BuildRequest(sessionId, userText ?? "");
            stream = _tokenSource?.Invoke(request, cancellationToken) ?? BuildDefaultTokenStreamAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            stream = FallbackTokensAsync($"I could not start the local model stream, so I preserved your request without running tools. ({ex.GetType().Name})", cancellationToken);
        }

        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            string token;
            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    break;
                }

                token = enumerator.Current ?? "";
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                break;
            }
            catch (Exception ex)
            {
                token = $"I hit a local stream error and stopped safely: {ex.GetType().Name}.";
            }

            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            if (TryParseToolCall(token, out var toolName, out var toolJson))
            {
                var resultId = Guid.NewGuid().ToString("N");
                yield return new ChatStreamEvent(ChatStreamEventKind.ToolCallStart, ToolName: toolName, ToolCallJson: toolJson);
                var result = "Tool dispatch is reserved for C-PHASE 109; no tool was executed or policy bypassed.";
                _store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "tool", result, toolJson, resultId, DateTimeOffset.UtcNow, "deferred-tool-dispatch", EstimateTokens(result)));
                yield return new ChatStreamEvent(ChatStreamEventKind.ToolCallResult, Content: result, ToolName: toolName, ToolCallJson: toolJson, ToolResultId: resultId);
                yield return new ChatStreamEvent(ChatStreamEventKind.ToolCallEnd, ToolName: toolName, ToolCallJson: toolJson, ToolResultId: resultId);
                assistantText.Add($"[Tool {toolName} deferred: {resultId}]");
                continue;
            }

            assistantText.Add(token);
            yield return new ChatStreamEvent(ChatStreamEventKind.Token, token);
        }

        if (cancelled || cancellationToken.IsCancellationRequested)
        {
            Record(ChatTurnCancelledPacketKind, sessionId, didUseLocalModel: false, "Cancelled chat turn before assistant row was committed.", "Cancelled");
            yield return new ChatStreamEvent(ChatStreamEventKind.Cancelled, Content: "Cancelled.");
            yield break;
        }

        var finalText = string.Join("", assistantText);
        if (string.IsNullOrWhiteSpace(finalText))
        {
            finalText = "I did not receive a local model response, so I stopped without running tools.";
        }

        if (_modelAdapter is OllamaLocalModelAdapter)
        {
            modelId = LocalRuntimeProbeService.DefaultOllamaModel;
            didUseLocalModel = true;
        }
        else
        {
            modelId = LocalModelAdapter.Model;
        }

        _store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "assistant", finalText, null, null, DateTimeOffset.UtcNow, modelId, EstimateTokens(finalText)));
        Record(ChatTurnCompletedPacketKind, sessionId, didUseLocalModel, "Completed chat turn.", "Completed");
        if (_titleService is not null)
        {
            _ = await _titleService.TryTitleAfterFirstTurnAsync(sessionId, CancellationToken.None).ConfigureAwait(false);
        }

        yield return new ChatStreamEvent(ChatStreamEventKind.Complete, Content: finalText);
    }

    private async IAsyncEnumerable<string> BuildDefaultTokenStreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_modelAdapter is OllamaLocalModelAdapter ollama)
        {
            await foreach (var token in ollama.StreamAsync(request, cancellationToken).ConfigureAwait(false))
            {
                yield return token;
            }

            yield break;
        }

        ModelResponse response;
        try
        {
            response = await _modelAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            response = new ModelResponse(LocalModelAdapter.Provider, LocalModelAdapter.Model, $"Local model failed safely: {ex.GetType().Name}.", DidCallProvider: false);
        }

        foreach (var token in SplitForStreaming(response.Summary))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<string> FallbackTokensAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var token in SplitForStreaming(message))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
            await Task.Yield();
        }
    }

    private static ModelRequest BuildRequest(Guid sessionId, string userText)
    {
        return new ModelRequest(
            sessionId,
            "Wevito",
            PetHelperRole.ResearchHelper,
            "chat",
            userText ?? "",
            "Multi-turn local chat response.",
            TrustedContext: ["Wevito chat is local-first. Tool calls are placeholders until C-PHASE 109."],
            UntrustedContext: [],
            ApprovedForModelCall: true,
            ArtifactRoot: Path.Combine("vnext", "artifacts", "chat-sessions", sessionId.ToString("N")),
            RequestedAtUtc: DateTimeOffset.UtcNow);
    }

    private void Record(string packetKind, Guid sessionId, bool didUseLocalModel, string summary, string status)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            DateTimeOffset.UtcNow,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: didUseLocalModel,
            DidMutate: false,
            ArtifactPath: _store.DatabasePath,
            Summary: $"{summary} session={sessionId}",
            Status: status));
    }

    private static bool TryParseToolCall(string token, out string toolName, out string toolJson)
    {
        toolName = "";
        toolJson = "";
        var trimmed = token.Trim();
        if (!trimmed.StartsWith("[[tool:", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith("]]", StringComparison.Ordinal))
        {
            return false;
        }

        var payload = trimmed[7..^2].Trim();
        var space = payload.IndexOf(' ');
        toolName = space < 0 ? payload : payload[..space].Trim();
        toolJson = space < 0 ? "{}" : payload[(space + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(toolJson))
        {
            toolJson = "{}";
        }

        if (!toolJson.StartsWith("{", StringComparison.Ordinal))
        {
            toolJson = JsonSerializer.Serialize(new { raw = toolJson });
        }

        return !string.IsNullOrWhiteSpace(toolName);
    }

    private static IEnumerable<string> SplitForStreaming(string value)
    {
        var parts = (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            yield break;
        }

        for (var index = 0; index < parts.Length; index++)
        {
            yield return index == parts.Length - 1 ? parts[index] : parts[index] + " ";
        }
    }

    private static int EstimateTokens(string? value)
    {
        return Math.Max(0, (value ?? "").Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
