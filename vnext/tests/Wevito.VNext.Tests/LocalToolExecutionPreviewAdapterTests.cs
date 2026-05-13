using System.Security.Cryptography;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalToolExecutionPreviewAdapterTests
{
    [Fact]
    public void BuildPreview_DefaultBlocked()
    {
        var root = CreateTempRoot();
        var tools = Path.Combine(root, "tools");
        Directory.CreateDirectory(tools);
        var script = Path.Combine(tools, "safe.ps1");
        File.WriteAllText(script, "Write-Output 'dry run only'");
        var adapter = new LocalToolExecutionPreviewAdapter(
            new UnifiedPolicyService(new LocalToolAccessPolicy(root)),
            () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var result = adapter.BuildPreview(BuildRequest(root, script));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("local_tool_exec_enabled=false", result.BlockReason);
        Assert.False(result.DidMutate);
    }

    [Fact]
    public void BuildPreview_WhenEnabledWritesDryRunPacketWithoutExecuting()
    {
        var root = CreateTempRoot();
        var tools = Path.Combine(root, "tools");
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "20260512-120000-local-tool-exec");
        Directory.CreateDirectory(tools);
        var script = Path.Combine(tools, "safe.ps1");
        File.WriteAllText(script, "New-Item should-not-exist.txt");
        var hash = ComputeSha256(script);
        var adapter = new LocalToolExecutionPreviewAdapter(
            new UnifiedPolicyService(new LocalToolAccessPolicy(root)),
            () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["local_tool_exec_enabled"] = bool.TrueString,
                ["local_tool_exec_sha256:safe.ps1"] = hash
            });

        var result = adapter.BuildPreview(BuildRequest(root, script, artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.False(File.Exists(Path.Combine(root, "should-not-exist.txt")));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("local-tool-exec-preview-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dispatcher_RoutesLocalToolExecToDryRunAdapter()
    {
        var root = CreateTempRoot();
        var tools = Path.Combine(root, "tools");
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "20260512-120000-local-tool-exec");
        Directory.CreateDirectory(tools);
        var script = Path.Combine(tools, "safe.ps1");
        File.WriteAllText(script, "Write-Output 'dry run only'");
        var hash = ComputeSha256(script);
        var adapter = new LocalToolExecutionPreviewAdapter(
            new UnifiedPolicyService(new LocalToolAccessPolicy(root)),
            () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["local_tool_exec_enabled"] = bool.TrueString,
                ["local_tool_exec_sha256:safe.ps1"] = hash
            });
        var dispatcher = new PetTaskAdapterPreviewDispatcher(localToolExecutionPreviewAdapter: adapter);

        var result = dispatcher.BuildPreview(BuildRequest(root, script, artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("localToolExec", result.ToolFamily);
    }

    private static TaskAdapterRequest BuildRequest(string root, string script, string? artifactRoot = null)
    {
        var intent = new TaskIntent(
            Guid.Parse("c1000000-0000-0000-0000-000000000001"),
            "preview local tool script",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ExternalAction,
            RequestedToolFamily: "localToolExec",
            TargetPathsOrAssets: [script]);
        var policy = new ToolPolicy(
            "local-tool-exec-preview",
            "localToolExec",
            ToolAccessMode.Write,
            ToolRiskLevel.High,
            ApprovalRequirement.BeforeExecution,
            ApprovedRootPaths: [Path.Combine(root, "tools")]);
        return new TaskAdapterRequest(
            Guid.Parse("c2000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: artifactRoot ?? Path.Combine(root, "vnext", "artifacts", "pet-tasks", "local-tool-exec-test"));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-tool-exec-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
