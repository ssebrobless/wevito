using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record RollbackProposalRequest(
    DateTimeOffset SinceUtc,
    DateTimeOffset UntilUtc,
    string ArtifactRoot,
    DateTimeOffset GeneratedAtUtc);

public sealed record RollbackProposalResult(
    bool Succeeded,
    bool ProposalCreated,
    string ArtifactFolder,
    string ProposalPath,
    TaskCard? TaskCard,
    string Message);

public sealed class RollbackProposalService
{
    public const string PacketKind = "rollback_proposal";
    public const string ToolFamily = "guardedMutation";

    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;

    public RollbackProposalService(AuditLedgerService ledger, KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _killSwitchService = killSwitchService;
    }

    public RollbackProposalResult Run(RollbackProposalRequest request)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new RollbackProposalResult(false, false, "", "", null, "kill_switch=true");
        }

        var rows = _ledger.Snapshot(request.SinceUtc, request.UntilUtc)
            .OrderBy(row => row.CreatedAtUtc)
            .ThenBy(row => row.Id)
            .ToList();
        var pair = FindApplyRegressionPair(rows);
        if (pair is null)
        {
            return new RollbackProposalResult(true, false, "", "", null, "No tuning apply followed by eval regression within 24 hours.");
        }

        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);
        var folder = Path.Combine(artifactRoot, $"{request.GeneratedAtUtc:yyyyMMdd-HHmmss}-rollback-proposal");
        Directory.CreateDirectory(folder);
        var proposalPath = Path.Combine(folder, "proposal.json");
        var summaryPath = Path.Combine(folder, "run-summary.md");
        var taskCardId = Guid.NewGuid();
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Review rollback proposal for tuning apply {pair.Value.Apply.PacketId}",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.PlanCodePatch,
            RequestedToolFamily: ToolFamily,
            RiskLevel: ToolRiskLevel.High,
            NeedsApproval: true,
            ExpectedOutput: "Draft rollback plan only. Do not execute automatically.",
            CreatedAtUtc: request.GeneratedAtUtc);
        var policy = new ToolPolicy(
            "rollback-proposal-guarded-mutation",
            ToolFamily,
            ToolAccessMode.Write,
            ToolRiskLevel.High,
            ApprovalRequirement.BeforeExecution,
            IsEnabled: false,
            BlockReason: "Rollback proposals are review-only until an operator explicitly approves a guarded mutation.");
        var taskCard = new TaskCard(
            taskCardId,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: ToolFamily,
            PolicySnapshot: policy,
            Timeline: [
                "rollback_proposal_created: eval regression followed a tuning apply within 24 hours",
                "rollback_not_executed: guarded mutation approval is required"
            ],
            ResultSummary: "Rollback proposal drafted. No rollback has run.",
            AuditLogPath: proposalPath,
            CreatedAtUtc: request.GeneratedAtUtc,
            UpdatedAtUtc: request.GeneratedAtUtc);

        File.WriteAllText(proposalPath, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            applyPacketId = pair.Value.Apply.PacketId,
            applyArtifactPath = pair.Value.Apply.ArtifactPath,
            applyCreatedAtUtc = pair.Value.Apply.CreatedAtUtc,
            regressionPacketId = pair.Value.Regression.PacketId,
            regressionArtifactPath = pair.Value.Regression.ArtifactPath,
            regressionCreatedAtUtc = pair.Value.Regression.CreatedAtUtc,
            taskCardId,
            toolFamily = ToolFamily,
            rollbackExecuted = false,
            nextStep = "Review the proposal and run the guarded mutation rollback path only after explicit approval."
        }, JsonDefaults.Options));
        File.WriteAllText(summaryPath, string.Join(Environment.NewLine, [
            "# Rollback Proposal",
            "",
            $"- Tuning apply packet: {pair.Value.Apply.PacketId}",
            $"- Eval regression packet: {pair.Value.Regression.PacketId}",
            "- Rollback executed: false",
            "- Next step: review the guarded mutation draft task card before any rollback."
        ]));

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            taskCardId,
            request.GeneratedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            folder,
            "Rollback proposal created after tuning apply was followed by eval regression within 24 hours.",
            "Draft"));

        return new RollbackProposalResult(
            true,
            true,
            folder,
            proposalPath,
            taskCard,
            "Rollback proposal drafted; no rollback executed.");
    }

    private static (AuditLedgerRow Apply, AuditLedgerRow Regression)? FindApplyRegressionPair(IReadOnlyList<AuditLedgerRow> rows)
    {
        var applies = rows
            .Where(row => row.PacketKind.Equals(AuditLedgerService.TuningApplyPacketKind, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var apply in applies)
        {
            var regression = rows.FirstOrDefault(row =>
                row.CreatedAtUtc > apply.CreatedAtUtc &&
                row.CreatedAtUtc <= apply.CreatedAtUtc.AddHours(24) &&
                IsRegressionRow(row));
            if (regression is not null)
            {
                return (apply, regression);
            }
        }

        return null;
    }

    private static bool IsRegressionRow(AuditLedgerRow row)
    {
        return row.PacketKind.Equals(AuditLedgerService.GoldenEvalPacketKind, StringComparison.OrdinalIgnoreCase) &&
            (row.Status.Contains("Regression", StringComparison.OrdinalIgnoreCase) ||
             row.Summary.Contains("regression", StringComparison.OrdinalIgnoreCase) ||
             row.Error.Contains("regression", StringComparison.OrdinalIgnoreCase));
    }
}
