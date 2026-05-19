using System.Diagnostics;
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
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class SelfImprovementProductTruthTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Held_out_store_lists_no_cases_under_empty_root()
    {
        var root = TempRoot("held-out-empty");
        Directory.CreateDirectory(root);

        var caseIds = new HeldOutEvalStore(root).ListCaseIds();

        Assert.Empty(caseIds);
    }

    [Fact]
    public void In_distribution_store_lists_no_cases_under_empty_root()
    {
        var root = TempRoot("in-distribution-empty");
        Directory.CreateDirectory(root);

        var caseIds = new InDistributionEvalStore(root).ListCaseIds();

        Assert.Empty(caseIds);
    }

    [Fact]
    public void Supervised_loop_refuses_with_apply_runner_not_implemented_in_v0()
    {
        var root = TempRoot("supervised-loop");
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var proposalCard = ProposalCard(root);
        var tick = loop.TryRun(SupervisedLoopRequest(root, SupervisedLoopSettings(), [proposalCard]));
        var approvalCard = Assert.Single(tick.TaskCards.Where(SupervisedImprovementLoop.IsAwaitingApprovalCard));
        var operationId = approvalCard.ReviewPayload!["operation_id"];
        var scopeHash = approvalCard.ReviewPayload["scope_hash"];

        var result = loop.HandleApplyApproval(
            new UserApplyApproval(true, operationId, Now, AutonomousScopeService.SpriteRepairBatchProposalScopeId, operationId, scopeHash),
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            scopeHash,
            approvalCard.Id,
            Now,
            tick.TaskCards);

        var refused = Assert.IsType<ApprovalResult.Refused>(result.ValidationResult);
        Assert.Equal(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, refused.Reason);
        Assert.False(result.DidMutate);
    }

    [Fact]
    public void Every_enabled_capability_flag_defaults_to_false()
    {
        var enabledFlags = CapabilityFlagInventory.Entries
            .Where(entry => entry.Name.EndsWith("_enabled", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(enabledFlags);
        Assert.All(enabledFlags, entry => Assert.Equal(bool.FalseString, entry.DefaultValue));
    }

    [Fact]
    public void Eval_gate_runner_returns_not_applicable_when_flag_unset()
    {
        var runner = new RecordingCommandRunner();

        var result = new EvalGateRunner().Execute(
            new EvalGateExecutionRequest("sprite-repair-batch-proposal", "operation-001", FindRepositoryRoot(), new Dictionary<string, string>()),
            runner,
            ValidHashInputs());

        Assert.All(result.Results.Values, value =>
        {
            var notApplicable = Assert.IsType<EvalGateResult.NotApplicable>(value);
            Assert.Equal("eval_gate_runner_v1_enabled=false", notApplicable.Reason);
        });
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public void Heuristic_judge_returns_empty_when_flag_unset()
    {
        var fixture = JudgeFixture.Create(flagEnabled: null);

        var findings = fixture.Service.Critique(fixture.OperationId, fixture.Now);

        Assert.Empty(findings);
        Assert.Empty(fixture.Ledger.Snapshot(fixture.Now.AddHours(-1), fixture.Now.AddHours(1)));
    }

    [Fact]
    public void Eval_coverage_proposal_scope_writes_nothing_when_flag_unset()
    {
        var fixture = EvalCoverageFixture.Create();
        fixture.InitializeLedger();

        var result = fixture.Scope.TryRun(EvalCoverageRequest(fixture.ArtifactRoot, enabled: false));

        Assert.False(result.Ran);
        Assert.Equal($"{AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.EvalCoverageProposalScopeId)}=false", result.BlockReason);
        Assert.Empty(fixture.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void Snapshot_export_signature_uses_empty_signature_field_then_inserts_hash()
    {
        var root = TempRoot("snapshot-export");
        var databasePath = Path.Combine(root, "ledger.sqlite");
        var operationId = "operation-snapshot-truth";
        var taskCardId = Guid.NewGuid();
        var ledger = new AuditLedgerService(databasePath);
        Record(ledger, SelfImprovementPacketKinds.ProposalDrafted, taskCardId, operationId, Now);
        Record(ledger, SelfImprovementPacketKinds.DryRunCompleted, taskCardId, operationId, Now.AddSeconds(1));

        var output = Path.Combine(root, "snapshot.json");
        Assert.Equal(0, InvokeSnapshotExportCli(["export", "--db", databasePath, "--operation-id", operationId, "--output", output]));
        var text = File.ReadAllText(output);
        using var document = JsonDocument.Parse(text);
        var signature = document.RootElement.GetProperty("snapshot_sha256").GetString();
        Assert.Matches("^[0-9a-f]{64}$", signature);

        var unsigned = text.Replace($"\"snapshot_sha256\": \"{signature}\"", "\"snapshot_sha256\": \"\"", StringComparison.Ordinal);

        Assert.Equal(Sha256(Encoding.UTF8.GetBytes(unsigned)), signature);
    }

    [Fact]
    public void Replay_harness_does_not_write_to_production_audit_ledger()
    {
        var fixture = ReplayFixture.Create();
        var capture = ReplayCapture(fixture.Scope, fixture.Root, "truth-seed");
        var harness = new ReplayHarness(fixture.Scope);

        harness.Replay(capture);

        Assert.Empty(fixture.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void Proposal_diff_explainer_blocks_outside_vnext_artifacts()
    {
        var root = TempRoot("proposal-diff");
        var databasePath = Path.Combine(root, "ledger.sqlite");
        var operationId = "operation-outside-artifact";
        var outside = Path.Combine(root, "outside.json");
        File.WriteAllText(outside, "{}");
        new AuditLedgerService(databasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            Guid.NewGuid(),
            Now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: outside,
            Summary: $"awaiting operation {operationId}",
            Status: "WaitingForApproval"));

        var explanation = new ProposalDiffExplainerService(databasePath).Explain(operationId);

        Assert.True(explanation.IsBlocked);
        Assert.Equal("artifact_path_outside_allowed_root", explanation.BlockReason);
    }

    [Fact]
    public void Plain_language_known_packet_kinds_contains_every_self_improvement_packet_kind()
    {
        var kinds = typeof(SelfImprovementPacketKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();
        var explainer = new PlainLanguageExplainer();

        foreach (var kind in kinds)
        {
            Assert.Contains(kind, PlainLanguageExplainer.KnownPacketKinds);
            Assert.False(explainer.ExplainPacketKind(kind).StartsWith("Unknown ", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Local_scoring_provider_default_is_NotConfiguredScoringProvider()
    {
        var provider = ShellCompositionRoot.CreateLocalScoringProvider();

        Assert.IsType<NotConfiguredScoringProvider>(provider);
    }

    [Fact]
    public void Ollama_loopback_scoring_provider_refuses_when_flags_off()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var http = new FakeScoringHttpClient("""{"response":"0.8","model":"qwen2.5"}""");
        var provider = new OllamaLoopbackScoringProvider(http, new KillSwitchService(() => settings), () => settings);

        var result = provider.Score(new LocalScoringRequest(new string('a', 64), "proposal rubric"), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("local_scoring_provider_enabled=false", refused.Reason);
        Assert.Empty(http.Calls);
    }

    [Fact]
    public void Capabilities_and_gates_snapshot_lists_every_capability_flag_inventory_entry()
    {
        var service = new CapabilitiesAndGatesService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var snapshot = service.Snapshot(Now);

        Assert.Equal(CapabilityFlagInventory.Entries.Select(entry => entry.Name), snapshot.Entries.Select(entry => entry.Name));
    }

    private static SupervisedImprovementLoopRequest SupervisedLoopRequest(string root, IReadOnlyDictionary<string, string> settings, IReadOnlyList<TaskCard> cards)
    {
        return new SupervisedImprovementLoopRequest(settings, ActiveStatus(), Path.Combine(root, "artifacts"), Now, cards);
    }

    private static AutonomousScopeRunRequest EvalCoverageRequest(string artifactRoot, bool enabled)
    {
        return new AutonomousScopeRunRequest(EvalCoverageSettings(enabled), ActiveStatus(), artifactRoot, Now, []);
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", "");
    }

    private static Dictionary<string, string> SupervisedLoopSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SupervisedImprovementLoopSettings.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = bool.TrueString
        };
    }

    private static Dictionary<string, string> EvalCoverageSettings(bool enabled)
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

    private static TaskCard ProposalCard(string root)
    {
        var artifactRoot = Path.Combine(root, "source-artifacts");
        Directory.CreateDirectory(artifactRoot);
        var proposalPath = Path.Combine(artifactRoot, "proposal.json");
        var dryRunPath = Path.Combine(artifactRoot, "dry-run.json");
        var evalPath = Path.Combine(artifactRoot, "eval.json");
        File.WriteAllText(proposalPath, """{"kind":"proposal"}""");
        File.WriteAllText(dryRunPath, """{"kind":"dry-run"}""");
        File.WriteAllText(evalPath, """{"kind":"eval"}""");
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "Review self-improvement sprite repair proposal.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            NeedsApproval: true);
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.Draft,
            ToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            CreatedAtUtc: Now,
            UpdatedAtUtc: Now,
            ReviewPayload: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["proposal_path"] = proposalPath,
                ["dry_run_path"] = dryRunPath,
                ["eval_path"] = evalPath
            });
    }

    private static ScopeHashInputs ValidHashInputs()
    {
        return new ScopeHashInputs(
            "sprite-repair-batch-proposal",
            "operation-001",
            new string('a', 64),
            new string('b', 64),
            new string('c', 64),
            "1",
            [SelfImprovementPacketKinds.ProposalDrafted]);
    }

    private static void Record(AuditLedgerService ledger, string packetKind, Guid taskCardId, string operationId, DateTimeOffset createdAtUtc)
    {
        ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            createdAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"operation {operationId}",
            Status: "Completed"));
    }

    private static int InvokeSnapshotExportCli(string[] args)
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.SnapshotExport", "Wevito.Tools.SnapshotExport.csproj"));
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", project, "--nologo" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        Assert.NotNull(process);
        process!.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.SnapshotExport.dll");
        var programType = Assembly.LoadFrom(assemblyPath).GetType("Wevito.Tools.SnapshotExport.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, new StringWriter(), new StringWriter()])!;
    }

    private static ReplayCapture ReplayCapture(SpriteRepairBatchProposalScope scope, string artifactRoot, string seed)
    {
        var packets = new List<EvidencePacket>();
        var result = scope.TryRun(ReplayRequest(artifactRoot), seed, packets.Add);
        Assert.True(result.Ran);
        return new ReplayCapture(scope.Descriptor.ScopeId, $"operation-{seed}", seed, Now, packets);
    }

    private static AutonomousScopeRunRequest ReplayRequest(string artifactRoot)
    {
        return new AutonomousScopeRunRequest(
            SupervisedLoopSettings(),
            ActiveStatus(),
            artifactRoot,
            Now,
            []);
    }

    private static string WriteSpriteRepairQueue(string root, SpriteRepairQueueRow row)
    {
        var queuePath = Path.Combine(root, "repair_queue.json");
        var manifest = new SpriteRepairQueueManifest(
            "1.0",
            Now,
            "visual_qa_manifest.json",
            Now.AddMinutes(-30).ToString("O"),
            1,
            1,
            new Dictionary<string, int> { [row.Priority] = 1 },
            new Dictionary<string, int> { ["crop_detected"] = 1 },
            [row]);
        File.WriteAllText(queuePath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        return queuePath;
    }

    private static SpriteRepairQueueRow SpriteRepairRow(string root)
    {
        return new SpriteRepairQueueRow(
            "snake_baby_female",
            "snake",
            "baby",
            "female",
            "P1",
            "queued",
            1,
            ["blue"],
            ["idle"],
            ["tools/fake_repair.py"],
            [
                new SpriteRepairQueueIssue(
                    "blue",
                    "idle",
                    "P1",
                    ["crop_detected"],
                    ["test warning"],
                    "tools/fake_repair.py",
                    "test reason",
                    Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"),
                    null)
            ]);
    }

    private static string TempRoot(string slug)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-product-truth", slug, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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

    private sealed class RecordingCommandRunner : ICommandRunner
    {
        public List<ProofExecutionRequest> Invocations { get; } = [];

        public Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
        {
            Invocations.Add(request);
            throw new InvalidOperationException("The product-truth disabled eval gate must not invoke command execution.");
        }
    }

    private sealed record EvalCoverageFixture(string Root, string ArtifactRoot, AuditLedgerService Ledger, EvalCoverageProposalScope Scope)
    {
        public static EvalCoverageFixture Create()
        {
            var root = TempRoot("eval-coverage");
            var artifactRoot = Path.Combine(root, "artifacts");
            var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
            return new EvalCoverageFixture(root, artifactRoot, ledger, new EvalCoverageProposalScope(Path.Combine(root, "ledger.sqlite"), ledger));
        }

        public void InitializeLedger()
        {
            _ = Ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1));
        }
    }

    private sealed record ReplayFixture(string Root, AuditLedgerService Ledger, SpriteRepairBatchProposalScope Scope)
    {
        public static ReplayFixture Create()
        {
            var root = TempRoot("replay-harness");
            Directory.CreateDirectory(Path.Combine(root, "tools"));
            Directory.CreateDirectory(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue"));
            File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
            File.WriteAllText(Path.Combine(root, "tools", "fake_repair.py"), "# fake repair");
            File.WriteAllText(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"), "before");
            var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
            var queuePath = WriteSpriteRepairQueue(root, SpriteRepairRow(root));
            return new ReplayFixture(root, ledger, new SpriteRepairBatchProposalScope(queuePath, ledger));
        }
    }

    private sealed record JudgeFixture(string OperationId, DateTimeOffset Now, HeuristicJudgeService Service, AuditLedgerService Ledger)
    {
        public static JudgeFixture Create(bool? flagEnabled)
        {
            var root = TempRoot("heuristic-judge");
            var operationId = $"operation-{Guid.NewGuid():N}";
            var now = SelfImprovementProductTruthTests.Now;
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (flagEnabled.HasValue)
            {
                settings[HeuristicJudgeService.EnabledSetting] = flagEnabled.Value.ToString();
            }

            return new JudgeFixture(
                operationId,
                now,
                new HeuristicJudgeService(databasePath, ledger, new KillSwitchService(() => settings), () => settings),
                ledger);
        }
    }
}
