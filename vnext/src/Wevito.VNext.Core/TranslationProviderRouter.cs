using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class TranslationProviderRouter
{
    private const string SelectedProviderKey = "WEVITO_TRANSLATION_PROVIDER";
    private const string DefaultProviderKey = "WEVITO_TRANSLATION_DEFAULT_PROVIDER";
    private readonly Func<Uri, LibreTranslateClient> _libreTranslateClientFactory;

    public TranslationProviderRouter(Func<Uri, LibreTranslateClient>? libreTranslateClientFactory = null)
    {
        _libreTranslateClientFactory = libreTranslateClientFactory ?? (uri => new LibreTranslateClient(new HttpClient(), uri));
    }

    public IReadOnlyList<TranslationProviderStatus> GetProviderStatuses(
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => entry.Key.ToString() ?? string.Empty,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        return BuildProviderStatuses(env, libreTranslateAvailability: null, libreTranslateDetail: null);
    }

    public async Task<IReadOnlyList<TranslationProviderStatus>> GetProviderStatusesAsync(
        IReadOnlyDictionary<string, string?>? environment = null,
        CancellationToken cancellationToken = default)
    {
        var env = environment ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => entry.Key.ToString() ?? string.Empty,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);
        var endpoint = ResolveLibreTranslateEndpoint(env);
        TranslationProviderAvailability? availability = null;
        string? detail = null;
        if (endpoint is not null)
        {
            try
            {
                var languages = await _libreTranslateClientFactory(endpoint)
                    .GetLanguagesAsync(cancellationToken)
                    .ConfigureAwait(false);
                availability = TranslationProviderAvailability.Available;
                detail = $"LibreTranslate sidecar responded at {endpoint} with {languages.Count} language(s).";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                availability = TranslationProviderAvailability.MissingEndpoint;
                detail = $"LibreTranslate endpoint was configured at {endpoint}, but /languages did not respond: {ex.Message}";
            }
        }

        return BuildProviderStatuses(env, availability, detail);
    }

    public TranslationProviderStatus SelectPreferredProvider(IReadOnlyList<TranslationProviderStatus> statuses)
    {
        return statuses.FirstOrDefault(status =>
                   status.IsUserSelected &&
                   status.Availability is TranslationProviderAvailability.Configured or TranslationProviderAvailability.Available) ??
               statuses.FirstOrDefault(status =>
                   status.IsDefault &&
                   status.Availability is TranslationProviderAvailability.Configured or TranslationProviderAvailability.Available) ??
               statuses.FirstOrDefault(status =>
                   status.Provider == TranslationProviderKind.DeepL &&
                   status.Availability == TranslationProviderAvailability.Configured) ??
               statuses.FirstOrDefault(status =>
                   status.Availability is TranslationProviderAvailability.Configured or TranslationProviderAvailability.Available) ??
               statuses.First(status => status.Provider == TranslationProviderKind.DeepL);
    }

    public string BuildFirstUseConsentSummary(TranslationProviderKind provider, IReadOnlyDictionary<string, string?>? environment = null)
    {
        var env = environment ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        return provider switch
        {
            TranslationProviderKind.LibreTranslate =>
                $"Provider: LibreTranslate. Destination: {ResolveLibreTranslateEndpoint(env) ?? new Uri("http://localhost:5000/")}. Text stays on your machine only if this endpoint is local; retention is controlled by your sidecar.",
            TranslationProviderKind.DeepL =>
                "Provider: DeepL API. Destination: DeepL API endpoint. Text leaves this machine after explicit approval; API key is read from environment and not written to artifacts.",
            _ =>
                $"Provider: {provider}. Destination and retention depend on that provider and require explicit approval before execution."
        };
    }

    private IReadOnlyList<TranslationProviderStatus> BuildProviderStatuses(
        IReadOnlyDictionary<string, string?> env,
        TranslationProviderAvailability? libreTranslateAvailability,
        string? libreTranslateDetail)
    {
        var selectedProvider = ParseProvider(TryGetNonEmpty(env, SelectedProviderKey));
        var defaultProvider = ParseProvider(TryGetNonEmpty(env, DefaultProviderKey));
        var libreEndpoint = ResolveLibreTranslateEndpoint(env);

        TranslationProviderStatus Build(
            TranslationProviderKind provider,
            TranslationProviderAvailability availability,
            bool supportsGlossary,
            bool supportsSelfHosted,
            string detail)
        {
            return new TranslationProviderStatus(
                provider,
                availability,
                supportsGlossary,
                supportsSelfHosted,
                detail,
                IsDefault: defaultProvider == provider,
                IsUserSelected: selectedProvider == provider,
                ConsentSummary: BuildFirstUseConsentSummary(provider, env));
        }

        return
        [
            Build(
                TranslationProviderKind.DeepL,
                HasAny(env, "DEEPL_API_KEY", "DEEPL_AUTH_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                supportsGlossary: true,
                supportsSelfHosted: false,
                HasAny(env, "DEEPL_API_KEY", "DEEPL_AUTH_KEY")
                    ? "DeepL credentials are present in the process environment."
                    : "Set DEEPL_API_KEY or DEEPL_AUTH_KEY to enable DeepL execution later."),
            Build(
                TranslationProviderKind.GoogleCloudTranslation,
                HasAny(env, "GOOGLE_APPLICATION_CREDENTIALS", "GOOGLE_CLOUD_TRANSLATION_API_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                supportsGlossary: true,
                supportsSelfHosted: false,
                HasAny(env, "GOOGLE_APPLICATION_CREDENTIALS", "GOOGLE_CLOUD_TRANSLATION_API_KEY")
                    ? "Google Cloud Translation credentials are present in the process environment."
                    : "Set GOOGLE_APPLICATION_CREDENTIALS or GOOGLE_CLOUD_TRANSLATION_API_KEY for a future Google provider."),
            Build(
                TranslationProviderKind.AzureAiTranslator,
                HasAny(env, "AZURE_TRANSLATOR_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                supportsGlossary: false,
                supportsSelfHosted: false,
                HasAny(env, "AZURE_TRANSLATOR_KEY")
                    ? "Azure Translator key is present in the process environment."
                    : "Set AZURE_TRANSLATOR_KEY and AZURE_TRANSLATOR_REGION for a future Azure provider."),
            Build(
                TranslationProviderKind.LibreTranslate,
                libreTranslateAvailability ??
                (libreEndpoint is not null
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingEndpoint),
                supportsGlossary: false,
                supportsSelfHosted: true,
                libreTranslateDetail ??
                (libreEndpoint is not null
                    ? $"LibreTranslate endpoint is configured at {libreEndpoint}. Run readiness probe to confirm /languages."
                    : "Set LIBRETRANSLATE_URL for a future self-hosted/offline-capable provider. Default sidecar endpoint is http://localhost:5000/."))
        ];
    }

    private static bool HasAny(IReadOnlyDictionary<string, string?> environment, params string[] keys)
    {
        return keys.Any(key =>
            environment.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value));
    }

    private static string? TryGetNonEmpty(IReadOnlyDictionary<string, string?> environment, string key)
    {
        return environment.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static TranslationProviderKind? ParseProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<TranslationProviderKind>(value, ignoreCase: true, out var provider)
            ? provider
            : null;
    }

    private static Uri? ResolveLibreTranslateEndpoint(IReadOnlyDictionary<string, string?> environment)
    {
        var configured = TryGetNonEmpty(environment, "LIBRETRANSLATE_URL");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        return Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            ? uri
            : null;
    }
}
