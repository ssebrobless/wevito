using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class SpriteAssetServiceFrameSelectionTests
{
    [Fact]
    public void ResolveFrameIndex_UsesForcedVisualQaFrameWhenPresent()
    {
        var pet = CreatePet() with { VisualQaForcedFrameIndex = 3 };

        var frameIndex = SpriteAssetService.ResolveFrameIndex(
            pet,
            frameCount: 6,
            now: DateTimeOffset.Parse("2026-05-14T12:00:10Z"),
            frameDuration: 250);

        Assert.Equal(3, frameIndex);
    }

    [Fact]
    public void ResolveFrameIndex_ClampsForcedVisualQaFrameToAvailableFrames()
    {
        var pet = CreatePet() with { VisualQaForcedFrameIndex = 99 };

        var frameIndex = SpriteAssetService.ResolveFrameIndex(
            pet,
            frameCount: 4,
            now: DateTimeOffset.Parse("2026-05-14T12:00:10Z"),
            frameDuration: 250);

        Assert.Equal(3, frameIndex);
    }

    [Fact]
    public void ResolveFrameIndex_AppliesVisualQaPlaybackSpeedWhenLooping()
    {
        var start = DateTimeOffset.Parse("2026-05-14T12:00:00Z");
        var pet = CreatePet() with
        {
            AnimationStartedAtUtc = start,
            VisualQaPlaybackSpeed = 2
        };

        var frameIndex = SpriteAssetService.ResolveFrameIndex(
            pet,
            frameCount: 6,
            now: start.AddMilliseconds(500),
            frameDuration: 250);

        Assert.Equal(4, frameIndex);
    }

    private static PetActor CreatePet()
    {
        return new PetActor(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "QA Pet",
            "goose",
            AgeStage: PetAgeStage.Baby,
            Gender: PetGender.Female,
            ColorVariant: "blue",
            ActiveStatuses: []);
    }
}
