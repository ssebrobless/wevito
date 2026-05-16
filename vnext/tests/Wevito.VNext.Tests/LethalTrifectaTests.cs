using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LethalTrifectaTests
{
    [Fact]
    public void Evaluate_BlocksCapabilityCombiningUntrustedPrivateAndNetwork()
    {
        var evaluator = new HelperAllowlistEvaluator(
            [
                new HelperToolCapability(
                    "Agent slot 1",
                    "goose 1",
                    "dangerTool",
                    ReadsUntrustedExternal: true,
                    ReadsPrivateData: true,
                    SendsNetwork: true)
            ]);
        var helper = new AgentSlotProfile(Guid.NewGuid(), "goose 1", 0, AllowedToolFamilies: ["dangerTool"]);

        var decision = evaluator.Evaluate(helper, "dangerTool");

        Assert.False(decision.IsAllowed);
        Assert.Contains("untrusted", decision.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Single(evaluator.FindLethalTrifectaViolations());
    }

    [Fact]
    public async Task PetModelSummaryService_AppendsFakeModelSummaryOnlyWhenAllowedAndApproved()
    {
        var fake = new FakeModelAdapter();
        var service = new PetModelSummaryService(fake);
        var request = BuildRequest("spriteAudit");
        var result = new TaskAdapterResult(
            request.TaskCardId,
            "spriteAudit",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            PreviewSummary: "Sprite audit preview ready.");
        var helper = new AgentSlotProfile(Guid.NewGuid(), "goose 1", 0);

        var enriched = await service.AppendIfAllowedAsync(request, result, helper, approvedForModelCall: true);

        Assert.True(fake.WasCalled);
        Assert.Contains("Model suggestion: Check the flagged outlines.", enriched.PreviewSummary);
        Assert.Contains(enriched.WrittenPaths ?? [], path => path.EndsWith("model-call.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PetModelSummaryService_SkipsModelWhenHelperToolIsNotAllowed()
    {
        var fake = new FakeModelAdapter();
        var service = new PetModelSummaryService(fake);
        var request = BuildRequest("spriteAudit");
        var result = new TaskAdapterResult(
            request.TaskCardId,
            "spriteAudit",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            PreviewSummary: "Sprite audit preview ready.");
        var helper = new AgentSlotProfile(Guid.NewGuid(), "fox 1", 1);

        var enriched = await service.AppendIfAllowedAsync(request, result, helper, approvedForModelCall: true);

        Assert.False(fake.WasCalled);
        Assert.Equal(result.PreviewSummary, enriched.PreviewSummary);
    }

    private static TaskAdapterRequest BuildRequest(string toolFamily)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-model-summary-tests", Guid.NewGuid().ToString("N"));
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "review goose baby female blue sprites",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: toolFamily);
        var policy = new ToolPolicy(
            toolFamily + "-readonly",
            toolFamily,
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [root]);
        return new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: root,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-07T12:00:00Z"));
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
                "Check the flagged outlines.",
                DidCallProvider: request.ApprovedForModelCall,
                AuditLogPath: Path.Combine(request.ArtifactRoot, "model-call.json")));
        }
    }
}
