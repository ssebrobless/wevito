using SkiaSharp;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowContactSheetGeneratorTests
{
    [Fact]
    public void Generate_WritesContactSheetForRuntimeFrames()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-sheet-tests", Guid.NewGuid().ToString("N"));
        WritePng(Path.Combine(root, "sprites_runtime", "frog", "teen", "male", "yellow", "jump_00.png"), 24, 24);
        WritePng(Path.Combine(root, "sprites_runtime", "frog", "teen", "male", "yellow", "jump_01.png"), 30, 24);
        var row = new SpriteWorkflowManifestReader().Read(root).Rows.Single();
        var outputPath = Path.Combine(root, "vnext", "artifacts", "sheet.png");

        var result = new SpriteWorkflowContactSheetGenerator().Generate(
            new SpriteWorkflowContactSheetRequest(row, SpriteWorkflowRootKind.Runtime, outputPath, CellSize: 48));

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.FrameCount);
        Assert.True(File.Exists(outputPath));
        using var sheet = SKBitmap.Decode(outputPath);
        Assert.NotNull(sheet);
        Assert.Equal(96, sheet.Width);
        Assert.True(sheet.Height > 48);
    }

    [Fact]
    public void Generate_ReturnsFalseWhenRootKindIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-sheet-tests", Guid.NewGuid().ToString("N"));
        WritePng(Path.Combine(root, "sprites_runtime", "frog", "teen", "male", "yellow", "jump_00.png"), 24, 24);
        var row = new SpriteWorkflowManifestReader().Read(root).Rows.Single();

        var result = new SpriteWorkflowContactSheetGenerator().Generate(
            new SpriteWorkflowContactSheetRequest(row, SpriteWorkflowRootKind.AuthoredVerified, Path.Combine(root, "missing.png")));

        Assert.False(result.Succeeded);
        Assert.Equal(0, result.FrameCount);
    }

    private static void WritePng(string path, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = SKColors.DarkSeaGreen };
        canvas.DrawRoundRect(new SKRect(3, 3, width - 3, height - 3), 4, 4, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
    }
}
