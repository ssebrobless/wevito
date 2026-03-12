using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed class SpriteAssetService
{
    private readonly string _petSpriteRoot;
    private readonly string _sharedAssetRoot;
    private readonly ConcurrentDictionary<string, BitmapImage?> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _animationCache = new(StringComparer.OrdinalIgnoreCase);

    public SpriteAssetService(string petSpriteRoot, string sharedAssetRoot)
    {
        _petSpriteRoot = petSpriteRoot;
        _sharedAssetRoot = sharedAssetRoot;
    }

    public ImageSource? GetPetFrame(PetActor pet, DateTimeOffset now)
    {
        var animationId = pet.CurrentAnimationState.ToString().ToLowerInvariant();
        var frames = GetAnimationFrames(pet, animationId);
        if (frames.Count == 0 && !string.Equals(animationId, "idle", StringComparison.Ordinal))
        {
            frames = GetAnimationFrames(pet, "idle");
        }

        if (frames.Count == 0)
        {
            return null;
        }

        var animationStart = pet.AnimationStartedAtUtc == default ? now : pet.AnimationStartedAtUtc;
        var frameDuration = GetFrameDuration(pet.CurrentAnimationState);
        var elapsed = Math.Max(0, (now - animationStart).TotalMilliseconds);
        var frameIndex = (int)(elapsed / frameDuration) % frames.Count;
        return LoadImage(frames[frameIndex]);
    }

    public ImageSource? GetEnvironment(string assetId)
    {
        return LoadSharedAsset(Path.Combine("environment", $"{assetId}.png"));
    }

    public ImageSource? GetIcon(string iconId)
    {
        return LoadSharedAsset(Path.Combine("icons", $"{iconId}.png"));
    }

    public ImageSource? GetStatusIcon(string statusId)
    {
        return LoadSharedAsset(Path.Combine("status", $"{statusId}.png"));
    }

    public ImageSource? GetCelestial(DateTimeOffset now, bool nightMode)
    {
        var prefix = nightMode ? "moon" : "sun";
        var folder = Path.Combine(_sharedAssetRoot, "celestial");
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var frames = Directory
            .EnumerateFiles(folder, $"{prefix}_*.png", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (frames.Count == 0)
        {
            return null;
        }

        var frameIndex = (int)((now.TimeOfDay.TotalSeconds / 2.5) % frames.Count);
        return LoadImage(frames[frameIndex]);
    }

    public double GetPetScale(PetActor pet)
    {
        return pet.AgeStage switch
        {
            PetAgeStage.Baby => 2.2,
            PetAgeStage.Teen => 2.6,
            _ => 3.0
        };
    }

    private IReadOnlyList<string> GetAnimationFrames(PetActor pet, string animationId)
    {
        var key = $"{pet.SpeciesId}|{pet.AgeStage}|{pet.Gender}|{pet.ColorVariant}|{animationId}";
        return _animationCache.GetOrAdd(key, _ => ResolveAnimationFrames(pet, animationId));
    }

    private IReadOnlyList<string> ResolveAnimationFrames(PetActor pet, string animationId)
    {
        var directory = BuildPetDirectory(pet);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, $"{animationId}_*.png", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string BuildPetDirectory(PetActor pet)
    {
        var age = pet.AgeStage.ToString().ToLowerInvariant();
        var gender = pet.Gender.ToString().ToLowerInvariant();
        var color = pet.ColorVariant.ToLowerInvariant();
        return Path.Combine(_petSpriteRoot, pet.SpeciesId, age, gender, color);
    }

    private ImageSource? LoadSharedAsset(string relativePath)
    {
        return LoadImage(Path.Combine(_sharedAssetRoot, relativePath));
    }

    private BitmapImage? LoadImage(string fullPath)
    {
        return _imageCache.GetOrAdd(fullPath, path =>
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        });
    }

    private static double GetFrameDuration(PetAnimationState animationState)
    {
        return animationState switch
        {
            PetAnimationState.Walk => 120,
            PetAnimationState.Eat => 140,
            PetAnimationState.Happy => 150,
            PetAnimationState.Bathe => 170,
            PetAnimationState.Sick => 220,
            PetAnimationState.Sad => 260,
            PetAnimationState.Sleep => 420,
            _ => 240
        };
    }
}
