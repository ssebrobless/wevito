using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Judge;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class PostC174SelfImprovementBaselineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Baseline_eval_gate_manifest_lists_eleven_documented_gates()
    {
        Assert.Equal(
            [
                "Build",
                "Unit tests",
                "Benchmark suite",
                "In-distribution eval",
                "Held-out eval",
                "Performance",
                "Scope hash",
                "Dry-run",
                "Backup",
                "Post-proof",
                "Rollback"
            ],
            EvalGateManifest.Default().Gates);
    }

    [Fact]
    public void Baseline_eval_gate_runner_enabled_setting_is_eval_gate_runner_v1_enabled()
    {
        Assert.Equal("eval_gate_runner_v1_enabled", EvalGateRunner.EnabledSetting);
    }

    [Fact]
    public void Baseline_heuristic_judge_enabled_setting_is_heuristic_judge_enabled()
    {
        Assert.Equal("heuristic_judge_enabled", HeuristicJudgeService.EnabledSetting);
    }

    [Fact]
    public void Baseline_heuristic_judge_registers_exactly_six_rules()
    {
        var fixture = JudgeFixture.Create();

        var findings = fixture.Service.Critique(fixture.OperationId, Now);

        Assert.Equal(6, findings.Count);
    }

    [Fact]
    public void Baseline_apply_runner_prereq_check_returns_exactly_ten_entries()
    {
        var root = TempRoot("apply-prereq");
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplyRunnerPrerequisiteCheckService.EnabledSetting] = bool.TrueString,
            [EvalGateRunner.EnabledSetting] = bool.TrueString,
            [HeuristicJudgeService.EnabledSetting] = bool.TrueString
        };
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new ApplyRunnerPrerequisiteCheckService(
            Path.Combine(root, "artifacts"),
            Path.Combine(root, "ledger.sqlite"),
            ledger,
            new StaticHeldOutEvalStore([]),
            new StaticInDistributionEvalStore([]),
            new KillSwitchService(() => settings),
            () => settings);

        var result = service.Check("baseline-op", Now);

        Assert.Equal(10, result.Entries.Count);
    }

    [Fact]
    public void Baseline_apply_prerequisite_packet_kind_is_self_improvement_apply_prerequisite_check()
    {
        Assert.Equal("self_improvement_apply_prerequisite_check", SelfImprovementPacketKinds.ApplyPrerequisiteCheck);
        Assert.Contains("self_improvement_apply_prerequisite_check", PlainLanguageExplainer.KnownPacketKinds);
    }

    [Fact]
    public void Baseline_supervised_loop_apply_runner_not_implemented_reason_is_v0()
    {
        Assert.Equal("apply_runner_not_implemented_in_v0", SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
    }

    [Fact]
    public void Baseline_local_scoring_request_only_exposes_prompt_sha_and_rubric()
    {
        var properties = typeof(LocalScoringRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("PromptSha256", properties);
        Assert.Contains("Rubric", properties);
        Assert.DoesNotContain(properties, property =>
            !property.Equals("PromptSha256", StringComparison.OrdinalIgnoreCase) &&
            property.Contains("prompt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("answer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("rationale", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("text", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Baseline_not_configured_scoring_provider_default_is_refused()
    {
        var provider = new NotConfiguredScoringProvider();

        var result = provider.Score(new LocalScoringRequest("", ""), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("local_scoring_provider_not_configured", refused.Reason);
        Assert.Equal("local_scoring_provider_enabled", NotConfiguredScoringProvider.EnabledSetting);
    }

    [Fact]
    public void Baseline_ollama_loopback_scoring_provider_constants_are_pinned()
    {
        Assert.Equal("local_scoring_provider_ollama_enabled", OllamaLoopbackScoringProvider.OllamaEnabledSetting);
        Assert.Equal("127.0.0.1:11434", OllamaLoopbackScoringProvider.DefaultEndpoint);
        Assert.Equal("qwen2.5:7b-instruct-q4_k_m", OllamaLoopbackScoringProvider.DefaultModel);
    }

    [Fact]
    public void Baseline_capability_inventory_contains_every_flag_named_above()
    {
        var expected = new[]
        {
            KillSwitchService.KillSwitchSetting,
            EvalGateRunner.EnabledSetting,
            HeuristicJudgeService.EnabledSetting,
            NotConfiguredScoringProvider.EnabledSetting,
            OllamaLoopbackScoringProvider.OllamaEnabledSetting,
            OllamaLoopbackScoringProvider.LoopbackEndpointSetting,
            OllamaLoopbackScoringProvider.OllamaModelSetting,
            ApplyRunnerPrerequisiteCheckService.EnabledSetting
        };

        foreach (var name in expected)
        {
            var entry = Assert.Single(CapabilityFlagInventory.Entries, entry => entry.Name == name);
            if (name.EndsWith("_enabled", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(bool.FalseString, entry.DefaultValue);
            }
        }
    }

    [Fact]
    public void Baseline_tool_projects_are_not_referenced_from_shell_or_tests()
    {
        var root = FindRepositoryRoot();
        var shellProject = File.ReadAllText(Path.Combine(root, "vnext", "src", "Wevito.VNext.Shell", "Wevito.VNext.Shell.csproj"));
        var testsProject = File.ReadAllText(Path.Combine(root, "vnext", "tests", "Wevito.VNext.Tests", "Wevito.VNext.Tests.csproj"));
        var toolNames = new[]
        {
            "Wevito.Tools.HeldOutSeed",
            "Wevito.Tools.SnapshotExport",
            "Wevito.Tools.SnapshotVerify",
            "Wevito.Tools.ReplayRunner",
            "Wevito.Tools.InDistributionSeed"
        };

        foreach (var toolName in toolNames)
        {
            var projectPath = Path.Combine(root, "vnext", "tools", toolName, $"{toolName}.csproj");
            Assert.True(File.Exists(projectPath), $"Missing tool project: {projectPath}");
            Assert.DoesNotContain($"{toolName}.csproj", shellProject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"{toolName}.csproj", testsProject, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string TempRoot(string slug)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-post-c174-baseline", slug, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
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

    private sealed record JudgeFixture(string OperationId, HeuristicJudgeService Service)
    {
        public static JudgeFixture Create()
        {
            var root = TempRoot("heuristic-judge");
            var operationId = "baseline-op";
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HeuristicJudgeService.EnabledSetting] = bool.TrueString
            };
            var artifacts = Path.Combine(root, "vnext", "artifacts", operationId);
            Directory.CreateDirectory(artifacts);

            var proposalPath = Path.Combine(artifacts, "proposal.json");
            var dryRunPath = Path.Combine(artifacts, "dry-run.json");
            var evalPath = Path.Combine(artifacts, "eval.json");
            var awaitingPath = Path.Combine(artifacts, "awaiting.json");

            File.WriteAllText(proposalPath, JsonSerializer.Serialize(new
            {
                didMutate = false,
                sourceHashes = new Dictionary<string, string>()
            }, JsonDefaults.Options));
            File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new
            {
                didMutate = false
            }, JsonDefaults.Options));
            File.WriteAllText(evalPath, JsonSerializer.Serialize(new
            {
                didMutate = false,
                results = EvalGateManifest.Default().Gates.ToDictionary(gate => gate, _ => "not_applicable", StringComparer.Ordinal)
            }, JsonDefaults.Options));
            File.WriteAllText(awaitingPath, JsonSerializer.Serialize(new
            {
                operationId,
                proposalPath,
                dryRunPath,
                evalPath,
                scopeHash = new string('a', 64),
                applyRunner = "not_implemented_in_v0"
            }, JsonDefaults.Options));

            ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyAwaitingApproval,
                Guid.NewGuid(),
                Now,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: awaitingPath,
                Summary: JsonSerializer.Serialize(new { operation_id = operationId }, JsonDefaults.Options),
                Status: "WaitingForApproval"));

            return new JudgeFixture(
                operationId,
                new HeuristicJudgeService(databasePath, ledger, new KillSwitchService(() => settings), () => settings));
        }
    }

    private sealed class StaticHeldOutEvalStore(IReadOnlyList<string> ids) : IHeldOutEvalStore
    {
        public IReadOnlyList<string> ListCaseIds()
        {
            return ids;
        }

        public string? ReadCase(string caseId)
        {
            throw new InvalidOperationException("Baseline prerequisite count test must not read held-out contents.");
        }
    }

    private sealed class StaticInDistributionEvalStore(IReadOnlyList<string> ids) : IInDistributionEvalStore
    {
        public override IReadOnlyList<string> ListCaseIds()
        {
            return ids;
        }

        public override string? ReadCase(string caseId)
        {
            throw new InvalidOperationException("Baseline prerequisite count test must not read in-distribution contents.");
        }
    }
}
