using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Core.SelfImprovement.Experiments;

public sealed class SpriteRepairBatchProposalScope : IAutonomousScope
{
    private readonly string _queuePath;
    private readonly AuditLedgerService _ledger;
    private readonly ConstitutionalDecisionService _decisionService;
    private readonly ConstitutionalReviewedEmitter _constitutionalReviewedEmitter;
    private readonly EvalGateRunner _evalGateRunner;

    public SpriteRepairBatchProposalScope(
        string queuePath,
        AuditLedgerService ledger,
        ConstitutionalDecisionService? decisionService = null,
        ConstitutionalReviewedEmitter? constitutionalReviewedEmitter = null,
        EvalGateRunner? evalGateRunner = null)
    {
        _queuePath = queuePath;
        _ledger = ledger;
        _decisionService = decisionService ?? ShellCompositionRoot.CreateConstitutionalDecisionService();
        _constitutionalReviewedEmitter = constitutionalReviewedEmitter ?? new ConstitutionalReviewedEmitter(ledger);
        _evalGateRunner = evalGateRunner ?? new EvalGateRunner();
    }

    public AutonomousScopeDescriptor Descriptor { get; } = AutonomousScopeService.KnownScopes.Single(scope =>
        scope.ScopeId.Equals(AutonomousScopeService.SpriteRepairBatchProposalScopeId, StringComparison.OrdinalIgnoreCase));

