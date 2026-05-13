using System.Text.RegularExpressions;

namespace Wevito.VNext.Core;

public sealed record CitationEnforcementResult(
    string Text,
    double CitationCoverageRatio,
    int TotalSentences,
    int CitedSentences);

public sealed class CitationEnforcer
{
    private static readonly Regex CitationRegex = new(@"\[(\d+)\]", RegexOptions.Compiled);

    public CitationEnforcementResult Enforce(string text, IReadOnlyList<RetrievalChunk> chunks)
    {
        try
        {
            var validNumbers = Enumerable.Range(1, chunks.Count).ToHashSet();
            var sentences = SplitSentences(text);
            if (sentences.Count == 0)
            {
                return new CitationEnforcementResult("(needs citation)", 0, 1, 0);
            }

            var cited = 0;
            var output = new List<string>();
            foreach (var sentence in sentences)
            {
                var hasValidCitation = CitationRegex.Matches(sentence)
                    .Select(match => int.TryParse(match.Groups[1].Value, out var parsed) ? parsed : -1)
                    .Any(validNumbers.Contains);
                if (hasValidCitation)
                {
                    cited++;
                    output.Add(sentence.Trim());
                }
                else
                {
                    output.Add("(needs citation)");
                }
            }

            return new CitationEnforcementResult(
                string.Join(" ", output),
                cited / (double)sentences.Count,
                sentences.Count,
                cited);
        }
        catch
        {
            return new CitationEnforcementResult("(needs citation)", 0, 1, 0);
        }
    }

    private static IReadOnlyList<string> SplitSentences(string text)
    {
        return (text ?? "")
            .ReplaceLineEndings(" ")
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Select(sentence => sentence + ".")
            .ToList();
    }
}
