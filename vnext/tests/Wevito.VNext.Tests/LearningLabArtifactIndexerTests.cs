using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LearningLabArtifactIndexerTests
{
    [Fact]
    public void Index_ReadsMarkdownAndJsonArtifacts()
    {
        var root = CreateRepoRoot();
        WriteText(
            Path.Combine(root, "vnext", "artifacts", "visual-review", "shared-care-review.md"),
            "# Shared Care Review\n\nNotes.");
        WriteJson(
            Path.Combine(root, "vnext", "artifacts", "animation-runs", "goose-drop", "manifest.json"),
            """
            {
              "status": "review_only_not_applied",
              "target": {
                "species": "goose",
                "age": "baby",
                "gender": "female",
                "color": "blue",
                "family": "drop_ball"
              }
            }
            """);

        var index = new LearningLabArtifactIndexer().Index(new LearningLabArtifactIndexRequest(
            root,
            DateTimeOffset.Parse("2026-05-07T00:00:00Z")));

        Assert.Equal(2, index.Metrics.Raw);
        Assert.Equal(1, index.Metrics.MarkdownFiles);
        Assert.Equal(1, index.Metrics.JsonFiles);
        Assert.Contains(index.Artifacts, artifact => artifact.Title == "Shared Care Review");
        Assert.Contains(index.Artifacts, artifact =>
            artifact.Status == "review_only_not_applied" &&
            artifact.Target == "goose/baby/female/blue/drop_ball");
    }

    [Fact]
    public void Index_ToleratesInvalidJsonManifest()
    {
        var root = CreateRepoRoot();
        WriteJson(
            Path.Combine(root, "vnext", "artifacts", "animation-runs", "broken", "manifest.json"),
            "{ nope");

        var index = new LearningLabArtifactIndexer().Index(new LearningLabArtifactIndexRequest(root));

        var artifact = Assert.Single(index.Artifacts);
        Assert.Equal("parse-error", artifact.Status);
        Assert.Equal("invalid-json", artifact.ParseStatus);
    }

    [Fact]
    public void Index_ToleratesMissingArtifactRoots()
    {
        var root = CreateRepoRoot();

        var index = new LearningLabArtifactIndexer().Index(new LearningLabArtifactIndexRequest(root));

        Assert.Empty(index.Artifacts);
        Assert.Equal(0, index.Metrics.Raw);
    }

    private static string CreateRepoRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-learning-lab-indexer-tests", Guid.NewGuid().ToString("N"));
    }

    private static void WriteJson(string path, string json)
    {
        WriteText(path, json);
    }

    private static void WriteText(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, text);
    }
}
