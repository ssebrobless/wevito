using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutonomousTaskSchedulerTests
{
    [Fact]
    public void TryCreateProposal_DefaultDisabledDoesNotEmitCard()
    {
        var scheduler = BuildScheduler(out _);
        var request = BuildRequest(settings: new Dictionary<string, string>());

        var result = scheduler.TryCreateProposal(request);

        Assert.False(result.Created);
        Assert.Null(result.TaskCard);
        Assert.Contains("disabled", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RuntimeSupervisorMode.Quiet)]
    [InlineData(RuntimeSupervisorMode.PetOnly)]
    public void TryCreateProposal_DormantWhenSupervisorIsNotActive(RuntimeSupervisorMode mode)
    {
        var scheduler = BuildScheduler(out _);
        var request = BuildRequest(status: new RuntimeSupervisorStatus(mode, false, false, mode == RuntimeSupervisorMode.Quiet, "paused", $"{mode} blocks helper background work."));

        var result = scheduler.TryCreateProposal(request);

        Assert.False(result.Created);
        Assert.Contains("blocks", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateProposal_DormantWhenBackgroundWorkIsDisabled()
    {
        var scheduler = BuildScheduler(out _);
        var request = BuildRequest(status: new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, false, true, false, "active", "Background helper work is disabled."));

        var result = scheduler.TryCreateProposal(request);

        Assert.False(result.Created);
        Assert.Contains("disabled", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateProposal_EmitsDraftOnlyAndWritesEvidencePacket()
    {
        var scheduler = BuildScheduler(out var artifactRoot);
        var request = BuildRequest(artifactRoot: artifactRoot);

        var result = scheduler.TryCreateProposal(request);

        Assert.True(result.Created, result.BlockReason);
        Assert.NotNull(result.TaskCard);
        Assert.Equal(TaskCardStatus.Draft, result.TaskCard.Status);
        Assert.Equal("localDocs", result.TaskCard.ToolFamily);
        Assert.False(result.EvidencePacket?.DidDispatchAdapter);
        Assert.False(result.EvidencePacket?.DidMutate);
        Assert.False(result.EvidencePacket?.DidUseNetwork);
        Assert.False(result.EvidencePacket?.DidUseHostedAi);
        Assert.True(File.Exists(Path.Combine(result.ArtifactFolder, "scheduler-proposal.json")));
        Assert.True(File.Exists(Path.Combine(result.ArtifactFolder, "run-summary.md")));
        Assert.StartsWith(artifactRoot, result.ArtifactFolder, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateProposal_RespectsHourlyBudget()
    {
        var scheduler = BuildScheduler(out _);
        var first = scheduler.TryCreateProposal(BuildRequest(budget: new RuntimeBudgetSnapshot(1, 90, 1024)));
        var second = scheduler.TryCreateProposal(BuildRequest(
            budget: new RuntimeBudgetSnapshot(1, 90, 1024),
            existingCards: []));

        Assert.True(first.Created, first.BlockReason);
        Assert.False(second.Created);
        Assert.Contains("exhausted", second.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateProposal_DoesNotDuplicateExistingDraftForSameTrigger()
    {
        var scheduler = BuildScheduler(out _);
        var existing = BuildExistingSchedulerDraft();
        var request = BuildRequest(existingCards: [existing]);

        var result = scheduler.TryCreateProposal(request);

        Assert.False(result.Created);
        Assert.Contains("already exists", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildShellTriggers_OnlyUsesAllowedTriggerKinds()
    {
        var now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");

        var triggers = AutonomousTaskScheduler.BuildShellTriggers([], new Dictionary<string, string>(), now);

        Assert.Single(triggers);
        Assert.Equal(SchedulerTriggerKind.StaleDashboardReport, triggers[0].Kind);
        Assert.Equal("localDocs", triggers[0].ToolFamily);
    }

    private static AutonomousTaskScheduler BuildScheduler(out string artifactRoot)
    {
        artifactRoot = Path.Combine(Path.GetTempPath(), "wevito-scheduler-tests", Guid.NewGuid().ToString("N"), "pet-tasks");
        var meterPath = Path.Combine(Path.GetTempPath(), "wevito-scheduler-tests", Guid.NewGuid().ToString("N"), "budget-meter.json");
        var now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");
        var meter = new RuntimeBudgetMeter(meterPath, () => now, () => new RuntimeResourceSnapshot(0, 128, now));
        return new AutonomousTaskScheduler(new RuntimeSupervisorService(), meter);
    }

    private static AutonomousSchedulerRequest BuildRequest(
        IReadOnlyDictionary<string, string>? settings = null,
        RuntimeSupervisorStatus? status = null,
        RuntimeBudgetSnapshot? budget = null,
        IReadOnlyList<TaskCard>? existingCards = null,
        string? artifactRoot = null)
    {
        var now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");
        return new AutonomousSchedulerRequest(
            settings ?? new Dictionary<string, string>
            {
                [AutonomousTaskScheduler.SchedulerEnabledSetting] = bool.TrueString
            },
            status ?? new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            budget ?? new RuntimeBudgetSnapshot(4, 90, 1024),
            [
                new SchedulerTrigger(
                    SchedulerTriggerKind.StaleDashboardReport,
                    "Dashboard report is stale.",
                    "summarize the local docs",
                    "localDocs",
                    now,
                    ["docs"])
            ],
            existingCards ?? [],
            artifactRoot ?? Path.Combine(Path.GetTempPath(), "wevito-scheduler-tests", Guid.NewGuid().ToString("N"), "pet-tasks"),
            now);
    }

    private static TaskCard BuildExistingSchedulerDraft()
    {
        var now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "summarize the local docs",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.SummarizeDocs,
            RequestedToolFamily: "localDocs",
            CreatedAtUtc: now);
        return new TaskCard(
            intent.Id,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: "localDocs",
            Timeline: ["scheduler_proposed: stale_dashboard_report"],
            CreatedAtUtc: now,
            UpdatedAtUtc: now);
    }
}
