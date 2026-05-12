using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetMemoryWriteGateTests
{
    [Fact]
    public void Evaluate_RequiresApprovalBeforeMemoryWrite()
    {
        var gate = new PetMemoryWriteGate();

        var decision = gate.Evaluate(new PetMemoryWriteRequest(
            Guid.NewGuid(),
            "Scout",
            "localDocs",
            "summarize local docs",
            "docs preference"));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ApprovalRequirement.BeforeExecution, decision.ApprovalRequirement);
    }

    [Fact]
    public void Evaluate_AllowsApprovedMemoryWrite()
    {
        var gate = new PetMemoryWriteGate();

        var decision = gate.Evaluate(new PetMemoryWriteRequest(
            Guid.NewGuid(),
            "Inspector",
            "spriteAudit",
            "review sprite frames",
            "sprite preference",
            Approved: true));

        Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
    }

    [Fact]
    public void Evaluate_BlocksMissingConcretePet()
    {
        var gate = new PetMemoryWriteGate();

        var decision = gate.Evaluate(new PetMemoryWriteRequest(
            Guid.Empty,
            "",
            "spriteAudit",
            "review sprite frames",
            "sprite preference",
            Approved: true));

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
    }

    [Fact]
    public void CommandParser_ClassifiesRememberAsPetMemoryApprovalTask()
    {
        var helper = new PetHelperProfile(Guid.NewGuid(), "Scout", PetHelperRole.ResearchHelper);
        var intent = new PetCommandParser().Parse("Scout, remember that sprite reviews should check silhouettes", [helper]);

        Assert.Equal(TaskKind.UpdatePetMemory, intent.TaskKind);
        Assert.Equal("petMemory", intent.RequestedToolFamily);
        Assert.True(intent.NeedsApproval);
    }

    [Fact]
    public void PreviewAdapter_WritesGatedMemoryPreviewWithoutMutatingMemory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "wevito-pet-memory-preview-tests", Guid.NewGuid().ToString("N"));
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260507-pet-memory");
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "Scout, remember that sprite reviews should check silhouettes",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TargetPetNameSnapshot: "Scout",
            TaskKind: TaskKind.UpdatePetMemory,
            RequestedToolFamily: "petMemory",
            NeedsApproval: true);
        var policy = new ToolPolicy(
            "pet-memory-write",
            "petMemory",
            ToolAccessMode.Write,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution,
            ApprovedRootPaths: [tempRoot]);
        var request = new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: artifactRoot);

        var result = new PetMemoryPreviewAdapter().BuildPreview(request);

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("gated", result.PreviewSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("pet-memory-preview-report.json", StringComparison.OrdinalIgnoreCase));
    }
}
