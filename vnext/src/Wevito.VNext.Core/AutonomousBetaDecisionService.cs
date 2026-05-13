namespace Wevito.VNext.Core;

public enum AutonomousBetaDecisionLabel
{
    EnableAutonomousBeta,
    KeepSupervisedPreview,
    PauseForReliabilityWork
}

public sealed record AutonomousBetaCheck(
    string CheckId,
    bool Passed,
    bool IsSafetyCheck,
    string Detail);

public sealed record AutonomousBetaDecision(
    AutonomousBetaDecisionLabel Decision,
    IReadOnlyList<AutonomousBetaCheck> Checks,
    DateTimeOffset CreatedAtUtc,
    string Summary);

public sealed class AutonomousBetaDecisionService
{
    private readonly AuditLedgerService _ledger;

    public AutonomousBetaDecisionService(AuditLedgerService ledger)
    {
        _ledger = ledger;
    }

    public AutonomousBetaDecision Decide(DateTimeOffset nowUtc, TimeSpan? lookback = null)
    {
        var rows = _ledger.Snapshot(nowUtc.Subtract(lookback ?? TimeSpan.FromDays(7)), nowUtc);
        if (rows.Count == 0)
        {
            return new AutonomousBetaDecision(
                AutonomousBetaDecisionLabel.KeepSupervisedPreview,
                [new AutonomousBetaCheck("ledger_history_present", false, true, "Cannot compute autonomous beta decision without audit ledger rows.")],
                nowUtc,
                "Autonomous beta decision refused because no ledger history exists.");
        }

        var checks = BuildChecks(rows);
        var decision = checks.Any(check => check.IsSafetyCheck && !check.Passed)
            ? AutonomousBetaDecisionLabel.PauseForReliabilityWork
            : checks.All(check => check.Passed)
                ? AutonomousBetaDecisionLabel.EnableAutonomousBeta
                : AutonomousBetaDecisionLabel.KeepSupervisedPreview;
        return new AutonomousBetaDecision(
            decision,
            checks,
            nowUtc,
            $"Autonomous beta decision {decision}: {checks.Count(check => check.Passed)}/{checks.Count} checks passed.");
    }

    private static IReadOnlyList<AutonomousBetaCheck> BuildChecks(IReadOnlyList<AuditLedgerRow> rows)
    {
        return
        [
            Check("active_uptime_present", rows.Any(row => row.PacketKind.StartsWith("runtime_session", StringComparison.OrdinalIgnoreCase) && row.Summary.Contains("uptime_hours>=4", StringComparison.OrdinalIgnoreCase)), false, "Requires an active runtime-session proof row."),
            Check("zero_policy_violations", !rows.Any(IsPolicyViolation), true, "No policy violation rows may be present."),
            Check("mutations_have_proof_packets", MutationsHaveProof(rows), true, "Every mutation row must have proof/post-proof evidence nearby."),
            Check("zero_hosted_ai_local_only", rows.All(row => !row.DidUseHostedAi), true, "Hosted AI calls are not allowed for local-first autonomous beta."),
            Check("zero_focus_steal_events", HasCleanSnapshot(rows, "focus_steal_snapshot", "focus_steal=true"), true, "A focus-steal snapshot must exist and report no focus-steal events."),
            Check("resource_budget_within_tolerance", HasCleanSnapshot(rows, "budget_meter_snapshot", "budget_exceeded=true"), true, "A budget snapshot must exist and report no budget overrun."),
            Check("preview_activity_present", rows.Any(row => row.Status is "PreviewReady" or "Draft" or "Completed"), false, "Requires successful preview/proposal activity.")
        ];

        static AutonomousBetaCheck Check(string id, bool passed, bool safety, string detail)
        {
            return new AutonomousBetaCheck(id, passed, safety, detail);
        }
    }

    private static bool IsPolicyViolation(AuditLedgerRow row)
    {
        return row.PacketKind.Equals("policy_block", StringComparison.OrdinalIgnoreCase) ||
               (row.PacketKind.Contains("policy", StringComparison.OrdinalIgnoreCase) && row.Error.Length > 0);
    }

    private static bool HasCleanSnapshot(IReadOnlyList<AuditLedgerRow> rows, string packetKind, string badFlag)
    {
        var snapshots = rows.Where(row => row.PacketKind.Equals(packetKind, StringComparison.OrdinalIgnoreCase)).ToList();
        return snapshots.Count > 0 &&
               !snapshots.Any(row => row.Summary.Contains(badFlag, StringComparison.OrdinalIgnoreCase) ||
                                     row.Error.Contains(badFlag, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MutationsHaveProof(IReadOnlyList<AuditLedgerRow> rows)
    {
        var mutationRows = rows.Where(row => row.DidMutate || row.PacketKind.Contains("mutation", StringComparison.OrdinalIgnoreCase)).ToList();
        if (mutationRows.Count == 0)
        {
            return true;
        }

        return mutationRows.All(row =>
            row.PacketKind.Contains("proof", StringComparison.OrdinalIgnoreCase) ||
            row.Summary.Contains("post-proof", StringComparison.OrdinalIgnoreCase) ||
            row.ArtifactPath.Contains("proof", StringComparison.OrdinalIgnoreCase) ||
            rows.Any(candidate =>
                candidate.CreatedAtUtc >= row.CreatedAtUtc.AddMinutes(-10) &&
                candidate.CreatedAtUtc <= row.CreatedAtUtc.AddMinutes(10) &&
                (candidate.PacketKind.Contains("proof", StringComparison.OrdinalIgnoreCase) ||
                 candidate.Summary.Contains("post-proof", StringComparison.OrdinalIgnoreCase))));
    }
}
