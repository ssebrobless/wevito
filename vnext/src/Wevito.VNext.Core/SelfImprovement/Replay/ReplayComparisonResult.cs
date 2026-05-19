namespace Wevito.VNext.Core.SelfImprovement.Replay;

public abstract record ReplayComparisonResult
{
    public ReplayComparisonResult()
    {
    }

    public ReplayComparisonResult(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public sealed record Identical(int PacketCount) : ReplayComparisonResult
    {
        public Identical(KillSwitchService killSwitchService)
            : this(0)
        {
            _ = killSwitchService;
        }
    }

    public sealed record Diverged(IReadOnlyList<string> Diffs) : ReplayComparisonResult
    {
        public Diverged(KillSwitchService killSwitchService)
            : this([])
        {
            _ = killSwitchService;
        }
    }

    public sealed record NotApplicable(string Reason) : ReplayComparisonResult
    {
        public NotApplicable(KillSwitchService killSwitchService)
            : this("kill_switch=true")
        {
            _ = killSwitchService;
        }
    }
}
