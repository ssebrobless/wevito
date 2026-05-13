using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class LiveStatusFeedTests
{
    [Fact]
    public void BuildDaily_ReturnsEmptySnapshotWhenLedgerIsEmpty()
    {
        var service = new LiveStatusFeed(new AuditLedgerService(CreateLedgerPath()));
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var snapshot = service.BuildDaily(now, new Dictionary<string, string>());

        Assert.True(snapshot.IsEmpty);
        Assert.Equal("", snapshot.LastPacketKind);
        Assert.Equal(0, snapshot.TodayCounts.Previews);
        Assert.Equal("Last action: none yet · today: 0 previews, 0 approvals, 0 mutations", service.FormatBanner(snapshot));
    }

    [Fact]
    public void BuildDaily_CountsOnlyCurrentDay()
    {
        var ledger = new AuditLedgerService(CreateLedgerPath());
        ledger.Record(Packet("localDocs", "Draft", DateTimeOffset.Parse("2026-05-12T23:59:00Z")));
        ledger.Record(Packet("scheduler_proposal", "Draft", DateTimeOffset.Parse("2026-05-13T09:00:00Z")));
        ledger.Record(Packet("translateText", "Approved", DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
        ledger.Record(Packet("mutation_apply", "Completed", DateTimeOffset.Parse("2026-05-13T11:00:00Z"), mutate: true));
        var service = new LiveStatusFeed(ledger);

        var snapshot = service.BuildDaily(DateTimeOffset.Parse("2026-05-13T12:00:00Z"), new Dictionary<string, string>());

        Assert.False(snapshot.IsEmpty);
        Assert.Equal("mutation_apply", snapshot.LastPacketKind);
        Assert.Equal(1, snapshot.TodayCounts.Previews);
        Assert.Equal(1, snapshot.TodayCounts.Approvals);
        Assert.Equal(1, snapshot.TodayCounts.Mutations);
    }

    [Fact]
    public void Build_CanSummarizeMultiDayWindow()
    {
        var ledger = new AuditLedgerService(CreateLedgerPath());
        ledger.Record(Packet("localDocs", "Draft", DateTimeOffset.Parse("2026-05-12T08:00:00Z")));
        ledger.Record(Packet("web_fetch", "Completed", DateTimeOffset.Parse("2026-05-13T08:00:00Z"), network: true));
        var service = new LiveStatusFeed(ledger);

        var snapshot = service.Build(
            DateTimeOffset.Parse("2026-05-12T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-13T23:59:59Z"));

        Assert.Equal("web_fetch", snapshot.LastPacketKind);
        Assert.Equal(1, snapshot.TodayCounts.Previews);
        Assert.Equal(1, snapshot.TodayCounts.NetworkUses);
        Assert.Single(snapshot.FlaggedRows);
    }

    [Fact]
    public void BuildDaily_KillSwitchBlocksPollingAndKeepsSnapshotEmpty()
    {
        var ledger = new AuditLedgerService(CreateLedgerPath());
        ledger.Record(Packet("scheduler_proposal", "Draft", DateTimeOffset.Parse("2026-05-13T09:00:00Z")));
        var service = new LiveStatusFeed(ledger);
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };

        var snapshot = service.BuildDaily(DateTimeOffset.Parse("2026-05-13T12:00:00Z"), settings);

        Assert.True(snapshot.IsEmpty);
        Assert.Equal(0, snapshot.TodayCounts.Previews);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("1", 5)]
    [InlineData("8", 8)]
    [InlineData("600", 300)]
    [InlineData("banana", 10)]
    public void ReadPollInterval_ClampsAndDefaults(string? value, int expectedSeconds)
    {
        var service = new LiveStatusFeed(new AuditLedgerService(CreateLedgerPath()));
        var settings = new Dictionary<string, string>();
        if (value is not null)
        {
            settings[LiveStatusFeed.PollSecondsSetting] = value;
        }

        var interval = service.ReadPollInterval(settings);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), interval);
    }

    [Fact]
    public void BannerVisibility_HonorsQuietPetOnlyFullscreenAndIdleRules()
    {
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");
        var active = new RuntimeSupervisorStatus(
            RuntimeSupervisorMode.Active,
            BackgroundWorkAllowed: false,
            ToolWindowAllowed: true,
            IsQuietedForFullscreen: false,
            UserStatus: "active",
            BlockReason: "");

        Assert.True(OverlayStatusBannerView.ShouldShow(active, killSwitchActive: false, now, now.AddSeconds(-10)));
        Assert.False(OverlayStatusBannerView.ShouldShow(active, killSwitchActive: false, now, now.AddSeconds(-61)));
        Assert.True(OverlayStatusBannerView.ShouldShow(active, killSwitchActive: true, now, now.AddMinutes(-30)));

        var petOnly = active with { Mode = RuntimeSupervisorMode.PetOnly };
        Assert.False(OverlayStatusBannerView.ShouldShow(petOnly, killSwitchActive: false, now, now));

        var quietedFullscreen = active with { IsQuietedForFullscreen = true };
        Assert.False(OverlayStatusBannerView.ShouldShow(quietedFullscreen, killSwitchActive: false, now, now));
    }

    private static EvidencePacket Packet(
        string kind,
        string status,
        DateTimeOffset createdAt,
        bool network = false,
        bool hosted = false,
        bool localModel = false,
        bool mutate = false)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            network,
            hosted,
            localModel,
            mutate,
            "artifact",
            "private summary omitted from live status",
            status);
    }

    private static string CreateLedgerPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-live-status-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "ledger.sqlite");
    }
}
