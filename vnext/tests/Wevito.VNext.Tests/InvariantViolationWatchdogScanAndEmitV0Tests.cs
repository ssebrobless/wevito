using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Tests;

public sealed class InvariantViolationWatchdogScanAndEmitV0Tests
{
    [Fact]
    public void ScanAndEmit_composes_scan_and_emit_under_normal_operation()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));

        var manualWatchdog = fixture.CreateWatchdog();
        var expectedResults = manualWatchdog.Scan(fixture.Now);
        manualWatchdog.EmitInvariantCheckFailedPackets(expectedResults, fixture.Now);
        var expectedRows = fixture.NewInvariantRows();

        var secondFixture = WatchdogFixture.Create();
        secondFixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), secondFixture.Now.AddMinutes(-1));

        var actualResults = secondFixture.CreateWatchdog().ScanAndEmit(secondFixture.Now);

        var actualRows = secondFixture.NewInvariantRows();
        Assert.Single(expectedRows);
        Assert.Equal(expectedResults.Count(result => result.Triggered), actualResults.Count(result => result.Triggered));
        Assert.Equal(expectedRows.Count, actualRows.Count);
        Assert.All(actualRows, row => Assert.Equal(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, row.PacketKind));
    }

    [Fact]
    public void ScanAndEmit_emits_zero_packets_when_killswitch_tripped()
    {
        var fixture = WatchdogFixture.Create(killSwitchActive: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().ScanAndEmit(fixture.Now);

        Assert.Empty(results);
        Assert.Empty(fixture.NewInvariantRows());
    }

    [Fact]
    public void ScanAndEmit_emits_zero_packets_when_scan_returns_empty()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, OperationId(1), fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().ScanAndEmit(fixture.Now);

        Assert.All(results, result => Assert.False(result.Triggered));
        Assert.Empty(fixture.NewInvariantRows());
    }

    private static string OperationId(int value)
    {
        return $"apply-{value.ToString("x32")}";
    }

    private static EvidencePacket Packet(string kind, string operationId, DateTimeOffset timestamp)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            TaskCardId: null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"operation_id={operationId}",
            Status: "Completed");
    }

    private sealed class WatchdogFixture
    {
        private WatchdogFixture(bool killSwitchActive)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-watchdog-scan-and-emit-v0", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            DatabasePath = Path.Combine(root, "ledger.sqlite");
            Ledger = new AuditLedgerService(DatabasePath);
            Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InvariantViolationWatchdog.EnabledSetting] = bool.TrueString,
                [InvariantViolationWatchdog.V0InvariantCheckEmitEnabledSetting] = bool.TrueString
            };

            if (killSwitchActive)
            {
                Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
            }
        }

        public string DatabasePath { get; }

        public AuditLedgerService Ledger { get; }

        public DateTimeOffset Now { get; }

        public Dictionary<string, string> Settings { get; }

        public static WatchdogFixture Create(bool killSwitchActive = false)
        {
            return new WatchdogFixture(killSwitchActive);
        }

        public InvariantViolationWatchdog CreateWatchdog()
        {
            return new InvariantViolationWatchdog(
                DatabasePath,
                Ledger,
                new KillSwitchService(() => Settings),
                () => Settings);
        }

        public long Record(string kind, string operationId, DateTimeOffset timestamp)
        {
            return Ledger.Record(Packet(kind, operationId, timestamp));
        }

        public IReadOnlyList<AuditLedgerRow> NewInvariantRows()
        {
            return Ledger
                .Snapshot(Now.AddHours(-1), Now.AddHours(1))
                .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
