namespace Wevito.VNext.Core.SelfImprovement.Readiness;

public sealed record LocalOllamaReadinessSnapshot(
    string Endpoint,
    string ConfiguredModel,
    bool ProbeRan,
    bool LoopbackReachable,
    bool ConfiguredModelPresent,
    DateTimeOffset ProbedAtUtc,
    string Reason)
{
    public LocalOllamaReadinessSnapshot(KillSwitchService killSwitchService)
        : this("", "", false, false, false, DateTimeOffset.MinValue, "")
    {
        _ = killSwitchService;
    }
}
