using System.Reflection;
using System.Text.RegularExpressions;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Tests;

public sealed class CapabilitiesAndGatesServiceObserverWiringTests
{
    private const string ObserverFlagName = "snapshot_v0_invariant_observer_in_capabilities_and_gates_enabled";
    private const int C192KnownPacketKindCount = 160;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void ProductTruth_capabilities_and_gates_observer_flag_registered_default_false()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == ObserverFlagName);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_service_ctor_backward_compatible()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _ = new CapabilitiesAndGatesService(() => settings);
        _ = new CapabilitiesAndGatesService(() => settings, new KillSwitchService(() => settings));

        var watchdogParameter = typeof(CapabilitiesAndGatesService)
            .GetConstructors()
            .SelectMany(ctor => ctor.GetParameters())
            .Single(parameter => parameter.Name == "watchdog");
        Assert.Equal(typeof(InvariantViolationWatchdog), watchdogParameter.ParameterType);
        Assert.True(watchdogParameter.HasDefaultValue);
        Assert.Null(watchdogParameter.DefaultValue);
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_snapshot_flag_off_does_not_invoke_watchdog()
    {
        var fixture = ObserverFixture.Create(observerEnabled: false);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));
        var service = fixture.CreateService(withWatchdog: true);

        _ = service.Snapshot(Now);

        Assert.Empty(fixture.InvariantRows());
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_snapshot_flag_on_invokes_watchdog_exactly_once()
    {
        var fixture = ObserverFixture.Create(observerEnabled: true);
        fixture.Record(SelfImprovementPacketKinds.ApplyV0Completed, fixture.OperationId, Now.AddMinutes(-1));
        var service = fixture.CreateService(withWatchdog: true);

        _ = service.Snapshot(Now);

        var row = Assert.Single(fixture.InvariantRows());
        Assert.Contains(fixture.OperationId, row.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_snapshot_flag_on_null_watchdog_does_not_throw()
    {
        var fixture = ObserverFixture.Create(observerEnabled: true);
        var service = fixture.CreateService(withWatchdog: false);

        var snapshot = service.Snapshot(Now);

        Assert.NotNull(snapshot);
        Assert.Empty(fixture.InvariantRows());
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_service_source_does_not_call_scan_or_emit_directly()
    {
        var source = File.ReadAllText(ServiceSourcePath());

        Assert.DoesNotMatch(new Regex(@"\.Scan\s*\(", RegexOptions.CultureInvariant), source);
        Assert.DoesNotContain(".EmitInvariantCheckFailedPackets(", source, StringComparison.Ordinal);
        Assert.Contains(".ScanAndEmit(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_no_new_audit_packet_kind()
    {
        Assert.Equal(C192KnownPacketKindCount, PlainLanguageExplainer.KnownPacketKinds.Count);
    }

    [Fact]
    public void ProductTruth_capabilities_and_gates_service_source_references_killswitch()
    {
        var source = File.ReadAllText(ServiceSourcePath());

        Assert.Contains("KillSwitch", source, StringComparison.Ordinal);
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

    private static string ServiceSourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "CapabilitiesAndGatesService.cs");
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
        private ObserverFixture(bool observerEnabled)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-capabilities-observer-tests", Guid.NewGuid().ToString("N"));
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
            KillSwitch = new KillSwitchService(() => Settings);
        }

        public string DatabasePath { get; }

        public AuditLedgerService Ledger { get; }

        public string OperationId { get; }

        public Dictionary<string, string> Settings { get; }

        public KillSwitchService KillSwitch { get; }

        public static ObserverFixture Create(bool observerEnabled)
        {
            return new ObserverFixture(observerEnabled);
        }

        public CapabilitiesAndGatesService CreateService(bool withWatchdog)
        {
            var watchdog = withWatchdog
                ? new InvariantViolationWatchdog(DatabasePath, Ledger, KillSwitch, () => Settings)
                : null;

            return new CapabilitiesAndGatesService(() => Settings, KillSwitch, watchdog);
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
