using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed record SupervisedImprovementLoopRequest(
    IReadOnlyDictionary<string, string> Settings,
    RuntimeSupervisorStatus RuntimeStatus,
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc,
    IReadOnlyList<TaskCard> ExistingTaskCards);

public sealed record SupervisedImprovementLoopResult(
    bool Ran,
    bool DidMutate,
    IReadOnlyList<TaskCard> TaskCards,
    string Summary,
    string BlockReason = "");

public sealed record SupervisedImprovementApprovalResult(
    ApprovalResult ValidationResult,
    bool DidMutate,
    string RefusalReason,
    IReadOnlyList<TaskCard> TaskCards);

public sealed class SupervisedImprovementLoop
{
    public const string ApprovalToolFamily = "self-improvement-apply-awaiting-approval";
    public const string ApplyRunnerNotImplementedReason = "apply_runner_not_implemented_in_v0";
    private readonly AuditLedgerService _ledger;
    private readonly UserApplyApprovalValidator _approvalValidator;
    private readonly KillSwitchService? _killSwitchService;

    public SupervisedImprovementLoop(
        AuditLedgerService ledger,
        UserApplyApprovalValidator? approvalValidator = null,
        KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _approvalValidator = approvalValidator ?? new UserApplyApprovalValidator();
        _killSwitchService = killSwitchService;
    }

    public SupervisedImprovementLoopResult TryRun(SupervisedImprovementLoopRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return Block(request.ExistingTaskCards, "kill_switch=true");
        }

        var settings = SupervisedImprovementLoopSettings.FromSettings(request.Settings);
        if (!settings.Enabled)
        {
            return Block(request.ExistingTaskCards, $"{SupervisedImprovementLoopSettings.EnabledSetting}=false");
        }

        var autonomousConfig = AutonomousOperationsConfig.FromSettings(request.Settings);
        if (!autonomousConfig.Enabled)
        {
            return Block(request.ExistingTaskCards, $"{AutonomousOperationsConfig.EnabledSetting}=false");
        }

        if (!AutonomousScopeService.IsEnabled(request.Settings, AutonomousScopeService.SpriteRepairBatchProposalScopeId))
        {
            return Block(
                request.ExistingTaskCards,
                $"{AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)}=false");
        }

        if (request.RuntimeStatus.Mode != RuntimeSupervisorMode.Active)
        {
            return Block(request.ExistingTaskCards, "Runtime supervisor must be Active.");
        }

        if (!request.RuntimeStatus.BackgroundWorkAllowed)
        {
            return Block(request.ExistingTaskCards, "Runtime supervisor background work must be allowed.");
        }

        if (FindOpenApprovalCard(request.ExistingTaskCards) is not null)
        {
            return Block(request.ExistingTaskCards, "self_improvement_apply_awaiting_approval already open");
        }

        var proposalCard = request.ExistingTaskCards
            .LastOrDefault(IsSpriteRepairBatchProposalCard);
        if (proposalCard is null)
        {
            return Block(request.ExistingTaskCards, "no proposal card available");
        }

        var operationId = BuildOperationId(proposalCard);
        var proposalPath = GetPayloadValue(proposalCard, "proposal_path");
        var dryRunPath = GetPayloadValue(proposalCard, "dry_run_path");
        var evalPath = GetPayloadValue(proposalCard, "eval_path");
        var scopeHash = BuildScopeHash(operationId, proposalPath, dryRunPath, evalPath);
        var artifactPath = WriteAwaitingApprovalArtifact(request, proposalCard, operationId, scopeHash);
        var approvalCard = BuildAwaitingApprovalCard(request.RequestedAtUtc, proposalCard, operationId, artifactPath, scopeHash);
        Record(
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            approvalCard.Id,
            request.RequestedAtUtc,
            artifactPath,
            "WaitingForApproval",
            $"Supervised self-improvement proposal is awaiting explicit user approval for operation {operationId}.");

