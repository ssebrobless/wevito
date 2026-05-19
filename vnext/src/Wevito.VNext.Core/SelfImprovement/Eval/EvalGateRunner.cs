using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class EvalGateRunner
{
    public const string EnabledSetting = "eval_gate_runner_v1_enabled";
    public const string BuildCommand = "dotnet build .\\vnext\\Wevito.VNext.sln --nologo";
    public const string TestCommand = "dotnet test .\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj --no-build";

    private readonly EvalGateManifest _manifest;
    private readonly KillSwitchService? _killSwitchService;

    public EvalGateRunner(
        EvalGateManifest? manifest = null,
        KillSwitchService? killSwitchService = null)
    {
        _manifest = manifest ?? EvalGateManifest.Default();
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyDictionary<string, EvalGateResult> Preview()
    {
        var reason = _killSwitchService?.IsActive() == true
            ? "kill_switch=true"
            : "no_eval_run_wired_v0";

        return _manifest.Gates.ToDictionary(
            gate => gate,
            _ => (EvalGateResult)new EvalGateResult.NotApplicable(reason),
            StringComparer.OrdinalIgnoreCase);
    }

    public EvalGateExecutionResult Execute(
        EvalGateExecutionRequest request,
        ICommandRunner runner,
        ScopeHashInputs hashInputs)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(hashInputs);

        var ranAt = DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true)
        {
            return BuildNotApplicableResult("kill_switch=true", ranAt);
        }

        if (!request.Settings.TryGetValue(EnabledSetting, out var enabled) ||
            !string.Equals(enabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return BuildNotApplicableResult("eval_gate_runner_v1_enabled=false", ranAt);
        }

        var results = _manifest.Gates.ToDictionary(
            gate => gate,
            _ => (EvalGateResult)new EvalGateResult.NotApplicable("not_wired_in_v1"),
            StringComparer.OrdinalIgnoreCase);

        results[EvalGateManifest.Build] = RunCommandGate(
            runner,
            request,
            "eval-gate-build",
            BuildCommand,
            ["build", ".\\vnext\\Wevito.VNext.sln", "--nologo"]);

        results[EvalGateManifest.UnitTests] = RunCommandGate(
            runner,
            request,
            "eval-gate-unit-tests",
            TestCommand,
            ["test", ".\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj", "--no-build"]);

        results[EvalGateManifest.ScopeHash] = BuildScopeHashResult(hashInputs);

        return new EvalGateExecutionResult(results, ranAt, BuildCommand, TestCommand);
    }

    private EvalGateExecutionResult BuildNotApplicableResult(string reason, DateTimeOffset ranAt)
    {
        return new EvalGateExecutionResult(
            _manifest.Gates.ToDictionary(
                gate => gate,
                _ => (EvalGateResult)new EvalGateResult.NotApplicable(reason),
                StringComparer.OrdinalIgnoreCase),
            ranAt,
            BuildCommand,
            TestCommand);
    }

    private static EvalGateResult RunCommandGate(
        ICommandRunner runner,
        EvalGateExecutionRequest request,
        string commandId,
        string commandText,
        IReadOnlyList<string> arguments)
    {
        var artifactRoot = Path.Combine(
            Path.GetFullPath(request.RepoRoot),
            "vnext",
            "artifacts",
            "eval-gates",
            SafePathSegment(request.OperationId),
            commandId);
        var proofRequest = new ProofExecutionRequest(
            Guid.NewGuid(),
            commandId,
            new ProofExecutionCommand(commandId, "dotnet", arguments, request.RepoRoot, TimeSpan.FromMinutes(10), MustSkipAssetPrep: true),
            artifactRoot,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            DateTimeOffset.UtcNow);

        var result = runner.RunAsync(proofRequest).GetAwaiter().GetResult();
        if (result.ExitCode == 0 && result.Status == ProofExecutionResultStatus.Succeeded)
        {
            return new EvalGateResult.Passed();
        }

        return new EvalGateResult.Failed(ClipStderr(result.StderrPath, result.Summary, commandText));
    }

    private static EvalGateResult BuildScopeHashResult(ScopeHashInputs hashInputs)
    {
        var hash = ScopeHash.Compute(hashInputs);
        return hashInputs.PacketKindsTouched.Count > 0 && Regex.IsMatch(hash, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
            ? new EvalGateResult.Passed()
            : new EvalGateResult.Failed("scope_hash_inputs_invalid");
    }

    private static string ClipStderr(string stderrPath, string fallback, string commandText)
    {
        string text;
        try
        {
            text = File.Exists(stderrPath) ? File.ReadAllText(stderrPath) : fallback;
        }
        catch (IOException)
        {
            text = fallback;
        }
        catch (UnauthorizedAccessException)
        {
            text = fallback;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = $"{commandText} failed.";
        }

        return text.Length <= 512 ? text : text[^512..];
    }

    private static string SafePathSegment(string value)
    {
        var safe = new string((value ?? "").Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "operation" : safe;
    }
}
