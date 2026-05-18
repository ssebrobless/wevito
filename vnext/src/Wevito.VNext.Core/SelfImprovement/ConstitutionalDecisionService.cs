namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ConstitutionalDecisionService
{
    private readonly KillSwitchService? _killSwitchService;
    private readonly ExperimentRegistry _experimentRegistry;
    private readonly IReadOnlyList<ConstitutionalRule> _rules;
    private readonly DefaultDenyRule _defaultDenyRule;

    public ConstitutionalDecisionService(
        KillSwitchService? killSwitchService = null,
        ExperimentRegistry? experimentRegistry = null,
        IReadOnlyList<ConstitutionalRule>? rules = null,
        DefaultDenyRule? defaultDenyRule = null)
    {
        _killSwitchService = killSwitchService;
        _experimentRegistry = experimentRegistry ?? ExperimentRegistry.Empty();
        _rules = rules ?? DefaultRules();
        _defaultDenyRule = defaultDenyRule ?? new DefaultDenyRule();
    }

    public ConstitutionalDecisionOutcome Decide(ConstitutionalDecisionInput input)
    {
        var effectiveInput = input with
        {
            KillSwitchActive = input.KillSwitchActive || (_killSwitchService?.IsActive() == true),
            ExperimentRegistryIsEmpty = _experimentRegistry.RegisteredKinds.Count == 0
        };

        foreach (var rule in _rules)
        {
            var outcome = rule.Evaluate(effectiveInput);
            if (outcome is not null)
            {
                return outcome;
            }
        }

        return _defaultDenyRule.Evaluate(effectiveInput);
    }

    public static IReadOnlyList<ConstitutionalRule> DefaultRules()
    {
        return
        [
            new KillSwitchActiveRule(),
            new ScopeMustBeEnabledRule(),
            new NoHostedAiRule(),
            new NoNetworkInScopeRule()
        ];
    }
}
