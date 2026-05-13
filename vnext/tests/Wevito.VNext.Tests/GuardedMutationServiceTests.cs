using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class GuardedMutationServiceTests
{
    [Fact]
    public void DryRun_NeverWritesOutsideBackupFolderOrMutatesTarget()
    {
        var root = CreateTempRoot();
        var target = WriteTarget(root, "before");
        var service = new GuardedMutationService(policyService: Policy(root));

        var result = service.DryRun(Plan(root, target, "after"), ApprovedCard(), ActiveStatus());

        Assert.True(result.Succeeded, result.Message);
        Assert.False(result.DidMutate);
        Assert.Equal("before", File.ReadAllText(target));
        Assert.True(File.Exists(result.ManifestPath));
        Assert.False(Directory.Exists(Path.Combine(root, "vnext", "artifacts", "mutation-backups")));
    }

    [Fact]
    public void Apply_RefusesScopeExpandOutsideApprovedRoots()
    {
        var root = CreateTempRoot();
        var outside = Path.Combine(root, "outside", "escape.txt");
        var service = new GuardedMutationService(policyService: Policy(root));

        var result = service.Apply(Plan(root, outside, "after"), ApprovedCard(), ActiveStatus());

        Assert.False(result.Succeeded);
        Assert.Contains("outside approved", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_PostProofFailureRollsBackByteExact()
    {
        var root = CreateTempRoot();
        var target = WriteTarget(root, "before");
        var beforeHash = GuardedMutationService.ComputeSha256(target);
        var service = new GuardedMutationService(policyService: Policy(root), postProofRunner: _ => false);

        var result = service.Apply(Plan(root, target, "after"), ApprovedCard(), ActiveStatus());

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.RolledBack);
        Assert.Equal("before", File.ReadAllText(target));
        Assert.Equal(beforeHash, GuardedMutationService.ComputeSha256(target));
    }

    [Fact]
    public void Apply_KillSwitchBlocks()
    {
        var root = CreateTempRoot();
        var target = WriteTarget(root, "before");
        var service = new GuardedMutationService(
            policyService: Policy(root),
            killSwitchService: new KillSwitchService(() => new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" }));

        var result = service.Apply(Plan(root, target, "after"), ApprovedCard(), ActiveStatus());

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.Message);
    }

    [Fact]
    public void Apply_RequiresApprovedCardAndActiveRuntime()
    {
        var root = CreateTempRoot();
        var target = WriteTarget(root, "before");
        var service = new GuardedMutationService(policyService: Policy(root));

        var draft = service.Apply(Plan(root, target, "after"), ApprovedCard() with { Status = TaskCardStatus.Draft }, ActiveStatus());
        var quiet = service.Apply(Plan(root, target, "after"), ApprovedCard(), ActiveStatus() with { Mode = RuntimeSupervisorMode.Quiet });

        Assert.False(draft.Succeeded);
        Assert.False(quiet.Succeeded);
    }

    private static GuardedMutationPlan Plan(string root, string target, string content)
    {
        return new GuardedMutationPlan(
            "1",
            Guid.Parse("74000000-0000-0000-0000-000000000001"),
            Guid.Parse("74000000-0000-0000-0000-000000000002"),
            "synthetic-text",
            root,
            [Path.Combine(root, "vnext", "content")],
            [new GuardedMutationEdit(target, content)],
            [],
            DryRunOnly: false,
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static TaskCard ApprovedCard()
    {
        var intent = new TaskIntent(
            Guid.Parse("74000000-0000-0000-0000-000000000003"),
            "apply guarded mutation",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.PlanCodePatch,
            RequestedToolFamily: "guardedMutation");
        return new TaskCard(
            Guid.Parse("74000000-0000-0000-0000-000000000002"),
            intent,
            TaskCardStatus.Approved,
            ToolFamily: "guardedMutation");
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "Active", "");
    }

    private static UnifiedPolicyService Policy(string root)
    {
        return new UnifiedPolicyService(new LocalToolAccessPolicy(root, [Path.Combine(root, "vnext", "content")]));
    }

    private static string WriteTarget(string root, string text)
    {
        var target = Path.Combine(root, "vnext", "content", "synthetic.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, text);
        return target;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-guarded-mutation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "content"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "artifacts"));
        return root;
    }
}
