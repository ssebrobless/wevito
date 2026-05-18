namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record EvalGateManifest(IReadOnlyList<string> Gates)
{
    public const string Build = "Build";
    public const string UnitTests = "Unit tests";
    public const string BenchmarkSuite = "Benchmark suite";
    public const string InDistributionEval = "In-distribution eval";
    public const string HeldOutEval = "Held-out eval";
    public const string Performance = "Performance";
    public const string ScopeHash = "Scope hash";
    public const string DryRun = "Dry-run";
    public const string Backup = "Backup";
    public const string PostProof = "Post-proof";
    public const string Rollback = "Rollback";

    public static EvalGateManifest Default()
    {
        return new EvalGateManifest([
            Build,
            UnitTests,
            BenchmarkSuite,
            InDistributionEval,
            HeldOutEval,
            Performance,
            ScopeHash,
            DryRun,
            Backup,
            PostProof,
            Rollback
        ]);
    }
}
