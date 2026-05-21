using System.Reflection;
using System.Text.RegularExpressions;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerActivityServiceObserverWiringTests
{
    private const string ObserverFlagName = "apply_v0_invariant_observer_in_activity_service_enabled";
    private const int C190KnownPacketKindCount = 160;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void Observer_flag_default_false()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == ObserverFlagName);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    [Fact]
    public void Observer_flag_off_no_watchdog_invocation()
    {
        var fixture = ObserverFixture.Create(observerEnabled: false);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));
        var service = fixture.CreateService(withWatchdog: true);

        _ = service.ReadRecent(20);

        Assert.Empty(fixture.InvariantRows());
    }

    [Fact]
    public void Observer_flag_on_invokes_scan_and_emit_on_read()
    {
        var fixture = ObserverFixture.Create(observerEnabled: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));
        var service = fixture.CreateService(withWatchdog: true);

        _ = service.ReadRecent(20);

        var row = Assert.Single(fixture.InvariantRows());
        Assert.Contains(fixture.OperationId, row.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Observer_flag_on_killswitch_tripped_no_invocation()
    {
        var fixture = ObserverFixture.Create(observerEnabled: true, killSwitchActive: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));
        var service = fixture.CreateService(withWatchdog: true);

        _ = service.ReadRecent(20);

        Assert.Empty(fixture.InvariantRows());
    }

    [Fact]
    public void Observer_does_not_call_scan_or_emit_directly()
    {
        var source = File.ReadAllText(ActivitySourcePath());

        Assert.DoesNotMatch(new Regex(@"\.Scan\s*\(", RegexOptions.CultureInvariant), source);
        Assert.DoesNotContain("EmitInvariantCheckFailedPackets(", source, StringComparison.Ordinal);
        Assert.Contains(".ScanAndEmit(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Observer_does_not_emit_new_packet_kind()
    {
        Assert.Equal(C190KnownPacketKindCount, PlainLanguageExplainer.KnownPacketKinds.Count);
    }

    [Fact]
    public void Observer_host_constructor_backward_compatible()
    {
        var fixture = ObserverFixture.Create(observerEnabled: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));

        var service = new ApplyRunnerActivityService(fixture.DatabasePath, fixture.KillSwitch);

        var entries = service.ReadRecent(20);
        Assert.Single(entries);
        Assert.Empty(fixture.InvariantRows());
        var optionalWatchdogParameter = typeof(ApplyRunnerActivityService)
            .GetConstructors()
            .SelectMany(ctor => ctor.GetParameters())
            .Single(parameter => parameter.Name == "watchdog");
        Assert.True(optionalWatchdogParameter.HasDefaultValue);
    }

    [Fact]
    public void Observer_no_audit_ledger_mutation_outside_watchdog_record()
    {
        foreach (var source in new[] { ActivitySourcePath(), DipointSourcePath() }.Select(File.ReadAllText))
        {
            Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE ", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ALTER TABLE", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static EvidencePacket Packet(string kind, string operationId, DateTimeOffset timestamp)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            TaskCardId: null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: kind.Equals(SelfImprovementPacketKinds.ApplyV0Completed, StringComparison.OrdinalIgnoreCase),
            ArtifactPath: "",
            Summary: $"operation_id={operationId}",
            Status: "Completed");
    }

    private static string ActivitySourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ApplyRunnerActivityService.cs");
    }

    private static string DipointSourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Shell", "ShellCoordinator.cs");
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {string.Join('/', parts)} from test output directory.");
    }

    private sealed class ObserverFixture
    {
        private ObserverFixture(bool observerEnabled, bool killSwitchActive)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-activity-observer-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            DatabasePath = Path.Combine(root, "ledger.sqlite");
            Ledger = new AuditLedgerService(DatabasePath);
            OperationId = $"apply-{Guid.NewGuid():N}";
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InvariantViolationWatchdog.EnabledSetting] = bool.TrueString,
                [InvariantViolationWatchdog.V0InvariantCheckEmitEnabledSetting] = bool.TrueString,
                [ObserverFlagName] = observerEnabled.ToString()
            };

            if (killSwitchActive)
            {
                Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
            }

            KillSwitch = new KillSwitchService(() => Settings);
        }

        public string DatabasePath { get; }

        public AuditLedgerService Ledger { get; }

        public string OperationId { get; }

        public Dictionary<string, string> Settings { get; }

        public KillSwitchService KillSwitch { get; }

        public static ObserverFixture Create(bool observerEnabled, bool killSwitchActive = false)
        {
            return new ObserverFixture(observerEnabled, killSwitchActive);
        }

        public ApplyRunnerActivityService CreateService(bool withWatchdog)
        {
            var watchdog = withWatchdog
                ? new InvariantViolationWatchdog(DatabasePath, Ledger, KillSwitch, () => Settings)
                : null;

            return new ApplyRunnerActivityService(DatabasePath, KillSwitch, watchdog, () => Settings);
        }

        public long Record(string kind, string operationId, DateTimeOffset timestamp)
        {
            return Ledger.Record(Packet(kind, operationId, timestamp));
        }

        public IReadOnlyList<AuditLedgerRow> InvariantRows()
        {
            return Ledger
                .Snapshot(Now.AddHours(-1), DateTimeOffset.UtcNow.AddMinutes(5))
                .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, StringComparison.OrdinalIgnoreCase))
                .Where(row => row.Summary.Contains(OperationId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
