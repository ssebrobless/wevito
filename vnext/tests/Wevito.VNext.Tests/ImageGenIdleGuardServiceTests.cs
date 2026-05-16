using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ImageGenIdleGuardServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void BlocksBackgroundWhenPetActive()
    {
        var harness = BuildHarness();
        var pet = Pet(idleSince: Now.AddSeconds(-3));

        var result = harness.Service.CanStart(pet, isExplicitUserTrigger: false, Now);

        Assert.False(result.Allowed);
        Assert.Equal(ImageGenIdleGuardService.BlockedPacketKind, result.PacketKind);
        var row = Assert.Single(harness.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal(ImageGenIdleGuardService.BlockedPacketKind, row.PacketKind);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void AllowsUserTriggeredImmediate()
    {
        var service = new ImageGenIdleGuardService();

        var result = service.CanStart(Pet(idleSince: Now), isExplicitUserTrigger: true, Now);

        Assert.True(result.Allowed);
        Assert.Contains("explicit_user_trigger=true", result.Reason);
    }

    private static PetActor Pet(DateTimeOffset idleSince)
    {
        return new PetActor(Guid.NewGuid(), "Snake 1", "snake", IdleSince: idleSince);
    }

    private static Harness BuildHarness()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-image-guard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        return new Harness(new ImageGenIdleGuardService(ledger), ledger);
    }

    private sealed record Harness(ImageGenIdleGuardService Service, AuditLedgerService Ledger);
}
