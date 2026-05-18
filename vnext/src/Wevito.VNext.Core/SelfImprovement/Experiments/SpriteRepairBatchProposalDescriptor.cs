namespace Wevito.VNext.Core.SelfImprovement.Experiments;

public static class SpriteRepairBatchProposalDescriptor
{
    public const string Kind = "sprite-repair-batch-proposal";
    public const string MutationPosture = "ReviewOnly";

    public static ExperimentDescriptor Descriptor { get; } = new(
        new ExperimentKind(Kind),
        "Sprite-repair batch proposal",
        "Review-only dry-run proposal for a guarded sprite repair batch. No sprite mutation. No apply.",
        EnabledByDefault: false);
}
