using SkiaSharp;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowApplyRollbackTests
{
    [Fact]
    public void Apply_CreatesBackupAndReplacesRuntimeFrame()
    {
        var fixture = CreateFixture();
        var dryRun = CreateDryRun(fixture.Root, fixture.Target, fixture.CandidateFolder);
        var runtimePath = dryRun.Changes.Single().RuntimePath;
        var before = File.ReadAllBytes(runtimePath);

        var result = new SpriteWorkflowApplyService().Apply(new SpriteWorkflowApplyRequest(dryRun, DateTimeOffset.Parse("2026-05-07T00:00:02Z")));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Manifest);
        Assert.True(File.Exists(result.ApplyLogPath));
        Assert.True(File.Exists(Path.Combine(result.Manifest.BackupFolder, Path.GetFileName(runtimePath))));
        Assert.NotEqual(before, File.ReadAllBytes(runtimePath));
    }

    [Fact]
    public void Rollback_RestoresRuntimeFrameByBackupHash()
    {
        var fixture = CreateFixture();
        var dryRun = CreateDryRun(fixture.Root, fixture.Target, fixture.CandidateFolder);
        var runtimePath = dryRun.Changes.Single().RuntimePath;
        var before = File.ReadAllBytes(runtimePath);
        var apply = new SpriteWorkflowApplyService().Apply(new SpriteWorkflowApplyRequest(dryRun, DateTimeOffset.Parse("2026-05-07T00:00:02Z"))).Manifest!;

        var rollback = new SpriteWorkflowRollbackService().Rollback(new SpriteWorkflowRollbackRequest(apply, DateTimeOffset.Parse("2026-05-07T00:00:03Z")));

        Assert.True(rollback.Succeeded);
        Assert.True(File.Exists(rollback.RollbackLogPath));
        Assert.Equal(before, File.ReadAllBytes(runtimePath));
    }

    [Fact]
    public void PostApplyProof_FailureRollsBack()
    {
        var fixture = CreateFixture();
        var dryRun = CreateDryRun(fixture.Root, fixture.Target, fixture.CandidateFolder);
        var runtimePath = dryRun.Changes.Single().RuntimePath;
        var before = File.ReadAllBytes(runtimePath);
        var apply = new SpriteWorkflowApplyService().Apply(new SpriteWorkflowApplyRequest(dryRun, DateTimeOffset.Parse("2026-05-07T00:00:02Z"))).Manifest!;

        var proof = new SpriteWorkflowPostApplyProof(_ => false)
            .VerifyOrRollback(apply, DateTimeOffset.Parse("2026-05-07T00:00:03Z"));

        Assert.False(proof.Succeeded);
        Assert.True(proof.RolledBack);
        Assert.Equal(before, File.ReadAllBytes(runtimePath));
    }

    [Fact]
    public void Apply_PrunesBackupsToRetentionLimit()
    {
        var fixture = CreateFixture();
        var backupRoot = Path.Combine(fixture.Root, "sprites_runtime", ".backup");
        Directory.CreateDirectory(backupRoot);
        for (var index = 0; index < 55; index++)
        {
            Directory.CreateDirectory(Path.Combine(backupRoot, $"old-{index:00}"));
        }

        var dryRun = CreateDryRun(fixture.Root, fixture.Target, fixture.CandidateFolder);
        var result = new SpriteWorkflowApplyService().Apply(new SpriteWorkflowApplyRequest(dryRun, DateTimeOffset.Parse("2026-05-07T00:00:02Z")));

        Assert.True(result.Succeeded);
        Assert.True(Directory.GetDirectories(backupRoot).Length <= SpriteWorkflowApplyService.BackupRetentionLimit);
    }

    [Fact]
    public void Apply_RejectsStagingOnDifferentVolume()
    {
        var fixture = CreateFixture();
        var dryRun = CreateDryRun(fixture.Root, fixture.Target, fixture.CandidateFolder);

        var result = new SpriteWorkflowApplyService((_, _) => false)
            .Apply(new SpriteWorkflowApplyRequest(dryRun, DateTimeOffset.Parse("2026-05-07T00:00:02Z")));

        Assert.False(result.Succeeded);
        Assert.Contains("same volume", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyConfirmation_RequiresExactRowId()
    {
        const string rowId = "goose/baby/female/blue/hold_ball";

        Assert.True(SpriteWorkflowV2Window.IsApplyConfirmationMatch("goose/baby/female/blue/hold_ball", rowId));
        Assert.True(SpriteWorkflowV2Window.IsApplyConfirmationMatch("  goose/baby/female/blue/hold_ball  ", rowId));
        Assert.False(SpriteWorkflowV2Window.IsApplyConfirmationMatch("goose/baby/female/blue/drop_ball", rowId));
        Assert.False(SpriteWorkflowV2Window.IsApplyConfirmationMatch("GOOSE/baby/female/blue/hold_ball", rowId));
    }

    private static (string Root, SpriteRowKey Target, string CandidateFolder) CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-workflow-apply-tests", Guid.NewGuid().ToString("N"));
        var target = new SpriteRowKey("snake", PetAgeStage.Adult, PetGender.Female, "violet", "drop_ball");
        WritePng(Path.Combine(root, "sprites_runtime", "snake", "adult", "female", "violet", "drop_ball_00.png"), SKColors.Blue);
        var candidateFolder = SpriteWorkflowCandidateImporter.ResolveCandidateFolder(root, target, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
        WritePng(Path.Combine(candidateFolder, "drop_ball_00.png"), SKColors.Green);
        return (root, target, candidateFolder);
    }

    private static SpriteWorkflowDryRunApplyManifest CreateDryRun(string root, SpriteRowKey target, string candidateFolder)
    {
        return new SpriteWorkflowDryRunApplyService()
            .Plan(new SpriteWorkflowDryRunApplyRequest(
                root,
                target,
                candidateFolder,
                Path.Combine(root, "vnext", "artifacts", "dryrun"),
                DateTimeOffset.Parse("2026-05-07T00:00:01Z")))
            .Manifest!;
    }

    private static void WritePng(string path, SKColor color)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var bitmap = new SKBitmap(24, 24);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = color };
        canvas.DrawOval(new SKRect(3, 3, 21, 21), paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(stream);
    }
}
