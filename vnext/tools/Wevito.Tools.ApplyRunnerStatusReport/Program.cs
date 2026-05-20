using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.Tools.ApplyRunnerStatusReport;

public static class Program
{
    private const string DatabaseEnvVar = "WEVITO_AUDIT_LEDGER_PATH";
    private const string EnabledEnvVar = "WEVITO_APPLY_RUNNER_STATUS_REPORT_ENABLED";

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
        if (args.Length == 0 || !args[0].Equals("report", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: report [--emit]");
            return 1;
        }

        var emit = args.Skip(1).Any(argument => argument.Equals("--emit", StringComparison.OrdinalIgnoreCase));
        var ledger = new AuditLedgerService(Environment.GetEnvironmentVariable(DatabaseEnvVar));
        var settings = ReadSettings();
        var killSwitch = new KillSwitchService(() => settings);
        var service = ShellCompositionRoot.CreateApplyRunnerStatusReportService(ledger, killSwitch, () => settings);
        if (!emit)
        {
            var latest = service.ReadLatest(ledger.DatabasePath, DateTimeOffset.UtcNow);
            if (latest is null)
            {
                error.WriteLine("No apply-runner status report is present.");
                return 4;
            }

            output.WriteLine(JsonSerializer.Serialize(latest, JsonDefaults.Options));
            return 0;
        }

        if (!IsTrue(settings, ApplyRunnerStatusReportService.EnabledSetting))
        {
            error.WriteLine($"{ApplyRunnerStatusReportService.EnabledSetting}=false; refusing to emit.");
            return 3;
        }

        var report = service.EmitReport(DateTimeOffset.UtcNow);
        output.WriteLine(JsonSerializer.Serialize(report, JsonDefaults.Options));
        return string.IsNullOrWhiteSpace(report.ReportId) ? 2 : 0;
    }

    private static Dictionary<string, string> ReadSettings()
    {
        var enabled = Environment.GetEnvironmentVariable(EnabledEnvVar);
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(enabled))
        {
            settings[ApplyRunnerStatusReportService.EnabledSetting] = enabled;
        }

        return settings;
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }
}
