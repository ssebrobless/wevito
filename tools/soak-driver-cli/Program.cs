using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";
var options = ParseOptions(args.Skip(1).ToArray());
var artifactRoot = FullPath(options.TryGetValue("artifact-root", out var root) ? root : Path.Combine("vnext", "artifacts", "soak"));
var settings = LoadSettings(options);
var ledger = new AuditLedgerService();
var killSwitch = new KillSwitchService(() => settings);
var service = new SoakDriverCommandService(ledger, artifactRoot, killSwitchService: killSwitch);

try
{
    return command switch
    {
        "heartbeat" => PrintResult(service.Heartbeat(Option(options, "reason", "scheduled"), settings)),
        "day-end" => PrintResult(service.DayEnd()),
        "window-end" => PrintResult(service.WindowEnd(Option(options, "reason", "completed"))),
        "promotion-snapshot" => PrintPromotionSnapshot(options, settings, ledger),
        "status" => PrintStatus(service.Status(settings), settings),
        _ => Fail($"Unknown command '{command}'.")
    };
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static int PrintResult(SoakDriverCommandResult result)
{
    if (!result.Succeeded)
    {
        Console.Error.WriteLine(result.Message);
        return 1;
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ok = true,
        message = result.Message,
        artifactPath = result.ArtifactPath
    }, JsonDefaults.Options));
    return 0;
}

static int PrintStatus(SoakDriverCommandResult result, IReadOnlyDictionary<string, string> settings)
{
    if (!result.Succeeded || result.Status is null)
    {
        Console.Error.WriteLine(result.Message);
        return 1;
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        status = result.Status,
        settingsSnapshot = settings,
        settingsSnapshotSha256 = EvidenceCollectionStatusService.ComputeSettingsHash(settings)
    }, JsonDefaults.Options));
    return 0;
}

static int PrintPromotionSnapshot(
    IReadOnlyDictionary<string, string> options,
    IReadOnlyDictionary<string, string> settings,
    AuditLedgerService ledger)
{
    var now = DateTimeOffset.UtcNow;
    var window = int.TryParse(Option(options, "window", "7"), out var parsedWindow) ? parsedWindow : 7;
    var promotionRoot = FullPath(Option(options, "promotion-root", Path.Combine("vnext", "artifacts", "promotion")));
    var timestamp = now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
    var folder = Path.Combine(promotionRoot, $"{timestamp}-promotion");
    var goldenEvalPath = LatestFile(Path.Combine(FindRepoRoot(), "vnext", "artifacts", "eval-golden"), "eval-report.json");
    var snapshot = new PromotionCriteriaSnapshot(ledger);
    var decision = snapshot.Compute(new PromotionCriteriaSnapshotRequest(
        now.AddDays(-window),
        now,
        settings,
        GoldenEvalReportPath: goldenEvalPath,
        EmitAuditRow: true));
    PromotionCriteriaSnapshot.WriteArtifacts(folder, decision);
    ledger.Record(new EvidencePacket(
        Guid.NewGuid(),
        PromotionCriteriaSnapshot.DecisionPacketKind,
        TaskCardId: null,
        now,
        DidUseNetwork: false,
        DidUseHostedAi: false,
        DidUseLocalModel: false,
        DidMutate: false,
        ArtifactPath: Path.Combine(folder, "decision.json"),
        Summary: decision.Summary,
        Status: "Completed"));
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ok = decision.Label == PromotionDecisionLabel.EnableAutonomousBeta,
        decision = decision.Label.ToString(),
        artifactPath = folder,
        summary = decision.Summary
    }, JsonDefaults.Options));
    return decision.Label == PromotionDecisionLabel.EnableAutonomousBeta ? 0 : 1;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static Dictionary<string, string> LoadSettings(IReadOnlyDictionary<string, string> options)
{
    var settings = new Dictionary<string, string>(SoakDriverCommandService.BuildDefaultSettingsSnapshot(), StringComparer.OrdinalIgnoreCase);
    if (options.TryGetValue("settings-json", out var path) && File.Exists(path))
    {
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        if (loaded is not null)
        {
            foreach (var pair in loaded)
            {
                settings[pair.Key] = pair.Value;
            }
        }
    }

    return settings;
}

static Dictionary<string, string> ParseOptions(string[] values)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        var raw = values[i];
        if (!raw.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = raw[2..];
        var value = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++i]
            : bool.TrueString;
        options[key] = value;
    }

    return options;
}

static string Option(IReadOnlyDictionary<string, string> options, string key, string fallback)
{
    return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}

static string FullPath(string path)
{
    return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(FindRepoRoot(), path));
}

static string FindRepoRoot()
{
    var current = AppContext.BaseDirectory;
    while (!string.IsNullOrWhiteSpace(current))
    {
        if (Directory.Exists(Path.Combine(current, ".git")) ||
            File.Exists(Path.Combine(current, "vnext", "Wevito.VNext.sln")))
        {
            return current;
        }

        current = Directory.GetParent(current)?.FullName ?? "";
    }

    return Directory.GetCurrentDirectory();
}

static string? LatestFile(string root, string fileName)
{
    return Directory.Exists(root)
        ? Directory.GetFiles(root, fileName, SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault()
        : null;
}
