using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BuildProofExecutionAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_BlocksWhenNotExecuteMode()
    {
        var adapter = new BuildProofExecutionAdapter(commandRunner: new FakeCommandRunner());
        var request = BuildRequest(TaskAdapterRunMode.DryRunPreview, "run a build proof");

        var result = await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("Execute mode", result.BlockReason);
    }

    [Fact]
    public async Task ExecuteAsync_UsesFakeRunnerForDefaultBuildProof()
    {
        var adapter = new BuildProofExecutionAdapter(commandRunner: new FakeCommandRunner());
        var request = BuildRequest(TaskAdapterRunMode.Execute, "run a build proof");

        var result = await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("did not start a process", result.ResultSummary);
        Assert.Contains("dotnet-build-vnext", result.WrittenPaths?.FirstOrDefault(path => path.Contains("proof-execution-manifest", StringComparison.OrdinalIgnoreCase)) ?? "");
    }

    [Fact]
    public async Task ExecuteAsync_SelectsTestCommandFromTaskText()
    {
        var runner = new CapturingCommandRunner();
        var adapter = new BuildProofExecutionAdapter(commandRunner: runner);
        var request = BuildRequest(TaskAdapterRunMode.Execute, "run a build proof test");

        await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        Assert.Equal("dotnet-test-vnext-no-build", runner.LastCommandId);
    }

    private static TaskAdapterRequest BuildRequest(TaskAdapterRunMode runMode, string rawText)
    {
        var intent = new TaskIntent(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            rawText,
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.BuildProof,
            RequestedToolFamily: "buildProof",
            RiskLevel: ToolRiskLevel.Medium,
            NeedsApproval: true);
        var policy = new ToolPolicy(
            "build-proof-execution-approval",
            "buildProof",
            ToolAccessMode.Write,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);

        return new TaskAdapterRequest(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            intent,
            policy,
            runMode,
            Path.Combine(Path.GetTempPath(), "wevito-build-proof-adapter-tests", Guid.NewGuid().ToString("N")),
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
    }

    private sealed class CapturingCommandRunner : ICommandRunner
    {
        public string LastCommandId { get; private set; } = "";

        public Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
        {
            LastCommandId = request.CommandId;
            var started = request.RequestedAtUtc;
            return Task.FromResult(new ProofExecutionResult(
                request.TaskCardId,
                request.CommandId,
                ProofExecutionResultStatus.Succeeded,
                0,
                Path.Combine(request.ArtifactRoot, "stdout.txt"),
                Path.Combine(request.ArtifactRoot, "stderr.txt"),
                Path.Combine(request.ArtifactRoot, "merged.log"),
                Path.Combine(request.ArtifactRoot, "proof-execution-manifest.json"),
                MutationDetected: false,
                "captured",
                started,
                started.AddMilliseconds(1)));
        }
    }
}
