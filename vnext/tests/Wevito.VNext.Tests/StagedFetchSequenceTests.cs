using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class StagedFetchSequenceTests
{
    private readonly PetSimulationEngine _engine = new();

    [Fact]
    public void StartFetchSequenceBeginsAtMoveToBallWithBallOverlay()
    {
        var now = DateTimeOffset.UtcNow;
        var pet = new PetActor(Guid.NewGuid(), "Goose 1", "goose", ActiveStatuses: []);

        var updated = _engine.StartFetchSequence(pet, now);

        Assert.Equal(FetchStage.MoveToBall, updated.ActiveFetchSequence?.Stage);
        Assert.Equal(AnimationFamily.Walk, updated.CurrentActionVisualIntent?.Family);
        Assert.Equal(PropOverlayKind.Ball, updated.CurrentActionVisualIntent?.Overlay);
        Assert.Equal("fetch_ball", updated.LastActionId);
    }

    [Fact]
    public void AdvanceFetchSequenceWaitsUntilStageTimeout()
    {
        var now = DateTimeOffset.UtcNow;
        var started = _engine.StartFetchSequence(new PetActor(Guid.NewGuid(), "Goose 1", "goose", ActiveStatuses: []), now);

        var updated = _engine.AdvanceFetchSequence(started, now.AddSeconds(1));

        Assert.Equal(FetchStage.MoveToBall, updated.ActiveFetchSequence?.Stage);
        Assert.Equal(started.AnimationStartedAtUtc, updated.AnimationStartedAtUtc);
    }

    [Fact]
    public void AdvanceFetchSequenceMovesToPickupAfterMoveTimeout()
    {
        var now = DateTimeOffset.UtcNow;
        var started = _engine.StartFetchSequence(new PetActor(Guid.NewGuid(), "Goose 1", "goose", ActiveStatuses: []), now);

        var updated = _engine.AdvanceFetchSequence(started, now.AddSeconds(3));

        Assert.Equal(FetchStage.Pickup, updated.ActiveFetchSequence?.Stage);
        Assert.Equal(AnimationFamily.PickupBall, updated.CurrentActionVisualIntent?.Family);
        Assert.Equal(PropOverlayKind.Ball, updated.CurrentActionVisualIntent?.Overlay);
    }

    [Fact]
    public void ResolveFetchStageIntentFallsBackWhenOptionalFamilyMissing()
    {
        var available = new HashSet<AnimationFamily> { AnimationFamily.PlayBall, AnimationFamily.Happy };

        var intent = PetSimulationEngine.ResolveFetchStageIntent(FetchStage.Pickup, available);

        Assert.Equal(AnimationFamily.PlayBall, intent.Family);
        Assert.Equal(PropOverlayKind.Ball, intent.Overlay);
    }
}
