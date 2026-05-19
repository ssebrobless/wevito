using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Wevito.Tools.SnapshotVerify;

public static partial class Program
{
    private const string ExpectedSchemaVersion = "1";

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
        if (args.Length == 0 || !args[0].Equals("verify", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: verify --snapshot <path>");
            return 1;
        }

        var options = Parse(args.Skip(1).ToArray());
        if (!options.TryGetValue("snapshot", out var snapshotPath) || string.IsNullOrWhiteSpace(snapshotPath))
        {
            error.WriteLine("Snapshot path is required.");
            return 2;
        }

        var canonicalSnapshot = Path.GetFullPath(snapshotPath);
        if (!File.Exists(canonicalSnapshot))
        {
            error.WriteLine("Snapshot file does not exist.");
            return 2;
        }

        var text = File.ReadAllText(canonicalSnapshot, Encoding.UTF8);
        var snapshot = JsonSerializer.Deserialize<Snapshot>(text);
        if (snapshot is null)
        {
            error.WriteLine("Snapshot JSON could not be parsed.");
            return 3;
        }

        if (!snapshot.SchemaVersion.Equals(ExpectedSchemaVersion, StringComparison.Ordinal))
        {
            error.WriteLine($"Schema version mismatch. Expected {ExpectedSchemaVersion}.");
            return 3;
        }

        var embeddedHash = snapshot.SnapshotSha256;
        var unsignedSnapshot = snapshot with { SnapshotSha256 = "" };
        var unsigned = JsonSerializer.Serialize(unsignedSnapshot, new JsonSerializerOptions { WriteIndented = true });
        var computedHash = Sha256(unsigned);
        if (!computedHash.Equals(embeddedHash, StringComparison.Ordinal))
        {
            error.WriteLine("Signature mismatch.");
            return 4;
        }

        if (snapshot.RowCount != snapshot.Rows.Count)
        {
            error.WriteLine($"Row count mismatch. Expected {snapshot.RowCount}, found {snapshot.Rows.Count}.");
            return 5;
        }

        for (var index = 0; index < snapshot.Rows.Count; index++)
        {
            var row = snapshot.Rows[index];
            var invariantError = ValidateRow(row);
            if (!string.IsNullOrEmpty(invariantError))
            {
                error.WriteLine($"Invariant violation at row {index}: {invariantError}");
                return 6;
            }
        }

        for (var index = 0; index < snapshot.Rows.Count; index++)
        {
            var row = snapshot.Rows[index];
            output.WriteLine($"row-{index} packet_kind={row.PacketKind} status={row.Status} pass=true");
        }

        output.WriteLine($"verify ok rows={snapshot.Rows.Count} sha256={computedHash}");
        return 0;
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

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for argument: {key}");
            }

            options[key[2..]] = args[++index];
        }

        return options;
    }

    private static string ValidateRow(SnapshotRow row)
    {
        if (!row.PacketKind.StartsWith("self_improvement_", StringComparison.Ordinal))
        {
            return "packet_kind must start with self_improvement_.";
        }

        if (row.DidUseHostedAi)
        {
            return "did_use_hosted_ai must be false.";
        }

        if (row.DidUseNetwork)
        {
            return "did_use_network must be false.";
        }

        if (row.DidMutate)
        {
            return "did_mutate must be false.";
        }

        if (!row.PacketId.Equals("redacted", StringComparison.Ordinal))
        {
            return "packet_id must be redacted.";
        }

        if (!row.TaskCardId.Equals("redacted", StringComparison.Ordinal))
        {
            return "task_card_id must be redacted.";
        }

        if (!row.Summary.Equals("redacted", StringComparison.Ordinal))
        {
            return "summary must be redacted.";
        }

        if (!row.Error.Equals("redacted", StringComparison.Ordinal))
        {
            return "error must be redacted.";
        }

        if (!CreatedAtRowPattern().IsMatch(row.CreatedAtUtc))
        {
            return "created_at_utc must match row-<index>.";
        }

        return "";
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    [GeneratedRegex("^row-\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex CreatedAtRowPattern();

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
