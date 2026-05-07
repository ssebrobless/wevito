using SkiaSharp;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowCandidateImporterTests
{
    [Fact]
    public void Import_CopiesOnlyIntoCandidatesFolder()
    {
        var root = CreateRepoRoot();
        var source = Path.Combine(root, "incoming");
        WritePng(Path.Combine(source, "frame-a.png"), 24, 24);
        WritePng(Path.Combine(source, "frame-b.png"), 24, 24);
        var target = new SpriteRowKey("goose", PetAgeStage.Baby, PetGender.Female, "blue", "drop_ball");
        var timestamp = DateTimeOffset.Parse("2026-05-07T00:00:00Z");

        var result = new SpriteWorkflowCandidateImporter().Import(
            new SpriteWorkflowCandidateImportRequest(root, target, source, timestamp));

        Assert.True(result.Succeeded);
        Assert.Contains(Path.Combine("sprites_authored", "goose", "baby", "female", "blue", ".candidates"), result.CandidateFolder);
        Assert.True(File.Exists(Path.Combine(result.CandidateFolder, "drop_ball_00.png")));
        Assert.True(File.Exists(Path.Combine(result.CandidateFolder, "drop_ball_01.png")));
        Assert.True(File.Exists(result.ManifestPath));
        Assert.False(Directory.Exists(Path.Combine(root, "sprites_runtime", ".candidates")));
    }

    [Fact]
    public void Import_RefusesToOverwriteExistingCandidateFolder()
    {
        var root = CreateRepoRoot();
        var source = Path.Combine(root, "incoming");
        WritePng(Path.Combine(source, "frame-a.png"), 24, 24);
        var target = new SpriteRowKey("goose", PetAgeStage.Baby, PetGender.Female, "blue", "drop_ball");
        var timestamp = DateTimeOffset.Parse("2026-05-07T00:00:00Z");
        var importer = new SpriteWorkflowCandidateImporter();

        var first = importer.Import(new SpriteWorkflowCandidateImportRequest(root, target, source, timestamp));
        var second = importer.Import(new SpriteWorkflowCandidateImportRequest(root, target, source, timestamp));

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.Message);
    }

    private static string CreateRepoRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-import-tests", Guid.NewGuid().ToString("N"));
    }

    private static void WritePng(string path, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = SKColors.Orange };
        canvas.DrawRect(new SKRect(3, 3, width - 3, height - 3), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
    }
}
