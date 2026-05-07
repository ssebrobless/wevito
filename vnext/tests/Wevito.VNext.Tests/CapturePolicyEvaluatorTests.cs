using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CapturePolicyEvaluatorTests
{
    private readonly CapturePolicyEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_RequiresApprovalForWevitoWindowStillCapture()
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.WevitoWindow,
            CaptureTargetKind.WevitoWindow));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ToolRiskLevel.Medium, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.ActionTime, decision.ApprovalRequirement);
    }

    [Fact]
    public void Evaluate_BlocksProofSurfaceUntilDedicatedTargetExists()
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.ProofSurface,
            CaptureTargetKind.ProofSurface));

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Equal(ToolRiskLevel.Blocked, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.HandOffRequired, decision.ApprovalRequirement);
    }

    [Theory]
    [InlineData(CapturePreset.SelectedRegion, CaptureTargetKind.SelectedRegion)]
    [InlineData(CapturePreset.LastRegion, CaptureTargetKind.LastRegion)]
    [InlineData(CapturePreset.CurrentForegroundWindow, CaptureTargetKind.ForegroundWindow)]
    public void Evaluate_RequiresApprovalForUserOrAppScopeCapture(CapturePreset preset, CaptureTargetKind targetKind)
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            preset,
            targetKind,
            PrivacyLevel: CapturePrivacyLevel.SelectedRegion));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ToolRiskLevel.Medium, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.ActionTime, decision.ApprovalRequirement);
    }

    [Fact]
    public void Evaluate_RequiresHighRiskApprovalForFullDesktop()
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.FullDesktop,
            CaptureTargetKind.FullDesktop,
            PrivacyLevel: CapturePrivacyLevel.Desktop));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ToolRiskLevel.High, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.ActionTime, decision.ApprovalRequirement);
    }

    [Theory]
    [InlineData(CaptureOutputKind.ClipMp4)]
    [InlineData(CaptureOutputKind.ClipGif)]
    public void Evaluate_RequiresApprovalForRecordingOutputs(CaptureOutputKind outputKind)
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.ShortRecording,
            CaptureTargetKind.WevitoWindow,
            OutputKind: outputKind,
            IsRecording: true));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ToolRiskLevel.Medium, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.ActionTime, decision.ApprovalRequirement);
    }

    [Fact]
    public void Evaluate_BlocksExternalShareRequests()
    {
        var decision = _evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.WevitoWindow,
            CaptureTargetKind.WevitoWindow,
            PrivacyLevel: CapturePrivacyLevel.ExternalShare,
            IsExternalShareRequested: true));

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Equal(ToolRiskLevel.Blocked, decision.RiskLevel);
        Assert.Equal(ApprovalRequirement.HandOffRequired, decision.ApprovalRequirement);
        Assert.Contains("External upload/share", decision.Reason);
    }
}
