using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class ProposalQualityMetricsServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Snapshot_KillSwitchActive_ReturnsEmptySnapshot()
    {
        var fixture = Fixture.Create(killSwitchActive: true);

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.Equal("kill_switch=true", snapshot.Reason);
        Assert.Equal("none", snapshot.LatestReplayResultKind);
        Assert.Empty(snapshot.PacketCountsByKind);
    }

    [Fact]
    public void Snapshot_EmptyLedgerAndMissingArtifactRoot_ReturnsNoRows()
    {
        var fixture = Fixture.Create(createArtifactRoot: false);

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.Equal("no_rows", snapshot.Reason);
        Assert.Equal(EvalGateManifest.Default().Gates, snapshot.LatestEvalGatesMissing);
    }

    [Fact]
    public void Snapshot_JudgeCritiqueSummary_ExtractsRuleCounts()
    {
        var fixture = Fixture.Create();
        fixture.Record(SelfImprovementPacketKinds.JudgeCritique, summary: JsonSerializer.Serialize(new
        {
            operation_id = fixture.OperationId,
            rules_evaluated = 6,
            rules_passed = 6
        }, JsonDefaults.Options));

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.Equal(6, snapshot.JudgeRulesEvaluated);
        Assert.Equal(6, snapshot.JudgeRulesPassed);
        Assert.Equal(1, snapshot.PacketCountsByKind[SelfImprovementPacketKinds.JudgeCritique]);
    }

    [Fact]
    public void Snapshot_ApplyRefusedNotImplemented_CountsRows()
    {
        var fixture = Fixture.Create();
        fixture.Record(
            SelfImprovementPacketKinds.ApplyRefused,
            summary: $"operation {fixture.OperationId}",
            status: "Refused",
            error: SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.Equal(1, snapshot.ApplyRefusedNotImplementedCount);
    }

    [Fact]
    public void Snapshot_EvalGatesPartial_ComputesPresentAndMissing()
    {
        var fixture = Fixture.Create();
        var evalPath = Path.Combine(fixture.ArtifactRoot, fixture.OperationId, "eval.json");
        Directory.CreateDirectory(Path.GetDirectoryName(evalPath)!);
        File.WriteAllText(evalPath, JsonSerializer.Serialize(new
        {
            results = new Dictionary<string, object>
            {
                [EvalGateManifest.Build] = new { status = "Passed" },
                [EvalGateManifest.UnitTests] = new { status = "Passed" }
            }
        }, JsonDefaults.Options));
        fixture.Record(
            SelfImprovementPacketKinds.EvalCompleted,
            artifactPath: evalPath,
            summary: $"eval complete for {fixture.OperationId}");

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.Equal("ok", snapshot.Reason);
        Assert.Equal([EvalGateManifest.Build, EvalGateManifest.UnitTests], snapshot.LatestEvalGatesPresent);
        Assert.Equal(9, snapshot.LatestEvalGatesMissing.Count);
        Assert.Contains(EvalGateManifest.Rollback, snapshot.LatestEvalGatesMissing);
    }

    [Fact]
    public void Snapshot_AwaitingApprovalScopeHashStatusSnapshotAndReplay_AreReadOnlyMetrics()
    {
        var fixture = Fixture.Create();
        var operationRoot = Path.Combine(fixture.ArtifactRoot, fixture.OperationId);
        Directory.CreateDirectory(operationRoot);
        var awaitingPath = Path.Combine(operationRoot, "apply-awaiting-approval.json");
        File.WriteAllText(awaitingPath, JsonSerializer.Serialize(new
        {
            operationId = fixture.OperationId,
            scopeHash = new string('a', 64)
        }, JsonDefaults.Options));
        fixture.Record(
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            artifactPath: awaitingPath,
            summary: JsonSerializer.Serialize(new { operation_id = fixture.OperationId }, JsonDefaults.Options),
            status: "WaitingForApproval");
        var snapshotPath = Path.Combine(operationRoot, "snapshot.json");
        File.WriteAllText(snapshotPath, $"{{\"operation_id\":\"{fixture.OperationId}\"}}");
        File.SetLastWriteTimeUtc(snapshotPath, Now.UtcDateTime.AddDays(-2));
        File.WriteAllText(Path.Combine(operationRoot, "replay-result.json"), JsonSerializer.Serialize(new
        {
            OperationId = fixture.OperationId,
            ResultKind = "Identical",
            ReplayedAtUtc = Now
        }, JsonDefaults.Options));

        var snapshot = fixture.Service.Snapshot(fixture.OperationId, Now);

        Assert.True(snapshot.LatestScopeHashFormatValid);
        Assert.Equal("WaitingForApproval", snapshot.LatestAwaitingApprovalStatus);
        Assert.Equal("Identical", snapshot.LatestReplayResultKind);
        Assert.True(snapshot.SnapshotAgeDays is >= 1.9 and <= 2.1);
    }

    [Fact]
    public void Service_HasNoAuditLedgerServiceField()
    {
        var fields = typeof(ProposalQualityMetricsService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.DoesNotContain(fields, field => field.FieldType == typeof(AuditLedgerService));
    }

    private sealed class Fixture
    {
        private Fixture(string root, string artifactRoot, string operationId, AuditLedgerService ledger, ProposalQualityMetricsService service)
        {
            Root = root;
            ArtifactRoot = artifactRoot;
            OperationId = operationId;
            Ledger = ledger;
            Service = service;
        }

        public string Root { get; }
        public string ArtifactRoot { get; }
        public string OperationId { get; }
        public AuditLedgerService Ledger { get; }
        public ProposalQualityMetricsService Service { get; }

        public static Fixture Create(bool killSwitchActive = false, bool createArtifactRoot = true)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-proposal-quality", Guid.NewGuid().ToString("N"));
            var artifactRoot = Path.Combine(root, "vnext", "artifacts");
            if (createArtifactRoot)
            {
                Directory.CreateDirectory(artifactRoot);
            }

            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            _ = ledger.Snapshot(Now.AddHours(-1), Now.AddHours(1));
            var operationId = $"operation-{Guid.NewGuid():N}";
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            var service = new ProposalQualityMetricsService(databasePath, artifactRoot, new KillSwitchService(() => settings));
            return new Fixture(root, artifactRoot, operationId, ledger, service);
        }

        public void Record(
            string packetKind,
            string artifactPath = "",
            string? summary = null,
            string status = "Completed",
            string error = "")
        {
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                packetKind,
                Guid.NewGuid(),
                Now,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: artifactPath,
                Summary: summary ?? $"operation {OperationId}",
                Status: status,
                Error: error));
        }
    }
}
