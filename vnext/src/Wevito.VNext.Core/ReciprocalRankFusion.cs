namespace Wevito.VNext.Core;

public static class ReciprocalRankFusion
{
    public const int DefaultK = 60;

    public static IReadOnlyDictionary<string, double> Fuse(
        IReadOnlyList<string> denseRankedIds,
        IReadOnlyList<string> keywordRankedIds,
        int k = DefaultK)
    {
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        Add(scores, denseRankedIds, k);
        Add(scores, keywordRankedIds, k);
        return scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, double> scores, IReadOnlyList<string> ids, int k)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < ids.Count; index++)
        {
            var id = ids[index];
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
            {
                continue;
            }

            scores[id] = scores.GetValueOrDefault(id) + (1d / (Math.Max(1, k) + index + 1));
        }
    }
}
