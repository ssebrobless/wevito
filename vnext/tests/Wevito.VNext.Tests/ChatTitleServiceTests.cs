using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ChatTitleServiceTests
{
    [Fact]
    public async Task GeneratesTitleFromFirstTurn()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("New chat");
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "user", "how do sprites work", null, null, DateTimeOffset.Parse("2026-05-15T12:00:00Z"), "", 4));
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "assistant", "they use runtime rows", null, null, DateTimeOffset.Parse("2026-05-15T12:01:00Z"), "test", 4));
        var service = new ChatTitleService(store, new FixedModelAdapter("Sprite Runtime Row Planning"));

        var title = await service.TryTitleAfterFirstTurnAsync(session);

        Assert.Equal("Sprite Runtime Row Planning", title);
        Assert.Equal("Sprite Runtime Row Planning", store.ListSessions().Single().Title);
    }

    [Fact]
    public async Task DoesNotTitleOnSubsequentTurns()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("New chat");
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "user", "one", null, null, DateTimeOffset.Parse("2026-05-15T12:00:00Z"), "", 1));
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "assistant", "two", null, null, DateTimeOffset.Parse("2026-05-15T12:01:00Z"), "test", 1));
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "user", "three", null, null, DateTimeOffset.Parse("2026-05-15T12:02:00Z"), "", 1));
        var service = new ChatTitleService(store, new FixedModelAdapter("Should Not Set"));

        var title = await service.TryTitleAfterFirstTurnAsync(session);

        Assert.Null(title);
        Assert.Equal("New chat", store.ListSessions().Single().Title);
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-chat-title-tests", Guid.NewGuid().ToString("N"), "chat.sqlite");
    }

    private sealed class FixedModelAdapter(string title) : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ModelResponse("test", "fixed", title, DidCallProvider: true));
        }
    }
}
