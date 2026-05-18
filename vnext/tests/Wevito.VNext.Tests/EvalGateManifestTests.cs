using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class EvalGateManifestTests
{
    [Fact]
    public void DefaultManifest_ListsAllElevenGates()
    {
        var manifest = EvalGateManifest.Default();

        Assert.Equal(11, manifest.Gates.Count);
        Assert.Contains(EvalGateManifest.Build, manifest.Gates);
        Assert.Contains(EvalGateManifest.UnitTests, manifest.Gates);
        Assert.Contains(EvalGateManifest.BenchmarkSuite, manifest.Gates);
        Assert.Contains(EvalGateManifest.InDistributionEval, manifest.Gates);
        Assert.Contains(EvalGateManifest.HeldOutEval, manifest.Gates);
        Assert.Contains(EvalGateManifest.Performance, manifest.Gates);
        Assert.Contains(EvalGateManifest.ScopeHash, manifest.Gates);
        Assert.Contains(EvalGateManifest.DryRun, manifest.Gates);
        Assert.Contains(EvalGateManifest.Backup, manifest.Gates);
        Assert.Contains(EvalGateManifest.PostProof, manifest.Gates);
        Assert.Contains(EvalGateManifest.Rollback, manifest.Gates);
    }

    [Fact]
    public void EvalGateResult_OnlyAllowsPassedFailedOrNotApplicable()
    {
        var resultTypes = typeof(EvalGateResult).GetNestedTypes()
            .Where(type => type.IsAssignableTo(typeof(EvalGateResult)))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(["Failed", "NotApplicable", "Passed"], resultTypes);
    }

    [Fact]
    public void RunnerPreview_DoesNotRunActualGatesYet()
    {
        var results = new EvalGateRunner().Preview();

        Assert.Equal(11, results.Count);
        Assert.All(results.Values, result =>
        {
            var notApplicable = Assert.IsType<EvalGateResult.NotApplicable>(result);
            Assert.Equal("no_eval_run_wired_v0", notApplicable.Reason);
        });
    }

    [Fact]
    public void RunnerPreview_KillSwitchReportsNotApplicableWithoutReadingHeldOutStore()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var store = new ThrowingHeldOutEvalStore();

        var results = new EvalGateRunner(heldOutEvalStore: store, killSwitchService: killSwitch).Preview();

        Assert.All(results.Values, result =>
        {
            var notApplicable = Assert.IsType<EvalGateResult.NotApplicable>(result);
            Assert.Equal("kill_switch=true", notApplicable.Reason);
        });
    }

    private sealed class ThrowingHeldOutEvalStore : IHeldOutEvalStore
    {
        public IReadOnlyList<string> ListCaseIds()
        {
            throw new InvalidOperationException("Held-out store should not be read in C-PHASE 147.");
        }

        public string? ReadCase(string caseId)
        {
            throw new InvalidOperationException("Held-out store should not be read in C-PHASE 147.");
        }
    }
}
