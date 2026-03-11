using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BasketServiceTests
{
    private readonly BasketService _service = new(5);

    [Fact]
    public void Add_ReplacesDuplicateUrlsAndKeepsLatestFirst()
    {
        var existing = new List<BasketItem>();
        BasketService.TryCreate("https://example.com/one", "One", "clipboard", out var first);
        BasketService.TryCreate("https://example.com/two", "Two", "clipboard", out var second);
        BasketService.TryCreate("https://example.com/one", "One again", "clipboard", out var replacement);

        existing = _service.Add(first, existing).ToList();
        existing = _service.Add(second, existing).ToList();
        existing = _service.Add(replacement, existing).ToList();

        Assert.Equal(2, existing.Count);
        Assert.Equal("https://example.com/one", existing[0].Url);
        Assert.Equal("One again", existing[0].Label);
    }

    [Fact]
    public void Add_EnforcesCapacity()
    {
        var entries = new List<BasketItem>();
        for (var i = 0; i < 7; i++)
        {
            BasketService.TryCreate($"https://example.com/{i}", $"Item {i}", "clipboard", out var item);
            entries = _service.Add(item, entries).ToList();
        }

        Assert.Equal(5, entries.Count);
        Assert.DoesNotContain(entries, item => item.Url == "https://example.com/0");
        Assert.Equal("https://example.com/6", entries[0].Url);
    }
}
