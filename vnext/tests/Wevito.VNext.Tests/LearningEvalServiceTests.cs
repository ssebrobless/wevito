using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LearningEvalServiceTests
{
    [Fact]
    public void Evaluate_ComputesMetricsAndPromotesFirstBaseline()
    {
        var root = CreateTempRoot();
        var datasetRoot = CreateDataset(root);
        var service = new LearningEvalService(new KeywordEmbeddingService());

        var result = service.Evaluate(new LearningEvalRequest(
            datasetRoot,
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            Path.Combine(datasetRoot, "eval-baseline.json"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.BaselinePromoted);
        Assert.False(result.Regression);
        Assert.Equal(1, result.Metrics.RecallAt1);
        Assert.Equal(1, result.Metrics.RecallAt3);
        Assert.Equal(1, result.Metrics.MeanReciprocalRank);
        Assert.True(File.Exists(result.ReportPath));
        Assert.True(File.Exists(result.SummaryPath));
        Assert.True(File.Exists(result.BaselinePath));
    }

    [Fact]
    public void Evaluate_BlocksBaselinePromotionWhenRegressionExceedsTolerance()
    {
        var root = CreateTempRoot();
        var datasetRoot = CreateDataset(root);
        var baselinePath = Path.Combine(datasetRoot, "eval-baseline.json");
        var prior = new LearningEvalRunRecord(
            "1",
            "v0001-20260512-120000",
            "",
            new LearningEvalMetrics(1, 1, 1, 1, 1),
            null,
            false,
            true,
            3,
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
        File.WriteAllText(baselinePath, JsonSerializer.Serialize(prior, JsonDefaults.Options));
        var service = new LearningEvalService(new ConstantEmbeddingService());

        var result = service.Evaluate(new LearningEvalRequest(
            datasetRoot,
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            baselinePath,
            DateTimeOffset.Parse("2026-05-12T12:30:00Z")));

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.Regression);
        Assert.False(result.BaselinePromoted);
        var baselineAfter = JsonSerializer.Deserialize<LearningEvalRunRecord>(File.ReadAllText(baselinePath), JsonDefaults.Options);
        Assert.NotNull(baselineAfter);
        Assert.Equal(1, baselineAfter.Metrics.RecallAt1);
    }

    [Fact]
    public void PetMemoryStore_RenamesLegacyDatabaseWhenEmbeddingDimensionsChange()
    {
        var root = Path.Combine(CreateTempRoot(), "memory");
        var petId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var first = new PetMemoryStore(root, new HashingTextEmbeddingService(16));
        first.AddExample(petId, "localDocs", "goose docs", "docs", DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
        var second = new PetMemoryStore(root, new HashingTextEmbeddingService(24));

        var status = second.EnsureReady(petId, DateTimeOffset.Parse("2026-05-12T12:30:00Z"));

        Assert.True(status.WasRebuilt);
        Assert.Equal(24, status.EmbeddingDimensions);
        Assert.True(Directory.EnumerateFiles(Path.GetDirectoryName(status.DatabasePath)!, "*.legacy-*.db").Any());
    }

    private static string CreateDataset(string root)
    {
        var datasetRoot = Path.Combine(root, "vnext", "artifacts", "learning-datasets");
        var dataset = Path.Combine(datasetRoot, "v0001-20260512-120000");
        Directory.CreateDirectory(dataset);
        var rows = new[]
        {
            Row("goose-hold", "visual-review", "goose baby female blue hold_ball", "clean goose silhouette"),
            Row("rat-habitat", "visual-review", "rat adult male habitat", "rat perch staging"),
            Row("frog-jump", "visual-review", "frog baby female jump", "frog extended legs")
        };
        File.WriteAllLines(Path.Combine(dataset, "examples.jsonl"), rows.Select(row => JsonSerializer.Serialize(row, JsonDefaults.Options)));
        return datasetRoot;
    }

    private static LearningPromotionExampleRow Row(string id, string kind, string target, string notes)
    {
        return new LearningPromotionExampleRow(
            SourcePath: id,
            RelativePath: $"{id}.md",
            ArtifactKind: kind,
            Target: target,
            Label: "accept",
            Reviewer: "tester",
            Notes: notes,
            Content: $"{kind} {target} {notes}");
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-learning-eval-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class KeywordEmbeddingService : ITextEmbeddingService
    {
        public int Dimensions => 3;

        public float[] Embed(string text)
        {
            var normalized = text.ToLowerInvariant();
            return [
                normalized.Contains("goose", StringComparison.Ordinal) ? 1 : 0,
                normalized.Contains("rat", StringComparison.Ordinal) ? 1 : 0,
                normalized.Contains("frog", StringComparison.Ordinal) ? 1 : 0
            ];
        }
    }

    private sealed class ConstantEmbeddingService : ITextEmbeddingService
    {
        public int Dimensions => 4;

        public float[] Embed(string text)
        {
            return [1, 0, 0, 0];
        }
    }
}
