namespace Wevito.VNext.Core.SelfImprovement;

public readonly record struct ExperimentKind(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}
