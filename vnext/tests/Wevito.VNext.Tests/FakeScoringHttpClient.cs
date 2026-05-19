using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed record FakeScoringHttpClient : IScoringHttpClient
{
    private readonly string _response;

    public FakeScoringHttpClient(string response)
    {
        _response = response;
    }

    public List<(Uri Uri, string Body)> Calls { get; } = [];

    public override Task<string> PostAsync(Uri uri, string body, CancellationToken cancellationToken)
    {
        Calls.Add((uri, body));
        return Task.FromResult(_response);
    }
}
