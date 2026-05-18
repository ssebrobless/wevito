using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ConstitutionalDecisionServiceTests
{
    [Fact]
    public void Decide_DefaultOutcomeIsBlocked()
    {
        var service = new ConstitutionalDecisionService(experimentRegistry: RegisteredRegistry());

        var outcome = service.Decide(Input(scopeEnabled: true, registryEmpty: false));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(DefaultDenyRule.DefaultDenyReason, blocked.Reason);
    }

    [Fact]
    public void Decide_KillSwitchWinsBeforeOtherRules()
    {
        var service = new ConstitutionalDecisionService();

        var outcome = service.Decide(Input(killSwitchActive: true, scopeEnabled: false, hostedAi: true, network: true));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(KillSwitchActiveRule.Reason, blocked.Reason);
    }

    [Fact]
    public void Decide_UsesKillSwitchService()
    {
        var settings = new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" };
        var service = new ConstitutionalDecisionService(new KillSwitchService(() => settings), RegisteredRegistry());

        var outcome = service.Decide(Input(scopeEnabled: true, registryEmpty: false));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(KillSwitchActiveRule.Reason, blocked.Reason);
    }

    [Fact]
    public void Decide_BlocksWhenScopeIsNotEnabled()
    {
        var service = new ConstitutionalDecisionService();

        var outcome = service.Decide(Input(scopeEnabled: false));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(ScopeMustBeEnabledRule.Reason, blocked.Reason);
    }

    [Fact]
    public void Decide_BlocksHostedAiBeforeNetwork()
    {
        var service = new ConstitutionalDecisionService();

        var outcome = service.Decide(Input(scopeEnabled: true, hostedAi: true, network: true));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(NoHostedAiRule.Reason, blocked.Reason);
    }

    [Fact]
    public void Decide_BlocksNetworkWhenScopeDoesNotAllowNetwork()
    {
        var service = new ConstitutionalDecisionService();

        var outcome = service.Decide(Input(scopeEnabled: true, network: true, scopeAllowsNetwork: false));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(NoNetworkInScopeRule.Reason, blocked.Reason);
    }

    [Fact]
    public void Decide_BlocksEmptyRegistryBeforeDefaultDeny()
    {
        var service = new ConstitutionalDecisionService();

        var outcome = service.Decide(Input(scopeEnabled: true, registryEmpty: true));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(DefaultDenyRule.EmptyRegistryReason, blocked.Reason);
    }

    [Fact]
    public void ShellCompositionRoot_CreatesDefaultDenyService()
    {
        var service = ShellCompositionRoot.CreateConstitutionalDecisionService();

        var outcome = service.Decide(Input(scopeEnabled: true, registryEmpty: false));

        var blocked = Assert.IsType<ConstitutionalDecisionOutcome.Blocked>(outcome);
        Assert.Equal(DefaultDenyRule.DefaultDenyReason, blocked.Reason);
    }

    [Fact]
    public void DefaultRules_DoNotContainAllowlistRule()
    {
        var rules = ConstitutionalDecisionService.DefaultRules();

        Assert.All(rules, rule => Assert.DoesNotContain("Allow", rule.GetType().Name, StringComparison.OrdinalIgnoreCase));
    }

    private static ConstitutionalDecisionInput Input(
        bool scopeEnabled = true,
        bool network = false,
        bool scopeAllowsNetwork = false,
        bool hostedAi = false,
        bool registryEmpty = true,
        bool killSwitchActive = false)
    {
        return new ConstitutionalDecisionInput(
            ScopeId: "sprite-repair-triage",
            ExperimentKind: "sprite-repair-batch-proposal",
            ScopeEnabled: scopeEnabled,
            RequestsNetwork: network,
            ScopeAllowsNetwork: scopeAllowsNetwork,
            RequestsHostedAi: hostedAi,
            ExperimentRegistryIsEmpty: registryEmpty,
            KillSwitchActive: killSwitchActive);
    }

    private static ExperimentRegistry RegisteredRegistry()
    {
        return ExperimentRegistry.ForTests(new ExperimentDescriptor(
            new ExperimentKind("sprite-repair-batch-proposal"),
            "Sprite repair batch proposal",
            "Review-only sprite repair proposal."));
    }
}
