using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class RegionSelectionStore
{
    private const string DirectoryName = "Wevito";
    private const string FileName = "last_region.json";
    private readonly string _path;

    public RegionSelectionStore(string? path = null)
    {
        _path = string.IsNullOrWhiteSpace(path) ? ResolveDefaultPath() : System.IO.Path.GetFullPath(path);
    }

    public string Path => _path;

    public bool TryLoad(out CaptureRegion region)
    {
        region = new CaptureRegion(0, 0, 0, 0);
        if (!File.Exists(_path))
        {
            return false;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredRegion>(File.ReadAllText(_path), JsonDefaults.Options);
            if (stored is null || stored.Width <= 0 || stored.Height <= 0)
            {
                return false;
            }

            region = new CaptureRegion(stored.X, stored.Y, stored.Width, stored.Height);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Save(CaptureRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Region must have positive width and height.");
        }

        var parent = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var stored = new StoredRegion(region.X, region.Y, region.Width, region.Height);
        File.WriteAllText(_path, JsonSerializer.Serialize(stored, JsonDefaults.Options));
    }

    private static string ResolveDefaultPath()
    {
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DirectoryName,
            FileName);
    }

    private sealed record StoredRegion(int X, int Y, int Width, int Height);
}
