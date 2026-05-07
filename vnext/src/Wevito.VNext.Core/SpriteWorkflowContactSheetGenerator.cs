using SkiaSharp;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowContactSheetRequest(
    SpriteWorkflowQueueRow Row,
    SpriteWorkflowRootKind RootKind,
    string OutputPath,
    int CellSize = 72);

public sealed record SpriteWorkflowContactSheetResult(
    bool Succeeded,
    string OutputPath,
    int FrameCount,
    string Message);

public sealed class SpriteWorkflowContactSheetGenerator
{
    public SpriteWorkflowContactSheetResult Generate(SpriteWorkflowContactSheetRequest request)
    {
        var evidence = request.Row.Evidence.FirstOrDefault(item => item.RootKind == request.RootKind);
        if (evidence is null || evidence.Frames.Count == 0)
        {
            return new SpriteWorkflowContactSheetResult(false, request.OutputPath, 0, $"No {request.RootKind} frames are available.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath)) ?? ".");
        var cellSize = Math.Max(32, request.CellSize);
        var labelHeight = 18;
        var width = Math.Max(cellSize, evidence.Frames.Count * cellSize);
        var height = cellSize + labelHeight;
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(16, 26, 34, 255));

        using var borderPaint = new SKPaint { Color = new SKColor(91, 126, 144, 255), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var textPaint = new SKPaint { Color = new SKColor(235, 244, 246, 255), TextSize = 10, IsAntialias = true };
        using var transparentPaint = new SKPaint { Color = new SKColor(35, 50, 59, 255), Style = SKPaintStyle.Fill };

        for (var index = 0; index < evidence.Frames.Count; index++)
        {
            var frame = evidence.Frames[index];
            var x = index * cellSize;
            var frameRect = new SKRect(x + 4, 4, x + cellSize - 4, cellSize - 8);
            canvas.DrawRect(new SKRect(x, 0, x + cellSize, cellSize), transparentPaint);
            canvas.DrawRect(new SKRect(x + 0.5f, 0.5f, x + cellSize - 0.5f, cellSize - 0.5f), borderPaint);

            using var bitmap = SKBitmap.Decode(frame.AbsolutePath);
            if (bitmap is not null)
            {
                var destination = FitInside(bitmap.Width, bitmap.Height, frameRect);
                canvas.DrawBitmap(bitmap, destination);
            }

            canvas.DrawText(frame.FrameId, x + 5, cellSize + 12, textPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(request.OutputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);

        return new SpriteWorkflowContactSheetResult(true, request.OutputPath, evidence.Frames.Count, $"Wrote {request.RootKind} contact sheet.");
    }

    private static SKRect FitInside(int sourceWidth, int sourceHeight, SKRect bounds)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return bounds;
        }

        var scale = Math.Min(bounds.Width / sourceWidth, bounds.Height / sourceHeight);
        var width = sourceWidth * scale;
        var height = sourceHeight * scale;
        var left = bounds.Left + (bounds.Width - width) / 2f;
        var top = bounds.Top + (bounds.Height - height) / 2f;
        return new SKRect(left, top, left + width, top + height);
    }
}
