using System.Diagnostics;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LearningEvalService
{
    public const string PacketKind = "eval_run";

    private readonly ITextEmbeddingService _embeddingService;

    public LearningEvalService(ITextEmbeddingService? embeddingService = null)
    {
        _embeddingService = embeddingService ?? new CachingTextEmbeddingService();
    }

    public LearningEvalResult Evaluate(LearningEvalRequest request)
    {
        var datasetFolder = ResolveDatasetFolder(request.DatasetRoot, request.DatasetVersion);
        if (string.IsNullOrWhiteSpace(datasetFolder))
        {
            return Block("No promoted learning dataset was found.");
        }

        var examplesPath = Path.Combine(datasetFolder, "examples.jsonl");
        if (!File.Exists(examplesPath))
        {
            return Block("Promoted dataset is missing examples.jsonl.");
        }

        var examples = ReadExamples(examplesPath);
        if (examples.Count == 0)
        {
            return Block("Promoted dataset has no eval examples.");
        }

        var metrics = ComputeMetrics(examples);
        var prior = ReadBaseline(request.BaselinePath);
        var regression = prior is not null && IsRegression(metrics, prior.Metrics, request.RegressionTolerance);
        var baselinePromoted = !regression;

        Directory.CreateDirectory(request.ArtifactRoot);
        var runFolder = Path.Combine(request.ArtifactRoot, $"{request.CreatedAtUtc:yyyyMMdd-HHmmss}-eval-run");
        Directory.CreateDirectory(runFolder);
        var datasetVersion = Path.GetFileName(datasetFolder);
        var record = new LearningEvalRunRecord(
            "1",
            datasetVersion,
            prior?.DatasetVersion ?? "",
            metrics,
            prior?.Metrics,
            regression,
            baselinePromoted,
            examples.Count,
            request.CreatedAtUtc);
        var reportPath = Path.Combine(runFolder, "eval-report.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(record, JsonDefaults.Options));
        var summaryPath = Path.Combine(runFolder, "run-summary.md");
        File.WriteAllText(summaryPath, BuildSummary(record));
        if (baselinePromoted)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(request.BaselinePath) ?? request.ArtifactRoot);
            File.WriteAllText(request.BaselinePath, JsonSerializer.Serialize(record, JsonDefaults.Options));
        }

        return new LearningEvalResult(
            true,
            datasetVersion,
            reportPath,
            summaryPath,
            request.BaselinePath,
            metrics,
            regression,
            baselinePromoted,
            regression
                ? "Learning eval detected a regression; baseline was not promoted."
                : "Learning eval completed and baseline was promoted.");
    }

    private LearningEvalMetrics ComputeMetrics(IReadOnlyList<LearningEvalExample> examples)
    {
        var exampleEmbeddings = examples
            .Select(example => (example, embedding: _embeddingService.Embed(example.Content)))
            .ToList();
        var reciprocalRanks = new List<double>();
        var recallAt1 = 0;
        var recallAt3 = 0;
        var latencies = new List<double>();

        foreach (var target in examples)
        {
            var stopwatch = Stopwatch.StartNew();
            var queryEmbedding = _embeddingService.Embed(target.Query);
            var ranked = exampleEmbeddings
                .Select(candidate => new
                {
                    candidate.example.SourceId,
                    Score = Cosine(queryEmbedding, candidate.embedding)
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.SourceId, StringComparer.Ordinal)
                .ToList();
            stopwatch.Stop();
            latencies.Add(stopwatch.Elapsed.TotalMilliseconds);

            var rank = ranked.FindIndex(candidate => string.Equals(candidate.SourceId, target.SourceId, StringComparison.OrdinalIgnoreCase)) + 1;
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

        return new LearningEvalMetrics(
            RecallAt1: recallAt1 / (double)examples.Count,
            RecallAt3: recallAt3 / (double)examples.Count,
            MeanReciprocalRank: reciprocalRanks.Average(),
            LatencyMsP50: Percentile(latencies, 0.50),
            LatencyMsP95: Percentile(latencies, 0.95));
    }

    private static IReadOnlyList<LearningEvalExample> ReadExamples(string examplesPath)
    {
        return File.ReadAllLines(examplesPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<LearningPromotionExampleRow>(line, JsonDefaults.Options))
            .Where(row => row is not null)
            .Select(row => new LearningEvalExample(
                SourceId: row!.SourcePath,
                Query: $"{row.Target} {row.Notes}".Trim(),
                Content: row.Content))
            .ToList();
    }

    private static string ResolveDatasetFolder(string datasetRoot, string datasetVersion)
    {
        if (!Directory.Exists(datasetRoot))
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(datasetVersion))
        {
            var requested = Path.Combine(datasetRoot, datasetVersion);
            return Directory.Exists(requested) ? requested : "";
        }

        return Directory.EnumerateDirectories(datasetRoot, "v????-*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? "";
    }

    private static LearningEvalRunRecord? ReadBaseline(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LearningEvalRunRecord>(File.ReadAllText(path), JsonDefaults.Options);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsRegression(LearningEvalMetrics current, LearningEvalMetrics prior, double tolerance)
    {
        return prior.RecallAt1 - current.RecallAt1 > tolerance ||
            prior.MeanReciprocalRank - current.MeanReciprocalRank > tolerance;
    }

    private static string BuildSummary(LearningEvalRunRecord record)
    {
        return string.Join(Environment.NewLine, [
            "# Learning Eval Run",
            "",
            $"- Dataset: {record.DatasetVersion}",
            $"- Examples evaluated: {record.ExamplesEvaluated}",
            $"- recall@1: {record.Metrics.RecallAt1:0.###}",
            $"- recall@3: {record.Metrics.RecallAt3:0.###}",
            $"- mrr: {record.Metrics.MeanReciprocalRank:0.###}",
            $"- latency p50: {record.Metrics.LatencyMsP50:0.###} ms",
            $"- latency p95: {record.Metrics.LatencyMsP95:0.###} ms",
            $"- Regression: {record.Regression.ToString().ToLowerInvariant()}",
            $"- Baseline promoted: {record.BaselinePromoted.ToString().ToLowerInvariant()}",
            "",
            "Eval is deterministic and local; it uses reviewed promoted data only."
        ]);
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

    private static double Cosine(float[] left, float[] right)
    {
        var count = Math.Min(left.Length, right.Length);
        if (count == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftLength = 0;
        double rightLength = 0;
        for (var index = 0; index < count; index++)
        {
            dot += left[index] * right[index];
            leftLength += left[index] * left[index];
            rightLength += right[index] * right[index];
        }

        return leftLength <= 0 || rightLength <= 0
            ? 0
            : dot / (Math.Sqrt(leftLength) * Math.Sqrt(rightLength));
    }

    private static LearningEvalResult Block(string message)
    {
        return new LearningEvalResult(false, "", "", "", "", new LearningEvalMetrics(0, 0, 0, 0, 0), false, false, message);
    }
}
