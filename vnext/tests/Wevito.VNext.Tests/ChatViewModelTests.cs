using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ChatViewModelTests
{
    [Fact]
    public void RendersToolExpanderForToolCallTurn()
    {
        var model = new ChatViewModel();
        var session = Guid.NewGuid();

        model.RenderTurns([
            new ChatTurn(session, Guid.NewGuid(), "tool", "Tool dispatch is deferred.", "{\"tool\":\"localDocs\"}", "result-1", DateTimeOffset.UtcNow, "tool", 3)
        ]);

        var message = Assert.Single(model.Messages);
        Assert.True(message.IsToolCall);
        Assert.Equal("Tool: localDocs", message.Header);
    }

    [Fact]
    public void SearchFiltersByFtsHit()
    {
        var model = new ChatViewModel();
        var session = Guid.NewGuid();

        model.RenderSearchResults([
            new ChatTurn(session, Guid.NewGuid(), "assistant", "Found goose sprite notes.", null, null, DateTimeOffset.UtcNow, "test", 4)
        ]);

        Assert.Single(model.Messages);
        Assert.Contains("1 search hit", model.StatusText, StringComparison.OrdinalIgnoreCase);
    }
}
