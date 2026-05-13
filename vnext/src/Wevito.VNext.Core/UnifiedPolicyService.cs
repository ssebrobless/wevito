using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class UnifiedPolicyService
{
    private readonly LocalToolAccessPolicy _accessPolicy;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public UnifiedPolicyService(
        LocalToolAccessPolicy? accessPolicy = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _accessPolicy = accessPolicy ?? new LocalToolAccessPolicy();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public PolicyDecision EvaluateRead(
        string path,
        IReadOnlyList<string>? approvedRoots = null,
        Guid? taskCardId = null,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var decision = _killSwitchService?.IsActive() == true
            ? Block(PolicyDecisionScope.FileRead, path, "kill_switch=true")
            : _accessPolicy.EvaluateRead(path, approvedRoots);
        Record(decision, taskCardId, timestamp);
        return decision;
    }

    public PolicyDecision EvaluateLocalToolExecution(
        string scriptPath,
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyDictionary<string, string>? allowedScriptSha256 = null,
        Guid? taskCardId = null,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var decision = _killSwitchService?.IsActive() == true
            ? Block(PolicyDecisionScope.LocalToolExecution, scriptPath, "kill_switch=true")
            : _accessPolicy.EvaluateToolScript(scriptPath, settings, allowedScriptSha256);
        Record(decision, taskCardId, timestamp);
        return decision;
    }

    public ToolPolicyDecision EvaluateToolPolicy(TaskIntent intent, IReadOnlyList<ToolPolicy> policies)
    {
        if (intent.RiskLevel == ToolRiskLevel.Blocked ||
            !string.IsNullOrWhiteSpace(intent.RefusalOrClarificationReason))
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: string.IsNullOrWhiteSpace(intent.RefusalOrClarificationReason)
                    ? "Task intent is blocked before policy evaluation."
                    : intent.RefusalOrClarificationReason);
        }

        if (string.IsNullOrWhiteSpace(intent.RequestedToolFamily))
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: "Task intent does not declare a requested tool family.");
        }

        var policy = policies.FirstOrDefault(candidate =>
            string.Equals(candidate.ToolFamily, intent.RequestedToolFamily, StringComparison.OrdinalIgnoreCase));

        if (policy is null)
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: $"No tool policy is registered for \"{intent.RequestedToolFamily}\".");
        }

        if (!policy.IsEnabled || policy.RiskLevel == ToolRiskLevel.Blocked)
        {
            return new ToolPolicyDecision(
                policy.ToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                policy,
                string.IsNullOrWhiteSpace(policy.BlockReason)
                    ? $"Tool family \"{policy.ToolFamily}\" is disabled."
                    : policy.BlockReason);
        }

        var effectiveRisk = MaxRisk(intent.RiskLevel, policy.RiskLevel);
        if (intent.NeedsApproval || policy.ApprovalRequirement != ApprovalRequirement.None)
        {
            return new ToolPolicyDecision(
                policy.ToolFamily,
                ToolPolicyDecisionStatus.ApprovalRequired,
                effectiveRisk,
                policy.ApprovalRequirement == ApprovalRequirement.None
                    ? ApprovalRequirement.BeforeExecution
                    : policy.ApprovalRequirement,
                policy,
                "Tool policy requires approval before execution.");
        }

        return new ToolPolicyDecision(
            policy.ToolFamily,
            ToolPolicyDecisionStatus.Allowed,
            effectiveRisk,
            ApprovalRequirement.None,
            policy,
            "Read-only or otherwise pre-approved tool family.");
    }

    public CapturePolicyDecision EvaluateCapture(CaptureRequest request)
    {
        if (request.IsExternalShareRequested || request.PrivacyLevel == CapturePrivacyLevel.ExternalShare)
        {
            return CaptureDecision(
                request,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                "External upload/share is blocked for capture artifacts.");
        }

        if (request.IsRecording || request.OutputKind is CaptureOutputKind.ClipMp4 or CaptureOutputKind.ClipGif)
        {
            return CaptureDecision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Screen recording requires explicit approval before capture starts.");
        }

        return request.TargetKind switch
        {
            CaptureTargetKind.WevitoWindow => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Wevito-window still capture requires explicit approval before capture starts."),

            CaptureTargetKind.ProofSurface => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                "Proof-surface capture is blocked until a dedicated proof target is implemented."),

            CaptureTargetKind.SelectedRegion or CaptureTargetKind.LastRegion => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Selected-region capture may include private screen content and requires approval."),

            CaptureTargetKind.ForegroundWindow => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Foreground-window capture may include another application and requires approval."),

            CaptureTargetKind.FullDesktop => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.High,
                ApprovalRequirement.ActionTime,
                "Full-desktop capture may include private content and requires approval."),

            _ => CaptureDecision(
                request,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                $"Capture target \"{request.TargetKind}\" is not supported.")
        };
    }

    public HelperAllowlistDecision EvaluateHelperCapability(HelperAllowlistDecision decision)
    {
        return decision;
    }

    public ProofExecutionAllowlistResult EvaluateProofExecution(
        ProofExecutionCommand requestedCommand,
        IReadOnlyList<ProofExecutionCommand>? allowedCommands,
        IReadOnlyList<string> blockedExecutables,
        IReadOnlyList<string> blockedTokens)
    {
        var hardBlock = FindProofHardBlockReason(requestedCommand, blockedExecutables, blockedTokens);
        if (!string.IsNullOrWhiteSpace(hardBlock))
        {
            return new ProofExecutionAllowlistResult(
                ProofExecutionAllowlistDecision.Blocked,
                hardBlock,
                MatchedCommand: null);
        }

        var allowed = allowedCommands ?? [];
        var matched = allowed.FirstOrDefault(command => IsExactProofCommandMatch(command, requestedCommand));
        if (matched is null)
        {
            return new ProofExecutionAllowlistResult(
                ProofExecutionAllowlistDecision.Blocked,
                "Command did not exactly match an allowlisted executable and ordered argument list.",
                MatchedCommand: null);
        }

        return new ProofExecutionAllowlistResult(
            ProofExecutionAllowlistDecision.Allowed,
            "Command exactly matched allowlist.",
            matched);
    }

    private void Record(PolicyDecision decision, Guid? taskCardId, DateTimeOffset timestamp)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            "unified_policy",
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: decision.NormalizedPath ?? "",
            $"{decision.Scope}:{decision.Subject} => {decision.Status}: {decision.Reason}",
            decision.Status.ToString(),
            decision.IsBlocked ? decision.Reason : ""));
    }

    private static ToolRiskLevel MaxRisk(ToolRiskLevel left, ToolRiskLevel right)
    {
        return (ToolRiskLevel)Math.Max((int)left, (int)right);
    }

    private static CapturePolicyDecision CaptureDecision(
        CaptureRequest request,
        ToolPolicyDecisionStatus status,
        ToolRiskLevel riskLevel,
        ApprovalRequirement approvalRequirement,
        string reason)
    {
        return new CapturePolicyDecision(
            request.Preset,
            request.TargetKind,
            status,
            riskLevel,
            approvalRequirement,
            reason);
    }

    private static string FindProofHardBlockReason(
        ProofExecutionCommand command,
        IReadOnlyList<string> blockedExecutables,
        IReadOnlyList<string> blockedTokens)
    {
        var executableName = Path.GetFileNameWithoutExtension(command.Executable);
        if (blockedExecutables.Any(blocked => string.Equals(blocked, executableName, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.Equals(executableName, "git", StringComparison.OrdinalIgnoreCase))
            {
                return $"Executable '{command.Executable}' is hard-blocked.";
            }

            var gitSubcommand = command.Arguments.FirstOrDefault() ?? "";
            if (gitSubcommand.Equals("reset", StringComparison.OrdinalIgnoreCase) ||
                gitSubcommand.Equals("checkout", StringComparison.OrdinalIgnoreCase))
            {
                return $"git {gitSubcommand} is hard-blocked.";
            }
        }

        var parts = new[] { command.Executable }.Concat(command.Arguments);
        foreach (var part in parts)
        {
            if (blockedTokens.Any(token => part.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                return $"Command contains blocked shell composition or automation token '{part}'.";
            }
        }

        if (command.Arguments.Any(argument => argument.Contains("build-vnext.ps1", StringComparison.OrdinalIgnoreCase)) &&
            !command.Arguments.Any(argument => argument.Equals("-SkipAssetPrep", StringComparison.OrdinalIgnoreCase)))
        {
            return "build-vnext.ps1 is blocked unless -SkipAssetPrep is present.";
        }

        if (command.Arguments.Any(argument => argument.Contains("prep", StringComparison.OrdinalIgnoreCase) || argument.Contains("asset-prep", StringComparison.OrdinalIgnoreCase)) &&
            !command.Arguments.Any(argument => argument.Equals("-SkipAssetPrep", StringComparison.OrdinalIgnoreCase)))
        {
            return "Asset preparation is blocked in buildProof execution.";
        }

        return "";
    }

    private static bool IsExactProofCommandMatch(ProofExecutionCommand allowed, ProofExecutionCommand requested)
    {
        return string.Equals(allowed.Executable, requested.Executable, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(NormalizeWorkingDirectory(allowed.WorkingDirectory), NormalizeWorkingDirectory(requested.WorkingDirectory), StringComparison.OrdinalIgnoreCase) &&
               allowed.Arguments.SequenceEqual(requested.Arguments, StringComparer.Ordinal) &&
               allowed.MustSkipAssetPrep == requested.MustSkipAssetPrep;
    }

    private static string NormalizeWorkingDirectory(string workingDirectory)
    {
        return string.IsNullOrWhiteSpace(workingDirectory) ? "." : workingDirectory.Trim();
    }

    private static PolicyDecision Block(PolicyDecisionScope scope, string subject, string reason)
    {
        return new PolicyDecision(
            scope,
            subject,
            ToolPolicyDecisionStatus.Blocked,
            ToolRiskLevel.Blocked,
            ApprovalRequirement.HandOffRequired,
            reason);
    }
}
