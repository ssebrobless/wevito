using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetTasksPreviewSmokeTests
{
    [Fact]
    public void SpriteAuditPreview_ParsesRoutesAndWritesPetTaskReportWithoutMutatingSprites()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(spriteRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-150000-sprite-audit");
        Directory.CreateDirectory(rowRoot);
        var pngPath = Path.Combine(rowRoot, "idle_00.png");
        File.WriteAllBytes(pngPath, BuildPngHeaderBytes(width: 24, height: 28));
        var beforeBytes = File.ReadAllBytes(pngPath);

        var parser = new ChatPromptParser();
        var dispatcher = new AgentToolDispatcher();
        var helper = new AgentSlotProfile(AgentSlotService.BuildSlotId(0), "Bean", 0);
        var intent = parser.Parse("Bean, review goose baby female blue sprites", [helper]);
        var card = parser.CreateDraftTaskCard(intent, [helper]);
        var policy = new ToolPolicy(
            "sprite-audit-readonly",
            "spriteAudit",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [spriteRoot]);

        var result = dispatcher.BuildPreview(new TaskAdapterRequest(
            card.Id,
            intent,
            policy,
            TaskAdapterRunMode.DryRunPreview,
            artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(pngPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(artifactRoot, "run-summary.md")));
        Assert.True(File.Exists(Path.Combine(artifactRoot, "sprite-audit-report.json")));
        Assert.Equal(beforeBytes, File.ReadAllBytes(pngPath));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pet-tasks-preview-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static byte[] BuildPngHeaderBytes(int width, int height)
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
}
