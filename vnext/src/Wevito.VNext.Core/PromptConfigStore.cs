using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PromptConfig(
    string SchemaVersion,
    string ConfigId,
    string DatasetVersion,
    int TopK,
    double ConfidenceThreshold,
    IReadOnlyDictionary<string, string> Templates,
    DateTimeOffset CreatedAtUtc)
{
    public static PromptConfig CreateDefault(string datasetVersion, DateTimeOffset createdAtUtc)
    {
        return new PromptConfig(
            "1",
            $"prompt-{createdAtUtc:yyyyMMdd-HHmmss}",
            datasetVersion,
            TopK: 3,
            ConfidenceThreshold: 0.2,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["localDocs"] = "Summarize the local evidence packet. Cite file paths.",
                ["spriteAudit"] = "Review sprite evidence only. Do not mutate art.",
                ["localResearch"] = "Use approved local evidence first and record provenance."
            },
            createdAtUtc);
    }
}

public sealed record PromptConfigWriteResult(
    bool Succeeded,
    string ConfigPath,
    string BackupPath,
    string PreSha256,
    string PostSha256,
    string Message);

public sealed class PromptConfigStore
{
    public const string RelativeConfigPath = "local-ai/prompt-config.json";

    public PromptConfigWriteResult Write(string contentRoot, string artifactRoot, PromptConfig config, DateTimeOffset timestamp)
    {
        var configPath = ResolveContentPath(contentRoot, RelativeConfigPath);
        var contentRootFull = Path.GetFullPath(contentRoot);
        EnsureUnderRoot(configPath, contentRootFull, "Prompt config writes must stay under vnext/content.");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? contentRootFull);

        var backupPath = "";
        var preHash = "";
        if (File.Exists(configPath))
        {
            Directory.CreateDirectory(artifactRoot);
            var backupFolder = Path.Combine(Path.GetFullPath(artifactRoot), $"{timestamp:yyyyMMdd-HHmmss}-prompt-config-backup");
            Directory.CreateDirectory(backupFolder);
            backupPath = Path.Combine(backupFolder, Path.GetFileName(configPath));
            File.Copy(configPath, backupPath, overwrite: true);
            preHash = Sha256(configPath);
        }

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonDefaults.Options));
        return new PromptConfigWriteResult(true, configPath, backupPath, preHash, Sha256(configPath), "Prompt config written with backup metadata.");
    }

    public static string ResolveContentPath(string contentRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(contentRoot, relativePath));
    }

    public static void EnsureUnderRoot(string path, string root, string message)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message);
        }
    }

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
