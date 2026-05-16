namespace Wevito.VNext.Core;

public sealed record WorkloadTierSnapshot(
    double UserForegroundCpuReservedPercent = 60,
    double MaintenanceCpuReservedPercent = 30,
    double ExperimentationCpuReservedPercent = 10,
    double UserForegroundCpuInUsePercent = 0,
    double MaintenanceCpuInUsePercent = 0,
    double ExperimentationCpuInUsePercent = 0);

public sealed record WorkloadTierReservation(
    bool Allowed,
    WorkloadTier Tier,
    double ReservedPercent,
    string Reason);

public sealed class WorkloadTierService
{
    public const string TierPriorityAdjustedPacketKind = "tier_priority_adjusted";
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public WorkloadTierService(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public WorkloadTierReservation TryReserve(
        WorkloadTier tier,
        double requestedCpuPercent,
        WorkloadTierSnapshot snapshot)
    {
        var request = Math.Max(0, requestedCpuPercent);
        var reserved = ReservedFor(tier, snapshot);
        var inUse = InUseFor(tier, snapshot);
        var totalInUse = snapshot.UserForegroundCpuInUsePercent +
                         snapshot.MaintenanceCpuInUsePercent +
                         snapshot.ExperimentationCpuInUsePercent;
        var slack = Math.Max(0, 100 - totalInUse);

        if (tier == WorkloadTier.UserForeground)
        {
            var foregroundResult = request <= reserved + slack
                ? new WorkloadTierReservation(true, tier, reserved, "")
                : new WorkloadTierReservation(false, tier, reserved, "Foreground workload would exceed total CPU budget.");
            RecordIfAdjusted(foregroundResult);
            return foregroundResult;
        }

        var foregroundFloorRemaining = Math.Max(0, snapshot.UserForegroundCpuReservedPercent - snapshot.UserForegroundCpuInUsePercent);
        if (slack - request < foregroundFloorRemaining)
        {
            var result = new WorkloadTierReservation(false, tier, reserved, "Foreground reserve must stay available.");
            RecordIfAdjusted(result);
            return result;
        }

        if (tier == WorkloadTier.Experimentation)
        {
            var maintenanceFloorRemaining = Math.Max(0, snapshot.MaintenanceCpuReservedPercent - snapshot.MaintenanceCpuInUsePercent);
            if (slack - request < foregroundFloorRemaining + maintenanceFloorRemaining)
            {
                var result = new WorkloadTierReservation(false, tier, reserved, "Experimentation yields to maintenance and foreground work.");
                RecordIfAdjusted(result);
                return result;
            }
        }

        var reservation = inUse + request <= reserved + slack
            ? new WorkloadTierReservation(true, tier, reserved, "")
            : new WorkloadTierReservation(false, tier, reserved, "Requested work exceeds available tier budget.");
        RecordIfAdjusted(reservation);
        return reservation;
    }

    private void RecordIfAdjusted(WorkloadTierReservation reservation)
    {
        if (reservation.Allowed || _killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            TierPriorityAdjustedPacketKind,
            null,
            DateTimeOffset.UtcNow,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"tier={reservation.Tier} reason={reservation.Reason}",
            Status: "Completed"));
    }

    private static double ReservedFor(WorkloadTier tier, WorkloadTierSnapshot snapshot)
    {
        return tier switch
        {
            WorkloadTier.UserForeground => snapshot.UserForegroundCpuReservedPercent,
            WorkloadTier.Maintenance => snapshot.MaintenanceCpuReservedPercent,
            WorkloadTier.Experimentation => snapshot.ExperimentationCpuReservedPercent,
            _ => 0
        };
    }

    private static double InUseFor(WorkloadTier tier, WorkloadTierSnapshot snapshot)
    {
        return tier switch
        {
            WorkloadTier.UserForeground => snapshot.UserForegroundCpuInUsePercent,
            WorkloadTier.Maintenance => snapshot.MaintenanceCpuInUsePercent,
            WorkloadTier.Experimentation => snapshot.ExperimentationCpuInUsePercent,
            _ => 0
        };
    }
}
