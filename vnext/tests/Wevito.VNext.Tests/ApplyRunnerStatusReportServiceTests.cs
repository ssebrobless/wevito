using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerStatusReportServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void EmitReport_KillSwitchActive_ReturnsRefusedAndWritesNoPacket()
    {
        var fixture = Fixture.Create(killSwitchActive: true, enabled: true);

        var report = fixture.Service.EmitReport(Now);

        Assert.Equal("", report.ReportId);
        Assert.False(report.ApplyRunnerImplemented);
        Assert.Equal(["kill_switch=true"], report.OutstandingPrerequisites);
        Assert.Empty(fixture.Rows());
    }

    [Fact]
    public void EmitReport_FlagOff_ReturnsRefusedAndWritesNoPacket()
    {
        var fixture = Fixture.Create(enabled: false);

        var report = fixture.Service.EmitReport(Now);

        Assert.Equal("", report.ReportId);
        Assert.False(report.ApplyRunnerImplemented);
        Assert.Equal([$"{ApplyRunnerStatusReportService.EnabledSetting}=false"], report.OutstandingPrerequisites);
        Assert.Empty(fixture.Rows());
    }

    [Fact]
    public void EmitReport_FlagOn_WritesExactlyOneStatusReportPacket()
    {
        var fixture = Fixture.Create(enabled: true);

        var report = fixture.Service.EmitReport(Now);
        var rows = fixture.Rows();

        Assert.NotEqual("", report.ReportId);
        Assert.False(report.ApplyRunnerImplemented);
        Assert.Equal(12, report.OutstandingPrerequisites.Count);
        var row = Assert.Single(rows);
        Assert.Equal(SelfImprovementPacketKinds.ApplyRunnerStatusReport, row.PacketKind);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
        Assert.Equal("Completed", row.Status);
    }

    [Fact]
    public void ReadLatest_ReturnsMostRecentStatusReport()
    {
        var fixture = Fixture.Create(enabled: true);
        var older = new ApplyRunnerStatusReport("older", false, ["one"], SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, Now.AddMinutes(-5));
        var newer = new ApplyRunnerStatusReport("newer", false, ["two"], SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, Now);
        fixture.Record(older);
        fixture.Record(newer);

        var latest = fixture.Service.ReadLatest(fixture.Ledger.DatabasePath, Now);

        Assert.NotNull(latest);
        Assert.Equal("newer", latest!.ReportId);
        Assert.Equal(["two"], latest.OutstandingPrerequisites);
    }

    [Fact]
    public void ReadLatest_MissingOrEmptyLedger_ReturnsNull()
    {
        var fixture = Fixture.Create(enabled: true);

        var latest = fixture.Service.ReadLatest(fixture.Ledger.DatabasePath, Now);

        Assert.Null(latest);
    }

    [Fact]
    public void EmitReport_NotImplementedMarkerMeansApplyRunnerImplementedFalse()
    {
        var fixture = Fixture.Create(enabled: true);

        var report = fixture.Service.EmitReport(Now);

        Assert.Equal("apply_runner_not_implemented_in_v0", SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
        Assert.False(report.ApplyRunnerImplemented);
        Assert.Equal(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, report.SourceConstant);
    }

    private sealed class Fixture
    {
        private Fixture(AuditLedgerService ledger, ApplyRunnerStatusReportService service)
        {
            Ledger = ledger;
            Service = service;
        }

        public AuditLedgerService Ledger { get; }
        public ApplyRunnerStatusReportService Service { get; }

        public static Fixture Create(bool killSwitchActive = false, bool enabled = false)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-apply-runner-status", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString(),
                [ApplyRunnerStatusReportService.EnabledSetting] = enabled.ToString()
            };
            var service = new ApplyRunnerStatusReportService(ledger, new KillSwitchService(() => settings), () => settings);
            return new Fixture(ledger, service);
        }

        public IReadOnlyList<AuditLedgerRow> Rows()
        {
            return Ledger.Snapshot(Now.AddDays(-1), Now.AddDays(1));
        }

        public void Record(ApplyRunnerStatusReport report)
        {
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyRunnerStatusReport,
                null,
                report.GeneratedAtUtc,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: "",
                Summary: JsonSerializer.Serialize(report, JsonDefaults.Options),
                Status: "Completed"));
        }
    }
}
