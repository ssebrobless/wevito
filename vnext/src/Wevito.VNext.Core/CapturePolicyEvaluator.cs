using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CapturePolicyEvaluator
{
    private readonly UnifiedPolicyService _unifiedPolicyService;

    public CapturePolicyEvaluator(UnifiedPolicyService? unifiedPolicyService = null)
    {
        _unifiedPolicyService = unifiedPolicyService ?? new UnifiedPolicyService();
    }

    public CapturePolicyDecision Evaluate(CaptureRequest request)
    {
        return _unifiedPolicyService.EvaluateCapture(request);
    }
}
