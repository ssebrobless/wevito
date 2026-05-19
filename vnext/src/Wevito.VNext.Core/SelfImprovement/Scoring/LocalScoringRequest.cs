namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed record LocalScoringRequest(string PromptSha256, string Rubric)
{
    public LocalScoringRequest(KillSwitchService killSwitchService)
        : this("", "")
    {
        _ = killSwitchService;
    }
}
