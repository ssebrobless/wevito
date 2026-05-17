using System.Text.Json;
using Blake3;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowDryRunApplyRequest(
    string RepoRoot,
    SpriteRowKey Target,
    string CandidateFolder,
    string ArtifactRoot,
    DateTimeOffset GeneratedAtUtc);

public sealed record SpriteWorkflowDryRunApplyResult(
    bool Succeeded,
    string ManifestPath,
    SpriteWorkflowDryRunApplyManifest? Manifest,
    string Message);

public sealed class SpriteWorkflowDryRunApplyService
{
    public SpriteWorkflowDryRunApplyResult Plan(SpriteWorkflowDryRunApplyRequest request)
    {
        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var candidateFolder = Path.GetFullPath(request.CandidateFolder);
        var authoredRoot = Path.Combine(repoRoot, "sprites_authored");
        if (!IsPathUnderRoot(candidateFolder, authoredRoot) ||
            !candidateFolder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => string.Equals(part, ".candidates", StringComparison.OrdinalIgnoreCase)))
        {
            return Fail("Candidate folder must be under sprites_authored/.candidates.");
        }

        if (!Directory.Exists(candidateFolder))
        {
            return Fail("Candidate folder does not exist.");
        }

        var candidateFiles = Directory.EnumerateFiles(candidateFolder, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidateFiles.Count == 0)
        {
            return Fail("Candidate folder does not contain PNG frames.");
        }

        var runtimeRowFolder = ResolveRuntimeRowFolder(repoRoot, request.Target);
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        var backupFolder = Path.Combine(
            artifactRoot,
            "backup",
            $"{request.Target.Species}-{FormatAge(request.Target.AgeStage)}-{FormatGender(request.Target.Gender)}-{request.Target.ColorVariant}-{request.Target.Family}-{request.GeneratedAtUtc:yyyyMMdd-HHmmss}");
        var changes = candidateFiles.Select(candidatePath =>
        {
            var frameId = Path.GetFileNameWithoutExtension(candidatePath);
            var runtimePath = Path.Combine(runtimeRowFolder, Path.GetFileName(candidatePath));
            var backupPath = Path.Combine(backupFolder, Path.GetFileName(candidatePath));
            return new SpriteWorkflowDryRunChange(
                frameId,
                runtimePath,
                candidatePath,
                backupPath,
                File.Exists(runtimePath) ? ComputeBlake3(runtimePath) : "",
                ComputeBlake3(candidatePath),
                WouldOverwriteRuntime: File.Exists(runtimePath));
        }).ToList();

        var manifest = new SpriteWorkflowDryRunApplyManifest(
            "1",
            request.Target,
            candidateFolder,
            runtimeRowFolder,
            backupFolder,
            changes,
            WouldMutateRuntime: true,
            request.GeneratedAtUtc);

        Directory.CreateDirectory(artifactRoot);
        var manifestPath = Path.Combine(artifactRoot, "dry-run-apply.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));

        return new SpriteWorkflowDryRunApplyResult(
            true,
            manifestPath,
            manifest,
            $"Dry-run planned {changes.Count} runtime overwrite candidate(s). No runtime files were changed.");
    }

    public static string ResolveRuntimeRowFolder(string repoRoot, SpriteRowKey target)
    {
        return Path.GetFullPath(Path.Combine(
            repoRoot,
            "sprites_runtime",
            target.Species,
            FormatAge(target.AgeStage),
            FormatGender(target.Gender),
            target.ColorVariant));
    }

    private static string ComputeBlake3(string absolutePath)
    {
        var hash = Hasher.Hash(File.ReadAllBytes(absolutePath));
        return Convert.ToHexString(hash.AsSpan()).ToLowerInvariant();
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAge(PetAgeStage age) => age.ToString().ToLowerInvariant();

    private static string FormatGender(PetGender gender) => gender.ToString().ToLowerInvariant();

    private static SpriteWorkflowDryRunApplyResult Fail(string message)
    {
        return new SpriteWorkflowDryRunApplyResult(false, "", null, message);
    }
}
