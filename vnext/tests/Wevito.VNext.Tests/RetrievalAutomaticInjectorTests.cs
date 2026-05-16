namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class RetrievalAutomaticInjectorTests
{
    [Fact]
    public void AutoInjectsTop3PerUserTurn()
    {
        var root = TestRoot();
        var memory = new PetMemoryStore(Path.Combine(root, "memory"));
        var sessionId = Guid.NewGuid();
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "fox sprite cleanup notes", "chat-context");
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "snake motion audit notes", "chat-context");
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "goose color review notes", "chat-context");
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "frog jump issue notes", "chat-context");

        var result = new RetrievalAutomaticInjector(memory).RetrieveForUserTurn(sessionId, "fox sprite", topK: 3);

        Assert.Equal(3, result.ContextLines.Count);
    }

    [Fact]
    public void SkipsWhenNoRelevantMemory()
    {
        var result = new RetrievalAutomaticInjector(new PetMemoryStore(Path.Combine(TestRoot(), "memory")))
            .RetrieveForUserTurn(Guid.NewGuid(), "", topK: 3);

        Assert.Empty(result.ContextLines);
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-retrieval-injector-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
