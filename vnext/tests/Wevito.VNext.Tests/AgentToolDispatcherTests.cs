using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AgentToolDispatcherTests
{
    private readonly AgentToolDispatcher _dispatcher = new();

    [Fact]
    public void BuildPreview_RoutesLocalDocsRequests()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        Directory.CreateDirectory(docsRoot);
        var docPath = Path.Combine(docsRoot, "plan.md");
        File.WriteAllText(docPath, "local docs plan");

        var result = _dispatcher.BuildPreview(BuildRequest("localDocs", TaskKind.SummarizeDocs, docsRoot, [docsRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("localDocs", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(docPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("local-docs-preview-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
        Assert.EndsWith("run-summary.md", result.AuditLogPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPreview_DoesNotAppendModelSummaryByDefault()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        Directory.CreateDirectory(docsRoot);
        File.WriteAllText(Path.Combine(docsRoot, "plan.md"), "local docs plan");

        var result = _dispatcher.BuildPreview(BuildRequest("localDocs", TaskKind.SummarizeDocs, docsRoot, [docsRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.DoesNotContain("Model suggestion:", result.PreviewSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPreview_RoutesLocalResearchRequestsWithoutNetworkOrHostedAi()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260512-120000-local-research");
        Directory.CreateDirectory(docsRoot);
        var docPath = Path.Combine(docsRoot, "roadmap.md");
        File.WriteAllText(docPath, "local first AI roadmap");

        var result = _dispatcher.BuildPreview(BuildRequest("localResearch", TaskKind.Research, docsRoot, [docsRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("localResearch", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains("No hosted AI or network fetch was used", result.PreviewSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("research-evidence-packet.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesSpriteAuditRequests()
    {
        var tempRoot = CreateTempRoot();
        var spriteRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(spriteRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-sprite-audit");
        Directory.CreateDirectory(rowRoot);
        var pngPath = Path.Combine(rowRoot, "idle_00.png");
        File.WriteAllBytes(pngPath, BuildPngHeaderBytes(width: 24, height: 28));

        var result = _dispatcher.BuildPreview(BuildRequest("spriteAudit", TaskKind.ReviewSprites, spriteRoot, [rowRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("spriteAudit", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(pngPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("sprite-audit-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.WrittenPaths ?? [], path => Assert.StartsWith(artifactRoot, path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesPetStateRequestsWithRuntimeContext()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-pet-state");
        var content = BuildContent();
        var engine = new PetSimulationEngine();
        var pet = engine.CreatePet(
            content.Species[0],
            PetAgeStage.Baby,
            PetGender.Female,
            "blue",
            "Bean",
            DateTimeOffset.Parse("2026-05-05T00:00:00Z"),
            activeStatuses: [PetStatusType.Sick]);

        var result = _dispatcher.BuildPreview(
            BuildRequest("petState", TaskKind.ReviewPetState, tempRoot, [], artifactRoot),
            DateTimeOffset.Parse("2026-05-05T00:01:00Z"),
            content,
            [pet],
            CompanionMode.Focused);

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("petState", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("pet-state-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesAssetInventoryRequests()
    {
        var tempRoot = CreateTempRoot();
        var runtimeRoot = Path.Combine(tempRoot, "sprites_runtime");
        var rowRoot = Path.Combine(runtimeRoot, "goose", "baby", "female", "blue");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-asset-inventory");
        Directory.CreateDirectory(rowRoot);
        File.WriteAllBytes(Path.Combine(rowRoot, "idle_00.png"), BuildPngHeaderBytes(width: 24, height: 28));

        var result = _dispatcher.BuildPreview(BuildRequest("assetInventory", TaskKind.InventoryAssets, runtimeRoot, [], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("assetInventory", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("asset-inventory-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesCodeReviewRequests()
    {
        var tempRoot = CreateTempRoot();
        var sourceRoot = Path.Combine(tempRoot, "vnext", "src");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-code-review");
        Directory.CreateDirectory(sourceRoot);
        File.WriteAllText(Path.Combine(sourceRoot, "Sample.cs"), "public sealed class Sample {}\n");

        var result = _dispatcher.BuildPreview(BuildRequest("codeReview", TaskKind.ReviewCode, tempRoot, [sourceRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("codeReview", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("code-review-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesCodePatchPlanRequests()
    {
        var tempRoot = CreateTempRoot();
        var sourceRoot = Path.Combine(tempRoot, "vnext", "src");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-code-patch-plan");
        Directory.CreateDirectory(sourceRoot);
        File.WriteAllText(Path.Combine(sourceRoot, "Sample.cs"), "public sealed class Sample {}\n");

        var result = _dispatcher.BuildPreview(BuildRequest("codePatchPlan", TaskKind.PlanCodePatch, tempRoot, [sourceRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("codePatchPlan", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("code-patch-plan-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesBuildProofRequests()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-141000-build-proof");
        Directory.CreateDirectory(tempRoot);

        var result = _dispatcher.BuildPreview(BuildRequest("buildProof", TaskKind.BuildProof, tempRoot, [], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("buildProof", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("build-proof-plan-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesTranslateTextRequests()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-142000-translate-text");
        Directory.CreateDirectory(tempRoot);

        var result = _dispatcher.BuildPreview(BuildRequest("translateText", TaskKind.TranslateText, tempRoot, [], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("translateText", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("translation-preview-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesAudioAssistRequests()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-145000-audio-assist");
        Directory.CreateDirectory(tempRoot);

        var result = _dispatcher.BuildPreview(BuildRequest("audioAssist", TaskKind.AudioAssist, tempRoot, [], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("audioAssist", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("audio-assist-status-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_RoutesScreenCaptureRequests()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-152000-screen-capture");
        Directory.CreateDirectory(tempRoot);

        var result = _dispatcher.BuildPreview(BuildRequest("screenCapture", TaskKind.ScreenCapture, tempRoot, [], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Equal("screenCapture", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("screen-capture-preview-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public void BuildPreview_BlocksUnknownToolFamily()
    {
        var tempRoot = CreateTempRoot();
        Directory.CreateDirectory(tempRoot);

        var result = _dispatcher.BuildPreview(BuildRequest("proofCapture", TaskKind.CaptureProof, tempRoot, [tempRoot]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Equal("proofCapture", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Empty(result.ReadPaths ?? []);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("No PET TASKS dry-run preview adapter", result.BlockReason);
    }

    [Fact]
    public void BuildPreview_BlocksMismatchedIntentAndPolicyFamiliesBeforeRouting()
    {
        var tempRoot = CreateTempRoot();
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "Bean, audit sprites",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            TargetPathsOrAssets: [tempRoot]);
        var policy = new ToolPolicy(
            "local-docs-readonly",
            "localDocs",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [tempRoot]);

        var result = _dispatcher.BuildPreview(new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-mismatch")));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Equal("localDocs", result.ToolFamily);
        Assert.False(result.DidMutate);
        Assert.Empty(result.ReadPaths ?? []);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("same tool family", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        string toolFamily,
        TaskKind taskKind,
        string approvedRoot,
        IReadOnlyList<string> targets,
        string? artifactRoot = null)
    {
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "Bean, run a preview",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: taskKind,
            RequestedToolFamily: toolFamily,
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            toolFamily + "-readonly",
            toolFamily,
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [approvedRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: artifactRoot ?? Path.Combine(approvedRoot, "vnext", "artifacts", "pet-tasks", "20260505-130000-preview"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pet-task-dispatcher-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static GameContent BuildContent()
    {
        return new GameContent(
            [new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond")],
            [
                new ActionDefinition("feed", "Feed", "Feed pets"),
                new ActionDefinition("medicine", "Medicine", "Treat pets")
            ],
            [],
            [],
            [],
            [],
            [],
            []);
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
