using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ApplyPrerequisiteExplainerServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    private static readonly IReadOnlyDictionary<string, string> ExpectedPlainLanguage = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["KillSwitch armed"] = "Stop Everything must be off so we can react to a halt.",
        ["EvalGateRunner v1 enabled"] = "The cheap deterministic eval gates must be enabled before any apply.",
        ["Heuristic judge enabled"] = "The deterministic critique must have passed for the current proposal.",
        ["Snapshot signed and verified recently"] = "A signed snapshot of recent self-improvement audit rows must verify.",
        ["Held-out store contains >= 1 case"] = "At least one held-out evaluation case must exist.",
        ["In-distribution store contains >= 1 case"] = "At least one in-distribution evaluation case must exist.",
        ["Scope hash matches latest awaiting-approval artifact"] = "The proposal artifacts' content hashes must still match the live files.",
        ["Replay run within window"] = "A recent deterministic replay must have returned Identical.",
        ["Capability default-off audit"] = "No capability flag is unexpectedly enabled outside the documented allowlist.",
        ["Apply runner declared not implemented"] = "The v0 apply runner must remain explicitly not implemented."
    };

    [Fact]
    public void Explain_KillSwitchActive_ReturnsEmptyReasonAndWritesNothing()
    {
        var fixture = Fixture.Create(killSwitchActive: true);
        fixture.RecordPrerequisitePacket(allPassed: true);

        var explanation = fixture.Service.Explain(fixture.OperationId, Now);

        Assert.Empty(explanation.Entries);
        Assert.False(explanation.AllPassed);
        Assert.Equal("kill_switch=true", explanation.Reason);
        Assert.Single(fixture.Rows(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
    }

    [Fact]
    public void Explain_NoPacket_ReturnsNoPacketReason()
    {
        var fixture = Fixture.Create();

        var explanation = fixture.Service.Explain(fixture.OperationId, Now);

        Assert.Empty(explanation.Entries);
        Assert.False(explanation.AllPassed);
        Assert.Equal(ApplyPrerequisiteExplainerService.NoPacketReason, explanation.Reason);
    }

    [Fact]
    public void Explain_WithPacketAndArtifact_ReturnsTenEntriesWithPlainLanguage()
    {
        var fixture = Fixture.Create();
        fixture.RecordPrerequisitePacket(allPassed: true);
        fixture.WritePrerequisiteArtifact(allPassed: true);

        var explanation = fixture.Service.Explain(fixture.OperationId, Now);

        Assert.Equal(fixture.OperationId, explanation.OperationId);
        Assert.True(explanation.AllPassed);
        Assert.Equal("", explanation.Reason);
        Assert.Equal(10, explanation.Entries.Count);
        foreach (var expected in ExpectedPlainLanguage)
        {
            var row = Assert.Single(explanation.Entries, entry => entry.Name == expected.Key);
            Assert.Equal(expected.Value, row.PlainLanguage);
            Assert.True(row.Passed);
        }
    }

    [Fact]
    public void Explain_WithPacketAndNoArtifact_CarriesAllPassedAndReportsDetailsUnavailable()
    {
        var fixture = Fixture.Create();
        fixture.RecordPrerequisitePacket(allPassed: true);

        var explanation = fixture.Service.Explain(fixture.OperationId, Now);

        Assert.Empty(explanation.Entries);
        Assert.True(explanation.AllPassed);
        Assert.Equal(ApplyPrerequisiteExplainerService.DetailsUnavailableReason, explanation.Reason);
    }

    [Fact]
    public void ExplainerService_DoesNotExposeRunnerLikeMethods()
    {
        var forbiddenNames = new[] { "Check", "Run", "Apply", "Execute", "Emit" };
        var declaredMethods = typeof(ApplyPrerequisiteExplainerService)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(declaredMethods, method => forbiddenNames.Contains(method, StringComparer.Ordinal));
    }

    private sealed class Fixture
    {
        private Fixture(
            string root,
            string artifactRoot,
            string databasePath,
            string operationId,
            AuditLedgerService ledger,
            ApplyPrerequisiteExplainerService service)
        {
            Root = root;
            ArtifactRoot = artifactRoot;
            DatabasePath = databasePath;
            OperationId = operationId;
            Ledger = ledger;
            Service = service;
        }

        public string Root { get; }
        public string ArtifactRoot { get; }
        public string DatabasePath { get; }
        public string OperationId { get; }
        public AuditLedgerService Ledger { get; }
        public ApplyPrerequisiteExplainerService Service { get; }

        public static Fixture Create(bool killSwitchActive = false)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-apply-prereq-explainer", Guid.NewGuid().ToString("N"));
            var artifactRoot = Path.Combine(root, "vnext", "artifacts");
            Directory.CreateDirectory(artifactRoot);
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            _ = ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1));
            var operationId = $"operation-{Guid.NewGuid():N}";
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            var service = new ApplyPrerequisiteExplainerService(
                databasePath,
                artifactRoot,
                new KillSwitchService(() => settings));
            return new Fixture(root, artifactRoot, databasePath, operationId, ledger, service);
        }

        public void RecordPrerequisitePacket(bool allPassed)
        {
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyPrerequisiteCheck,
                Guid.NewGuid(),
                Now,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: "",
                Summary: JsonSerializer.Serialize(new
                {
                    operation_id = OperationId,
                    checks_total = 10,
                    checks_passed = allPassed ? 10 : 9,
                    all_passed = allPassed
                }, JsonDefaults.Options),
                Status: allPassed ? "Completed" : "Refused"));
        }

        public void WritePrerequisiteArtifact(bool allPassed)
        {
            var operationRoot = Path.Combine(ArtifactRoot, "apply-prerequisites", OperationId);
            Directory.CreateDirectory(operationRoot);
            File.WriteAllText(Path.Combine(operationRoot, "prerequisite-check.json"), JsonSerializer.Serialize(new
            {
                operation_id = OperationId,
                all_passed = allPassed,
                entries = ExpectedPlainLanguage.Keys.Select(name => new
                {
                    name,
                    passed = true,
                    detail = $"detail for {name}"
                }).ToArray()
            }, JsonDefaults.Options));
        }

        public IReadOnlyList<AuditLedgerRow> Rows(string packetKind)
        {
            return Ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1))
                .Where(row => row.PacketKind.Equals(packetKind, StringComparison.Ordinal))
                .ToArray();
        }
    }
}
