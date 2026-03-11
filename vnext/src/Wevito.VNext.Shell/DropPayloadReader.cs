using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Wevito.VNext.Shell;

internal static class DropPayloadReader
{
    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<string> ExtractUrls(IDataObject data)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (data.GetDataPresent(DataFormats.Text))
        {
            foreach (Match match in UrlPattern.Matches((string)data.GetData(DataFormats.Text)!))
            {
                urls.Add(match.Value.Trim());
            }
        }

        if (data.GetDataPresent(DataFormats.UnicodeText))
        {
            foreach (Match match in UrlPattern.Matches((string)data.GetData(DataFormats.UnicodeText)!))
            {
                urls.Add(match.Value.Trim());
            }
        }

        if (data.GetDataPresent(DataFormats.FileDrop))
        {
            var filePaths = (string[])data.GetData(DataFormats.FileDrop)!;
            foreach (var path in filePaths)
            {
                foreach (var url in ExtractUrlsFromFile(path))
                {
                    urls.Add(url);
                }
            }
        }

        return urls.ToList();
    }

    private static IEnumerable<string> ExtractUrlsFromFile(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (extension is not ("url" or "webloc" or "txt"))
        {
            yield break;
        }

        var content = File.ReadAllText(path);
        foreach (Match match in UrlPattern.Matches(content))
        {
            yield return match.Value.Trim();
        }
    }
}
