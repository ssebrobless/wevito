using System.Text.Json;
using Blake3;
using SkiaSharp;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowCandidateImportRequest(
    string RepoRoot,
    SpriteRowKey Target,
    string SourceFolder,
    DateTimeOffset ImportedAtUtc);

public sealed record SpriteWorkflowCandidateImportResult(
    bool Succeeded,
    string CandidateFolder,
    string ManifestPath,
    SpriteWorkflowCandidateImportManifest? Manifest,
    string Message);

public sealed class SpriteWorkflowCandidateImporter
{
    public SpriteWorkflowCandidateImportResult Import(SpriteWorkflowCandidateImportRequest request)
    {
        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var sourceFolder = Path.GetFullPath(request.SourceFolder);
        if (!Directory.Exists(sourceFolder))
        {
            return Fail("Candidate source folder does not exist.");
        }

        var candidateFiles = Directory.EnumerateFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidateFiles.Count == 0)
        {
            return Fail("Candidate source folder does not contain PNG frames.");
        }

        var candidateFolder = ResolveCandidateFolder(repoRoot, request.Target, request.ImportedAtUtc);
        var authoredRoot = Path.Combine(repoRoot, "sprites_authored");
        if (!IsPathUnderRoot(candidateFolder, authoredRoot) ||
            !candidateFolder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => string.Equals(part, ".candidates", StringComparison.OrdinalIgnoreCase)))
        {
            return Fail("Candidate destination failed the .candidates path safety check.");
        }

        if (Directory.Exists(candidateFolder))
        {
            return Fail("Candidate destination already exists; refusing to overwrite.");
        }

        Directory.CreateDirectory(candidateFolder);
        var importedFrames = new List<SpriteWorkflowFrameEntry>();
        for (var index = 0; index < candidateFiles.Count; index++)
        {
            var targetFileName = $"{request.Target.Family}_{index:00}.png";
            var destination = Path.Combine(candidateFolder, targetFileName);
            File.Copy(candidateFiles[index], destination, overwrite: false);
            importedFrames.Add(BuildFrameEntry(SpriteWorkflowRootKind.Candidate, candidateFolder, destination));
        }

        var manifest = new SpriteWorkflowCandidateImportManifest(
            "1",
            request.Target,
            sourceFolder,
            candidateFolder,
            importedFrames,
            request.ImportedAtUtc);
        var manifestPath = Path.Combine(candidateFolder, "candidate-import.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));

        return new SpriteWorkflowCandidateImportResult(
            true,
            candidateFolder,
            manifestPath,
            manifest,
            $"Imported {importedFrames.Count} candidate frame(s) to .candidates.");
    }

    public static string ResolveCandidateFolder(string repoRoot, SpriteRowKey target, DateTimeOffset timestamp)
    {
        var safeStamp = timestamp.ToString("yyyyMMdd-HHmmss");
        return Path.GetFullPath(Path.Combine(
            repoRoot,
            "sprites_authored",
            target.Species,
            FormatAge(target.AgeStage),
            FormatGender(target.Gender),
            target.ColorVariant,
            ".candidates",
            $"{target.Family}-{safeStamp}"));
    }

    private static SpriteWorkflowFrameEntry BuildFrameEntry(SpriteWorkflowRootKind rootKind, string rootPath, string absolutePath)
    {
        using var bitmap = SKBitmap.Decode(absolutePath);
        var geometry = bitmap is null
            ? new SpriteFrameGeometry(0, 0)
            : new SpriteFrameGeometry(bitmap.Width, bitmap.Height);
        var hash = Hasher.Hash(File.ReadAllBytes(absolutePath));

        return new SpriteWorkflowFrameEntry(
            rootKind,
            Path.GetFileNameWithoutExtension(absolutePath),
            Path.GetRelativePath(rootPath, absolutePath).Replace(Path.DirectorySeparatorChar, '/'),
            Path.GetFullPath(absolutePath),
            Convert.ToHexString(hash.AsSpan()).ToLowerInvariant(),
            geometry);
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAge(PetAgeStage age) => age.ToString().ToLowerInvariant();

    private static string FormatGender(PetGender gender) => gender.ToString().ToLowerInvariant();

    private static SpriteWorkflowCandidateImportResult Fail(string message)
    {
        return new SpriteWorkflowCandidateImportResult(false, "", "", null, message);
    }
}
