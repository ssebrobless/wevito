using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public class LibreTranslateClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public LibreTranslateClient(HttpClient httpClient, Uri? baseUri = null)
    {
        _httpClient = httpClient;
        _baseUri = NormalizeBaseUri(baseUri ?? new Uri("http://localhost:5000/"));
    }

    public async Task<IReadOnlyList<LibreTranslateLanguage>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(new Uri(_baseUri, "languages"), cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LibreTranslate language probe failed with HTTP {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<IReadOnlyList<LibreTranslateLanguage>>(body, JsonDefaults.Options) ?? [];
    }

    public async Task<LibreTranslateResponse> TranslateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Translation text is required.");
        }

        if (string.IsNullOrWhiteSpace(targetLanguageCode))
        {
            throw new InvalidOperationException("LibreTranslate target language is required.");
        }

        var payload = new
        {
            q = text,
            source = string.IsNullOrWhiteSpace(sourceLanguageCode) ? "auto" : sourceLanguageCode.ToLowerInvariant(),
            target = targetLanguageCode.ToLowerInvariant(),
            format = "text"
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "translate"));
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LibreTranslate translation failed with HTTP {(int)response.StatusCode}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<LibreTranslateResponse>(body, JsonDefaults.Options);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.TranslatedText))
        {
            throw new InvalidOperationException("LibreTranslate response did not include translated text.");
        }

        return parsed;
    }

    private static Uri NormalizeBaseUri(Uri uri)
    {
        var text = uri.ToString();
        return text.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(text + "/");
    }
}

public sealed record LibreTranslateLanguage(
    [property: JsonPropertyName("code")]
    string Code,
    [property: JsonPropertyName("name")]
    string Name);

public sealed record LibreTranslateResponse(
    [property: JsonPropertyName("translatedText")]
    string TranslatedText);

public static class LibreTranslateSetupGuideWriter
{
    public static async Task<string> WriteAsync(string artifactRoot, DateTimeOffset? nowUtc = null, CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        Directory.CreateDirectory(artifactRoot);
        var path = Path.Combine(artifactRoot, "setup-guide.md");
        var markdown = string.Join(Environment.NewLine, new[]
        {
            "# LibreTranslate Local Sidecar Setup",
            "",
            $"Generated: {timestamp:O}",
            "",
            "## What This Enables",
            "",
            "- LibreTranslate can power Wevito translation on your machine through a localhost sidecar.",
            "- Wevito does not auto-install or auto-start the sidecar.",
            "- User text stays on your machine when the endpoint is truly local, e.g. `http://localhost:5000`.",
            "",
            "## Setup",
            "",
            "1. Install LibreTranslate from https://github.com/LibreTranslate/LibreTranslate.",
            "2. Start the service locally, commonly at `http://localhost:5000`.",
            "3. Optional: set `LIBRETRANSLATE_URL` if you use a different local endpoint.",
            "4. In Wevito, preview translation first, then approve execution only when you want text sent to that sidecar.",
            "",
            "## Consent Note",
            "",
            "Before first execution, Wevito should show: provider=LibreTranslate, destination=local endpoint, retention=controlled by your local sidecar, no cloud provider by default."
        }) + Environment.NewLine;
        await File.WriteAllTextAsync(path, markdown, cancellationToken).ConfigureAwait(false);
        return path;
    }
}
