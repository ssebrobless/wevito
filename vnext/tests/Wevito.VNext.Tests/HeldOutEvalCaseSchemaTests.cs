using System.Text.Json;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class HeldOutEvalCaseSchemaTests
{
    private static readonly DateTimeOffset AuthoredAt = DateTimeOffset.Parse("2026-05-19T12:00:00Z");
    private const string PromptSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ExpectedSha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void RoundTrip_WithIndentedJson_ReturnsSameRecord()
    {
        var original = ValidCase();
        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });

        var roundTrip = JsonSerializer.Deserialize<HeldOutEvalCase>(json);

        Assert.Equal(original, roundTrip);
    }

    [Fact]
    public void Validate_HappyPath_ReturnsValid()
    {
        Assert.IsType<HeldOutEvalCaseValidationResult.Valid>(HeldOutEvalCaseValidator.Validate(ValidCase()));
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_InvalidShape_ReturnsReason(HeldOutEvalCase candidate)
    {
        var invalid = Assert.IsType<HeldOutEvalCaseValidationResult.Invalid>(HeldOutEvalCaseValidator.Validate(candidate));

        Assert.False(string.IsNullOrWhiteSpace(invalid.Reason));
    }

    public static IEnumerable<object[]> InvalidCases()
    {
        yield return [ValidCase() with { Id = "" }];
        yield return [ValidCase() with { Id = "-bad" }];
        yield return [ValidCase() with { Domain = "" }];
        yield return [ValidCase() with { Domain = "BadDomain" }];
        yield return [ValidCase() with { PromptSha256 = "abc" }];
        yield return [ValidCase() with { ExpectedKindSha256 = "ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD" }];
        yield return [ValidCase() with { ExpectedKindSha256 = PromptSha }];
        yield return [ValidCase() with { AuthoredAtUtc = DateTimeOffset.MinValue }];
        yield return [ValidCase() with { Notes = new string('x', 1025) }];
    }

    private static HeldOutEvalCase ValidCase()
    {
        return new HeldOutEvalCase(
            "sprite-case-001",
            "sprite-repair",
            PromptSha,
            ExpectedSha,
            AuthoredAt,
            "reviewed test-only case");
    }
}
