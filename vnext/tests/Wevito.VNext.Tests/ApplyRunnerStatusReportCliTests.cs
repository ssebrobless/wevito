using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerStatusReportCliTests
{
    private const string DatabaseEnvVar = "WEVITO_AUDIT_LEDGER_PATH";
    private const string EnabledEnvVar = "WEVITO_APPLY_RUNNER_STATUS_REPORT_ENABLED";

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Report_WithoutEmitAndNoPacket_ReturnsExit4()
    {
        using var env = EnvScope.Create(databasePath: CreateDatabasePath(), enabled: null);

        var exitCode = InvokeCli(["report"], new StringWriter(), new StringWriter());

        Assert.Equal(4, exitCode);
    }

    [Fact]
    public void Report_WithoutEmitWithExistingPacket_ReturnsJson()
    {
        var databasePath = CreateDatabasePath();
        var ledger = new AuditLedgerService(databasePath);
        ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyRunnerStatusReport,
            null,
            Now,
            false,
            false,
            false,
            false,
            "",
            JsonSerializer.Serialize(new ApplyRunnerStatusReport("report-1", false, ["gate"], SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, Now), JsonDefaults.Options),
            "Completed"));
        using var env = EnvScope.Create(databasePath, enabled: null);
        var output = new StringWriter();

        var exitCode = InvokeCli(["report"], output, new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.Contains("applyRunnerImplemented", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("false", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Report_WithEmitAndFlagOff_ReturnsExit3()
    {
        using var env = EnvScope.Create(databasePath: CreateDatabasePath(), enabled: "false");

        var exitCode = InvokeCli(["report", "--emit"], new StringWriter(), new StringWriter());

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public void Report_WithEmitAndFlagOn_AppendsOnePacket()
    {
        var databasePath = CreateDatabasePath();
        using var env = EnvScope.Create(databasePath, enabled: "true");
        var output = new StringWriter();

        var exitCode = InvokeCli(["report", "--emit"], output, new StringWriter());
        var rows = new AuditLedgerService(databasePath).Snapshot(Now.AddDays(-10), DateTimeOffset.UtcNow.AddDays(1));

        Assert.Equal(0, exitCode);
        var row = Assert.Single(rows);
        Assert.Equal(SelfImprovementPacketKinds.ApplyRunnerStatusReport, row.PacketKind);
        Assert.Contains("applyRunnerImplemented", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("false", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Source_DoesNotReferenceSqliteDirectly()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "vnext", "tools", "Wevito.Tools.ApplyRunnerStatusReport", "Program.cs"));
        var project = File.ReadAllText(Path.Combine(root, "vnext", "tools", "Wevito.Tools.ApplyRunnerStatusReport", "Wevito.Tools.ApplyRunnerStatusReport.csproj"));

        Assert.DoesNotContain("Microsoft.Data.Sqlite", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SqliteConnection", source, StringComparison.Ordinal);
    }

    private static int InvokeCli(string[] args, TextWriter output, TextWriter error)
    {
        var programType = LoadCliAssembly().GetType("Wevito.Tools.ApplyRunnerStatusReport.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, output, error])!;
    }

    private static Assembly LoadCliAssembly()
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.ApplyRunnerStatusReport", "Wevito.Tools.ApplyRunnerStatusReport.csproj"));
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
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.ApplyRunnerStatusReport.dll");
        return Assembly.LoadFrom(assemblyPath);
    }

    private static string CreateDatabasePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-apply-runner-status-cli", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "ledger.sqlite");
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

    private sealed class EnvScope : IDisposable
    {
        private readonly string? _previousDatabase;
        private readonly string? _previousEnabled;

        private EnvScope(string databasePath, string? enabled)
        {
            _previousDatabase = Environment.GetEnvironmentVariable(DatabaseEnvVar);
            _previousEnabled = Environment.GetEnvironmentVariable(EnabledEnvVar);
            Environment.SetEnvironmentVariable(DatabaseEnvVar, databasePath);
            Environment.SetEnvironmentVariable(EnabledEnvVar, enabled);
        }

        public static EnvScope Create(string databasePath, string? enabled)
        {
            return new EnvScope(databasePath, enabled);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(DatabaseEnvVar, _previousDatabase);
            Environment.SetEnvironmentVariable(EnabledEnvVar, _previousEnabled);
        }
    }
}