    public AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request)
    {
        var repoRoot = ResolveRepoRoot(_queuePath);
        var row = ReadPriorityRows(_queuePath, repoRoot).FirstOrDefault();
        if (row is null)
        {
            return new AutonomousScopeRunResult(Descriptor.ScopeId, true, false, request.ExistingTaskCards, "0 P0/P1 repair rows found.");
        }

        var issue = row.Issues.FirstOrDefault() ?? BuildFallbackIssue(row);
        var requestId = Guid.NewGuid();
        var proposalPath = WriteProposalArtifact(repoRoot, request.ArtifactRoot, request.RequestedAtUtc, requestId, row, issue);
        var decisionInput = new ConstitutionalDecisionInput(
            Descriptor.ScopeId,
            SpriteRepairBatchProposalDescriptor.Kind,
            ScopeEnabled: true,
            RequestsNetwork: false,
            ScopeAllowsNetwork: false,
            RequestsHostedAi: false,
            ExperimentRegistryIsEmpty: false);
        var decision = _decisionService.Decide(decisionInput);
        var dryRunPath = WriteDryRunArtifact(request.ArtifactRoot, request.RequestedAtUtc, requestId, row, issue);
        var evalPath = WriteEvalArtifact(request.ArtifactRoot, request.RequestedAtUtc, requestId);
        var card = BuildReviewCard(request.RequestedAtUtc, row, issue, proposalPath, dryRunPath, evalPath);
        var cards = request.ExistingTaskCards.Concat([card]).ToArray();

        Record(SelfImprovementPacketKinds.ProposalDrafted, card.Id, request.RequestedAtUtc, proposalPath, "Drafted", $"Drafted review-only sprite repair batch proposal for {row.RowId}.");
        _constitutionalReviewedEmitter.Emit(decisionInput, decision, request.RequestedAtUtc, card.Id);
        Record(SelfImprovementPacketKinds.DryRunCompleted, card.Id, request.RequestedAtUtc, dryRunPath, "Completed", $"Completed review-only dry run for {row.RowId}; no sprite files were written.");
        Record(SelfImprovementPacketKinds.EvalCompleted, card.Id, request.RequestedAtUtc, evalPath, "Completed", $"Completed eval preview for {row.RowId}; apply-only gates are NotApplicable.");

        return new AutonomousScopeRunResult(
            Descriptor.ScopeId,
            true,
            false,
            cards,
            $"drafted review-only sprite repair batch proposal for {row.RowId}; mutate=false apply=false.");
    }

    public Task<AutonomousScopePreview> DescribePlannedActionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_queuePath))
        {
            return Task.FromResult(new AutonomousScopePreview(
                Descriptor.ScopeId,
                $"No proposal would be drafted because the queue is missing: {_queuePath}",
                0,
                AutonomousScopeEvidenceFlags.PreviewOnly,
                "queue_missing"));
        }

        var repoRoot = ResolveRepoRoot(_queuePath);
        var rows = ReadPriorityRows(_queuePath, repoRoot).Take(5).ToArray();
        var items = rows.Select(row => new AutonomousScopePreviewItem(
            $"{row.Priority} {row.RowId} -> draft review-only self-improvement proposal",
            SourcePath: _queuePath)).ToArray();
        var summary = rows.Length == 0
            ? "No P0/P1 sprite repair rows would draft a self-improvement proposal."
            : $"Would draft one review-only self-improvement sprite-repair proposal from {rows.Length} candidate row(s).";
        return Task.FromResult(new AutonomousScopePreview(
            Descriptor.ScopeId,
            summary,
            rows.Length == 0 ? 0 : 1,
            AutonomousScopeEvidenceFlags.PreviewOnly,
            Items: items));
    }

    private IReadOnlyList<SpriteRepairQueueRow> ReadPriorityRows(string queuePath, string repoRoot)
    {
        if (!File.Exists(queuePath))
        {
            return [];
        }

        try
        {
            return new SpriteRepairQueueReader().Load(queuePath, repoRoot).Rows
                .Where(row => row.Priority.Equals("P0", StringComparison.OrdinalIgnoreCase) || row.Priority.Equals("P1", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private string WriteProposalArtifact(
        string repoRoot,
        string artifactRoot,
        DateTimeOffset timestamp,
        Guid requestId,
        SpriteRepairQueueRow row,
        SpriteRepairQueueIssue issue)
    {
        var root = Path.Combine(artifactRoot, "sprite-repair-batch-proposal", requestId.ToString("N"));
        Directory.CreateDirectory(root);
        var sourcePaths = ResolveSourcePaths(repoRoot, row, issue);
        var hashes = sourcePaths.ToDictionary(path => path, path => File.Exists(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar))) ? Sha256(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar))) : "", StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(root, "proposal.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            generatedAtUtc = timestamp,
            experimentKind = SpriteRepairBatchProposalDescriptor.Kind,
            mutationPosture = SpriteRepairBatchProposalDescriptor.MutationPosture,
            reviewOnly = true,
            rowId = row.RowId,
            speciesId = row.SpeciesId,
            lifeStage = row.LifeStage,
            gender = row.Gender,
            colorVariant = issue.ColorVariant,
            animationFamily = issue.AnimationFamily,
            sourcePaths,
            sourceHashes = hashes,
            willMutateSprites = false,
            willApply = false
        }, JsonDefaults.Options));
        return path;
    }

    private string WriteDryRunArtifact(
        string artifactRoot,
        DateTimeOffset timestamp,
        Guid requestId,
        SpriteRepairQueueRow row,
        SpriteRepairQueueIssue issue)
    {
        var root = Path.Combine(artifactRoot, "sprite-repair-batch-proposal", requestId.ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "dry-run.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            completedAtUtc = timestamp,
            rowId = row.RowId,
            issue = new { issue.ColorVariant, issue.AnimationFamily, issue.Severity },
            replacements = 0,
            additions = 0,
            mutations = 0,
            didMutate = false,
            reason = "review_only_v0"
        }, JsonDefaults.Options));
        return path;
    }

    private string WriteEvalArtifact(string artifactRoot, DateTimeOffset timestamp, Guid requestId)
    {
        var root = Path.Combine(artifactRoot, "sprite-repair-batch-proposal", requestId.ToString("N"));
        Directory.CreateDirectory(root);
        var results = _evalGateRunner.Preview().ToDictionary(
            pair => pair.Key,
            pair => pair.Value switch
            {
                EvalGateResult.Passed => new { status = "Passed", reason = "" },
                EvalGateResult.Failed failed => new { status = "Failed", reason = failed.Reason },
                EvalGateResult.NotApplicable notApplicable => new { status = "NotApplicable", reason = IsApplyOnlyGate(pair.Key) ? "review_only_v0" : notApplicable.Reason },
                _ => new { status = "NotApplicable", reason = "unknown" }
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

    private static bool IsApplyOnlyGate(string gate)
    {
        return gate.Equals(EvalGateManifest.Backup, StringComparison.OrdinalIgnoreCase) ||
               gate.Equals(EvalGateManifest.PostProof, StringComparison.OrdinalIgnoreCase) ||
               gate.Equals(EvalGateManifest.Rollback, StringComparison.OrdinalIgnoreCase);
    }

    private static TaskCard BuildReviewCard(
        DateTimeOffset timestamp,
        SpriteRepairQueueRow row,
        SpriteRepairQueueIssue issue,
        string proposalPath,
        string dryRunPath,
        string evalPath)
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Review self-improvement sprite repair proposal for {row.RowId}.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            TargetPathsOrAssets: [row.RowId, row.SpeciesId, row.LifeStage, row.Gender, issue.ColorVariant, issue.AnimationFamily],
            RiskLevel: ToolRiskLevel.Medium,
            NeedsApproval: true,
            ExpectedOutput: "Review proposal only; no sprite mutation.");
        var policy = new ToolPolicy(
            "self-improvement-sprite-repair-batch-proposal",
            SpriteRepairBatchProposalDescriptor.Kind,
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
            ToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            PolicySnapshot: policy,
            Timeline:
            [
                $"{timestamp:O} drafted review-only self-improvement sprite repair proposal.",
                $"proposal={proposalPath}",
                $"dry_run={dryRunPath}",
                $"eval={evalPath}"
            ],
            ResultSummary: "Draft only. No sprite mutation, no candidate generation, no apply.",
            AuditLogPath: "",
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp,
            ReviewPayload: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["experiment_kind"] = SpriteRepairBatchProposalDescriptor.Kind,
                ["mutation_posture"] = SpriteRepairBatchProposalDescriptor.MutationPosture,
                ["proposal_path"] = proposalPath,
                ["dry_run_path"] = dryRunPath,
                ["eval_path"] = evalPath
            });
    }

    private static SpriteRepairQueueIssue BuildFallbackIssue(SpriteRepairQueueRow row)
    {
        return new SpriteRepairQueueIssue(
            row.ColorsAffected.FirstOrDefault() ?? "blue",
            row.AnimationsAffected.FirstOrDefault() ?? "idle",
            row.Priority,
            [],
            [],
            row.RecommendedTools.FirstOrDefault() ?? "",
            "fallback review issue for row with no issue details.",
            null,
            null);
    }

    private static IReadOnlyList<string> ResolveSourcePaths(string repoRoot, SpriteRepairQueueRow row, SpriteRepairQueueIssue issue)
    {
        var paths = new[] { issue.SourcePath, issue.CapturePath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetRelativePath(repoRoot, Path.IsPathRooted(path!) ? path! : Path.Combine(repoRoot, path!)).Replace('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (paths.Length > 0)
        {
            return paths;
        }

        return
        [
            Path.Combine("sprites_runtime", row.SpeciesId, row.LifeStage, row.Gender, issue.ColorVariant, $"{issue.AnimationFamily}_00.png").Replace('\\', '/')
        ];
    }

    private static string ResolveRepoRoot(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, "wevito.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private void Record(string packetKind, Guid taskCardId, DateTimeOffset timestamp, string artifactPath, string status, string summary)
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
}
