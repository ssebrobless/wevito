using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutonomousBetaDecisionServicePrecisionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

    [Fact]
    public void Decide_DoesNotTreatPowerEventsAsPolicyViolations()
    {
        var ledger = CreateLedger();
        SeedRequiredRows(ledger);
        ledger.Record(Packet("power_sleep", "power_event force_quiet=true", "Completed", Now.AddMinutes(-30)));
        ledger.Record(Packet("session_lock", "power_event force_quiet=true", "Completed", Now.AddMinutes(-29)));

        var decision = new AutonomousBetaDecisionService(ledger).Decide(Now);

        Assert.Contains(decision.Checks, check => check.CheckId == "zero_policy_violations" && check.Passed);
    }

    [Fact]
    public void Decide_TreatsOnlyPolicyBlocksOrErroredPolicyRowsAsViolations()
    {
        var ledger = CreateLedger();
        SeedRequiredRows(ledger);
        ledger.Record(Packet("policy_observation", "policy clean", "Completed", Now.AddMinutes(-30)));
        ledger.Record(Packet("policy_block", "policy denied", "Blocked", Now.AddMinutes(-29), error: "blocked"));

        var decision = new AutonomousBetaDecisionService(ledger).Decide(Now);

        Assert.Contains(decision.Checks, check => check.CheckId == "zero_policy_violations" && !check.Passed);
    }

    [Fact]
    public void Decide_RequiresCleanBudgetAndFocusSnapshots()
    {
        var ledger = CreateLedger();
        ledger.Record(Packet("runtime_session_heartbeat", "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed", Now.AddHours(-2)));
        ledger.Record(Packet("localDocs", "preview", "PreviewReady", Now.AddHours(-1)));

        var decision = new AutonomousBetaDecisionService(ledger).Decide(Now);

        Assert.Contains(decision.Checks, check => check.CheckId == "zero_focus_steal_events" && !check.Passed);
        Assert.Contains(decision.Checks, check => check.CheckId == "resource_budget_within_tolerance" && !check.Passed);
    }

    [Fact]
    public void Decide_FailsDirtyBudgetOrFocusSnapshot()
    {
        var ledger = CreateLedger();
        ledger.Record(Packet("runtime_session_heartbeat", "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed", Now.AddHours(-2)));
        ledger.Record(Packet("focus_steal_snapshot", "focus_steal=true day_delta=1 total=1", "Completed", Now.AddHours(-1)));
        ledger.Record(Packet("budget_meter_snapshot", "budget_exceeded=true used_this_hour=4 max_this_hour=4", "Completed", Now.AddHours(-1)));
        ledger.Record(Packet("localDocs", "preview", "PreviewReady", Now.AddHours(-1)));

        var decision = new AutonomousBetaDecisionService(ledger).Decide(Now);

        Assert.Contains(decision.Checks, check => check.CheckId == "zero_focus_steal_events" && !check.Passed);
        Assert.Contains(decision.Checks, check => check.CheckId == "resource_budget_within_tolerance" && !check.Passed);
    }

    private static void SeedRequiredRows(AuditLedgerService ledger)
    {
        ledger.Record(Packet("runtime_session_heartbeat", "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed", Now.AddHours(-2)));
        ledger.Record(Packet("focus_steal_snapshot", "focus_steal=false day_delta=0 total=0", "Completed", Now.AddHours(-1)));
        ledger.Record(Packet("budget_meter_snapshot", "budget_exceeded=false used_this_hour=0 max_this_hour=4", "Completed", Now.AddHours(-1)));
        ledger.Record(Packet("localDocs", "preview", "PreviewReady", Now.AddHours(-1)));
    }

    private static AuditLedgerService CreateLedger()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-autonomous-beta-precision-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static EvidencePacket Packet(
        string kind,
        string summary,
        string status,
        DateTimeOffset createdAt,
        string error = "")
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "artifact",
            summary,
            status,
            error);
    }
}
