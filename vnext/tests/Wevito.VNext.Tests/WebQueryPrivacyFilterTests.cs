using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class WebQueryPrivacyFilterTests
{
    [Fact]
    public void Sanitize_StripsPrivateLocalMaterial()
    {
        var filter = new WebQueryPrivacyFilter();

        var result = filter.Sanitize("research C:\\Users\\fishe\\secret.txt api_key=abc123 sebastian@example.com %APPDATA%");

        Assert.True(result.Allowed);
        Assert.DoesNotContain("fishe", result.SanitizedQuery, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("abc123", result.SanitizedQuery, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("windows_path", result.Redactions);
        Assert.Contains("credential", result.Redactions);
        Assert.Contains("email", result.Redactions);
        Assert.Contains("env_var", result.Redactions);
    }

    [Fact]
    public void Sanitize_RefusesNearEmptyPrivateOnlyQuery()
    {
        var filter = new WebQueryPrivacyFilter();

        var result = filter.Sanitize("C:\\Users\\fishe\\secret.txt api_key=abc123");

        Assert.False(result.Allowed);
        Assert.Contains("empty", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
