using System.Runtime.CompilerServices;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ChatStreamingServiceTests
{
    [Fact]
    public async Task StreamsTokensInOrder()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("stream");
        var service = new ChatStreamingService(store, tokenSource: (_, ct) => TokensAsync(["hello ", "there"], ct));

        var events = await CollectAsync(service.StreamAssistantTurnAsync(session, "say hi"));

        Assert.Equal("hello there", string.Concat(events.Where(e => e.Kind == ChatStreamEventKind.Token).Select(e => e.Content)));
        Assert.Contains(events, e => e.Kind == ChatStreamEventKind.Complete);
        Assert.Contains(store.GetTurns(session), turn => turn.Role == "assistant" && turn.Content == "hello there");
    }

    [Fact]
    public async Task DetectsToolCallMidStream()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("tool");
        var service = new ChatStreamingService(store, tokenSource: (_, ct) => TokensAsync(["before ", "[[tool:localDocs {\"query\":\"sprites\"}]]", " after"], ct));

        var events = await CollectAsync(service.StreamAssistantTurnAsync(session, "use docs"));

        Assert.Contains(events, e => e.Kind == ChatStreamEventKind.ToolCallStart && e.ToolName == "localDocs");
        Assert.Contains(events, e => e.Kind == ChatStreamEventKind.ToolCallEnd && !string.IsNullOrWhiteSpace(e.ToolResultId));
    }

    [Fact]
    public async Task InjectsToolResultBackIntoContext()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("tool-result");
        var service = new ChatStreamingService(store, tokenSource: (_, ct) => TokensAsync(["[[tool:localDocs {}]]"], ct));

        _ = await CollectAsync(service.StreamAssistantTurnAsync(session, "tool please"));

        var turns = store.GetTurns(session);
        Assert.Contains(turns, turn => turn.Role == "tool" && turn.Content.Contains("C-PHASE 109", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(turns, turn => turn.Role == "assistant" && turn.Content.Contains("Tool localDocs deferred", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CancellationStopsStreamCleanly()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("cancel");
        using var cts = new CancellationTokenSource();
        var service = new ChatStreamingService(store, tokenSource: (_, ct) => CancellableTokensAsync(cts, ct));

        var events = await CollectAsync(service.StreamAssistantTurnAsync(session, "cancel", cts.Token));

        Assert.Contains(events, e => e.Kind == ChatStreamEventKind.Cancelled);
        Assert.DoesNotContain(store.GetTurns(session), turn => turn.Role == "assistant");
        Assert.DoesNotContain(store.GetTurns(session), turn => turn.Role == "tool");
    }

    [Fact]
    public async Task FallsBackOnAdapterError()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("fallback");
        var service = new ChatStreamingService(store, modelAdapter: new ThrowingModelAdapter());

        var events = await CollectAsync(service.StreamAssistantTurnAsync(session, "recover"));

        Assert.Contains(events, e => e.Kind == ChatStreamEventKind.Token);
        Assert.Contains(store.GetTurns(session), turn => turn.Role == "assistant" && turn.Content.Contains("failed safely", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<ChatStreamEvent>> CollectAsync(IAsyncEnumerable<ChatStreamEvent> source)
    {
        var events = new List<ChatStreamEvent>();
        await foreach (var item in source)
        {
            events.Add(item);
        }

        return events;
    }

    private static async IAsyncEnumerable<string> TokensAsync(IEnumerable<string> tokens, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<string> CancellableTokensAsync(CancellationTokenSource cts, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "partial ";
        cts.Cancel();
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-chat-stream-tests", Guid.NewGuid().ToString("N"), "chat.sqlite");
    }

    private sealed class ThrowingModelAdapter : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
