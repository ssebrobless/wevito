using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record GraderTriad(
    bool DeterministicRequired = true,
    bool ProductionLlmAdvisoryOnly = true,
    bool HeldOutDeferred = true)
{
    public static GraderTriad Default { get; } = new();

    public bool ExactOrSubstring(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        return string.Equals(actual?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase) ||
               (actual ?? string.Empty).Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    public bool JsonShapeMatches(string payload, IReadOnlyList<string>? requiredFields)
    {
        if (requiredFields is null || requiredFields.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        using var document = JsonDocument.Parse(payload);
        return requiredFields.All(field => document.RootElement.TryGetProperty(field, out _));
    }

    public bool ContainsExpectedTool(string actualToolFamily, string expectedToolFamily)
    {
        if (string.IsNullOrWhiteSpace(expectedToolFamily))
        {
            return true;
        }

        return string.Equals(actualToolFamily, expectedToolFamily, StringComparison.OrdinalIgnoreCase);
    }

    public static double RecallAt(IReadOnlyList<string> expectedIds, IReadOnlyList<string> actualIds, int k)
    {
        if (expectedIds.Count == 0)
        {
            return 1;
        }

        var top = actualIds.Take(Math.Max(1, k)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return expectedIds.Count(top.Contains) / (double)expectedIds.Count;
    }

    public static double MeanReciprocalRank(IReadOnlyList<string> expectedIds, IReadOnlyList<string> actualIds)
    {
        if (expectedIds.Count == 0)
        {
            return 1;
        }

        var rank = actualIds.ToList().FindIndex(id => expectedIds.Contains(id, StringComparer.OrdinalIgnoreCase)) + 1;
        return rank <= 0 ? 0 : 1d / rank;
    }
}
