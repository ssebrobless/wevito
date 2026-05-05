using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetDebugTruthReportBuilderTests
{
    [Fact]
    public void Build_ReportsPetWellbeingAndActionReadiness()
    {
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
        var builder = new PetDebugTruthReportBuilder(engine);

        var report = builder.Build(content, [pet], CompanionMode.Focused, DateTimeOffset.Parse("2026-05-05T00:01:00Z"));

        Assert.Single(report.Pets);
        Assert.Equal(PetAnimationState.Sick, report.Pets[0].ExpectedAnimationHint);
        Assert.Contains(report.Actions, action => action.ActionId == "medicine" && action.IsEnabled);
        Assert.Contains(report.Findings, finding => finding.Contains("expected hint is Sick", StringComparison.Ordinal));
    }

    [Fact]
    public void ToMarkdown_IncludesPetsActionsAndFindings()
    {
        var content = BuildContent();
        var engine = new PetSimulationEngine();
        var pet = engine.CreatePet(
            content.Species[0],
            PetAgeStage.Adult,
            PetGender.Male,
            "orange",
            "Pip",
            DateTimeOffset.Parse("2026-05-05T00:00:00Z"));
        var builder = new PetDebugTruthReportBuilder(engine);
        var report = builder.Build(content, [pet], CompanionMode.Passive, DateTimeOffset.Parse("2026-05-05T00:01:00Z"));

        var markdown = builder.ToMarkdown(report);

        Assert.Contains("# Pet Debug Truth Report", markdown);
        Assert.Contains("| Pip |", markdown);
        Assert.Contains("| Feed |", markdown);
        Assert.Contains("## Findings", markdown);
    }

    private static GameContent BuildContent()
    {
        return new GameContent(
            [new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond")],
            [
                new ActionDefinition("feed", "Feed", "Feed pets"),
                new ActionDefinition("water", "Water", "Water pets"),
                new ActionDefinition("rest", "Rest", "Rest pets"),
                new ActionDefinition("play", "Play", "Play with pets"),
                new ActionDefinition("groom", "Groom", "Groom pets"),
                new ActionDefinition("bath", "Bath", "Bathe pets"),
                new ActionDefinition("medicine", "Medicine", "Treat pets"),
                new ActionDefinition("doctor", "Doctor", "Doctor care"),
                new ActionDefinition("home", "Home", "Call home")
            ],
            [],
            [],
            [],
            [],
            [],
            []);
    }
}
