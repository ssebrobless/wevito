using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Judge;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerPrerequisiteCheckServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void DisabledFlag_ReturnsSyntheticEntryAndWritesNoPacket()
    {
        var fixture = Fixture.Create();

        var result = fixture.Service.Check(fixture.OperationId, Now);

        Assert.False(result.AllPassed);
        var entry = Assert.Single(result.Entries);
        Assert.Equal(ApplyRunnerPrerequisiteCheckService.EnabledSetting, entry.Name);
        Assert.Equal("false", entry.Detail);
        Assert.Empty(fixture.Rows(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
    }

    [Fact]
    public void KillSwitch_ReturnsBlockedEntriesAndWritesNoPacket()
    {
        var fixture = Fixture.Create(settings: EnabledSettings(), killSwitchActive: true);

        var result = fixture.Service.Check(fixture.OperationId, Now);

        Assert.False(result.AllPassed);
        Assert.Equal(10, result.Entries.Count);
        Assert.All(result.Entries, entry =>
        {
            Assert.False(entry.Passed);
            Assert.Equal("kill_switch=true", entry.Detail);
        });
        Assert.Empty(fixture.Rows(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
    }

    [Fact]
    public void HappyPath_AllPrereqsMet_WritesOneCompletedPacket()
    {
        var fixture = Fixture.Create(settings: EnabledSettings());
        fixture.SeedHappyPathEvidence();

        var result = fixture.Service.Check(fixture.OperationId, Now);

        Assert.True(result.AllPassed, string.Join(" | ", result.Entries.Where(entry => !entry.Passed).Select(entry => $"{entry.Name}: {entry.Detail}")));
        Assert.Equal(10, result.Entries.Count);
        Assert.All(result.Entries, entry => Assert.True(entry.Passed, entry.Detail));
        var packet = Assert.Single(fixture.Rows(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
        Assert.Equal("Completed", packet.Status);
        Assert.False(packet.DidUseNetwork);
        Assert.False(packet.DidUseHostedAi);
        Assert.False(packet.DidUseLocalModel);
        Assert.False(packet.DidMutate);
        using var summary = JsonDocument.Parse(packet.Summary);
        Assert.Equal(10, summary.RootElement.GetProperty("checks_total").GetInt32());
        Assert.Equal(10, summary.RootElement.GetProperty("checks_passed").GetInt32());
        Assert.True(summary.RootElement.GetProperty("all_passed").GetBoolean());
    }

    [Theory]
    [InlineData("EvalGateRunner v1 enabled")]
    [InlineData("Heuristic judge enabled")]
    [InlineData("Snapshot signed and verified recently")]
    [InlineData("Held-out store contains >= 1 case")]
    [InlineData("In-distribution store contains >= 1 case")]
    [InlineData("Scope hash matches latest awaiting-approval artifact")]
    [InlineData("Replay run within window")]
    [InlineData("Capability default-off audit")]
    public void IndividualPrereqFailure_WritesRefusedPacketWithReadableDetail(string failingEntryName)
    {
        var settings = EnabledSettings();
        if (failingEntryName == "EvalGateRunner v1 enabled")
        {
            settings[EvalGateRunner.EnabledSetting] = bool.FalseString;
        }
        else if (failingEntryName == "Capability default-off audit")
        {
            settings[AutonomousTaskScheduler.SchedulerEnabledSetting] = bool.TrueString;
        }

        var heldOut = failingEntryName == "Held-out store contains >= 1 case" ? RecordingHeldOutStore.Empty() : RecordingHeldOutStore.WithCases("held-out-001");
        var inDistribution = failingEntryName == "In-distribution store contains >= 1 case" ? RecordingInDistributionStore.Empty() : RecordingInDistributionStore.WithCases("in-dist-001");
        var fixture = Fixture.Create(settings: settings, heldOut: heldOut, inDistribution: inDistribution);
        fixture.SeedHappyPathEvidence(
            includeJudge: failingEntryName != "Heuristic judge enabled",
            includeSnapshot: failingEntryName != "Snapshot signed and verified recently",
            corruptScopeHash: failingEntryName == "Scope hash matches latest awaiting-approval artifact",
            includeReplay: failingEntryName != "Replay run within window");

        var result = fixture.Service.Check(fixture.OperationId, Now);

        Assert.False(result.AllPassed);
        var failed = Assert.Single(result.Entries.Where(entry => entry.Name == failingEntryName));
        Assert.False(failed.Passed);
        Assert.False(string.IsNullOrWhiteSpace(failed.Detail));
        var packet = Assert.Single(fixture.Rows(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
        Assert.Equal("Refused", packet.Status);
    }

    [Fact]
    public void PlainLanguageExplainer_KnowsPrerequisitePacketKind()
    {
        Assert.Contains(SelfImprovementPacketKinds.ApplyPrerequisiteCheck, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Equal(
            "Self-improvement apply runner prerequisite checklist; lists every gate that must pass before any future apply runner is permitted to run.",
            new PlainLanguageExplainer().ExplainPacketKind(SelfImprovementPacketKinds.ApplyPrerequisiteCheck));
    }

    [Fact]
    public void NoProducerCallsApplyRunnerPrerequisiteCheckService()
    {
        var root = Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core");
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("ApplyRunnerPrerequisiteCheckService.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("ArtifactRenameApplyRunner.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("ArtifactRenameRollbackRunner.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("ShellCompositionRoot.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("CapabilityFlagInventory.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("ApplyRunnerPrerequisiteCheckService", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ServiceHasNoApplyExecuteOrRunMethod()
    {
        var methodNames = typeof(ApplyRunnerPrerequisiteCheckService)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain("Apply", methodNames);
        Assert.DoesNotContain("Execute", methodNames);
        Assert.DoesNotContain("Run", methodNames);
        Assert.Contains("Check", methodNames);
    }

    private static Dictionary<string, string> EnabledSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplyRunnerPrerequisiteCheckService.EnabledSetting] = bool.TrueString,
            [EvalGateRunner.EnabledSetting] = bool.TrueString,
            [HeuristicJudgeService.EnabledSetting] = bool.TrueString
        };
    }

    private sealed class Fixture
    {
        private Fixture(
            string root,
            string artifactRoot,
            string databasePath,
            string operationId,
            AuditLedgerService ledger,
            ApplyRunnerPrerequisiteCheckService service)
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
        public ApplyRunnerPrerequisiteCheckService Service { get; }

        public static Fixture Create(
            IReadOnlyDictionary<string, string>? settings = null,
            bool killSwitchActive = false,
            RecordingHeldOutStore? heldOut = null,
            RecordingInDistributionStore? inDistribution = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-apply-prereq", Guid.NewGuid().ToString("N"));
            var artifactRoot = Path.Combine(root, "vnext", "artifacts");
            Directory.CreateDirectory(artifactRoot);
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            _ = ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1));
            var operationId = $"operation-{Guid.NewGuid():N}";
            var allSettings = new Dictionary<string, string>(settings ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            var service = new ApplyRunnerPrerequisiteCheckService(
                artifactRoot,
                databasePath,
                ledger,
                heldOut ?? RecordingHeldOutStore.WithCases("held-out-001"),
                inDistribution ?? RecordingInDistributionStore.WithCases("in-dist-001"),
                new KillSwitchService(() => allSettings),
                () => allSettings);

            return new Fixture(root, artifactRoot, databasePath, operationId, ledger, service);
        }

        public void SeedHappyPathEvidence(
            bool includeJudge = true,
            bool includeSnapshot = true,
            bool corruptScopeHash = false,
            bool includeReplay = true)
        {
            var operationRoot = Path.Combine(ArtifactRoot, "supervised-improvement-pilot", OperationId);
            Directory.CreateDirectory(operationRoot);
            var proposalPath = Path.Combine(operationRoot, "proposal.json");
            var dryRunPath = Path.Combine(operationRoot, "dry-run.json");
            var evalPath = Path.Combine(operationRoot, "eval.json");
            File.WriteAllText(proposalPath, JsonSerializer.Serialize(new { didMutate = false, sourceHashes = new Dictionary<string, string>() }, JsonDefaults.Options));
            File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new { didMutate = false, mutations = 0 }, JsonDefaults.Options));
            File.WriteAllText(evalPath, JsonSerializer.Serialize(new
            {
                didMutate = false,
                results = EvalGateManifest.Default().Gates.ToDictionary(gate => gate, _ => new { status = "Passed", reason = "" })
            }, JsonDefaults.Options));
            var scopeHash = ComputeScopeHash(OperationId, proposalPath, dryRunPath, evalPath);
            var awaitingPath = Path.Combine(operationRoot, "apply-awaiting-approval.json");
            File.WriteAllText(awaitingPath, JsonSerializer.Serialize(new
            {
                schemaVersion = "1",
                packetKind = SelfImprovementPacketKinds.ApplyAwaitingApproval,
                scopeId = AutonomousScopeService.SpriteRepairBatchProposalScopeId,
                operationId = OperationId,
                scopeHash = corruptScopeHash ? new string('0', 64) : scopeHash,
                proposalPath,
                dryRunPath,
                evalPath,
                didMutate = false,
                applyRunner = "not_implemented_in_v0"
            }, JsonDefaults.Options));
            Record(SelfImprovementPacketKinds.ApplyAwaitingApproval, awaitingPath, "WaitingForApproval", $"awaiting operation {OperationId}");
            if (includeJudge)
            {
                Record(SelfImprovementPacketKinds.JudgeCritique, awaitingPath, "Completed", JsonSerializer.Serialize(new
                {
                    operation_id = OperationId,
                    source = "heuristic_judge",
                    rules_evaluated = 6,
                    rules_passed = 6
                }));
            }

            if (includeSnapshot)
            {
                WriteSignedSnapshot(Path.Combine(operationRoot, "snapshot.json"), OperationId);
            }

            if (includeReplay)
            {
                File.WriteAllText(Path.Combine(operationRoot, "replay-result.json"), JsonSerializer.Serialize(new ReplayResultSummary(
                    OperationId,
                    "Identical",
                    0,
                    Now,
                    []), JsonDefaults.Options));
            }
        }

        public IReadOnlyList<AuditLedgerRow> Rows(string packetKind)
        {
            return Ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1))
                .Where(row => row.PacketKind.Equals(packetKind, StringComparison.Ordinal))
                .ToArray();
        }

        private void Record(string packetKind, string artifactPath, string status, string summary)
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
                Summary: summary,
                Status: status));
        }
    }

    private static void WriteSignedSnapshot(string path, string operationId)
    {
        var unsigned = $$"""
            {
              "schemaVersion": "1",
              "scope": "self_improvement_chain",
              "operation_id": "{{operationId}}",
              "row_count": 1,
              "rows": [
                {
                  "packet_id": "redacted",
                  "packet_kind": "{{SelfImprovementPacketKinds.ProposalDrafted}}",
                  "task_card_id": "redacted",
                  "created_at_utc": "row-0",
                  "did_use_network": false,
                  "did_use_hosted_ai": false,
                  "did_use_local_model": false,
                  "did_mutate": false,
                  "artifact_path": "",
                  "summary": "redacted",
                  "status": "Completed",
                  "error": "redacted"
                }
              ],
              "snapshot_sha256": ""
            }
            """;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(unsigned))).ToLowerInvariant();
        File.WriteAllText(path, unsigned.Replace("\"snapshot_sha256\": \"\"", $"\"snapshot_sha256\": \"{hash}\"", StringComparison.Ordinal));
    }

    private static string ComputeScopeHash(string operationId, string proposalPath, string dryRunPath, string evalPath)
    {
        var descriptor = SpriteRepairBatchProposalDescriptor.Descriptor;
        var packetKindsTouched = new[]
        {
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ApplyAwaitingApproval
        };
        return ScopeHash.Compute(new ScopeHashInputs(
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            FileHash(proposalPath),
            FileHash(dryRunPath),
            FileHash(evalPath),
            descriptor.ManifestVersion,
            packetKindsTouched,
            ExperimentManifestHash.Compute(descriptor, SpriteRepairBatchProposalDescriptor.MutationPosture, packetKindsTouched)));
    }

    private static string FileHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}

