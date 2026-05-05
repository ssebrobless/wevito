using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class TranslationProviderRouter
{
    public IReadOnlyList<TranslationProviderStatus> GetProviderStatuses(
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => entry.Key.ToString() ?? string.Empty,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

        return
        [
            new TranslationProviderStatus(
                TranslationProviderKind.DeepL,
                HasAny(env, "DEEPL_API_KEY", "DEEPL_AUTH_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                SupportsGlossary: true,
                SupportsSelfHosted: false,
                HasAny(env, "DEEPL_API_KEY", "DEEPL_AUTH_KEY")
                    ? "DeepL credentials are present in the process environment."
                    : "Set DEEPL_API_KEY or DEEPL_AUTH_KEY to enable DeepL execution later."),
            new TranslationProviderStatus(
                TranslationProviderKind.GoogleCloudTranslation,
                HasAny(env, "GOOGLE_APPLICATION_CREDENTIALS", "GOOGLE_CLOUD_TRANSLATION_API_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                SupportsGlossary: true,
                SupportsSelfHosted: false,
                HasAny(env, "GOOGLE_APPLICATION_CREDENTIALS", "GOOGLE_CLOUD_TRANSLATION_API_KEY")
                    ? "Google Cloud Translation credentials are present in the process environment."
                    : "Set GOOGLE_APPLICATION_CREDENTIALS or GOOGLE_CLOUD_TRANSLATION_API_KEY for a future Google provider."),
            new TranslationProviderStatus(
                TranslationProviderKind.AzureAiTranslator,
                HasAny(env, "AZURE_TRANSLATOR_KEY")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingCredentials,
                SupportsGlossary: false,
                SupportsSelfHosted: false,
                HasAny(env, "AZURE_TRANSLATOR_KEY")
                    ? "Azure Translator key is present in the process environment."
                    : "Set AZURE_TRANSLATOR_KEY and AZURE_TRANSLATOR_REGION for a future Azure provider."),
            new TranslationProviderStatus(
                TranslationProviderKind.LibreTranslate,
                HasAny(env, "LIBRETRANSLATE_URL")
                    ? TranslationProviderAvailability.Configured
                    : TranslationProviderAvailability.MissingEndpoint,
                SupportsGlossary: false,
                SupportsSelfHosted: true,
                HasAny(env, "LIBRETRANSLATE_URL")
                    ? "LibreTranslate endpoint is present in the process environment."
                    : "Set LIBRETRANSLATE_URL for a future self-hosted/offline-capable provider.")
        ];
    }

    public TranslationProviderStatus SelectPreferredProvider(IReadOnlyList<TranslationProviderStatus> statuses)
    {
        return statuses.FirstOrDefault(status =>
                   status.Provider == TranslationProviderKind.DeepL &&
                   status.Availability == TranslationProviderAvailability.Configured) ??
               statuses.FirstOrDefault(status =>
                   status.Availability == TranslationProviderAvailability.Configured) ??
               statuses.First(status => status.Provider == TranslationProviderKind.DeepL);
    }

    private static bool HasAny(IReadOnlyDictionary<string, string?> environment, params string[] keys)
    {
        return keys.Any(key =>
            environment.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value));
    }
}
