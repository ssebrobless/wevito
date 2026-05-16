using System.Net;
using System.Net.Http;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class OllamaModelBootstrapServiceTests
{
    [Fact]
    public async Task ProbeOnceOnStartupOnly()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new OllamaModelBootstrapService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger,
            modelExists: (_, _) => false,
            artifactRoot: TempArtifactRoot());

        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now);
        await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now.AddMinutes(1));

        Assert.Equal(1, handler.CallCount);
        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(2));
        Assert.Single(rows);
    }

    [Fact]
    public async Task EmitsBootstrapRequiredWhenModelMissing()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new OllamaModelBootstrapService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger,
            modelExists: (_, _) => false,
            artifactRoot: TempArtifactRoot());
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        var status = await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now);

        Assert.Equal(OllamaModelBootstrapService.BootstrapRequiredPacketKind, status.PacketKind);
        Assert.True(File.Exists(status.ArtifactPath));
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.Equal(OllamaModelBootstrapService.BootstrapRequiredPacketKind, row.PacketKind);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
        Assert.Contains(OllamaModelBootstrapService.PullInstruction, row.Summary);
    }

    [Fact]
    public async Task EmitsRuntimeAbsentWhenOllamaDown()
    {
        var handler = new RecordingHandler((_, _) => throw new HttpRequestException("down"));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var modelChecks = 0;
        var service = new OllamaModelBootstrapService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger,
            modelExists: (_, _) =>
            {
                modelChecks++;
                return true;
            },
            artifactRoot: TempArtifactRoot());
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        var status = await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now);

        Assert.Equal(OllamaModelBootstrapService.RuntimeAbsentPacketKind, status.PacketKind);
        Assert.Equal(0, modelChecks);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.Equal(OllamaModelBootstrapService.RuntimeAbsentPacketKind, row.PacketKind);
    }

    [Fact]
    public async Task RespectsKillSwitch()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Kill switch should block before HTTP."));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new OllamaModelBootstrapService(
            new LocalRuntimeProbeService(new HttpClient(handler), killSwitch),
            ledger,
            killSwitch,
            modelExists: (_, _) => true,
            artifactRoot: TempArtifactRoot());
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        var status = await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now);

        Assert.True(status.WasSkipped);
        Assert.False(handler.WasCalled);
        Assert.Empty(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    [Fact]
    public async Task SuccessPathEmitsNoBootstrapPacket()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new OllamaModelBootstrapService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger,
            modelExists: (_, _) => true,
            artifactRoot: TempArtifactRoot());
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        var status = await service.ProbeStartupAsync(new Dictionary<string, string>(), nowUtc: now);

        Assert.Equal("model_bootstrap_available", status.PacketKind);
        Assert.True(status.ModelPresent);
        Assert.Empty(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    private static string TempLedgerPath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-ollama-bootstrap-tests", Guid.NewGuid().ToString("N"), "ledger.sqlite");
    }

    private static string TempArtifactRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-ollama-bootstrap-tests", Guid.NewGuid().ToString("N"), "artifacts");
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public bool WasCalled => CallCount > 0;
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
