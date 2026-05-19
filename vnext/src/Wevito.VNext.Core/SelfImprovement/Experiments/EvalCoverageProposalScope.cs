using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Core.SelfImprovement.Experiments;

public sealed class EvalCoverageProposalScope : IAutonomousScope
{
    private readonly string _databasePath;
    private readonly AuditLedgerService _ledger;
    private readonly ConstitutionalDecisionService _decisionService;
    private readonly ConstitutionalReviewedEmitter _constitutionalReviewedEmitter;
    private readonly EvalGateRunner _evalGateRunner;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Action<string>? _commandObserver;

    public EvalCoverageProposalScope(
        string databasePath,
        AuditLedgerService ledger,
        ConstitutionalDecisionService? decisionService = null,
        ConstitutionalReviewedEmitter? constitutionalReviewedEmitter = null,
        EvalGateRunner? evalGateRunner = null,
        KillSwitchService? killSwitchService = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _ledger = ledger;
        _decisionService = decisionService ?? ShellCompositionRoot.CreateConstitutionalDecisionService(killSwitchService);
        _constitutionalReviewedEmitter = constitutionalReviewedEmitter ?? new ConstitutionalReviewedEmitter(ledger);
        _evalGateRunner = evalGateRunner ?? new EvalGateRunner(killSwitchService: killSwitchService);
        _killSwitchService = killSwitchService;
        _commandObserver = commandObserver;
    }

    public AutonomousScopeDescriptor Descriptor { get; } = AutonomousScopeService.KnownScopes.Single(scope =>
        scope.ScopeId.Equals(AutonomousScopeService.EvalCoverageProposalScopeId, StringComparison.OrdinalIgnoreCase));