        return new SupervisedImprovementLoopResult(
            true,
            false,
            request.ExistingTaskCards.Concat([approvalCard]).ToArray(),
            $"awaiting explicit apply approval for {operationId}; mutate=false apply=false.");
    }

    public SupervisedImprovementApprovalResult HandleApplyApproval(
        UserApplyApproval? approval,
        string expectedScopeId,
        string expectedOperationId,
        string expectedScopeHash,
        Guid taskCardId,
        DateTimeOffset nowUtc,
        IReadOnlyList<TaskCard> existingTaskCards)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            RecordRefusal(taskCardId, nowUtc, "kill_switch=true");
            return Refused(existingTaskCards, "kill_switch=true");
        }

        var validationResult = _approvalValidator.ValidateUserApplyApproval(
            approval,
            expectedScopeId,
            expectedOperationId,
            expectedScopeHash,
            nowUtc);

        if (validationResult is ApprovalResult.Refused refused)
        {
            RecordRefusal(taskCardId, nowUtc, refused.Reason);
            return new SupervisedImprovementApprovalResult(validationResult, false, refused.Reason, existingTaskCards);
        }

        RecordRefusal(taskCardId, nowUtc, ApplyRunnerNotImplementedReason);
        return new SupervisedImprovementApprovalResult(
            new ApprovalResult.Refused(ApplyRunnerNotImplementedReason),
            false,
            ApplyRunnerNotImplementedReason,
            existingTaskCards);
    }

    public static bool IsAwaitingApprovalCard(TaskCard card)
    {
        return string.Equals(card.ToolFamily, ApprovalToolFamily, StringComparison.OrdinalIgnoreCase) &&
               card.ReviewPayload?.TryGetValue("packet_kind", out var packetKind) == true &&
               string.Equals(packetKind, SelfImprovementPacketKinds.ApplyAwaitingApproval, StringComparison.Ordinal);
    }

    public static string BuildOperationId(TaskCard proposalCard)
    {
        return $"apply-{proposalCard.Id:N}";
    }

    private static bool IsSpriteRepairBatchProposalCard(TaskCard card)
    {
        return string.Equals(card.ToolFamily, SpriteRepairBatchProposalDescriptor.Kind, StringComparison.OrdinalIgnoreCase) &&
               card.ReviewPayload?.ContainsKey("proposal_path") == true;
    }

    private static TaskCard? FindOpenApprovalCard(IReadOnlyList<TaskCard> cards)
    {
        return cards.FirstOrDefault(card =>
            IsAwaitingApprovalCard(card) &&
            card.Status is TaskCardStatus.Draft or TaskCardStatus.WaitingForApproval or TaskCardStatus.Approved or TaskCardStatus.Reviewing);
    }

    private static TaskCard BuildAwaitingApprovalCard(
        DateTimeOffset timestamp,
        TaskCard proposalCard,
        string operationId,
        string artifactPath,
        string scopeHash)
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Approve supervised self-improvement apply operation {operationId}.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: ApprovalToolFamily,
            TargetPathsOrAssets: proposalCard.Intent.TargetPathsOrAssets ?? [],
            RiskLevel: ToolRiskLevel.High,
            NeedsApproval: true,
            ExpectedOutput: "Typed operation-id approval only; apply runner is not implemented in v0.");
        var policy = new ToolPolicy(
            "supervised-self-improvement-apply-approval",
            ApprovalToolFamily,
            ToolAccessMode.Write,
            ToolRiskLevel.High,
            ApprovalRequirement.ActionTime,
            IsEnabled: false,
            ApprovedRootPaths: [],
            BlockReason: "Apply remains blocked until the user types the exact operation id. v0 still refuses because the apply runner is not implemented.");
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.WaitingForApproval,
            ToolFamily: ApprovalToolFamily,
            PolicySnapshot: policy,
            Timeline:
            [
                $"{timestamp:O} supervised loop prepared explicit apply approval card.",
                $"source_task_card_id={proposalCard.Id}",
                $"operation_id={operationId}",
                $"scope_hash={scopeHash}",
                "apply_runner=not_implemented_in_v0"
            ],
            ResultSummary: "Awaiting explicit user approval. No apply has run, and the v0 apply runner still refuses safely.",
            AuditLogPath: artifactPath,
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp,
            ReviewPayload: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["packet_kind"] = SelfImprovementPacketKinds.ApplyAwaitingApproval,
                ["scope_id"] = AutonomousScopeService.SpriteRepairBatchProposalScopeId,
                ["operation_id"] = operationId,
                ["scope_hash"] = scopeHash,
                ["source_task_card_id"] = proposalCard.Id.ToString(),
                ["artifact_path"] = artifactPath
            });
    }

    private static string WriteAwaitingApprovalArtifact(
        SupervisedImprovementLoopRequest request,
        TaskCard proposalCard,
        string operationId,
        string scopeHash)
    {
        var root = Path.Combine(request.ArtifactRoot, "supervised-improvement-pilot", operationId);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "apply-awaiting-approval.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            generatedAtUtc = request.RequestedAtUtc,
            packetKind = SelfImprovementPacketKinds.ApplyAwaitingApproval,
            scopeId = AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            scopeHash,
            sourceTaskCardId = proposalCard.Id,
            proposalPath = proposalCard.ReviewPayload?.TryGetValue("proposal_path", out var proposalPath) == true ? proposalPath : "",
            dryRunPath = proposalCard.ReviewPayload?.TryGetValue("dry_run_path", out var dryRunPath) == true ? dryRunPath : "",
            evalPath = proposalCard.ReviewPayload?.TryGetValue("eval_path", out var evalPath) == true ? evalPath : "",
            didMutate = false,
            applyRunner = "not_implemented_in_v0",
            approvalRequired = true
        }, JsonDefaults.Options));
        return path;
    }

    private static string BuildScopeHash(string operationId, string proposalPath, string dryRunPath, string evalPath)
    {
        var descriptor = SpriteRepairBatchProposalDescriptor.Descriptor;
        var packetKindsTouched = new[]
        {
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ApplyAwaitingApproval
        };
        var manifestHash = ExperimentManifestHash.Compute(
            descriptor,
            SpriteRepairBatchProposalDescriptor.MutationPosture,
            packetKindsTouched);

        return ScopeHash.Compute(new ScopeHashInputs(
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            ComputeFileSha256(proposalPath),
            ComputeFileSha256(dryRunPath),
            ComputeFileSha256(evalPath),
            descriptor.ManifestVersion,
            packetKindsTouched,
            manifestHash));
    }

    private static string ComputeFileSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string GetPayloadValue(TaskCard card, string key)
    {
        return card.ReviewPayload?.TryGetValue(key, out var value) == true ? value : "";
    }

    private void RecordRefusal(Guid taskCardId, DateTimeOffset nowUtc, string reason)
    {
        Record(
            SelfImprovementPacketKinds.ApplyRefused,
            taskCardId,
            nowUtc,
            "",
            "Refused",
            $"Supervised self-improvement apply refused: {reason}.",
            reason);
    }

    private void Record(
        string packetKind,
        Guid taskCardId,
        DateTimeOffset timestamp,
        string artifactPath,
        string status,
        string summary,
        string error = "")
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: artifactPath,
            Summary: summary,
            Status: status,
            Error: error));
    }

    private static SupervisedImprovementLoopResult Block(IReadOnlyList<TaskCard> cards, string reason)
    {
        return new SupervisedImprovementLoopResult(false, false, cards, "", reason);
    }

    private static SupervisedImprovementApprovalResult Refused(IReadOnlyList<TaskCard> cards, string reason)
    {
        return new SupervisedImprovementApprovalResult(new ApprovalResult.Refused(reason), false, reason, cards);
    }
}
