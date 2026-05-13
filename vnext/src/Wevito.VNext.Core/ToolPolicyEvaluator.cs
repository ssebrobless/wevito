using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ToolPolicyEvaluator
{
    private readonly UnifiedPolicyService _unifiedPolicyService;

    public ToolPolicyEvaluator(UnifiedPolicyService? unifiedPolicyService = null)
    {
        _unifiedPolicyService = unifiedPolicyService ?? new UnifiedPolicyService();
    }

    public ToolPolicyDecision Evaluate(TaskIntent intent, IReadOnlyList<ToolPolicy> policies)
    {
        return _unifiedPolicyService.EvaluateToolPolicy(intent, policies);
    }
}
