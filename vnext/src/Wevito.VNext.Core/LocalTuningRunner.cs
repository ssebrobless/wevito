using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record LocalTuningRunRequest(
    string RepoRoot,
    string ContentRoot,
    string ArtifactRoot,
    string DatasetVersion,
    LocalTrainingStage Stage,
    bool DryRun,
    LearningEvalMetrics BaselineMetrics,
    LearningEvalMetrics CandidateMetrics,
    DateTimeOffset CreatedAtUtc,
    double RegressionTolerance = 0.02,
    IReadOnlyDictionary<string, string>? Settings = null);

public sealed record LocalTuningRunResult(
    bool Succeeded,
    bool RolledBack,
    bool DidMutate,
    string TargetPath,
    string BackupPath,
    string PreSha256,
    string PostSha256,
    string ArtifactFolder,
    string Message);

public sealed class LocalTuningRunner
{
    public const string ApplyPacketKind = "tuning_apply";
    public const string RollbackPacketKind = "tuning_rollback";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public LocalTuningRunner(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public LocalTuningRunResult Run(LocalTuningRunRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return Block(request, "kill_switch=true");
        }

        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var contentRoot = Path.GetFullPath(request.ContentRoot);
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        LocalTrainingPlanService.EnsureAllowedWriteRoot(contentRoot, repoRoot, "vnext", "content");
        LocalTrainingPlanService.EnsureAllowedWriteRoot(artifactRoot, repoRoot, "vnext", "artifacts");
        Directory.CreateDirectory(artifactRoot);
        var runFolder = Path.Combine(artifactRoot, $"{request.CreatedAtUtc:yyyyMMdd-HHmmss}-local-tuning-{request.Stage.ToString().ToLowerInvariant()}");
        Directory.CreateDirectory(runFolder);

        var targetPath = ResolveTargetPath(request.Stage, contentRoot);
        PromptConfigStore.EnsureUnderRoot(targetPath, contentRoot, "Tuning target must stay under vnext/content.");
        if (request.DryRun)
        {
            var dryRunPath = Path.Combine(runFolder, "dry-run.json");
            File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new
            {
                request.Stage,
                request.DatasetVersion,
                targetPath,
                wouldMutate = false
            }, JsonDefaults.Options));
            return new LocalTuningRunResult(true, false, false, targetPath, "", "", "", runFolder, "Dry-run completed without mutation.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? contentRoot);
        var hadExisting = File.Exists(targetPath);
        var preHash = hadExisting ? PromptConfigStore.Sha256(targetPath) : "";
        var backupPath = "";
        if (hadExisting)
        {
            backupPath = Path.Combine(runFolder, "backup-" + Path.GetFileName(targetPath));
            File.Copy(targetPath, backupPath, overwrite: true);
        }

        WriteCandidate(request, targetPath);
        var postHash = PromptConfigStore.Sha256(targetPath);
        var comparison = LearningEvalService.EvaluateAgainst(request.CandidateMetrics, request.BaselineMetrics, request.RegressionTolerance);
        var rolledBack = false;
        if (comparison.Regression)
        {
            Rollback(targetPath, backupPath, hadExisting);
            rolledBack = true;
            if (hadExisting && PromptConfigStore.Sha256(targetPath) != preHash)
            {
                throw new InvalidOperationException("Rollback failed to restore byte-exact pre-apply hash.");
            }
        }

        File.WriteAllText(Path.Combine(runFolder, "tuning-result.json"), JsonSerializer.Serialize(new
        {
            request.Stage,
            request.DatasetVersion,
            targetPath,
            backupPath,
            preSha256 = preHash,
            postSha256 = postHash,
            comparison,
            rolledBack
        }, JsonDefaults.Options));
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            rolledBack ? RollbackPacketKind : ApplyPacketKind,
            TaskCardId: null,
            request.CreatedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            runFolder,
            rolledBack ? "Local tuning rolled back after eval regression." : "Local tuning applied after eval gate.",
            rolledBack ? "RolledBack" : "Completed"));

        return new LocalTuningRunResult(
            true,
            rolledBack,
            DidMutate: true,
            targetPath,
            backupPath,
            preHash,
            rolledBack && hadExisting ? PromptConfigStore.Sha256(targetPath) : postHash,
            runFolder,
            rolledBack ? "Eval regression exceeded tolerance; rollback restored pre-apply bytes." : "Tuning applied after eval gate.");
    }

    private static string ResolveTargetPath(LocalTrainingStage stage, string contentRoot)
    {
        return stage switch
        {
            LocalTrainingStage.RetrievalRerank => Path.Combine(contentRoot, "local-ai", "rerank-head.json"),
            LocalTrainingStage.Routing => Path.Combine(contentRoot, "local-ai", "router-config.json"),
            LocalTrainingStage.PromptConfig => Path.Combine(contentRoot, PromptConfigStore.RelativeConfigPath),
            LocalTrainingStage.LoraPilot => throw new InvalidOperationException("LoRA pilot is plan-only in C-PHASE 73 and cannot be executed."),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported tuning stage.")
        };
    }

    private static void WriteCandidate(LocalTuningRunRequest request, string targetPath)
    {
        object payload = request.Stage switch
        {
            LocalTrainingStage.RetrievalRerank => RerankHead.CreateDefault(request.DatasetVersion, request.CreatedAtUtc),
            LocalTrainingStage.Routing => RouterConfig.CreateDefault(request.DatasetVersion, request.CreatedAtUtc),
            LocalTrainingStage.PromptConfig => PromptConfig.CreateDefault(request.DatasetVersion, request.CreatedAtUtc),
            _ => throw new InvalidOperationException("Unsupported tuning stage.")
        };
        File.WriteAllText(targetPath, JsonSerializer.Serialize(payload, JsonDefaults.Options));
    }

    private static void Rollback(string targetPath, string backupPath, bool hadExisting)
    {
        if (hadExisting)
        {
            File.Copy(backupPath, targetPath, overwrite: true);
        }
        else if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }

    private static LocalTuningRunResult Block(LocalTuningRunRequest request, string reason)
    {
        return new LocalTuningRunResult(false, false, false, "", "", "", "", request.ArtifactRoot, reason);
    }
}
