using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AppRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsTaskCardsWithoutExecutingThem()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "wevito-tests", Guid.NewGuid().ToString("N"));
        var repository = new AppRepository(Path.Combine(tempRoot, "state.db"));
        await repository.InitializeAsync();
        var timestamp = DateTimeOffset.Parse("2026-05-05T11:00:00Z");
        var taskCard = BuildTaskCard(timestamp);
        var basketItem = new BasketItem(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            "https://example.test",
            "Example",
            "test",
            timestamp);
        var state = new CompanionState(
            CompanionMode.Focused,
            IsPinned: false,
            ActiveEnvironmentId: "pond",
            ActiveTool: new ToolSession("helpers", true),
            ActivePets:
            [
                new PetActor(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Bean", "goose")
            ],
            BasketItems: [basketItem],
            SettingsSnapshot: new Dictionary<string, string>
            {
                ["webtools_visible"] = bool.TrueString
            },
            TaskCards: [taskCard]);

        await repository.SaveAsync(state);

        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Single(loaded.TaskCards ?? []);
        Assert.Equal("Bean, review the goose hold_ball proof", loaded.TaskCards![0].Intent.RawText);
        Assert.Equal(TaskCardStatus.Draft, loaded.TaskCards[0].Status);
        Assert.Single(loaded.BasketItems);
        Assert.Equal("https://example.test", loaded.BasketItems[0].Url);
    }

    private static TaskCard BuildTaskCard(DateTimeOffset timestamp)
    {
        var intent = new TaskIntent(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            "Bean, review the goose hold_ball proof",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            CreatedAtUtc: timestamp);

        return new TaskCard(
            intent.Id,
            intent,
            TaskCardStatus.Draft,
            AssignedPetId: intent.TargetPetId,
            AssignedPetNameSnapshot: "Bean",
            ToolFamily: "spriteAudit",
            Timeline: ["draft_created"],
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);
    }
}
