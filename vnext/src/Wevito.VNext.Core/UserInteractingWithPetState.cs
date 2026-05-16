namespace Wevito.VNext.Core;

public sealed record UserInteractingWithPetObservation(
    bool IsActive,
    string PacketKind,
    string Summary);

public sealed class UserInteractingWithPetState
{
    public const string EnteredPacketKind = "user_interacting_state_entered";
    public const string ClearedPacketKind = "user_interacting_state_cleared";
    public static readonly TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private DateTimeOffset? _activeUntilUtc;
    private bool _wasActive;

    public UserInteractingWithPetState(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public bool IsActive(DateTimeOffset nowUtc)
    {
        return _activeUntilUtc is not null && nowUtc < _activeUntilUtc.Value;
    }

    public UserInteractingWithPetObservation EnterFromGodotPetInput(DateTimeOffset nowUtc, string inputKind)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new UserInteractingWithPetObservation(false, "", "kill_switch=true");
        }

        _activeUntilUtc = nowUtc + ActiveDuration;
        _wasActive = true;
        var summary = $"source=godot_pet_input input={SafeToken(inputKind)} active_seconds=5";
        Record(EnteredPacketKind, nowUtc, summary, "Completed");
        return new UserInteractingWithPetObservation(true, EnteredPacketKind, summary);
    }

    public UserInteractingWithPetObservation Observe(DateTimeOffset nowUtc)
    {
        var active = IsActive(nowUtc);
        if (!active && _wasActive)
        {
            _wasActive = false;
            var summary = "source=godot_pet_input active=false";
            Record(ClearedPacketKind, nowUtc, summary, "Completed");
            return new UserInteractingWithPetObservation(false, ClearedPacketKind, summary);
        }

        return new UserInteractingWithPetObservation(active, "", "");
    }

    private void Record(string packetKind, DateTimeOffset nowUtc, string summary, string status)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            summary,
            status));
    }

    private static string SafeToken(string value)
    {
        return string.Concat((value ?? "").Where(character => char.IsLetterOrDigit(character) || character is '_' or '-'));
    }
}
