using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ChatSessionServiceTests
{
    [Fact]
    public void NewSessionGetsFreshId()
    {
        var service = new ChatSessionService(new ChatHistoryStore(CreateDatabasePath()));

        var first = service.StartNewSession("First");
        var second = service.StartNewSession("Second");

        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(Guid.Empty, second);
        Assert.NotEqual(first, second);
        Assert.Equal(second, service.GetCurrentSessionId());
    }

    [Fact]
    public void ListSessionsOrderedByMostRecent()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var service = new ChatSessionService(store);
        var older = store.CreateSession("Older", DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
        var newer = store.CreateSession("Newer", DateTimeOffset.Parse("2026-05-15T12:05:00Z"));
        store.AppendTurn(new ChatTurn(older, Guid.NewGuid(), "user", "old", null, null, DateTimeOffset.Parse("2026-05-15T12:01:00Z"), "", 1));
        store.AppendTurn(new ChatTurn(newer, Guid.NewGuid(), "user", "new", null, null, DateTimeOffset.Parse("2026-05-15T12:06:00Z"), "", 1));

        var sessions = service.ListSessions(10);

        Assert.Equal(newer, sessions[0].SessionId);
        Assert.Equal(older, sessions[1].SessionId);
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-chat-session-tests", Guid.NewGuid().ToString("N"), "chat.sqlite");
    }
}
