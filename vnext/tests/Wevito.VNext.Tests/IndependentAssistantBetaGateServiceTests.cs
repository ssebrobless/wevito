using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class IndependentAssistantBetaGateServiceTests
{
    [Fact]
    public void Run_EnablesLimitedAutonomyOnlyWhenAllChecksPass()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        SeedPassingRows(ledger);
        var service = new IndependentAssistantBetaGateService(ledger);

        var result = service.Run(new IndependentAssistantBetaGateRequest(
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));

        Assert.Equal(BetaGateDecisionLabel.EnableLimitedAutonomy, result.Decision.Decision);
        Assert.True(result.Decision.Checks.All(check => check.Passed));
        Assert.True(File.Exists(result.DecisionPath));
        Assert.True(File.Exists(result.SummaryPath));
    }

    [Fact]
    public void Run_KeepsPreviewOnlyWhenNonSafetyProofIsMissing()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet("runtime_session", "uptime_hours>=4 quiet_honored=true", "Completed"));
        ledger.Record(Packet("localDocs", "preview", "PreviewReady"));
        var service = new IndependentAssistantBetaGateService(ledger);

        var result = service.Run(new IndependentAssistantBetaGateRequest(
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));

        Assert.Equal(BetaGateDecisionLabel.KeepPreviewOnly, result.Decision.Decision);
        Assert.Contains(result.Decision.Checks, check => check.CheckId == "learning_eval_proof" && !check.Passed);
    }

    [Fact]
    public void Run_PausesForSafetyWorkWhenNetworkWasUsed()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        SeedPassingRows(ledger);
        ledger.Record(new EvidencePacket(Guid.NewGuid(), "web_fetch", null, DateTimeOffset.Parse("2026-05-12T11:00:00Z"), true, false, false, false, "artifact", "network fetch", "Completed"));
        var service = new IndependentAssistantBetaGateService(ledger);

        var result = service.Run(new IndependentAssistantBetaGateRequest(
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z")));

        Assert.Equal(BetaGateDecisionLabel.PauseForSafetyWork, result.Decision.Decision);
        Assert.Contains(result.Decision.Checks, check => check.CheckId == "offline_local_only" && !check.Passed);
    }

    private static void SeedPassingRows(AuditLedgerService ledger)
    {
        ledger.Record(Packet("runtime_session", "uptime_hours>=4 quiet_honored=true", "Completed"));
        ledger.Record(Packet("localDocs", "preview", "PreviewReady"));
        ledger.Record(Packet("learning_promotion", "dataset promoted", "Completed"));
        ledger.Record(Packet("eval_run", "regression=false", "Completed"));
    }

    private static EvidencePacket Packet(string kind, string summary, string status)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            DateTimeOffset.Parse("2026-05-12T10:00:00Z"),
            false,
            false,
            false,
            kind.Equals("learning_promotion", StringComparison.OrdinalIgnoreCase),
            "artifact",
            summary,
            status);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-beta-gate-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
