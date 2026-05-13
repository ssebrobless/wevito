using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class GuardedMutationPilotTests
{
    [Fact]
    public void PilotProposal_DryRunAndApplyRoundTripSyntheticFixture()
    {
        var root = CreateTempRoot();
        var target = WritePilotTarget(root, "before");
        var adapter = new GuardedMutationPreviewAdapter();
        var proposal = adapter.Propose(
            root,
            Path.Combine("vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId, "example.txt"),
            "after",
            Path.Combine(root, "vnext", "artifacts", "pet-tasks", "pilot-preview"),
            DateTimeOffset.Parse("2026-05-13T13:00:00Z"));
        Assert.True(proposal.Succeeded, proposal.BlockReason);
        Assert.NotNull(proposal.Plan);
        Assert.NotNull(proposal.TaskCard);
        Assert.Equal(TaskCardStatus.Draft, proposal.TaskCard.Status);
        var beforeHash = GuardedMutationService.ComputeSha256(target);
        var service = new GuardedMutationService(policyService: Policy(root));

        var dryRun = service.DryRun(proposal.Plan, proposal.TaskCard, ActiveStatus());
        var diff = File.ReadAllText(Path.Combine(dryRun.ArtifactFolder, "mutation-diff.md"));
        var apply = service.Apply(proposal.Plan, proposal.TaskCard with { Status = TaskCardStatus.Approved }, ActiveStatus());

        Assert.True(dryRun.Succeeded, dryRun.Message);
        Assert.False(dryRun.DidMutate);
        Assert.Contains("-before", diff);
        Assert.Contains("+after", diff);
        Assert.True(apply.Succeeded, apply.Message);
        Assert.False(apply.RolledBack);
        Assert.Equal("after", File.ReadAllText(target));
        Assert.NotEqual(beforeHash, GuardedMutationService.ComputeSha256(target));
        Assert.True(File.Exists(apply.ManifestPath));
    }

    [Fact]
    public void PilotProposal_PostProofFailureRollsBackByteExact()
    {
        var root = CreateTempRoot();
        var target = WritePilotTarget(root, "before");
        var beforeHash = GuardedMutationService.ComputeSha256(target);
        var proposal = new GuardedMutationPreviewAdapter().Propose(
            root,
            Path.Combine("vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId, "example.txt"),
            "after",
            Path.Combine(root, "vnext", "artifacts", "pet-tasks", "pilot-preview"),
            DateTimeOffset.Parse("2026-05-13T13:00:00Z"));
        Assert.True(proposal.Succeeded, proposal.BlockReason);
        var service = new GuardedMutationService(policyService: Policy(root), postProofRunner: _ => false);

        var apply = service.Apply(proposal.Plan!, proposal.TaskCard! with { Status = TaskCardStatus.Approved }, ActiveStatus());

        Assert.True(apply.Succeeded, apply.Message);
        Assert.True(apply.RolledBack);
        Assert.Equal("before", File.ReadAllText(target));
        Assert.Equal(beforeHash, GuardedMutationService.ComputeSha256(target));
    }

    [Fact]
    public void PilotProposal_RefusesScopeExpansion()
    {
        var root = CreateTempRoot();
        var proposal = new GuardedMutationPreviewAdapter().Propose(
            root,
            Path.Combine("vnext", "content", "outside-pilot.txt"),
            "after",
            Path.Combine(root, "vnext", "artifacts", "pet-tasks", "pilot-preview"),
            DateTimeOffset.Parse("2026-05-13T13:00:00Z"));

        Assert.False(proposal.Succeeded);
        Assert.Contains("guarded-mutation-pilot", proposal.BlockReason);
    }

    [Fact]
    public void PilotApply_KillSwitchBlocksMidRun()
    {
        var root = CreateTempRoot();
        WritePilotTarget(root, "before");
        var proposal = new GuardedMutationPreviewAdapter().Propose(
            root,
            Path.Combine("vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId, "example.txt"),
            "after",
            Path.Combine(root, "vnext", "artifacts", "pet-tasks", "pilot-preview"),
            DateTimeOffset.Parse("2026-05-13T13:00:00Z"));
        Assert.True(proposal.Succeeded, proposal.BlockReason);
        var service = new GuardedMutationService(
            policyService: Policy(root),
            killSwitchService: new KillSwitchService(() => new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" }));

        var apply = service.Apply(proposal.Plan!, proposal.TaskCard! with { Status = TaskCardStatus.Approved }, ActiveStatus());

        Assert.False(apply.Succeeded);
        Assert.Equal("kill_switch=true", apply.Message);
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "Active", "");
    }

    private static UnifiedPolicyService Policy(string root)
    {
        return new UnifiedPolicyService(new LocalToolAccessPolicy(root, [Path.Combine(root, "vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId)]));
    }

    private static string WritePilotTarget(string root, string text)
    {
        var target = Path.Combine(root, "vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId, "example.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, text);
        return target;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-guarded-mutation-pilot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "content", GuardedMutationPreviewAdapter.PilotScopeId));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "artifacts"));
        return root;
    }
}
