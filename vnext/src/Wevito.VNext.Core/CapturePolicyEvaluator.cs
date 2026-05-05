using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CapturePolicyEvaluator
{
    public CapturePolicyDecision Evaluate(CaptureRequest request)
    {
        if (request.IsExternalShareRequested || request.PrivacyLevel == CapturePrivacyLevel.ExternalShare)
        {
            return Decision(
                request,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                "External upload/share is blocked for capture artifacts.");
        }

        if (request.IsRecording || request.OutputKind is CaptureOutputKind.ClipMp4 or CaptureOutputKind.ClipGif)
        {
            return Decision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Screen recording requires explicit approval before capture starts.");
        }

        return request.TargetKind switch
        {
            CaptureTargetKind.WevitoWindow or CaptureTargetKind.ProofSurface => Decision(
                request,
                ToolPolicyDecisionStatus.Allowed,
                ToolRiskLevel.Low,
                ApprovalRequirement.None,
                "Wevito-only still capture is allowed as a low-risk local proof artifact."),

            CaptureTargetKind.SelectedRegion or CaptureTargetKind.LastRegion => Decision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Selected-region capture may include private screen content and requires approval."),

            CaptureTargetKind.ForegroundWindow => Decision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.ActionTime,
                "Foreground-window capture may include another application and requires approval."),

            CaptureTargetKind.FullDesktop => Decision(
                request,
                ToolPolicyDecisionStatus.ApprovalRequired,
                ToolRiskLevel.High,
                ApprovalRequirement.ActionTime,
                "Full-desktop capture may include private content and requires approval."),

            _ => Decision(
                request,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                $"Capture target \"{request.TargetKind}\" is not supported.")
        };
    }

    private static CapturePolicyDecision Decision(
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
}
