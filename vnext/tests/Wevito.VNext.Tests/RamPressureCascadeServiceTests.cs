using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RamPressureCascadeServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void SuspendsExperimentsAtTier1()
    {
        var result = new RamPressureCascadeService().Evaluate(new RamPressureSnapshot(2.5, CapturedAtUtc: Now));

        Assert.Equal(RamPressureTier.SuspendBackground, result.Tier);
        Assert.True(result.SuspendBackgroundExperiments);
        Assert.False(result.UnloadImageGenSidecar);
    }

    [Fact]
    public void UnloadsImageGenAtTier2()
    {
        var result = new RamPressureCascadeService().Evaluate(new RamPressureSnapshot(1.75, CapturedAtUtc: Now));

        Assert.Equal(RamPressureTier.UnloadImageGen, result.Tier);
        Assert.True(result.UnloadImageGenSidecar);
        Assert.False(result.UnloadLlmModel);
    }

    [Fact]
    public void UnloadsLlmAtTier3()
    {
        var result = new RamPressureCascadeService().Evaluate(new RamPressureSnapshot(1.25, CapturedAtUtc: Now));

        Assert.Equal(RamPressureTier.UnloadLlm, result.Tier);
        Assert.True(result.UnloadLlmModel);
        Assert.False(result.StopCardRequired);
    }

    [Fact]
    public void EmergencyAtTier4()
    {
        var harness = BuildHarness();

        var result = harness.Service.Evaluate(new RamPressureSnapshot(0.75, CapturedAtUtc: Now));

        Assert.Equal(RamPressureTier.Emergency, result.Tier);
        Assert.True(result.StopCardRequired);
        Assert.Equal(RamPressureCascadeService.EmergencyPacketKind, result.PacketKind);
        var row = Assert.Single(harness.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal(RamPressureCascadeService.EmergencyPacketKind, row.PacketKind);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void PetGameReservedMinNotTouched()
    {
        var result = new RamPressureCascadeService().Evaluate(new RamPressureSnapshot(0.5, PetReservedMinGb: 1, CapturedAtUtc: Now));

        Assert.False(result.PetReservedMinTouched);
        Assert.Contains("pet_reserved_min_touched=false", result.Summary);
    }

    private static Harness BuildHarness()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-ram-pressure-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        return new Harness(new RamPressureCascadeService(ledger), ledger);
    }

    private sealed record Harness(RamPressureCascadeService Service, AuditLedgerService Ledger);
}
