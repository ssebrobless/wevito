using System.Net;
using System.Net.Http;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class OllamaLocalModelAdapterTests
{
    [Fact]
    public async Task SuggestAsync_FallsBackToDeterministicWhenEndpointUnreachable()
    {
        var handler = new RecordingHandler((_, _) => throw new HttpRequestException("offline"));
        var adapter = new OllamaLocalModelAdapter(
            new HttpClient(handler),
            settingsProvider: () => DefaultSettings());

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.False(response.DidCallProvider);
        Assert.Equal("ollama", response.Provider);
        Assert.Contains("deterministic-fallback", response.Model);
        Assert.Contains("No hosted model call was made", response.Summary);
        Assert.True(File.Exists(response.AuditLogPath));
        var packet = JsonSerializer.Deserialize<ModelInferenceEvidencePacket>(File.ReadAllText(response.AuditLogPath), JsonDefaults.Options);
        Assert.NotNull(packet);
        Assert.False(packet.DidUseLocalModel);
        Assert.True(packet.DidFallbackToDeterministic);
        Assert.False(packet.DidUseNetwork);
        Assert.False(packet.DidUseHostedAi);
    }

    [Fact]
    public async Task SuggestAsync_RecordsEvidenceForSuccessfulLocalCall()
    {
        var handler = new RecordingHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"models":[{"name":"llama3.2:3b"}]}""")
                };
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/v1/chat/completions", request.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"choices":[{"message":{"role":"assistant","content":"Use the local evidence packet first."}}]}""")
            };
        });
        var adapter = new OllamaLocalModelAdapter(new HttpClient(handler), settingsProvider: () => DefaultSettings());

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.True(response.DidCallProvider);
        Assert.Equal("Use the local evidence packet first.", response.Summary);
        Assert.True(handler.CalledUris.All(uri => uri.IsLoopback));
        var packet = JsonSerializer.Deserialize<ModelInferenceEvidencePacket>(File.ReadAllText(response.AuditLogPath), JsonDefaults.Options);
        Assert.NotNull(packet);
        Assert.Equal("ollama", packet.Provider);
        Assert.Equal("llama3.2:3b", packet.Model);
        Assert.Equal("ollama", packet.RuntimeId);
        Assert.NotEmpty(packet.PromptSha256);
        Assert.NotEmpty(packet.ResponseSha256);
        Assert.True(packet.DidUseLocalModel);
        Assert.False(packet.DidUseNetwork);
        Assert.False(packet.DidUseHostedAi);
    }

    [Fact]
    public async Task SuggestAsync_DoesNotCallHostedEndpointWhenConfiguredEndpointIsRemote()
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Remote endpoint must not be called."));
        var adapter = new OllamaLocalModelAdapter(
            new HttpClient(handler),
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [LocalRuntimeProbeService.OllamaEndpointSetting] = "https://api.openai.com",
                [LocalRuntimeProbeService.OllamaModelSetting] = "llama3.2:3b"
            });

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.False(response.DidCallProvider);
        Assert.Contains("localhost", response.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(handler.CalledUris);
    }

    [Fact]
    public async Task SuggestAsync_KillSwitchBlocks()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Kill switch must block before HTTP."));
        var adapter = new OllamaLocalModelAdapter(
            new HttpClient(handler),
            settingsProvider: () => DefaultSettings(),
            killSwitchService: killSwitch);

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.False(response.DidCallProvider);
        Assert.Equal("kill_switch=true", response.BlockReason);
        Assert.Empty(handler.CalledUris);
    }

    private static ModelRequest BuildRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-ollama-local-model-tests", Guid.NewGuid().ToString("N"));
        return new ModelRequest(
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            "Scout",
            PetHelperRole.ResearchHelper,
            "localResearch",
            "research local docs",
            "localResearch preview ready",
            TrustedContext: ["docs/plan.md"],
            UntrustedContext: ["user task"],
            ApprovedForModelCall: true,
            ArtifactRoot: root,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static IReadOnlyDictionary<string, string> DefaultSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LocalRuntimeProbeService.OllamaEndpointSetting] = LocalRuntimeProbeService.DefaultOllamaEndpoint,
            [LocalRuntimeProbeService.OllamaModelSetting] = LocalRuntimeProbeService.DefaultOllamaModel
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Uri> CalledUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
            {
                CalledUris.Add(request.RequestUri);
            }

            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
