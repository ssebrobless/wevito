using System.Diagnostics;
using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed class OllamaModelBootstrapService
{
    public const string BootstrapRequiredPacketKind = "model_bootstrap_required";
    public const string RuntimeAbsentPacketKind = "model_bootstrap_runtime_absent";
    public const string PullInstruction = "Run ollama pull qwen2.5:7b-instruct-q4_K_M to enable chat";

    private readonly LocalRuntimeProbeService _probeService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<string, string, bool> _modelExists;
    private readonly string _artifactRoot;
    private OllamaModelBootstrapStatus? _startupStatus;

    public OllamaModelBootstrapService(
        LocalRuntimeProbeService? probeService = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<string, string, bool>? modelExists = null,
        string? artifactRoot = null)
    {
        _probeService = probeService ?? new LocalRuntimeProbeService(killSwitchService: killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _modelExists = modelExists ?? OllamaModelExists;
        _artifactRoot = Path.GetFullPath(artifactRoot ?? Path.Combine(Environment.CurrentDirectory, "vnext", "artifacts", "model-bootstrap"));
    }

    public async Task<OllamaModelBootstrapStatus> ProbeStartupAsync(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus = null,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new OllamaModelBootstrapStatus("kill_switch", false, false, false, true, "", "Ollama model bootstrap skipped because Stop Everything is active.");
        }

        if (_startupStatus is not null)
        {
            return _startupStatus;
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var probe = await _probeService.ProbeAsync(settings, runtimeStatus, timestamp, cancellationToken).ConfigureAwait(false);
        if (!probe.IsAvailable)
        {
            _startupStatus = RecordPacket(
                RuntimeAbsentPacketKind,
                timestamp,
                probe.Endpoint,
                probe.Model,
                "Unavailable",
                "Install Ollama from https://ollama.com/download, then run tools/pull-default-model.ps1.",
                "Ollama is not reachable on localhost; deterministic local fallback remains active.",
                probe);
            return _startupStatus;
        }

        var modelPresent = _modelExists("ollama", probe.Model);
        if (!modelPresent)
        {
            _startupStatus = RecordPacket(
                BootstrapRequiredPacketKind,
                timestamp,
                probe.Endpoint,
                probe.Model,
                "Bootstrap required",
                PullInstruction,
                "Ollama responded, but the configured reasoning model is missing.",
                probe);
            return _startupStatus;
        }

        _startupStatus = new OllamaModelBootstrapStatus("model_bootstrap_available", true, true, true, false, "", $"Ollama runtime is available and model {probe.Model} is present.");
        return _startupStatus;
    }

    private OllamaModelBootstrapStatus RecordPacket(
        string packetKind,
        DateTimeOffset timestamp,
        string endpoint,
        string model,
        string runtimeStatus,
        string instruction,
        string summary,
        LocalRuntimeProbeResult probe)
    {
        var packet = new OllamaModelBootstrapEvidencePacket(
            packetKind,
            timestamp,
            endpoint,
            model,
            runtimeStatus,
            instruction,
            "https://ollama.com/download",
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            DecisionId: "Q15-L2");
        var artifactPath = WritePacket(packet, timestamp);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            timestamp,
            DidUseNetwork: packet.DidUseNetwork,
            DidUseHostedAi: packet.DidUseHostedAi,
            DidUseLocalModel: packet.DidUseLocalModel,
            DidMutate: packet.DidMutate,
            artifactPath,
            $"{summary} {instruction}",
            "Blocked",
            probe.Reason));
        return new OllamaModelBootstrapStatus(packetKind, true, probe.IsAvailable, false, false, artifactPath, summary);
    }

    private string WritePacket(OllamaModelBootstrapEvidencePacket packet, DateTimeOffset timestamp)
    {
        var folderName = timestamp.ToUniversalTime().ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var folder = Path.Combine(_artifactRoot, folderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "packet.json");
        File.WriteAllText(path, JsonSerializer.Serialize(packet, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static bool OllamaModelExists(string command, string model)
    {
        if (!string.Equals(command, "ollama", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(2000) || process.ExitCode != 0)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            return output.Contains(model, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
