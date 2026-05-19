using System.Text.Json.Serialization;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record InDistributionEvalCase
{
    public const string CaseKindDiscriminator = "in_distribution";

    [JsonConstructor]
    public InDistributionEvalCase(
        string id,
        string domain,
        string promptSha256,
        string expectedKindSha256,
        DateTimeOffset authoredAtUtc,
        string notes = "")
    {
        Id = id;
        Domain = domain;
        PromptSha256 = promptSha256;
        ExpectedKindSha256 = expectedKindSha256;
        AuthoredAtUtc = authoredAtUtc;
        Notes = notes;
    }

    public InDistributionEvalCase(KillSwitchService killSwitchService)
        : this("", "", "", "", DateTimeOffset.MinValue)
    {
        _ = killSwitchService;
    }

    [JsonPropertyName("case_kind")]
    public string CaseKind => CaseKindDiscriminator;

    public string Id { get; init; }

    public string Domain { get; init; }

    public string PromptSha256 { get; init; }

    public string ExpectedKindSha256 { get; init; }

    public DateTimeOffset AuthoredAtUtc { get; init; }

    public string Notes { get; init; }
}
