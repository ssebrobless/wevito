namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public abstract record LocalScoringResult
{
    protected LocalScoringResult()
    {
    }

    public LocalScoringResult(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public sealed record Refused(string Reason) : LocalScoringResult
    {
        public Refused(KillSwitchService killSwitchService)
            : this("")
        {
            _ = killSwitchService;
        }
    }

    public sealed record Scored(double Score, string Rubric, string ModelIdentity) : LocalScoringResult
    {
        public Scored(KillSwitchService killSwitchService)
            : this(0, "", "")
        {
            _ = killSwitchService;
        }
    }
}
