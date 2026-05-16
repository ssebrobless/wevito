namespace Wevito.VNext.Core.Tools;

using Wevito.VNext.Core;

public sealed record RetrieveFromMemoryToolResult(
    IReadOnlyList<string> Rows,
    IReadOnlyList<double> Scores);

public sealed class RetrieveFromMemoryTool
{
    private readonly PetMemoryStore _memoryStore;

    public RetrieveFromMemoryTool(PetMemoryStore? memoryStore = null)
    {
        _memoryStore = memoryStore ?? new PetMemoryStore();
    }

    public RetrieveFromMemoryToolResult Run(Guid sessionId, string query, int topK = 5, string kindFilter = "")
    {
        var results = _memoryStore.Search(sessionId, query, kindFilter ?? "", Math.Max(1, topK));
        return new RetrieveFromMemoryToolResult(
            results.Select(result => result.Example.Content).ToList(),
            results.Select(result => result.Score).ToList());
    }
}
