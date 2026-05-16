using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Tools;

namespace Wevito.VNext.Tests;

public sealed class PetStateToolTests
{
    [Fact]
    public void ReturnsAccuratePetStateForActiveSlot()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var logger = new PetInteractionLogger(Path.Combine(root, "pet-interactions.jsonl"), ledger);
        var pets = BuildPets(now);
        logger.Append(new PetInteractionLogEntry(pets[1].Id, pets[1].Name, "feed", "test", now.AddMinutes(-5)));
        var tool = new PetStateTool(
            new UnifiedPolicyService(auditLedgerService: ledger),
            logger,
            ledger);

        var result = tool.GetState(new PetStateToolRequest(1), pets, now);

        Assert.NotNull(result);
        Assert.Equal(1, result!.PetSlot);
        Assert.Equal(pets[1].Id, result.PetId);
        Assert.Equal("fox", result.Species);
        Assert.Equal("seek_food_zone", result.CurrentGoal);
        Assert.Single(result.RecentInteractions);
        Assert.Equal("feed", result.RecentInteractions[0].ActionId);
    }

    [Fact]
    public void FallsBackToSlot0WhenNoArgs()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var tool = new PetStateTool(interactionLogger: new PetInteractionLogger(Path.Combine(CreateTempRoot(), "interactions.jsonl")));
        var pets = BuildPets(now);

        var result = tool.GetState(new PetStateToolRequest(), pets, now);

        Assert.NotNull(result);
        Assert.Equal(0, result!.PetSlot);
        Assert.Equal(pets[0].Id, result.PetId);
    }

    [Fact]
    public void RespectsUnifiedPolicyService()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var tool = new PetStateTool(
            new UnifiedPolicyService(auditLedgerService: ledger),
            new PetInteractionLogger(Path.Combine(root, "interactions.jsonl")),
            ledger);

        var result = tool.GetState(new PetStateToolRequest(0, Guid.Parse("11111111-1111-1111-1111-111111111111")), BuildPets(now), now);

        Assert.NotNull(result);
        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == "unified_policy" && row.Summary.Contains("PetStateRead", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rows, row => row.PacketKind == PetStateTool.PacketKind);
    }

    private static IReadOnlyList<PetActor> BuildPets(DateTimeOffset now)
    {
        var engine = new PetSimulationEngine();
        var goose = new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond");
        var fox = new SpeciesDefinition("fox", "Fox", "#d47431", 112, "den");
        return
        [
            engine.CreatePet(goose, PetAgeStage.Baby, PetGender.Female, "blue", "Goose 1", now, hunger: 84, thirst: 88),
            engine.CreatePet(fox, PetAgeStage.Adult, PetGender.Male, "red", "Fox 1", now, hunger: 20, thirst: 75, activeStatuses: [PetStatusType.Hungry])
        ];
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pet-state-tool-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
