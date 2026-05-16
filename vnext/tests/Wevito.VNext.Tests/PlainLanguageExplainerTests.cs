using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PlainLanguageExplainerTests
{
    public static IEnumerable<object[]> KnownPacketKinds()
    {
        return PlainLanguageExplainer.KnownPacketKinds.Select(kind => new object[] { kind });
    }

    [Theory]
    [MemberData(nameof(KnownPacketKinds))]
    public void ExplainPacketKind_MapsKnownKinds(string packetKind)
    {
        var explainer = new PlainLanguageExplainer();

        var text = explainer.ExplainPacketKind(packetKind);

        Assert.False(text.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(".", text);
    }

    [Fact]
    public void ExplainPacketKind_WarnsForUnknownKind()
    {
        var warnings = new List<string>();
        var explainer = new PlainLanguageExplainer(warnings.Add);

        var text = explainer.ExplainPacketKind("mystery_packet");

        Assert.Equal("Unknown mystery_packet", text);
        Assert.Equal(["mystery_packet"], warnings);
    }

    [Fact]
    public void CoversModelBootstrapRequiredKind()
    {
        var text = new PlainLanguageExplainer().ExplainPacketKind(OllamaModelBootstrapService.BootstrapRequiredPacketKind);

        Assert.Contains("reasoning model", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CoversModelBootstrapRuntimeAbsentKind()
    {
        var text = new PlainLanguageExplainer().ExplainPacketKind(OllamaModelBootstrapService.RuntimeAbsentPacketKind);

        Assert.Contains("Ollama", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explain_DoesNotExposePrivateSummaryText()
    {
        var explainer = new PlainLanguageExplainer();
        var row = Row(
            packetKind: "web_fetch",
            summary: "secret private query about something sensitive",
            status: "Completed",
            network: true);

        var text = explainer.Explain(row);

        Assert.Contains("web research", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("used network", text);
        Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private query", text, StringComparison.OrdinalIgnoreCase);
    }

    private static AuditLedgerRow Row(
        string packetKind,
        string summary,
        string status,
        bool network = false,
        bool hosted = false,
        bool localModel = false,
        bool mutate = false)
    {
        return new AuditLedgerRow(
            1,
            Guid.NewGuid(),
            packetKind,
            null,
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"),
            network,
            hosted,
            localModel,
            mutate,
            "artifact",
            summary,
            status,
            "");
    }
}
