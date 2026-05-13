using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutonomousBetaDecisionServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");

    [Fact]
    public void Decide_RefusesWithoutLedgerRows()
    {
        var service = new AutonomousBetaDecisionService(CreateLedger());

        var decision = service.Decide(Now);

        Assert.Equal(AutonomousBetaDecisionLabel.KeepSupervisedPreview, decision.Decision);
        Assert.Contains(decision.Checks, check => check.CheckId == "ledger_history_present" && !check.Passed);
        Assert.Contains("refused", decision.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decide_EnablesAutonomousBetaWhenAllChecksPass()
    {
        var ledger = CreateLedger();
        SeedPassingRows(ledger);
        var service = new AutonomousBetaDecisionService(ledger);

        var decision = service.Decide(Now);

        Assert.Equal(AutonomousBetaDecisionLabel.EnableAutonomousBeta, decision.Decision);
        Assert.All(decision.Checks, check => Assert.True(check.Passed, check.CheckId));
    }

    [Fact]
    public void Decide_PausesForHostedAiUseInLocalOnlyWindow()
    {
        var ledger = CreateLedger();
        SeedPassingRows(ledger);
        ledger.Record(Packet("model_call", Now.AddMinutes(-20), "hosted model call", "Completed", hostedAi: true));
        var service = new AutonomousBetaDecisionService(ledger);

        var decision = service.Decide(Now);

        Assert.Equal(AutonomousBetaDecisionLabel.PauseForReliabilityWork, decision.Decision);
        Assert.Contains(decision.Checks, check => check.CheckId == "zero_hosted_ai_local_only" && !check.Passed);
    }

    [Fact]
    public void Decide_PausesForMutationWithoutProof()
    {
        var ledger = CreateLedger();
        ledger.Record(Packet("runtime_session_heartbeat", Now.AddHours(-2), "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed"));
        ledger.Record(Packet("focus_steal_snapshot", Now.AddHours(-1), "focus_steal=false day_delta=0 total=0", "Completed"));
        ledger.Record(Packet("budget_meter_snapshot", Now.AddHours(-1), "budget_exceeded=false used_this_hour=0 max_this_hour=4", "Completed"));
        ledger.Record(Packet("localDocs", Now.AddHours(-1), "preview", "PreviewReady"));
        ledger.Record(Packet("mutation_apply", Now.AddMinutes(-30), "applied change", "Completed", mutate: true));
        var service = new AutonomousBetaDecisionService(ledger);

        var decision = service.Decide(Now);

        Assert.Equal(AutonomousBetaDecisionLabel.PauseForReliabilityWork, decision.Decision);
        Assert.Contains(decision.Checks, check => check.CheckId == "mutations_have_proof_packets" && !check.Passed);
    }

    private static void SeedPassingRows(AuditLedgerService ledger)
    {
        ledger.Record(Packet("runtime_session_heartbeat", Now.AddHours(-2), "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed"));
        ledger.Record(Packet("focus_steal_snapshot", Now.AddHours(-1), "focus_steal=false day_delta=0 total=0", "Completed"));
        ledger.Record(Packet("budget_meter_snapshot", Now.AddHours(-1), "budget_exceeded=false used_this_hour=0 max_this_hour=4", "Completed"));
        ledger.Record(Packet("localDocs", Now.AddHours(-1), "preview", "PreviewReady"));
        ledger.Record(Packet("mutation_apply", Now.AddMinutes(-40), "post-proof passed", "Completed", mutate: true));
        ledger.Record(Packet("proof_packet", Now.AddMinutes(-39), "post-proof passed", "Completed"));
    }

    private static AuditLedgerService CreateLedger()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-autonomous-beta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static EvidencePacket Packet(
        string kind,
        DateTimeOffset createdAt,
        string summary,
        string status,
        bool hostedAi = false,
        bool mutate = false)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: hostedAi,
            DidUseLocalModel: false,
            DidMutate: mutate,
            ArtifactPath: "artifact",
            summary,
            status);
    }
}
