using System.IO;
using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Readiness;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class LocalOllamaReadinessProbeServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void KillSwitchActive_WritesNoPacket()
    {
        var harness = Harness(
            settings: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [LocalOllamaReadinessProbeService.EnabledSetting] = bool.TrueString,
                [KillSwitchService.KillSwitchSetting] = bool.TrueString
            });

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.False(snapshot.ProbeRan);
        Assert.Equal("kill_switch=true", snapshot.Reason);
        Assert.Empty(harness.Rows());
        Assert.Empty(harness.Http.GetCalls);
    }

    [Fact]
    public void FlagOff_WritesNoPacket()
    {
        var harness = Harness();

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.False(snapshot.ProbeRan);
        Assert.Equal("local_ollama_readiness_probe_enabled=false", snapshot.Reason);
        Assert.Empty(harness.Rows());
        Assert.Empty(harness.Http.GetCalls);
    }

    [Fact]
    public void NonLoopbackEndpoint_IsRefusedWithoutPacket()
    {
        var harness = Harness(
            settings: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [LocalOllamaReadinessProbeService.EnabledSetting] = bool.TrueString,
                [LocalOllamaReadinessProbeService.EndpointSetting] = "127.0.0.2:11434"
            });

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.False(snapshot.ProbeRan);
        Assert.Equal("non_loopback_endpoint", snapshot.Reason);
        Assert.Empty(harness.Rows());
        Assert.Empty(harness.Http.GetCalls);
    }

    [Fact]
    public void HappyPath_ModelPresent_WritesHonestProbePacket()
    {
        var harness = Harness(settings: EnabledSettings());
        harness.Http.QueueGetResponse("""{"models":[{"name":"qwen2.5:7b-instruct-q4_k_m"}]}""");

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.True(snapshot.ProbeRan);
        Assert.True(snapshot.LoopbackReachable);
        Assert.True(snapshot.ConfiguredModelPresent);
        Assert.Equal("ok", snapshot.Reason);
        Assert.Single(harness.Http.GetCalls);
        Assert.Equal("http://127.0.0.1:11434/api/tags", harness.Http.GetCalls[0].ToString());

        var row = Assert.Single(harness.Rows());
        Assert.Equal(SelfImprovementPacketKinds.LocalRuntimeProbe, row.PacketKind);
        Assert.True(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
        Assert.DoesNotContain("response", row.Summary, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(row.Summary);
        Assert.True(document.RootElement.GetProperty("configured_model_present").GetBoolean());
    }

    [Fact]
    public void HappyPath_ModelAbsent_WritesProbePacketWithModelAbsent()
    {
        var harness = Harness(settings: EnabledSettings());
        harness.Http.QueueGetResponse("""{"models":[{"name":"tinyllama"}]}""");

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.True(snapshot.ProbeRan);
        Assert.True(snapshot.LoopbackReachable);
        Assert.False(snapshot.ConfiguredModelPresent);
        Assert.Equal("ok", snapshot.Reason);
        var row = Assert.Single(harness.Rows());
        using var document = JsonDocument.Parse(row.Summary);
        Assert.False(document.RootElement.GetProperty("configured_model_present").GetBoolean());
    }

    [Fact]
    public void IoException_WritesRuntimeUnreachablePacket()
    {
        var harness = Harness(settings: EnabledSettings());
        harness.Http.QueueGetException(new IOException("loopback refused"));

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.True(snapshot.ProbeRan);
        Assert.False(snapshot.LoopbackReachable);
        Assert.False(snapshot.ConfiguredModelPresent);
        Assert.Equal("runtime_unreachable", snapshot.Reason);
        var row = Assert.Single(harness.Rows());
        using var document = JsonDocument.Parse(row.Summary);
        Assert.Equal("runtime_unreachable", document.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void InvalidJson_WritesInvalidTagsResponsePacket()
    {
        var harness = Harness(settings: EnabledSettings());
        harness.Http.QueueGetResponse("not-json");

        var snapshot = harness.Service.Probe(Now, CancellationToken.None);

        Assert.True(snapshot.ProbeRan);
        Assert.True(snapshot.LoopbackReachable);
        Assert.False(snapshot.ConfiguredModelPresent);
        Assert.Equal("invalid_tags_response", snapshot.Reason);
        var row = Assert.Single(harness.Rows());
        using var document = JsonDocument.Parse(row.Summary);
        Assert.Equal("invalid_tags_response", document.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void DependsOnAbstraction_NotDefaultScoringHttpClient()
    {
        var fields = typeof(LocalOllamaReadinessProbeService)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.Contains(fields, field => field.FieldType == typeof(IScoringHttpClient));
        Assert.DoesNotContain(fields, field => field.FieldType == typeof(DefaultScoringHttpClient));
    }

    private static HarnessState Harness(Dictionary<string, string>? settings = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-ollama-readiness", Guid.NewGuid().ToString("N"));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var effectiveSettings = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var killSwitch = new KillSwitchService(() => effectiveSettings);
        var http = new FakeScoringHttpClient("");
        var service = new LocalOllamaReadinessProbeService(http, ledger, killSwitch, () => effectiveSettings);
        return new HarnessState(ledger, http, service);
    }

    private static Dictionary<string, string> EnabledSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LocalOllamaReadinessProbeService.EnabledSetting] = bool.TrueString
        };
    }

    private sealed record HarnessState(
        AuditLedgerService Ledger,
        FakeScoringHttpClient Http,
        LocalOllamaReadinessProbeService Service)
    {
        public IReadOnlyList<AuditLedgerRow> Rows()
        {
            return Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        }
    }
}
