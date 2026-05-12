using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LearningLabLabelBundleTests
{
    [Fact]
    public async Task LabelStore_SavesListsAndDeletesLatestLabel()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "wevito-learning-lab-label-tests", Guid.NewGuid().ToString("N"), "learning_lab.db");
        var store = new LearningLabLabelStore(databasePath);
        var sourcePath = Path.Combine(Path.GetTempPath(), "artifact.json");

        var saved = await store.SaveAsync(new LearningLabLabelInput(
            sourcePath,
            "accept",
            "tester",
            "clean example",
            DateTimeOffset.Parse("2026-05-07T00:00:00Z")));
        var latest = await store.GetLatestForSourceAsync(sourcePath);
        var all = await store.ListLatestAsync();
        var deleted = await store.DeleteForSourceAsync(sourcePath);

        Assert.True(saved.Id > 0);
        Assert.NotNull(latest);
        Assert.Equal("accept", latest.Label);
        Assert.Equal(LearningLabLabelStore.CurrentSchemaVersion, latest.SchemaVersion);
        Assert.Single(all);
        Assert.True(deleted > 0);
        Assert.Null(await store.GetLatestForSourceAsync(sourcePath));
    }

    [Fact]
    public async Task LabelStore_RejectsUnsupportedLabels()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "wevito-learning-lab-label-tests", Guid.NewGuid().ToString("N"), "learning_lab.db");
        var store = new LearningLabLabelStore(databasePath);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.SaveAsync(new LearningLabLabelInput(
                Path.Combine(Path.GetTempPath(), "artifact.json"),
                "train-now",
                "tester",
                "",
                DateTimeOffset.Parse("2026-05-07T00:00:00Z"))));
    }

    [Fact]
    public async Task LabelStore_RequiresReviewerForExportableLabels()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "wevito-learning-lab-label-tests", Guid.NewGuid().ToString("N"), "learning_lab.db");
        var store = new LearningLabLabelStore(databasePath);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveAsync(new LearningLabLabelInput(
                Path.Combine(Path.GetTempPath(), "artifact.json"),
                "accept",
                "",
                "clean example",
                DateTimeOffset.Parse("2026-05-07T00:00:00Z"))));
    }

    [Fact]
    public void BundleService_BlocksUntilAcceptedLabeledEvidenceHasUseAndRollback()
    {
        var root = CreateTempRoot();
        var artifact = BuildArtifact(root);
        var index = BuildIndex(root, artifact);

        var blocked = new LearningLabBundleService().Evaluate(new LearningLabBundleRequest(
            index,
            new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase),
            "",
            RollbackPathKnown: false));
        var ready = new LearningLabBundleService().Evaluate(new LearningLabBundleRequest(
            index,
            new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase)
            {
                [artifact.AbsolutePath] = new LearningLabLabelRecord(1, artifact.AbsolutePath, "accept", "tester", "", DateTimeOffset.Parse("2026-05-07T00:00:00Z"), 1)
            },
            "visual eval reference",
            RollbackPathKnown: true));

        Assert.False(blocked.IsReady);
        Assert.Contains(blocked.Reasons, reason => reason.Contains("reviewer label", StringComparison.OrdinalIgnoreCase));
        Assert.True(ready.IsReady);
        Assert.Equal(1, ready.AcceptedCount);
        Assert.Equal(0, ready.BlockedCount);
        Assert.Contains("optional-overlay-policy", ready.EvalBenchmarks);
    }

    [Fact]
    public void BundleService_BlockedLabelsPreventExportReadyResult()
    {
        var root = CreateTempRoot();
        var artifact = BuildArtifact(root);
        var index = BuildIndex(root, artifact);

        var result = new LearningLabBundleService().Evaluate(new LearningLabBundleRequest(
            index,
            new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase)
            {
                [artifact.AbsolutePath] = new LearningLabLabelRecord(1, artifact.AbsolutePath, "blocked", "tester", "needs human decision", DateTimeOffset.Parse("2026-05-07T00:00:00Z"), 1)
            },
            "visual eval reference",
            RollbackPathKnown: true));

        Assert.False(result.IsReady);
        Assert.Equal(1, result.BlockedCount);
        Assert.Contains(result.Reasons, reason => reason.Contains("Blocked labels", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BundleService_ExportsReviewedBundleUnderCreativeLearningLabArtifactsOnly()
    {
        var root = CreateTempRoot();
        var artifact = BuildArtifact(root);
        var index = BuildIndex(root, artifact);
        var outputRoot = Path.Combine(root, "vnext", "artifacts", "creative-learning-lab");

        var result = new LearningLabBundleService().ExportReviewedBundle(new LearningLabReviewedBundleExportRequest(
            new LearningLabBundleRequest(
                index,
                new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase)
                {
                    [artifact.AbsolutePath] = new LearningLabLabelRecord(1, artifact.AbsolutePath, "accept", "tester", "clean example", DateTimeOffset.Parse("2026-05-07T00:00:00Z"), 1)
                },
                "visual eval reference",
                RollbackPathKnown: true),
            outputRoot,
            DateTimeOffset.Parse("2026-05-07T00:00:01Z")));

        Assert.True(result.Succeeded);
        Assert.StartsWith(outputRoot, result.BundleFolder, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.LabelsPath));
        Assert.True(File.Exists(result.SourcesPath));
        Assert.True(File.Exists(result.SummaryPath));
        Assert.Contains("\"label\":\"accept\"", File.ReadAllText(result.LabelsPath));
        Assert.Contains("\"copiedBinaryAssets\":false", File.ReadAllText(result.SourcesPath));
        Assert.Contains("Nothing trains automatically", File.ReadAllText(result.SummaryPath));
    }

    [Fact]
    public void BundleService_RejectsExportOutsideCreativeLearningLabArtifacts()
    {
        var root = CreateTempRoot();
        var artifact = BuildArtifact(root);
        var index = BuildIndex(root, artifact);

        var result = new LearningLabBundleService().ExportReviewedBundle(new LearningLabReviewedBundleExportRequest(
            new LearningLabBundleRequest(
                index,
                new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase)
                {
                    [artifact.AbsolutePath] = new LearningLabLabelRecord(1, artifact.AbsolutePath, "accept", "tester", "clean example", DateTimeOffset.Parse("2026-05-07T00:00:00Z"), 1)
                },
                "visual eval reference",
                RollbackPathKnown: true),
            Path.Combine(root, "exports"),
            DateTimeOffset.Parse("2026-05-07T00:00:01Z")));

        Assert.False(result.Succeeded);
        Assert.Contains("creative-learning-lab", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BundleService_DoesNotEnableTrainingOrBinaryCopies()
    {
        Assert.False(LearningLabBundleService.AutomaticTrainingEnabled);
        Assert.False(LearningLabBundleService.AutomaticMemoryPromotionEnabled);
        Assert.False(LearningLabBundleService.CopiesBinaryAssets);
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-learning-lab-bundle-tests", Guid.NewGuid().ToString("N"));
    }

    private static LearningLabArtifactRecord BuildArtifact(string root)
    {
        return new LearningLabArtifactRecord(
            "manifest.json",
            "vnext/artifacts/animation-runs",
            "vnext/artifacts/animation-runs/run/manifest.json",
            Path.Combine(root, "vnext", "artifacts", "animation-runs", "run", "manifest.json"),
            "manifest.json",
            ".json",
            "optional-animation-candidate",
            "manifest",
            "review-needed",
            "goose/baby/female/blue/drop_ball",
            "json",
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
    }

    private static LearningLabArtifactIndex BuildIndex(string root, LearningLabArtifactRecord artifact)
    {
        return new LearningLabArtifactIndex(
            "1",
            root,
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"),
            new LearningLabMetrics(1, 0, 0, 0, 0, 0, 1),
            [artifact]);
    }
}
