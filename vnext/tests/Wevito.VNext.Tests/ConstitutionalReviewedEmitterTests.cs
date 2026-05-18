using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ConstitutionalReviewedEmitterTests
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "wevito-constitutional-reviewed-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Emit_WritesHonestConstitutionalReviewedPacket()
    {
        var ledgerPath = Path.Combine(_tempRoot, "audit", "ledger.sqlite");
        var ledger = new AuditLedgerService(ledgerPath);
        var emitter = new ConstitutionalReviewedEmitter(ledger);
        var input = new ConstitutionalDecisionInput(
            ScopeId: "sprite-repair-triage",
            ExperimentKind: "sprite-repair-batch-proposal",
            ScopeEnabled: true,
            RequestsNetwork: false,
            ScopeAllowsNetwork: false,
            RequestsHostedAi: false,
            ExperimentRegistryIsEmpty: true);
        var outcome = new ConstitutionalDecisionOutcome.Blocked(DefaultDenyRule.EmptyRegistryReason);
        var timestamp = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        emitter.Emit(input, outcome, timestamp);

        var row = Assert.Single(ledger.Snapshot(timestamp.AddMinutes(-1), timestamp.AddMinutes(1)));
        Assert.Equal(SelfImprovementPacketKinds.ConstitutionalReviewed, row.PacketKind);
        Assert.False(row.DidMutate);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.Equal("Blocked", row.Status);
        Assert.Equal(DefaultDenyRule.EmptyRegistryReason, row.Error);
    }
}
