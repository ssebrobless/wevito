namespace Wevito.VNext.Core.SelfImprovement.Judge;

public sealed record HeuristicJudgeRule(string Id, string Description)
{
    public HeuristicJudgeRule(KillSwitchService killSwitchService)
        : this("", "")
    {
        _ = killSwitchService;
    }
}
