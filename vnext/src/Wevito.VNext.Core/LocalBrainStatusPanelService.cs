namespace Wevito.VNext.Core;

public sealed record LocalBrainObservedSnapshot(
    LocalBrainStatus Status,
    DateTimeOffset? LastProbeAtUtc,
    DateTimeOffset? LastPacketAtUtc,
    string Summary);

public sealed record LocalBrainSetupCommand(
    string Id,
    string Label,
    string Command,
    string Description);

public sealed record LocalBrainStatusPanelSnapshot(
    LocalBrainStatus Status,
    LocalRuntimeProbeResult? LastProbe,
    string StateLabel,
    string Summary,
    string RecommendedEndpoint,
    string RecommendedModel,
    string KeepAliveGuidance,
    string FallbackInstallerNote,
    IReadOnlyList<LocalBrainSetupCommand> SetupCommands,
    bool RefreshRateLimited,
    string RefreshMessage);

public sealed record LocalBrainCopyCommandResult(
    bool Success,
    string CommandId,
    string Command,
    string Message);

public sealed class LocalBrainStatusPanelService
{
    public const string StatusPanelShownPacketKind = "local_brain_status_panel_shown";
    public const string SetupInstructionCopiedPacketKind = "local_brain_setup_instruction_copied";
    public const string RecommendedEndpoint = LocalRuntimeProbeService.DefaultOllamaEndpoint;
    public const string RecommendedModel = "qwen3:8b";
    public static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(10);

    private static readonly IReadOnlyList<LocalBrainSetupCommand> Commands =
    [
        new("install-ollama", "Install Ollama with winget", "winget install -e --id Ollama.Ollama", "Installs the local Ollama runtime through Windows Package Manager."),
        new("serve-ollama", "Start Ollama server", "ollama serve", "Starts the local loopback Ollama server."),
        new("pull-qwen", "Pull qwen3:8b", "ollama pull qwen3:8b", "Downloads the recommended local reasoning model."),
        new("keep-alive", "Keep model warm for 30 minutes", "setx OLLAMA_KEEP_ALIVE 30m", "Asks Ollama to keep the model loaded longer than its default idle unload window.")
    ];

    private readonly LocalBrainHeartbeatService _heartbeatService;
    private readonly LocalRuntimeProbeService _probeService;
    private readonly AuditLedgerService _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private LocalRuntimeProbeResult? _lastPanelProbe;
    private DateTimeOffset? _lastRefreshAtUtc;

    public LocalBrainStatusPanelService(
        LocalBrainHeartbeatService heartbeatService,
        LocalRuntimeProbeService probeService,
        AuditLedgerService auditLedgerService,
        KillSwitchService? killSwitchService = null)
    {
        _heartbeatService = heartbeatService;
        _probeService = probeService;
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public static IReadOnlyList<LocalBrainSetupCommand> SetupCommands => Commands;

    public LocalBrainStatusPanelSnapshot Show(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus,
        DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() != true && !KillSwitchService.IsActive(settings))
        {
            Record(StatusPanelShownPacketKind, nowUtc, "Showed the local brain status panel.", "Shown");
        }

        return BuildSnapshot(settings, runtimeStatus, nowUtc, refreshRateLimited: false, refreshMessage: "Panel opened from the BRAIN badge.");
    }

    public async Task<LocalBrainStatusPanelSnapshot> RefreshAsync(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
        {
            return BuildSnapshot(settings, runtimeStatus, nowUtc, refreshRateLimited: false, refreshMessage: "Refresh blocked because Stop Everything is active.");
        }

        if (_lastRefreshAtUtc is not null && nowUtc - _lastRefreshAtUtc.Value < RefreshCooldown)
        {
            return BuildSnapshot(settings, runtimeStatus, nowUtc, refreshRateLimited: true, refreshMessage: "Refresh is rate-limited. Try again in a few seconds.");
        }

        _lastRefreshAtUtc = nowUtc;
        _lastPanelProbe = await _probeService.ProbeAsync(settings, runtimeStatus, nowUtc, cancellationToken).ConfigureAwait(false);
        return BuildSnapshot(settings, runtimeStatus, nowUtc, refreshRateLimited: false, refreshMessage: "Loopback status refreshed.");
    }

    public LocalBrainCopyCommandResult CopyCommand(string commandId, DateTimeOffset nowUtc)
    {
        var command = Commands.FirstOrDefault(candidate => string.Equals(candidate.Id, commandId, StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            return new LocalBrainCopyCommandResult(false, commandId, "", "Unknown setup command.");
        }

        if (_killSwitchService?.IsActive() == true)
        {
            return new LocalBrainCopyCommandResult(false, command.Id, command.Command, "Copy blocked because Stop Everything is active.");
        }

        Record(
            SetupInstructionCopiedPacketKind,
            nowUtc,
            $"Copied local brain setup instruction id={command.Id}.",
            "Copied");
        return new LocalBrainCopyCommandResult(true, command.Id, command.Command, $"Copied {command.Label}.");
    }

    private LocalBrainStatusPanelSnapshot BuildSnapshot(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus,
        DateTimeOffset nowUtc,
        bool refreshRateLimited,
        string refreshMessage)
    {
        var observed = _heartbeatService.LastObserved;
        var status = _lastPanelProbe is null
            ? observed.Status
            : FromProbe(_lastPanelProbe, nowUtc);

        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
        {
            status = new LocalBrainStatus(
                LocalBrainAvailability.Blocked,
                "ollama",
                LocalRuntimeProbeService.DefaultOllamaEndpoint,
                LocalRuntimeProbeService.DefaultOllamaModel,
                "kill_switch=true",
                nowUtc,
                DidUseLocalModel: false);
        }
        else if (runtimeStatus?.Mode is RuntimeSupervisorMode.Quiet or RuntimeSupervisorMode.PetOnly)
        {
            status = new LocalBrainStatus(
                LocalBrainAvailability.Dormant,
                "ollama",
                LocalRuntimeProbeService.DefaultOllamaEndpoint,
                LocalRuntimeProbeService.DefaultOllamaModel,
                runtimeStatus.UserStatus,
                nowUtc,
                DidUseLocalModel: false);
        }

        return new LocalBrainStatusPanelSnapshot(
            status,
            _lastPanelProbe,
            StateLabel(status),
            LocalBrainHeartbeatService.BuildSummary(status),
            RecommendedEndpoint,
            RecommendedModel,
            "Ollama unloads models after about 5 minutes by default. Use OLLAMA_KEEP_ALIVE=30m for a warmer local brain, or -1 if you intentionally want it always loaded.",
            "If winget is unavailable, download the installer from https://ollama.com/download/windows.",
            Commands,
            refreshRateLimited,
            refreshMessage);
    }

    private static LocalBrainStatus FromProbe(LocalRuntimeProbeResult probe, DateTimeOffset nowUtc)
    {
        return LocalBrainHeartbeatService.FromProbe(probe);
    }

    private static string StateLabel(LocalBrainStatus status)
    {
        return status.Availability switch
        {
            LocalBrainAvailability.Ready => "ready",
            LocalBrainAvailability.Dormant => "quiet",
            LocalBrainAvailability.Blocked => "paused",
            LocalBrainAvailability.Offline => "offline",
            _ => "starting"
        };
    }

    private void Record(string packetKind, DateTimeOffset timestamp, string summary, string status)
    {
        _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            TaskCardId: null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            summary,
            status));
    }
}
