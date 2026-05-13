using System.Text.RegularExpressions;

namespace Wevito.VNext.Core;

public sealed record WebQueryPrivacyFilterResult(
    bool Allowed,
    string SanitizedQuery,
    string Reason,
    IReadOnlyList<string> Redactions);

public sealed class WebQueryPrivacyFilter
{
    private static readonly Regex WindowsPathRegex = new(@"[A-Za-z]:\\[^\s]+", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CredentialRegex = new(@"\b(api[_-]?key|token|secret|password|credential)\s*[:=]\s*\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EnvVarRegex = new(@"%[A-Z0-9_]+%|\$env:[A-Z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public WebQueryPrivacyFilterResult Sanitize(string query)
    {
        var sanitized = query ?? string.Empty;
        var redactions = new List<string>();
        sanitized = Replace(WindowsPathRegex, sanitized, "[windows-path]", "windows_path", redactions);
        sanitized = Replace(EmailRegex, sanitized, "[email]", "email", redactions);
        sanitized = Replace(CredentialRegex, sanitized, "[credential]", "credential", redactions);
        sanitized = Replace(EnvVarRegex, sanitized, "[env-var]", "env_var", redactions);
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        var meaningful = Regex.Replace(sanitized, @"\[[^\]]+\]", "").Trim();
        if (meaningful.Length < 3)
        {
            return new WebQueryPrivacyFilterResult(false, sanitized, "Query is empty or only contains private redacted material.", redactions);
        }

        return new WebQueryPrivacyFilterResult(true, sanitized, "", redactions);
    }

    private static string Replace(Regex regex, string value, string replacement, string label, List<string> redactions)
    {
        if (regex.IsMatch(value))
        {
            redactions.Add(label);
        }

        return regex.Replace(value, replacement);
    }
}
