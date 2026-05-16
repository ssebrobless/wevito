using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CodexCompileThrottleServiceTests
{
    [Fact]
    public void Sets2CoresWhenUserActive()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new CodexCompileThrottleService(
            clock: () => now,
            lastInputReader: () => now.AddSeconds(-2));

        var decision = service.Decide(Running(), logicalProcessorCount: 8);

        Assert.True(decision.IsThrottled);
        Assert.Equal(2, decision.ProcessorCount);
    }

    [Fact]
    public void Sets6CoresWhenUserIdle()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new CodexCompileThrottleService(
            clock: () => now,
            lastInputReader: () => now.AddMinutes(-6));

        var decision = service.Decide(Running(), logicalProcessorCount: 8);

        Assert.False(decision.IsThrottled);
        Assert.Equal(6, decision.ProcessorCount);
    }

    [Fact]
    public void EmitsThrottlePacketOnLaunch()
    {
        var audit = new AuditLedgerService(BuildTempLedger());
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new CodexCompileThrottleService(
            clock: () => now,
            lastInputReader: () => now,
            auditLedgerService: audit);

        var decision = service.Decide(Running(), logicalProcessorCount: 8);
        var rows = audit.Snapshot(now.AddMinutes(-1), now.AddMinutes(1));

        Assert.True(decision.IsThrottled);
        Assert.Contains(rows, row => row.PacketKind == CodexCompileThrottleService.CodexCompileThrottledPacketKind);
    }

    private static CodexLoopStatusSnapshot Running()
    {
        return new CodexLoopStatusSnapshot("running", "C-PHASE 121", DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
    }

    private static string BuildTempLedger()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-codex-throttle-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "audit.jsonl");
    }
}
