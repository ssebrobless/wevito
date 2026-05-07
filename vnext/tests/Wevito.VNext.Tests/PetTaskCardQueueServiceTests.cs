using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetTaskCardQueueServiceTests
{
    [Fact]
    public void AppendDraft_AddsNewestCardFirstAndCapsQueue()
    {
        var service = new PetTaskCardQueueService(maxCards: 2);
        var older = BuildCard("older", DateTimeOffset.Parse("2026-05-05T10:00:00Z"));
        var middle = BuildCard("middle", DateTimeOffset.Parse("2026-05-05T10:01:00Z"));
        var newer = BuildCard("newer", DateTimeOffset.Parse("2026-05-05T10:02:00Z"));

        var queue = service.AppendDraft([older, middle], newer);

        Assert.Equal(2, queue.Count);
        Assert.Equal("newer", queue[0].Intent.RawText);
        Assert.Equal("middle", queue[1].Intent.RawText);
        Assert.DoesNotContain(queue, card => card.Intent.RawText == "older");
    }

    [Fact]
    public void AppendDraft_ReplacesExistingCardWithSameId()
    {
        var service = new PetTaskCardQueueService();
        var createdAt = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var original = BuildCard("review sprites", createdAt);
        var updated = original with
        {
            Status = TaskCardStatus.WaitingForApproval,
            UpdatedAtUtc = createdAt.AddMinutes(1),
            ResultSummary = "waiting"
        };

        var queue = service.AppendDraft([original], updated);

        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.WaitingForApproval, queue[0].Status);
        Assert.Equal("waiting", queue[0].ResultSummary);
    }

    [Fact]
    public void TryTransitionStatus_ApprovesWaitingCardWithoutRunningIt()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var card = BuildCard("run build proof", timestamp) with
        {
            Status = TaskCardStatus.WaitingForApproval
        };

        var changed = service.TryTransitionStatus(
            [card],
            card.Id,
            TaskCardStatus.Approved,
            timestamp.AddMinutes(1),
            out var queue,
            out var updatedCard,
            out var reason);

        Assert.True(changed, reason);
        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.Approved, updatedCard?.Status);
        Assert.Contains(updatedCard?.Timeline ?? [], item => item.StartsWith("user_approved:", StringComparison.Ordinal));
        Assert.Contains("future execution adapter", updatedCard?.ResultSummary);
    }

    [Fact]
    public void TryTransitionStatus_BlocksApprovalWhenCardIsOnlyDraft()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var card = BuildCard("review sprites", timestamp);

        var changed = service.TryTransitionStatus(
            [card],
            card.Id,
            TaskCardStatus.Approved,
            timestamp.AddMinutes(1),
            out var queue,
            out var updatedCard,
            out var reason);

        Assert.False(changed);
        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.Draft, updatedCard?.Status);
        Assert.Contains("Cannot move", reason);
    }

    [Fact]
    public void TryTransitionStatus_CancelsDraftWithoutExecution()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T10:00:00Z");
        var card = BuildCard("review sprites", timestamp);

        var changed = service.TryTransitionStatus(
            [card],
            card.Id,
            TaskCardStatus.Cancelled,
            timestamp.AddMinutes(1),
            out _,
            out var updatedCard,
            out var reason);

        Assert.True(changed, reason);
        Assert.Equal(TaskCardStatus.Cancelled, updatedCard?.Status);
        Assert.Contains("before execution", updatedCard?.ResultSummary);
    }

    [Fact]
    public void TryApplyAdapterResult_MovesDraftPreviewToReviewingWithAuditPath()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T13:30:00Z");
        var card = BuildCard("review sprites", timestamp);
        var result = new TaskAdapterResult(
            card.Id,
            "spriteAudit",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            PreviewSummary: "Wrote spriteAudit markdown and JSON reports.",
            ResultSummary: "spriteAudit report ready",
            AuditLogPath: "C:\\temp\\pet-tasks\\run-summary.md",
            CompletedAtUtc: timestamp);

        var changed = service.TryApplyAdapterResult([card], result, timestamp.AddMinutes(1), out var queue, out var updatedCard, out var reason);

        Assert.True(changed, reason);
        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.Reviewing, updatedCard?.Status);
        Assert.Equal("spriteAudit report ready", updatedCard?.ResultSummary);
        Assert.Equal("C:\\temp\\pet-tasks\\run-summary.md", updatedCard?.AuditLogPath);
        Assert.Contains(updatedCard?.Timeline ?? [], entry => entry.StartsWith("preview_ready:", StringComparison.Ordinal));
    }

    [Fact]
    public void TryApplyAdapterResult_BlocksPreviewFromWaitingForApproval()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T13:30:00Z");
        var card = BuildCard("run build proof", timestamp) with
        {
            Status = TaskCardStatus.WaitingForApproval
        };
        var result = new TaskAdapterResult(
            card.Id,
            "spriteAudit",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            PreviewSummary: "preview",
            CompletedAtUtc: timestamp);

        var changed = service.TryApplyAdapterResult([card], result, timestamp.AddMinutes(1), out var queue, out var updatedCard, out var reason);

        Assert.False(changed);
        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.WaitingForApproval, updatedCard?.Status);
        Assert.Contains("Cannot preview", reason);
    }

    [Fact]
    public void TryApplyAdapterResult_MovesReviewingExecutionToDoneWithAuditPath()
    {
        var service = new PetTaskCardQueueService();
        var timestamp = DateTimeOffset.Parse("2026-05-05T14:45:00Z");
        var card = BuildCard("translate Hello goose to Spanish", timestamp) with
        {
            Status = TaskCardStatus.Reviewing,
            ToolFamily = "translateText",
            Intent = new TaskIntent(
                Guid.NewGuid(),
                "translate Hello goose to Spanish",
                TaskIntentTargetMode.RouteToBestHelper,
                TaskKind: TaskKind.TranslateText,
                RequestedToolFamily: "translateText",
                CreatedAtUtc: timestamp)
        };
        var result = new TaskAdapterResult(
            card.Id,
            "translateText",
            TaskAdapterResultStatus.Completed,
            DidMutate: false,
            ResultSummary: "translateText execution complete",
            AuditLogPath: "C:\\temp\\pet-tasks\\translation\\run-summary.md",
            CompletedAtUtc: timestamp);

        var changed = service.TryApplyAdapterResult([card], result, timestamp.AddMinutes(1), out var queue, out var updatedCard, out var reason);

        Assert.True(changed, reason);
        Assert.Single(queue);
        Assert.Equal(TaskCardStatus.Done, updatedCard?.Status);
        Assert.Equal("translateText execution complete", updatedCard?.ResultSummary);
        Assert.Equal("C:\\temp\\pet-tasks\\translation\\run-summary.md", updatedCard?.AuditLogPath);
        Assert.Contains(updatedCard?.Timeline ?? [], entry => entry.StartsWith("adapter_completed:", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveArtifactReportPath_AllowsRunSummaryUnderPetTasksRoot()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "wevito-artifact-control-tests", Guid.NewGuid().ToString("N"));
        var reportPath = Path.Combine(repoRoot, "vnext", "artifacts", "pet-tasks", "20260506-local-docs", "run-summary.md");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, "# report");

        var resolution = PetTaskCardQueueService.ResolveArtifactReportPath(reportPath, repoRoot);

        Assert.True(resolution.IsAllowed, resolution.BlockReason);
        Assert.Equal(Path.GetFullPath(reportPath), resolution.ReportPath);
        Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(reportPath)), resolution.ArtifactFolder);
    }

    [Fact]
    public void ResolveArtifactReportPath_ResolvesDirectoryToRunSummary()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "wevito-artifact-control-tests", Guid.NewGuid().ToString("N"));
        var artifactFolder = Path.Combine(repoRoot, "vnext", "artifacts", "pet-tasks", "20260506-local-docs");
        Directory.CreateDirectory(artifactFolder);

        var resolution = PetTaskCardQueueService.ResolveArtifactReportPath(artifactFolder, repoRoot);

        Assert.True(resolution.IsAllowed, resolution.BlockReason);
        Assert.Equal(Path.Combine(Path.GetFullPath(artifactFolder), "run-summary.md"), resolution.ReportPath);
        Assert.Equal(Path.GetFullPath(artifactFolder), resolution.ArtifactFolder);
    }

    [Fact]
    public void ResolveArtifactReportPath_BlocksPathOutsidePetTasksRoot()
    {
        var repoRoot = Path.Combine(Path.GetTempPath(), "wevito-artifact-control-tests", Guid.NewGuid().ToString("N"));
        var outsideReport = Path.Combine(repoRoot, "docs", "run-summary.md");

        var resolution = PetTaskCardQueueService.ResolveArtifactReportPath(outsideReport, repoRoot);

        Assert.False(resolution.IsAllowed);
        Assert.Contains("outside artifact root", resolution.BlockReason);
    }

    private static TaskCard BuildCard(string rawText, DateTimeOffset timestamp)
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            rawText,
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            CreatedAtUtc: timestamp);

        return new TaskCard(
            intent.Id,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: "spriteAudit",
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);
    }
}
