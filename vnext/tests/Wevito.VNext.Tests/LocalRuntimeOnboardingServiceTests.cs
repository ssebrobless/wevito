using System.Net;
using System.Net.Http;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalRuntimeOnboardingServiceTests
{
    [Fact]
    public void InstallPlan_DoesNotRunCommands()
    {
        var commandProbeCalls = 0;
        var modelProbeCalls = 0;
        var service = new LocalRuntimeOnboardingService(
            commandExists: _ =>
            {
                commandProbeCalls++;
                return true;
            },
            modelExists: (_, _) =>
            {
                modelProbeCalls++;
                return true;
            });

        var plan = service.InstallPlan(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LocalRuntimeProbeService.OllamaModelSetting] = "llama3.2:3b"
        });

        Assert.Equal(4, plan.Count);
        Assert.Equal(0, commandProbeCalls);
        Assert.Equal(0, modelProbeCalls);
        Assert.Contains(plan, step => step.Command.Contains("winget list Ollama.Ollama --exact", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(plan, step => step.Command.Contains("ollama pull llama3.2:3b", StringComparison.OrdinalIgnoreCase) && step.RequiresInteractiveApproval);
    }

    [Fact]
    public async Task StatusAsync_ReturnsStatusAndWritesRuntimeOnboardingLedgerRow()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"models":[{"name":"qwen2.5:7b-instruct-q4_K_M"}]}""")
        });
        var ledgerPath = Path.Combine(Path.GetTempPath(), "wevito-onboarding-tests", Guid.NewGuid().ToString("N"), "ledger.sqlite");
        var ledger = new AuditLedgerService(ledgerPath);
        var service = new LocalRuntimeOnboardingService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger,
            commandExists: command => command == "ollama",
            modelExists: (_, model) => model == LocalRuntimeProbeService.DefaultOllamaModel);
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var status = await service.StatusAsync(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            runtimeStatus: null,
            now);

        Assert.True(status.Installed);
        Assert.True(status.ModelPresent);
        Assert.True(status.EndpointReachable);
        Assert.True(handler.WasCalled);
        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1));
        var row = Assert.Single(rows);
        Assert.Equal("runtime_onboarding", row.PacketKind);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public async Task StatusAsync_ProbeIsDormantInQuietMode()
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Quiet mode must not call HTTP."));
        var service = new LocalRuntimeOnboardingService(new LocalRuntimeProbeService(new HttpClient(handler)));

        var status = await service.StatusAsync(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Quiet, false, false, false, "quiet", "quiet"),
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(status.EndpointReachable);
        Assert.Contains("dormant", status.Reason, StringComparison.OrdinalIgnoreCase);
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
