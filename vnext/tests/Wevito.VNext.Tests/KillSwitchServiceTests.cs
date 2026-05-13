using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class KillSwitchServiceTests
{
    [Fact]
    public void IsActive_DefaultsFalseAndPersistsFromSettings()
    {
        Assert.False(KillSwitchService.IsActive(new Dictionary<string, string>()));
        Assert.True(KillSwitchService.IsActive(new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        }));
    }

    [Fact]
    public void KillSwitchBlocksPreviewDispatcher()
    {
        var root = CreateTempRoot();
        var dispatcher = new PetTaskAdapterPreviewDispatcher(killSwitchService: ActiveKillSwitch(root));

        var result = dispatcher.BuildPreview(BuildRequest(root));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Equal("kill_switch=true", result.BlockReason);
    }

    [Fact]
    public async Task KillSwitchBlocksSchedulerPromotionEvalAndModelAdapter()
    {
        var root = CreateTempRoot();
        var killSwitch = ActiveKillSwitch(root);
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString,
            [AutonomousTaskScheduler.SchedulerEnabledSetting] = bool.TrueString
        };
        var scheduler = new AutonomousTaskScheduler(new RuntimeSupervisorService(), new RuntimeBudgetMeter(Path.Combine(root, "budget.json")), killSwitchService: killSwitch);
        var proposal = scheduler.TryCreateProposal(new AutonomousSchedulerRequest(
            settings,
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            new RuntimeBudgetSnapshot(4, 20, 512),
            [new SchedulerTrigger(SchedulerTriggerKind.StaleDashboardReport, "detail", "summarize docs", "localDocs", DateTimeOffset.Parse("2026-05-12T12:00:00Z"))],
            [],
            Path.Combine(root, "artifacts"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));
        Assert.False(proposal.Created);
        Assert.Equal("kill_switch=true", proposal.BlockReason);

        var promotion = new LearningLabPromotionService(killSwitchService: killSwitch).Promote(new LearningLabPromotionRequest(
            Path.Combine(root, "bundle"),
            Path.Combine(root, "datasets"),
            Path.Combine(root, "artifacts"),
            BuildApprovedCard(),
            Guid.NewGuid(),
            "goose 1",
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));
        Assert.False(promotion.Succeeded);
        Assert.Equal("kill_switch=true", promotion.Message);

        var eval = new LearningEvalService(killSwitchService: killSwitch).Evaluate(new LearningEvalRequest(
            Path.Combine(root, "datasets"),
            Path.Combine(root, "artifacts"),
            Path.Combine(root, "baseline.json"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));
        Assert.False(eval.Succeeded);
        Assert.Equal("kill_switch=true", eval.Message);

        var model = new LocalModelAdapter(killSwitch);
        var response = await model.SuggestAsync(new ModelRequest(Guid.NewGuid(), "goose 1", PetHelperRole.ResearchHelper, "localDocs", "task", "summary"));
        Assert.False(response.DidCallProvider);
        Assert.Equal("kill_switch=true", response.BlockReason);
    }

    private static KillSwitchService ActiveKillSwitch(string root)
    {
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        return new KillSwitchService(() => settings, new AuditLedgerService(Path.Combine(root, "ledger.sqlite")));
    }

    private static TaskAdapterRequest BuildRequest(string root)
    {
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "summarize docs",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.SummarizeDocs,
            RequestedToolFamily: "localDocs");
        var policy = new ToolPolicy("local-docs", "localDocs", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None, ApprovedRootPaths: [root]);
        return new TaskAdapterRequest(Guid.Parse("90000000-0000-0000-0000-000000000001"), intent, policy, ArtifactRoot: Path.Combine(root, "artifacts"));
    }

    private static TaskCard BuildApprovedCard()
    {
        var id = Guid.Parse("70000000-0000-0000-0000-000000000001");
        return new TaskCard(
            id,
            new TaskIntent(id, "promote learning", TaskIntentTargetMode.RouteToBestHelper, TaskKind: TaskKind.UpdatePetMemory, RequestedToolFamily: LearningLabPromotionService.ToolFamily),
            TaskCardStatus.Approved,
            ToolFamily: LearningLabPromotionService.ToolFamily,
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-kill-switch-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
