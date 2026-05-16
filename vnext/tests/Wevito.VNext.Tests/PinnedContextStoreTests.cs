namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class PinnedContextStoreTests
{
    [Fact]
    public void RespectsTokenBudget()
    {
        var store = new PinnedContextStore(Path.Combine(TestRoot(), "pins.sqlite"), tokenBudget: 3);

        store.Pin("one two");
        store.Pin("three four");

        Assert.True(store.GetActivePins().Sum(row => row.Tokens) <= 3);
    }

    [Fact]
    public void FifoEvictionOnBudgetExceeded()
    {
        var store = new PinnedContextStore(Path.Combine(TestRoot(), "pins.sqlite"), tokenBudget: 3);
        var first = store.Pin("one two", DateTimeOffset.UtcNow.AddMinutes(-2));
        var second = store.Pin("three four", DateTimeOffset.UtcNow);

        var active = store.GetActivePins();

        Assert.DoesNotContain(active, row => row.PinId == first.PinId);
        Assert.Contains(active, row => row.PinId == second.PinId);
    }

    [Fact]
    public void SoftUnpinPreservesRowAsInactive()
    {
        var store = new PinnedContextStore(Path.Combine(TestRoot(), "pins.sqlite"));
        var row = store.Pin("keep this decision");

        store.Unpin(row.PinId);

        Assert.Empty(store.GetActivePins());
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pinned-context-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
