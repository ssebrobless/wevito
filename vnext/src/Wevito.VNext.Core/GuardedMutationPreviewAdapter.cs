using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class GuardedMutationPreviewAdapter
{
    public const string ToolFamily = "guardedMutation";
    public const string PilotScopeId = "guarded-mutation-pilot";

    public GuardedMutationPilotProposal Propose(
        string repoRoot,
        string relativeTargetPath,
        string proposedContent,
        string artifactRoot,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var root = Path.GetFullPath(repoRoot);
        var approvedRoot = Path.GetFullPath(Path.Combine(root, "vnext", "content", PilotScopeId));
        var target = Path.GetFullPath(Path.Combine(root, relativeTargetPath));
        if (!target.StartsWith(approvedRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return new GuardedMutationPilotProposal(
                false,
                null,
                null,
                null,
                "Pilot mutation target must stay under vnext/content/guarded-mutation-pilot/.");
        }

        var taskCardId = Guid.NewGuid();
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Propose guarded pilot mutation for {relativeTargetPath}",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.PlanCodePatch,
            RequestedToolFamily: ToolFamily,
            TargetPathsOrAssets: [target],
            RiskLevel: ToolRiskLevel.High,
            NeedsApproval: true,
            ExpectedOutput: "Draft guarded mutation plan only.",
            CreatedAtUtc: timestamp);
        var policy = new ToolPolicy(
            "guarded-mutation-pilot",
            ToolFamily,
            ToolAccessMode.Write,
            ToolRiskLevel.High,
            ApprovalRequirement.BeforeExecution,
            IsEnabled: false,
            ApprovedRootPaths: [approvedRoot],
            BlockReason: "Pilot is default-disabled until an operator approves the task card.");
        var taskCard = new TaskCard(
            taskCardId,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: ToolFamily,
            PolicySnapshot: policy,
            Timeline: ["guarded_mutation_pilot_proposed: draft only"],
            ResultSummary: "Draft guarded mutation pilot proposal created. Nothing has been applied.",
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);
        var plan = new GuardedMutationPlan(
            "1",
            Guid.NewGuid(),
            taskCardId,
            PilotScopeId,
            root,
            [approvedRoot],
            [new GuardedMutationEdit(target, proposedContent)],
            [],
            DryRunOnly: false,
            timestamp);
        var preview = BuildPreview(new TaskAdapterRequest(taskCardId, intent, policy, ArtifactRoot: artifactRoot, RequestedAtUtc: timestamp), timestamp);
        return new GuardedMutationPilotProposal(true, plan, taskCard, preview, "");
    }

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Policy must target guardedMutation.", timestamp);
        }

        var artifactRoot = string.IsNullOrWhiteSpace(request.ArtifactRoot)
            ? Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", $"{timestamp:yyyyMMdd-HHmmss}-guarded-mutation-preview"))
            : Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);
        var report = new
        {
            schemaVersion = "1",
            request.TaskCardId,
            toolFamily = ToolFamily,
            requested = request.Intent.RawText,
            targetPaths = request.Intent.TargetPathsOrAssets ?? [],
            didMutate = false,
            requiredFlow = new[] { "dry-run", "backup", "apply", "post-proof", "rollback-on-failure" },
            safety = "Preview only. No file edits were attempted."
        };
        var jsonPath = Path.Combine(artifactRoot, "guarded-mutation-preview.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, string.Join(Environment.NewLine, [
            "# Guarded Mutation Preview",
            "",
            $"- TaskCard: `{request.TaskCardId}`",
            $"- Requested: {request.Intent.RawText}",
            "- Did mutate: false",
            "- Required flow: dry-run -> backup -> apply -> post-proof -> rollback-on-failure",
            "",
            "This adapter creates a preview packet only. Execution requires an approved task card and GuardedMutationService."
        ]));
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: request.Intent.TargetPathsOrAssets ?? [],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: "Guarded mutation preview packet created. No files were changed.",
            ResultSummary: markdownPath,
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(request.TaskCardId, ToolFamily, TaskAdapterResultStatus.Blocked, false, [], [], BlockReason: reason, CompletedAtUtc: timestamp);
    }
}

public sealed record GuardedMutationPilotProposal(
    bool Succeeded,
    GuardedMutationPlan? Plan,
    TaskCard? TaskCard,
    TaskAdapterResult? Preview,
    string BlockReason);
