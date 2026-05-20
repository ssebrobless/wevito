using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;
using Wevito.VNext.Core.SelfImprovement.Readiness;
using Wevito.VNext.Core.SelfImprovement.Replay;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class HeldOutEvalStoreVisibilityTests
{
    [Fact]
    public void HeldOutStore_IsNotReferencedByForbiddenSurfaces()
    {
        // ApplyRunnerPrerequisiteCheckService is the explicit C-PHASE 174 exception:
        // it may receive IHeldOutEvalStore only so ApplyRunnerPrerequisiteHeldOutAccessTests
        // can prove it calls ListCaseIds() and never reads held-out case contents.
        // EvalCoverageHealthService is the explicit C-PHASE 178 exception: it may receive
        // IHeldOutEvalStore only to count IDs through ListCaseIds(), never ReadCase().
        var forbiddenTypes = new[]
        {
            typeof(ToolRegistry),
            typeof(EvidenceSummaryService),
            typeof(AutonomousScopeService),
            typeof(OperationTimelineService),
            typeof(RefusedApprovalAggregateService),
            typeof(ApprovalCardDetailService),
            typeof(MaturityScoreboardService),
            typeof(InvariantViolationWatchdog),
            typeof(SupervisedImprovementLoop),
            typeof(CapabilityFlagAuditService),
            typeof(ReplayHarness),
            typeof(ReplayResultStore),
            typeof(ReplayResultSummary),
            typeof(ProposalDiffExplainerService),
            typeof(ProposalDiffExplanation),
            typeof(ApplyPrerequisiteExplainerService),
            typeof(ApplyPrerequisiteExplanation),
            typeof(LocalOllamaReadinessProbeService),
            typeof(SupervisedScoringDryRunService),
            typeof(ProposalQualityMetricsService),
            typeof(ProposalQualityMetricsSnapshot),
            typeof(ApplyRunnerStatusReportService),
            typeof(ApplyRunnerStatusReport),
            // C-PHASE 183: the narrow artifact-rename apply runner must stay blind to
            // held-out and in-distribution eval content; it only consumes approval metadata.
            typeof(ArtifactRenameApplyRunner),
            typeof(ApplyRequest),
            typeof(ApplyResult),
            typeof(SpriteRepairBatchProposalScope)
        };

        foreach (var type in forbiddenTypes)
        {
            var signatures = type.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)));

            Assert.DoesNotContain(signatures, signature => signature == typeof(IHeldOutEvalStore) || signature == typeof(HeldOutEvalStore));
        }
    }

    [Fact]
    public void HeldOutStore_KillSwitchPreventsListingAndReading()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-held-out-eval", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "secret-case.json"), "{\"case\":\"held-out\"}");
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });

        var store = new HeldOutEvalStore(root, killSwitch);

        Assert.Empty(store.ListCaseIds());
        Assert.Null(store.ReadCase("secret-case"));
    }

    [Fact]
    public void HeldOutStore_BlocksPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-held-out-eval", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = new HeldOutEvalStore(root);

        Assert.Null(store.ReadCase("..\\outside"));
    }
}
