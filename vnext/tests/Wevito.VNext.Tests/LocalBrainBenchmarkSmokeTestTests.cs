using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalBrainBenchmarkSmokeTestTests
{
    [Fact]
    public async Task RunsFivePromptsAndRecordsLocalModelUseWhenAdapterCallsProvider()
    {
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        var ledger = new AuditLedgerService(TempLedgerPath());
        var adapter = new FakeModelAdapter("ollama", "qwen2.5:7b-instruct-q4_K_M", didCallProvider: true);
        var service = new LocalBrainBenchmarkSmokeTest(adapter, ledger);

        var result = await service.RunAsync(TempArtifactRoot(), now);

        Assert.True(result.Succeeded);
        Assert.False(result.Degraded);
        Assert.True(result.DidUseLocalModel);
        Assert.Equal(5, result.Cases.Count);
        Assert.True(File.Exists(result.ArtifactPath));
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.Equal(LocalBrainBenchmarkSmokeTest.PacketKind, row.PacketKind);
        Assert.True(row.DidUseLocalModel);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public async Task ReportsDegradedWhenAdapterUsesDeterministicFallback()
    {
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        var ledger = new AuditLedgerService(TempLedgerPath());
        var adapter = new LocalModelAdapter();
        var service = new LocalBrainBenchmarkSmokeTest(adapter, ledger);

        var result = await service.RunAsync(TempArtifactRoot(), now);

        Assert.True(result.Succeeded);
        Assert.True(result.Degraded);
        Assert.False(result.DidUseLocalModel);
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.Equal("Degraded", row.Status);
        Assert.False(row.DidUseLocalModel);
    }

    [Fact]
    public async Task RefusesHostedProviderResults()
    {
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        var ledger = new AuditLedgerService(TempLedgerPath());
        var adapter = new FakeModelAdapter("anthropic", "claude", didCallProvider: true);
        var service = new LocalBrainBenchmarkSmokeTest(adapter, ledger);

        var result = await service.RunAsync(TempArtifactRoot(), now);

        Assert.False(result.Succeeded);
        Assert.Equal("Blocked", result.Status);
        Assert.All(result.Cases, candidate => Assert.Equal("hosted_provider_refused", candidate.BlockReason));
        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
    }

    [Fact]
    public async Task RespectsKillSwitchBeforeWritingSmokeCases()
    {
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        var ledger = new AuditLedgerService(TempLedgerPath());
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var adapter = new FakeModelAdapter("ollama", "qwen", didCallProvider: true);
        var service = new LocalBrainBenchmarkSmokeTest(adapter, ledger, killSwitch);

        var result = await service.RunAsync(TempArtifactRoot(), now);

        Assert.False(result.Succeeded);
        Assert.Equal("Blocked", result.Status);
        Assert.Empty(result.Cases);
        Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    private static string TempLedgerPath()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-local-brain-smoke-tests", Guid.NewGuid().ToString("N"), "ledger.sqlite");
    }

    private static string TempArtifactRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-local-brain-smoke-tests", Guid.NewGuid().ToString("N"), "artifacts");
    }

    private sealed class FakeModelAdapter(string provider, string model, bool didCallProvider) : IModelAdapter
    {
        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ModelResponse(
                provider,
                model,
                $"This is a sufficiently detailed smoke-test response for {request.UserTask}",
                didCallProvider,
                AuditLogPath: Path.Combine(request.ArtifactRoot, "fake-model.json")));
        }
    }
}
