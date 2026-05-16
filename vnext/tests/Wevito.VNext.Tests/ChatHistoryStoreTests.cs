using Microsoft.Data.Sqlite;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ChatHistoryStoreTests
{
    [Fact]
    public void AppendIsAppendOnly()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("Append test", DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
        var turn = new ChatTurn(session, Guid.NewGuid(), "user", "hello snake", null, null, DateTimeOffset.Parse("2026-05-15T12:01:00Z"), "", 2);

        store.AppendTurn(turn);
        store.AssertAppendOnlyGuards();

        using var connection = new SqliteConnection($"Data Source={store.DatabasePath}");
        connection.Open();
        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE chat_turns SET content='mutated';";
        Assert.Throws<SqliteException>(() => update.ExecuteNonQuery());
        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM chat_turns;";
        Assert.Throws<SqliteException>(() => delete.ExecuteNonQuery());
    }

    [Fact]
    public void FtsFindsTurnsByContent()
    {
        var store = new ChatHistoryStore(CreateDatabasePath());
        var session = store.CreateSession("Search test", DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
        store.AppendTurn(new ChatTurn(session, Guid.NewGuid(), "user", "please audit the goose sprites", null, null, DateTimeOffset.Parse("2026-05-15T12:01:00Z"), "", 5));

        var hits = store.SearchTurns("goose", 10);

        var hit = Assert.Single(hits);
        Assert.Equal(session, hit.SessionId);
        Assert.Contains("goose", hit.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = "true"
        };
        var store = new ChatHistoryStore(CreateDatabasePath(), new KillSwitchService(() => settings));

        Assert.Throws<InvalidOperationException>(() => store.CreateSession("blocked"));
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-chat-tests", Guid.NewGuid().ToString("N"), "chat.sqlite");
    }
}
