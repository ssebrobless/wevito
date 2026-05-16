using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ModelCapabilitySettingsTests
{
    [Fact]
    public void ApplyDefaultSettings_DisablesModelCapabilityAndFirstCallApproval()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>());

        Assert.Equal(bool.FalseString, settings["pet_model_adapter_enabled"]);
        Assert.Equal(bool.FalseString, settings["pet_model_first_call_approved"]);
    }

    [Fact]
    public void ApplyDefaultSettings_InitializesRuntimeSupervisorSafely()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>());

        Assert.Equal(bool.FalseString, settings[RuntimeSupervisorService.QuietModeSetting]);
        Assert.Equal(bool.FalseString, settings[RuntimeSupervisorService.PetOnlyModeSetting]);
        Assert.Equal(bool.FalseString, settings[RuntimeSupervisorService.BackgroundWorkAllowedSetting]);
        Assert.Equal(bool.TrueString, settings[RuntimeSupervisorService.NoFocusStealSetting]);
        Assert.Equal(bool.TrueString, settings[RuntimeSupervisorService.AutoQuietFullscreenSetting]);
        Assert.Equal("4", settings[RuntimeSupervisorService.MaxBackgroundTasksPerHourSetting]);
        Assert.Equal("20", settings[RuntimeSupervisorService.CpuBudgetPercentSetting]);
        Assert.Equal("512", settings[RuntimeSupervisorService.MemoryBudgetMbSetting]);
    }

    [Fact]
    public void ApplyDefaultSettings_PreservesExplicitModelCapabilityChoices()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pet_model_adapter_enabled"] = bool.TrueString,
            ["pet_model_first_call_approved"] = bool.TrueString
        });

        Assert.Equal(bool.TrueString, settings["pet_model_adapter_enabled"]);
        Assert.Equal(bool.TrueString, settings["pet_model_first_call_approved"]);
    }

    [Fact]
    public async Task PetModelSummaryService_DoesNotAppendSummaryWhenCapabilityNotApproved()
    {
        var fake = new FakeModelAdapter();
        var service = new PetModelSummaryService(fake);
        var request = BuildRequest();
        var result = BuildResult(request);
        var helper = new AgentSlotProfile(Guid.NewGuid(), "goose 1", 0);

        var enriched = await service.AppendIfAllowedAsync(request, result, helper, approvedForModelCall: false);

        Assert.False(fake.WasCalled);
        Assert.Equal(result.PreviewSummary, enriched.PreviewSummary);
    }

    [Fact]
    public async Task PetModelSummaryService_CanAppendFakeSummaryWhenCapabilityIsApprovedInTests()
    {
        var fake = new FakeModelAdapter();
        var service = new PetModelSummaryService(fake);
        var request = BuildRequest();
        var result = BuildResult(request);
        var helper = new AgentSlotProfile(Guid.NewGuid(), "goose 1", 0);

        var enriched = await service.AppendIfAllowedAsync(request, result, helper, approvedForModelCall: true);

        Assert.True(fake.WasCalled);
        Assert.Contains("Model suggestion: Keep the sprite body-pose only.", enriched.PreviewSummary);
        Assert.Contains(enriched.WrittenPaths ?? [], path => path.EndsWith("model-call.json", StringComparison.OrdinalIgnoreCase));
    }

    private static TaskAdapterRequest BuildRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-model-capability-settings-tests", Guid.NewGuid().ToString("N"));
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "review goose baby female blue sprites",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit");
        var policy = new ToolPolicy(
            "spriteAudit-readonly",
            "spriteAudit",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [root]);
        return new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: root,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static TaskAdapterResult BuildResult(TaskAdapterRequest request)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            "spriteAudit",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            PreviewSummary: "Sprite audit preview ready.");
    }

    private sealed class FakeModelAdapter : IModelAdapter
    {
        public bool WasCalled { get; private set; }

        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ModelResponse(
                "fake",
                "fake-model",
                "Keep the sprite body-pose only.",
                DidCallProvider: request.ApprovedForModelCall,
                AuditLogPath: Path.Combine(request.ArtifactRoot, "model-call.json")));
        }
    }
}
