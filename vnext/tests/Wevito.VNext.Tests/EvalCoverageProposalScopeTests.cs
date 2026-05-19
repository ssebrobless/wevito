using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Tests;

public sealed class EvalCoverageProposalScopeTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void TryRun_KillSwitchActive_BlocksWithoutWrites()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        var scope = fixture.CreateScope(killSwitchActive: true);

        var result = scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        Assert.False(result.Ran);
        Assert.Equal("kill_switch=true", result.BlockReason);
        Assert.Empty(fixture.Rows());
    }

    [Fact]
    public void TryRun_FlagOff_BlocksWithoutWrites()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        var scope = fixture.CreateScope();

        var result = scope.TryRun(Request(Settings(enabled: false), fixture.ArtifactRoot, []));

        Assert.False(result.Ran);
        Assert.Equal($"{AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.EvalCoverageProposalScopeId)}=false", result.BlockReason);
        Assert.Empty(fixture.Rows());
    }

    [Fact]
    public void TryRun_DatabaseMissing_BlocksWithoutWrites()
    {
        var fixture = Fixture.Create();
        var scope = fixture.CreateScope();

        var result = scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        Assert.False(result.Ran);
        Assert.Equal("audit ledger not found", result.BlockReason);
        Assert.False(File.Exists(fixture.DatabasePath));
    }

    [Fact]
    public void TryRun_EmptyLedger_ProposesEveryManifestGate()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        var scope = fixture.CreateScope();

        var result = scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        Assert.True(result.Ran);
        Assert.False(result.DidMutate);
        var card = Assert.Single(result.TaskCards);
        Assert.Equal(EvalCoverageProposalDescriptor.Kind, card.ToolFamily);
        Assert.Contains(EvalGateManifest.Build, card.ReviewPayload!["gap_gates"]);
        var proposalPath = card.ReviewPayload["proposal_path"];
        using var proposal = JsonDocument.Parse(File.ReadAllText(proposalPath));
        var gaps = proposal.RootElement.GetProperty("gapGates").EnumerateArray().Select(item => item.GetString() ?? "").ToArray();
        Assert.Equal(EvalGateManifest.Default().Gates, gaps);
    }

    [Fact]
    public void TryRun_SeededPassedBuildEval_ExcludesBuildFromGaps()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        fixture.SeedPassedEvalArtifact(EvalGateManifest.Build);
        var scope = fixture.CreateScope();

        var result = scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        var card = Assert.Single(result.TaskCards);
        var gaps = card.ReviewPayload!["gap_gates"].Split('|', StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain(EvalGateManifest.Build, gaps);
        Assert.Contains(EvalGateManifest.UnitTests, gaps);
    }

    [Fact]
    public void TryRun_EmitsExpectedReviewOnlyPacketSequence()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        var scope = fixture.CreateScope();

        scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        Assert.Equal(
            [
                SelfImprovementPacketKinds.ProposalDrafted,
                SelfImprovementPacketKinds.ConstitutionalReviewed,
                SelfImprovementPacketKinds.DryRunCompleted,
                SelfImprovementPacketKinds.EvalCompleted
            ],
            fixture.Rows().Select(row => row.PacketKind).ToArray());
    }

    [Fact]
    public void TryRun_EmittedPacketsHaveSafeEvidenceFlags()
    {
        var fixture = Fixture.Create();
        fixture.InitializeLedger();
        var scope = fixture.CreateScope();

        scope.TryRun(Request(Settings(enabled: true), fixture.ArtifactRoot, []));

        Assert.All(fixture.Rows(), row =>
        {
            Assert.False(row.DidMutate);
            Assert.False(row.DidUseNetwork);
            Assert.False(row.DidUseHostedAi);
            Assert.False(row.DidUseLocalModel);
        });
    }

    [Fact]
    public void Source_DoesNotIssueWriteSql()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Source_DoesNotReferenceEvalStores()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
    }

    private static AutonomousScopeRunRequest Request(
        IReadOnlyDictionary<string, string> settings,
        string artifactRoot,
        IReadOnlyList<TaskCard> cards)
    {
        return new AutonomousScopeRunRequest(settings, new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""), artifactRoot, Now, cards);
    }

    private static Dictionary<string, string> Settings(bool enabled)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.EvalCoverageProposalScopeId)] = enabled.ToString()
        };
    }

    private static string SourcePath()
    {
        return Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Experiments", "EvalCoverageProposalScope.cs");
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

    private sealed class Fixture
    {
        private Fixture(string root)
        {
            Root = root;
            ArtifactRoot = Path.Combine(root, "artifacts");
            DatabasePath = Path.Combine(root, "ledger.sqlite");
            Ledger = new AuditLedgerService(DatabasePath);
        }

        public string Root { get; }

        public string ArtifactRoot { get; }

        public string DatabasePath { get; }

        public AuditLedgerService Ledger { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-eval-coverage-proposal", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(root);
        }

        public void InitializeLedger()
        {
            _ = Ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1));
        }

        public EvalCoverageProposalScope CreateScope(bool killSwitchActive = false)
        {
            var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            });

            return new EvalCoverageProposalScope(DatabasePath, Ledger, killSwitchService: killSwitch);
        }

        public IReadOnlyList<AuditLedgerRow> Rows()
        {
            return Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)).OrderBy(row => row.Id).ToArray();
        }

        public void SeedPassedEvalArtifact(string passedGate)
        {
            var root = Path.Combine(ArtifactRoot, "seeded-eval");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "eval.json");
            var results = EvalGateManifest.Default().Gates.ToDictionary(
                gate => gate,
                gate => new
                {
                    status = gate.Equals(passedGate, StringComparison.OrdinalIgnoreCase) ? "Passed" : "Failed",
                    reason = gate.Equals(passedGate, StringComparison.OrdinalIgnoreCase) ? "" : "seeded_gap"
                },
                StringComparer.OrdinalIgnoreCase);
            File.WriteAllText(path, JsonSerializer.Serialize(new { schemaVersion = "1", results }, JsonDefaults.Options));
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.EvalCompleted,
                null,
                Now.AddMinutes(-5),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: path,
                Summary: "seeded passed eval row",
                Status: "Passed"));
        }
    }
}
