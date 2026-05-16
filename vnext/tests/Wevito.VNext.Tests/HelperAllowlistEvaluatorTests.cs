using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class HelperAllowlistEvaluatorTests
{
    [Theory]
    [InlineData(0, "goose 1", "localDocs")]
    [InlineData(1, "fox 1", "codeReview")]
    [InlineData(0, "goose 1", "spriteAudit")]
    [InlineData(0, "goose 1", "petState")]
    [InlineData(1, "fox 1", "codePatchPlan")]
    public void Evaluate_AllowsDeclaredReadOnlyAgentTools(int slotIndex, string helperName, string toolFamily)
    {
        var evaluator = new HelperAllowlistEvaluator();
        var helper = new PetHelperProfile(Guid.NewGuid(), helperName, slotIndex);

        var decision = evaluator.Evaluate(helper, toolFamily);

        Assert.True(decision.IsAllowed, decision.Reason);
    }

    [Fact]
    public void Evaluate_DenyWinsOverDeclaredAllow()
    {
        var evaluator = new HelperAllowlistEvaluator(
            helperDeny: new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Scout"] = new HashSet<string>(["localDocs"], StringComparer.OrdinalIgnoreCase)
            });
        var helper = new PetHelperProfile(Guid.NewGuid(), "Scout", 0);

        var decision = evaluator.Evaluate(helper, "localDocs");

        Assert.False(decision.IsAllowed);
        Assert.Contains("denied", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindLethalTrifectaViolations_DefaultMatrixHasNone()
    {
        var evaluator = new HelperAllowlistEvaluator();

        Assert.Empty(evaluator.FindLethalTrifectaViolations());
    }
}