public sealed class RecordingHeldOutStore : IHeldOutEvalStore
{
    private readonly IReadOnlyList<string> _caseIds;

    private RecordingHeldOutStore(IReadOnlyList<string> caseIds)
    {
        _caseIds = caseIds;
    }

    public List<string> Calls { get; } = [];

    public static RecordingHeldOutStore WithCases(params string[] caseIds)
    {
        return new RecordingHeldOutStore(caseIds);
    }

    public static RecordingHeldOutStore Empty()
    {
        return new RecordingHeldOutStore([]);
    }

    public IReadOnlyList<string> ListCaseIds()
    {
        Calls.Add(nameof(ListCaseIds));
        return _caseIds;
    }

    public string? ReadCase(string caseId)
    {
        Calls.Add(nameof(ReadCase));
        return null;
    }
}

public sealed class RecordingInDistributionStore : IInDistributionEvalStore
{
    private readonly IReadOnlyList<string> _caseIds;

    private RecordingInDistributionStore(IReadOnlyList<string> caseIds)
    {
        _caseIds = caseIds;
    }

    public static RecordingInDistributionStore WithCases(params string[] caseIds)
    {
        return new RecordingInDistributionStore(caseIds);
    }

    public static RecordingInDistributionStore Empty()
    {
        return new RecordingInDistributionStore([]);
    }

    public override IReadOnlyList<string> ListCaseIds()
    {
        return _caseIds;
    }

    public override string? ReadCase(string caseId)
    {
        return null;
    }
}
