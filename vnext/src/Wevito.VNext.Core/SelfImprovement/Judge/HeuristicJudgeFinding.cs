namespace Wevito.VNext.Core.SelfImprovement.Judge;

public sealed record HeuristicJudgeFinding(
    HeuristicJudgeRule Rule,
    bool Passed,
    string EvidenceSummary)
{
    public HeuristicJudgeFinding(KillSwitchService killSwitchService)
        : this(new HeuristicJudgeRule("", ""), false, "")
    {
        _ = killSwitchService;
    }
}
