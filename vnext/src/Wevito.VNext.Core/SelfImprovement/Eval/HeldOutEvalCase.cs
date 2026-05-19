using System.Text.Json.Serialization;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record HeldOutEvalCase
{
    [JsonConstructor]
    public HeldOutEvalCase(
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

    public HeldOutEvalCase(KillSwitchService killSwitchService)
        : this("", "", "", "", DateTimeOffset.MinValue)
    {
        _ = killSwitchService;
    }

    public string Id { get; init; }

    public string Domain { get; init; }

    public string PromptSha256 { get; init; }

    public string ExpectedKindSha256 { get; init; }

    public DateTimeOffset AuthoredAtUtc { get; init; }

    public string Notes { get; init; }
}
