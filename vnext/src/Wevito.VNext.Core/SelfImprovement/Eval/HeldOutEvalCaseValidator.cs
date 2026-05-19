using System.Text.RegularExpressions;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public abstract record HeldOutEvalCaseValidationResult
{
    public HeldOutEvalCaseValidationResult()
    {
    }

    public HeldOutEvalCaseValidationResult(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public sealed record Valid : HeldOutEvalCaseValidationResult
    {
        public Valid()
        {
        }

        public Valid(KillSwitchService killSwitchService)
        {
            _ = killSwitchService;
        }
    }

    public sealed record Invalid(string Reason) : HeldOutEvalCaseValidationResult
    {
        public Invalid(KillSwitchService killSwitchService)
            : this("")
        {
            _ = killSwitchService;
        }
    }
}

public sealed partial class HeldOutEvalCaseValidator
{
    public HeldOutEvalCaseValidator(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public static HeldOutEvalCaseValidationResult Validate(HeldOutEvalCase candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!IdRegex().IsMatch(candidate.Id ?? ""))
        {
            return Invalid("id_invalid");
        }

        if (!DomainRegex().IsMatch(candidate.Domain ?? ""))
        {
            return Invalid("domain_invalid");
        }

        if (!Sha256Regex().IsMatch(candidate.PromptSha256 ?? ""))
        {
            return Invalid("prompt_sha256_invalid");
        }

        if (!Sha256Regex().IsMatch(candidate.ExpectedKindSha256 ?? ""))
        {
            return Invalid("expected_kind_sha256_invalid");
        }

        if (string.Equals(candidate.PromptSha256, candidate.ExpectedKindSha256, StringComparison.Ordinal))
        {
            return Invalid("hashes_must_differ");
        }

        if (candidate.AuthoredAtUtc <= DateTimeOffset.MinValue)
        {
            return Invalid("authored_at_utc_invalid");
        }

        if ((candidate.Notes ?? "").Length > 1024)
        {
            return Invalid("notes_too_long");
        }

        return new HeldOutEvalCaseValidationResult.Valid();
    }

    private static HeldOutEvalCaseValidationResult.Invalid Invalid(string reason)
    {
        return new HeldOutEvalCaseValidationResult.Invalid(reason);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdRegex();

    [GeneratedRegex("^[a-z][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
