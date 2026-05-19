namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public abstract record IScoringHttpClient
{
    protected IScoringHttpClient()
    {
    }

    public IScoringHttpClient(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public abstract Task<string> PostAsync(Uri uri, string body, CancellationToken cancellationToken);
}
