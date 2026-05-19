using System.Text.Json.Serialization;
using Wevito.VNext.Core;

namespace Wevito.VNext.Core.SelfImprovement.Replay;

[method: JsonConstructor]
public sealed record ReplayResultSummary(
    string OperationId,
    string ResultKind,
    int DiffCount,
    DateTimeOffset ReplayedAtUtc,
    IReadOnlyList<string> FirstTenDiffs)
{
    public ReplayResultSummary(KillSwitchService killSwitchService)
        : this("", "", 0, DateTimeOffset.MinValue, [])
    {
        _ = killSwitchService;
    }
}
