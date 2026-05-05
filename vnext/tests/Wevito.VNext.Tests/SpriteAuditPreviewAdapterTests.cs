using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteAuditPreviewAdapterTests
{
    private readonly SpriteAuditPreviewAdapter _adapter = new();

    [Fact]
    public void BuildReport_WritesMarkdownAndJsonWithoutMutatingPngs()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(spriteRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-120000-sprite-audit");
        Directory.CreateDirectory(rowRoot);
        var pngPath = Path.Combine(rowRoot, "idle_00.png");
        File.WriteAllBytes(pngPath, BuildTinyPngBytes(width: 24, height: 28));
        var beforeHash = HashFile(pngPath);

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [rowRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Equal(beforeHash, HashFile(pngPath));
        Assert.Contains(pngPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("sprite-audit-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.WrittenPaths ?? [], path => Assert.StartsWith(artifactRoot, path, StringComparison.OrdinalIgnoreCase));

        var reportPath = Path.Combine(artifactRoot, "sprite-audit-report.json");
        var report = JsonSerializer.Deserialize<SpriteAuditReport>(File.ReadAllText(reportPath), JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal(1, report.PngCount);
        Assert.False(report.DidMutate);
        Assert.Equal(24, report.Findings[0].Width);
        Assert.Equal(28, report.Findings[0].Height);
    }

    [Fact]
    public void BuildReport_BlocksOutsideApprovedTarget()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var outsideRoot = Path.Combine(tempRoot, "outside");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-120000-sprite-audit");
        Directory.CreateDirectory(spriteRoot);
        Directory.CreateDirectory(outsideRoot);
        var outsidePng = Path.Combine(outsideRoot, "idle_00.png");
        File.WriteAllBytes(outsidePng, BuildTinyPngBytes(width: 24, height: 28));

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [outsidePng]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("outside approved", result.BlockReason);
        Assert.False(Directory.Exists(artifactRoot));
    }

    [Fact]
    public void BuildReport_ResolvesRelativeRowTargetInsideApprovedRoot()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(spriteRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-sprite-audit");
        Directory.CreateDirectory(rowRoot);
        var pngPath = Path.Combine(rowRoot, "idle_00.png");
        File.WriteAllBytes(pngPath, BuildTinyPngBytes(width: 24, height: 28));

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [Path.Combine("goose", "baby", "female", "blue")]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(pngPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildReport_BlocksRelativeTargetThatEscapesApprovedRoot()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-sprite-audit");
        Directory.CreateDirectory(spriteRoot);

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [Path.Combine("..", "outside")]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("escapes approved", result.BlockReason);
        Assert.Empty(result.WrittenPaths ?? []);
    }

    [Fact]
    public void BuildReport_BlocksExistingVisualPacketArtifactRoot()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(spriteRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "animation-runs", "20260505-goose-baby-female-blue-drop-ball-candidate");
        Directory.CreateDirectory(rowRoot);
        File.WriteAllBytes(Path.Combine(rowRoot, "idle_00.png"), BuildTinyPngBytes(width: 24, height: 28));

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [rowRoot]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("pet-tasks", result.BlockReason);
        Assert.Empty(result.WrittenPaths ?? []);
    }

    [Fact]
    public void BuildReport_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-120000-sprite-audit");
        Directory.CreateDirectory(spriteRoot);

        var result = _adapter.BuildReport(BuildRequest(spriteRoot, artifactRoot, [], TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("dry-run report", result.BlockReason);
        Assert.False(result.DidMutate);
    }

    private static TaskAdapterRequest BuildRequest(
        string spriteRoot,
        string artifactRoot,
        IReadOnlyList<string> targets,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            "Bean, audit goose sprites",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            "sprite-audit-readonly",
            "spriteAudit",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [spriteRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-audit-preview-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static byte[] BuildTinyPngBytes(int width, int height)
    {
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
        WriteBigEndianInt32(bytes, 16, width);
        WriteBigEndianInt32(bytes, 20, height);
        return bytes;
    }

    private static void WriteBigEndianInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xFF);
        bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 3] = (byte)(value & 0xFF);
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
