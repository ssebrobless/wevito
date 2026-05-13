using System.Net;
using System.Net.Http;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalRuntimeProbeServiceTests
{
    [Fact]
    public async Task ProbeAsync_ReturnsAvailableWhenOllamaTagsResponds()
    {
        var handler = new RecordingHandler((request, _) =>
        {
            Assert.EndsWith("/api/tags", request.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"models":[{"name":"llama3.2:3b"}]}""")
            };
        });
        var service = new LocalRuntimeProbeService(new HttpClient(handler));

        var result = await service.ProbeAsync();

        Assert.True(result.IsAvailable);
        Assert.False(result.WasDormant);
        Assert.True(handler.WasCalled);
    }

    [Theory]
    [InlineData(RuntimeSupervisorMode.Quiet)]
    [InlineData(RuntimeSupervisorMode.PetOnly)]
    public async Task ProbeAsync_IsDormantInQuietOrPetOnly(RuntimeSupervisorMode mode)
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Probe should be dormant."));
        var service = new LocalRuntimeProbeService(new HttpClient(handler));

        var result = await service.ProbeAsync(runtimeStatus: new RuntimeSupervisorStatus(mode, false, false, false, "quiet", "quiet"));

        Assert.False(result.IsAvailable);
        Assert.True(result.WasDormant);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ProbeAsync_BlocksNonLocalhostEndpointBeforeHttp()
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Non-local endpoint should not be called."));
        var service = new LocalRuntimeProbeService(new HttpClient(handler));

        var result = await service.ProbeAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LocalRuntimeProbeService.OllamaEndpointSetting] = "https://api.example.com"
        });

        Assert.False(result.IsAvailable);
        Assert.Contains("localhost", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ProbeAsync_KillSwitchBlocks()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Kill switch should block before HTTP."));
        var service = new LocalRuntimeProbeService(new HttpClient(handler), killSwitch);

        var result = await service.ProbeAsync();

        Assert.False(result.IsAvailable);
        Assert.Equal("kill_switch=true", result.Reason);
        Assert.False(handler.WasCalled);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
