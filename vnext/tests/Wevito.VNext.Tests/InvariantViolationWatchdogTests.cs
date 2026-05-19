using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Tests;

public sealed class InvariantViolationWatchdogTests
{
    [Fact]
    public void Scan_SilentMutationInReviewOnlyScope_TriggersSilentMutationDetectedAndWritesOneReset()
    {
        var fixture = WatchdogFixture.Create();
        var proposalCardId = Guid.NewGuid();
        fixture.Ledger.Record(Packet(
            SelfImprovementPacketKinds.DryRunCompleted,
            fixture.Now.AddMinutes(-1),
            proposalCardId,
            didMutate: true,
            summary: $"Completed review-only dry run for {AutonomousScopeService.SpriteRepairBatchProposalScopeId}."));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_silent_mutation_in_review_only_scope", MaturityClockResetReason.SilentMutationDetected);
        AssertResetCount(fixture, MaturityClockResetReason.SilentMutationDetected, 1);
    }

    [Fact]
    public void Scan_SilentNetwork_TriggersSilentNetworkDetected()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, fixture.Now.AddMinutes(-1), Guid.NewGuid(), didUseNetwork: true));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_silent_network", MaturityClockResetReason.SilentNetworkDetected);
        AssertResetCount(fixture, MaturityClockResetReason.SilentNetworkDetected, 1);
    }

    [Fact]
    public void Scan_SilentHostedAi_TriggersSilentHostedAiDetected()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, fixture.Now.AddMinutes(-1), Guid.NewGuid(), didUseHostedAi: true));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_silent_hosted_ai", MaturityClockResetReason.SilentHostedAiDetected);
        AssertResetCount(fixture, MaturityClockResetReason.SilentHostedAiDetected, 1);
    }

    [Fact]
    public void Scan_ApplyCompletedWithoutAwaitingApproval_TriggersInvariantViolation()
    {
        var fixture = WatchdogFixture.Create();
        var proposalCardId = Guid.NewGuid();
        var operationId = SupervisedImprovementLoop.BuildOperationId(ProposalCard(proposalCardId));
        fixture.Ledger.Record(Packet(
            SelfImprovementPacketKinds.ApplyCompleted,
            fixture.Now.AddMinutes(-1),
            Guid.NewGuid(),
            summary: $"Completed apply operation {operationId}."));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_apply_completed_without_awaiting_approval", MaturityClockResetReason.InvariantViolation);
        AssertResetCount(fixture, MaturityClockResetReason.InvariantViolation, 1);
    }

    [Fact]
    public void Scan_AwaitingApprovalWithoutPrecedingChain_TriggersInvariantViolation()
    {
        var fixture = WatchdogFixture.Create();
        var proposalCardId = Guid.NewGuid();
        var operationId = SupervisedImprovementLoop.BuildOperationId(ProposalCard(proposalCardId));
        fixture.Ledger.Record(Packet(
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            fixture.Now.AddMinutes(-1),
            Guid.NewGuid(),
            summary: $"Supervised self-improvement proposal is awaiting explicit user approval for operation {operationId}."));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_awaiting_approval_without_preceding_chain", MaturityClockResetReason.InvariantViolation);
        AssertResetCount(fixture, MaturityClockResetReason.InvariantViolation, 1);
    }

    [Fact]
    public void Scan_DuplicateAwaitingApproval_TriggersInvariantViolation()
    {
        var fixture = WatchdogFixture.Create();
        var proposalCardId = Guid.NewGuid();
        var operationId = SupervisedImprovementLoop.BuildOperationId(ProposalCard(proposalCardId));
        SeedPrecedingChain(fixture, proposalCardId);
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ApplyAwaitingApproval, fixture.Now.AddMinutes(-2), Guid.NewGuid(), summary: $"operation {operationId}"));
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ApplyAwaitingApproval, fixture.Now.AddMinutes(-1), Guid.NewGuid(), summary: $"operation {operationId}"));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        AssertTriggered(results, "rule_duplicate_awaiting_approval", MaturityClockResetReason.InvariantViolation);
        AssertResetCount(fixture, MaturityClockResetReason.InvariantViolation, 1);
    }

    [Fact]
    public void Scan_WithValidAwaitingApprovalChain_DoesNotTriggerPrecedingChainRule()
    {
        var fixture = WatchdogFixture.Create();
        var proposalCardId = Guid.NewGuid();
        var operationId = SupervisedImprovementLoop.BuildOperationId(ProposalCard(proposalCardId));
        SeedPrecedingChain(fixture, proposalCardId);
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ApplyAwaitingApproval, fixture.Now.AddMinutes(-1), Guid.NewGuid(), summary: $"operation {operationId}"));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        var chain = Assert.Single(results.Where(result => result.Check.Id == "rule_awaiting_approval_without_preceding_chain"));
        Assert.False(chain.Triggered);
    }

    [Fact]
    public void Scan_DuplicateTriggersAcrossTwoScans_WriteOnlyOnePacketPerReason()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, fixture.Now.AddMinutes(-1), Guid.NewGuid(), didUseNetwork: true));
        var watchdog = fixture.CreateWatchdog();

        watchdog.Scan(fixture.Now.AddSeconds(-1));
        watchdog.Scan(fixture.Now);

        AssertResetCount(fixture, MaturityClockResetReason.SilentNetworkDetected, 1);
    }

    [Fact]
    public void Scan_WithKillSwitchActive_ReturnsEmptyAndWritesNothing()
    {
        var fixture = WatchdogFixture.Create(killSwitchActive: true);
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, fixture.Now.AddMinutes(-1), Guid.NewGuid(), didUseNetwork: true));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        Assert.Empty(results);
        Assert.Empty(ResetRows(fixture));
    }

    [Fact]
    public void Scan_WithFlagFalseOrAbsent_ReturnsEmptyAndWritesNothing()
    {
        var absent = WatchdogFixture.Create(flagEnabled: null);
        absent.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, absent.Now.AddMinutes(-1), Guid.NewGuid(), didUseNetwork: true));
        var disabled = WatchdogFixture.Create(flagEnabled: false);
        disabled.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, disabled.Now.AddMinutes(-1), Guid.NewGuid(), didUseNetwork: true));

        Assert.Empty(absent.CreateWatchdog().Scan(absent.Now));
        Assert.Empty(disabled.CreateWatchdog().Scan(disabled.Now));
        Assert.Empty(ResetRows(absent));
        Assert.Empty(ResetRows(disabled));
    }

    [Fact]
    public void Scan_ExcludesMaturityClockResetRowsFromInputs()
    {
        var fixture = WatchdogFixture.Create();
        fixture.Ledger.Record(Packet(
            SelfImprovementPacketKinds.MaturityClockReset,
            fixture.Now.AddMinutes(-1),
            null,
            didUseNetwork: true,
            didUseHostedAi: true,
            didMutate: true,
            summary: nameof(MaturityClockResetReason.SilentNetworkDetected)));

        var results = fixture.CreateWatchdog().Scan(fixture.Now);

        Assert.All(results, result => Assert.False(result.Triggered));
        AssertResetCount(fixture, MaturityClockResetReason.SilentNetworkDetected, 1);
    }

    [Fact]
    public void Source_DoesNotIssueInsertUpdateOrDelete()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Source_WritePathReferencesOnlyMaturityClockResetPacketKind()
    {
        var source = File.ReadAllLines(SourcePath());
        var recordLine = Array.FindIndex(source, line => line.Contains("_auditLedgerService.Record", StringComparison.Ordinal));

        Assert.True(recordLine >= 0);
        var writeBlock = string.Join(Environment.NewLine, source.Skip(recordLine).Take(25));
        Assert.Contains("SelfImprovementPacketKinds.MaturityClockReset", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyAwaitingApproval", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyRefused", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ProposalDrafted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.DryRunCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.EvalCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.RollbackVerified", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ConstitutionalReviewed", writeBlock, StringComparison.Ordinal);
    }

    private static void SeedPrecedingChain(WatchdogFixture fixture, Guid proposalCardId)
    {
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ProposalDrafted, fixture.Now.AddMinutes(-5), proposalCardId));
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.ConstitutionalReviewed, fixture.Now.AddMinutes(-4), proposalCardId));
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.DryRunCompleted, fixture.Now.AddMinutes(-3), proposalCardId));
        fixture.Ledger.Record(Packet(SelfImprovementPacketKinds.EvalCompleted, fixture.Now.AddMinutes(-2), proposalCardId));
    }

    private static TaskCard ProposalCard(Guid id)
    {
        return new TaskCard(
            id,
            new TaskIntent(Guid.NewGuid(), "proposal", TaskIntentTargetMode.RouteToBestHelper, TaskKind: TaskKind.ReviewSprites, RequestedToolFamily: "sprite-repair-batch-proposal"),
            TaskCardStatus.Draft,
            ToolFamily: "sprite-repair-batch-proposal",
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-18T12:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-05-18T12:00:00Z"));
    }

    private static EvidencePacket Packet(
        string kind,
        DateTimeOffset timestamp,
        Guid? taskCardId,
        bool didUseNetwork = false,
        bool didUseHostedAi = false,
        bool didUseLocalModel = false,
        bool didMutate = false,
        string artifactPath = "",
        string summary = "test packet",
        string status = "Completed",
        string error = "")
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            taskCardId,
            timestamp,
            didUseNetwork,
            didUseHostedAi,
            didUseLocalModel,
            didMutate,
            artifactPath,
            summary,
            status,
            error);
    }

    private static void AssertTriggered(
        IReadOnlyList<InvariantCheckResult> results,
        string checkId,
        MaturityClockResetReason reason)
    {
        var result = Assert.Single(results.Where(candidate => candidate.Check.Id == checkId));
        Assert.True(result.Triggered);
        Assert.Equal(reason, result.Check.Reason);
        Assert.NotEqual("no violation detected", result.EvidenceSummary);
    }

    private static void AssertResetCount(
        WatchdogFixture fixture,
        MaturityClockResetReason reason,
        int expected)
    {
        Assert.Equal(expected, ResetRows(fixture).Count(row => row.Summary.Contains(reason.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    private static IReadOnlyList<AuditLedgerRow> ResetRows(WatchdogFixture fixture)
    {
        return fixture.Ledger
            .Snapshot(fixture.Now.AddHours(-1), fixture.Now.AddHours(1))
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.MaturityClockReset, StringComparison.OrdinalIgnoreCase))
            .ToArray();
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
        private WatchdogFixture(string databasePath, bool? flagEnabled, bool killSwitchActive)
        {
            DatabasePath = databasePath;
            Ledger = new AuditLedgerService(databasePath);
            Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (flagEnabled is not null)
            {
                Settings[InvariantViolationWatchdog.EnabledSetting] = flagEnabled.Value.ToString();
            }

            if (killSwitchActive)
            {
                Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
            }
        }

        public string DatabasePath { get; }

        public AuditLedgerService Ledger { get; }

        public DateTimeOffset Now { get; }

        public Dictionary<string, string> Settings { get; }

        public static WatchdogFixture Create(bool? flagEnabled = true, bool killSwitchActive = false)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-invariant-watchdog-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new WatchdogFixture(Path.Combine(root, "ledger.sqlite"), flagEnabled, killSwitchActive);
        }

        public InvariantViolationWatchdog CreateWatchdog()
        {
            return new InvariantViolationWatchdog(
                DatabasePath,
                Ledger,
                new KillSwitchService(() => Settings),
                () => Settings);
        }
    }
}
