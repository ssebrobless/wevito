namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ConstitutionalDecisionService
{
    private readonly KillSwitchService? _killSwitchService;
    private readonly IReadOnlyList<ConstitutionalRule> _rules;
    private readonly DefaultDenyRule _defaultDenyRule;

    public ConstitutionalDecisionService(
        KillSwitchService? killSwitchService = null,
        IReadOnlyList<ConstitutionalRule>? rules = null,
        DefaultDenyRule? defaultDenyRule = null)
    {
        _killSwitchService = killSwitchService;
        _rules = rules ?? DefaultRules();
        _defaultDenyRule = defaultDenyRule ?? new DefaultDenyRule();
    }

    public ConstitutionalDecisionOutcome Decide(ConstitutionalDecisionInput input)
    {
        var effectiveInput = input with
        {
            KillSwitchActive = input.KillSwitchActive || (_killSwitchService?.IsActive() == true)
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
