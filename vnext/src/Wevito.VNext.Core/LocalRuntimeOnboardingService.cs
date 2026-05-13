namespace Wevito.VNext.Core;

public sealed record LocalRuntimeOnboardingStatus(
    bool Installed,
    bool ModelPresent,
    bool EndpointReachable,
    DateTimeOffset? LastProbeAtUtc,
    string Endpoint,
    string Model,
    string Reason);

public sealed record LocalRuntimeInstallStep(
    int Order,
    string Title,
    string Command,
    bool RequiresInteractiveApproval);

public sealed class LocalRuntimeOnboardingService
{
    private readonly LocalRuntimeProbeService _probeService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly Func<string, bool> _commandExists;
    private readonly Func<string, string, bool> _modelExists;

    public LocalRuntimeOnboardingService(
        LocalRuntimeProbeService? probeService = null,
        AuditLedgerService? auditLedgerService = null,
        Func<string, bool>? commandExists = null,
        Func<string, string, bool>? modelExists = null)
    {
        _probeService = probeService ?? new LocalRuntimeProbeService();
        _auditLedgerService = auditLedgerService;
        _commandExists = commandExists ?? (_ => false);
        _modelExists = modelExists ?? ((_, _) => false);
    }

    public async Task<LocalRuntimeOnboardingStatus> StatusAsync(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus = null,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var endpoint = Read(settings, LocalRuntimeProbeService.OllamaEndpointSetting, LocalRuntimeProbeService.DefaultOllamaEndpoint);
        var model = Read(settings, LocalRuntimeProbeService.OllamaModelSetting, LocalRuntimeProbeService.DefaultOllamaModel);
        var installed = _commandExists("ollama");
        var modelPresent = installed && _modelExists("ollama", model);
        var probe = await _probeService.ProbeAsync(settings, runtimeStatus, timestamp, cancellationToken).ConfigureAwait(false);
        var status = new LocalRuntimeOnboardingStatus(installed, modelPresent, probe.IsAvailable, probe.ProbedAtUtc, endpoint, model, probe.Reason);
        Record(status, timestamp);
        return status;
    }

    public IReadOnlyList<LocalRuntimeInstallStep> InstallPlan(IReadOnlyDictionary<string, string>? settings)
    {
        var model = Read(settings, LocalRuntimeProbeService.OllamaModelSetting, LocalRuntimeProbeService.DefaultOllamaModel);
        return
        [
            new LocalRuntimeInstallStep(1, "Check for Ollama", "winget list Ollama.Ollama --exact; Get-Command ollama -ErrorAction SilentlyContinue", RequiresInteractiveApproval: false),
            new LocalRuntimeInstallStep(2, "Install Ollama if missing", "winget install Ollama.Ollama", RequiresInteractiveApproval: true),
            new LocalRuntimeInstallStep(3, "Pull local model", $"ollama pull {model}", RequiresInteractiveApproval: true),
            new LocalRuntimeInstallStep(4, "Probe localhost runtime", "Invoke-RestMethod http://127.0.0.1:11434/api/tags", RequiresInteractiveApproval: false)
        ];
    }

    public string FormatStatus(LocalRuntimeOnboardingStatus status)
    {
        var installed = status.Installed ? "installed" : "not installed";
        var model = status.ModelPresent ? "model present" : "model missing or not checked";
        var endpoint = status.EndpointReachable ? "endpoint reachable" : "endpoint not reachable";
        return $"Local runtime: {installed}; {model}; {endpoint}; model={status.Model}; endpoint={status.Endpoint}; lastProbe={status.LastProbeAtUtc:O}; reason={status.Reason}";
    }

    private void Record(LocalRuntimeOnboardingStatus status, DateTimeOffset timestamp)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            "runtime_onboarding",
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: FormatStatus(status),
            Status: status.EndpointReachable ? "Completed" : "PreviewReady"));
    }

    private static string Read(IReadOnlyDictionary<string, string>? settings, string key, string defaultValue)
    {
        return settings is not null &&
               settings.TryGetValue(key, out var raw) &&
               !string.IsNullOrWhiteSpace(raw)
            ? raw
            : defaultValue;
    }
}
