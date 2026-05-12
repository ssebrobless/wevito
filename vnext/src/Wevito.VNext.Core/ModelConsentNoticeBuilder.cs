namespace Wevito.VNext.Core;

public sealed record ModelConsentNotice(
    string Provider,
    string WhatIsSent,
    string CredentialStorage,
    string LocalAuditPath,
    string AllowlistSummary,
    string NoToolExecutionStatement,
    string RetentionSummary,
    string TrainingSummary,
    string PrivacyUrl);

public static class ModelConsentNoticeBuilder
{
    public static ModelConsentNotice BuildAnthropicNotice()
    {
        return new ModelConsentNotice(
            "Anthropic",
            "The selected helper name/role, requested tool family, user task text, the read-only tool summary, trusted context, and any untrusted file/web snippets wrapped in <untrusted> tags.",
            "API keys are expected in Windows Credential Manager under Wevito/anthropic/api-key, not plaintext project config.",
            "If a later approved model call runs, Wevito writes a local model-call.json audit record under that PET TASKS artifact folder with provider, model, args hash, decision, and latency.",
            "Only the safe helper/tool allowlist can request a model summary; helper names and pet personality do not grant permission.",
            "Enabling AI helper summaries only permits read-only suggestions. It does not execute tools, mutate sprites, run commands, or bypass PET TASKS approval gates.",
            "Wevito writes a local model-call.json audit record with provider, model, args hash, decision, and latency. It does not store the raw API key.",
            "Provider-side training/retention follows Anthropic account/API terms; confirm current terms before enabling live calls.",
            "https://www.anthropic.com/legal/privacy");
    }
}
