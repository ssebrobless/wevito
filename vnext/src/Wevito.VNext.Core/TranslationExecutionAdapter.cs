using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class TranslationExecutionAdapter
{
    private const string ToolFamily = "translateText";
    private const int MaxRequestBytes = 128 * 1024;
    private readonly Func<string, DeepLTranslationClient> _deepLClientFactory;
    private readonly IReadOnlyDictionary<string, string?> _environment;
    private readonly TranslationGlossaryService _glossaryService;

    public TranslationExecutionAdapter(
        Func<string, DeepLTranslationClient>? deepLClientFactory = null,
        IReadOnlyDictionary<string, string?>? environment = null,
        TranslationGlossaryService? glossaryService = null)
    {
        _deepLClientFactory = deepLClientFactory ?? (authKey => new DeepLTranslationClient(new HttpClient(), authKey));
        _environment = environment ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => entry.Key.ToString() ?? string.Empty,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);
        _glossaryService = glossaryService ?? new TranslationGlossaryService();
    }

    public async Task<TaskAdapterResult> ExecuteAsync(TaskAdapterRequest request, DateTimeOffset? nowUtc = null, CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.Execute)
        {
            return Block(request, "Translation execution requires explicit execute mode.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target translateText.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.Network ||
            request.PolicySnapshot.ApprovalRequirement == ApprovalRequirement.None)
        {
            return Block(request, "Translation execution requires an approval-gated network policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Translation execution artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var authKey = ResolveDeepLAuthKey(_environment);
        if (string.IsNullOrWhiteSpace(authKey))
        {
            return Block(request, "DeepL credentials are missing. Set DEEPL_API_KEY or DEEPL_AUTH_KEY before translation execution.", timestamp);
        }

        var parse = ParseRequest(request.Intent.RawText);
        if (parse.TargetLanguageCode == "UNSPECIFIED")
        {
            return Block(request, "Translation execution requires a target language.", timestamp);
        }

        if (Encoding.UTF8.GetByteCount(parse.Text) > MaxRequestBytes)
        {
            return Block(request, "Translation text exceeds DeepL's 128 KiB request limit.", timestamp);
        }

        var client = _deepLClientFactory(authKey);
        var glossaryEntries = _glossaryService.FindApplicableEntries(parse.Text, parse.SourceLanguageCode, parse.TargetLanguageCode);
        var protectedText = _glossaryService.ProtectTerms(parse.Text, glossaryEntries);
        var providerSourceLanguageCode = !string.IsNullOrWhiteSpace(parse.SourceLanguageCode)
            ? parse.SourceLanguageCode
            : glossaryEntries.Count > 0 ? "EN" : string.Empty;
        var glossaryMode = glossaryEntries.Count == 0
            ? "none"
            : "protected-token-shim";
        var providerFallbackUsed = glossaryEntries.Count > 0;
        var glossaryId = string.Empty;

        if (glossaryEntries.Count > 0 && IsLikelyDeepLProKey(authKey))
        {
            try
            {
                glossaryId = await client.CreateGlossaryAsync(
                    $"Wevito {providerSourceLanguageCode}-{parse.TargetLanguageCode}",
                    providerSourceLanguageCode,
                    parse.TargetLanguageCode,
                    glossaryEntries,
                    cancellationToken).ConfigureAwait(false);
                glossaryMode = "deepl-native-v3";
                providerFallbackUsed = false;
            }
            catch (InvalidOperationException)
            {
                glossaryId = string.Empty;
                glossaryMode = "protected-token-shim-after-native-glossary-failure";
                providerFallbackUsed = true;
            }
        }

        var textForProvider = string.IsNullOrWhiteSpace(glossaryId) ? protectedText.Text : parse.Text;
        var response = await client.TranslateAsync(textForProvider, parse.TargetLanguageCode, providerSourceLanguageCode, glossaryId, cancellationToken).ConfigureAwait(false);
        var translatedText = string.IsNullOrWhiteSpace(glossaryId)
            ? _glossaryService.RestoreProtectedTerms(response.Text, protectedText.Replacements)
            : response.Text;
        var qaWarnings = TranslationGlossaryService.BuildQaWarnings(
            parse.Text,
            translatedText,
            glossaryEntries,
            glossaryMode,
            providerFallbackUsed);
        var report = new TranslationExecutionReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            TranslationProviderKind.DeepL,
            parse.Text,
            translatedText,
            string.IsNullOrWhiteSpace(providerSourceLanguageCode) ? "auto" : providerSourceLanguageCode,
            parse.TargetLanguageCode,
            response.DetectedSourceLanguage,
            parse.Text.Length,
            response.BilledCharacters,
            glossaryMode,
            glossaryEntries,
            qaWarnings,
            BuildSafetyNotes(),
            DidCallProvider: true,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var textPath = Path.Combine(artifactRoot, "translated-text.txt");
        var jsonPath = Path.Combine(artifactRoot, "translation-execution-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        await File.WriteAllTextAsync(textPath, translatedText, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), cancellationToken).ConfigureAwait(false);

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Completed,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [textPath, jsonPath, markdownPath],
            PreviewSummary: "",
            ResultSummary: $"translateText execution complete: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static string? ResolveDeepLAuthKey(IReadOnlyDictionary<string, string?> environment)
    {
        return TryGetNonEmpty(environment, "DEEPL_API_KEY") ??
               TryGetNonEmpty(environment, "DEEPL_AUTH_KEY");
    }

    private static string? TryGetNonEmpty(IReadOnlyDictionary<string, string?> environment, string key)
    {
        return environment.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static bool IsLikelyDeepLProKey(string authKey)
    {
        return !authKey.Trim().EndsWith(":fx", StringComparison.OrdinalIgnoreCase);
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
            MapLanguageCode(sourceLanguage, isSource: true),
            MapLanguageCode(targetLanguage, isSource: false));
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
        return match.Success
            ? match.Groups["language"].Value.Trim(' ', '.', ',', ';', ':')
            : string.Empty;
    }

    private static string MapLanguageCode(string language, bool isSource)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return isSource ? string.Empty : "UNSPECIFIED";
        }

        return language.Trim().ToLowerInvariant() switch
        {
            "english" or "en" => isSource ? "EN" : "EN-US",
            "spanish" or "es" => "ES",
            "french" or "fr" => "FR",
            "german" or "de" => "DE",
            "japanese" or "ja" => "JA",
            "italian" or "it" => "IT",
            "portuguese" or "pt" => "PT-BR",
            "polish" or "pl" => "PL",
            "dutch" or "nl" => "NL",
            "chinese" or "zh" => "ZH",
            var code when code.Length is 2 or 5 => code.ToUpperInvariant(),
            _ => "UNSPECIFIED"
        };
    }

    private static IReadOnlyList<string> BuildSafetyNotes()
    {
        return
        [
            "DeepL was called only because execute mode and an approval-gated network policy were provided.",
            "The API key was read from the process environment and was not written to artifacts.",
            "Only translated output and metadata were written to the artifact folder.",
            "No project files or assets were mutated."
        ];
    }

    private static string BuildMarkdown(TranslationExecutionReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Translation Result",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- Provider: {report.Provider}",
            $"- Source language: {report.SourceLanguage}",
            $"- Target language: {report.TargetLanguage}",
            $"- Detected source language: {report.DetectedSourceLanguage}",
            $"- Character count: {report.CharacterCount}",
            $"- Billed characters: {(report.BilledCharacters?.ToString() ?? "not reported")}",
            "- Provider called: true",
            "- Did mutate files: false",
            $"- Glossary mode: {report.GlossaryMode}",
            "",
            "## Translated Text",
            "",
            "```text",
            report.TranslatedText,
            "```",
            "",
            "## Applied Glossary Entries",
            ""
        };

        if (report.AppliedGlossaryEntries.Count == 0)
        {
            lines.Add("- None.");
        }
        else
        {
            lines.AddRange(report.AppliedGlossaryEntries.Select(entry =>
                $"- `{entry.Source}` -> `{entry.Target}` ({(entry.CaseSensitive ? "case-sensitive" : "case-insensitive")}): {entry.Notes}"));
        }

        lines.Add("");
        lines.Add("## QA Warnings");
        lines.Add("");
        if (report.QaWarnings.Count == 0)
        {
            lines.Add("- None.");
        }
        else
        {
            lines.AddRange(report.QaWarnings.Select(warning => $"- {warning}"));
        }

        lines.AddRange(
        [
            "",
            "## Safety Notes",
            ""
        ]);

        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-translation-execution";
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
        string SourceLanguageCode,
        string TargetLanguageCode);
}
