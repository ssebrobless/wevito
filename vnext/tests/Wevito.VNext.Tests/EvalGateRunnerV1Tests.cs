using System.Reflection;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class EvalGateRunnerV1Tests
{
    [Fact]
    public void Execute_FlagAbsent_ReturnsNotApplicableWithoutRunner()
    {
        var runner = new RecordingCommandRunner();

        var result = new EvalGateRunner().Execute(
            Request(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)),
            runner,
            ValidHashInputs());

        Assert.All(result.Results.Values, value => AssertNotApplicable(value, "eval_gate_runner_v1_enabled=false"));
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public void Execute_FlagFalse_ReturnsNotApplicableWithoutRunner()
    {
        var runner = new RecordingCommandRunner();

        var result = new EvalGateRunner().Execute(Request(new Dictionary<string, string>
        {
            [EvalGateRunner.EnabledSetting] = bool.FalseString
        }), runner, ValidHashInputs());

        Assert.All(result.Results.Values, value => AssertNotApplicable(value, "eval_gate_runner_v1_enabled=false"));
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public void Execute_KillSwitchActive_ReturnsNotApplicableWithoutRunner()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var runner = new RecordingCommandRunner();

        var result = new EvalGateRunner(killSwitchService: killSwitch).Execute(Request(EnabledSettings()), runner, ValidHashInputs());

        Assert.All(result.Results.Values, value => AssertNotApplicable(value, "kill_switch=true"));
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public void Execute_EnabledAndCommandsSucceed_RunsThreeV1Gates()
    {
        var runner = new RecordingCommandRunner();

        var result = new EvalGateRunner().Execute(Request(EnabledSettings()), runner, ValidHashInputs());

        Assert.IsType<EvalGateResult.Passed>(result.Results[EvalGateManifest.Build]);
        Assert.IsType<EvalGateResult.Passed>(result.Results[EvalGateManifest.UnitTests]);
        Assert.IsType<EvalGateResult.Passed>(result.Results[EvalGateManifest.ScopeHash]);
        AssertOtherGatesNotApplicable(result);
        Assert.Equal(["eval-gate-build", "eval-gate-unit-tests"], runner.Invocations.Select(invocation => invocation.CommandId).ToArray());
    }

    [Fact]
    public void Execute_BuildFails_StillRunsUnitTestsAndReportsFailure()
    {
        var runner = new RecordingCommandRunner(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["eval-gate-build"] = 2
        });

        var result = new EvalGateRunner().Execute(Request(EnabledSettings()), runner, ValidHashInputs());

        var failed = Assert.IsType<EvalGateResult.Failed>(result.Results[EvalGateManifest.Build]);
        Assert.Contains("eval-gate-build stderr", failed.Reason);
        Assert.IsType<EvalGateResult.Passed>(result.Results[EvalGateManifest.UnitTests]);
        Assert.Equal(["eval-gate-build", "eval-gate-unit-tests"], runner.Invocations.Select(invocation => invocation.CommandId).ToArray());
    }

    [Fact]
    public void Execute_UnitTestsFail_ReportFailure()
    {
        var runner = new RecordingCommandRunner(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["eval-gate-unit-tests"] = 1
        });

        var result = new EvalGateRunner().Execute(Request(EnabledSettings()), runner, ValidHashInputs());

        Assert.IsType<EvalGateResult.Passed>(result.Results[EvalGateManifest.Build]);
        var failed = Assert.IsType<EvalGateResult.Failed>(result.Results[EvalGateManifest.UnitTests]);
        Assert.Contains("eval-gate-unit-tests stderr", failed.Reason);
    }

    [Fact]
    public void Execute_EmptyPacketKinds_FailsScopeHash()
    {
        var result = new EvalGateRunner().Execute(Request(EnabledSettings()), new RecordingCommandRunner(), ValidHashInputs() with
        {
            PacketKindsTouched = []
        });

        var failed = Assert.IsType<EvalGateResult.Failed>(result.Results[EvalGateManifest.ScopeHash]);
        Assert.Equal("scope_hash_inputs_invalid", failed.Reason);
    }

    [Fact]
    public void Constructor_DoesNotTakeHeldOutOrInDistributionStores()
    {
        var forbidden = typeof(EvalGateRunner)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
            .Where(type => type == typeof(IHeldOutEvalStore) ||
                           type == typeof(HeldOutEvalStore) ||
                           type == typeof(IInDistributionEvalStore) ||
                           type == typeof(InDistributionEvalStore))
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Source_DoesNotCallProcessStartDirectly()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Eval", "EvalGateRunner.cs"));

        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
    }

    private static EvalGateExecutionRequest Request(IReadOnlyDictionary<string, string> settings)
    {
        return new EvalGateExecutionRequest(
            "sprite-repair-batch-proposal",
            Guid.NewGuid().ToString("N"),
            FindRepositoryRoot(),
            settings);
    }

    private static IReadOnlyDictionary<string, string> EnabledSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EvalGateRunner.EnabledSetting] = bool.TrueString
        };
    }

    private static ScopeHashInputs ValidHashInputs()
    {
        return new ScopeHashInputs(
            "sprite-repair-batch-proposal",
            "operation-001",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "1",
            [SelfImprovementPacketKinds.ProposalDrafted]);
    }

    private static void AssertOtherGatesNotApplicable(EvalGateExecutionResult result)
    {
        foreach (var gate in EvalGateManifest.Default().Gates.Except([EvalGateManifest.Build, EvalGateManifest.UnitTests, EvalGateManifest.ScopeHash]))
        {
            AssertNotApplicable(result.Results[gate], "not_wired_in_v1");
        }
    }

    private static void AssertNotApplicable(EvalGateResult result, string reason)
    {
        var notApplicable = Assert.IsType<EvalGateResult.NotApplicable>(result);
        Assert.Equal(reason, notApplicable.Reason);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed class RecordingCommandRunner : ICommandRunner
    {
        private readonly IReadOnlyDictionary<string, int> _exitCodes;

        public RecordingCommandRunner(IReadOnlyDictionary<string, int>? exitCodes = null)
        {
            _exitCodes = exitCodes ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ProofExecutionRequest> Invocations { get; } = [];

        public Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
        {
            Invocations.Add(request);
            var exitCode = _exitCodes.TryGetValue(request.CommandId, out var configured) ? configured : 0;
            Directory.CreateDirectory(request.ArtifactRoot);
            var stdout = Path.Combine(request.ArtifactRoot, "stdout.txt");
            var stderr = Path.Combine(request.ArtifactRoot, "stderr.txt");
            var merged = Path.Combine(request.ArtifactRoot, "merged.log");
            var manifest = Path.Combine(request.ArtifactRoot, "proof-execution-manifest.json");
            File.WriteAllText(stdout, $"{request.CommandId} stdout");
            File.WriteAllText(stderr, exitCode == 0 ? "" : $"{request.CommandId} stderr");
            File.WriteAllText(merged, "");
            File.WriteAllText(manifest, "{}");
            var started = request.RequestedAtUtc;

            return Task.FromResult(new ProofExecutionResult(
                request.TaskCardId,
                request.CommandId,
                exitCode == 0 ? ProofExecutionResultStatus.Succeeded : ProofExecutionResultStatus.Failed,
                exitCode,
                stdout,
                stderr,
                merged,
                manifest,
                MutationDetected: false,
                $"{request.CommandId} summary",
                started,
                started.AddMilliseconds(1)));
        }
    }
}
