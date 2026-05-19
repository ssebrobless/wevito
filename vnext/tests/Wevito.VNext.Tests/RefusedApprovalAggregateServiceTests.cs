using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class RefusedApprovalAggregateServiceTests
{
    [Fact]
    public void Build_CountsEveryKnownReason()
    {
        var fixture = RefusedFixture.Create();
        foreach (var reason in KnownReasons())
        {
            fixture.Record(reason);
        }

        var aggregate = fixture.Service.Build();

        Assert.False(aggregate.IsBlocked);
        Assert.Equal(KnownReasons().Count, aggregate.Total);
        foreach (var reason in KnownReasons())
        {
            Assert.True(aggregate.ByReason.TryGetValue(reason, out var count), $"Missing reason: {reason}");
            Assert.Equal(1, count);
        }
        Assert.Empty(aggregate.OtherBucketByHashPrefix);
    }

    [Fact]
    public void Build_BucketsUnknownReasonByHashPrefixWithoutRawText()
    {
        var fixture = RefusedFixture.Create();
        fixture.Record("private user typed text should not render");

        var aggregate = fixture.Service.Build();

        var bucket = Assert.Single(aggregate.OtherBucketByHashPrefix);
        Assert.Equal(8, bucket.Key.Length);
        Assert.Equal(1, bucket.Value);
        Assert.DoesNotContain("private", bucket.Key, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(aggregate.ByReason);
    }

    [Fact]
    public void Build_KillSwitchActiveReturnsBlockedAggregate()
    {
        var fixture = RefusedFixture.Create(killSwitchActive: true);
        fixture.Record(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);

        var aggregate = fixture.Service.Build();

        Assert.True(aggregate.IsBlocked);
        Assert.Equal("kill_switch=true", aggregate.BlockedReason);
        Assert.Equal(0, aggregate.Total);
        Assert.Empty(aggregate.ByReason);
    }

    [Fact]
    public void Build_UsesReadOnlySqlAndWritesNoPackets()
    {
        var commands = new List<string>();
        var fixture = RefusedFixture.Create(commandObserver: commands.Add);
        fixture.Record(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);

        _ = fixture.Service.Build();

        Assert.All(commands, command =>
        {
            Assert.DoesNotContain("INSERT", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", command, StringComparison.OrdinalIgnoreCase);
        });
        var source = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "RefusedApprovalAggregateService.cs"));
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogIncludesProductionEmittedReasons()
    {
        var catalogSource = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "RefusedApprovalReasonCatalog.cs"));
        var productionSources = string.Join(
            Environment.NewLine,
            File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "SupervisedImprovementLoop.cs")),
            File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "UserApplyApprovalValidator.cs")));

        foreach (var reason in KnownReasons())
        {
            Assert.Contains(reason, catalogSource, StringComparison.Ordinal);
            Assert.Contains(reason, productionSources, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ToolPopupRefusedApprovalPanelDoesNotDisplayRawUserText()
    {
        var xaml = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("AutomationId=\"RefusedApprovalPanel\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "RefusedApprovalPanel was not found.");
        var end = xaml.IndexOf("EvidenceSummaryGrid", start, StringComparison.Ordinal);
        Assert.True(end > start, "RefusedApprovalPanel should appear before EvidenceSummaryGrid.");
        var section = xaml[start..end];

        Assert.DoesNotContain("ConfirmationText", section, StringComparison.Ordinal);
        Assert.DoesNotContain("user typed", section, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> KnownReasons()
    {
        return
        [
            SupervisedImprovementLoop.ApplyRunnerNotImplementedReason,
            "scope_hash_mismatch",
            "not_confirmed_in_this_message",
            "empty_confirmation_text",
            "stale_confirmation",
            "scope_id_mismatch",
            "operation_id_mismatch",
            "kill_switch=true",
            "approval_missing"
        ];
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

    private sealed class RefusedFixture
    {
        private readonly AuditLedgerService _ledger;
        private readonly DateTimeOffset _baseTime = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        private int _offset;

        private RefusedFixture(string databasePath, bool killSwitchActive, Action<string>? commandObserver)
        {
            _ledger = new AuditLedgerService(databasePath);
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            Service = new RefusedApprovalAggregateService(
                databasePath,
                new KillSwitchService(() => settings),
                commandObserver);
        }

        public RefusedApprovalAggregateService Service { get; }

        public static RefusedFixture Create(bool killSwitchActive = false, Action<string>? commandObserver = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-refused-approval-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new RefusedFixture(Path.Combine(root, "ledger.sqlite"), killSwitchActive, commandObserver);
        }

        public void Record(string reason)
        {
            _ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyRefused,
                TaskCardId: Guid.NewGuid(),
                _baseTime.AddSeconds(_offset++),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: "",
                Summary: $"Supervised self-improvement apply refused: {reason}.",
                Status: "Refused",
                Error: reason));
        }
    }
}
