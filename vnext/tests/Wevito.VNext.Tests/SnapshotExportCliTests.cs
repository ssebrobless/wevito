using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Tests;

public sealed class SnapshotExportCliTests
{
    [Fact]
    public void Export_OperationChain_WritesDeterministicRedactedSignedSnapshot()
    {
        var fixture = CreateFixture();
        var taskCardId = Guid.NewGuid();
        var operationId = "operation-snapshot-001";
        var ledger = new AuditLedgerService(fixture.DatabasePath);
        Record(ledger, SelfImprovementPacketKinds.ProposalDrafted, taskCardId, fixture.Start, $"secret proposal for {operationId}", "secret error 1");
        Record(ledger, SelfImprovementPacketKinds.DryRunCompleted, taskCardId, fixture.Start.AddSeconds(1), "secret dry run", "secret error 2");
        Record(ledger, SelfImprovementPacketKinds.EvalCompleted, taskCardId, fixture.Start.AddSeconds(2), "secret eval", "");
        Record(ledger, SelfImprovementPacketKinds.ApplyRefused, Guid.NewGuid(), fixture.Start.AddSeconds(3), "other operation", "other error");

        var outputA = Path.Combine(fixture.Root, "snapshot-a.json");
        var outputB = Path.Combine(fixture.Root, "snapshot-b.json");

        Assert.Equal(0, InvokeCli(["export", "--db", fixture.DatabasePath, "--operation-id", operationId, "--output", outputA], new StringWriter(), new StringWriter()));
        Assert.Equal(0, InvokeCli(["export", "--db", fixture.DatabasePath, "--operation-id", operationId, "--output", outputB], new StringWriter(), new StringWriter()));

        var textA = File.ReadAllText(outputA);
        var textB = File.ReadAllText(outputB);
        Assert.Equal(textA, textB);
        using var document = JsonDocument.Parse(textA);
        var root = document.RootElement;
        Assert.Equal("1", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("self_improvement_chain", root.GetProperty("scope").GetString());
        Assert.Equal(operationId, root.GetProperty("operation_id").GetString());
        Assert.Equal(3, root.GetProperty("row_count").GetInt32());

        var rows = root.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(SelfImprovementPacketKinds.ProposalDrafted, rows[0].GetProperty("packet_kind").GetString());
        Assert.Equal(SelfImprovementPacketKinds.DryRunCompleted, rows[1].GetProperty("packet_kind").GetString());
        Assert.Equal(SelfImprovementPacketKinds.EvalCompleted, rows[2].GetProperty("packet_kind").GetString());
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.Equal("redacted", rows[index].GetProperty("packet_id").GetString());
            Assert.Equal("redacted", rows[index].GetProperty("task_card_id").GetString());
            Assert.Equal($"row-{index}", rows[index].GetProperty("created_at_utc").GetString());
            Assert.Equal("redacted", rows[index].GetProperty("summary").GetString());
            Assert.Equal("redacted", rows[index].GetProperty("error").GetString());
            Assert.Equal("Completed", rows[index].GetProperty("status").GetString());
        }

        var signature = root.GetProperty("snapshot_sha256").GetString();
        Assert.False(string.IsNullOrWhiteSpace(signature));
        var unsigned = textA.Replace($"\"snapshot_sha256\": \"{signature}\"", "\"snapshot_sha256\": \"\"", StringComparison.Ordinal);
        Assert.Equal(Sha256(Encoding.UTF8.GetBytes(unsigned)), signature);
        Assert.DoesNotContain("secret", textA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(taskCardId.ToString(), textA, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_MissingDatabase_ReturnsExitCode2()
    {
        var fixture = CreateFixture();
        var exitCode = InvokeCli([
            "export",
            "--db", Path.Combine(fixture.Root, "missing.sqlite"),
            "--operation-id", "operation",
            "--output", Path.Combine(fixture.Root, "snapshot.json")
        ], new StringWriter(), new StringWriter());

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void Export_EmptyOperationId_ReturnsExitCode3()
    {
        var fixture = CreateFixture();
        new AuditLedgerService(fixture.DatabasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ProposalDrafted,
            Guid.NewGuid(),
            fixture.Start,
            false,
            false,
            false,
            false,
            "",
            "seed",
            "Completed"));
        var exitCode = InvokeCli([
            "export",
            "--db", fixture.DatabasePath,
            "--operation-id", " ",
            "--output", Path.Combine(fixture.Root, "snapshot.json")
        ], new StringWriter(), new StringWriter());

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public void Export_OutputExistsWithoutForce_ReturnsExitCode4AndDoesNotOverwrite()
    {
        var fixture = CreateFixture();
        var output = Path.Combine(fixture.Root, "snapshot.json");
        File.WriteAllText(output, "keep me");
        new AuditLedgerService(fixture.DatabasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ProposalDrafted,
            Guid.NewGuid(),
            fixture.Start,
            false,
            false,
            false,
            false,
            "",
            "operation-output-exists",
            "Completed"));

        var exitCode = InvokeCli([
            "export",
            "--db", fixture.DatabasePath,
            "--operation-id", "operation-output-exists",
            "--output", output
        ], new StringWriter(), new StringWriter());

        Assert.Equal(4, exitCode);
        Assert.Equal("keep me", File.ReadAllText(output));
    }

    [Fact]
    public void Source_UsesReadOnlyDatabaseAndNoHeldOutOrNetworkSurfaces()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.SnapshotExport", "Program.cs"));

        Assert.Contains("Mode = SqliteOpenMode.ReadOnly", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelfImprovementPacketKinds", source, StringComparison.Ordinal);
    }

    private static void Record(AuditLedgerService ledger, string packetKind, Guid taskCardId, DateTimeOffset createdAtUtc, string summary, string error)
    {
        ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            createdAtUtc,
            false,
            false,
            false,
            false,
            "",
            summary,
            "Completed",
            error));
    }

    private static int InvokeCli(string[] args, TextWriter output, TextWriter error)
    {
        var programType = LoadCliAssembly().GetType("Wevito.Tools.SnapshotExport.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, output, error])!;
    }

    private static Assembly LoadCliAssembly()
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.SnapshotExport", "Wevito.Tools.SnapshotExport.csproj"));
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
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.SnapshotExport.dll");
        return Assembly.LoadFrom(assemblyPath);
    }

    private static CliFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-snapshot-export-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new CliFixture(root, Path.Combine(root, "ledger.sqlite"), new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero));
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
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

    private sealed record CliFixture(string Root, string DatabasePath, DateTimeOffset Start);
}
