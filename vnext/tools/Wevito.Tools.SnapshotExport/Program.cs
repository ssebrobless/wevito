using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Wevito.Tools.SnapshotExport;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args, Console.Out, Console.Error);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || !args[0].Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: export --db <path> --operation-id <id> --output <file> [--force]");
            return 1;
        }

        var options = Parse(args.Skip(1).ToArray());
        if (!options.TryGetValue("db", out var databasePath) || !File.Exists(databasePath))
        {
            error.WriteLine("Audit database file does not exist.");
            return 2;
        }

        if (!options.TryGetValue("operation-id", out var operationId) || string.IsNullOrWhiteSpace(operationId))
        {
            error.WriteLine("Operation id is required.");
            return 3;
        }

        if (!options.TryGetValue("output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            error.WriteLine("Output path is required.");
            return 1;
        }

        var force = options.ContainsKey("force");
        var canonicalOutput = Path.GetFullPath(outputPath);
        if (File.Exists(canonicalOutput) && !force)
        {
            error.WriteLine("Output already exists. Re-run with --force to overwrite.");
            return 4;
        }

        var rows = ReadRows(Path.GetFullPath(databasePath), operationId.Trim());
        var snapshot = new Snapshot(
            "1",
            "self_improvement_chain",
            operationId.Trim(),
            rows.Count,
            rows.Select((row, index) => Redact(row, index)).ToArray(),
            "");
        var unsigned = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        var hash = Sha256(unsigned);
        var signed = unsigned.Replace("\"snapshot_sha256\": \"\"", $"\"snapshot_sha256\": \"{hash}\"", StringComparison.Ordinal);

        Directory.CreateDirectory(Path.GetDirectoryName(canonicalOutput) ?? ".");
        File.WriteAllText(canonicalOutput, signed);
        output.WriteLine(Path.GetRelativePath(Environment.CurrentDirectory, canonicalOutput));
        return 0;
    }

    private static IReadOnlyList<SnapshotRow> ReadRows(string databasePath, string operationId)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, packet_id, packet_kind, task_card_id, created_at_utc,
                   did_use_network, did_use_hosted_ai, did_use_local_model,
                   did_mutate, artifact_path, summary, status, error
            FROM audit_ledger
            WHERE packet_kind LIKE 'self_improvement_%'
              AND (summary LIKE $opPattern
                   OR task_card_id IN (
                       SELECT task_card_id FROM audit_ledger
                        WHERE packet_kind LIKE 'self_improvement_%'
                          AND summary LIKE $opPattern
                          AND task_card_id <> ''
                   ))
            ORDER BY created_at_utc ASC, id ASC;
            """;
        command.Parameters.AddWithValue("$opPattern", $"%{operationId}%");

        var rows = new List<SnapshotRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new SnapshotRow(
                Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["packet_id"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["packet_kind"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? "",
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

    private static SnapshotRow Redact(SnapshotRow row, int index)
    {
        return row with
        {
            PacketId = "redacted",
            TaskCardId = "redacted",
            CreatedAtUtc = $"row-{index}",
            Summary = "redacted",
            Error = "redacted"
        };
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Invalid argument: {key}");
            }

            var option = key[2..];
            if (option.Equals("force", StringComparison.OrdinalIgnoreCase))
            {
                options[option] = bool.TrueString;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for argument: {key}");
            }

            options[option] = args[++index];
        }

        return options;
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private sealed record Snapshot(
        [property: JsonPropertyName("schemaVersion")]
        string SchemaVersion,
        [property: JsonPropertyName("scope")]
        string Scope,
        [property: JsonPropertyName("operation_id")]
        string OperationId,
        [property: JsonPropertyName("row_count")]
        int RowCount,
        [property: JsonPropertyName("rows")]
        IReadOnlyList<SnapshotRow> Rows,
        [property: JsonPropertyName("snapshot_sha256")]
        string SnapshotSha256);

    private sealed record SnapshotRow(
        [property: JsonPropertyName("id")]
        long Id,
        [property: JsonPropertyName("packet_id")]
        string PacketId,
        [property: JsonPropertyName("packet_kind")]
        string PacketKind,
        [property: JsonPropertyName("task_card_id")]
        string TaskCardId,
        [property: JsonPropertyName("created_at_utc")]
        string CreatedAtUtc,
        [property: JsonPropertyName("did_use_network")]
        bool DidUseNetwork,
        [property: JsonPropertyName("did_use_hosted_ai")]
        bool DidUseHostedAi,
        [property: JsonPropertyName("did_use_local_model")]
        bool DidUseLocalModel,
        [property: JsonPropertyName("did_mutate")]
        bool DidMutate,
        [property: JsonPropertyName("artifact_path")]
        string ArtifactPath,
        [property: JsonPropertyName("summary")]
        string Summary,
        [property: JsonPropertyName("status")]
        string Status,
        [property: JsonPropertyName("error")]
        string Error);
}
