using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class BasketService
{
    private readonly int _capacity;

    public BasketService(int capacity)
    {
        _capacity = capacity;
    }

    public IReadOnlyList<BasketItem> Add(BasketItem item, IReadOnlyList<BasketItem> existing)
    {
        var normalized = NormalizeUrl(item.Url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return existing;
        }

        var updated = existing
            .Where(entry => !string.Equals(NormalizeUrl(entry.Url), normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        updated.Insert(0, item with { Url = normalized });
        if (updated.Count > _capacity)
        {
            updated = updated.Take(_capacity).ToList();
        }

        return updated;
    }

    public IReadOnlyList<BasketItem> Remove(Guid id, IReadOnlyList<BasketItem> existing)
    {
        return existing.Where(item => item.Id != id).ToList();
    }

    public BasketItem? Get(Guid id, IReadOnlyList<BasketItem> existing)
    {
        return existing.FirstOrDefault(item => item.Id == id);
    }

    public static bool TryCreate(string url, string label, string source, out BasketItem item)
    {
        var normalized = NormalizeUrl(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            item = null!;
            return false;
        }

        item = new BasketItem(
            Guid.NewGuid(),
            normalized,
            string.IsNullOrWhiteSpace(label) ? normalized : label.Trim(),
            source,
            DateTimeOffset.UtcNow);
        return true;
    }

    public static string NormalizeUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return string.Empty;
        }

        return uri.ToString();
    }
}
