using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;

namespace Wevito.VNext.Tests;

public sealed class InvariantViolationWatchdogV0Tests
{
    private const string V0CompletedWithoutApplied = "v0_completed_without_applied";
    private const string V0RolledBackFollowedByCompleted = "v0_rolled_back_followed_by_completed";
    private const string ExplicitRollbackCompletedWithoutStarted = "explicit_rollback_completed_without_started";

    [Fact]
    public void V0_R1_happy_path()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, operationId, fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0CompletedWithoutApplied);
    }

    [Fact]
    public void V0_R1_fail_completed_without_applied()
    {
        var fixture = WatchdogFixture.Create();
        var rowId = fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertFailContains(results, V0CompletedWithoutApplied, $"row_id={rowId}");
    }

    [Fact]
    public void V0_R1_cross_op_isolation()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, OperationId(1), fixture.Now.AddMinutes(-2));
        var rowId = fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(2), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertFailContains(results, V0CompletedWithoutApplied, $"row_id={rowId}");
        AssertFailContains(results, V0CompletedWithoutApplied, OperationId(2));
    }

    [Fact]
    public void V0_R1_empty_ledger()
    {
        var fixture = WatchdogFixture.Create();
        fixture.EnsureLedgerExists();

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0CompletedWithoutApplied);
    }

    [Fact]
    public void V0_R2_happy_path_rollback_only()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, operationId, fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0RolledBack, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0RolledBackFollowedByCompleted);
    }

    [Fact]
    public void V0_R2_happy_path_two_ops()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, OperationId(1), fixture.Now.AddMinutes(-4));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-3));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, OperationId(2), fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0RolledBack, OperationId(2), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0RolledBackFollowedByCompleted);
    }

    [Fact]
    public void V0_R2_fail_completed_after_rollback()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, operationId, fixture.Now.AddMinutes(-3));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0RolledBack, operationId, fixture.Now.AddMinutes(-2));
        var rowId = fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertFailContains(results, V0RolledBackFollowedByCompleted, $"row_id={rowId}");
    }

    [Fact]
    public void V0_R2_completed_before_rollback_passes_R2()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, operationId, fixture.Now.AddMinutes(-3));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, operationId, fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0RolledBack, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0RolledBackFollowedByCompleted);
    }

    [Fact]
    public void V0_R3_happy_path()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackStarted, operationId, fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, ExplicitRollbackCompletedWithoutStarted);
    }

    [Fact]
    public void V0_R3_fail_completed_without_started()
    {
        var fixture = WatchdogFixture.Create();
        var rowId = fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted, OperationId(1), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertFailContains(results, ExplicitRollbackCompletedWithoutStarted, $"row_id={rowId}");
    }

    [Fact]
    public void V0_R3_refused_not_counted_as_start()
    {
        var fixture = WatchdogFixture.Create();
        var operationId = OperationId(1);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackRefused, operationId, fixture.Now.AddMinutes(-2));
        var rowId = fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted, operationId, fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertFailContains(results, ExplicitRollbackCompletedWithoutStarted, $"row_id={rowId}");
    }

    [Fact]
    public void Empty_ledger_passes_all_three_v0_rules()
    {
        var fixture = WatchdogFixture.Create();
        fixture.EnsureLedgerExists();

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertPass(results, V0CompletedWithoutApplied);
        AssertPass(results, V0RolledBackFollowedByCompleted);
        AssertPass(results, ExplicitRollbackCompletedWithoutStarted);
    }

    [Fact]
    public void KillSwitch_active_returns_blocked_result()
    {
        var fixture = WatchdogFixture.Create(killSwitchActive: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        Assert.Empty(results);
        Assert.Empty(fixture.NewInvariantRows());
    }

    [Fact]
    public void Emit_default_off_writes_no_packet()
    {
        var fixture = WatchdogFixture.Create(v0EmitEnabled: false);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-1));
        var watchdog = fixture.CreateWatchdog();
        var results = watchdog.Scan(fixture.Now);

        watchdog.EmitInvariantCheckFailedPackets(results, fixture.Now.AddSeconds(1));

        Assert.NotEmpty(results.Where(result => result.Check.Id.StartsWith("v0_", StringComparison.OrdinalIgnoreCase) && result.Triggered));
        Assert.Empty(fixture.NewInvariantRows());
    }

    [Fact]
    public void Emit_flag_on_writes_one_packet_per_failing_rule()
    {
        var fixture = WatchdogFixture.Create(v0EmitEnabled: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(1), fixture.Now.AddMinutes(-5));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Applied, OperationId(2), fixture.Now.AddMinutes(-4));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0RolledBack, OperationId(2), fixture.Now.AddMinutes(-3));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, OperationId(2), fixture.Now.AddMinutes(-2));
        fixture.Record(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted, OperationId(3), fixture.Now.AddMinutes(-1));
        var watchdog = fixture.CreateWatchdog();
        var results = watchdog.Scan(fixture.Now);

        watchdog.EmitInvariantCheckFailedPackets(results, fixture.Now.AddSeconds(1));

        var rows = fixture.NewInvariantRows();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.False(row.DidMutate);
            Assert.False(row.DidUseNetwork);
            Assert.False(row.DidUseHostedAi);
            Assert.False(row.DidUseLocalModel);
            Assert.Equal(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, row.PacketKind);
        });
    }

    [Fact]
    public void Plain_language_explainer_returns_registered_sentence_for_new_kind()
    {
        var explainer = new PlainLanguageExplainer();

        var sentence = explainer.ExplainPacketKind(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed);

        Assert.Equal(
            "Wevito recorded a self-improvement apply-v0 sequence invariant violation found by the watchdog.",
            sentence);
        Assert.Contains(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, PlainLanguageExplainer.KnownPacketKinds);
    }

    [Fact]
    public void Watchdog_source_has_no_update_or_delete_sql()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string OperationId(int value)
    {
        return $"apply-{value.ToString("x32")}";
    }

    private static void AssertPass(IReadOnlyList<InvariantCheckResult> results, string checkId)
    {
        var result = Assert.Single(results.Where(candidate => candidate.Check.Id == checkId));
        Assert.False(result.Triggered);
        Assert.Equal("no violation detected", result.EvidenceSummary);
    }

    private static void AssertFailContains(IReadOnlyList<InvariantCheckResult> results, string checkId, string expectedText)
    {
        var result = Assert.Single(results.Where(candidate => candidate.Check.Id == checkId));
        Assert.True(result.Triggered);
        Assert.Equal(MaturityClockResetReason.InvariantViolation, result.Check.Reason);
        Assert.Contains(expectedText, result.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
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

    private static string SourcePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Invariants", "InvariantViolationWatchdog.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate InvariantViolationWatchdog.cs from test base directory.");
    }

    private sealed class WatchdogFixture
    {
        private WatchdogFixture(bool flagEnabled, bool v0EmitEnabled, bool killSwitchActive)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-watchdog-v0", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            DatabasePath = Path.Combine(root, "ledger.sqlite");
            Ledger = new AuditLedgerService(DatabasePath);
            Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InvariantViolationWatchdog.EnabledSetting] = flagEnabled.ToString(),
                [InvariantViolationWatchdog.V0InvariantCheckEmitEnabledSetting] = v0EmitEnabled.ToString()
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

        public static WatchdogFixture Create(bool flagEnabled = true, bool v0EmitEnabled = false, bool killSwitchActive = false)
        {
            return new WatchdogFixture(flagEnabled, v0EmitEnabled, killSwitchActive);
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

        public void EnsureLedgerExists()
        {
            _ = Ledger.Snapshot(Now.AddHours(-1), Now.AddHours(1));
        }
    }
}
