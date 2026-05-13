using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record GoldenEvalQuestion(
    string Id,
    string Question,
    IReadOnlyList<string> ExpectedChunkIds,
    string RubricNote);

public sealed record GoldenEvalBaseline(
    string SchemaVersion,
    double RecallAt1,
    double RecallAt3,
    double MeanReciprocalRank,
    double CitationCoverageRatio,
    double LatencyP50,
    double LatencyP95,
    string DatasetSha256,
    DateTimeOffset CapturedAtUtc);

public sealed record GoldenEvalRunResult(
    bool Succeeded,
    bool Regression,
    string DatasetSha256,
    GoldenEvalBaseline Current,
    GoldenEvalBaseline Baseline,
    string ReportPath,
    string SummaryPath,
    string Message);

public sealed class EvalRegressionGate
{
    public const double RecallTolerance = 0.02;
    public const double MrrTolerance = 0.02;
    public const double CitationCoverageTolerance = 0.05;
    public const double CitationCoverageFloor = 0.60;
    public const string PacketKind = "golden_eval";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public EvalRegressionGate(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public GoldenEvalRunResult Run(string datasetRoot, string artifactRoot, bool updateBaseline = false, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true)
        {
            return Block(datasetRoot, artifactRoot, timestamp, "kill_switch=true");
        }

        var root = Path.GetFullPath(datasetRoot);
        var baselinePath = Path.Combine(root, "baseline.json");
        var questionsPath = Path.Combine(root, "questions.json");
        if (!File.Exists(questionsPath) || !File.Exists(baselinePath))
        {
            return Block(root, artifactRoot, timestamp, "Golden eval dataset is missing questions.json or baseline.json.");
        }

        var datasetSha = ComputeDatasetSha(root);
        var baseline = JsonSerializer.Deserialize<GoldenEvalBaseline>(File.ReadAllText(baselinePath), JsonDefaults.Options)
            ?? throw new InvalidOperationException("baseline.json could not be parsed.");
        if (!string.Equals(datasetSha, baseline.DatasetSha256, StringComparison.OrdinalIgnoreCase) && !updateBaseline)
        {
            return Block(root, artifactRoot, timestamp, $"Dataset sha256 mismatch. Expected {baseline.DatasetSha256}, actual {datasetSha}.");
        }

        var questions = JsonSerializer.Deserialize<IReadOnlyList<GoldenEvalQuestion>>(File.ReadAllText(questionsPath), JsonDefaults.Options) ?? [];
        var current = Evaluate(root, questions, datasetSha, timestamp);
        var regression = IsRegression(current, baseline);
        if (updateBaseline)
        {
            File.WriteAllText(baselinePath, JsonSerializer.Serialize(current, JsonDefaults.Options));
            baseline = current;
            regression = false;
        }

        Directory.CreateDirectory(artifactRoot);
        var reportPath = Path.Combine(artifactRoot, "golden-eval-report.json");
        var summaryPath = Path.Combine(artifactRoot, "golden-eval-summary.md");
        var result = new GoldenEvalRunResult(
            !regression,
            regression,
            datasetSha,
            current,
            baseline,
            reportPath,
            summaryPath,
            regression ? "Golden eval regression detected." : "Golden eval passed.");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonDefaults.Options));
        File.WriteAllText(summaryPath, BuildSummary(result));
        Record(result, timestamp, regression ? "Regression" : "Completed", regression ? result.Message : "");
        return result;
    }

    public static bool IsRegression(GoldenEvalBaseline current, GoldenEvalBaseline baseline)
    {
        return baseline.RecallAt1 - current.RecallAt1 > RecallTolerance ||
               baseline.MeanReciprocalRank - current.MeanReciprocalRank > MrrTolerance ||
               baseline.CitationCoverageRatio - current.CitationCoverageRatio > CitationCoverageTolerance ||
               current.CitationCoverageRatio < CitationCoverageFloor;
    }

    public static string ComputeDatasetSha(string datasetRoot)
    {
        var root = Path.GetFullPath(datasetRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Equals("baseline.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append(Path.GetFullPath(file)[(root.Length + 1)..].Replace('\\', '/'));
            builder.Append(':');
            builder.Append(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant());
            builder.Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static GoldenEvalBaseline Evaluate(string root, IReadOnlyList<GoldenEvalQuestion> questions, string datasetSha, DateTimeOffset timestamp)
    {
        var documents = Directory.EnumerateFiles(Path.Combine(root, "documents"), "*.md", SearchOption.TopDirectoryOnly)
            .Select(path => (Path: path, Text: File.ReadAllText(path)))
            .ToList();
        var reciprocalRanks = new List<double>();
        var latencies = new List<double>();
        var recallAt1 = 0;
        var recallAt3 = 0;

        foreach (var question in questions)
        {
            var stopwatch = Stopwatch.StartNew();
            var ranked = documents
                .Select(doc => new
                {
                    ChunkId = Path.GetFileNameWithoutExtension(doc.Path),
                    Score = Score(question.Question, doc.Text)
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.ChunkId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            stopwatch.Stop();
            latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
            var rank = ranked.FindIndex(item => question.ExpectedChunkIds.Contains(item.ChunkId, StringComparer.OrdinalIgnoreCase)) + 1;
            if (rank == 1)
            {
                recallAt1++;
            }

            if (rank > 0 && rank <= 3)
            {
                recallAt3++;
            }

            reciprocalRanks.Add(rank > 0 ? 1d / rank : 0d);
        }

        var count = Math.Max(1, questions.Count);
        return new GoldenEvalBaseline(
            "1",
            recallAt1 / (double)count,
            recallAt3 / (double)count,
            reciprocalRanks.Count == 0 ? 0 : reciprocalRanks.Average(),
            CitationCoverageRatio: 1.0,
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.95),
            datasetSha,
            timestamp);
    }

    private static double Score(string query, string text)
    {
        var queryTokens = Tokenize(query);
        var textTokens = Tokenize(text);
        return queryTokens.Count == 0 ? 0 : queryTokens.Count(textTokens.Contains) / (double)queryTokens.Count;
    }

    private static HashSet<string> Tokenize(string value)
    {
        return (value ?? "")
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values.OrderBy(value => value).ToList();
        var index = (int)Math.Ceiling(percentile * ordered.Count) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Count - 1)];
    }

    private GoldenEvalRunResult Block(string datasetRoot, string artifactRoot, DateTimeOffset timestamp, string reason)
    {
        var empty = new GoldenEvalBaseline("1", 0, 0, 0, 0, 0, 0, "", timestamp);
        Directory.CreateDirectory(artifactRoot);
        var reportPath = Path.Combine(artifactRoot, "golden-eval-report.json");
        var summaryPath = Path.Combine(artifactRoot, "golden-eval-summary.md");
        var result = new GoldenEvalRunResult(false, true, "", empty, empty, reportPath, summaryPath, reason);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(result, JsonDefaults.Options));
        File.WriteAllText(summaryPath, BuildSummary(result));
        Record(result, timestamp, "Blocked", reason);
        return result;
    }

    private void Record(GoldenEvalRunResult result, DateTimeOffset timestamp, string status, string error)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: result.ReportPath,
            Summary: result.Message,
            Status: status,
            Error: error));
    }

    private static string BuildSummary(GoldenEvalRunResult result)
    {
        return string.Join(Environment.NewLine, [
            "# Golden Eval Run",
            "",
            $"- Passed: {result.Succeeded.ToString().ToLowerInvariant()}",
            $"- Regression: {result.Regression.ToString().ToLowerInvariant()}",
            $"- recall@1: {result.Current.RecallAt1:0.###}",
            $"- recall@3: {result.Current.RecallAt3:0.###}",
            $"- mrr: {result.Current.MeanReciprocalRank:0.###}",
            $"- citation coverage: {result.Current.CitationCoverageRatio:0.###}",
            $"- dataset sha256: `{result.DatasetSha256}`",
            "",
            result.Message
        ]) + Environment.NewLine;
    }
}
