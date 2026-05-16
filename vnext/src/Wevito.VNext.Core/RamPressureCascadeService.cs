namespace Wevito.VNext.Core;

public enum RamPressureTier
{
    Normal,
    SuspendBackground,
    UnloadImageGen,
    UnloadLlm,
    Emergency
}

public sealed record RamPressureSnapshot(
    double FreeRamGb,
    double PetReservedMinGb = 1.0,
    DateTimeOffset CapturedAtUtc = default);

public sealed record RamPressureCascadeResult(
    RamPressureTier Tier,
    bool SuspendBackgroundExperiments,
    bool UnloadImageGenSidecar,
    bool UnloadLlmModel,
    bool StopCardRequired,
    bool PetReservedMinTouched,
    string PacketKind,
    string Summary);

public sealed class RamPressureCascadeService
{
    public const string EventPacketKind = "ram_pressure_event";
    public const string EmergencyPacketKind = "ram_pressure_emergency";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public RamPressureCascadeService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public RamPressureCascadeResult Evaluate(RamPressureSnapshot snapshot)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new RamPressureCascadeResult(
                RamPressureTier.SuspendBackground,
                SuspendBackgroundExperiments: true,
                UnloadImageGenSidecar: false,
                UnloadLlmModel: false,
                StopCardRequired: false,
                PetReservedMinTouched: false,
                EventPacketKind,
                "kill_switch=true ram cascade observed without mutation");
        }

        var tier = ResolveTier(snapshot.FreeRamGb);
        var result = new RamPressureCascadeResult(
            tier,
            SuspendBackgroundExperiments: tier >= RamPressureTier.SuspendBackground,
            UnloadImageGenSidecar: tier >= RamPressureTier.UnloadImageGen,
            UnloadLlmModel: tier >= RamPressureTier.UnloadLlm,
            StopCardRequired: tier == RamPressureTier.Emergency,
            PetReservedMinTouched: false,
            tier == RamPressureTier.Emergency ? EmergencyPacketKind : EventPacketKind,
            BuildSummary(snapshot, tier));

        if (tier != RamPressureTier.Normal)
        {
            Record(result, snapshot.CapturedAtUtc == default ? DateTimeOffset.UtcNow : snapshot.CapturedAtUtc);
        }

        return result;
    }

    private static RamPressureTier ResolveTier(double freeRamGb)
    {
        if (freeRamGb < 1.0)
        {
            return RamPressureTier.Emergency;
        }

        if (freeRamGb < 1.5)
        {
            return RamPressureTier.UnloadLlm;
        }

        if (freeRamGb < 2.0)
        {
            return RamPressureTier.UnloadImageGen;
        }

        return freeRamGb < 3.0 ? RamPressureTier.SuspendBackground : RamPressureTier.Normal;
    }

    private static string BuildSummary(RamPressureSnapshot snapshot, RamPressureTier tier)
    {
        return $"free_ram_gb={snapshot.FreeRamGb:0.##} tier={tier} pet_reserved_min_gb={snapshot.PetReservedMinGb:0.##} pet_reserved_min_touched=false";
    }

    private void Record(RamPressureCascadeResult result, DateTimeOffset nowUtc)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            result.PacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            result.Summary,
            result.StopCardRequired ? "Blocked" : "Completed",
            Error: result.StopCardRequired ? "ram_pressure_emergency" : ""));
    }
}
