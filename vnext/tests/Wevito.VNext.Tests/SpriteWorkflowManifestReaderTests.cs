using SkiaSharp;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowManifestReaderTests
{
    [Fact]
    public void Read_BuildsRowsAcrossRuntimeAuthoredAndVerifiedRoots()
    {
        var root = CreateRepoRoot();
        WritePng(Path.Combine(root, "sprites_runtime", "goose", "baby", "female", "blue", "idle_00.png"), 28, 24);
        WritePng(Path.Combine(root, "sprites_runtime", "goose", "baby", "female", "blue", "idle_01.png"), 28, 24);
        WritePng(Path.Combine(root, "sprites_authored", "goose", "baby", "female", "blue", "idle_00.png"), 56, 48);
        WritePng(Path.Combine(root, "sprites_authored_verified", "goose", "baby", "female", "blue", "idle_00.png"), 56, 48);

        var snapshot = new SpriteWorkflowManifestReader().Read(root, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        var row = Assert.Single(snapshot.Rows);
        Assert.Equal("goose/baby/female/blue/idle", row.RowId);
        Assert.Equal(PetAgeStage.Baby, row.Key.AgeStage);
        Assert.Equal(PetGender.Female, row.Key.Gender);
        Assert.Contains(row.Evidence, evidence => evidence.RootKind == SpriteWorkflowRootKind.Runtime && evidence.Frames.Count == 2);
        Assert.Contains(row.Evidence, evidence => evidence.RootKind == SpriteWorkflowRootKind.Authored && evidence.Frames.Count == 1);
        Assert.Contains(row.Evidence, evidence => evidence.RootKind == SpriteWorkflowRootKind.AuthoredVerified && evidence.Frames.Count == 1);
        Assert.All(row.Evidence.SelectMany(evidence => evidence.Frames), frame => Assert.Equal(64, frame.Blake3.Length));
    }

    [Fact]
    public void Read_ReportsMixedGeometryFinding()
    {
        var root = CreateRepoRoot();
        WritePng(Path.Combine(root, "sprites_runtime", "rat", "adult", "male", "red", "walk_00.png"), 28, 24);
        WritePng(Path.Combine(root, "sprites_runtime", "rat", "adult", "male", "red", "walk_01.png"), 32, 24);

        var snapshot = new SpriteWorkflowManifestReader().Read(root);

        var row = Assert.Single(snapshot.Rows);
        Assert.Contains(row.Findings, finding => finding.Contains("mixed frame geometry", StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateRepoRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-reader-tests", Guid.NewGuid().ToString("N"));
    }

    private static void WritePng(string path, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = SKColors.CornflowerBlue };
        canvas.DrawOval(new SKRect(4, 4, width - 4, height - 4), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
    }
}
