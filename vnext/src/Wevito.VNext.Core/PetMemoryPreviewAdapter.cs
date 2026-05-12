using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetMemoryPreviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    string RequestedMemory,
    ToolPolicyDecisionStatus GateStatus,
    ApprovalRequirement ApprovalRequirement,
    string GateReason,
    bool DidWriteMemory,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed class PetMemoryPreviewAdapter
{
    private const string ToolFamily = "petMemory";
    private readonly PetMemoryWriteGate _gate;

    public PetMemoryPreviewAdapter(PetMemoryWriteGate? gate = null)
    {
        _gate = gate ?? new PetMemoryWriteGate();
    }

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Pet memory writes only support dry-run preview in this phase.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target petMemory.", timestamp);
        }

        var decision = _gate.Evaluate(new PetMemoryWriteRequest(
            request.Intent.TargetPetId ?? Guid.Empty,
            request.Intent.TargetPetNameSnapshot,
            request.Intent.RequestedToolFamily,
            request.Intent.RawText,
            "pending-user-approved-memory",
            ContainsUntrustedContent: request.Intent.RawText.Contains("<untrusted>", StringComparison.OrdinalIgnoreCase),
            Approved: false));

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        Directory.CreateDirectory(artifactRoot);
        var report = new PetMemoryPreviewReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            request.Intent.RawText,
            decision.Status,
            decision.ApprovalRequirement,
            decision.Reason,
            DidWriteMemory: false,
            DidMutate: false,
            timestamp);
        var jsonPath = Path.Combine(artifactRoot, "pet-memory-preview-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Pet memory write is gated: {decision.Reason}",
            ResultSummary: $"petMemory preview ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-pet-memory";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug));
    }

    private static string BuildMarkdown(PetMemoryPreviewReport report)
    {
        return string.Join(Environment.NewLine, [
            "# PET TASKS Pet Memory Preview",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Gate",
            "",
            $"- Status: {report.GateStatus}",
            $"- Approval: {report.ApprovalRequirement}",
            $"- Reason: {report.GateReason}",
            "- Did write memory: false",
            "- Did mutate: false",
            "",
            "Memory writes are not performed by preview. They require an explicit approved execution path."
        ]) + Environment.NewLine;
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }
}
