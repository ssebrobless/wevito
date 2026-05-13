using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LearningLabPromotionServiceTests
{
    [Fact]
    public void Promote_BlocksWithoutApprovedLearningPromotionCard()
    {
        var root = CreateTempRoot();
        var bundle = CreateBundle(root);
        var service = new LearningLabPromotionService(new PetMemoryStore(Path.Combine(root, "memory")));

        var result = service.Promote(BuildRequest(root, bundle, BuildCard(TaskCardStatus.Draft)));

        Assert.False(result.Succeeded);
        Assert.Contains("approved task card", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Promote_FiltersOnlyAcceptedRowsAndWritesDatasetManifest()
    {
        var root = CreateTempRoot();
        var bundle = CreateBundle(root);
        var petId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var memoryStore = new PetMemoryStore(Path.Combine(root, "memory"));
        var service = new LearningLabPromotionService(memoryStore);

        var result = service.Promote(BuildRequest(root, bundle, BuildCard(TaskCardStatus.Approved), petId));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(4, result.ExcludedCount);
        Assert.Equal(1, result.MemoryRowsWritten);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.ExamplesPath));
        Assert.True(File.Exists(result.PromotionReportPath));
        var manifest = JsonSerializer.Deserialize<LearningDatasetManifest>(File.ReadAllText(result.ManifestPath), JsonDefaults.Options);
        Assert.NotNull(manifest);
        Assert.Equal(result.DatasetVersion, manifest.DatasetVersion);
        Assert.Equal(ComputeSha256(result.ExamplesPath), manifest.ExamplesSha256);
        Assert.False(manifest.AutomaticTrainingEnabled);
        Assert.False(manifest.AutomaticMemoryPromotionEnabled);
        Assert.Single(File.ReadAllLines(result.ExamplesPath));

        var stored = memoryStore.Search(petId, "goose silhouette", LearningLabPromotionService.ToolFamily, topK: 3);
        var example = Assert.Single(stored);
        Assert.Equal(result.DatasetVersion, example.Example.DatasetVersion);
        Assert.Equal(BuildCardId.ToString(), example.Example.SourceTaskCardId);
    }

    [Fact]
    public void Promote_DoesNotCopyBinaryAssets()
    {
        var root = CreateTempRoot();
        var bundle = CreateBundle(root);
        var service = new LearningLabPromotionService(new PetMemoryStore(Path.Combine(root, "memory")));

        var result = service.Promote(BuildRequest(root, bundle, BuildCard(TaskCardStatus.Approved)));

        Assert.True(result.Succeeded, result.Message);
        Assert.Empty(Directory.EnumerateFiles(result.DatasetFolder, "*.png", SearchOption.AllDirectories));
        Assert.Contains("Binary assets copied: false", File.ReadAllText(Path.Combine(Path.GetDirectoryName(result.PromotionReportPath)!, "run-summary.md")));
    }

    [Fact]
    public void BundleAndPromotionConstantsKeepTrainingOff()
    {
        Assert.False(LearningLabBundleService.AutomaticTrainingEnabled);
        Assert.False(LearningLabBundleService.AutomaticMemoryPromotionEnabled);
        Assert.False(LearningLabPromotionService.AutomaticTrainingEnabled);
        Assert.False(LearningLabPromotionService.AutomaticMemoryPromotionEnabled);
    }

    private static readonly Guid BuildCardId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static LearningLabPromotionRequest BuildRequest(
        string root,
        string bundle,
        TaskCard card,
        Guid? petId = null)
    {
        return new LearningLabPromotionRequest(
            bundle,
            Path.Combine(root, "vnext", "artifacts", "learning-datasets"),
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            card,
            petId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Scout",
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static TaskCard BuildCard(TaskCardStatus status)
    {
        var intent = new TaskIntent(
            BuildCardId,
            "promote reviewed learning bundle",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.UpdatePetMemory,
            RequestedToolFamily: LearningLabPromotionService.ToolFamily,
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
        return new TaskCard(
            BuildCardId,
            intent,
            status,
            ToolFamily: LearningLabPromotionService.ToolFamily,
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static string CreateBundle(string root)
    {
        var bundle = Path.Combine(root, "vnext", "artifacts", "creative-learning-lab", "20260512-120000-reviewed-bundle");
        Directory.CreateDirectory(bundle);
        var sourceRows = new[]
        {
            Source(root, "accepted.md", "visual-review", "goose/baby/female/blue/hold_ball"),
            Source(root, "rejected.md", "visual-review", "rat/baby/female/blue/idle"),
            Source(root, "revise.md", "visual-review", "pigeon/baby/female/blue/idle"),
            Source(root, "defer.md", "visual-review", "snake/baby/female/blue/idle"),
            Source(root, "blocked.md", "visual-review", "crow/baby/female/blue/idle")
        };
        var labelRows = new[]
        {
            Label(sourceRows[0].AbsolutePath, "accept", "clean goose silhouette"),
            Label(sourceRows[1].AbsolutePath, "reject", "wrong"),
            Label(sourceRows[2].AbsolutePath, "revise", "needs work"),
            Label(sourceRows[3].AbsolutePath, "defer", "later"),
            Label(sourceRows[4].AbsolutePath, "blocked", "human call")
        };

        File.WriteAllText(Path.Combine(bundle, "labels.json"), JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            labels = labelRows
        }, JsonDefaults.Options));
        File.WriteAllText(Path.Combine(bundle, "sources.json"), JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            copiedBinaryAssets = false,
            sources = sourceRows
        }, JsonDefaults.Options));
        File.WriteAllText(Path.Combine(bundle, "summary.md"), "# Reviewed Bundle");
        return bundle;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-learning-promotion-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static TestSourceRow Source(string root, string fileName, string artifactKind, string target)
    {
        var absolutePath = Path.Combine(root, "vnext", "artifacts", "visual-review", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.WriteAllText(absolutePath, "# source");
        return new TestSourceRow(
            Path.GetRelativePath(root, absolutePath).Replace(Path.DirectorySeparatorChar, '/'),
            absolutePath,
            artifactKind,
            target);
    }

    private static object Label(string absolutePath, string label, string notes)
    {
        return new
        {
            absolutePath,
            label,
            reviewer = "tester",
            notes
        };
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record TestSourceRow(
        string RelativePath,
        string AbsolutePath,
        string ArtifactKind,
        string Target);
}
