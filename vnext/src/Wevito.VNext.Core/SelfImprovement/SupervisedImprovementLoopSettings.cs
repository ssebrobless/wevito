namespace Wevito.VNext.Core.SelfImprovement;

public sealed record SupervisedImprovementLoopSettings(bool Enabled)
{
    public const string EnabledSetting = "supervised_improvement_loop_enabled";

    public static SupervisedImprovementLoopSettings FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        var enabled = settings.TryGetValue(EnabledSetting, out var raw) &&
                      bool.TryParse(raw, out var parsed) &&
                      parsed;
        return new SupervisedImprovementLoopSettings(enabled);
    }
}