    public AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return Blocked(request.ExistingTaskCards, "kill_switch=true");
        }

        if (!AutonomousScopeService.IsEnabled(request.Settings, Descriptor.ScopeId))
        {
            return Blocked(request.ExistingTaskCards, $"{AutonomousScopeService.BuildEnabledSettingKey(Descriptor.ScopeId)}=false");
        }

        if (!File.Exists(_databasePath))
        {
            return Blocked(request.ExistingTaskCards, "audit ledger not found");
        }

        var recentlyPassedGates = ReadRecentlyPassedGates(request.RequestedAtUtc);
        var gaps = EvalGateManifest.Default().Gates
            .Where(gate => !recentlyPassedGates.Contains(gate))
            .ToArray();

        if (gaps.Length == 0)
        {
            return new AutonomousScopeRunResult(Descriptor.ScopeId, true, false, request.ExistingTaskCards, "no eval coverage gaps");
        }

        var requestId = Guid.NewGuid();
        var root = ArtifactRoot(request.ArtifactRoot, requestId);
        var proposalPath = WriteProposalArtifact(root, request.RequestedAtUtc, requestId, gaps, recentlyPassedGates);
        var dryRunPath = WriteDryRunArtifact(root, request.RequestedAtUtc, gaps);
        var evalPath = WriteEvalArtifact(root, request.RequestedAtUtc, gaps);
        var card = BuildReviewCard(request.RequestedAtUtc, requestId, gaps, proposalPath, dryRunPath, evalPath);
        var cards = request.ExistingTaskCards.Concat([card]).ToArray();
        var decisionInput = new ConstitutionalDecisionInput(
            Descriptor.ScopeId,
            EvalCoverageProposalDescriptor.Kind,
            ScopeEnabled: true,
            RequestsNetwork: false,
            ScopeAllowsNetwork: false,
            RequestsHostedAi: false,
            ExperimentRegistryIsEmpty: false);
        var decision = _decisionService.Decide(decisionInput);

        Record(SelfImprovementPacketKinds.ProposalDrafted, card.Id, request.RequestedAtUtc, proposalPath, "Drafted", $"Drafted review-only eval coverage proposal for {gaps.Length} missing gate(s).");
        _constitutionalReviewedEmitter.Emit(decisionInput, decision, request.RequestedAtUtc, card.Id);
        Record(SelfImprovementPacketKinds.DryRunCompleted, card.Id, request.RequestedAtUtc, dryRunPath, "Completed", $"Completed review-only eval coverage dry run for {gaps.Length} missing gate(s); no files were changed.");
        Record(SelfImprovementPacketKinds.EvalCompleted, card.Id, request.RequestedAtUtc, evalPath, "Completed", $"Completed eval coverage preview for {gaps.Length} missing gate(s).");

        return new AutonomousScopeRunResult(
            Descriptor.ScopeId,
            true,
            false,
            cards,
            $"drafted review-only eval coverage proposal for {gaps.Length} missing gate(s); mutate=false apply=false.");
    }

    public Task<AutonomousScopePreview> DescribePlannedActionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_killSwitchService?.IsActive() == true)
        {
            return Task.FromResult(AutonomousScopePreview.Blocked(Descriptor.ScopeId, "kill_switch=true"));
        }

        if (!File.Exists(_databasePath))
        {
            return Task.FromResult(AutonomousScopePreview.Blocked(Descriptor.ScopeId, "audit ledger not found"));
        }

        var gaps = EvalGateManifest.Default().Gates
            .Where(gate => !ReadRecentlyPassedGates(DateTimeOffset.UtcNow).Contains(gate))
            .ToArray();
        var items = gaps.Select(gate => new AutonomousScopePreviewItem($"missing Passed eval coverage for gate: {gate}")).ToArray();
        var summary = gaps.Length == 0
            ? "No eval coverage gaps would be proposed."
            : $"Would draft one review-only eval coverage proposal for {gaps.Length} missing gate(s).";

        return Task.FromResult(new AutonomousScopePreview(
            Descriptor.ScopeId,
            summary,
            gaps.Length == 0 ? 0 : 1,
            AutonomousScopeEvidenceFlags.PreviewOnly,
            Items: items));
    }

    private HashSet<string> ReadRecentlyPassedGates(DateTimeOffset nowUtc)
    {
        var cutoff = nowUtc.AddDays(-30);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT artifact_path
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND status = $status
              AND created_at_utc >= $cutoff
            ORDER BY created_at_utc DESC, id DESC;
            """;
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.EvalCompleted);
        command.Parameters.AddWithValue("$status", "Passed");
        command.Parameters.AddWithValue("$cutoff", cutoff.ToString("O", CultureInfo.InvariantCulture));
        _commandObserver?.Invoke(command.CommandText);

        var gates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            foreach (var gate in ReadPassedGatesFromArtifact(Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? ""))
            {
                gates.Add(gate);
            }
        }

        return gates;
    }

    private static IReadOnlyList<string> ReadPassedGatesFromArtifact(string artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
            if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var passed = new List<string>();
            foreach (var gate in results.EnumerateObject())
            {
                if (gate.Value.ValueKind == JsonValueKind.Object &&
                    gate.Value.TryGetProperty("status", out var status) &&
                    status.ValueKind == JsonValueKind.String &&
                    string.Equals(status.GetString(), "Passed", StringComparison.OrdinalIgnoreCase))
                {
                    passed.Add(gate.Name);
                }
            }

            return passed;
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private string WriteProposalArtifact(
        string root,
        DateTimeOffset timestamp,
        Guid requestId,
        IReadOnlyList<string> gaps,
        IReadOnlySet<string> recentlyPassedGates)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "proposal.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            generatedAtUtc = timestamp,
            experimentKind = EvalCoverageProposalDescriptor.Kind,
            mutationPosture = EvalCoverageProposalDescriptor.MutationPosture,
            reviewOnly = true,
            requestId,
            recentlyPassedGates = recentlyPassedGates.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            gapGates = gaps,
            willMutateFiles = false,
            willApply = false
        }, JsonDefaults.Options));
        return path;
    }

    private string WriteDryRunArtifact(string root, DateTimeOffset timestamp, IReadOnlyList<string> gaps)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "dry-run.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            completedAtUtc = timestamp,
            gapCount = gaps.Count,
            gapGates = gaps,
            mutations = 0,
            didMutate = false,
            reason = "review_only_v0"
        }, JsonDefaults.Options));
        return path;
    }

    private string WriteEvalArtifact(string root, DateTimeOffset timestamp, IReadOnlyList<string> gaps)
    {
        Directory.CreateDirectory(root);
        var gapSet = gaps.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preview = _evalGateRunner.Preview();
        var results = EvalGateManifest.Default().Gates.ToDictionary(
            gate => gate,
            gate => new
            {
                status = gapSet.Contains(gate) ? "Failed" : "Passed",
                reason = gapSet.Contains(gate)
                    ? "coverage_gap"
                    : preview.TryGetValue(gate, out var value) && value is EvalGateResult.NotApplicable notApplicable
                        ? notApplicable.Reason
                        : ""
            },
            StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(root, "eval.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            completedAtUtc = timestamp,
            results,
            didMutate = false
        }, JsonDefaults.Options));
        return path;
    }

    private static TaskCard BuildReviewCard(
        DateTimeOffset timestamp,
        Guid requestId,
        IReadOnlyList<string> gaps,
        string proposalPath,
        string dryRunPath,
        string evalPath)
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Review eval coverage proposal for {gaps.Count} missing gate(s).",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewCode,
            RequestedToolFamily: EvalCoverageProposalDescriptor.Kind,
            TargetPathsOrAssets: gaps,
            RiskLevel: ToolRiskLevel.Medium,
            NeedsApproval: true,
            ExpectedOutput: "Review proposal only; no mutation.");
        var policy = new ToolPolicy(
            "self-improvement-eval-coverage-proposal",
            EvalCoverageProposalDescriptor.Kind,
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution,
            IsEnabled: false,
            ApprovedRootPaths: [],
            BlockReason: "Review-only self-improvement proposal. No apply path is enabled.");
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.Draft,
            null,
            "",
            EvalCoverageProposalDescriptor.Kind,
            policy,
            [
                $"{timestamp:O} drafted review-only eval coverage proposal.",
                $"proposal={proposalPath}",
                $"dry_run={dryRunPath}",
                $"eval={evalPath}"
            ],
            "Draft only. No mutation, no model call, no apply.",
            "",
            timestamp,
            timestamp,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["experiment_kind"] = EvalCoverageProposalDescriptor.Kind,
                ["mutation_posture"] = EvalCoverageProposalDescriptor.MutationPosture,
                ["request_id"] = requestId.ToString("N"),
                ["gap_gates"] = string.Join("|", gaps),
                ["proposal_path"] = proposalPath,
                ["dry_run_path"] = dryRunPath,
                ["eval_path"] = evalPath
            });
    }

    private void Record(
        string packetKind,
        Guid taskCardId,
        DateTimeOffset timestamp,
        string artifactPath,
        string status,
        string summary)
    {
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: artifactPath,
            Summary: summary,
            Status: status));
    }

    private static string ArtifactRoot(string artifactRoot, Guid requestId)
    {
        return Path.Combine(artifactRoot, "eval-coverage-proposal", requestId.ToString("N"));
    }

    private AutonomousScopeRunResult Blocked(IReadOnlyList<TaskCard> cards, string reason)
    {
        return new AutonomousScopeRunResult(Descriptor.ScopeId, false, false, cards, "", reason);
    }
}
