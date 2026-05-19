namespace Wevito.VNext.Core.SelfImprovement.Experiments;

public sealed class EvalCoverageProposalDescriptor
{
    public const string Kind = "eval-coverage-proposal";
    public const string MutationPosture = "ReviewOnly";

    public EvalCoverageProposalDescriptor(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public static ExperimentDescriptor Descriptor { get; } = new(
        new ExperimentKind(Kind),
        "Eval-coverage proposal",
        "Review-only dry-run proposal listing eval gates with no Passed eval_completed packet in the last 30 days. No mutation. No apply.",
        EnabledByDefault: false);
}
