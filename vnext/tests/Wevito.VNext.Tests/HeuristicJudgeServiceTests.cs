using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Judge;

namespace Wevito.VNext.Tests;

public sealed class HeuristicJudgeServiceTests
{
    [Fact]
    public void Critique_WithValidFixture_AllRulesPassesAndWritesOnePacket()
    {
        var fixture = JudgeFixture.Create();

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        Assert.Equal(6, findings.Count);
        Assert.All(findings, finding => Assert.True(finding.Passed, finding.Rule.Id));
        AssertJudgePacketCount(fixture, 1);
    }

    [Fact]
    public void Critique_WithControlledBadFixture_EachRuleCanFail()
    {
        var fixture = JudgeFixture.Create(
            didMutate: true,
            applyRunner: "implemented",
            scopeHash: "BAD",
            useOutsideDryRunPath: true,
            mismatchSourceHash: true,
            omitEvalGate: EvalGateManifest.Rollback);

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        AssertFailed(findings, "rule_did_mutate_false");
        AssertFailed(findings, "rule_apply_runner_not_implemented");
        AssertFailed(findings, "rule_scope_hash_present_format");
        AssertFailed(findings, "rule_artifact_paths_under_artifacts");
        AssertFailed(findings, "rule_proposal_source_hashes_match_live");
        AssertFailed(findings, "rule_eval_lists_every_manifest_gate");
        AssertJudgePacketCount(fixture, 1);
    }

