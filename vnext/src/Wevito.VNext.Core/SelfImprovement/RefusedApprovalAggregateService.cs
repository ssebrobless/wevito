using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class RefusedApprovalAggregateService
{
    private readonly string _databasePath;
    private readonly KillSwitchService? _killSwitch;
    private readonly Action<string>? _commandObserver;

    public RefusedApprovalAggregateService(
        string databasePath,
        KillSwitchService? killSwitch = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _killSwitch = killSwitch;
        _commandObserver = commandObserver;
    }

    public RefusedApprovalAggregate Build()
    {
        if (_killSwitch?.IsActive() == true)
        {
            return RefusedApprovalAggregate.Blocked("kill_switch=true");
        }

        if (!File.Exists(_databasePath))
        {
            return new RefusedApprovalAggregate(
                0,
                new Dictionary<string, int>(StringComparer.Ordinal),
                new Dictionary<string, int>(StringComparer.Ordinal));
        }

        var reasons = ReadReasons();
        var known = new Dictionary<string, int>(StringComparer.Ordinal);
        var other = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var reason in reasons)
        {
            if (RefusedApprovalReasonCatalog.Known.Contains(reason))
            {
                known[reason] = known.GetValueOrDefault(reason) + 1;
            }
            else
            {
                var hashPrefix = HashPrefix(reason);
                other[hashPrefix] = other.GetValueOrDefault(hashPrefix) + 1;
            }
        }

        return new RefusedApprovalAggregate(
            reasons.Count,
            ToOrderedDictionary(known),
            ToOrderedDictionary(other));
    }

    public static string FormatReasonForDisplay(string reason)
    {
        return RefusedApprovalReasonCatalog.ToDisplayText(reason);
    }

    private IReadOnlyList<string> ReadReasons()
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT error
            FROM audit_ledger
            WHERE packet_kind = 'self_improvement_apply_refused'
            ORDER BY created_at_utc DESC, id DESC;
            """;
        _commandObserver?.Invoke(command.CommandText);

        var reasons = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var reason = Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? "";
            reasons.Add(string.IsNullOrWhiteSpace(reason) ? "unknown_empty_reason" : reason);
        }

        return reasons;
    }

    private static string HashPrefix(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..8];
    }

    private static IReadOnlyDictionary<string, int> ToOrderedDictionary(Dictionary<string, int> values)
    {
        var ordered = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ordered[pair.Key] = pair.Value;
        }

        return ordered;
    }
}
