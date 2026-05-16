namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class ChatColdStorageServiceTests
{
    [Fact]
    public void MovesSessionsInactive6Months()
    {
        var root = TestRoot();
        var active = SeedOldSession(root, out var sessionId);
        var cold = new ChatHistoryStore(Path.Combine(root, "cold.sqlite"));
        var result = new ChatColdStorageService(active, cold).ArchiveInactiveSessions(DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.SessionsArchived);
        Assert.DoesNotContain(active.ListSessions(10), session => session.SessionId == sessionId);
        Assert.Contains(cold.ListSessions(10), session => session.SessionId == sessionId);
    }

    [Fact]
    public void PreservesAppendOnlyInvariantInColdStore()
    {
        var root = TestRoot();
        var active = SeedOldSession(root, out _);
        var cold = new ChatHistoryStore(Path.Combine(root, "cold.sqlite"));
        new ChatColdStorageService(active, cold).ArchiveInactiveSessions(DateTimeOffset.UtcNow);

        cold.AssertAppendOnlyGuards();
    }

    [Fact]
    public void LazyReadOnRetrievalMiss()
    {
        var root = TestRoot();
        var active = SeedOldSession(root, out _);
        var cold = new ChatHistoryStore(Path.Combine(root, "cold.sqlite"));
        var service = new ChatColdStorageService(active, cold);
        service.ArchiveInactiveSessions(DateTimeOffset.UtcNow);

        var results = service.SearchWithColdFallback("archived");

        Assert.NotEmpty(results);
    }

    private static ChatHistoryStore SeedOldSession(string root, out Guid sessionId)
    {
        var store = new ChatHistoryStore(Path.Combine(root, "active.sqlite"));
        var old = DateTimeOffset.UtcNow.AddMonths(-7);
        sessionId = store.CreateSession("old", old);
        store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "user", "archived context", null, null, old, "", 2));
        return store;
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-cold-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
