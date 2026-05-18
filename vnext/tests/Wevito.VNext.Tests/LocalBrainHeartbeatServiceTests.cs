using System.Net;
using System.Net.Http;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalBrainHeartbeatServiceTests
{
    [Fact]
    public async Task EmitsHeartbeatPacketOncePerTenMinutes()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new LocalBrainHeartbeatService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        var first = await service.TickAsync(new Dictionary<string, string>(), ActiveStatus(), now);
        var second = await service.TickAsync(new Dictionary<string, string>(), ActiveStatus(), now.AddSeconds(61));

        Assert.Equal(LocalBrainAvailability.Ready, first.Availability);
        Assert.Equal(LocalBrainAvailability.Ready, second.Availability);
        Assert.Equal(2, handler.CallCount);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(2)));
        Assert.Equal(LocalBrainHeartbeatService.HeartbeatPacketKind, row.PacketKind);
        Assert.True(row.DidUseLocalModel);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public async Task RespectsKillSwitchBeforeProbeOrWrite()
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Kill switch should block before HTTP."));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new LocalBrainHeartbeatService(
            new LocalRuntimeProbeService(new HttpClient(handler), killSwitch),
            ledger,
            killSwitch);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        var status = await service.TickAsync(new Dictionary<string, string>(), ActiveStatus(), now);

        Assert.Equal(LocalBrainAvailability.Blocked, status.Availability);
        Assert.False(handler.WasCalled);
        Assert.Empty(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    [Fact]
    public async Task DormantRuntimeDoesNotClaimLocalModelUse()
    {
        var handler = new RecordingHandler((_, _) => throw new InvalidOperationException("Quiet mode should block before HTTP."));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new LocalBrainHeartbeatService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        var status = await service.TickAsync(new Dictionary<string, string>(), QuietStatus(), now);

        Assert.Equal(LocalBrainAvailability.Dormant, status.Availability);
        Assert.False(handler.WasCalled);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.False(row.DidUseLocalModel);
        Assert.Equal("Dormant", row.Status);
    }

    [Fact]
    public async Task OfflineRuntimeRecordsNoLocalModelUse()
    {
        var handler = new RecordingHandler((_, _) => throw new HttpRequestException("offline"));
        var ledger = new AuditLedgerService(TempLedgerPath());
        var service = new LocalBrainHeartbeatService(
            new LocalRuntimeProbeService(new HttpClient(handler)),
            ledger);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        var status = await service.TickAsync(new Dictionary<string, string>(), ActiveStatus(), now);

        Assert.Equal(LocalBrainAvailability.Offline, status.Availability);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidUseHostedAi);
    }

    private static RuntimeSupervisorStatus ActiveStatus() => new(
        RuntimeSupervisorMode.Active,
        BackgroundWorkAllowed: true,
        ToolWindowAllowed: true,
        IsQuietedForFullscreen: false,
        UserStatus: "active",
        BlockReason: "");

    private static RuntimeSupervisorStatus QuietStatus() => new(
        RuntimeSupervisorMode.Quiet,
        BackgroundWorkAllowed: false,
        ToolWindowAllowed: false,
        IsQuietedForFullscreen: false,
        UserStatus: "quiet",
        BlockReason: "quiet");

    private static string TempLedgerPath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-local-brain-heartbeat-tests", Guid.NewGuid().ToString("N"), "ledger.sqlite");
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
