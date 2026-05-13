using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum LocalTrainingStage
{
    RetrievalRerank,
    Routing,
    PromptConfig,
    LoraPilot
}

public enum LocalTrainingStageStatus
{
    Planned,
    Blocked,
    PlanOnly
}

public sealed record LocalTrainingPlanRequest(
    string RepoRoot,
    string DatasetVersion,
    string ContentRoot,
    string ArtifactRoot,
    IReadOnlyDictionary<string, string> Settings,
    DateTimeOffset CreatedAtUtc,
    bool DryRun = true);

public sealed record LocalTrainingStagePlan(
    LocalTrainingStage Stage,
    LocalTrainingStageStatus Status,
    bool MayMutate,
    string OutputPath,
    string Reason);

public sealed record LocalTrainingPlan(
    string SchemaVersion,
    string DatasetVersion,
    bool DryRun,
    IReadOnlyList<LocalTrainingStagePlan> Stages,
    DateTimeOffset CreatedAtUtc);

public sealed record LocalTrainingPlanResult(
    bool Succeeded,
    LocalTrainingPlan Plan,
    string PlanPath,
    string SummaryPath,
    string Message);

public sealed class LocalTrainingPlanService
{
    public const string PacketKind = "train_plan";
    public const string LoraEnabledSetting = "tuning_lora_enabled";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public LocalTrainingPlanService(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public LocalTrainingPlanResult CreatePlan(LocalTrainingPlanRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            var blocked = new LocalTrainingPlan("1", request.DatasetVersion, request.DryRun, [], request.CreatedAtUtc);
            return new LocalTrainingPlanResult(false, blocked, "", "", "kill_switch=true");
        }

        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        EnsureAllowedWriteRoot(artifactRoot, Path.GetFullPath(request.RepoRoot), "vnext", "artifacts");
        Directory.CreateDirectory(artifactRoot);
        var runFolder = Path.Combine(artifactRoot, $"{request.CreatedAtUtc:yyyyMMdd-HHmmss}-local-training-plan");
        Directory.CreateDirectory(runFolder);

        var loraEnabled = request.Settings.TryGetValue(LoraEnabledSetting, out var raw) &&
                          bool.TryParse(raw, out var enabled) &&
                          enabled;
        var stages = new List<LocalTrainingStagePlan>
        {
            new(LocalTrainingStage.RetrievalRerank, LocalTrainingStageStatus.Planned, !request.DryRun, Path.Combine(request.ContentRoot, "local-ai", "rerank-head.json"), "Retrieval tuning applies a deterministic rerank head after eval approval."),
            new(LocalTrainingStage.Routing, LocalTrainingStageStatus.Planned, !request.DryRun, Path.Combine(request.ContentRoot, "local-ai", "router-config.json"), "Routing tuning updates deterministic helper routing thresholds only after eval approval."),
            new(LocalTrainingStage.PromptConfig, LocalTrainingStageStatus.Planned, !request.DryRun, Path.Combine(request.ContentRoot, PromptConfigStore.RelativeConfigPath), "Prompt config tuning adjusts local templates/top-k/thresholds only after eval approval."),
            new(LocalTrainingStage.LoraPilot, loraEnabled ? LocalTrainingStageStatus.PlanOnly : LocalTrainingStageStatus.Blocked, false, Path.Combine(runFolder, "lora-plan.md"), loraEnabled ? "LoRA pilot is plan-only in C-PHASE 73; scripts must not run Python here." : "tuning_lora_enabled=false; LoRA is refused by default.")
        };
        var plan = new LocalTrainingPlan("1", request.DatasetVersion, request.DryRun, stages, request.CreatedAtUtc);
        var planPath = Path.Combine(runFolder, "train-plan.json");
        var summaryPath = Path.Combine(runFolder, "run-summary.md");
        var loraPlanPath = Path.Combine(runFolder, "lora-plan.md");
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, JsonDefaults.Options));
        File.WriteAllText(summaryPath, BuildSummary(plan));
        File.WriteAllText(loraPlanPath, BuildLoraPlan(request.DatasetVersion, loraEnabled));
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            TaskCardId: null,
            request.CreatedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            runFolder,
            $"Created local training plan for {request.DatasetVersion}; lora_enabled={loraEnabled.ToString().ToLowerInvariant()}.",
            "Completed"));
        return new LocalTrainingPlanResult(true, plan, planPath, summaryPath, "Local training plan created.");
    }

    public static void EnsureAllowedWriteRoot(string targetRoot, string repoRoot, params string[] relativeSegments)
    {
        var allowedRoot = Path.GetFullPath(Path.Combine([repoRoot, .. relativeSegments]))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Training writes are limited to {allowedRoot}.");
        }
    }

    private static string BuildSummary(LocalTrainingPlan plan)
    {
        var lines = new List<string>
        {
            "# Local Training Plan",
            "",
            $"- Dataset: {plan.DatasetVersion}",
            $"- Dry run: {plan.DryRun.ToString().ToLowerInvariant()}",
            ""
        };
        foreach (var stage in plan.Stages)
        {
            lines.Add($"- {stage.Stage}: {stage.Status}; may_mutate={stage.MayMutate.ToString().ToLowerInvariant()}");
        }

        lines.Add("");
        lines.Add("No hosted AI, web access, Python training, or config mutation is performed by plan creation.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildLoraPlan(string datasetVersion, bool loraEnabled)
    {
        return string.Join(Environment.NewLine, [
            "# LoRA Pilot Plan",
            "",
            $"- Dataset: {datasetVersion}",
            $"- tuning_lora_enabled: {loraEnabled.ToString().ToLowerInvariant()}",
            "- C-PHASE 73 execution status: plan-only",
            "- Training launched: false",
            "- Model downloads: false",
            "- Hosted AI use: false",
            "",
            "This packet documents the future LoRA handoff shape only. The companion scripts refuse real training in this phase."
        ]);
    }
}
