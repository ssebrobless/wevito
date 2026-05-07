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
    public void BundleService_BlocksUntilAcceptedLabeledEvidenceHasUseAndRollback()
    {
        var artifact = new LearningLabArtifactRecord(
            "manifest.json",
            "vnext/artifacts/animation-runs",
            "vnext/artifacts/animation-runs/run/manifest.json",
            Path.Combine(Path.GetTempPath(), "run", "manifest.json"),
            "manifest.json",
            ".json",
            "optional-animation-candidate",
            "manifest",
            "review-needed",
            "goose/baby/female/blue/drop_ball",
            "json",
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
        var index = new LearningLabArtifactIndex(
            "1",
            Path.GetTempPath(),
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"),
            new LearningLabMetrics(1, 0, 0, 0, 0, 0, 1),
            [artifact]);

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
        Assert.Contains("optional-overlay-policy", ready.EvalBenchmarks);
    }
}
