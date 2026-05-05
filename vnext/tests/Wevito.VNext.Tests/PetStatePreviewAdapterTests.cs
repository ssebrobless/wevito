using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetStatePreviewAdapterTests
{
    [Fact]
    public void BuildReport_WritesMarkdownAndJsonWithoutMutatingPets()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-160000-pet-state");
        var content = BuildContent();
        var engine = new PetSimulationEngine();
        var pet = engine.CreatePet(
            content.Species[0],
            PetAgeStage.Baby,
            PetGender.Female,
            "blue",
            "Bean",
            DateTimeOffset.Parse("2026-05-05T00:00:00Z"),
            health: 50,
            activeStatuses: [PetStatusType.Sick]);
        var before = JsonSerializer.Serialize(pet, JsonDefaults.Options);
        var adapter = new PetStatePreviewAdapter(new PetDebugTruthReportBuilder(engine));
        var request = BuildRequest(artifactRoot);

        var result = adapter.BuildReport(request, content, [pet], CompanionMode.Focused, DateTimeOffset.Parse("2026-05-05T00:01:00Z"));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.ReadPaths ?? []);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("pet-state-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, JsonSerializer.Serialize(pet, JsonDefaults.Options));
        Assert.Contains("expected hint is Sick", File.ReadAllText(Path.Combine(artifactRoot, "run-summary.md")));
    }

    [Fact]
    public void BuildReport_BlocksArtifactRootsOutsidePetTasks()
    {
        var tempRoot = CreateTempRoot();
        var adapter = new PetStatePreviewAdapter();

        var result = adapter.BuildReport(
            BuildRequest(Path.Combine(tempRoot, "outside")),
            BuildContent(),
            [],
            CompanionMode.Focused,
            DateTimeOffset.Parse("2026-05-05T00:01:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("pet-tasks", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(string artifactRoot)
    {
        var intent = new TaskIntent(
            Guid.Parse("81000000-0000-0000-0000-000000000001"),
            "Pip, review pet state",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Pip",
            TaskKind: TaskKind.ReviewPetState,
            RequestedToolFamily: "petState");
        var policy = new ToolPolicy(
            "pet-state-readonly",
            "petState",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: []);

        return new TaskAdapterRequest(
            Guid.Parse("91000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: artifactRoot);
    }

    private static GameContent BuildContent()
    {
        return new GameContent(
            [new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond")],
            [
                new ActionDefinition("feed", "Feed", "Feed pets"),
                new ActionDefinition("medicine", "Medicine", "Treat pets"),
                new ActionDefinition("doctor", "Doctor", "Doctor care")
            ],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pet-state-adapter-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
