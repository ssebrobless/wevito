# PET TASKS Translation Preview

Generated: 2026-05-12T20:58:43.1121148+00:00
TaskCard: `c656d0bd-0eff-4c34-88db-5d1bc65c7d78`

## Summary

- Source language: auto
- Target language: Spanish
- Character count: 11
- Preferred provider: DeepL
- Provider called: false
- Did mutate files: false
- Execution provider route: DeepL only in the current executable path; LibreTranslate is surfaced as provider status/self-hosted planning, not automatic execution fallback.
- Fallback policy: if DeepL credentials are missing, execution blocks instead of silently sending text to another provider.

## Applicable Glossary Entries

- `goose` -> `goose` (case-insensitive): Species names stay canonical English until localized species naming is approved.
## Text Preview

```text
Hello goose
```

## Provider Status

- DeepL: MissingCredentials - Set DEEPL_API_KEY or DEEPL_AUTH_KEY to enable DeepL execution later.
- GoogleCloudTranslation: MissingCredentials - Set GOOGLE_APPLICATION_CREDENTIALS or GOOGLE_CLOUD_TRANSLATION_API_KEY for a future Google provider.
- AzureAiTranslator: MissingCredentials - Set AZURE_TRANSLATOR_KEY and AZURE_TRANSLATOR_REGION for a future Azure provider.
- LibreTranslate: MissingEndpoint - Set LIBRETRANSLATE_URL for a future self-hosted/offline-capable provider. Default sidecar endpoint is http://localhost:5000/.

## Safety Notes

- No translation provider was called by this preview.
- No text was sent over the network.
- A future execution adapter must require explicit approval before sending text to a provider.
- Preferred provider is not ready yet: Set DEEPL_API_KEY or DEEPL_AUTH_KEY to enable DeepL execution later.
