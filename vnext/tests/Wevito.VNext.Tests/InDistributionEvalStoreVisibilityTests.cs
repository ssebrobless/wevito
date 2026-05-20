using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;
using Wevito.VNext.Core.SelfImprovement.Readiness;
using Wevito.VNext.Core.SelfImprovement.Replay;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class InDistributionEvalStoreVisibilityTests
{
    [Fact]
    public void InDistributionStore_IsNotReferencedByForbiddenSurfaces()
    {
        // EvalCoverageHealthService is the explicit C-PHASE 178 exception: it may receive
        // IInDistributionEvalStore only to count IDs through ListCaseIds(), never ReadCase().
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
            typeof(SpriteRepairBatchProposalScope)
        };

        foreach (var type in forbiddenTypes)
        {
            var signatures = type.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)));

            Assert.DoesNotContain(signatures, signature => signature == typeof(IInDistributionEvalStore) || signature == typeof(InDistributionEvalStore));
        }
    }
}
