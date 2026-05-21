using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed class ApplyRunnerActivityService
{
    public const string ObserverEnabledSetting = "apply_v0_invariant_observer_in_activity_service_enabled";

    private const string PacketPrefix = "self_improvement_apply_v0_";

    private readonly string _databasePath;
    private readonly KillSwitchService _killSwitch;
    private readonly InvariantViolationWatchdog? _watchdog;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;

    public ApplyRunnerActivityService(
        string databasePath,
        KillSwitchService killSwitch,
        InvariantViolationWatchdog? watchdog = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _killSwitch = killSwitch;
        _watchdog = watchdog;
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ApplyRunnerActivityEntry> ReadRecent(int maxEntries)
    {
        if (_killSwitch.IsActive() || maxEntries <= 0 || !File.Exists(_databasePath))
        {
            return [];
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT packet_kind, created_at_utc, did_mutate, summary
            FROM audit_ledger
            WHERE packet_kind LIKE $prefix
            ORDER BY created_at_utc DESC, id DESC;
            """;
        command.Parameters.AddWithValue("$prefix", PacketPrefix + "%");

        var rows = new List<Row>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var packetKind = Convert.ToString(reader["packet_kind"], CultureInfo.InvariantCulture) ?? "";
            var timestamp = DateTimeOffset.Parse(
                Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
            var didMutate = Convert.ToInt32(reader["did_mutate"], CultureInfo.InvariantCulture) != 0;
            var summaryText = Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "";
            var summary = ParseSummary(summaryText);
            rows.Add(new Row(packetKind, timestamp, didMutate, summary));
        }

        var entries = rows
            .GroupBy(row => GetValue(row.Summary, "operation_id", "operationId"), StringComparer.Ordinal)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => BuildEntry(group.Key, group))
            .OrderByDescending(entry => entry.Packets.Count == 0 ? DateTimeOffset.MinValue : entry.Packets[^1].Timestamp)
            .Take(maxEntries)
            .ToList();

        if (_watchdog is not null && IsTrue(_settingsProvider(), ObserverEnabledSetting))
        {
            _watchdog.ScanAndEmit(DateTimeOffset.UtcNow);
        }

        return entries;
    }

    private static ApplyRunnerActivityEntry BuildEntry(string operationId, IEnumerable<Row> group)
    {
        var orderedRows = group.OrderBy(row => row.Timestamp).ToArray();
        var packets = orderedRows
            .Select(row => new ApplyRunnerActivityPacket(row.PacketKind, row.Timestamp, row.Summary, row.DidMutate))
            .ToList();
        var merged = orderedRows
            .SelectMany(row => row.Summary)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Last().Value, StringComparer.OrdinalIgnoreCase);
        var lastKind = packets.Count == 0 ? "" : packets[^1].PacketKind;
        var disposition = lastKind switch
        {
            SelfImprovementPacketKinds.ApplyV0Completed => ApplyRunnerActivityDisposition.Succeeded,
            SelfImprovementPacketKinds.ApplyV0RolledBack or SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted => ApplyRunnerActivityDisposition.RolledBack,
            _ => ApplyRunnerActivityDisposition.InProgress
        };

        return new ApplyRunnerActivityEntry(
            operationId,
            GetValue(merged, "scope_id", "scopeId"),
            GetValue(merged, "scope_hash", "scopeHash"),
            packets,
            disposition);
    }

    private static IReadOnlyDictionary<string, string> ParseSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (TryParseJson(summary, out var values))
        {
            return values;
        }

        var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in summary.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = token.IndexOf('=', StringComparison.Ordinal);
            if (index <= 0 || index >= token.Length - 1)
            {
                continue;
            }

            parsed[token[..index]] = token[(index + 1)..];
        }

        return parsed;
    }

    private static bool TryParseJson(string summary, out IReadOnlyDictionary<string, string> values)
    {
        values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var document = JsonDocument.Parse(summary);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                parsed[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? "",
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    JsonValueKind.Number => property.Value.ToString(),
                    _ => property.Value.GetRawText()
                };
            }

            values = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return "";
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }

    private sealed record Row(
        string PacketKind,
        DateTimeOffset Timestamp,
        bool DidMutate,
        IReadOnlyDictionary<string, string> Summary);
}
