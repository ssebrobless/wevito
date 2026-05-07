using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class DeepLTranslationClient
{
    private const int MaxRequestBytes = 128 * 1024;
    private readonly HttpClient _httpClient;
    private readonly string _authKey;
    private readonly Uri _endpoint;

    public DeepLTranslationClient(HttpClient httpClient, string authKey, Uri? endpoint = null)
    {
        _httpClient = httpClient;
        _authKey = string.IsNullOrWhiteSpace(authKey)
            ? throw new ArgumentException("DeepL auth key is required.", nameof(authKey))
            : authKey;
        _endpoint = endpoint ?? ResolveDefaultEndpoint(authKey);
    }

    public async Task<DeepLTranslationResponse> TranslateAsync(
        string text,
        string targetLanguageCode,
        string sourceLanguageCode = "",
        string glossaryId = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Translation text is required.");
        }

        if (Encoding.UTF8.GetByteCount(text) > MaxRequestBytes)
        {
            throw new InvalidOperationException("DeepL translation text exceeds the 128 KiB request limit.");
        }

        if (string.IsNullOrWhiteSpace(targetLanguageCode))
        {
            throw new InvalidOperationException("DeepL target language is required.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["text"] = new[] { text },
            ["target_lang"] = targetLanguageCode,
            ["show_billed_characters"] = true
        };
        if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
        {
            payload["source_lang"] = sourceLanguageCode;
        }
        if (!string.IsNullOrWhiteSpace(glossaryId))
        {
            payload["glossary_id"] = glossaryId;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", _authKey);
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DeepL translation failed with HTTP {(int)response.StatusCode}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<DeepLTranslateEnvelope>(body, JsonDefaults.Options);
        var first = parsed?.Translations?.FirstOrDefault();
        if (first is null || string.IsNullOrWhiteSpace(first.Text))
        {
            throw new InvalidOperationException("DeepL translation response did not include translated text.");
        }

        return new DeepLTranslationResponse(
            first.Text,
            first.DetectedSourceLanguage ?? string.Empty,
            parsed?.BilledCharacters);
    }

    public async Task<string> CreateGlossaryAsync(
        string name,
        string sourceLanguageCode,
        string targetLanguageCode,
        IReadOnlyList<TranslationGlossaryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            throw new InvalidOperationException("At least one glossary entry is required.");
        }

        var glossaryEndpoint = new Uri(_endpoint.GetLeftPart(UriPartial.Authority) + "/v3/glossaries");
        var payload = new
        {
            name,
            dictionaries = new[]
            {
                new
                {
                    source_lang = sourceLanguageCode.ToLowerInvariant(),
                    target_lang = targetLanguageCode.ToLowerInvariant(),
                    entries = TranslationGlossaryService.ToDeepLTsv(entries),
                    entries_format = "tsv"
                }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, glossaryEndpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", _authKey);
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DeepL glossary creation failed with HTTP {(int)response.StatusCode}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<DeepLGlossaryEnvelope>(body, JsonDefaults.Options);
        if (string.IsNullOrWhiteSpace(parsed?.GlossaryId))
        {
            throw new InvalidOperationException("DeepL glossary response did not include a glossary id.");
        }

        return parsed.GlossaryId;
    }

    public static Uri ResolveDefaultEndpoint(string authKey)
    {
        var host = authKey.Trim().EndsWith(":fx", StringComparison.OrdinalIgnoreCase)
            ? "https://api-free.deepl.com/v2/translate"
            : "https://api.deepl.com/v2/translate";
        return new Uri(host);
    }

    private sealed record DeepLTranslateEnvelope(
        [property: JsonPropertyName("translations")]
        IReadOnlyList<DeepLTranslateItem>? Translations,
        [property: JsonPropertyName("billed_characters")]
        int? BilledCharacters);

    private sealed record DeepLTranslateItem(
        [property: JsonPropertyName("detected_source_language")]
        string? DetectedSourceLanguage,
        [property: JsonPropertyName("text")]
        string? Text);

    private sealed record DeepLGlossaryEnvelope(
        [property: JsonPropertyName("glossary_id")]
        string? GlossaryId);
}

public sealed record DeepLTranslationResponse(
    string Text,
    string DetectedSourceLanguage,
    int? BilledCharacters);
