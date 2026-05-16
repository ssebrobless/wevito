namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class RollingSummarizerServiceTests
{
    [Fact]
    public async Task TriggeredAt80Percent()
    {
        var root = TestRoot();
        var store = SeedPressureStore(root, out var sessionId);
        var result = await BuildService(root, store).RunIfNeededAsync(sessionId, Path.Combine(root, "artifacts"));

        Assert.True(result.Succeeded);
        Assert.Equal(sessionId, result.SessionId);
        Assert.NotEmpty(result.Summary);
    }

    [Fact]
    public async Task PreservesOriginalsInChatHistory()
    {
        var root = TestRoot();
        var store = SeedPressureStore(root, out var sessionId);
        var before = store.GetTurns(sessionId, 10).Count;
        await BuildService(root, store).RunIfNeededAsync(sessionId, Path.Combine(root, "artifacts"));

        Assert.Equal(before, store.GetTurns(sessionId, 10).Count);
    }

    [Fact]
    public async Task SummaryStoredInPetMemoryStore()
    {
        var root = TestRoot();
        var store = SeedPressureStore(root, out var sessionId);
        var memory = new PetMemoryStore(Path.Combine(root, "memory"));
        await BuildService(root, store, memory).RunIfNeededAsync(sessionId, Path.Combine(root, "artifacts"));

        var results = memory.Search(sessionId, "fox planning", RollingSummarizerService.MemoryKind, 3);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task RespectsKillSwitch()
    {
        var root = TestRoot();
        var store = SeedPressureStore(root, out var sessionId);
        var settings = new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" };
        var killSwitch = new KillSwitchService(() => settings);
        var service = new RollingSummarizerService(store, null, new PetMemoryStore(Path.Combine(root, "memory")), new FakeModelAdapter(), killSwitchService: killSwitch);

        var result = await service.RunIfNeededAsync(sessionId, Path.Combine(root, "artifacts"));

        Assert.False(result.Succeeded);
        Assert.Contains("kill_switch", result.Message);
    }

    private static RollingSummarizerService BuildService(string root, ChatHistoryStore store, PetMemoryStore? memory = null)
    {
        return new RollingSummarizerService(
            store,
            new ChatContextBudgetService(store),
            memory ?? new PetMemoryStore(Path.Combine(root, "memory")),
            new FakeModelAdapter());
    }

    private static ChatHistoryStore SeedPressureStore(string root, out Guid sessionId)
    {
        var store = new ChatHistoryStore(Path.Combine(root, "chat.sqlite"));
        sessionId = store.CreateSession();
        store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "user", "please remember the fox sprite cleanup plan", null, null, DateTimeOffset.UtcNow.AddMinutes(-5), "", 56_000));
        return store;
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-rolling-summary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class FakeModelAdapter : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ModelResponse("fake", "fake", "summary about fox planning", DidCallProvider: true));
        }
    }
}
