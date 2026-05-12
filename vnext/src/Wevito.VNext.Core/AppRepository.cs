using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AppRepository
{
    public const int CurrentSchemaVersion = 2;
    private readonly string _databasePath;

    public AppRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS app_state (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS basket_items (
                id TEXT PRIMARY KEY,
                url TEXT NOT NULL,
                label TEXT NOT NULL,
                source TEXT NOT NULL,
                captured_at_utc TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<CompanionState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var appStateCommand = connection.CreateCommand();
        appStateCommand.CommandText = "SELECT value FROM app_state WHERE key = 'companion_state';";
        var stateJson = (string?)await appStateCommand.ExecuteScalarAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return null;
        }

        var state = JsonSerializer.Deserialize<CompanionState>(stateJson, JsonDefaults.Options);
        if (state is null)
        {
            return null;
        }

        var basketCommand = connection.CreateCommand();
        basketCommand.CommandText = """
            SELECT id, url, label, source, captured_at_utc
            FROM basket_items
            ORDER BY captured_at_utc DESC;
            """;
        var basketItems = new List<BasketItem>();
        await using var reader = await basketCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            basketItems.Add(new BasketItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4))));
        }

        return MigrateCompanionState(state with { BasketItems = basketItems });
    }

    public async Task SaveAsync(CompanionState state, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var stateCommand = connection.CreateCommand();
        stateCommand.Transaction = transaction;
        stateCommand.CommandText = """
            INSERT INTO app_state (key, value)
            VALUES ('companion_state', $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        stateCommand.Parameters.AddWithValue("$value", JsonSerializer.Serialize(MigrateCompanionState(state) with { BasketItems = [] }, JsonDefaults.Options));
        await stateCommand.ExecuteNonQueryAsync(cancellationToken);

        var deleteBasket = connection.CreateCommand();
        deleteBasket.Transaction = transaction;
        deleteBasket.CommandText = "DELETE FROM basket_items;";
        await deleteBasket.ExecuteNonQueryAsync(cancellationToken);

        foreach (var item in state.BasketItems)
        {
            var basketCommand = connection.CreateCommand();
            basketCommand.Transaction = transaction;
            basketCommand.CommandText = """
                INSERT INTO basket_items (id, url, label, source, captured_at_utc)
                VALUES ($id, $url, $label, $source, $capturedAtUtc);
                """;
            basketCommand.Parameters.AddWithValue("$id", item.Id.ToString());
            basketCommand.Parameters.AddWithValue("$url", item.Url);
            basketCommand.Parameters.AddWithValue("$label", item.Label);
            basketCommand.Parameters.AddWithValue("$source", item.Source);
            basketCommand.Parameters.AddWithValue("$capturedAtUtc", item.CapturedAtUtc.UtcDateTime.ToString("O"));
            await basketCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private SqliteConnection OpenConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }

    private static CompanionState MigrateCompanionState(CompanionState state)
    {
        return state with
        {
            SchemaVersion = CurrentSchemaVersion,
            ActivePets = state.ActivePets ?? [],
            BasketItems = state.BasketItems ?? [],
            SettingsSnapshot = state.SettingsSnapshot ?? new Dictionary<string, string>(),
            TaskCards = state.TaskCards ?? [],
            ActiveTool = state.ActiveTool is null || string.IsNullOrWhiteSpace(state.ActiveTool.ToolId)
                ? new ToolSession("basket", state.ActiveTool?.IsOpen ?? false)
                : state.ActiveTool
        };
    }
}
