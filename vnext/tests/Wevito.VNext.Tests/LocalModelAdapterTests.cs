using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalModelAdapterTests
{
    [Fact]
    public async Task SuggestAsync_ProducesDeterministicNoProviderSummary()
    {
        var adapter = new LocalModelAdapter();
        var request = new ModelRequest(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Scout",
            "ResearchAgent",
            "localResearch",
            "research local sprite workflow options",
            "No tool output yet.",
            TrustedContext: ["docs/C_PHASE62_FULL_EXPANDED_VISION_REASSESSMENT_2026-05-12.md"],
            UntrustedContext: ["user pasted task"],
            ApprovedForModelCall: false);

        var response = await adapter.SuggestAsync(request);

        Assert.Equal("local", response.Provider);
        Assert.False(response.DidCallProvider);
        Assert.Contains("No hosted model call was made", response.Summary);
        Assert.Contains("trusted=1", response.Summary);
        Assert.Contains("untrusted=1", response.Summary);
    }
}
