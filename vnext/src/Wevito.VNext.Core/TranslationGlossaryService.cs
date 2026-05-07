using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class TranslationGlossaryService
{
    private readonly string _glossaryPath;

    public TranslationGlossaryService(string? glossaryPath = null)
    {
        _glossaryPath = string.IsNullOrWhiteSpace(glossaryPath)
            ? ResolveDefaultGlossaryPath()
            : Path.GetFullPath(glossaryPath);
    }

    public IReadOnlyList<TranslationGlossaryEntry> FindApplicableEntries(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var pair = FindLanguagePair(sourceLanguage, targetLanguage);
        if (pair is null)
        {
            return [];
        }

        var visibleText = string.Concat(TranslationTextSegments.Parse(text)
            .Where(segment => !segment.IsProtected)
            .Select(segment => segment.Text));
        return pair.Entries
            .Where(entry => ContainsTerm(visibleText, entry))
            .Select(entry => new TranslationGlossaryEntry(entry.Source, entry.Target, entry.CaseSensitive, entry.Notes))
            .ToArray();
    }

    public GlossaryProtectedText ProtectTerms(
        string text,
        IReadOnlyList<TranslationGlossaryEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new GlossaryProtectedText(text, []);
        }

        var replacements = new List<GlossaryReplacement>();
        var builder = new StringBuilder();
        foreach (var segment in TranslationTextSegments.Parse(text))
        {
            if (segment.IsProtected)
            {
                builder.Append(segment.Text);
                continue;
            }

            var current = segment.Text;
            foreach (var entry in entries.OrderByDescending(entry => entry.Source.Length))
            {
                var token = $"__WEVITO_GLOSSARY_{replacements.Count:D3}__";
                var options = entry.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                var pattern = $@"(?<![\w]){Regex.Escape(entry.Source)}(?![\w])";
                var replaced = false;
                current = Regex.Replace(
                    current,
                    pattern,
                    match =>
                    {
                        replaced = true;
                        return token;
                    },
                    options);
                if (replaced)
                {
                    replacements.Add(new GlossaryReplacement(token, entry.Source, entry.Target));
                }
            }

            builder.Append(current);
        }

        return new GlossaryProtectedText(builder.ToString(), replacements);
    }

    public string RestoreProtectedTerms(
        string text,
        IReadOnlyList<GlossaryReplacement> replacements)
    {
        var restored = text;
        foreach (var replacement in replacements)
        {
            restored = restored.Replace(replacement.Token, replacement.Target, StringComparison.Ordinal);
        }

        return restored;
    }

    public static IReadOnlyList<string> BuildQaWarnings(
        string sourceText,
        string translatedText,
        IReadOnlyList<TranslationGlossaryEntry> appliedEntries,
        string glossaryMode,
        bool providerFallbackUsed)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(translatedText))
        {
            warnings.Add("target_empty: provider returned an empty translation.");
        }

        var missingPlaceholders = ExtractPlaceholders(sourceText)
            .Where(placeholder => !translatedText.Contains(placeholder, StringComparison.Ordinal))
            .ToArray();
        if (missingPlaceholders.Length > 0)
        {
            warnings.Add($"placeholder_drift: missing {string.Join(", ", missingPlaceholders)}.");
        }

        if (CountOccurrences(sourceText, "```") != CountOccurrences(translatedText, "```") ||
            CountOccurrences(sourceText, "**") != CountOccurrences(translatedText, "**") ||
            CountHeadingMarkers(sourceText) != CountHeadingMarkers(translatedText))
        {
            warnings.Add("markdown_drift: markdown marker counts changed.");
        }

        foreach (var entry in appliedEntries)
        {
            if (!translatedText.Contains(entry.Target, entry.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"glossary_term_not_preserved: expected target term '{entry.Target}'.");
            }
        }

        if (providerFallbackUsed || glossaryMode.Contains("shim", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"fallback_used: {glossaryMode}.");
        }

        return warnings;
    }

    public static string ToDeepLTsv(IReadOnlyList<TranslationGlossaryEntry> entries)
    {
        return string.Join(
            "\n",
            entries.Select(entry => $"{EscapeTsv(entry.Source)}\t{EscapeTsv(entry.Target)}"));
    }

    private GlossaryLanguagePair? FindLanguagePair(string sourceLanguage, string targetLanguage)
    {
        if (!File.Exists(_glossaryPath))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<GlossaryDocument>(File.ReadAllText(_glossaryPath), JsonDefaults.Options);
        var target = NormalizeLanguage(targetLanguage);
        var source = NormalizeLanguage(sourceLanguage);
        return document?.LanguagePairs.FirstOrDefault(pair =>
            string.Equals(NormalizeLanguage(pair.TargetLanguage), target, StringComparison.OrdinalIgnoreCase) &&
            (source == "AUTO" || string.Equals(NormalizeLanguage(pair.SourceLanguage), source, StringComparison.OrdinalIgnoreCase)));
    }

    private static string ResolveDefaultGlossaryPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "vnext", "content", "translation_glossaries.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine("vnext", "content", "translation_glossaries.json"));
    }

    private static string NormalizeLanguage(string language)
    {
        return language.Trim().ToUpperInvariant() switch
        {
            "" => "AUTO",
            "AUTO" => "AUTO",
            "ENGLISH" or "EN-US" or "EN-GB" => "EN",
            "SPANISH" => "ES",
            var code when code.Length >= 2 => code[..2],
            _ => language.Trim().ToUpperInvariant()
        };
    }

    private static bool ContainsTerm(string text, GlossaryEntry entry)
    {
        var options = entry.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return Regex.IsMatch(text, $@"(?<![\w]){Regex.Escape(entry.Source)}(?![\w])", options);
    }

    private static IReadOnlyList<string> ExtractPlaceholders(string text)
    {
        return Regex.Matches(text, @"\{\{[^}]+\}\}")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static int CountOccurrences(string text, string marker)
    {
        if (string.IsNullOrEmpty(marker))
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }

    private static int CountHeadingMarkers(string text)
    {
        return Regex.Matches(text, @"(?m)^#{1,6}\s").Count;
    }

    private static string EscapeTsv(string value)
    {
        return value.Replace("\t", " ", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }

    private sealed record GlossaryDocument(
        string SchemaVersion,
        IReadOnlyList<GlossaryLanguagePair> LanguagePairs);

    private sealed record GlossaryLanguagePair(
        string SourceLanguage,
        string TargetLanguage,
        IReadOnlyList<GlossaryEntry> Entries);

    private sealed record GlossaryEntry(
        string Source,
        string Target,
        bool CaseSensitive,
        string Notes);
}

public sealed record GlossaryProtectedText(
    string Text,
    IReadOnlyList<GlossaryReplacement> Replacements);

public sealed record GlossaryReplacement(
    string Token,
    string Source,
    string Target);

internal static class TranslationTextSegments
{
    public static IReadOnlyList<TranslationTextSegment> Parse(string text)
    {
        var segments = new List<TranslationTextSegment>();
        var lineStart = 0;
        var inFence = false;
        while (lineStart < text.Length)
        {
            var lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length - 1;
            }

            var length = lineEnd - lineStart + 1;
            var line = text.Substring(lineStart, length);
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                segments.Add(new TranslationTextSegment(line, IsProtected: true));
                inFence = !inFence;
            }
            else if (inFence)
            {
                segments.Add(new TranslationTextSegment(line, IsProtected: true));
            }
            else
            {
                AddInlineCodeSegments(line, segments);
            }

            lineStart += length;
        }

        return segments;
    }

    private static void AddInlineCodeSegments(string line, List<TranslationTextSegment> segments)
    {
        var index = 0;
        while (index < line.Length)
        {
            var start = line.IndexOf('`', index);
            if (start < 0)
            {
                segments.Add(new TranslationTextSegment(line[index..], IsProtected: false));
                return;
            }

            if (start > index)
            {
                segments.Add(new TranslationTextSegment(line[index..start], IsProtected: false));
            }

            var end = line.IndexOf('`', start + 1);
            if (end < 0)
            {
                segments.Add(new TranslationTextSegment(line[start..], IsProtected: true));
                return;
            }

            segments.Add(new TranslationTextSegment(line[start..(end + 1)], IsProtected: true));
            index = end + 1;
        }
    }
}

internal sealed record TranslationTextSegment(
    string Text,
    bool IsProtected);
