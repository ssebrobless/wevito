using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ModelConsentNoticeBuilderTests
{
    [Fact]
    public void BuildAnthropicNotice_ExplainsProviderDataAndCredentialStorage()
    {
        var notice = ModelConsentNoticeBuilder.BuildAnthropicNotice();

        Assert.Equal("Anthropic", notice.Provider);
        Assert.Contains("tool summary", notice.WhatIsSent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows Credential Manager", notice.CredentialStorage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Wevito/anthropic/api-key", notice.CredentialStorage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("model-call.json", notice.LocalAuditPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("allowlist", notice.AllowlistSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not execute tools", notice.NoToolExecutionStatement, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mutate sprites", notice.NoToolExecutionStatement, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("privacy", notice.PrivacyUrl, StringComparison.OrdinalIgnoreCase);
    }
}
