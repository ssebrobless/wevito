using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class SpriteAssetServiceOptionalFamilyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"wevito-sprite-service-{Guid.NewGuid():N}");

    [Fact]
    public void OptionalFamilyFolderExists_SelectsOptionalFolderFrames()
    {
        var runtimeRoot = Path.Combine(_root, "runtime");
        var authoredRoot = Path.Combine(_root, "authored");
        var petDirectory = Path.Combine(runtimeRoot, "goose", "baby", "female", "blue");
        var optionalDirectory = Path.Combine(petDirectory, "drink");
        Directory.CreateDirectory(optionalDirectory);
        var optionalFrame = Path.Combine(optionalDirectory, "drink_00.png");
        var baseFrame = Path.Combine(petDirectory, "eat_00.png");
        File.WriteAllBytes(optionalFrame, []);
        File.WriteAllBytes(baseFrame, []);
        var service = CreateService(authoredRoot, runtimeRoot);
        var pet = CreatePet();

        var frames = service.GetAnimationFramePaths(pet, "drink");

        Assert.Equal([optionalFrame], frames);
    }

    [Fact]
    public void OptionalFamilyFolderMissing_SelectsBaseFrames()
    {
        var runtimeRoot = Path.Combine(_root, "runtime");
        var authoredRoot = Path.Combine(_root, "authored");
        var petDirectory = Path.Combine(runtimeRoot, "goose", "baby", "female", "blue");
        Directory.CreateDirectory(petDirectory);
        var baseFrame = Path.Combine(petDirectory, "drink_00.png");
        File.WriteAllBytes(baseFrame, []);
        var service = CreateService(authoredRoot, runtimeRoot);
        var pet = CreatePet();

        var frames = service.GetAnimationFramePaths(pet, "drink");

        Assert.Equal([baseFrame], frames);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static SpriteAssetService CreateService(string authoredRoot, string runtimeRoot)
    {
        return new SpriteAssetService(
            authoredRoot,
            runtimeRoot,
            Path.Combine(Path.GetTempPath(), "wevito-shared-runtime-empty"),
            Path.Combine(Path.GetTempPath(), "wevito-shared-empty"),
            preferVerifiedLocomotion: false,
            preferAuthoredAll: false);
    }

    private static PetActor CreatePet()
    {
        return new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            AgeStage: PetAgeStage.Baby,
            Gender: PetGender.Female,
            ColorVariant: "blue",
            ActiveStatuses: []);
    }
}
