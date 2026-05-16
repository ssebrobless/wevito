namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class ChatContextBudgetServiceTests
{
    [Fact]
    public void TracksTokensAccurately()
    {
        var root = TestRoot();
        var store = new ChatHistoryStore(Path.Combine(root, "chat.sqlite"));
        var sessionId = store.CreateSession();
        store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "user", "hello", null, null, DateTimeOffset.UtcNow, "", 12));

        var service = new ChatContextBudgetService(store);
        var snapshot = service.Snapshot(sessionId);

        Assert.Equal(12, snapshot.TurnsTokensUsed);
        Assert.Equal(ChatContextBudgetService.TurnsBudget - 12, snapshot.RemainingTurnsBudget);
    }

    [Fact]
    public void EmitsPressurePacketAt80Percent()
    {
        var root = TestRoot();
        var store = new ChatHistoryStore(Path.Combine(root, "chat.sqlite"));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var sessionId = store.CreateSession();
        store.AppendTurn(new ChatTurn(sessionId, Guid.NewGuid(), "user", "big", null, null, DateTimeOffset.UtcNow, "", 56_000));

        var service = new ChatContextBudgetService(store, ledger);
        var snapshot = service.Snapshot(sessionId, DateTimeOffset.UtcNow);

        Assert.True(snapshot.IsPressureThresholdReached);
        Assert.Contains(ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1)), row => row.PacketKind == ChatContextBudgetService.ContextBudgetPressurePacketKind);
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-context-budget-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
