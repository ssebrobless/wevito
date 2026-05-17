using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AppRepositoryTests
{
    [Fact]
    public void ResolveDefaultDatabasePath_UsesLiveSqliteProfileLocation()
    {
        var root = Path.Combine("C:\\Users\\Example\\AppData\\Local");

        var path = AppRepository.ResolveDefaultDatabasePath(root);

        Assert.Equal(Path.Combine(root, "WevitoVNext", "wevito-vnext.db"), path);
    }

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
        Assert.Equal(AppRepository.CurrentSchemaVersion, loaded.SchemaVersion);
    }

    [Fact]
    public async Task LoadAsync_MigratesLegacyStateToSchemaVersion2()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "wevito-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(tempRoot, "state.db");
        var repository = new AppRepository(databasePath);
        await repository.InitializeAsync();

        const string legacyState = """
            {
              "mode": "Focused",
              "isPinned": false,
              "activeEnvironmentId": "pond",
              "activeTool": { "toolId": "", "isOpen": true },
              "activePets": [
                {
                  "id": "10000000-0000-0000-0000-000000000001",
                  "name": "Bean",
                  "speciesId": "goose",
                  "activeStatuses": []
                }
              ],
              "basketItems": [],
              "settingsSnapshot": {}
            }
            """;

        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_state (key, value)
                VALUES ('companion_state', $value);
                """;
            command.Parameters.AddWithValue("$value", legacyState);
            await command.ExecuteNonQueryAsync();
        }

        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(AppRepository.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal("basket", loaded.ActiveTool.ToolId);
        Assert.True(loaded.ActiveTool.IsOpen);
        var pet = Assert.Single(loaded.ActivePets);
        Assert.False(pet.IsDead);
        Assert.False(pet.IsGhost);
        Assert.Null(pet.DiedAtUtc);
        Assert.Null(pet.MemorialExpiresAtUtc);
        Assert.Equal(string.Empty, pet.MemorialObjectId);
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
