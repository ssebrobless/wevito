using System.Windows.Forms;

namespace Wevito.VNext.Broker;

internal static class ClipboardService
{
    public static string TryGetClipboardUrl()
    {
        if (!Clipboard.ContainsText())
        {
            return string.Empty;
        }

        var text = Clipboard.GetText().Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        return uri.Scheme is "http" or "https" ? uri.ToString() : string.Empty;
    }
}
