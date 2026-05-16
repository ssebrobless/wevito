using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetInteractionLoggerTests
{
    [Fact]
    public void AppendIsAppendOnly()
    {
        var root = CreateTempRoot();
        var logPath = Path.Combine(root, "pet-interactions.jsonl");
        var logger = new PetInteractionLogger(logPath);
        var petId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        logger.Append(new PetInteractionLogEntry(petId, "Pip", "feed", "test", now));
        logger.Append(new PetInteractionLogEntry(petId, "Pip", "water", "test", now.AddMinutes(1)));

        var lines = File.ReadAllLines(logPath);
        Assert.Equal(2, lines.Length);
        Assert.Contains("feed", lines[0]);
        Assert.Contains("water", lines[1]);
    }

    [Fact]
    public void RecentInteractionsRespectsTimeWindow()
    {
        var root = CreateTempRoot();
        var logger = new PetInteractionLogger(Path.Combine(root, "pet-interactions.jsonl"));
        var petId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var otherPetId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        logger.Append(new PetInteractionLogEntry(petId, "Pip", "feed", "test", now.AddHours(-2)));
        logger.Append(new PetInteractionLogEntry(otherPetId, "Bean", "water", "test", now.AddMinutes(-10)));
        logger.Append(new PetInteractionLogEntry(petId, "Pip", "groom", "test", now.AddMinutes(-5)));

        var recent = logger.RecentInteractions(petId, now.AddHours(-1), now);

        Assert.Single(recent);
        Assert.Equal("groom", recent[0].ActionId);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-pet-interaction-logger-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
