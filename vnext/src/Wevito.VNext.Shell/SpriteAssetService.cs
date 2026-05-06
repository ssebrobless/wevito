using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed class SpriteAssetService
{
    private readonly string _authoredPetSpriteRoot;
    private readonly string _petSpriteRoot;
    private readonly string _sharedAssetRuntimeRoot;
    private readonly string _sharedAssetRoot;
    private readonly bool _preferVerifiedLocomotion;
    private readonly bool _preferAuthoredAll;
    private readonly ConcurrentDictionary<string, BitmapImage?> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _animationCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _traceOnceCache = new(StringComparer.OrdinalIgnoreCase);

    public SpriteAssetService(
        string authoredPetSpriteRoot,
        string petSpriteRoot,
        string sharedAssetRuntimeRoot,
        string sharedAssetRoot,
        bool preferVerifiedLocomotion,
        bool preferAuthoredAll)
    {
        _authoredPetSpriteRoot = authoredPetSpriteRoot;
        _petSpriteRoot = petSpriteRoot;
        _sharedAssetRuntimeRoot = sharedAssetRuntimeRoot;
        _sharedAssetRoot = sharedAssetRoot;
        _preferVerifiedLocomotion = preferVerifiedLocomotion;
        _preferAuthoredAll = preferAuthoredAll;
    }

    public ImageSource? GetPetFrame(PetActor pet, DateTimeOffset now)
    {
        var animationId = GetAnimationId(pet);
        var frames = GetAnimationFrames(pet, animationId);
        var fallbackAnimationId = GetFallbackAnimationId(pet.CurrentAnimationState);
        if (frames.Count == 0 && !string.Equals(animationId, fallbackAnimationId, StringComparison.Ordinal))
        {
            TraceOnce(
                "sprite-fallback",
                $"{pet.SpeciesId}|{pet.AgeStage}|{pet.Gender}|{pet.ColorVariant}|{animationId}|{fallbackAnimationId}",
                $"species={pet.SpeciesId} age={pet.AgeStage} gender={pet.Gender} color={pet.ColorVariant} anim={animationId} fallback={fallbackAnimationId}");
            frames = GetAnimationFrames(pet, fallbackAnimationId);
        }

        if (frames.Count == 0 && !string.Equals(fallbackAnimationId, "idle", StringComparison.Ordinal))
        {
            TraceOnce(
                "sprite-fallback",
                $"{pet.SpeciesId}|{pet.AgeStage}|{pet.Gender}|{pet.ColorVariant}|{animationId}|idle",
                $"species={pet.SpeciesId} age={pet.AgeStage} gender={pet.Gender} color={pet.ColorVariant} anim={animationId} fallback=idle");
            frames = GetAnimationFrames(pet, "idle");
        }

        if (frames.Count == 0)
        {
            TraceOnce(
                "sprite-missing",
                $"{pet.SpeciesId}|{pet.AgeStage}|{pet.Gender}|{pet.ColorVariant}|{animationId}",
                $"species={pet.SpeciesId} age={pet.AgeStage} gender={pet.Gender} color={pet.ColorVariant} anim={animationId}");
            return null;
        }

        var animationStart = pet.AnimationStartedAtUtc == default ? now : pet.AnimationStartedAtUtc;
        var frameDuration = GetFrameDuration(pet.CurrentAnimationState);
        var elapsed = Math.Max(0, (now - animationStart).TotalMilliseconds);
        var frameIndex = (int)(elapsed / frameDuration) % frames.Count;
        return LoadImage(frames[frameIndex]);
    }

    public IReadOnlyList<string> GetAnimationFramePaths(PetActor pet, string animationId)
    {
        return GetAnimationFrames(pet, animationId);
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

    public ImageSource? GetItem(string categoryFolder, string assetId)
    {
        return LoadSharedAsset(Path.Combine("items", categoryFolder, $"{assetId}.png"));
    }

    public ImageSource? GetPortrait(string speciesId)
    {
        return LoadSharedAsset(Path.Combine("portraits", $"{speciesId}.png"));
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
        // Keep pet rendering on integer multiples so WPF does not soften the sprites.
        return pet.SpeciesId switch
        {
            "snake" => 2.0,
            "frog" => 3.0,
            _ => 2.0
        };
    }

    public void InvalidateCache(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _imageCache.TryRemove(fullPath, out _);

        foreach (var entry in _animationCache)
        {
            if (entry.Value.Any(frame => string.Equals(Path.GetFullPath(frame), fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                _animationCache.TryRemove(entry.Key, out _);
            }
        }
    }

    private IReadOnlyList<string> GetAnimationFrames(PetActor pet, string animationId)
    {
        var key = $"{pet.SpeciesId}|{pet.AgeStage}|{pet.Gender}|{pet.ColorVariant}|{animationId}";
        return _animationCache.GetOrAdd(key, _ => ResolveAnimationFrames(pet, animationId));
    }

    private IReadOnlyList<string> ResolveAnimationFrames(PetActor pet, string animationId)
    {
        var runtimeFrames = EnumerateAnimationFrames(BuildPetDirectory(_petSpriteRoot, pet), animationId);
        var authoredFrames = EnumerateAnimationFrames(BuildPetDirectory(_authoredPetSpriteRoot, pet), animationId);

        if (ShouldPreferAuthored(animationId, authoredFrames.Count > 0))
        {
            return authoredFrames;
        }

        if (runtimeFrames.Count > 0)
        {
            return runtimeFrames;
        }

        return authoredFrames;
    }

    private bool ShouldPreferAuthored(string animationId, bool hasAuthoredFrames)
    {
        if (!hasAuthoredFrames)
        {
            return false;
        }

        if (_preferAuthoredAll)
        {
            return true;
        }

        return _preferVerifiedLocomotion &&
               (string.Equals(animationId, "idle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(animationId, "walk", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> EnumerateAnimationFrames(string directory, string animationId)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var optionalFamilyDirectory = Path.Combine(directory, animationId);
        if (Directory.Exists(optionalFamilyDirectory))
        {
            var optionalFrames = Directory
                .EnumerateFiles(optionalFamilyDirectory, "*.png", SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (optionalFrames.Count > 0)
            {
                return optionalFrames;
            }
        }

        return Directory
            .EnumerateFiles(directory, $"{animationId}_*.png", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildPetDirectory(string root, PetActor pet)
    {
        var age = pet.AgeStage.ToString().ToLowerInvariant();
        var gender = pet.Gender.ToString().ToLowerInvariant();
        var color = pet.ColorVariant.ToLowerInvariant();
        return Path.Combine(root, pet.SpeciesId, age, gender, color);
    }

    private ImageSource? LoadSharedAsset(string relativePath)
    {
        var cleanedPath = Path.Combine(_sharedAssetRuntimeRoot, relativePath);
        var image = LoadImage(cleanedPath);
        if (image is not null)
        {
            if (relativePath.Contains("hay_bed", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Contains("nest_bed", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Contains("water_bowl", StringComparison.OrdinalIgnoreCase))
            {
                TraceLog.Write("shared-asset", $"cleaned {relativePath} => {cleanedPath}");
            }
            return image;
        }

        var fallbackPath = Path.Combine(_sharedAssetRoot, relativePath);
        var fallback = LoadImage(fallbackPath);
        if (fallback is not null &&
            (relativePath.Contains("hay_bed", StringComparison.OrdinalIgnoreCase) ||
             relativePath.Contains("nest_bed", StringComparison.OrdinalIgnoreCase) ||
             relativePath.Contains("water_bowl", StringComparison.OrdinalIgnoreCase)))
        {
            TraceLog.Write("shared-asset", $"fallback {relativePath} => {fallbackPath}");
        }

        return fallback;
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

    private void TraceOnce(string category, string key, string message)
    {
        if (_traceOnceCache.TryAdd($"{category}|{key}", 0))
        {
            TraceLog.Write(category, message);
        }
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
            PetAnimationState.Waving => 150,
            PetAnimationState.Jumping => 120,
            PetAnimationState.Failed => 220,
            PetAnimationState.Waiting => 300,
            PetAnimationState.Review => 180,
            _ => 240
        };
    }

    private static string GetAnimationId(PetActor pet)
    {
        if (pet.CurrentActionVisualIntent is { } intent)
        {
            return intent.Family switch
            {
                AnimationFamily.Walk => "walk",
                AnimationFamily.Eat => "eat",
                AnimationFamily.Happy => "happy",
                AnimationFamily.Sad => "sad",
                AnimationFamily.Sleep => "sleep",
                AnimationFamily.Sick => "sick",
                AnimationFamily.Bathe => "bathe",
                AnimationFamily.Drink => "drink",
                AnimationFamily.PlayBall => "play_ball",
                AnimationFamily.HoldBall => "hold_ball",
                AnimationFamily.PickupBall => "pickup_ball",
                AnimationFamily.DropBall => "drop_ball",
                AnimationFamily.CarryBallWalk => "carry_ball_walk",
                AnimationFamily.CarryBallRun => "carry_ball_run",
                _ => "idle"
            };
        }

        return pet.CurrentAnimationState.ToString().ToLowerInvariant();
    }

    private static string GetFallbackAnimationId(PetAnimationState animationState)
    {
        return animationState switch
        {
            PetAnimationState.Waving => "happy",
            PetAnimationState.Jumping => "happy",
            PetAnimationState.Failed => "sad",
            PetAnimationState.Waiting => "idle",
            PetAnimationState.Review => "idle",
            _ => animationState.ToString().ToLowerInvariant()
        };
    }
}
