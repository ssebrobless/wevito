using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class OperationTimelineServiceTests
{
    [Fact]
    public void BuildFor_RendersSelfImprovementChainInChronologicalOrder()
    {
        var fixture = TimelineFixture.Create();
        fixture.Record(SelfImprovementPacketKinds.ProposalDrafted, "Proposal for op-alpha", "Completed", atOffsetSeconds: 0);
        fixture.Record(SelfImprovementPacketKinds.ConstitutionalReviewed, "No operation text here, same task card.", "Completed", atOffsetSeconds: 1);
        fixture.Record(SelfImprovementPacketKinds.DryRunCompleted, "Dry run for op-alpha", "Completed", atOffsetSeconds: 2);

        var rows = fixture.Service.BuildFor("op-alpha");

        Assert.Collection(
            rows,
            row =>
            {
                Assert.Equal(SelfImprovementPacketKinds.ProposalDrafted, row.PacketKind);
                Assert.Equal("Wevito drafted a supervised self-improvement proposal for review.", row.PlainLanguage);
                Assert.Equal("Completed", row.StatusBadge);
            },
            row =>
            {
                Assert.Equal(SelfImprovementPacketKinds.ConstitutionalReviewed, row.PacketKind);
                Assert.Equal("Wevito checked a self-improvement proposal against its safety rules.", row.PlainLanguage);
            },
            row =>
            {
                Assert.Equal(SelfImprovementPacketKinds.DryRunCompleted, row.PacketKind);
                Assert.Equal("Wevito completed a self-improvement dry run without applying changes.", row.PlainLanguage);
            });
    }

    [Fact]
    public void BuildFor_UnknownSelfImprovementKindUsesBucketText()
    {
        var fixture = TimelineFixture.Create();
        fixture.Record("self_improvement_future_packet", "Future packet for op-unknown", "Completed");

        var row = Assert.Single(fixture.Service.BuildFor("op-unknown"));

        Assert.Equal("self_improvement_future_packet", row.PacketKind);
        Assert.Equal("(unrecognized packet kind)", row.PlainLanguage);
    }

    [Fact]
    public void BuildFor_KillSwitchActiveReturnsBlockedSentinel()
    {
        var fixture = TimelineFixture.Create(killSwitchActive: true);
        fixture.Record(SelfImprovementPacketKinds.ProposalDrafted, "Proposal for op-kill", "Completed");

        var row = Assert.Single(fixture.Service.BuildFor("op-kill"));

        Assert.Equal("blocked", row.PacketKind);
        Assert.Equal("Blocked", row.StatusBadge);
        Assert.Contains("kill_switch=true", row.PlainLanguage, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFor_DoesNotDisplayRawSummaryOrErrorText()
    {
        var fixture = TimelineFixture.Create();
        fixture.Record(
            SelfImprovementPacketKinds.EvalCompleted,
            "held_out=secret op-secret",
            "Completed",
            error: "raw error should stay hidden");

        var row = Assert.Single(fixture.Service.BuildFor("op-secret"));

        Assert.DoesNotContain("held_out=secret", row.PlainLanguage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw error", row.PlainLanguage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Wevito completed the required self-improvement evaluation gates.", row.PlainLanguage);
    }

    [Fact]
    public void BuildFor_UsesReadOnlySqlAndWritesNoPackets()
    {
        var commands = new List<string>();
        var fixture = TimelineFixture.Create(commandObserver: commands.Add);
        fixture.Record(SelfImprovementPacketKinds.ApplyAwaitingApproval, "Approval waiting for op-sql", "WaitingForApproval");

        _ = fixture.Service.BuildFor("op-sql");

        Assert.All(commands, command =>
        {
            Assert.DoesNotContain("INSERT", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", command, StringComparison.OrdinalIgnoreCase);
        });

        var source = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "OperationTimelineService.cs"));
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolPopupTimelinePanelDoesNotBindRawSummaryOrError()
    {
        var xaml = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("AutomationId=\"OperationTimelinePanel\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "OperationTimelinePanel was not found.");
        var end = xaml.IndexOf("EvidenceSummaryGrid", start, StringComparison.Ordinal);
        Assert.True(end > start, "OperationTimelinePanel should appear before EvidenceSummaryGrid.");
        var section = xaml[start..end];

        Assert.DoesNotContain("Summary", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Error", section, StringComparison.Ordinal);
    }

    private static string SourcePath(params string[] parts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate source file: {Path.Combine(parts)}");
    }

    private sealed class TimelineFixture
    {
        private readonly AuditLedgerService _ledger;
        private readonly Guid _taskCardId = Guid.NewGuid();
        private readonly DateTimeOffset _baseTime = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

        private TimelineFixture(string databasePath, bool killSwitchActive, Action<string>? commandObserver)
        {
            _ledger = new AuditLedgerService(databasePath);
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            Service = new OperationTimelineService(
                databasePath,
                new PlainLanguageExplainer(),
                new KillSwitchService(() => settings),
                commandObserver);
        }

        public OperationTimelineService Service { get; }

        public static TimelineFixture Create(bool killSwitchActive = false, Action<string>? commandObserver = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-operation-timeline-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TimelineFixture(Path.Combine(root, "ledger.sqlite"), killSwitchActive, commandObserver);
        }

        public void Record(string packetKind, string summary, string status, int atOffsetSeconds = 0, string error = "")
        {
            _ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                packetKind,
                _taskCardId,
                _baseTime.AddSeconds(atOffsetSeconds),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: packetKind.Equals(SelfImprovementPacketKinds.EvalCompleted, StringComparison.Ordinal),
                DidMutate: packetKind.Equals(SelfImprovementPacketKinds.ApplyCompleted, StringComparison.Ordinal),
                ArtifactPath: "",
                Summary: summary,
                Status: status,
                Error: error));
        }
    }
}
