using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ImageGenIdleGuardDecision(
    bool Allowed,
    string Reason,
    string PacketKind = "");

public sealed class ImageGenIdleGuardService
{
    public const string BlockedPacketKind = "image_gen_idle_guard_blocked";
    public static readonly TimeSpan RequiredBackgroundIdle = TimeSpan.FromSeconds(10);

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public ImageGenIdleGuardService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public ImageGenIdleGuardDecision CanStart(
        PetActor pet,
        bool isExplicitUserTrigger,
        DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new ImageGenIdleGuardDecision(false, "kill_switch=true");
        }

        if (isExplicitUserTrigger)
        {
            return new ImageGenIdleGuardDecision(true, "explicit_user_trigger=true");
        }

        var idleSince = pet.IdleSince == default ? nowUtc : pet.IdleSince;
        var idleFor = nowUtc - idleSince;
        if (idleFor >= RequiredBackgroundIdle)
        {
            return new ImageGenIdleGuardDecision(true, $"pet_idle_seconds={idleFor.TotalSeconds:0}");
        }

        var summary = $"background_image_gen_blocked=true pet={pet.Name} pet_idle_seconds={Math.Max(0, idleFor.TotalSeconds):0} required_idle_seconds=10";
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            BlockedPacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            summary,
            "Blocked",
            Error: "pet_not_idle"));
        return new ImageGenIdleGuardDecision(false, summary, BlockedPacketKind);
    }
}
