using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class WebResearchConnectorTests
{
    [Fact]
    public async Task DefaultBackendIsOfflineAndDoesNotUseNetwork()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var connector = new WebResearchConnector(ledger: ledger);

        var result = await connector.FetchAsync(BuildRequest(root, approved: true));

        Assert.True(result.Succeeded, result.BlockReason);
        Assert.False(result.Records[0].Backend != "offline" && result.Records[0].Url.StartsWith("http", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(result.EvidencePath));
        var row = Assert.Single(ledger.Snapshot(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
        Assert.False(row.DidUseHostedAi);
    }

    [Fact]
    public async Task FetchWithoutApprovedCardIsBlocked()
    {
        var root = CreateTempRoot();
        var connector = new WebResearchConnector();

        var result = await connector.FetchAsync(BuildRequest(root, approved: false));

        Assert.True(result.Blocked);
        Assert.Contains("Approved task card", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuietModeAndKillSwitchBlockFetch()
    {
        var root = CreateTempRoot();
        var connector = new WebResearchConnector(killSwitch: ActiveKillSwitch());

        var killed = await connector.FetchAsync(BuildRequest(root, approved: true));
        Assert.True(killed.Blocked);
        Assert.Equal("kill_switch=true", killed.BlockReason);

        connector = new WebResearchConnector();
        var quiet = await connector.FetchAsync(BuildRequest(root, approved: true) with
        {
            RuntimeStatus = new RuntimeSupervisorStatus(RuntimeSupervisorMode.Quiet, false, false, false, "quiet", "Quiet mode blocks helper work.")
        });
        Assert.True(quiet.Blocked);
        Assert.Contains("Quiet mode", quiet.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RateLimitsApplyToNetworkBackend()
    {
        var root = CreateTempRoot();
        var backend = new FakeNetworkBackend();
        var connector = new WebResearchConnector([backend]);
        var request = BuildRequest(root, approved: true, backend: backend.BackendId) with
        {
            Settings = new Dictionary<string, string>
            {
                [WebResearchConnector.WebSearchEnabledSetting] = bool.TrueString,
                [WebResearchConnector.MaxFetchesPerHourSetting] = "1",
                [WebResearchConnector.MaxFetchesPerTaskSetting] = "1"
            },
            FetchesUsedThisHour = 1
        };

        var result = await connector.FetchAsync(request);

        Assert.True(result.Blocked);
        Assert.Contains("Hourly", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CacheHitAvoidsBackendRefetchUnlessForced()
    {
        var root = CreateTempRoot();
        var backend = new FakeNetworkBackend();
        var connector = new WebResearchConnector([backend]);
        var request = BuildRequest(root, approved: true, backend: backend.BackendId) with
        {
            Settings = new Dictionary<string, string>
            {
                [WebResearchConnector.WebSearchEnabledSetting] = bool.TrueString
            }
        };

        var first = await connector.FetchAsync(request);
        var second = await connector.FetchAsync(request);
        var third = await connector.FetchAsync(request with { ForceRefresh = true });

        Assert.True(first.Succeeded);
        Assert.True(second.Records[0].FromCache);
        Assert.True(third.Succeeded);
        Assert.Equal(2, backend.Calls);
    }

    private static WebResearchRequest BuildRequest(string root, bool approved, string backend = "")
    {
        return new WebResearchRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            approved,
            "recent local-first AI research",
            backend,
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            Path.Combine(root, "web-cache"),
            new Dictionary<string, string>
            {
                [WebResearchConnector.WebBackendSetting] = string.IsNullOrWhiteSpace(backend) ? "offline" : backend
            },
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static KillSwitchService ActiveKillSwitch()
    {
        return new KillSwitchService(() => new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
    }

    private sealed class FakeNetworkBackend : IWebSearchBackend
    {
        public string BackendId => "fake";

        public bool UsesNetwork => true;

        public int Calls { get; private set; }

        public Task<IReadOnlyList<WebFetchRecord>> SearchAsync(WebSearchBackendRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<WebFetchRecord>>([
                new WebFetchRecord(Guid.NewGuid(), BackendId, request.Query, request.Query, "https://example.test", "Example", "Snippet", request.RequestedAtUtc, false, "cache", "https://example.test")
            ]);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-web-research-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
