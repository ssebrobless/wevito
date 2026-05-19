namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public abstract record ILocalScoringProvider
{
    protected ILocalScoringProvider()
    {
    }

    public ILocalScoringProvider(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public abstract LocalScoringResult Score(LocalScoringRequest request, CancellationToken cancellationToken);
}
