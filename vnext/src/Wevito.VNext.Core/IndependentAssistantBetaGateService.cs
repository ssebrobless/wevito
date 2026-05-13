using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record IndependentAssistantBetaGateRequest(
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc,
    TimeSpan LookbackWindow = default);

public sealed record IndependentAssistantBetaGateResult(
    BetaGateDecision Decision,
    string DecisionPath,
    string SummaryPath);

public sealed class IndependentAssistantBetaGateService
{
    private readonly AuditLedgerService _ledger;

    public IndependentAssistantBetaGateService(AuditLedgerService ledger)
    {
        _ledger = ledger;
    }

    public IndependentAssistantBetaGateResult Run(IndependentAssistantBetaGateRequest request)
    {
        var lookback = request.LookbackWindow == default ? TimeSpan.FromDays(7) : request.LookbackWindow;
        var rows = _ledger.Snapshot(request.RequestedAtUtc.Subtract(lookback), request.RequestedAtUtc);
        var checks = BuildChecks(rows);
        var decision = ResolveDecision(checks);
        var packet = new BetaGateDecision(
            "1",
            decision,
            checks,
            request.RequestedAtUtc,
            BuildSummary(decision, checks));

        var folder = Path.Combine(request.ArtifactRoot, $"{request.RequestedAtUtc:yyyyMMdd-HHmmss}-beta-gate");
        Directory.CreateDirectory(folder);
        var decisionPath = Path.Combine(folder, "beta-gate-decision.json");
        var summaryPath = Path.Combine(folder, "run-summary.md");
        File.WriteAllText(decisionPath, JsonSerializer.Serialize(packet, JsonDefaults.Options));
        File.WriteAllText(summaryPath, BuildMarkdown(packet));
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            "beta_gate",
            null,
            request.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            folder,
            packet.Summary,
            decision.ToString()));
        return new IndependentAssistantBetaGateResult(packet, decisionPath, summaryPath);
    }

    private static IReadOnlyList<BetaGateCheckResult> BuildChecks(IReadOnlyList<AuditLedgerRow> rows)
    {
        var noNetworkOrHosted = rows.All(row => !row.DidUseNetwork && !row.DidUseHostedAi);
        return
        [
            Check("long_session_active", rows.Any(row => row.PacketKind.Equals("runtime_session", StringComparison.OrdinalIgnoreCase) && row.Summary.Contains("uptime_hours>=4", StringComparison.OrdinalIgnoreCase)), false, "Requires a >=4h active runtime-session proof row."),
            Check("quiet_mode_honored", rows.Any(row => row.Summary.Contains("quiet_honored=true", StringComparison.OrdinalIgnoreCase)), true, "Requires quiet/fullscreen/user-keystroke proof."),
            Check("runtime_budget_not_exceeded", !rows.Any(row => row.Summary.Contains("budget_exceeded=true", StringComparison.OrdinalIgnoreCase) || row.Error.Contains("budget_exceeded=true", StringComparison.OrdinalIgnoreCase)), true, "No budget-exceeded ledger row may be present."),
            Check("preview_history_present", rows.Any(row => row.Status is "PreviewReady" or "Completed" or "Draft"), false, "Requires preview/execution history."),
            Check("learning_eval_proof", HasLearningProof(rows), false, "Requires learning promotion plus non-regression eval proof."),
            Check("offline_local_only", noNetworkOrHosted, true, "No network or hosted-AI ledger rows are allowed for beta enablement.")
        ];

        static BetaGateCheckResult Check(string id, bool passed, bool safety, string detail)
        {
            return new BetaGateCheckResult(id, passed, safety, detail);
        }
    }

    private static bool HasLearningProof(IReadOnlyList<AuditLedgerRow> rows)
    {
        return rows.Any(row => row.PacketKind.Equals("learning_promotion", StringComparison.OrdinalIgnoreCase)) &&
            rows.Any(row =>
                row.PacketKind.Equals("eval_run", StringComparison.OrdinalIgnoreCase) &&
                !row.Summary.Contains("regression=true", StringComparison.OrdinalIgnoreCase) &&
                !row.Status.Equals("Regression", StringComparison.OrdinalIgnoreCase));
    }

    private static BetaGateDecisionLabel ResolveDecision(IReadOnlyList<BetaGateCheckResult> checks)
    {
        if (checks.Any(check => check.IsSafetyCheck && !check.Passed))
        {
            return BetaGateDecisionLabel.PauseForSafetyWork;
        }

        return checks.All(check => check.Passed)
            ? BetaGateDecisionLabel.EnableLimitedAutonomy
            : BetaGateDecisionLabel.KeepPreviewOnly;
    }

    private static string BuildSummary(BetaGateDecisionLabel decision, IReadOnlyList<BetaGateCheckResult> checks)
    {
        var passed = checks.Count(check => check.Passed);
        return $"Beta gate decision {decision}: {passed}/{checks.Count} checks passed.";
    }

    private static string BuildMarkdown(BetaGateDecision packet)
    {
        var lines = new List<string>
        {
            "# Independent Assistant Beta Gate",
            "",
            $"- Decision: {packet.Decision}",
            $"- Created: {packet.CreatedAtUtc:O}",
            $"- Summary: {packet.Summary}",
            "",
            "## Checks"
        };
        lines.AddRange(packet.Checks.Select(check => $"- {(check.Passed ? "PASS" : "FAIL")} `{check.CheckId}` safety={check.IsSafetyCheck}: {check.Detail}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
