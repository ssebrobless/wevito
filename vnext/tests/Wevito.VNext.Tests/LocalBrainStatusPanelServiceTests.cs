using System.Net;
using System.Net.Http;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalBrainStatusPanelServiceTests
{
    [Fact]
    public async Task LocalBrainStatusPanel_RespectsKillSwitch()
    {
        using var temp = new TempDirectory();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var ledger = CreateLedger(temp.Path);
        var killSwitch = new KillSwitchService(() => settings, ledger);
        var handler = new RecordingHandler();
        var service = CreateService(ledger, killSwitch, handler);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:00Z");

        var shown = service.Show(settings, ActiveSupervisor(), now);
        var copied = service.CopyCommand("serve-ollama", now.AddSeconds(1));
        var refreshed = await service.RefreshAsync(settings, ActiveSupervisor(), now.AddSeconds(2));

        Assert.Equal(LocalBrainAvailability.Blocked, shown.Status.Availability);
        Assert.Equal(LocalBrainAvailability.Blocked, refreshed.Status.Availability);
        Assert.False(copied.Success);
        Assert.Equal(0, handler.SendCount);
        Assert.Empty(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    [Fact]
    public async Task LocalBrainStatusPanel_RefreshIsRateLimited()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var handler = new RecordingHandler();
        var service = CreateService(ledger, killSwitchService: null, handler);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:00Z");

        var first = await service.RefreshAsync(new Dictionary<string, string>(), ActiveSupervisor(), now);
        var second = await service.RefreshAsync(new Dictionary<string, string>(), ActiveSupervisor(), now.AddSeconds(2));

        Assert.False(first.RefreshRateLimited);
        Assert.True(second.RefreshRateLimited);
        Assert.Equal(1, handler.SendCount);
    }

    [Fact]
    public void LocalBrainStatusPanel_CopyCommand_RecordsHonestPacket()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var service = CreateService(ledger);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:00Z");

        var result = service.CopyCommand("pull-qwen", now);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));

        Assert.True(result.Success);
        Assert.Equal("ollama pull qwen3:8b", result.Command);
        Assert.Equal(LocalBrainStatusPanelService.SetupInstructionCopiedPacketKind, row.PacketKind);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void LocalBrainStatusPanel_DoesNotInvokeOllamaProcess()
    {
        using var temp = new TempDirectory();
        var ledger = CreateLedger(temp.Path);
        var service = CreateService(ledger);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:00Z");

        var result = service.CopyCommand("serve-ollama", now);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));

        Assert.True(result.Success);
        Assert.Equal("ollama serve", result.Command);
        Assert.False(row.DidMutate);
        Assert.False(row.DidUseNetwork);
        Assert.All(LocalBrainStatusPanelService.SetupCommands, command =>
        {
            Assert.DoesNotContain("Start-Process", command.Command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Invoke-Expression", command.Command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("curl", command.Command, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(LocalBrainStatusPanelService.StatusPanelShownPacketKind)]
    [InlineData(LocalBrainStatusPanelService.SetupInstructionCopiedPacketKind)]
    public void PlainLanguageExplainer_KnowsStatusPanelAndCopyPackets(string packetKind)
    {
        var text = new PlainLanguageExplainer().ExplainPacketKind(packetKind);

        Assert.Contains(packetKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains("local brain", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    private static LocalBrainStatusPanelService CreateService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        RecordingHandler? handler = null)
    {
        var probe = new LocalRuntimeProbeService(
            new HttpClient(handler ?? new RecordingHandler()) { Timeout = TimeSpan.FromSeconds(2) },
            killSwitchService);
        var heartbeat = new LocalBrainHeartbeatService(probe, ledger, killSwitchService);
        return new LocalBrainStatusPanelService(heartbeat, probe, ledger, killSwitchService);
    }

    private static AuditLedgerService CreateLedger(string root)
    {
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static RuntimeSupervisorStatus ActiveSupervisor()
    {
        return new RuntimeSupervisorStatus(
            RuntimeSupervisorMode.Active,
            BackgroundWorkAllowed: true,
            ToolWindowAllowed: true,
            IsQuietedForFullscreen: false,
            UserStatus: "Active",
            BlockReason: "");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"models":[]}""")
            });
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wevito-local-brain-panel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch (IOException)
                {
                    // SQLite can briefly hold a pooled file handle after assertions; the temp folder is safe to leave.
                }
            }
        }
    }
}
