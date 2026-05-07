using SkiaSharp;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowDryRunApplyServiceTests
{
    [Fact]
    public void Plan_WritesManifestWithoutChangingRuntimeFiles()
    {
        var root = CreateRepoRoot();
        var target = new SpriteRowKey("pigeon", PetAgeStage.Teen, PetGender.Male, "red", "hold_ball");
        var runtimePath = Path.Combine(root, "sprites_runtime", "pigeon", "teen", "male", "red", "hold_ball_00.png");
        WritePng(runtimePath, 24, 24, SKColors.Blue);
        var beforeBytes = File.ReadAllBytes(runtimePath);
        var candidateFolder = SpriteWorkflowCandidateImporter.ResolveCandidateFolder(root, target, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
        WritePng(Path.Combine(candidateFolder, "hold_ball_00.png"), 24, 24, SKColors.Green);
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "dryrun");

        var result = new SpriteWorkflowDryRunApplyService().Plan(new SpriteWorkflowDryRunApplyRequest(
            root,
            target,
            candidateFolder,
            artifactRoot,
            DateTimeOffset.Parse("2026-05-07T00:00:01Z")));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Manifest);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(result.Manifest.WouldMutateRuntime);
        var change = Assert.Single(result.Manifest.Changes);
        Assert.True(change.WouldOverwriteRuntime);
        Assert.Equal(beforeBytes, File.ReadAllBytes(runtimePath));
    }

    [Fact]
    public void Plan_RejectsCandidateFolderOutsideCandidatesRoot()
    {
        var root = CreateRepoRoot();
        var target = new SpriteRowKey("pigeon", PetAgeStage.Teen, PetGender.Male, "red", "hold_ball");
        var unsafeFolder = Path.Combine(root, "sprites_runtime", "pigeon", "teen", "male", "red");
        WritePng(Path.Combine(unsafeFolder, "hold_ball_00.png"), 24, 24, SKColors.Green);

        var result = new SpriteWorkflowDryRunApplyService().Plan(new SpriteWorkflowDryRunApplyRequest(
            root,
            target,
            unsafeFolder,
            Path.Combine(root, "vnext", "artifacts", "dryrun"),
            DateTimeOffset.UtcNow));

        Assert.False(result.Succeeded);
        Assert.Contains(".candidates", result.Message);
    }

    private static string CreateRepoRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-dryrun-tests", Guid.NewGuid().ToString("N"));
    }

    private static void WritePng(string path, int width, int height, SKColor color)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = color };
        canvas.DrawOval(new SKRect(3, 3, width - 3, height - 3), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
    }
}
