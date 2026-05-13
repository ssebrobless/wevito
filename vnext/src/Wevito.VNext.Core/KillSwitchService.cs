using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class KillSwitchService
{
    public const string KillSwitchSetting = "runtime_kill_switch";

    private readonly Func<IReadOnlyDictionary<string, string>>? _settingsProvider;
    private readonly AuditLedgerService? _auditLedgerService;

    public KillSwitchService(
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        AuditLedgerService? auditLedgerService = null)
    {
        _settingsProvider = settingsProvider;
        _auditLedgerService = auditLedgerService;
    }

    public bool IsActive()
    {
        return IsActive(_settingsProvider?.Invoke());
    }

    public static bool IsActive(IReadOnlyDictionary<string, string>? settings)
    {
        return settings is not null &&
            settings.TryGetValue(KillSwitchSetting, out var raw) &&
            bool.TryParse(raw, out var active) &&
            active;
    }

    public TaskAdapterResult BlockTask(TaskAdapterRequest request, DateTimeOffset timestamp)
    {
        var result = new TaskAdapterResult(
            request.TaskCardId,
            request.PolicySnapshot.ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: "kill_switch=true",
            CompletedAtUtc: timestamp);
        Record("kill_switch", request.TaskCardId, timestamp, "Blocked helper task because kill_switch=true.", "Blocked");
        return result;
    }

    public void Record(string packetKind, Guid? taskCardId, DateTimeOffset timestamp, string summary, string status)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            summary,
            status,
            Error: status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? "kill_switch=true" : ""));
    }
}
