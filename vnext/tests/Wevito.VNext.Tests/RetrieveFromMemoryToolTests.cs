namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;
using Wevito.VNext.Core.Tools;

public sealed class RetrieveFromMemoryToolTests
{
    [Fact]
    public void ReturnsTopKMatchingQuery()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-retrieve-memory-tool-tests", Guid.NewGuid().ToString("N"));
        var memory = new PetMemoryStore(Path.Combine(root, "memory"));
        var sessionId = Guid.NewGuid();
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "fox sprite plan", "chat-context");
        memory.AddExample(sessionId, RollingSummarizerService.MemoryKind, "snake cleanup plan", "chat-context");

        var result = new RetrieveFromMemoryTool(memory).Run(sessionId, "fox", topK: 1, kindFilter: RollingSummarizerService.MemoryKind);

        Assert.Single(result.Rows);
    }
}
