using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wevito.VNext.Tests;

public sealed class SnapshotVerifyCliTests
{
    [Fact]
    public void Verify_ValidSnapshot_ReturnsPassAndRowLines()
    {
        var fixture = CreateFixture();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = InvokeCli(["verify", "--snapshot", fixture.SnapshotPath], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("row-0 packet_kind=self_improvement_proposal_drafted status=Completed pass=true", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("verify ok rows=2 sha256=", output.ToString(), StringComparison.Ordinal);
        Assert.Equal("", error.ToString());
    }

    [Fact]
    public void Verify_TamperedRows_ReturnsSignatureMismatch()
    {
        var fixture = CreateFixture();
        var text = File.ReadAllText(fixture.SnapshotPath);
        File.WriteAllText(fixture.SnapshotPath, text.Replace("Completed", "Tampered", StringComparison.Ordinal));

        var exitCode = InvokeCli(["verify", "--snapshot", fixture.SnapshotPath], new StringWriter(), new StringWriter());

        Assert.Equal(4, exitCode);
    }

    [Fact]
    public void Verify_RowCountMismatch_ReturnsExitCode5()
    {
        var fixture = CreateFixture();
        var snapshot = ReadSnapshot(fixture.SnapshotPath) with { RowCount = 99 };
        WriteSignedSnapshot(fixture.SnapshotPath, snapshot);

        var exitCode = InvokeCli(["verify", "--snapshot", fixture.SnapshotPath], new StringWriter(), new StringWriter());

        Assert.Equal(5, exitCode);
    }

    [Fact]
    public void Verify_NetworkFlagTrueWithValidSignature_ReturnsInvariantViolation()
    {
        var fixture = CreateFixture();
        var snapshot = ReadSnapshot(fixture.SnapshotPath);
        var rows = snapshot.Rows.ToArray();
        rows[0] = rows[0] with { DidUseNetwork = true };
        WriteSignedSnapshot(fixture.SnapshotPath, snapshot with { Rows = rows });

        var exitCode = InvokeCli(["verify", "--snapshot", fixture.SnapshotPath], new StringWriter(), new StringWriter());

        Assert.Equal(6, exitCode);
    }

    [Fact]
    public void Verify_SchemaVersionMismatch_ReturnsExitCode3()
    {
        var fixture = CreateFixture();
        var snapshot = ReadSnapshot(fixture.SnapshotPath) with { SchemaVersion = "2" };
        WriteSignedSnapshot(fixture.SnapshotPath, snapshot);

        var exitCode = InvokeCli(["verify", "--snapshot", fixture.SnapshotPath], new StringWriter(), new StringWriter());

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public void Verify_MissingFile_ReturnsExitCode2()
    {
        var fixture = CreateFixture();
        var missing = Path.Combine(fixture.Root, "missing.json");

        var exitCode = InvokeCli(["verify", "--snapshot", missing], new StringWriter(), new StringWriter());

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void Source_DoesNotReferenceSqliteOrNetworkSurfaces()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotVerify", "Program.cs"));
        var project = File.ReadAllText(Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotVerify", "Wevito.Tools.SnapshotVerify.csproj"));

        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SqliteConnection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net.Http", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net.Sockets", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Socket", source, StringComparison.Ordinal);
    }

    private static int InvokeCli(string[] args, TextWriter output, TextWriter error)
    {
        var programType = LoadCliAssembly().GetType("Wevito.Tools.SnapshotVerify.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, output, error])!;
    }

    private static Assembly LoadCliAssembly()
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.SnapshotVerify", "Wevito.Tools.SnapshotVerify.csproj"));
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", project, "--nologo" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        Assert.NotNull(process);
        process!.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.SnapshotVerify.dll");
        return Assembly.LoadFrom(assemblyPath);
    }

    private static CliFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-snapshot-verify-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var snapshotPath = Path.Combine(root, "snapshot.json");
        WriteSignedSnapshot(snapshotPath, new Snapshot(
            "1",
            "self_improvement_chain",
            "operation-verify-001",
            2,
            [
                new SnapshotRow(1, "redacted", "self_improvement_proposal_drafted", "redacted", "row-0", false, false, false, false, "", "redacted", "Completed", "redacted"),
                new SnapshotRow(2, "redacted", "self_improvement_eval_completed", "redacted", "row-1", false, false, false, false, "", "redacted", "Completed", "redacted")
            ],
            ""));
        return new CliFixture(root, snapshotPath);
    }

    private static Snapshot ReadSnapshot(string path)
    {
        return JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path))!;
    }

    private static void WriteSignedSnapshot(string path, Snapshot snapshot)
    {
        var unsigned = JsonSerializer.Serialize(snapshot with { SnapshotSha256 = "" }, new JsonSerializerOptions { WriteIndented = true });
        var signed = unsigned.Replace("\"snapshot_sha256\": \"\"", $"\"snapshot_sha256\": \"{Sha256(unsigned)}\"", StringComparison.Ordinal);
        File.WriteAllText(path, signed);
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed record CliFixture(string Root, string SnapshotPath);

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
