using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CodexPhaseQueueServiceTests
{
    [Fact]
    public void QueuePersistsAcrossRestart()
    {
        using var temp = CodexLoopTempWorkspace.Create();
        var service = new CodexPhaseQueueService(temp.DocsRoot);
        service.Enqueue(Entry("C-PHASE 114"));

        var reloaded = new CodexPhaseQueueService(temp.DocsRoot);

        Assert.Single(reloaded.ReadQueue());
        Assert.Equal("C-PHASE 114", reloaded.ReadQueue()[0].PhaseId);
    }

    [Fact]
    public void HistoryIsAppendOnly()
    {
        using var temp = CodexLoopTempWorkspace.Create();
        var service = new CodexPhaseQueueService(temp.DocsRoot);

        service.Enqueue(Entry("C-PHASE 114"));
        var next = service.DequeueNext(DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
        service.CompletePhase(next!.PhaseId, DateTimeOffset.Parse("2026-05-15T12:05:00Z"), "green");

        var history = service.ReadHistory();
        Assert.Equal(2, history.Count);
        Assert.Throws<InvalidOperationException>(() => service.InjectFront(Entry("C-PHASE 114")));
    }

    [Fact]
    public void InjectMovesPhaseToFront()
    {
        using var temp = CodexLoopTempWorkspace.Create();
        var service = new CodexPhaseQueueService(temp.DocsRoot);
        service.Enqueue(Entry("C-PHASE 115"));
        service.InjectFront(Entry("C-PHASE 114"));

        var queue = service.ReadQueue();

        Assert.Equal("C-PHASE 114", queue[0].PhaseId);
        Assert.Equal("C-PHASE 115", queue[1].PhaseId);
    }

    private static CodexPhaseQueueEntry Entry(string phaseId) => new(
        phaseId,
        $"docs/{phaseId}.md",
        $"claude-implementation/{phaseId.ToLowerInvariant()}",
        AutoContinue: true,
        DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
}

internal sealed class CodexLoopTempWorkspace : IDisposable
{
    private CodexLoopTempWorkspace(string root)
    {
        Root = root;
        DocsRoot = Path.Combine(root, "docs");
        Directory.CreateDirectory(DocsRoot);
    }

    public string Root { get; }

    public string DocsRoot { get; }

    public static CodexLoopTempWorkspace Create() => new(Path.Combine(Path.GetTempPath(), $"wevito-codex-loop-{Guid.NewGuid():N}"));

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
