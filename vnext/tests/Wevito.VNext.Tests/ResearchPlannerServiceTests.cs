using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ResearchPlannerServiceTests
{
    [Fact]
    public void Plan_UsesLocalEvidenceWithoutHostedAiOrNetwork()
    {
        var service = new ResearchPlannerService();
        var request = new ResearchPlannerRequest(
            "How should Wevito research sprite workflow improvements?",
            LocalMemory: ["Prefer local-first research and reviewed examples."],
            LocalDocumentPaths: ["docs/POST_CPHASE62_INDEPENDENT_AI_ROADMAP_2026-05-12.md"],
            PriorToolReports: ["vnext/artifacts/pet-tasks/local-docs/run-summary.md"],
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));

        var packet = service.Plan(request);

        Assert.False(packet.DidUseHostedAi);
        Assert.False(packet.DidUseNetwork);
        Assert.Equal(3, packet.SourcesInspected.Count);
        Assert.Contains(packet.ClaimsExtracted, claim => claim.Claim.Contains("Local evidence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Hosted AI was not used", packet.Synthesis);
        Assert.Contains("report-only", packet.NextRecommendedAction);
    }

    [Fact]
    public void Plan_WhenNetworkRequested_RecordsPlaceholderWithoutFetching()
    {
        var service = new ResearchPlannerService();
        var request = new ResearchPlannerRequest(
            "Research recent local AI options",
            AllowNetwork: true,
            AllowHostedAi: true,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));

        var packet = service.Plan(request);

        Assert.False(packet.DidUseHostedAi);
        Assert.False(packet.DidUseNetwork);
        var web = Assert.Single(packet.SourcesInspected.Where(source => source.IsNetworkSource));
        Assert.False(web.WasFetched);
        Assert.Contains(packet.ClaimsExtracted, claim => claim.Claim.Contains("Hosted AI was requested", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("explicit approval", packet.NextRecommendedAction);
    }
}
