using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SoakRunnerServiceTests
{
    [Fact]
    public void StartExplicitPreview_WritesStartPacketAndArtifact()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new SoakRunnerService(ledger);

        var result = service.StartExplicitPreview(Request(root));

        Assert.True(result.Started);
        Assert.True(File.Exists(result.SummaryPath));
        var rows = ledger.Snapshot(DateTimeOffset.Parse("2026-05-13T00:00:00Z"), DateTimeOffset.Parse("2026-05-14T00:00:00Z"));
        var row = Assert.Single(rows);
        Assert.Equal("soak_session_start", row.PacketKind);
        Assert.False(row.DidMutate);
        Assert.False(row.DidUseNetwork);
    }

    [Fact]
    public void StartExplicitPreview_RefusesDefaultOffCapabilityEnabled()
    {
        var root = CreateTempRoot();
        var service = new SoakRunnerService(new AuditLedgerService(Path.Combine(root, "ledger.sqlite")));
        var settings = new Dictionary<string, string>
        {
            ["pet_model_adapter_enabled"] = bool.TrueString
        };

        var result = service.StartExplicitPreview(Request(root, settings));

        Assert.False(result.Started);
        Assert.Contains("default-off capability", result.BlockReason);
    }

    [Fact]
    public void StartExplicitPreview_KillSwitchBlocksSoakRunner()
    {
        var root = CreateTempRoot();
        var service = new SoakRunnerService(new AuditLedgerService(Path.Combine(root, "ledger.sqlite")));
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };

        var result = service.StartExplicitPreview(Request(root, settings));

        Assert.False(result.Started);
        Assert.Contains("KillSwitch", result.BlockReason);
    }

    [Fact]
    public void RuntimeBudgetMeter_EnsuresStateFileSurvivesStartupReplay()
    {
        var root = CreateTempRoot();
        var path = Path.Combine(root, "budget-meter.json");
        var meter = new RuntimeBudgetMeter(
            path,
            () => DateTimeOffset.Parse("2026-05-13T12:00:00Z"),
            () => new RuntimeResourceSnapshot(1, 20, DateTimeOffset.Parse("2026-05-13T12:00:00Z")));

        var existedBefore = meter.EnsureStateFile();
        var flushed = meter.FlushIfDue(TimeSpan.Zero);

        Assert.False(existedBefore);
        Assert.True(flushed);
        Assert.True(File.Exists(path));
    }

    private static SoakRunnerRequest Request(string root, IReadOnlyDictionary<string, string>? settings = null)
    {
        return new SoakRunnerRequest(
            4,
            Path.Combine(root, "soak"),
            settings ?? new Dictionary<string, string>(),
            new RuntimeBudgetSnapshot(4, 20, 512),
            new FocusStealSnapshot(0, null),
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-soak-runner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
