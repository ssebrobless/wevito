using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record SoakRunnerRequest(
    int Hours,
    string ArtifactRoot,
    IReadOnlyDictionary<string, string> SettingsSnapshot,
    RuntimeBudgetSnapshot BudgetSnapshot,
    FocusStealSnapshot FocusStealSnapshot,
    DateTimeOffset StartedAtUtc);

public sealed record SoakRunnerResult(
    bool Started,
    string ArtifactFolder,
    string SummaryPath,
    string BlockReason);

public sealed class SoakRunnerService
{
    public const int MinHours = 1;
    public const int MaxHours = 24;

    private readonly AuditLedgerService _auditLedgerService;
    private readonly KillSwitchService _killSwitchService;

    public SoakRunnerService(AuditLedgerService auditLedgerService, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService ?? new KillSwitchService();
    }

    public SoakRunnerResult StartExplicitPreview(SoakRunnerRequest request)
    {
        if (request.Hours is < MinHours or > MaxHours)
        {
            return new SoakRunnerResult(false, "", "", "Soak hours must be between 1 and 24.");
        }

        if (KillSwitchService.IsActive(request.SettingsSnapshot) || _killSwitchService.IsActive())
        {
            return new SoakRunnerResult(false, "", "", "KillSwitch blocks soak validation.");
        }

        if (AnyDefaultOffCapabilityEnabled(request.SettingsSnapshot, out var capability))
        {
            return new SoakRunnerResult(false, "", "", $"Soak runner refuses to enable default-off capability: {capability}.");
        }

        var slug = $"{request.StartedAtUtc:yyyyMMdd-HHmmss}-soak-preview";
        var folder = Path.Combine(Path.GetFullPath(request.ArtifactRoot), slug);
        Directory.CreateDirectory(folder);
        var summaryPath = Path.Combine(folder, "run-summary.json");
        var summary = new
        {
            request.Hours,
            request.StartedAtUtc,
            EndsAtUtc = request.StartedAtUtc.AddHours(request.Hours),
            request.BudgetSnapshot,
            request.FocusStealSnapshot,
            ScheduledPreviewKind = "local_research",
            MutatesSettings = false,
            EnablesDefaultOffCapabilities = false
        };
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

        _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            "soak_session_start",
            null,
            request.StartedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: summaryPath,
            Summary: "Started explicit soak validation preview. No settings were modified.",
            Status: "PreviewReady"));

        return new SoakRunnerResult(true, folder, summaryPath, "");
    }

    public void CompletePreview(string artifactFolder, RuntimeBudgetReservation budgetReservation, FocusStealSnapshot focusStealSnapshot, DateTimeOffset completedAtUtc)
    {
        Directory.CreateDirectory(artifactFolder);
        var summaryPath = Path.Combine(artifactFolder, "completion-summary.json");
        var summary = new
        {
            CompletedAtUtc = completedAtUtc,
            Budget = budgetReservation,
            FocusSteal = focusStealSnapshot
        };
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            "soak_session_end",
            null,
            completedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: summaryPath,
            Summary: "Completed soak validation preview.",
            Status: "Completed"));
    }

    private static bool AnyDefaultOffCapabilityEnabled(IReadOnlyDictionary<string, string> settings, out string capability)
    {
        foreach (var key in new[]
                 {
                     "pet_model_adapter_enabled",
                     "runtime_autonomous_beta_enabled",
                     AutonomousOperationsConfig.EnabledSetting,
                     AutonomousTaskScheduler.SchedulerPreviewDispatchApprovedSetting,
                     WebResearchConnector.WebSearchEnabledSetting,
                     ModelProviderModeService.HostedProviderApprovedSetting
                 })
        {
            if (settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var enabled) && enabled)
            {
                capability = key;
                return true;
            }
        }

        capability = "";
        return false;
    }
}
