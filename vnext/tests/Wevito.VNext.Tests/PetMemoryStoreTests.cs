using Microsoft.Data.Sqlite;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetMemoryStoreTests
{
    [Fact]
    public void EnsureReady_CreatesPerPetDatabaseWithWalAndSchema()
    {
        var root = CreateTempRoot();
        var petId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var store = new PetMemoryStore(root);

        var status = store.EnsureReady(petId);

        Assert.True(status.DatabaseExists);
        Assert.True(File.Exists(status.DatabasePath));
        Assert.EndsWith(Path.Combine("memory", $"{petId:N}.db"), status.DatabasePath, StringComparison.OrdinalIgnoreCase);
        using var connection = new SqliteConnection($"Data Source={status.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM schema_info WHERE key = 'schema_version';";
        Assert.Equal(PetMemoryStore.SchemaVersion, command.ExecuteScalar()?.ToString());
    }

    [Fact]
    public void AddExample_AndSearch_ReturnsRelevantMemory()
    {
        var store = new PetMemoryStore(CreateTempRoot());
        var petId = Guid.Parse("20000000-0000-0000-0000-000000000001");

        var example = store.AddExample(
            petId,
            "spriteAudit",
            "goose baby female blue sprite review",
            "goose sprite polish",
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
        store.AddExample(
            petId,
            "localDocs",
            "summarize docs and plans",
            "docs summary",
            DateTimeOffset.Parse("2026-05-07T00:01:00Z"));

        var results = store.Search(petId, "review goose baby female blue sprites", "spriteAudit", topK: 3);

        Assert.NotEmpty(results);
        Assert.Equal(example.Id, results[0].Example.Id);
        Assert.Equal("spriteAudit", results[0].Example.Kind);
    }

    [Fact]
    public void EnsureReady_RenamesCorruptDatabaseAndStartsFresh()
    {
        var root = CreateTempRoot();
        var petId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var store = new PetMemoryStore(root);
        var path = store.ResolveDatabasePath(petId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not a sqlite database");

        var status = store.EnsureReady(petId, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        Assert.True(status.WasRebuilt);
        Assert.True(File.Exists(path));
        Assert.True(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.corrupt-*.db").Any());
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-pet-memory-tests", Guid.NewGuid().ToString("N"), "memory");
    }
}