    [Fact]
    public void Critique_WithKillSwitchActive_ReturnsEmptyAndWritesNothing()
    {
        var fixture = JudgeFixture.Create(killSwitchActive: true);

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        Assert.Empty(findings);
        AssertJudgePacketCount(fixture, 0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void Critique_WithFlagOffOrAbsent_ReturnsEmptyAndWritesNothing(bool? enabled)
    {
        var fixture = JudgeFixture.Create(flagEnabled: enabled);

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        Assert.Empty(findings);
        AssertJudgePacketCount(fixture, 0);
    }

    [Fact]
    public void Critique_WritesExactlyOnePacketPerCallRegardlessOfFindingCount()
    {
        var fixture = JudgeFixture.Create(didMutate: true, applyRunner: "implemented", scopeHash: "");

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        Assert.True(findings.Count(finding => !finding.Passed) >= 3);
        AssertJudgePacketCount(fixture, 1);
    }

    [Fact]
    public void Source_DoesNotReferenceModelOrEvalStoresOrWriteSql()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("IModelAdapter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Source_WritePathReferencesOnlyJudgeCritiquePacketKind()
    {
        var source = File.ReadAllLines(SourcePath());
        var recordLine = Array.FindIndex(source, line => line.Contains("_ledger.Record", StringComparison.Ordinal));

        Assert.True(recordLine >= 0);
        var writeBlock = string.Join(Environment.NewLine, source.Skip(recordLine).Take(25));
        Assert.Contains("SelfImprovementPacketKinds.JudgeCritique", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyAwaitingApproval", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ApplyRefused", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ProposalDrafted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.DryRunCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.EvalCompleted", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.RollbackVerified", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.ConstitutionalReviewed", writeBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds.MaturityClockReset", writeBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void JudgeCritique_IsKnownToPlainLanguageExplainer()
    {
        Assert.Contains(SelfImprovementPacketKinds.JudgeCritique, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Equal(
            "Wevito ran a deterministic critique over a self-improvement proposal.",
            new PlainLanguageExplainer().ExplainPacketKind(SelfImprovementPacketKinds.JudgeCritique));
    }

    private static void AssertFailed(IReadOnlyList<HeuristicJudgeFinding> findings, string ruleId)
    {
        var finding = Assert.Single(findings.Where(candidate => candidate.Rule.Id == ruleId));
        Assert.False(finding.Passed);
        Assert.False(string.IsNullOrWhiteSpace(finding.EvidenceSummary));
    }

    private static void AssertJudgePacketCount(JudgeFixture fixture, int expected)
    {
        var packets = fixture.Ledger.Snapshot(fixture.Now.AddHours(-1), fixture.Now.AddHours(1))
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.JudgeCritique, StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(expected, packets.Length);
        foreach (var packet in packets)
        {
            Assert.False(packet.DidUseNetwork);
            Assert.False(packet.DidUseHostedAi);
            Assert.False(packet.DidUseLocalModel);
            Assert.False(packet.DidMutate);
        }
    }

    private static string SourcePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "vnext",
                "src",
                "Wevito.VNext.Core",
                "SelfImprovement",
                "Judge",
                "HeuristicJudgeService.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate HeuristicJudgeService.cs.");
    }

    private sealed class JudgeFixture
    {
        private JudgeFixture(string databasePath, string operationId, DateTimeOffset now, HeuristicJudgeService service, AuditLedgerService ledger)
        {
            DatabasePath = databasePath;
            OperationId = operationId;
            Now = now;
            Service = service;
            Ledger = ledger;
        }

        public string DatabasePath { get; }
        public string OperationId { get; }
        public DateTimeOffset Now { get; }
        public HeuristicJudgeService Service { get; }
        public AuditLedgerService Ledger { get; }

        public static JudgeFixture Create(
            bool? flagEnabled = true,
            bool killSwitchActive = false,
            bool didMutate = false,
            string applyRunner = "not_implemented_in_v0",
            string? scopeHash = null,
            bool useOutsideDryRunPath = false,
            bool mismatchSourceHash = false,
            string? omitEvalGate = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-heuristic-judge-tests", Guid.NewGuid().ToString("N"));
            var repoRoot = Path.Combine(root, "repo");
            var artifactsRoot = Path.Combine(repoRoot, "vnext", "artifacts", "supervised-improvement-pilot");
            var operationId = $"operation-{Guid.NewGuid():N}";
            var operationRoot = Path.Combine(artifactsRoot, operationId);
            Directory.CreateDirectory(operationRoot);
            var sourceRelativePath = Path.Combine("docs", "source.txt");
            var sourcePath = Path.Combine(repoRoot, sourceRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllText(sourcePath, "source-v1");
            var sourceHash = mismatchSourceHash ? new string('0', 64) : Sha256(sourcePath);
            var proposalPath = Path.Combine(operationRoot, "proposal.json");
            var dryRunPath = useOutsideDryRunPath
                ? Path.Combine(root, "outside", "dry-run.json")
                : Path.Combine(operationRoot, "dry-run.json");
            Directory.CreateDirectory(Path.GetDirectoryName(dryRunPath)!);
            var evalPath = Path.Combine(operationRoot, "eval.json");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(proposalPath, JsonSerializer.Serialize(new
            {
                didMutate,
                sourceHashes = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [sourceRelativePath.Replace('\\', '/')] = sourceHash
                }
            }, jsonOptions));
            File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new { didMutate }, jsonOptions));
            File.WriteAllText(evalPath, JsonSerializer.Serialize(new
            {
                didMutate,
                results = EvalGateManifest.Default().Gates
                    .Where(gate => !gate.Equals(omitEvalGate, StringComparison.Ordinal))
                    .ToDictionary(gate => gate, _ => new { status = "Passed", reason = "" }, StringComparer.Ordinal)
            }, jsonOptions));
            var awaitingPath = Path.Combine(operationRoot, "apply-awaiting-approval.json");
            File.WriteAllText(awaitingPath, JsonSerializer.Serialize(new
            {
                operationId,
                scopeHash = scopeHash ?? new string('a', 64),
                proposalPath,
                dryRunPath,
                evalPath,
                applyRunner
            }, jsonOptions));

            var now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyAwaitingApproval,
                Guid.NewGuid(),
                now.AddMinutes(-1),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: awaitingPath,
                Summary: $"Awaiting approval for operation {operationId}.",
                Status: "WaitingForApproval"));

            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            if (flagEnabled.HasValue)
            {
                settings[HeuristicJudgeService.EnabledSetting] = flagEnabled.Value.ToString();
            }

            var service = new HeuristicJudgeService(
                databasePath,
                ledger,
                new KillSwitchService(() => settings),
                () => settings);
            return new JudgeFixture(databasePath, operationId, now, service, ledger);
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
    }
}
