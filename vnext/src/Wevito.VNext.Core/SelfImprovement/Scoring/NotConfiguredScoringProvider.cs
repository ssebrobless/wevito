namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed record NotConfiguredScoringProvider(KillSwitchService? KillSwitchService = null) : ILocalScoringProvider
{
    public const string EnabledSetting = "local_scoring_provider_enabled";

    public override LocalScoringResult Score(LocalScoringRequest request, CancellationToken cancellationToken)
    {
        if (KillSwitchService?.IsActive() == true)
        {
            return new LocalScoringResult.Refused("kill_switch=true");
        }

        return new LocalScoringResult.Refused("local_scoring_provider_not_configured");
    }
}
