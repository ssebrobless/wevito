using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Tests;

public sealed class ExperimentRegistryTests
{
    [Fact]
    public void EmptyRegistry_IsEmpty()
    {
        var registry = ExperimentRegistry.Empty();

        Assert.Empty(registry.RegisteredKinds);
    }

    [Fact]
    public void CompositionRoot_RegistersReviewOnlyExperimentKinds()
    {
        var registry = ShellCompositionRoot.CreateExperimentRegistry();

        Assert.Contains(registry.RegisteredKinds, descriptor =>
            descriptor.Kind.Value == SpriteRepairBatchProposalDescriptor.Kind &&
            descriptor.EnabledByDefault == false);
        Assert.Contains(registry.RegisteredKinds, descriptor =>
            descriptor.Kind.Value == EvalCoverageProposalDescriptor.Kind &&
            descriptor.EnabledByDefault == false);
    }

    [Fact]
    public void EmptyRegistry_ForcesConstitutionalDecisionBlocked()
    {
        var service = new ConstitutionalDecisionService(experimentRegistry: ExperimentRegistry.Empty());

        var outcome = service.Decide(Input());

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(DefaultDenyRule.EmptyRegistryReason, blocked.Reason);
    }

    [Fact]
    public void ForTests_CanRegisterDescriptor()
    {
        var registry = ExperimentRegistry.ForTests(new ExperimentDescriptor(
            new ExperimentKind("demo"),
            "Demo",
            "Demo descriptor."));

        var descriptor = Assert.Single(registry.RegisteredKinds);
        Assert.Equal("demo", descriptor.Kind.Value);
        Assert.False(descriptor.EnabledByDefault);
    }

    [Fact]
    public void RegisterRejectsDuplicates()
    {
        var descriptor = new ExperimentDescriptor(new ExperimentKind("demo"), "Demo", "Demo descriptor.");

        var ex = Assert.Throws<InvalidOperationException>(() => ExperimentRegistry.ForTests(descriptor, descriptor));

        Assert.Contains("already registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ConstitutionalDecisionInput Input()
    {
        return new ConstitutionalDecisionInput(
            ScopeId: "sprite-repair-triage",
            ExperimentKind: "sprite-repair-batch-proposal",
            ScopeEnabled: true,
            RequestsNetwork: false,
            ScopeAllowsNetwork: false,
            RequestsHostedAi: false,
            ExperimentRegistryIsEmpty: false);
    }
}
