using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RegionSelectionStoreTests
{
    [Fact]
    public void SaveAndTryLoad_RoundTripsGeometryOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-region-store-tests", Guid.NewGuid().ToString("N"), "last_region.json");
        var store = new RegionSelectionStore(path);
        var region = new CaptureRegion(100, 120, 640, 360);

        store.Save(region);

        Assert.True(store.TryLoad(out var loaded));
        Assert.Equal(region, loaded);
        var json = File.ReadAllText(path);
        Assert.Contains("\"x\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("image", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-region-store-tests", Guid.NewGuid().ToString("N"), "last_region.json");
        var store = new RegionSelectionStore(path);

        Assert.False(store.TryLoad(out var loaded));
        Assert.Equal(new CaptureRegion(0, 0, 0, 0), loaded);
    }
}
