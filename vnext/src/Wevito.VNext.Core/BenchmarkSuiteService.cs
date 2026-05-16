using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Core;

public sealed class BenchmarkSuiteService
{
    public const string BenchmarkRunPacketKind = "benchmark_run";

    private readonly IReadOnlyList<IBenchmarkKind> _kinds;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly BenchmarkRegressionGate _regressionGate;

    public BenchmarkSuiteService(
        IReadOnlyList<IBenchmarkKind>? kinds = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        BenchmarkRegressionGate? regressionGate = null)
    {
        _kinds = kinds ?? CreateDefaultKinds();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _regressionGate = regressionGate ?? new BenchmarkRegressionGate();
    }

    public static IReadOnlyList<IBenchmarkKind> CreateDefaultKinds() =>
    [
        new ChatCorrectnessBenchmarkKind(),
        new ToolUseCorrectnessBenchmarkKind(),
        new RetrievalAccuracyBenchmarkKind(),
        new SafetyRegressionBenchmarkKind(),
        new PerfRegressionBenchmarkKind()
    ];

    public BenchmarkRunResult Run(BenchmarkSuiteVersion version, BenchmarkRunRequest request)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return Block(version, request, "kill_switch=true");
        }

        var approvedRoot = Path.GetFullPath(request.ApprovedCaseRoot);
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(approvedRoot);
        Directory.CreateDirectory(artifactRoot);
        var runFolder = Path.Combine(artifactRoot, $"{request.CreatedAtUtc:yyyyMMdd-HHmmss}-benchmark-run");
        Directory.CreateDirectory(runFolder);

        var enabled = request.EnabledAxes is { Count: > 0 }
            ? request.EnabledAxes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : null;
        var context = new BenchmarkContext(version, GraderTriad.Default, request.CreatedAtUtc);
        var results = new List<BenchmarkKindResult>();
        foreach (var kind in _kinds.Where(kind => enabled is null || enabled.Contains(kind.AxisName)))
        {
            var cases = LoadCases(approvedRoot, kind.AxisName);
            results.Add(kind.RunCases(cases, context));
        }

        var hasAnyCase = results.Any(result => result.CasesEvaluated > 0);
        var allGreen = hasAnyCase && results.All(result => result.Failed == 0);
        var regressionDecision = _regressionGate.Evaluate(results, request.Baseline);
        var status = !hasAnyCase
            ? "NoBaseline"
            : regressionDecision.Action is BenchmarkRegressionAction.Halt or BenchmarkRegressionAction.HaltStopCard
                ? "Regression"
                : allGreen ? "Completed" : "NeedsReview";
        var compositeScore = results.Count == 0 ? 0 : results.Average(result => result.Score);
        var result = new BenchmarkRunResult(
            Succeeded: allGreen && status == "Completed",
            status,
            version.VersionId,
            request.ModelVersion,
            request.CreatedAtUtc,
            runFolder,
            Path.Combine(runFolder, "result.json"),
            Path.Combine(runFolder, "summary.md"),
            compositeScore,
            results,
            regressionDecision,
            status == "NoBaseline"
                ? "No approved benchmark cases exist yet; benchmark run was recorded as no_baseline."
                : $"Benchmark run completed with composite score {compositeScore:0.###}.");

        File.WriteAllText(result.ResultPath, JsonSerializer.Serialize(result, JsonDefaults.Options));
        File.WriteAllText(result.SummaryPath, BuildSummary(result));
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            BenchmarkRunPacketKind,
            null,
            request.CreatedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            result.ArtifactFolder,
            $"Benchmark {version.VersionId} for {request.ModelVersion}: status={status}, score={compositeScore:0.###}.",
            status));
        if (regressionDecision.Action is BenchmarkRegressionAction.Halt or BenchmarkRegressionAction.HaltStopCard or BenchmarkRegressionAction.TaskCard)
        {
            _auditLedgerService?.Record(new EvidencePacket(
                Guid.NewGuid(),
                BenchmarkRegressionGate.RegressionDetectedPacketKind,
                null,
                request.CreatedAtUtc,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                result.ArtifactFolder,
                regressionDecision.Reason,
                regressionDecision.Action.ToString()));
        }

        return result;
    }

    public static IReadOnlyList<BenchmarkCase> LoadCases(string approvedRoot, string axisName)
    {
        if (!Directory.Exists(approvedRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(approvedRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => ReadCases(path, axisName))
            .ToList();
    }

    private BenchmarkRunResult Block(BenchmarkSuiteVersion version, BenchmarkRunRequest request, string reason)
    {
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);
        var folder = Path.Combine(artifactRoot, $"{request.CreatedAtUtc:yyyyMMdd-HHmmss}-benchmark-blocked");
        Directory.CreateDirectory(folder);
        var result = new BenchmarkRunResult(
            false,
            "Blocked",
            version.VersionId,
            request.ModelVersion,
            request.CreatedAtUtc,
            folder,
            Path.Combine(folder, "result.json"),
            Path.Combine(folder, "summary.md"),
            0,
            [],
            new BenchmarkRegressionDecision(BenchmarkRegressionAction.Halt, reason, [reason]),
            reason);
        File.WriteAllText(result.ResultPath, JsonSerializer.Serialize(result, JsonDefaults.Options));
        File.WriteAllText(result.SummaryPath, BuildSummary(result));
        return result;
    }

    private static IEnumerable<BenchmarkCase> ReadCases(string path, string axisName)
    {
        try
        {
            var text = File.ReadAllText(path);
            var cases = text.TrimStart().StartsWith('[')
                ? JsonSerializer.Deserialize<IReadOnlyList<BenchmarkCase>>(text, JsonDefaults.Options) ?? []
                : [JsonSerializer.Deserialize<BenchmarkCase>(text, JsonDefaults.Options)!];
            return cases.Where(testCase => testCase is not null &&
                                           testCase.Axis.Equals(axisName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return [];
        }
    }

    private static string BuildSummary(BenchmarkRunResult result)
    {
        var lines = new List<string>
        {
            "# Benchmark Run",
            "",
            $"- Benchmark version: {result.BenchmarkVersion}",
            $"- Model version: {result.ModelVersion}",
            $"- Status: {result.Status}",
            $"- Composite score: {result.CompositeScore:0.###}",
            $"- Regression action: {result.RegressionDecision.Action}",
            "",
            "## Axes"
        };
        if (result.KindResults.Count == 0)
        {
            lines.Add("- No benchmark axes ran.");
        }
        else
        {
            lines.AddRange(result.KindResults.Select(kind => $"- {kind.AxisName}: score {kind.Score:0.###}, cases {kind.CasesEvaluated}, failed {kind.Failed}"));
        }

        lines.Add("");
        lines.Add(result.Message);
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
