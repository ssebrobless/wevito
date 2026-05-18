using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Core.Audit;

public sealed class EvidenceSummaryService
{
    public const string ExportedPacketKind = "evidence_dashboard_summary_exported";
    public const int DefaultMaxPackets = 100;
    public const int MaxAllowedPackets = 1000;

    private readonly string _databasePath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Action<string>? _commandObserver;

    public EvidenceSummaryService(
        string databasePath,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _commandObserver = commandObserver;
    }

    public EvidenceSummary GetSummary(EvidenceSummaryQuery query)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return EvidenceSummary.Blocked("kill_switch=true", NormalizeQuery(query));
        }

        var normalized = NormalizeQuery(query);
        if (!File.Exists(_databasePath))
        {
            return new EvidenceSummary(normalized, [], [], false, "audit ledger not found");
        }

        var rows = ReadRows(normalized);
        var knownKinds = PlainLanguageExplainer.KnownPacketKinds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var grouped = rows
            .Where(row => knownKinds.Contains(row.PacketKind))
            .GroupBy(row => row.PacketKind, StringComparer.OrdinalIgnoreCase)
            .Select(group => new EvidenceSummaryKindRow(
                group.Key,
                group.Count(),
                group.Max(row => row.CreatedAtUtc),
                group.Count(row => row.DidMutate),
                group.Count(row => row.DidUseNetwork),
                group.Count(row => row.DidUseHostedAi),
                group.Count(row => row.DidUseLocalModel),
                group.Count(row =>
                    row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ||
                    row.Status.Equals("Refused", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(row.Error))))
            .OrderByDescending(row => row.LastSeenUtc)
            .ThenBy(row => row.PacketKind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var unknownKinds = rows
            .Where(row => !knownKinds.Contains(row.PacketKind))
            .Select(row => row.PacketKind)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new EvidenceSummary(normalized, grouped, unknownKinds, false, "");
    }

    public EvidenceSummaryExportResult ExportSummary(EvidenceSummaryQuery query, string artifactRoot, DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new EvidenceSummaryExportResult(false, "", "kill_switch=true", EvidenceSummary.Blocked("kill_switch=true", NormalizeQuery(query)));
        }

        var root = Path.GetFullPath(artifactRoot);
        var expectedLeaf = Path.Combine("vnext", "artifacts", "c-phase-141-evidence-dashboard");
        if (!root.Replace('\\', '/').EndsWith(expectedLeaf.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
        {
            return new EvidenceSummaryExportResult(false, "", "export root must be vnext/artifacts/c-phase-141-evidence-dashboard", EvidenceSummary.Blocked("export root outside phase artifact folder", NormalizeQuery(query)));
        }

        Directory.CreateDirectory(root);
        var summary = GetSummary(query);
        if (summary.IsBlocked)
        {
            return new EvidenceSummaryExportResult(false, "", summary.StatusMessage, summary);
        }

        var fileName = $"{nowUtc:yyyyMMddTHHmmssZ}.json";
        var path = Path.GetFullPath(Path.Combine(root, fileName));
        var rootedPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new EvidenceSummaryExportResult(false, "", "resolved export path escaped artifact root", summary);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            ExportedPacketKind,
            TaskCardId: null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            ArtifactPath: path,
            Summary: $"Exported evidence dashboard summary with {summary.Rows.Count} known packet kind row(s).",
            Status: "Completed"));
        return new EvidenceSummaryExportResult(true, path, "", summary);
    }

    private IReadOnlyList<AuditLedgerRow> ReadRows(EvidenceSummaryQuery query)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, packet_id, packet_kind, task_card_id, created_at_utc,
                   did_use_network, did_use_hosted_ai, did_use_local_model,
                   did_mutate, artifact_path, summary, status, error
            FROM audit_ledger
            WHERE ($from_utc = '' OR created_at_utc >= $from_utc)
              AND ($to_utc = '' OR created_at_utc <= $to_utc)
            ORDER BY created_at_utc DESC, id DESC
            LIMIT $limit;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$from_utc", query.FromUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "");
        command.Parameters.AddWithValue("$to_utc", query.ToUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "");
        command.Parameters.AddWithValue("$limit", query.MaxPackets);

        var rows = new List<AuditLedgerRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var taskCard = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture);
            rows.Add(new AuditLedgerRow(
                Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
                Guid.Parse(Convert.ToString(reader["packet_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Convert.ToString(reader["packet_kind"], CultureInfo.InvariantCulture) ?? "",
                Guid.TryParse(taskCard, out var taskCardId) ? taskCardId : null,
                DateTimeOffset.Parse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                Convert.ToInt32(reader["did_use_network"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_use_hosted_ai"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_use_local_model"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_mutate"], CultureInfo.InvariantCulture) != 0,
                Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["status"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? ""));
        }

        return rows;
    }

    private static EvidenceSummaryQuery NormalizeQuery(EvidenceSummaryQuery query)
    {
        return query with
        {
            MaxPackets = Math.Clamp(query.MaxPackets <= 0 ? DefaultMaxPackets : query.MaxPackets, 1, MaxAllowedPackets)
        };
    }
}

public sealed record EvidenceSummaryQuery(DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, int MaxPackets = EvidenceSummaryService.DefaultMaxPackets);

public sealed record EvidenceSummary(
    EvidenceSummaryQuery Query,
    IReadOnlyList<EvidenceSummaryKindRow> Rows,
    IReadOnlyList<string> UnknownPacketKinds,
    bool IsBlocked,
    string StatusMessage)
{
    public static EvidenceSummary Blocked(string reason, EvidenceSummaryQuery query)
    {
        return new EvidenceSummary(query, [], [], true, reason);
    }
}

public sealed record EvidenceSummaryKindRow(
    string PacketKind,
    int Count,
    DateTimeOffset LastSeenUtc,
    int MutationYesCount,
    int NetworkYesCount,
    int HostedAiYesCount,
    int LocalModelYesCount,
    int RefusalCount);

public sealed record EvidenceSummaryExportResult(bool Exported, string Path, string BlockReason, EvidenceSummary Summary);
