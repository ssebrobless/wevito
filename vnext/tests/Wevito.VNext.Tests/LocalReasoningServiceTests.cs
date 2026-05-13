using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalReasoningServiceTests
{
    [Fact]
    public async Task LocalReasoning_UsesDeterministicAdapterWhenLocalModelUnavailable()
    {
        var root = NewTempDirectory();
        var service = new LocalReasoningService(new LocalModelAdapter());

        var result = await service.ReasonAsync(BuildRequest(root));

        Assert.True(result.Succeeded);
        Assert.False(result.Packet.DidUseHostedAi);
        Assert.False(result.Packet.DidUseNetwork);
        Assert.False(result.Packet.DidMutate);
        Assert.False(result.Packet.DidUseLocalModel);
        Assert.True(File.Exists(result.PacketPath));
    }

    [Fact]
    public async Task LocalReasoning_RefusesHostedAdapterEvenWhenInjected()
    {
        var service = new LocalReasoningService(new StubModelAdapter(new ModelResponse("anthropic", "claude", "Hosted answer [1].", DidCallProvider: true)));

        var result = await service.ReasonAsync(BuildRequest(NewTempDirectory()));

        Assert.False(result.Succeeded);
        Assert.Contains("Hosted", result.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Packet.DidUseHostedAi);
    }

    [Fact]
    public async Task LocalReasoning_EnforcesCitationsAndComputesCoverage()
    {
        var service = new LocalReasoningService(new StubModelAdapter(new ModelResponse("local", "fixture", "First claim [1]. Missing claim. Second claim [2].", DidCallProvider: false)));

        var result = await service.ReasonAsync(BuildRequest(NewTempDirectory()));

        Assert.True(result.Succeeded);
        Assert.Contains("First claim [1].", result.Synthesis);
        Assert.Contains("(needs citation)", result.Synthesis);
        Assert.Equal(2d / 3d, result.CitationCoverageRatio, precision: 6);
        Assert.Equal(result.CitationCoverageRatio, result.Packet.CitationCoverageRatio);
        Assert.Equal(["chunk-1", "chunk-2"], result.Packet.RetrievedChunkIds);
    }

    [Fact]
    public async Task LocalReasoning_EmptyRetrievalReturnsNoLocalEvidence()
    {
        var request = BuildRequest(NewTempDirectory()) with
        {
            Retrieved = new RetrievalResult([], new Dictionary<string, RetrievalScore>(), [], DateTimeOffset.Parse("2026-05-13T12:00:00Z"))
        };

        var result = await new LocalReasoningService(new ThrowingModelAdapter()).ReasonAsync(request);

        Assert.True(result.Succeeded);
        Assert.Contains("No local evidence", result.Synthesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.CitationCoverageRatio);
    }

    [Fact]
    public async Task LocalReasoning_KillSwitchBlocks()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new LocalReasoningService(new ThrowingModelAdapter(), killSwitchService: killSwitch);

        var result = await service.ReasonAsync(BuildRequest(NewTempDirectory()));

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.BlockReason);
    }

    private static LocalReasoningRequest BuildRequest(string root)
    {
        var chunks = new[]
        {
            new RetrievalChunk("chunk-1", "doc", "docs/goose.md", "sha1", "Goose pond care matters.", 0, 24, [1, 0, 0, 0], DateTimeOffset.Parse("2026-05-13T12:00:00Z")),
            new RetrievalChunk("chunk-2", "doc", "docs/snake.md", "sha2", "Snake habitats need heat.", 25, 49, [0, 1, 0, 0], DateTimeOffset.Parse("2026-05-13T12:00:00Z"))
        };
        return new LocalReasoningRequest(
            Guid.Parse("81000000-0000-0000-0000-000000000001"),
            "How should Wevito summarize care?",
            new RetrievalResult(chunks, new Dictionary<string, RetrievalScore>(), ["fixture"], DateTimeOffset.Parse("2026-05-13T12:00:00Z")),
            PetHelperRole.ResearchHelper,
            "localResearch",
            TrustedContext: ["trusted"],
            UntrustedContext: ["user text"],
            ArtifactRoot: root,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-local-reasoning-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StubModelAdapter(ModelResponse response) : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            Assert.All(request.UntrustedContext ?? [], item => Assert.Contains("<untrusted", item, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingModelAdapter : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Adapter should not be called.");
        }
    }
}
