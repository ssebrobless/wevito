using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class HelperAllowlistEvaluatorTests
{
    [Theory]
    [InlineData(PetHelperRole.ResearchHelper, "Scout", "localDocs")]
    [InlineData(PetHelperRole.ResearchHelper, "Scout", "codeReview")]
    [InlineData(PetHelperRole.SpriteReviewHelper, "Inspector", "spriteAudit")]
    [InlineData(PetHelperRole.SpriteReviewHelper, "Inspector", "petState")]
    [InlineData(PetHelperRole.ChecklistHelper, "Builder", "codePatchPlan")]
    [InlineData(PetHelperRole.ChecklistHelper, "Builder", "codeReview")]
    public void Evaluate_AllowsDeclaredReadOnlyHelperTools(PetHelperRole role, string helperName, string toolFamily)
    {
        var evaluator = new HelperAllowlistEvaluator();
        var helper = new PetHelperProfile(Guid.NewGuid(), helperName, role);

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
        var helper = new PetHelperProfile(Guid.NewGuid(), "Scout", PetHelperRole.ResearchHelper);

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
