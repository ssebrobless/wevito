using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed record FakeScoringHttpClient : IScoringHttpClient
{
    private readonly string _response;
    private readonly Queue<object> _getResults = new();

    public FakeScoringHttpClient(string response)
    {
        _response = response;
    }

    public List<(Uri Uri, string Body)> Calls { get; } = [];
    public List<Uri> GetCalls { get; } = [];

    public void QueueGetResponse(string response)
    {
        _getResults.Enqueue(response);
    }

    public void QueueGetException(Exception exception)
    {
        _getResults.Enqueue(exception);
    }

    public override Task<string> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        GetCalls.Add(uri);
        if (_getResults.Count == 0)
        {
            return Task.FromResult(_response);
        }

        var next = _getResults.Dequeue();
        return next is Exception exception ? Task.FromException<string>(exception) : Task.FromResult((string)next);
    }

    public override Task<string> PostAsync(Uri uri, string body, CancellationToken cancellationToken)
    {
        Calls.Add((uri, body));
        return Task.FromResult(_response);
    }
}
