using System.Text.Json;
using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class TranslationPreviewAdapter
{
    private const string ToolFamily = "translateText";
    private readonly TranslationProviderRouter _providerRouter;

    public TranslationPreviewAdapter(TranslationProviderRouter? providerRouter = null)
    {
        _providerRouter = providerRouter ?? new TranslationProviderRouter();
    }

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Translation preview only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target translateText.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Translation preview requires a read-only policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Translation preview artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var parse = ParseRequest(request.Intent.RawText);
        var providers = _providerRouter.GetProviderStatuses();
        var preferred = _providerRouter.SelectPreferredProvider(providers);
        var report = new TranslationPreviewReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            parse.Text,
            parse.SourceLanguage,
            parse.TargetLanguage,
            preferred.Provider.ToString(),
            parse.Text.Length,
            providers,
            BuildSafetyNotes(preferred),
            DidCallProvider: false,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "translation-preview-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote translateText preview for {report.CharacterCount} character(s). No provider was called.",
            ResultSummary: $"translateText preview ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static TranslationParseResult ParseRequest(string rawText)
    {
        var targetLanguage = ExtractLanguage(rawText, @"\bto\s+(?<language>[A-Za-z][A-Za-z -]{1,40})");
        var sourceLanguage = ExtractLanguage(rawText, @"\bfrom\s+(?<language>[A-Za-z][A-Za-z -]{1,40})");
        var text = ExtractQuotedText(rawText);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = Regex.Replace(rawText, @"\b(please\s+)?(translate|translation|make this|turn this into)\b", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(from|to)\s+[A-Za-z][A-Za-z -]{1,40}", "", RegexOptions.IgnoreCase).Trim(' ', '.', ':', '-');
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = rawText.Trim();
        }

        return new TranslationParseResult(
            text,
            string.IsNullOrWhiteSpace(sourceLanguage) ? "auto" : sourceLanguage,
            string.IsNullOrWhiteSpace(targetLanguage) ? "unspecified" : targetLanguage);
    }

    private static string ExtractQuotedText(string rawText)
    {
        var match = Regex.Match(rawText, "\"(?<text>[^\"]+)\"");
        return match.Success
            ? match.Groups["text"].Value.Trim()
            : string.Empty;
    }

    private static string ExtractLanguage(string rawText, string pattern)
    {
        var match = Regex.Match(rawText, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return string.Empty;
        }

        return match.Groups["language"].Value.Trim(' ', '.', ',', ';', ':');
    }

    private static IReadOnlyList<string> BuildSafetyNotes(TranslationProviderStatus preferred)
    {
        return
        [
            "No translation provider was called by this preview.",
            "No text was sent over the network.",
            "A future execution adapter must require explicit approval before sending text to a provider.",
            preferred.Availability == TranslationProviderAvailability.Configured
                ? $"Preferred provider appears configured: {preferred.Provider}."
                : $"Preferred provider is not ready yet: {preferred.Detail}"
        ];
    }

    private static string BuildMarkdown(TranslationPreviewReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Translation Preview",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- Source language: {report.SourceLanguage}",
            $"- Target language: {report.TargetLanguage}",
            $"- Character count: {report.CharacterCount}",
            $"- Preferred provider: {report.PreferredProvider}",
            "- Provider called: false",
            "- Did mutate files: false",
            "",
            "## Text Preview",
            "",
            "```text",
            report.RequestedText,
            "```",
            "",
            "## Provider Status",
            ""
        };

        lines.AddRange(report.Providers.Select(provider => $"- {provider.Provider}: {provider.Availability} - {provider.Detail}"));
        lines.Add("");
        lines.Add("## Safety Notes");
        lines.Add("");
        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-translation-preview";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug));
    }

    private static bool IsSafePetTaskArtifactRoot(string artifactRoot)
    {
        var fullPath = Path.GetFullPath(artifactRoot);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length >= 2 &&
               parts.Any(part => string.Equals(part, "pet-tasks", StringComparison.OrdinalIgnoreCase)) &&
               !parts.Any(part => part.StartsWith("candidate-frames", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("backup-before-", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("godot-packaged-proof-", StringComparison.OrdinalIgnoreCase));
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }

    private sealed record TranslationParseResult(
        string Text,
        string SourceLanguage,
        string TargetLanguage);
}
