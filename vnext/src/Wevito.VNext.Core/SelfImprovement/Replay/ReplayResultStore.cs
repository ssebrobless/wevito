using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core.SelfImprovement.Replay;

public sealed class ReplayResultStore
{
    private readonly string _artifactsRoot;
    private readonly KillSwitchService _killSwitch;

    public ReplayResultStore(string artifactsRoot, KillSwitchService killSwitch)
    {
        _artifactsRoot = Path.GetFullPath(artifactsRoot);
        _killSwitch = killSwitch;
    }

    public ReplayResultSummary? GetLatest(string operationId)
    {
        if (_killSwitch.IsActive() || string.IsNullOrWhiteSpace(operationId) || !Directory.Exists(_artifactsRoot))
        {
            return null;
        }

        try
        {
            var directRoot = Path.Combine(_artifactsRoot, Sanitize(operationId.Trim()));
            var candidates = Directory.Exists(directRoot)
                ? Directory.EnumerateFiles(directRoot, "replay-result.json", SearchOption.AllDirectories)
                : Directory.EnumerateFiles(_artifactsRoot, "replay-result.json", SearchOption.AllDirectories);

            return candidates
                .Select(TryRead)
                .Where(summary => summary is not null)
                .Cast<ReplayResultSummary>()
                .Where(summary => summary.OperationId.Equals(operationId.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(summary => summary.ReplayedAtUtc)
                .FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ReplayResultSummary? TryRead(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<ReplayResultSummary>(File.ReadAllText(path), JsonDefaults.Options);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Sanitize(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-')
            .ToArray();
        var sanitized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "operation" : sanitized;
    }
}
